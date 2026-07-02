# Roles & Permissions — Backend Refactor Brief (Frontend Alignment)

## Purpose

This document describes the current state of the backend roles and permissions
implementation following a significant refactor. It covers two distinct changes:
a maker-checker approval gate introduced across all access grant surfaces, and
the removal of the legacy permission bridge that previously translated old JWT
boolean flags into effective permissions. Read this fully before making any
frontend decisions — the changes affect API contracts, response shapes, and
the overall session initialisation flow.

---

## Change 1 — Maker-Checker on All Access Grants

### The Design Decision

Every action that *adds* access — assigning a role to a user, granting a
permission override to a user, or adding a permission to a role bundle —
now creates a pending record that does not take effect until a second admin
approves it. The person who submitted the request cannot be the same person
who approves it. This is enforced on the backend at the service and database
level.

Every action that *removes or restricts* access remains immediate with no
approval gate. This asymmetry is intentional — removing access is always
defensive, adding access is always the risk vector.

The three surfaces this applies to:

**1. Adding a permission to a role** — when an admin bundles a new permission
into a role, it is created as `Pending` and does not contribute to any user's
effective permissions until approved. Removing a permission from a role is
immediate.

**2. Assigning a role to a user** — when a role is assigned to a user, the
assignment is created as `Pending` and the user gains none of the role's
permissions until a second admin approves it. Removing a role from a user
is immediate.

**3. Granting a permission override to a user** — when an override with
`isGranted: true` is set for a user, it is created as `Pending` and does
not take effect until approved. An override with `isGranted: false` (an
explicit deny) is applied immediately, consistent with the removal-direction
asymmetry. Removing an override entirely is also immediate.

### Approval Status

All three of the above entities now carry an `approvalStatus` field on every
row. The possible integer values and their string names:

| Value | Name |
|---|---|
| `0` | `Pending` |
| `1` | `Approved` |
| `2` | `Rejected` |

All API responses that return these entities now include both `approvalStatus`
(integer, for any logic) and `approvalStatusName` (string, for direct display).

### What "Pending" Means Practically

A pending role assignment or permission grant is visible in API list responses
(so the UI can show its existence and status) but it is completely inactive —
it does not appear in a user's effective permissions and the backend will not
act on it for any authorization decision. It only becomes active when a second
admin approves it.

---

## Change 2 — Legacy Permission Bridge Removed

### What Was There Before

The effective permission computation previously contained a bridge block that
read two legacy boolean columns from the `AdminUsers` table —
`IsAdministrator` and `IsSecondApprover` — and translated them into permission
codes when computing what a user could do. Similarly, a legacy single
`RoleId` foreign key on `AdminUsers` was read as a fallback for users who
had not yet been migrated to the new many-to-many `UserRoles` table.

This bridge was always intended as a temporary migration shim, not permanent
architecture.

### Current State

Both bridge blocks have been removed entirely from the effective permission
computation. The backend now resolves a user's effective permissions exclusively
from:

1. Their approved `UserRoles` assignments → the approved `RolePermissions`
   bundled into those roles
2. Their approved `UserPermissionOverrides` grants and denies
3. A super-admin short circuit: if the resolved set contains
   `system.super-admin`, all active permission codes are substituted in place
   of the computed set

Nothing else feeds into the computation. The legacy boolean flags
(`isAdministrator`, `isSecondApprover`, `isSalesRm`, `isNonSalesRm`,
`isUnitHead`) and the legacy `role` claim on the JWT are no longer used by
the backend for any authorization decision. They still exist on the JWT token
because the token shape was not changed, but the backend ignores them entirely
for permission resolution.

### Implication for the Frontend

Any part of the frontend that currently makes access decisions based on the
legacy boolean claims from the JWT — `isAdministrator`, `isSecondApprover`,
`isSalesRm`, `isNonSalesRm`, `isUnitHead` — or based on the `role` string
claim is now operating on data the backend no longer honours. These frontend
checks will diverge from what the backend actually enforces. All access
decisions in the frontend must derive exclusively from the permission code
list fetched from `GET /api/auth/me/permissions` after login.

---

