using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WealthMart.Api.DTOs.AuditTrail;
using WealthMart.Api.Common; // adjust to match Result<T> / PagedMetadata<T> namespace
using WealthMart.Data;
using WealthMart.Data.Entities;
using WealthMart.Data.Enums;

namespace WealthMart.Api.Services.AuditTrail
{
    public class AuditTrailService : IAuditTrailService
    {
        private readonly WealthMartContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditTrailService> _logger;

        public AuditTrailService(
            WealthMartContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditTrailService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // -----------------------------------------------------------------
        // Write path
        // -----------------------------------------------------------------

        public async Task LogAsync(AuditTrailEntry entry)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var record = new Data.Entities.AuditTrail
                {
                    UserId = entry.UserId,
                    Username = entry.Username ?? ResolveUsername(httpContext) ?? "unknown",
                    FullName = entry.FullName ?? ResolveFullName(httpContext),
                    Module = entry.Module,
                    Action = entry.Action,
                    EntityType = entry.EntityType,
                    EntityId = entry.EntityId,
                    Description = entry.Description,
                    OldValues = entry.OldValues is null ? null : JsonSerializer.Serialize(entry.OldValues),
                    NewValues = entry.NewValues is null ? null : JsonSerializer.Serialize(entry.NewValues),
                    IsSuccessful = entry.IsSuccessful,
                    ErrorMessage = entry.ErrorMessage,
                    Channel = (byte)ResolveChannel(httpContext),
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    RequestPath = httpContext?.Request?.Path.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditTrails.Add(record);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit logging must never break the business action it's observing.
                _logger.LogError(ex, "Failed to write audit trail record. Module: {Module}, Action: {Action}",
                    entry.Module, entry.Action);
            }
        }

        public Task LogSuccessAsync(int? userId, string module, string action, string? description = null,
            string? entityType = null, string? entityId = null, object? oldValues = null, object? newValues = null)
        {
            return LogAsync(new AuditTrailEntry
            {
                UserId = userId,
                Module = module,
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IsSuccessful = true
            });
        }

        public Task LogFailureAsync(int? userId, string module, string action, string errorMessage,
            string? description = null, string? entityType = null, string? entityId = null)
        {
            return LogAsync(new AuditTrailEntry
            {
                UserId = userId,
                Module = module,
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                IsSuccessful = false,
                ErrorMessage = errorMessage
            });
        }

        // -----------------------------------------------------------------
        // Read path
        // -----------------------------------------------------------------

        public async Task<Result<PagedMetadata<AuditTrailListDto>>> GetPagedAsync(AuditTrailFilterDto filter)
        {
            var query = _context.AuditTrails.AsNoTracking().AsQueryable();

            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Module))
                query = query.Where(a => a.Module == filter.Module);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (filter.IsSuccessful.HasValue)
                query = query.Where(a => a.IsSuccessful == filter.IsSuccessful.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.Timestamp >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.Timestamp <= filter.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var keyword = filter.SearchKeyword.Trim();
                query = query.Where(a =>
                    a.Username.Contains(keyword) ||
                    (a.FullName != null && a.FullName.Contains(keyword)) ||
                    (a.Description != null && a.Description.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditTrailListDto
                {
                    Id = a.Id,
                    Username = a.Username,
                    FullName = a.FullName,
                    Module = a.Module,
                    Action = a.Action,
                    Description = a.Description,
                    IsSuccessful = a.IsSuccessful,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            var paged = new PagedMetadata<AuditTrailListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            _logger.LogInformation("Retrieved {Count} audit trail records for page {PageNumber} (page size {PageSize})",
                items.Count, filter.PageNumber, filter.PageSize);

            return Result<PagedMetadata<AuditTrailListDto>>.SuccessResponse(paged, "Audit trail records retrieved successfully");
        }

        public async Task<Result<AuditTrailDetailDto>> GetByIdAsync(int id)
        {
            var record = await _context.AuditTrails.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record is null)
                return Result<AuditTrailDetailDto>.NotFoundResponse("Audit trail record not found");

            var dto = new AuditTrailDetailDto
            {
                Id = record.Id,
                UserId = record.UserId,
                Username = record.Username,
                FullName = record.FullName,
                Module = record.Module,
                Action = record.Action,
                EntityType = record.EntityType,
                EntityId = record.EntityId,
                Description = record.Description,
                OldValues = record.OldValues,
                NewValues = record.NewValues,
                IsSuccessful = record.IsSuccessful,
                ErrorMessage = record.ErrorMessage,
                Channel = ((AuditChannelEnum)record.Channel).ToString(),
                IpAddress = record.IpAddress,
                UserAgent = record.UserAgent,
                RequestPath = record.RequestPath,
                Timestamp = record.Timestamp
            };

            return Result<AuditTrailDetailDto>.SuccessResponse(dto, "Audit trail record retrieved successfully");
        }

        // -----------------------------------------------------------------
        // Context resolution helpers
        // -----------------------------------------------------------------

        private static string? ResolveUsername(HttpContext? httpContext)
        {
            // Adjust claim type to whatever your JWT actually carries the username as.
            return httpContext?.User?.FindFirst("username")?.Value
                   ?? httpContext?.User?.Identity?.Name;
        }

        private static string? ResolveFullName(HttpContext? httpContext)
        {
            return httpContext?.User?.FindFirst("name")?.Value;
        }

        private static AuditChannelEnum ResolveChannel(HttpContext? httpContext)
        {
            var path = httpContext?.Request?.Path.ToString() ?? string.Empty;
            return path.StartsWith("/client/", StringComparison.OrdinalIgnoreCase)
                ? AuditChannelEnum.External
                : AuditChannelEnum.Internal;
        }
    }
}
