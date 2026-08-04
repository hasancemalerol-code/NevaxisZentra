# ADR-001: Modular Monolith Architecture

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Backend application architecture
- Product-boundary interpretation: ADR-011

## Context

CrevaFactOS combines tightly related production-planning, shift, machine, mold, maintenance, quality, risk, task, approval, audit, and integration workflows. Beginning with distributed microservices would add deployment, observability, consistency, and debugging cost before domain boundaries are proven.

A layerless monolith would make tenant, permission, audit, and module boundaries difficult to protect.

## Decision

The primary ASP.NET Core backend is a modular monolith. Worker, AI Orchestrator, and Edge Agent are separate executable applications with explicit contracts to the backend.

Each backend module separates Domain, Application, Infrastructure, and API concerns. Modules do not directly access another module’s internal tables or application services.

Cross-module communication uses:

- Explicit application contracts for necessary synchronous interactions
- Domain and versioned integration events
- Transactional outbox
- RabbitMQ

## Initial Foundation Modules

- Platform
- Organizations and tenant context
- Identity and permissions
- Audit and AI Decision Ledger
- Tasking and approvals
- Integrations

## Native Operational Module Candidates

- Order Intake
- Production Planning
- Shift Planning
- Machines
- Molds
- Maintenance
- Quality
- Material and Supplier Risk
- Delivery and Bottleneck Risk
- AI Operations
- CrevaFactOS Teams

Names describe bounded operational capabilities, not classic ERP ownership. External customer, supplier, personnel, stock, finance, accounting, official order, and purchasing records are accessed through integration references and projections according to ADR-011.

No standalone Finance, Accounting, Receivables/Payables, Official Inventory, Payroll, General Purchasing ERP, CRM, HR, or WMS module is authorized.

## Dependency Rules

```text
API -> Application -> Domain
Infrastructure -> Application and Domain contracts
```

- Domain code does not depend on web, database, broker, AI provider, or ERP SDKs.
- Infrastructure implements contracts defined inward.
- Module schemas and repositories are private to the module.
- Shared building blocks remain small and contain no module-specific rules.
- New cross-module dependencies require architecture review.

## Alternatives

### Distributed microservices

Rejected initially because operational cost and distributed consistency risk outweigh demonstrated scaling needs.

### Layerless monolith

Rejected because it would erode domain, tenant, and authorization boundaries.

### Separate database per initial module

Rejected initially because it would slow delivery and complicate transactions and reporting before service boundaries are validated.

## Consequences and Guardrails

- A shared PostgreSQL instance does not authorize cross-module table access.
- Architecture tests should verify dependency direction and forbidden references.
- Integration schemas are versioned.
- Modules may be extracted only for demonstrated scaling, availability, security, deployment, or ownership needs and require a new ADR.
- Product scope cannot be expanded through a module name; ADR-011 remains authoritative.
