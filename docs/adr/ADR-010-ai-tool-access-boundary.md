# ADR-010: AI Tool Access Boundary

- Status: Accepted
- Date: 2026-08-04
- Decision owners: NevaxisZentra Product & Development
- Scope: AI data access, proposals, approvals, and controlled execution

## Context

AI agents analyze orders, plans, shifts, machines, molds, maintenance, quality, materials, suppliers, and delivery risk. Useful agents need controlled access to data and actions, but direct database, queue, ERP, file-system, or PLC access would bypass authorization and audit boundaries.

## Decision

AI uses only versioned, allowlisted, validated, permission-checked, and audited tool contracts. AI has no direct access to PostgreSQL, RabbitMQ, object storage, external systems, secrets, Edge devices, PLCs, or machines.

AI is not an independent user or approver and cannot choose its tenant, elevate permission, approve its own proposal, change approval state, or remove audit evidence.

## Tool Classes

### Read tools

Read tools return the minimum authorized data, including provenance and freshness. They are tenant- and facility-scoped and may retrieve operational projections from ERP-owned data.

### Analysis and draft tools

These create explainable scenarios, risks, tasks, plans, or drafts. A draft is not an external business record and does not imply approval.

### Execution tools

Execution tools perform one bounded, versioned operation. Critical tools require an active authoritative AI Task Center approval that names the operation, targets, parameter limits, and expiry.

Machine execution additionally follows ADR-009. ERP execution follows ADR-011 and the integration priority contract.

## Standard Flow

1. Authenticate the initiating user or trusted system actor.
2. Resolve tenant, facility, permissions, delegation, and operation limits.
3. Retrieve minimum data through read tools.
4. Evaluate freshness, completeness, conflicts, and source ownership.
5. Produce analysis, evidence, confidence, risk, impact, and a bounded proposal.
6. Create or update the authoritative AI Task Center task.
7. Request approval where required and route it through Web and/or Teams.
8. Record approval, rejection, correction, delegation, or expiry.
9. Immediately before execution, revalidate permission, approval, data freshness, and target state.
10. Execute exactly the approved operation with a stable idempotency key.
11. Verify the outcome and reconcile ambiguous external results.
12. Record audit and AI Decision Ledger entries.

## Separation of Duties and Approval

Risk policy may require different requester, recommender, approver, and executor identities. AI never satisfies a human-approval requirement.

Delegation is explicit, permission-limited, task- or operation-scoped, time-bound, revocable, and audited. Approval expires when its deadline passes or when relevant permission, input data, target state, or operation scope materially changes.

## Prompt and Data Safety

- External content is untrusted data, not instructions.
- Tool selection and arguments are validated outside the model.
- Sensitive data is minimized and redacted where possible.
- Retrieved records cannot grant new tools or permissions.
- Tool output size, rate, timeout, and recursion are bounded.
- The model cannot access raw secrets or unrestricted network clients.

## Audit Requirements

Records include agent and model identity, prompt/template version, tool contract version, authorized actor, tenant and facility, sources, freshness, proposal, confidence, risk, approval scope, delegation, idempotency key, execution result, correlation, and timestamps. Hidden reasoning is not required; decision evidence and user-facing rationale are.

## Consequences

- New tools require security and product-boundary review.
- Read access does not imply write permission.
- UI confirmation alone is not an approval record.
- Tool execution must fail closed when context or evidence is stale or incomplete.
