# ADR-006: Transactional Outbox

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Consistency between PostgreSQL transactions and message publication

## Context

CrevaFactOS cannot atomically commit PostgreSQL data and RabbitMQ publication with a distributed transaction. Publishing before or after a database commit without durable coordination can lose events or announce state that never committed.

## Decision

Domain changes and their outgoing messages are persisted in the same PostgreSQL transaction. A Worker publishes pending outbox records to RabbitMQ and records the publication result.

Application and domain services must not publish RabbitMQ messages inside the critical database transaction.

## Standard Flow

1. Validate tenant, permissions, approval requirements, and current state.
2. Apply the domain change.
3. Create versioned integration events.
4. Persist domain changes and outbox records in one transaction.
5. Commit.
6. Claim eligible outbox records using concurrency-safe batching.
7. Publish each message with its stable `message_id`.
8. Mark successful publication and retain operational evidence.
9. Schedule bounded retry after transient failure.
10. Surface exhausted records for operational intervention.

## Outbox Record

```text
id
tenant_id
message_id
message_type
schema_version
occurred_at
correlation_id
causation_id
payload
headers
created_at
published_at
attempt_count
next_attempt_at
last_error
status
```

`message_id` is unique. Claims must prevent two publisher instances from treating the same record as independently completed.

## Idempotency

Outbox publication is at least once. Consumers deduplicate by message or operation identity and store their processing outcome. Approved ERP write-back uses a separate stable business-operation idempotency key so retries cannot duplicate external records.

An approval identifier alone is not a sufficient idempotency key when an approval can authorize more than one explicitly enumerated operation.

## Operations and Audit

- Payloads are versioned and protected from accidental mutation after commit.
- Errors are sanitized before persistence and logging.
- Metrics cover backlog age, attempt count, success rate, and exhausted records.
- Correlation links outbox publication to authoritative AI tasks, approvals, audit, and AI decisions.
- Manual replay is permission-bound and audited.

## Consequences

- Broker failure delays publication without losing committed intent.
- Consumers still require idempotency because acknowledgement can fail after successful handling.
- Write-back and critical actions still require immediate state and approval revalidation before execution.