## Current API Surface — Complete Reference

### Authentication

```
GET /api/auth/me/permissions
Authorization: Bearer {token}

Response: 200 OK
["clients.view", "kyc.view", "portfolio.view", ...]
```

Returns the flat list of approved effective permission codes for the currently
authenticated user. This is the only source of truth for frontend access
decisions. Unchanged from before — but the computation behind it is now
stricter (legacy bridge removed, only approved grants count).

---

### Permissions

```
GET    /api/permissions?pageNumber=1&searchKeyword=
GET    /api/permissions/all
GET    /api/permissions/module/{module}
GET    /api/permissions/{id}
POST   /api/permissions
PUT    /api/permissions/{id}
PATCH  /api/permissions/{id}/toggle
```

No changes to these endpoints. They manage the master list of permission
definitions and are not affected by the maker-checker refactor.

---

### Roles

```
GET    /api/roles
GET    /api/roles/paginated?...
GET    /api/roles/{id}
POST   /api/roles
PUT    /api/roles/{id}
PATCH  /api/roles/{id}/toggle-status
```

No changes to role CRUD. The following permission endpoints on roles have
changed:

```
GET    /api/roles/{roleId}/permissions
```

Response now includes `approvalStatus` and `approvalStatusName` on each
permission entry. Rejected entries are excluded. Pending entries are included
so the UI can display their in-flight state.

```
POST   /api/roles/{roleId}/permissions/{permissionId}
```

Previously this added the permission immediately. Now it creates a pending
request. Response message will indicate the request is pending approval.

```
DELETE /api/roles/{roleId}/permissions/{permissionId}
```

Unchanged — immediate removal, no approval step.

The following endpoint has been removed (was a bulk full-replace):
```
PUT /api/roles/{roleId}/permissions   ← REMOVED
```

---

### User Roles

```
GET /api/users/{userId}/roles
```

Response now includes `approvalStatus` and `approvalStatusName` on each role
entry. Rejected entries are excluded. Pending entries are included.

```
POST /api/users/{userId}/roles/{roleId}
```

Previously this assigned the role immediately. Now it creates a pending
request. Response message will indicate the request is pending approval.

```
DELETE /api/users/{userId}/roles/{roleId}
```

Unchanged — immediate removal.

The following endpoints have been removed:
```
PUT  /api/users/{userId}/roles          ← REMOVED (bulk replace incompatible with maker-checker)
POST /api/users/{userId}/roles/{roleId} (old AddRole variant) ← REMOVED
```

---

### User Permission Overrides

```
GET /api/users/{userId}/overrides
```

Response now includes `approvalStatus` and `approvalStatusName` on each
override entry.

```
POST /api/users/{userId}/overrides
Body: { "permissionId": int, "isGranted": bool, "reason": string? }
```

Behaviour now branches on `isGranted`:
- `isGranted: true` → creates a pending grant request, not yet active
- `isGranted: false` → applied immediately as a deny, active instantly

Response message will differ accordingly — "pending approval" for grants,
immediate confirmation for denies.

```
DELETE /api/users/{userId}/overrides/{permissionId}
```

Unchanged — immediate removal.

---

### User Effective Permissions

```
GET /api/users/{userId}/permissions/effective
```

Response shape unchanged structurally, but override entries now include
`approvalStatus` and `approvalStatusName` so an admin viewing this screen
can distinguish which overrides are actually active versus pending.

---

### Approvals Queue — New Controller

All approval actions live under a single dedicated controller. The route
prefix is derived from `[Route("api/[controller]/approvals")]` with the
controller named `PermissionApprovals`, making the base path:

```
/api/permissionapprovals/approvals/
```

#### Role Permission Approvals

```
GET  /api/permissionapprovals/approvals/role-permissions/pending
```

Returns all pending requests to add a permission to a role.

**Response item shape:**
```json
{
  "roleId": 1,
  "roleName": "Relationship Manager",
  "permissionId": 5,
  "permissionCode": "liquidation.initiate",
  "permissionName": "Initiate Liquidation",
  "assignedBy": 7,
  "assignedByName": "asmith",
  "assignedAt": "2025-01-15T08:00:00Z"
}
```

