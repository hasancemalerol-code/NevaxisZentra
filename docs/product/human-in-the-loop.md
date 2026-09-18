# NevaxisZentra Human-in-the-Loop and Unit Routing Model

- Status: Locked
- Version: 2.0
- Date: 2026-08-04
- Communication application: NevaxisZentra Teams
- Authoritative workflow: AI Task Center

## Purpose

This document defines how analysis becomes a controlled factory-operation action. NevaxisZentra is not a fully autonomous factory-management system, and AI cannot approve its own work.

## Authority

AI Task Center owns task, assignment, evidence, review, approval, delegation, expiry, execution, and result state. Web and NevaxisZentra Teams reference the same task and approval identifiers and transitions.

NevaxisZentra Teams provides internal messaging, routing, task interaction, approval viewing, and shift communication. A Teams message, emoji, reaction, or read receipt is not itself an approval unless it invokes a validated AI Task Center decision action.

## Standard Flow

```text
ERP + machines + order channels + documents
  -> Integration Hub and Edge Agent
  -> Operational Data Hub
  -> authorized AI agent
  -> analysis + evidence + freshness + confidence + risk + bounded proposal
  -> authoritative AI Task Center task and approval request
  -> Web and/or NevaxisZentra Teams routing
  -> authorized human review
  -> approve / reject / correct / delegate / expire
  -> permission, approval, freshness, and current-state revalidation
  -> NevaxisZentra action / approved ERP write-back / approved Edge command
  -> outcome verification and reconciliation
  -> audit + AI Decision Ledger
```

## Unit Routing

Work may route to purchasing, warehouse/material, order operations, finance/accounting, production, shift operations, maintenance, quality, and management. Each route records intended unit, required permission, assignee or candidate group, due time, urgency, and escalation policy.

Routing does not transfer authoritative data ownership and does not grant approval or execution permission.

## Task Lifecycle

Suggested states are:

```text
Draft -> Ready -> Assigned -> In Review -> Waiting for Approval
      -> Approved / Rejected / Correction Requested / Expired / Cancelled
      -> Executing -> Succeeded / Partially Succeeded / Failed / Reconciliation Required
```

Allowed transitions are permission- and state-checked. Every transition records actor, time, reason, correlation, and previous/new state.

## Approval Record

An approval records:

- Task and proposed-operation identifiers
- Tenant, company, facility, target records, and operation type
- Exact parameter scope and limits
- Evidence and source-data version/freshness
- Requester, recommender, approver, and intended executor
- Required permission and separation-of-duties policy
- Decision, reason, created time, expiry, and revocation
- Whether it is single-use or allows enumerated operations

## Separation of Duties and Delegation

Risk policy may require distinct requester, recommender, approver, and executor identities. AI never satisfies a human-approval requirement.

Delegation is explicit, scoped, time-bound, revocable, and cannot exceed the delegator’s permission. Delegation and its use are audited. Expired or revoked delegation invalidates pending decisions that depend on it.

## Expiry and Revalidation

Approval expires at its deadline and is invalidated by a material change in input data, target state, permission, delegation, connector mapping, operation parameters, or risk classification.

Immediately before execution the system performs stale-data revalidation: it reloads authoritative state and verifies tenant/facility, permission, approval scope, expiry, source freshness, target version, operation limits, and idempotency state. Failed revalidation returns the task for review; it does not silently execute under old evidence.

## Idempotent Execution

Every approved external operation uses a stable idempotency key tied to the task, approved operation, target, and version. Retries reuse that key. Ambiguous timeouts require reconciliation before another business operation is attempted.

## Audit and AI Decision Ledger

The record includes source evidence, freshness, agent/model/tool versions, user-facing rationale, confidence, risk, proposed scope, routing, task transitions, delegation, approval decision, revalidation, idempotency key, execution request/result, verification, and correlation identifiers.

Audit records cannot be modified by AI, the approver, or the executor.

## Machine and ERP Boundaries

ERP access is read-only-first. Write-back must be permission-bound, approval-bound, revalidated, idempotent, and audited.

Machine commands use Edge Agent and a versioned allowlist. Local safety controls remain authoritative and may reject any command. No Human-in-the-Loop approval can authorize bypassing local safety.
