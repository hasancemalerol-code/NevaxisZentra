# CrevaFactOS Local Development Infrastructure

FOUND-02 provides only the local PostgreSQL and RabbitMQ development dependencies. It does not create database schemas, migrations, queues, exchanges, policies, connectors, or product behavior.

## Prerequisites

- Docker Desktop with Docker Compose
- PowerShell 7 or Windows PowerShell 5.1
- pnpm configured according to the root repository prerequisites

Docker Desktop must be running before lifecycle or verification commands are used.

## Environment Setup

The Compose file has safe local defaults. To customize ports or credentials:

```powershell
Copy-Item infrastructure/.env.example infrastructure/.env
```

`infrastructure/.env` is ignored by Git. Values in `.env.example` are development-only examples and must never be reused for staging or production.

## Lifecycle Commands

Run from the repository root:

```powershell
pnpm infra:config
pnpm infra:start
pnpm infra:status
pnpm infra:verify
pnpm infra:logs
pnpm infra:stop
```

`infra:stop` stops containers without deleting their named volumes.

The following command is intentionally destructive and must be invoked explicitly:

```powershell
pnpm infra:reset
```

It removes the local containers, network, and both named data volumes. It is never part of normal shutdown.

## Local Endpoints

| Service | Host | Default port | Local identity |
| --- | --- | ---: | --- |
| PostgreSQL | `127.0.0.1` | `5432` | Database `crevafactos_local`, user `crevafactos` |
| RabbitMQ AMQP | `localhost` | `5672` | User `crevafactos` |
| RabbitMQ Management | `http://localhost:15672` | `15672` | User `crevafactos` |

The example password is `local_dev_only_change_me`. It is intentionally non-secret and suitable only for an isolated developer workstation.

The PostgreSQL development default uses explicit IPv4 (`127.0.0.1`) so a local IPv6/WSL `localhost` relay cannot redirect the application to a different PostgreSQL instance.

## Health Verification

`pnpm infra:verify` waits up to 120 seconds for:

- PostgreSQL container health via `pg_isready`
- RabbitMQ container health via `rabbitmq-diagnostics ping`
- RabbitMQ management API health at `/api/health/checks/local-alarms`

The verifier fails clearly if Docker Desktop is unavailable, a service is absent or unhealthy, credentials do not match, or the management endpoint cannot be reached.

Application `/health/live` never depends on PostgreSQL. `/health/ready` remains independent by default; PostgreSQL is included only when both `CREVAFACTOS__PERSISTENCE__ENABLED` and `CREVAFACTOS__PERSISTENCE__HEALTH_CHECKS_ENABLED` are `true`.

## Application Configuration Binding

`CrevaFactOS.Configuration` provides typed PostgreSQL and RabbitMQ endpoint options. It binds the `Infrastructure` configuration section and the documented `CREVAFACTOS_*` environment variables.

API, Worker, and Edge Agent retain the FOUND-02 infrastructure endpoint options. FOUND-03 additionally gives only the API an optional persistence registration. Worker and Edge Agent do not reference persistence.

## Persistence and Migrations

The API persistence layer is disabled by default. Local commands read `infrastructure/.env` when present, fall back to the documented workstation defaults, and allow process-level `CREVAFACTOS__PERSISTENCE__*` variables to override those values. Copy `.env.example` before changing ports or credentials.

Restore the pinned EF tool once after cloning:

```powershell
dotnet tool restore
```

Run database operations explicitly from the repository root:

```powershell
pnpm db:config
pnpm db:verify
pnpm db:state
pnpm db:migrations:list
pnpm db:migrations:add -- DescriptivePascalCaseName
pnpm db:migrations:apply
pnpm db:migrations:script
pnpm db:migrations:rollback
pnpm db:tenant-isolation:verify
pnpm db:authentication-membership:verify
pnpm auth:verify
pnpm db:permission-authorization:verify
pnpm authorization:verify
```

`db:verify` opens a real Npgsql connection and executes `SELECT 1`. `db:migrations:script` writes an idempotent script under the ignored `artifacts/database` directory. `db:migrations:rollback` is explicitly destructive and moves the application database to migration zero; it does not delete the database or Docker volume.

Migrations never run during API startup. The empty `InitialInfrastructureBaseline` migration creates no business tables, custom schemas, extensions, triggers, functions, roles, or seed data. Applying it creates only EF Core's standard `public.__EFMigrationsHistory` metadata table. The default history table and schema are retained deliberately to avoid inventing a product schema before domain boundaries exist.

Never commit `infrastructure/.env`, passwords, or connection strings. Diagnostics expose only non-secret endpoint metadata; they omit usernames, passwords, and full connection strings.

## Authentication and tenant selection

FOUND-05 keeps `X-CrevaFactOS-Tenant` only as a selector. It may contain a tenant UUID or stable tenant key, but it cannot establish context for an anonymous caller. The API first authenticates issuer/subject claims, resolves an exact active `identity_subjects` row, and then calls `resolve_active_tenant_membership(uuid, text)`. Tenant context is created only when identity, tenant, and membership are all active. Every tenant or membership mismatch uses the same response to prevent enumeration.

