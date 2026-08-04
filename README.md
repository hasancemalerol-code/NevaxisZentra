# CrevaFactOS

CrevaFactOS is an AI Factory Operations Assistant and AI Production Manager layer for plastic injection molding and mold manufacturing operations.

It works above existing ERP, HR, communication, machine, and factory systems. It does not replace the systems of record for accounting, finance, receivables/payables, official inventory, invoicing, payroll, purchasing records, or enterprise master data.

## Native Product Scope

- Omnichannel order intake and AI order drafts
- Production planning and plan reliability
- Shift planning, skills, and operational assignment
- Machine intelligence and performance analysis
- Mold lifecycle and maintenance optimization
- Maintenance and breakdown analysis
- Quality and scrap analysis
- Material, supplier, delivery, and bottleneck risk
- AI Task Center and Human-in-the-Loop approvals
- CrevaFactOS Teams for internal routing and shift communication
- Audit and AI Decision Ledger
- ERP connectors and controlled write-back
- Edge Agent, telemetry, and allowlisted machine integration

AI Task Center is authoritative for tasks and approvals. Web and CrevaFactOS Teams use the same task and approval records.

## Operating Model

```text
Observe data
  -> analyze freshness, risk, and impact
  -> prepare a plan, task, or bounded action
  -> request authorized human approval when critical
  -> revalidate current state
  -> execute through an allowlisted integration
  -> verify the outcome
  -> record audit and AI Decision Ledger evidence
```

ERP access is read-only-first. Write-back is permission-bound, approval-bound, revalidated, idempotent, and audited. Machine safety and hard-real-time controls remain local.

## Technical Direction

- Monorepo
- Next.js and TypeScript Web application
- ASP.NET Core modular-monolith backend
- PostgreSQL
- RabbitMQ and transactional outbox
- .NET Edge Agent
- Multi-tenant isolation and permission-based authorization
- Audit and AI Decision Ledger

## Documentation Authority

- [Product positioning](docs/product/product-positioning.md)
- [Core capabilities](docs/product/core-capabilities.md)
- [Out of scope](docs/product/out-of-scope.md)
- [Dashboard lock](docs/product/dashboard-lock.md)
- [Integration priorities](docs/product/integration-priorities.md)
- [ADR-011 product overlay](docs/adr/ADR-011-ai-factory-operations-overlay.md)

When an earlier architectural document can be read as authorizing a classic ERP module, ADR-011 and the locked product documents take precedence.

## Repository Status

The repository contains the FOUND-01 executable skeleton, FOUND-02 local infrastructure, FOUND-03 PostgreSQL persistence baseline, and FOUND-04 tenant-context/RLS foundation. Persistence remains disabled by default and contains no product entities or business schema. Authentication, permissions, messaging behavior, audit, AI agents, integrations, and product capabilities are intentionally not implemented yet.

## Development Prerequisites

- Node.js `22.18.x` as pinned by `.nvmrc`
- pnpm `11.20.0`
- .NET SDK `10.0.100` or a compatible `10.0` feature band allowed by `global.json`

## Install

```text
pnpm install
dotnet restore CrevaFactOS.sln
dotnet tool restore
```

## Build and Validate

```text
pnpm build
pnpm lint
pnpm typecheck
pnpm test

dotnet build CrevaFactOS.sln
dotnet test CrevaFactOS.sln
```

The root `pnpm build` and `pnpm test` commands include their corresponding .NET solution commands. The explicit .NET commands are useful for targeted validation.

A repository-wide format command is intentionally not included until a formatter and format policy are selected in a dedicated package. Existing `.editorconfig` and compiler/linter checks remain authoritative.

## Run Applications

```text
pnpm dev:web
pnpm dev:api
pnpm dev:worker
pnpm dev:edge-agent
```

The Web application defaults to `http://localhost:3000` and exposes `GET /health`. The API launch profile uses `http://localhost:5000` and exposes `GET /health/live` and `GET /health/ready`.

Worker and Edge Agent only validate configuration, start their generic hosts, and emit structured startup logs. They contain no product jobs or machine connectivity.

## Local Infrastructure

PostgreSQL and RabbitMQ local development dependencies are documented in [infrastructure/README.md](infrastructure/README.md).

```text
pnpm infra:start
pnpm infra:verify
pnpm infra:stop
```

No database schema, queue, connector, or product integration is created by the local infrastructure baseline.

## Persistence Baseline

FOUND-03 adds an empty EF Core context and a separate migration assembly. It never runs migrations automatically. See [infrastructure/README.md](infrastructure/README.md) for configuration, connectivity verification, migration lifecycle commands, rollback behavior, and secret-handling rules.

The initial migration creates no business schema. When explicitly applied, only EF Core's `public.__EFMigrationsHistory` metadata table exists.

FOUND-04 adds only the tenant registry, an infrastructure isolation probe, scoped tenant context, and PostgreSQL RLS enforcement. See the infrastructure guide for the temporary validated header contract and `pnpm db:tenant-isolation:verify`.
