using System;

namespace WealthMart.Api.DTOs.AuditTrail
{
    // Internal carrier used by IAuditTrailService.LogAsync — never exposed via a controller action.
    // OldValues/NewValues accept any object; the service serializes to JSON before persisting.
    public class AuditTrailEntry
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuditTrailFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchKeyword { get; set; } // matches against Username, FullName, Description
        public int? UserId { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public bool? IsSuccessful { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class AuditTrailListDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string? FullName { get; set; }
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AuditTrailDetailDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? FullName { get; set; }
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public string Channel { get; set; } = null!; // resolved display name: "Internal" / "External"
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
