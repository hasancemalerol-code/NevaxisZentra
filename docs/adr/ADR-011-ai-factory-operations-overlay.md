# ADR-011: AI Factory Operations Overlay

- Status: Accepted and locked
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Product boundary, data ownership, task authority, integrations, and execution safety
- Supersedes: Any full-ERP interpretation in ADR-001 through ADR-010

## Context

CrevaFactOS must improve production decisions without replacing the systems that own accounting, finance, official inventory, invoicing, payroll, purchasing records, and enterprise master data. Earlier documents used broad “Factory Operating System” wording that could be read as authorizing native classic ERP modules.

The product also needs one authoritative task and approval model shared by the Web application and CrevaFactOS Teams, with controlled actions against ERP and factory systems.

## Decision

CrevaFactOS is an **AI Factory Operations Assistant and AI Production Manager overlay** above existing ERP, HR, communications, machine, and factory systems. It is not a replacement ERP.

Existing source systems remain authoritative for classic ERP records. CrevaFactOS owns its native operational plans, factory intelligence, tasks, approvals, audit evidence, and integration state.

The native product scope includes:

- Omnichannel order intake and AI order drafts
- Production and shift planning
- Machine intelligence and telemetry analysis
- Mold lifecycle and mold maintenance optimization
- Maintenance, breakdown, quality, and scrap analysis
- Material, supplier, delivery, and bottleneck risk
- AI Task Center and Human-in-the-Loop workflows
- Audit and AI Decision Ledger
- Edge Agent and controlled factory integration

Classic accounting, finance, receivables/payables, official inventory ledger, invoicing, payroll, purchasing system-of-record, and general-purpose CRM/HR/WMS capabilities are not native CrevaFactOS modules.

## Authoritative Data Ownership

| Data area | Authoritative system | CrevaFactOS responsibility |
| --- | --- | --- |
| Finance and accounting | ERP/accounting system | Read operational signals and calculate non-official risk or impact analysis |
| Receivables/payables | ERP/accounting system | Read approved risk indicators; no official ledger ownership |
| Official stock ledger | ERP/WMS | Read stock projections and produce material-risk analysis |
| Customers | ERP/CRM where applicable | Store external references, mappings, and order-intake context |
| Suppliers | ERP/purchasing system | Store external references, mappings, and supplier-risk analysis |
| Personnel master records | HR/ERP | Store external references plus operational skills and availability needed for shift planning |
| Official orders | ERP/order system where applicable | Own omnichannel intake and AI drafts; synchronize approved order references |
| Production plans | CrevaFactOS | Authoritative native planning records and approved plan versions |
| Shift plans | CrevaFactOS | Authoritative native shift scenarios, assignments, and approved versions |
| AI tasks | CrevaFactOS AI Task Center | Authoritative task state, assignment, evidence, and execution state |
| Approvals | CrevaFactOS AI Task Center | Authoritative approval request, decision, scope, expiry, and delegation records |
| Machine telemetry | Source machine/Edge Agent for raw observations; CrevaFactOS for normalized operational telemetry | Normalize, retain, analyze, and correlate permitted telemetry |
| Mold lifecycle | CrevaFactOS | Authoritative operational mold history, counters, maintenance, and production relationships |
| Audit and AI decisions | CrevaFactOS | Immutable audit trail and AI Decision Ledger |
| Integration sync state | CrevaFactOS | Authoritative connector checkpoints, mappings, idempotency keys, and delivery outcomes |

Copies of externally owned data are operational projections, not replacement master records. Every projection must retain source-system identity, external key, synchronization time, and freshness state.

## AI Task Center and CrevaFactOS Teams

AI Task Center is the authoritative system for factory-operation tasks and approvals. The Web application and CrevaFactOS Teams must read and act on the same task and approval identifiers and state transitions.

CrevaFactOS Teams is the internal messaging, routing, task-interaction, approval-viewing, and shift-communication application. It must not create a parallel task or approval database. Messages may route users to work, but the underlying assignment, approval, rejection, delegation, expiry, and execution state belongs to AI Task Center.

Routing may target purchasing, warehouse/material, order operations, finance/accounting, production, shift, maintenance, quality, and management units. Routing to a unit does not transfer data ownership or grant execution permission.

## Human-in-the-Loop Execution

Critical actions use this sequence:

```text
Observe source data
  -> validate tenant, facility, identity, and permissions
  -> produce analysis, evidence, confidence, risk, and proposed scope
  -> create authoritative task and approval request
  -> route and present through Web and/or Teams
  -> authorized human approves, rejects, corrects, delegates, or lets it expire
  -> revalidate permissions, approval scope, source freshness, and current state
  -> execute once through an allowlisted tool
  -> verify the outcome
  -> record audit and AI Decision Ledger entries
```

The requester, recommender, approver, and executor are distinct actors where risk requires separation of duties. Delegation must be explicit, scoped, time-bound, and audited. Approval expires at its recorded deadline or when material input data, target state, permission, or requested scope changes.

## ERP Integration Policy

ERP access is read-only-first. Write-back is allowed only for a documented connector capability and must be:

- Permission-bound and tenant/facility scoped
- Bound to an active approval and its exact operation scope
- Revalidated immediately before execution
- Idempotent through a stable operation key
- Audited with requester, approver, executor, payload digest, source state, and outcome
- Safe to retry without creating duplicate business records

CrevaFactOS must never write official accounting or finance entries as an autonomous AI action.

## Edge and Machine Safety

Machine integrations run through Edge Agent. Safety PLCs, interlocks, emergency stops, and hard real-time controls remain local. No user, cloud service, or AI agent receives unrestricted PLC, register, node, script, or machine-write access.

Approved machine commands require allowlisting, permission, approval where critical, current-state validation, idempotency where applicable, local safety acceptance, and audit.

## Dashboard Constraint

The locked dashboard structure remains unchanged. Finance, accounting, receivables/payables, native stock ledger, payroll, and general purchasing ERP modules must not be introduced into the primary dashboard or sidebar. Risk indicators derived from ERP data may appear only in their locked operational context.

## Consequences

- Domain names and APIs must distinguish native records from external projections.
- Any earlier ADR module list is interpreted through this ownership decision.
- New classic ERP functionality requires an explicit replacement ADR and product approval; it cannot enter silently.
- Integrations must follow `docs/product/integration-priorities.md`.
