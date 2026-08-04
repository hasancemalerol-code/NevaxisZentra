# CrevaFactOS Security Documentation

CrevaFactOS is a multi-tenant AI Factory Operations Assistant operating above ERP and factory systems. It is not a replacement ERP and does not gain authority merely by synchronizing external data.

## Security Boundaries

- Tenant, company, and facility isolation
- Permission-based authorization and scoped memberships
- AI Task Center task and approval authority
- Separation of duties, delegation, and approval expiry
- Audit and AI Decision Ledger integrity
- Versioned AI tool access
- Read-only-first ERP connectors and controlled write-back
- RabbitMQ, outbox, and consumer idempotency
- Edge Agent identity and factory-local machine safety
- Secret, file, telemetry, and personal-data protection

## Authorization

Roles are permission bundles. Final access evaluates authenticated identity, active membership, tenant/facility, permission, resource scope, delegation, approval scope, operation limits, and current state.

Example permission families include:

```text
production_plans.read
production_plans.propose
production_plans.publish
shift_plans.read
shift_plans.propose
shift_plans.publish
ai_tasks.read
ai_tasks.assign
ai_tasks.approve
maintenance.execute
quality.containment.approve
integrations.configure
integrations.write_back.execute
edge_commands.approve
audit.read
```

Permission names describe bounded operations. Permissions for native finance, accounting, official stock, payroll, or general purchasing ledgers are not defined because those are not CrevaFactOS-owned modules.

## Critical Action Controls

Critical actions require an authoritative approval containing exact scope, evidence version, limits, approver, expiry, and separation-of-duties result. Immediately before execution the backend revalidates permissions, delegation, approval, data freshness, target state, and idempotency.

ERP writes are allowlisted, approval-bound, safe to retry, reconciled, and audited. AI never writes directly to ERP or machines.

## Edge Safety

Edge Agent connects through least-privilege, per-installation identity. Telemetry is read-first. Approved machine commands remain bounded and may be rejected by local safety logic. Emergency stops, interlocks, safety PLCs, and hard-real-time controls cannot be overridden from CrevaFactOS.

## Data and Secrets

- Collect and retain only data necessary for factory-operation decisions.
- Keep tenant credentials and connector mappings isolated.
- Never commit production secrets or use production customer data in development.
- Redact secrets and unnecessary sensitive data from logs, messages, prompts, and errors.
- Encrypt transport and sensitive persisted data using managed keys where required.

## Verification

Security-sensitive packages include targeted negative tests for cross-tenant access, missing permission, invalid delegation, self-approval, expired approval, stale evidence, duplicate execution, connector ambiguity, AI tool misuse, and local machine-safety rejection.

Security exceptions require an explicit risk decision, owner, expiry, compensating controls, and audit trail.
