# Frontend Prompt: Audit Trail (Administration)

## Business context

WealthMart needs a way for admin/compliance users to see a full trail of actions taken by
admin users on the platform — logins/logouts, create/update actions, approval actions, and
so on. This is a compliance and traceability feature for a regulated financial platform: the
key questions it needs to answer are "who did what, when, and did it succeed" and, for a
given record (e.g. a specific client or role), "what happened to it over time."

This is a **read-only reporting feature**. There is no create/edit/delete UI — the backend
writes audit records automatically as a side effect of other actions across the platform.
The frontend's job is purely to surface and filter what's already been recorded.

## Where it lives in the app

New section under **Administration**: **Audit Trail**.

- A new item in the Administration sidebar/menu labeled "Audit Trail"
- This item (and its route) should only be visible/accessible to users who have the
  `AuditTrail.View` permission — same permission-gating pattern already used for other
  Administration sub-sections
- Two screens are needed:
  1. **Audit Trail List** — paginated, filterable table
  2. **Audit Trail Detail** — full detail of a single record, reached by selecting a row from
     the list (modal vs. dedicated page/route is your call — either fits, use whichever is
     more consistent with how other "view details" flows are handled elsewhere in the app)

## API contract

### Authentication & permission

Standard JWT bearer auth, same as every other internal admin endpoint. Both endpoints below
additionally require the `AuditTrail.View` permission — a request without it returns
**403 Forbidden**.

### Endpoint 1 — Paginated list

```
GET /api/AuditTrail
```

Query parameters (`AuditTrailFilterDto` inherits from the shared `QueryParams` base, so the
usual pagination/date/search fields apply exactly as they do on every other paginated list
endpoint in the app):

| Param | Type | Source | Notes |
|---|---|---|---|
| `pageNumber` | int | `QueryParams` | standard pagination |
| `pageSize` | int | `QueryParams` | standard pagination |
| `searchKeyword` | string | `QueryParams` | matches against `Username`, `FullName`, `Description` |
| `dateFrom` | date | `QueryParams` | filters `Timestamp` >= this date |
| `dateTo` | date | `QueryParams` | filters `Timestamp` <= this date |
| `userId` | int, optional | own field | exact match |
| `module` | string, optional | own field | exact match — e.g. `"Auth"`, `"Clients"`, `"Roles"`, `"Workflow"` |
| `action` | string, optional | own field | exact match — e.g. `"Login"`, `"Create"`, `"Update"`, `"Approve"` |
| `isSuccessful` | bool, optional | own field | filters to successful or failed actions only |

There is currently no dedicated lookup/distinct-values endpoint for `module` or `action`.
Treat these as free-text filters, or maintain a small known list client-side if a dropdown is
preferred — either is fine, use your judgment on what fits the existing filter-bar patterns
in the app.

**Response:**

```json
{
  "data": {
    "items": [
      {
        "id": 1042,
        "username": "j.okafor",
        "fullName": "Jide Okafor",
        "module": "Auth",
        "action": "Login",
        "description": "User logged in successfully",
        "isSuccessful": true,
        "timestamp": "2026-07-02T09:14:03Z"
      }
    ],
    "totalCount": 5381,
    "pageNumber": 1,
    "pageSize": 20
  }
}
```

`items[]` is intentionally lean — enough for a table row. Use `id` to fetch full detail.

### Endpoint 2 — Record detail

```
GET /api/AuditTrail/{id}
```

**Response:**

```json
{
  "data": {
    "id": 1042,
    "userId": 88,
    "username": "j.okafor",
    "fullName": "Jide Okafor",
    "module": "Clients",
    "action": "Update",
    "entityType": "Prospect",
    "entityId": "5521",
    "description": "Prospect KYC details updated",
    "oldValues": "{\"Phone\":\"08011112222\",\"Email\":\"old@x.com\"}",
    "newValues": "{\"Phone\":\"08033334444\",\"Email\":\"new@x.com\"}",
    "isSuccessful": true,
    "errorMessage": null,
    "channel": "Internal",
    "ipAddress": "10.0.4.21",
    "userAgent": "Mozilla/5.0 ...",
    "requestPath": "/api/Prospects/5521",
    "timestamp": "2026-07-02T09:15:41Z"
  }
}
```

Returns **404** if the `id` doesn't exist.

## Field reference (for building filters and the detail layout)

| Field | Meaning |
|---|---|
| `userId` | Admin user who performed the action. Can be `null` (e.g. a failed login where the username never resolved to a real account) |
| `username` / `fullName` | Snapshot of the acting user's identity at the time of the action — always populated even if `userId` is null, and even if the user's actual username/name has since changed |
| `module` | Feature area the action belongs to (`Auth`, `Clients`, `Roles`, `Workflow`, etc.) |
| `action` | What was done (`Login`, `Create`, `Update`, `Approve`, etc.) |
| `entityType` / `entityId` | Identifies the specific record acted on (e.g. `"Prospect"` / `"5521"`). Both are `null` for actions with no single target record, like login/logout |
| `description` | Human-readable summary of the action |
| `oldValues` / `newValues` | JSON strings (not objects — `JSON.parse()` before rendering) showing before/after state, populated only for update-type actions. `null` otherwise |
| `isSuccessful` | Whether the action succeeded — the single most important field for scanning the list at a glance |
| `errorMessage` | Populated only when `isSuccessful` is `false` — why it failed |
| `channel` | `"Internal"` (came through the admin `/api/` routes) or `"External"` (came through the API-key-authenticated `/client/` routes). Display-only, not filterable |
| `ipAddress` / `userAgent` / `requestPath` | Request context, shown on the detail view only |
| `timestamp` | UTC — convert to local display time as you do elsewhere in the app |

## Suggested screen behavior (not prescriptive — adapt to existing patterns)

- **List**: table with columns for Username, Module, Action, Description, a Success/Failed
  status badge, and Timestamp. Filter bar above it for the fields listed above. Pagination
  wired to `totalCount`/`pageNumber`/`pageSize` the same way every other paginated list in
  the app already works.
- **Status badge**: this is the field a compliance user will scan for first — give
  success/failure clear, distinct visual treatment.
- **Detail**: flat key/value display for the top-level fields, a distinct "Changes" section
  rendering the `oldValues`/`newValues` diff when present, and a visually distinct "Error"
  section rendering `errorMessage` when the action failed. Request context fields
  (`ipAddress`, `userAgent`, `requestPath`) can sit in a secondary/collapsed area since
  they're mostly relevant for deeper investigation rather than everyday scanning.
- `userId` filter should resolve against a user picker/dropdown if a cached user lookup list
  is already available elsewhere in the app, rather than a raw numeric input — consistent
  with how other screens resolve user references.

No create, edit, or delete actions apply to this feature — every interaction is a GET.
