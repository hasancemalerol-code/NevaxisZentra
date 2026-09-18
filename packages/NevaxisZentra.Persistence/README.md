# Persistence Foundation

This project contains only PostgreSQL/EF Core foundations. `NevaxisZentraDbContext` maps the tenant registry, identity subjects, active-membership bindings, tenant permission authorization, immutable audit evidence, and the explicitly infrastructure-only tenant isolation probe; it contains no product entities.

Persistence is disabled by default. When enabled, configuration is validated before the API starts. Migrations are held in `NevaxisZentra.Persistence.Migrations`, use the default `public` schema, and deliberately retain EF Core's standard `__EFMigrationsHistory` table. Applications never apply migrations automatically.

Connection strings and passwords must not be logged. Use `PostgreSqlConnectionFactory.GetSafeEndpointDescription` for diagnostics; it omits the username, password, and complete connection string.

## Tenant isolation

`TenantContextAccessor` is scoped by dependency injection and does not use static or `AsyncLocal` state. Tenant-required code calls `RequireTenantId`, which fails closed when no validated context exists. A context may be established only once per scope.

Tenant-scoped saves assign the current tenant ID to new records and reject missing context, cross-tenant attachment, or tenant-ID mutation. Reads also have EF query filters as defense in depth, but PostgreSQL RLS is the authoritative isolation boundary.

`TenantDatabaseSession.BeginTransactionAsync` starts a transaction and sets `NevaxisZentra.tenant_id` with transaction-local `set_config(..., true)`. Transaction completion clears the value before a pooled connection can be reused.

The ordinary API role is `crevafactos_runtime`: non-superuser, non-owner, and explicitly `NOBYPASSRLS`. The migration role is separate and is never registered in the API. `FORCE ROW LEVEL SECURITY` also covers table-owner access, but PostgreSQL superusers still bypass RLS; superuser credentials are migration/administration-only.

## Identity and membership boundary

`identity_subjects` stores only the internal UUID, exact external issuer/subject pair, status, and creation time. It stores no password or profile data. The runtime role cannot select this table directly; `resolve_active_identity(text, text)` exposes only an exact active mapping.

`tenant_memberships` grants entry to one tenant but grants no role or permission. `resolve_active_tenant_membership(uuid, text)` revalidates active identity, tenant, and membership before returning the minimum tenant context. Direct membership reads remain protected by tenant RLS and `FORCE ROW LEVEL SECURITY`.

`CurrentIdentityAccessor` and `TenantContextAccessor` are independent scoped services. Establishing authenticated identity never establishes tenant context by itself.

## Permission boundary

`PermissionDefinition` is a global stable-key catalog, but the runtime can only evaluate one exact key through the narrow `has_tenant_permission` function. `TenantRole`, `TenantRolePermissionAssignment`, and `TenantMembershipRole` are tenant-scoped and protected by EF query filters plus forced PostgreSQL RLS. Composite tenant foreign keys reject cross-tenant assignments at the database boundary.

`IAuthorizationContext` is scoped and becomes available only after both identity and membership-derived tenant context exist. `IPermissionEvaluator` grants only when every identity, tenant, membership, role, assignment, and permission status is active and the key exactly matches. There is no cross-request cache, role-name authorization, wildcard, inheritance, global administrator, or direct membership permission.

## Audit boundary

`IAuditWriter` validates the fixed infrastructure taxonomy, obtains the scoped identity/tenant/membership and correlation context, sanitizes bounded metadata, and calls the narrow append function. It returns a safe success or failure code and never recursively audits its own failure. Security-sensitive deny paths may attempt best-effort audit without exposing details; future state-changing operations may require durable audit before completion, but that transaction policy is not implemented here.

Runtime audit rows cannot be updated, deleted, truncated, or inserted directly. Tenant reads use forced RLS and a fixed recent-record limit. Migration/administration authority can temporarily disable the mutation trigger only for explicit local fixture cleanup or future controlled maintenance; no runtime maintenance path exists. Retention and archival are intentionally deferred.
