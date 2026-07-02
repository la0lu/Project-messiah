using System;

namespace WealthMart.Data.Entities
{
    // Channel stored as byte (matches TINYINT) rather than the enum type directly.
    // Per convention: avoid CLR enum casting inside IQueryable — convert to/from
    // AuditChannelEnum in the service layer after materialization, not in projections.
    public partial class AuditTrail
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
        public byte Channel { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

/*
Add to WealthMartContext.OnModelCreating:

modelBuilder.Entity<AuditTrail>(entity =>
{
    entity.ToTable("AuditTrails");

    entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
    entity.Property(e => e.FullName).HasMaxLength(200);
    entity.Property(e => e.Module).HasMaxLength(100).IsRequired();
    entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
    entity.Property(e => e.EntityType).HasMaxLength(100);
    entity.Property(e => e.EntityId).HasMaxLength(50);
    entity.Property(e => e.Description).HasMaxLength(500);
    entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
    entity.Property(e => e.IpAddress).HasMaxLength(50);
    entity.Property(e => e.UserAgent).HasMaxLength(500);
    entity.Property(e => e.RequestPath).HasMaxLength(300);
    entity.Property(e => e.Timestamp).HasDefaultValueSql("(sysutcdatetime())");

    entity.HasIndex(e => new { e.UserId, e.Timestamp });
    entity.HasIndex(e => new { e.Module, e.Action });
    entity.HasIndex(e => e.Timestamp);
});

Add to the DbSet properties:
public virtual DbSet<AuditTrail> AuditTrails { get; set; } = null!;
*/
