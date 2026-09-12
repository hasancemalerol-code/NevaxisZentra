# ADR-004: PostgreSQL Primary Database

- Status: Accepted
- Date: 2026-08-04
- Decision owners: NevaxisFactOS Product & Development
- Scope: Native operational persistence and synchronized projections

## Context

NevaxisFactOS requires relational integrity, transactions, constraints, concurrency control, and tenant isolation for native factory-operation records. It also needs selected projections from systems that remain authoritative for classic ERP data.

Using PostgreSQL as the NevaxisFactOS database does not transfer ownership of externally governed records.

## Decision

PostgreSQL is the primary database for NevaxisFactOS. Entity Framework Core is the default data-access mechanism. Measured performance-critical queries may use reviewed parameterized SQL.

PostgreSQL stores:

- Tenant, membership, permission, and operational scope data
- Native production and shift plans
- AI Task Center tasks and approvals
- Machine intelligence and normalized telemetry references
- Mold lifecycle and operational maintenance history
- Quality, scrap, material, supplier, delivery, and bottleneck analysis
- Audit and AI Decision Ledger records
- Transactional outbox and inbox/idempotency records
- Connector mappings, checkpoints, and synchronization outcomes
- Minimal operational projections from external source systems

It is not the authoritative ledger for finance, accounting, receivables/payables, official stock, invoicing, payroll, classic purchasing, or external master data.

## Tenant Isolation

Tenant-scoped tables include `tenant_id`. Facility- or company-scoped records also include the appropriate scope identifier. Isolation is enforced through application context, repository filters, constraints, database policies where adopted, and negative isolation tests.

No background job, integration, report, or administrative endpoint may run tenant-scoped access with an implicit tenant.

## External Projections

Every externally owned projection includes, as applicable:

```text
tenant_id
source_system
external_id
mapping_version
source_updated_at
synchronized_at
freshness_status
payload_version
```

Projection tables use names and APIs that make external ownership clear. They must not masquerade as NevaxisFactOS-owned master or ledger tables.

## Integrity and Audit

- Primary keys are stable and globally unambiguous.
- Timestamps are stored in UTC.
- Concurrency-sensitive approvals and executions use optimistic or explicit locking.
- Audit and AI Decision Ledger retention follows an append-oriented model.
- Integration idempotency keys are uniquely constrained within their operation scope.
- Schema changes use reviewed migrations.

## Consequences

- Shared database does not permit cross-module table access.
- Backup, restore, migration, and tenant-isolation procedures must be tested.
- Data ownership remains governed by ADR-011 regardless of physical storage location.
