# RUNTIME_ARCHITECTURE_INVENTORY

Enterprise reliability (UM-9) runtime inventory for NevaxisZentra.

## API layer

| Component | Location | Responsibility |
|-----------|----------|----------------|
| Health probes | `/health/live`, `/health/ready` | Liveness without DB; readiness for traffic |
| System status API | `apps/api/EnterpriseReliability/EnterpriseReliabilityEndpointRouteBuilderExtensions.cs` | Workspace, metrics, diagnostics, recovery actions |
| Version endpoint | `GET /api/system-status/version` | Anonymous build metadata for rollback |

### Endpoints

- `GET /api/system-status/workspace` — operating mode, components, integrations, edge agents, alerts
- `GET /api/system-status/metrics` — bounded operability counters
- `GET /api/system-status/diagnostics` — safe diagnostic bundle
- `POST /api/system-status/connectors/outcome` — connector success/failure recording
- `POST /api/system-status/edge/heartbeat` — edge agent registration and buffer state
- `POST /api/system-status/reconcile` — backlog reconciliation runs
- `POST /api/system-status/quarantine/replay` — idempotent quarantine replay
- `POST /api/system-status/backup/verify` — backup verification audit trail

## Persistence layer

| Table | Purpose |
|-------|---------|
| `enterprise_reliability_policies` | Tenant operating mode policy |
| `system_component_health_snapshots` | Component health history |
| `integration_circuit_breaker_states` | Connector circuit breaker state |
| `recovery_reconciliation_runs` | Reconciliation audit |
| `background_job_leases` | Bounded job ownership |
| `technical_operational_alerts` | Open technical alerts |
| `edge_agent_registrations` | Edge agent heartbeat registry |
| `backup_verification_runs` | Backup verification audit |
| `integration_quarantine_replay_audit` | Replay idempotency audit |

All UM-9 tables use tenant RLS with `FORCE ROW LEVEL SECURITY`.

## Engines

`packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs`

| Engine | Function |
|--------|----------|
| `IntegrationCircuitBreakerEngine` | Opens after 5 failures, 5-minute cooldown |
| `BoundedRetryPolicyEngine` | Max 5 attempts, category-aware delays |
| `DegradedModeEngine` | Resolves `fully-operational`, `degraded-read-only`, `degraded-buffering`, `recovering` |
| `RecoveryReconciliationEngine` | Conflict-safe reconciliation summaries |
| `EdgeEventClassificationEngine` | Critical vs disposable event policy |
| `MetricCardinalityGuard` | Bounded metric label keys |

## Edge buffer package

`packages/NevaxisZentra.EdgeBuffer`

- `EdgeBufferStore` — local JSON-backed event buffer with idempotency and capacity guards
- `EdgeDeliveryWorker` — ordered batch delivery with retry policy
- `EdgeReliabilityState` — API availability tracking
- `EdgeReliabilityScenarios` — deterministic offline/poison/partial-failure scenarios

## Edge agent

`apps/edge-agent/Program.cs`

- Registers `EdgeBufferStore`, heartbeat reporter, health reporter
- Posts heartbeats to `/api/system-status/edge/heartbeat`
- Uses dev auth headers (`X-crevafactos-dev-Issuer`, `X-NevaxisZentra-Tenant`)

## Pilot identifiers

| Resource | ID |
|----------|-----|
| Pilot tenant | `bb000001-0001-4000-8000-000000000001` |
| Pilot facility | `bb000001-0005-4000-8000-000000000005` |
| Pilot edge agent | `bb000001-0009-4000-8000-000000000901` |

## Verification scripts

| Script | Scope |
|--------|-------|
| `verify-enterprise-reliability-um9-migration.mjs` | Schema, permissions, RLS |
| `verify-enterprise-reliability-um9-runtime.mjs` | Health gate, golden flows |
| `verify-enterprise-reliability-um9-security.mjs` | Auth boundaries, replay idempotency |
| `verify-enterprise-reliability-um9-data-integrity.mjs` | Audit, correlation, dedup |
| `verify-enterprise-reliability-um9-failure-injection.mjs` | Bounded failure scenarios |
| `verify-enterprise-reliability-um9-performance.mjs` | Bounded load profile |
| `scripts/db/verify-restore-um9.mjs` | Backup restore golden flow |

## Runbooks

See `docs/runbooks/` for operational recovery procedures.
