# NevaxisZentra Product Documentation

This directory contains the locked product boundary for NevaxisZentra, an AI Factory Operations Assistant and AI Production Manager layer for plastic injection molding and mold manufacturing.

NevaxisZentra is not a replacement ERP. Existing ERP and related systems remain authoritative for classic enterprise records; NevaxisZentra converts permitted data into operational plans, risks, tasks, approvals, and audited actions.

## Authoritative Documents

- [Product positioning](product-positioning.md)
- [Core capabilities](core-capabilities.md)
- [Out-of-scope areas](out-of-scope.md)
- [Locked dashboard](dashboard-lock.md)
- [User roles and authorization](user-roles.md)
- [AI agent catalog](ai-agent-catalog.md)
- [Human-in-the-Loop model](human-in-the-loop.md)
- [Integration priorities](integration-priorities.md)

ADR-011 defines the corresponding architecture-level product boundary and data-ownership matrix.

## Locked Principles

- Production planning and shift planning are native NevaxisZentra capabilities.
- Machine intelligence, mold lifecycle, maintenance, quality, and operational risk remain native.
- AI Task Center is authoritative for tasks and approvals.
- NevaxisZentra Teams is the internal communication and routing application; it uses the same authoritative records as Web.
- Critical actions require Human-in-the-Loop approval.
- ERP access is read-only-first; write-back is permission-bound, approval-bound, revalidated, idempotent, and audited.
- Machine safety remains local and unrestricted machine writes are prohibited.
- The locked dashboard cannot be structurally redesigned without an explicit product decision.
- Classic ERP modules cannot be introduced silently.
