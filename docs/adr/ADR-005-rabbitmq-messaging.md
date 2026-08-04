# ADR-005: RabbitMQ Messaging

- Status: Accepted
- Date: 2026-08-04
- Decision owners: CrevaFactOS Product & Development
- Scope: Asynchronous integration and background messaging

## Context

CrevaFactOS requires asynchronous processing for operational events, AI tasks, notifications, connector synchronization, and Edge telemetry. Directly performing all work inside HTTP requests would couple failures and extend transaction duration.

## Decision

RabbitMQ is the primary message broker for integration events and background work. Database state and outgoing events are coordinated through the transactional outbox defined by ADR-006.

RabbitMQ is used for:

- Versioned integration events
- Outbox publication
- Worker jobs and long-running operations
- Connector and notification work
- AI Task Center workflow notifications
- CrevaFactOS Teams routing notifications
- Edge and cloud event transfer where appropriate
- Retry and dead-letter handling

RabbitMQ is not a system of record and does not replace domain transactions or authoritative task and approval state.

## Message Envelope

Every message uses a versioned envelope:

```text
messageId
messageType
schemaVersion
occurredAt
tenantId
companyId
facilityId
correlationId
causationId
actorId
payload
```

Optional scope identifiers are present when relevant. Sensitive values and secrets must not be placed in message headers or logs.

## Delivery Semantics

- Delivery is treated as at least once.
- Consumers must be idempotent.
- Consumer outcomes are recorded using inbox or equivalent deduplication state.
- Retry uses bounded exponential backoff with jitter.
- Poison messages move to a dead-letter path with operational visibility.
- Ordering is assumed only where a specifically designed partition or sequence contract provides it.
- Handlers revalidate current state instead of trusting stale message payloads.

## Security and Ownership

Tenant and scope fields are validated against the referenced record; a message header alone never grants access. RabbitMQ permissions, virtual hosts, credentials, and network access follow least privilege.

Events about ERP-owned records represent synchronization observations or requested approved operations. They do not change system-of-record ownership.

## Consequences

- Schemas require compatibility and versioning rules.
- Correlation must connect the originating request, task, approval, outbox record, consumer, and audit record.
- Broker availability must not make a committed domain transaction disappear; publication resumes from the outbox.
