# ADR-002: ASP.NET Core Backend Platform

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: API, Worker, integration, and Edge backend technology

## Context

CrevaFactOS requires strong type safety, transactional consistency, long-term support, observability, and controlled integration with ERP and factory systems. It is an AI Factory Operations Assistant, not the system of record for classic ERP functions.

The backend must support multi-tenant operational planning, AI Task Center, approvals, audit, AI Decision Ledger, transactional messaging, integrations, and Edge connectivity.

## Decision

ASP.NET Core on `net10.0` is the primary backend platform.

The following applications use .NET:

- Web API
- Background Worker
- Outbox publisher and message consumers
- Integration services
- Edge Agent and device adapters
- Scheduled operational jobs

The API is a modular monolith. Worker and Edge Agent are separate executable applications. AI orchestration may be deployed separately but all business authorization and execution rules remain enforced by trusted backend application boundaries.

## Product Boundary

The backend owns native production plans, shift plans, machine and mold intelligence, AI tasks, approvals, audit, AI decisions, and integration state. Externally owned customer, supplier, personnel, finance, accounting, stock, order, and purchasing data is represented only through scoped references or operational projections unless ADR-011 explicitly assigns ownership to CrevaFactOS.

No native accounting, receivables/payables, invoicing, payroll, official stock ledger, or general purchasing ERP module is authorized by this ADR.

## Application Rules

- Tenant and facility context is mandatory for tenant-scoped operations.
- Permission checks occur in the application boundary, not only in the UI.
- Critical execution requires a valid authoritative AI Task Center approval.
- Audit and correlation identifiers flow through HTTP, messages, AI tools, and integrations.
- External writes use versioned allowlisted operations and idempotency keys.
- Secrets are supplied through environment-specific secure configuration.
- Domain modules cannot directly access another module’s tables.

## Alternatives

Node-only and mixed backend stacks were considered but rejected for the primary transactional boundary because they would increase operational and consistency costs. This does not prevent TypeScript in the Next.js application.

## Consequences

- The repository must pin a supported .NET SDK.
- Shared backend conventions belong in controlled building blocks, not a global service locator.
- Targeted architecture, authorization, tenant-isolation, and integration tests are required.
- Product scope remains governed by ADR-011.
