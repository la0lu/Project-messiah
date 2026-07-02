 public class EffectivePermissionService : IEffectivePermissionService
 {
     private readonly IDbContextFactory<WealthMartContext> _contextFactory;
     private readonly IPermissionCacheService _cache;
     private readonly ILogger<EffectivePermissionService> _logger;

     public EffectivePermissionService(
         IDbContextFactory<WealthMartContext> contextFactory,
         IPermissionCacheService cache,
         ILogger<EffectivePermissionService> logger)
     {
         _contextFactory = contextFactory;
         _cache = cache;
         _logger = logger;
     }

     // -------------------------------------------------------------------------
     // Effective Permission Computation
     // -------------------------------------------------------------------------

     public async Task<HashSet<string>> GetEffectivePermissionsAsync(int userId)
     {
         var cached = await _cache.GetUserPermissionsAsync(userId);
         if (cached is not null) return cached;

         await using var context = await _contextFactory.CreateDbContextAsync();

         // Step 1 — collect permissions from APPROVED roles only, and capture
         //           which RoleIds are already in UserRoles (any status) for
         //           the legacy bridge check below.
         var userRoleRows = await context.UserRoles
             .AsNoTracking()
             .Where(ur => ur.UserId == userId)
             .Select(ur => new
             {
                 ur.RoleId,
                 ur.ApprovalStatus,
                 PermissionCodes = ur.Role.RolePermissions
                     .Where(rp => rp.ApprovalStatus == (int)ApprovalStatusEnum.Approved)
                     .Select(rp => rp.Permission.Code)
             })
             .ToListAsync();

         var rolePermissions = userRoleRows
             .Where(ur => ur.ApprovalStatus == (int)ApprovalStatusEnum.Approved)
             .SelectMany(ur => ur.PermissionCodes)
             .Distinct()
             .ToList();

         var migratedRoleIds = userRoleRows
             .Select(ur => ur.RoleId)
             .ToHashSet();

         var effective = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);

         // Step 3 — apply overrides. Only APPROVED overrides count.
         //           Grants first, denies always win last.
         var overrides = await context.UserPermissionOverrides
             .AsNoTracking()
             .Where(o => o.UserId == userId && o.ApprovalStatus == (int)ApprovalStatusEnum.Approved)
             .Select(o => new { o.Permission.Code, o.IsGranted })
             .ToListAsync();

         foreach (var o in overrides.Where(o => o.IsGranted))
             effective.Add(o.Code);

         foreach (var o in overrides.Where(o => !o.IsGranted))
             effective.Remove(o.Code);

         // Step 5 — super-admin short circuit: expand to all active permissions
         if (effective.Contains(PermissionCodes.System.SuperAdmin))
         {
             var allActive = await context.Permissions
                 .AsNoTracking()
                 .Where(p => p.IsActive)
                 .Select(p => p.Code)
                 .ToListAsync();

             effective = new HashSet<string>(allActive, StringComparer.OrdinalIgnoreCase);
         }

         await _cache.SetUserPermissionsAsync(userId, effective);

         _logger.LogInformation(
             "Computed {Count} effective permissions for user {UserId}",
             effective.Count, userId);

         return effective;
     }

     // -------------------------------------------------------------------------
     // Cache Invalidation
     // -------------------------------------------------------------------------

     public async Task InvalidateAsync(int userId)
     {
         await _cache.InvalidateUserPermissionsAsync(userId);
         _logger.LogInformation("Permission cache invalidated for user {UserId}", userId);
     }
 }