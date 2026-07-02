using System.Threading.Tasks;
using WealthMart.Api.DTOs.AuditTrail;
// Adjust these two usings to match wherever Result<T> / PagedMetadata<T> actually live in your project.
using WealthMart.Api.Common;

namespace WealthMart.Api.Services.AuditTrail
{
    public interface IAuditTrailService
    {
        /// <summary>
        /// Writes an audit record synchronously. Never throws — write failures are logged
        /// via ILogger and swallowed so a business action can never fail because of audit logging.
        /// </summary>
        Task LogAsync(AuditTrailEntry entry);

        /// <summary>
        /// Convenience wrapper for the common success case. Pulls Username/FullName/IP/UserAgent/
        /// Channel from the current HttpContext automatically — callers only supply the business bits.
        /// </summary>
        Task LogSuccessAsync(int? userId, string module, string action, string? description = null,
            string? entityType = null, string? entityId = null, object? oldValues = null, object? newValues = null);

        /// <summary>
        /// Convenience wrapper for the common failure case (e.g. failed login, rejected update).
        /// </summary>
        Task LogFailureAsync(int? userId, string module, string action, string errorMessage,
            string? description = null, string? entityType = null, string? entityId = null);

        Task<Result<PagedMetadata<AuditTrailListDto>>> GetPagedAsync(AuditTrailFilterDto filter);

        Task<Result<AuditTrailDetailDto>> GetByIdAsync(int id);
    }
}
