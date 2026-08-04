# Transactional Outbox Foundation

The transactional outbox records durable infrastructure event intent in the same PostgreSQL transaction as a state change. It does not publish messages. Audit remains immutable evidence of actions and outcomes; the AI Decision Ledger remains structured recommendation evidence; the outbox is only a delivery-intent envelope for later messaging infrastructure.

## Envelope and transaction contract

Every tenant message has an immutable UUID, exact infrastructure message type and schema version, tenant, correlation and optional causation identifiers, a case-sensitive idempotency key, bounded flat JSON payload and headers, occurrence and availability timestamps, and explicit processing state. The writer requires a caller-owned database connection and transaction. It never creates an independent transaction, calls `SaveChanges`, opens a distributed transaction, or contacts RabbitMQ. Rolling back the caller transaction rolls back both its neutral state change and the outbox append.

Only `infrastructure.audit.recorded`, `infrastructure.ai-decision.recorded`, and `infrastructure.outbox.probe` at schema version 1 are recognized. Product events remain deferred. Payload and header values are shallow bounded strings; credentials, authorization headers, cookies, tokens, connection strings, API keys, prompts, completions, reasoning, raw requests, and arbitrary object serialization are prohibited.

## Idempotency and immutability

Idempotency keys use exact, case-sensitive matching within one tenant. A retry returns the existing message identifier with `Duplicate=true`; it never silently inserts a second row. Different tenants may reuse the same key. Identity, tenant, type, schema, aggregate, correlation, causation, idempotency, payload, headers, and envelope timestamps cannot change after insert. The runtime role has no direct insert, update, delete, or truncate grant.

## Lease contract

Messages begin `Pending`. `claim_outbox_messages` deterministically orders eligible messages by availability, occurrence, and ID, limits batches to 1–100, and uses `FOR UPDATE SKIP LOCKED`. It may reclaim an expired `Processing` lease. Claiming assigns a non-empty lock ID and future expiry, increments attempts, and records the attempt time. Only the matching lock owner can mark a message `Processed` or `Failed`. Processed and failed messages cannot be reclaimed; there is no transition back to Pending in this package.

Forced RLS limits diagnostics and processing to the current tenant. Ordinary tenant writers cannot create tenant-neutral messages; that future system boundary is deferred. Development-only `POST /internal/outbox` accepts a typed probe and `GET /internal/outbox/recent` returns at most 25 safe summaries under exact `system.outbox.write` and `system.outbox.read` permissions.

RabbitMQ publishing, consumers, dispatch loops, retries, dead-letter queues, inbox processing, product workflows, connectors, ERP projections, dashboard and Teams behavior are explicitly deferred.
