# Production Readiness Checklist — Enterprise Reliability (UM-9)

Use this checklist before promoting UM-9 enterprise reliability to production.

## Schema and migration

- [ ] `npm run verify:enterprise-reliability-um9-migration` passes
- [ ] All UM-9 tables deployed with RLS enabled
- [ ] Operability permissions seeded (`operability.read` through `operability.backup.verify`)
- [ ] Pilot edge agent registration present in non-prod only

## Health and observability

- [ ] `/health/live` returns 200 without database dependency
- [ ] `/health/ready` returns 200 when persistence health checks are enabled
- [ ] `GET /api/system-status/version` exposes build SHA
- [ ] Metrics use `MetricCardinalityGuard` label allowlist
- [ ] Diagnostic bundle excludes secrets

## Reliability engines

- [ ] Circuit breaker threshold = 5 failures
- [ ] Retry policy max attempts = 5
- [ ] Degraded read-only mode blocks unsafe mutations when DB unavailable
- [ ] Degraded buffering mode active when edge agents offline
- [ ] Reconciliation conflicts return 409, never silent overwrite

## Edge and integration

- [ ] Edge agent heartbeats update `edge_agent_registrations`
- [ ] Edge buffer enforces idempotency keys
- [ ] Critical events cannot be silently dropped
- [ ] Connector outcomes update circuit breaker state

## Security

- [ ] `npm run verify:enterprise-reliability-um9-security` passes
- [ ] Unauthenticated access to `/api/system-status/*` denied (except version)
- [ ] Cross-tenant workspace access denied
- [ ] Replay idempotency returns same audit row for duplicate keys

## Data integrity

- [ ] `npm run verify:enterprise-reliability-um9-data-integrity` passes
- [ ] Correlation ID captured on reconcile and replay
- [ ] Backup verification runs recorded with subject ID

## Failure injection and performance

- [ ] `npm run verify:enterprise-reliability-um9-failure-injection` passes
- [ ] `npm run verify:enterprise-reliability-um9-performance` passes
- [ ] Bounded load profile documented and within SLO

## Backup and restore

- [ ] `scripts/db/backup.ps1` produces valid pg_dump artifact
- [ ] `npm run verify:database-restore-um9` passes
- [ ] RPO/RTO documented in [rpo-rto.md](./rpo-rto.md)

## Runbooks

- [ ] `docs/runbooks/api-down.md` reviewed
- [ ] `docs/runbooks/db-down.md` reviewed
- [ ] `docs/runbooks/connector-down.md` reviewed
- [ ] `docs/runbooks/edge-agent-offline.md` reviewed
- [ ] `docs/runbooks/migration-failure.md` reviewed
- [ ] `docs/runbooks/backup-failure.md` reviewed
- [ ] `docs/runbooks/ai-provider-outage.md` reviewed
- [ ] `docs/runbooks/deployment-rollback.md` reviewed

## Golden flows (runtime)

- [ ] `npm run verify:enterprise-reliability-um9-runtime` passes
- [ ] All 10 golden flows verified (live or static proof):
  - API_RESTART_GOLDEN_FLOW
  - DB_OUTAGE_GOLDEN_FLOW
  - EDGE_OFFLINE_GOLDEN_FLOW
  - CONNECTOR_FAILURE_GOLDEN_FLOW
  - IDEMPOTENCY_GOLDEN_FLOW
  - FAILED_DEPLOYMENT_GOLDEN_FLOW
  - BACKUP_RESTORE_GOLDEN_FLOW
  - AI_OUTAGE_GOLDEN_FLOW
  - BACKLOG_RECOVERY_GOLDEN_FLOW
  - FULL_RECOVERY_GOLDEN_FLOW
