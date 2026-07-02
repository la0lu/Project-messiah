 [Route("api/[controller]/approvals")]
 [ApiController]
 public class PermissionApprovalsController : BaseApiController
 {
     private readonly IUserRoleService _userRoles;
     private readonly IUserPermissionOverrideService _overrides;
     private readonly IRolePermissionService _rolePermissions;

     public PermissionApprovalsController(IUserRoleService userRoles, IUserPermissionOverrideService overrides, IRolePermissionService rolePermissions)
     {
         _userRoles = userRoles;
         _overrides = overrides;
         _rolePermissions = rolePermissions;
     }


     // -------------------------------------------------------------------------
     // Role Permissions
     // -------------------------------------------------------------------------

     [HttpGet("role-permissions/pending")]
     [RequirePermission(PermissionCodes.Roles.Manage)]
     public async Task<IActionResult> GetPendingRolePermissions()
     {
         var result = await _rolePermissions.GetPendingPermissionApprovalsAsync();
         return HandleResult(result);
     }

     [HttpPost("role-permissions/{roleId:int}/{permissionId:int}/approve")]
     [RequirePermission(PermissionCodes.Roles.Manage)]
     public async Task<IActionResult> ApproveRolePermission(
         [FromRoute] int roleId, [FromRoute] int permissionId)
     {
         var result = await _rolePermissions.ApprovePermissionAsync(roleId, permissionId, GetCallerId());
         return HandleResult(result);
     }

     [HttpPost("role-permissions/{roleId:int}/{permissionId:int}/reject")]
     [RequirePermission(PermissionCodes.Roles.Manage)]
     public async Task<IActionResult> RejectRolePermission(
         [FromRoute] int roleId, [FromRoute] int permissionId, [FromBody] RejectApprovalDto dto)
     {
         var result = await _rolePermissions.RejectPermissionAsync(roleId, permissionId, GetCallerId(), dto);
         return HandleResult(result);
     }


     // -------------------------------------------------------------------------
     // User Roles Approvals
     // -------------------------------------------------------------------------

     [HttpGet("user-roles/pending")]
     [RequirePermission(PermissionCodes.Users.AssignRoles)]
     public async Task<IActionResult> GetPendingRoles()
     {
         var result = await _userRoles.GetPendingRoleApprovalsAsync();
         return HandleResult(result);
     }

     [HttpPost("user-roles/{userId:int}/{roleId:int}/approve")]
     [RequirePermission(PermissionCodes.Users.AssignRoles)]
     public async Task<IActionResult> ApproveRole([FromRoute] int userId, [FromRoute] int roleId)
     {
         var result = await _userRoles.ApproveRoleAsync(userId, roleId, GetCallerId());
         return HandleResult(result);
     }

     [HttpPost("user-roles/{userId:int}/{roleId:int}/reject")]
     [RequirePermission(PermissionCodes.Users.AssignRoles)]
     public async Task<IActionResult> RejectRole([FromRoute] int userId, [FromRoute] int roleId, [FromBody] RejectApprovalDto dto)
     {
         var result = await _userRoles.RejectRoleAsync(userId, roleId, GetCallerId(), dto);
         return HandleResult(result);
     }

     // -------------------------------------------------------------------------
     // Overrides Approvals
     // -------------------------------------------------------------------------

     [HttpGet("overrides/pending")]
     [RequirePermission(PermissionCodes.Users.AssignOverrides)]
     public async Task<IActionResult> GetPendingOverrides()
     {
         var result = await _overrides.GetPendingOverrideApprovalsAsync();
         return HandleResult(result);
     }

     [HttpPost("overrides/{overrideId:int}/approve")]
     [RequirePermission(PermissionCodes.Users.AssignOverrides)]
     public async Task<IActionResult> ApproveOverride([FromRoute] int overrideId)
     {
         var result = await _overrides.ApproveOverrideAsync(overrideId, GetCallerId());
         return HandleResult(result);
     }

     [HttpPost("overrides/{overrideId:int}/reject")]
     [RequirePermission(PermissionCodes.Users.AssignOverrides)]
     public async Task<IActionResult> RejectOverride([FromRoute] int overrideId, [FromBody] RejectApprovalDto dto)
     {
         var result = await _overrides.RejectOverrideAsync(overrideId, GetCallerId(), dto);
         return HandleResult(result);
     }
 }