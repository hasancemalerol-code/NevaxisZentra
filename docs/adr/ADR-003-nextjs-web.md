# ADR-003: Next.js Web Platform

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Web application technology and frontend boundary

## Context

CrevaFactOS needs a desktop-first operational interface for production planning, shift planning, AI tasks and approvals, machines, molds, maintenance, quality, material risk, and delivery risk.

The Web application is not a classic ERP interface and must preserve the locked dashboard structure.

## Decision

The Web application uses Next.js, React, and TypeScript in `apps/web` with:

- Next.js App Router
- TypeScript strict mode
- React Server Components by default
- Client Components only where interaction requires them
- A centralized, typed API client
- Shared design tokens and UI components
- Schema-based validation
- Permission-aware navigation and actions
- Tenant- and facility-scoped data access
- Desktop-first responsive behavior

## Authoritative Workflow Boundary

Web presents AI Task Center tasks and approvals from the same authoritative records used by CrevaFactOS Teams. It must not maintain browser-only or Web-only approval state.

Client-side hiding is not authorization. The API revalidates tenant, permission, approval scope, delegation, expiry, and current state for every protected action.

## Locked Dashboard

The information architecture and visual structure in `docs/product/dashboard-lock.md` must not be redesigned without an explicit product decision.

The primary sidebar and dashboard must not introduce native modules for:

- Finance or accounting
- Receivables/payables
- Official stock ledger
- Payroll or personnel master data
- General purchasing ERP
- General CRM, WMS, or HR

Operational risk indicators based on externally owned data may appear only in the locked locations and must be labeled as synchronized or derived information.

## Consequences

- UI packages contain presentation behavior, not domain rules.
- Approval and write-back controls cannot be bypassed through Server Actions.
- Accessibility, localization, loading, empty, error, stale-data, and permission-denied states are required.
- The dashboard lock is a product acceptance criterion for relevant changes.
