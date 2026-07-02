-- Run this against the WealthMart database first (DB-first workflow).
-- After running, scaffold AuditTrail into WealthMart.Data (targeted single-table scaffold)
-- and merge into WealthMartContext per your usual pattern, OR just add the hand-written
-- entity + OnModelCreating block provided alongside this script.

CREATE TABLE AuditTrails (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NULL,                  -- nullable: covers pre-auth events e.g. failed login with unknown user
    Username        NVARCHAR(100) NOT NULL,    -- denormalized snapshot, survives username changes
    FullName        NVARCHAR(200) NULL,        -- denormalized snapshot
    Module          NVARCHAR(100) NOT NULL,    -- e.g. "Clients", "Roles", "Workflow", "Auth"
    Action          NVARCHAR(100) NOT NULL,    -- e.g. "Login", "Create", "Update", "Approve"
    EntityType      NVARCHAR(100) NULL,        -- e.g. "Prospect", "RolePermission"
    EntityId        NVARCHAR(50) NULL,         -- string to uniformly cover int/guid keys
    Description     NVARCHAR(500) NULL,
    OldValues       NVARCHAR(MAX) NULL,        -- JSON snapshot, for updates
    NewValues       NVARCHAR(MAX) NULL,        -- JSON snapshot, for updates
    IsSuccessful    BIT NOT NULL,
    ErrorMessage    NVARCHAR(1000) NULL,
    Channel         TINYINT NOT NULL,          -- 1 = Internal (/api/), 2 = External (/client/)
    IpAddress       NVARCHAR(50) NULL,
    UserAgent       NVARCHAR(500) NULL,
    RequestPath     NVARCHAR(300) NULL,
    Timestamp       DATETIME2 NOT NULL CONSTRAINT DF_AuditTrails_Timestamp DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_AuditTrails_UserId_Timestamp ON AuditTrails(UserId, Timestamp DESC);
CREATE INDEX IX_AuditTrails_Module_Action ON AuditTrails(Module, Action);
CREATE INDEX IX_AuditTrails_Timestamp ON AuditTrails(Timestamp DESC);
GO
