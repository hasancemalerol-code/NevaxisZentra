# ADR-007: Multi-Tenant Data Isolation

- Status: Accepted
- Date: 2026-08-04
- Decision owners: NevaxisFactOS Product & Development
- Scope: Tenant, company, facility, and data-access isolation

## Context

NevaxisFactOS serves multiple customers through shared applications and infrastructure. A tenant may contain multiple companies, factories, facilities, branches, warehouses, or mold workshops.

No tenant may read, mutate, infer, route, or execute against another tenant’s data or integrations.

## Decision

Tenant is the highest customer isolation boundary. Company and facility are subordinate operational scopes.

Isolation is enforced together at:

1. Authentication and active membership
2. Tenant/company/facility context resolution
3. Permission and operation scope evaluation
4. Application handlers and repositories
5. Database constraints and policies where adopted
6. Messages, jobs, caches, files, search indexes, and telemetry
7. Connector credentials and synchronization state
8. Audit, logs, metrics, and traces

No single layer is considered sufficient.

## Context Rules

- Tenant context comes from a trusted authenticated membership, not a free-form request field.
- Users switching tenant or facility must establish a new validated active context.
- Background work carries an explicit tenant and originating correlation context.
- Platform support access is time-bound, purpose-bound, least-privilege, and audited.
- AI cannot choose or change tenant, company, facility, role, permission, or approval scope.

## Data Rules

- Tenant-scoped records include `tenant_id`.
- Composite uniqueness and foreign keys include tenant scope where required to prevent cross-tenant references.
- Queries fail closed when tenant context is absent.
- Cache, object, and queue keys are tenant-namespaced.
- Files and telemetry cannot be retrieved through guessable identifiers without authorization.
- External connector credentials and mappings are isolated per tenant and environment.

## Authorization

Roles are permission bundles, not the final access decision. Authorization evaluates authenticated user, active membership, tenant, company, facility, permission, resource scope, delegation, approval scope, operation limit, and current state.

Delegation cannot grant permissions the delegator does not hold. It is explicit, scoped, time-bound, revocable, and audited.

## Verification

Every tenant-aware module requires negative isolation tests covering reads, writes, lists, exports, messages, background jobs, caches, files, and integration callbacks. Tests must use at least two tenants and verify that identifiers from one tenant cannot be resolved in another.

## Consequences

- Convenience APIs that permit optional tenant context are prohibited for tenant data.
- Operational tooling must expose scope without leaking sensitive cross-tenant values.
- A tenant-isolation failure is a security incident and blocks release.