```
POST /api/permissionapprovals/approvals/role-permissions/{roleId}/{permissionId}/approve
POST /api/permissionapprovals/approvals/role-permissions/{roleId}/{permissionId}/reject
     Body (reject): { "reason": "optional string" }
```

Required permission for all three: `roles.manage`

---

#### User Role Approvals

```
GET  /api/permissionapprovals/approvals/user-roles/pending
```

**Response item shape:**
```json
{
  "userId": 42,
  "userName": "jdoe",
  "roleId": 3,
  "roleName": "KYC Reviewer",
  "assignedBy": 7,
  "assignedByName": "asmith",
  "assignedAt": "2025-01-15T08:00:00Z"
}
```

```
POST /api/permissionapprovals/approvals/user-roles/{userId}/{roleId}/approve
POST /api/permissionapprovals/approvals/user-roles/{userId}/{roleId}/reject
     Body (reject): { "reason": "optional string" }
```

Required permission for all three: `users.assign-roles`

---

#### Override Approvals

```
GET  /api/permissionapprovals/approvals/overrides/pending
```

**Response item shape:**
```json
{
  "id": 12,
  "userId": 42,
  "userName": "jdoe",
  "permissionId": 5,
  "permissionCode": "liquidation.initiate",
  "permissionName": "Initiate Liquidation",
  "reason": "Temporary access for Q1 close",
  "createdBy": 7,
  "createdByName": "asmith",
  "createdAt": "2025-01-15T08:00:00Z"
}
```

```
POST /api/permissionapprovals/approvals/overrides/{overrideId}/approve
POST /api/permissionapprovals/approvals/overrides/{overrideId}/reject
     Body (reject): { "reason": "optional string" }
```

Required permission for all three: `users.assign-overrides`

---

## Self-Approval Prevention

The backend enforces that the person who submitted a request cannot approve
it. If an approve action is attempted by the same user who submitted it, the
backend returns `400 Bad Request` with a clear message. The submitter's
identity is available on pending items (`assignedBy`/`assignedByName` or
`createdBy`/`createdByName`) and can be compared against the current logged-in
user's ID client-side to inform the UI — but this is purely a UX nicety, the
backend enforces it regardless.

---

## Approval Status Values — Quick Reference

These integer values and their string counterparts appear across all three
entity types wherever `approvalStatus` and `approvalStatusName` are returned:

| `approvalStatus` | `approvalStatusName` | Meaning |
|---|---|---|
| `0` | `Pending` | Submitted, awaiting a second approver. Not yet active. |
| `1` | `Approved` | Active and enforced. |
| `2` | `Rejected` | Declined. Excluded from list responses. |

---

## Permission Codes — No Changes

The permission codes table is unchanged from the previous implementation.
All codes defined previously remain valid. No new permission codes were
introduced by this refactor — the same `roles.manage`, `users.assign-roles`,
and `users.assign-overrides` codes that already guarded the request actions
also guard the approve and reject actions.

---

## Summary of Breaking Changes

| What changed | Previous behaviour | Current behaviour |
|---|---|---|
| Add permission to role | Immediate | Creates pending request |
| Assign role to user | Immediate | Creates pending request |
| Grant override (`isGranted: true`) | Immediate | Creates pending request |
| Role list response per user | No `approvalStatus` field | Includes `approvalStatus`, `approvalStatusName` |
| Role permissions list response | No `approvalStatus` field | Includes `approvalStatus`, `approvalStatusName` |
| Override list response | No `approvalStatus` field | Includes `approvalStatus`, `approvalStatusName` |
| `PUT /api/roles/{roleId}/permissions` | Full replace endpoint existed | Endpoint removed |
| `PUT /api/users/{userId}/roles` | Full replace endpoint existed | Endpoint removed |
| Effective permission computation | Read legacy `IsAdministrator`, `IsSecondApprover` booleans and legacy `RoleId` FK | Reads only approved `UserRoles` → approved `RolePermissions`, and approved `UserPermissionOverrides` |
| JWT boolean flags | Used by backend for permission bridging | Ignored by backend entirely |
