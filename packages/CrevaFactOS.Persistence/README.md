# Persistence Foundation

This project contains only PostgreSQL/EF Core foundations. `CrevaFactOSDbContext` maps the tenant registry and the explicitly infrastructure-only tenant isolation probe; it contains no product entities.

Persistence is disabled by default. When enabled, configuration is validated before the API starts. Migrations are held in `CrevaFactOS.Persistence.Migrations`, use the default `public` schema, and deliberately retain EF Core's standard `__EFMigrationsHistory` table. Applications never apply migrations automatically.

Connection strings and passwords must not be logged. Use `PostgreSqlConnectionFactory.GetSafeEndpointDescription` for diagnostics; it omits the username, password, and complete connection string.

## Tenant isolation

`TenantContextAccessor` is scoped by dependency injection and does not use static or `AsyncLocal` state. Tenant-required code calls `RequireTenantId`, which fails closed when no validated context exists. A context may be established only once per scope.

Tenant-scoped saves assign the current tenant ID to new records and reject missing context, cross-tenant attachment, or tenant-ID mutation. Reads also have EF query filters as defense in depth, but PostgreSQL RLS is the authoritative isolation boundary.

`TenantDatabaseSession.BeginTransactionAsync` starts a transaction and sets `crevafactos.tenant_id` with transaction-local `set_config(..., true)`. Transaction completion clears the value before a pooled connection can be reused.

The ordinary API role is `crevafactos_runtime`: non-superuser, non-owner, and explicitly `NOBYPASSRLS`. The migration role is separate and is never registered in the API. `FORCE ROW LEVEL SECURITY` also covers table-owner access, but PostgreSQL superusers still bypass RLS; superuser credentials are migration/administration-only.
