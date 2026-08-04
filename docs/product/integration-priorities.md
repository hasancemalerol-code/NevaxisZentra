# CrevaFactOS Integration Priorities

- Status: Locked
- Date: 2026-08-04
- Product: CrevaFactOS
- Category: AI Factory Operations Assistant

## Purpose

This document defines the order in which integration capabilities are introduced. The order favors low-risk ingestion and operational visibility before controlled external writes.

## Priority Order

1. Excel/CSV
2. Generic file import
3. REST API
4. Webhook
5. Email
6. File/folder watch
7. Read-only database connector
8. ERP-specific adapters
9. WhatsApp Business Platform
10. CrevaFactOS Teams routing
11. Edge Agent
12. Machine/PLC telemetry
13. Approved ERP write-back
14. Approved machine commands

Later priorities may use foundations built for earlier priorities, but a customer request must not silently bypass the safety and ownership controls associated with the sequence.

## Common Connector Contract

Every connector must define:

- Tenant, company, facility, and source-system scope
- Authentication and secret ownership
- Canonical schema and source-to-canonical mapping version
- Source record identity and synchronization checkpoint
- Data freshness, provenance, and quality state
- Retry, backoff, timeout, and dead-letter behavior
- Idempotency and duplicate detection rules
- Audit, correlation, and observability fields
- Permission requirements and supported read/write operations
- Data retention and sensitive-data controls

Integration sync state is owned by CrevaFactOS. Business records imported from an ERP remain projections when the ERP is their system of record.

## Read-Only-First Policy

New ERP and database connectors begin with read-only access. Initial validation covers mapping correctness, freshness, tenant isolation, reconciliation, rate limits, failure recovery, and audit visibility.

Read-only database connectors must use a dedicated least-privilege account, allowlisted views or queries, bounded extraction, and no schema mutation privileges. Direct access is not a shortcut around an available supported API.

## Write-Back Gate

ERP write-back is the thirteenth priority and is not enabled merely because read access exists. Each write operation requires:

- A versioned allowlisted operation contract
- Explicit user permission
- An active AI Task Center approval when the action is critical
- Separation of duties where required by risk
- Immediate stale-data and target-state revalidation
- A stable idempotency key and duplicate-result reconciliation
- An immutable audit and AI Decision Ledger record
- A verified success, partial-success, or failure outcome

Retries must not duplicate orders, purchasing drafts, or other external records. Financial postings are not autonomous CrevaFactOS write-back operations.

## Teams Routing

CrevaFactOS Teams routes messages, tasks, alerts, reviews, approvals, and shift communication to purchasing, warehouse/material, order operations, finance/accounting, production, shift, maintenance, quality, and management units.

Teams and Web reference the same authoritative AI Task Center task and approval records. Teams is not a second workflow store and cannot bypass assignment, permission, delegation, expiry, or approval checks.

## Edge and Machine Gate

Edge Agent owns controlled connectivity to the factory network. Telemetry is read-first. Approved machine commands are introduced only after telemetry identity, time synchronization, offline buffering, device allowlisting, and local safety behavior have been validated.

No connector can disable or override local safety logic. Approved commands require a versioned allowlist, permission, approval where critical, current-state validation, local acceptance, outcome verification, and audit.

## Change Control

Changing the priority order or weakening a gate requires an explicit product decision and, where architectural boundaries change, a new ADR.
