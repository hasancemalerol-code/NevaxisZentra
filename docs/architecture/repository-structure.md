# CrevaFactOS Repository Structure

## Purpose

This document defines the intended monorepo layout and dependency boundaries. The repository currently contains documentation and root toolchain settings; application folders are created incrementally in reviewable implementation packages.

## Intended Layout

```text
crevafactos/
├── apps/
│   ├── web/              # Next.js operational Web application
│   ├── api/              # ASP.NET Core modular-monolith API
│   ├── worker/           # Outbox, messaging, and background work
│   ├── ai-orchestrator/  # Controlled AI orchestration boundary
│   ├── mobile/           # Flutter field application
│   ├── teams/            # Internal messaging, routing, task interaction, and shift communication
│   └── edge-agent/       # .NET factory-local integration agent
├── packages/
│   ├── design-tokens/
│   ├── web-ui/
│   ├── api-contracts/
│   ├── shared-schemas/
│   └── localization/
├── infrastructure/
│   ├── docker/
│   ├── database/
│   ├── monitoring/
│   └── deployment/
├── docs/
│   ├── adr/
│   ├── architecture/
│   ├── product/
│   └── security/
├── scripts/
├── .github/workflows/
├── CrevaFactOS.sln
├── Directory.Build.props
├── global.json
├── package.json
└── pnpm-workspace.yaml
```

## Application Boundaries

- `web` renders the locked dashboard and operational workflows; it does not own approvals or domain rules.
- `api` is the trusted transactional, authorization, and modular-domain boundary.
- `worker` performs durable asynchronous work and cannot bypass application permissions or approval rules.
- `ai-orchestrator` uses versioned tools and never accesses databases, queues, ERP, or machines directly.
- `mobile` is a task-focused field client using the same authoritative task and approval records.
- `teams` is the CrevaFactOS Teams application. Its deployment packaging may be decided separately, but its logical application boundary is locked and it uses AI Task Center as the task and approval authority.
- `edge-agent` handles allowlisted factory-local connectivity while local safety remains authoritative.

CrevaFactOS Teams does not maintain parallel task or approval state.

## Shared Package Rules

- Share only code used by more than one application.
- Do not place domain entities or business rules in frontend packages.
- Contracts and schemas are versioned where compatibility matters.
- Packages do not weaken tenant, authorization, audit, or tool boundaries.
- Circular dependencies are prohibited.

## Product Boundary

The layout must not contain native classic ERP applications or packages for finance, accounting, receivables/payables, official inventory, invoicing, payroll, general purchasing, CRM, HR, or WMS. ERP-specific code belongs behind connector contracts and follows ADR-011.

## Incremental Creation

Folders shown above are targets, not evidence that implementation exists. Each application, package, or infrastructure directory is created only by an approved package with targeted validation.