`/health/live` and `/health/ready` remain anonymous and tenant-neutral. In Development only, `GET /internal/tenant-context` reports authentication state, identity-resolution state, tenant-context state, and the resolved internal UUIDs. It exposes no claims, tokens, issuer secrets, memberships, lists, or mutation behavior.

Middleware order is explicit: routing, authentication, identity resolution, tenant selection and membership validation, tenant-context establishment, permission authorization, then endpoint execution.

### Authentication modes

- `Disabled` is the repository default while no identity provider is configured.
- `Development` uses the isolated `CrevaFactOS.Development` handler. Deterministic requests provide `X-CrevaFactOS-Dev-Issuer` and `X-CrevaFactOS-Dev-Subject`; both are identity assertions for local fixtures, not passwords or tokens. Registration throws if this mode is selected outside the `Development` environment.
- `JwtBearer` uses ASP.NET Core JWT bearer authentication with configured `Authority`, `Audience`, HTTPS metadata, issuer, audience, lifetime, and signature validation. CrevaFactOS does not issue tokens.

Production must use externally issued bearer tokens and must never enable development headers. No login, registration, password, MFA, or token-issuance endpoint exists.

The API runtime uses `CREVAFACTOS__PERSISTENCE__*`. Explicit EF commands use the separate `CREVAFACTOS__MIGRATIONS__*` settings. The migration role requires narrowly controlled DDL and role-management authority because `AddTenantIsolationFoundation` creates the non-login `crevafactos_runtime` role. Local verification enables login with the development-only runtime password outside the production migration. Production credential provisioning belongs to deployment secret management.

PostgreSQL isolation uses transaction-local `crevafactos.tenant_id`, `current_tenant_id()`, RLS, and `FORCE ROW LEVEL SECURITY`. Missing context produces zero visible rows and rejects writes. `pnpm db:tenant-isolation:verify` creates deterministic temporary tenant A/B fixtures outside migrations, tests reads and all write directions through the non-superuser runtime role, proves pooled physical-connection reuse does not retain context, and removes the fixture rows afterward.

`pnpm db:authentication-membership:verify` is opt-in local bootstrap/verification tooling. It creates deterministic temporary identities, tenants, and memberships through migration credentials, verifies exact identity and active-membership resolution plus membership RLS through the restricted runtime role, and removes all fixtures. Production migrations contain no seed account or credential.

`pnpm auth:verify` additionally starts the API in a hidden Development process, uses the isolated development authentication headers, exercises the real PostgreSQL resolvers over HTTP, compares normalized denial responses, then stops the API and removes fixtures in a `finally` boundary. Its logs are written only under the ignored `artifacts/authentication` directory.

Membership means only that an authenticated subject may enter a tenant. It grants no action permission.

## Permission authorization

FOUND-06 adds deny-by-default authorization. Every protected diagnostic action declares one stable infrastructure permission key. The only seeded definitions are `system.diagnostics.read`, `tenant.context.read`, and `authorization.diagnostics.read`; no tenant role or assignment is seeded. Permission keys use lowercase dot-separated segments and exact, case-sensitive equality. Prefixes, `*`, wildcard matching, inheritance, implicit ownership, global roles, and platform-administrator bypasses do not exist.

Roles belong to one tenant. Composite foreign keys enforce that membership-role and role-permission assignments use the same tenant, while PostgreSQL RLS with `FORCE ROW LEVEL SECURITY` prevents runtime enumeration across tenants. The runtime evaluates one key through `has_tenant_permission(uuid, uuid, uuid, text)`, which revalidates active identity, tenant, membership, role, assignment, and permission definition. The runtime cannot enumerate `permission_definitions`.

`GET /internal/tenant-context` requires `tenant.context.read`. Development-only `GET /internal/authorization-context` and `GET /internal/permission-check/{permissionKey}` require `authorization.diagnostics.read`. They return only current internal identifiers and the requested boolean result. Health endpoints explicitly remain anonymous. Development endpoints are not mapped in Production.

`pnpm db:permission-authorization:verify` creates temporary tenant A/B roles and assignments, checks exact grants and all inactive/cross-tenant cases through the restricted runtime role, verifies composite-FK rejection and RLS/pool isolation, and cleans up. `pnpm authorization:verify` performs the corresponding real Development API checks. Product modules will introduce their own explicit permission definitions later; FOUND-06 adds none.

## Port Conflicts

If `5432`, `5672`, or `15672` is already in use:

1. Copy `.env.example` to `.env`.
2. Change the corresponding host-side port.
3. Run `pnpm infra:config` to inspect the resolved configuration.
4. Restart with `pnpm infra:start`.

Container ports remain unchanged; only workstation ports are overridden.

## Current Boundary

- Before FOUND-03 migration application, PostgreSQL is empty except for its standard initialization metadata. After application, only `public.__EFMigrationsHistory` is added.
- RabbitMQ uses only the minimal default local user and `/` virtual host created by the official image.
- No business tables or business migrations, queues, exchanges, users, additional vhosts, or policies are created.
- EF Core/Npgsql are isolated to the persistence baseline and connectivity tool. No application RabbitMQ client package is installed.
- These services are development dependencies, not CrevaFactOS product modules.
