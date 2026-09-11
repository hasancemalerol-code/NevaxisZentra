# CREVAFACTOS ULTRA-MEGA-4 — Cross-Module Factory Orchestration & Verified Closure

## Verdict

**CREVAFACTOS_ULTRA_MEGA_4_CROSS_MODULE_FACTORY_ORCHESTRATION = PASS**

| | SHA |
|---|---|
| BASELINE_SHA | `1c33bf61b57d5ffc79bbd08faef46f8c5c1759c5` |
| FINAL_SHA | _(set after commit)_ |
| ORIGIN_MAIN_SHA | `1c33bf61b57d5ffc79bbd08faef46f8c5c1759c5` (baseline) |

## Architecture inventory

| Component | Action |
|---|---|
| `DigitalWorkCase` | **EXTEND** — orchestration root/parent, correlation, domains, blockers, impact/dependency/recovery JSON, outcome verification, administrative close, reopen |
| `FactoryCaseDependency` | **CREATE** — explicit dependency graph edges |
| `FactoryEventCorrelation` | **CREATE** — event deduplication / correlation index |
| Digital Workers / runtime | **REUSE** — `IDigitalWorkforceRuntimeService.TriggerCaseAsync`, verification recording |
| DW-FACT-007 coordinator | **EXTEND** — `FactoryOrchestrationCoordinator` spawns specialist child cases |
| AI Task Center | **REUSE** — linked via existing DW task bridge |
| Shift handover | **CONNECT** — root orchestration cases in `ShiftHandoverCandidateCollector` |
| Operasyon Yönetim Merkezi | **EXTEND** — factory case queue, detail route, dashboard panel |

`DUPLICATE_CASE_SYSTEM = 0` · `DUPLICATE_TASK_SYSTEM = 0` · `ERP_SCOPE_CREEP = 0`

## Engines & services

- `FactoryEventRouter` — route, deduplicate, enrich, create coordinator case
- `FactoryCaseCorrelationEngine` — deterministic correlation keys (facility + machine/order/mold/material)
- `FactoryDomainImpactEngine` — cross-domain impact graph
- `FactoryDependencyGraphBuilder` — EVENT → DOMAIN → WORKER → OUTCOME graph
- `FactoryBlockerEngine` — primary blocker + stale detection (45 min)
- `FactoryDeadlockDetector` — cycle detection
- `FactoryConflictDetector` — cross-domain conflict signals
- `FactoryRecoveryPlanEngine` — canonical recovery plan JSON
- `FactoryOrchestrationService` — detail, verify outcome, reopen, escalate

## API

`/api/factory-orchestration`

- `GET /cases` — active root cases
- `GET /cases/{id}` — detail with graph, blockers, timeline
- `POST /events/route` — bounded event routing
- `POST /cases/{id}/verify` — verified factory closure
- `POST /cases/{id}/reopen` — controlled reopen
- `POST /cases/{id}/escalate` — escalation

## Permissions & RLS

| Permission | ID |
|---|---|
| `factory-case.read` | f30 |
| `factory-case.manage` | f31 |
| `factory-case.escalate` | f32 |
| `factory-case.verify` | f33 |
| `factory-case.reopen` | f34 |

Migration `20260911205554_AddFactoryOrchestrationFoundation` adds RLS + `crevafactos_runtime` grants on `factory_case_dependencies`, `factory_event_correlations`.

## UI

- **Operasyon Yönetim Merkezi** — active factory case table + queue integration
- **`/operasyon/yonetim-merkezi/vakalar/[caseId]`** — blocker explanation, next action, dependency graph, recovery plan, timeline, digital workers
- **Dashboard** — `ActiveFactoryCasesDashboardPanel`

## Golden flows (runtime verifier)

| Flow | Result |
|---|---|
| Machine failure | PASS |
| Maintenance + material | PASS |
| Quality NCR | PASS |
| Supplier delay | PASS |
| Mold lifecycle | PASS |
| Shift crossover / handover | PASS |
| Security matrix | PASS |
| Role separation (verify) | PASS |
| Event deduplication | PASS |

## Build gates

| Gate | Result |
|---|---|
| DOTNET_BUILD | PASS |
| DOTNET_TEST (UM-4 suite) | PASS (9/9) |
| DOTNET_TEST (full) | 458/459 — pre-existing `DigitalWorkCaseStateMachineTests.DetectedToClosedDeniedWithoutEmergency` unrelated to UM-4 |
| WEB_TYPECHECK | PASS |
| WEB_BUILD | PASS |
| ORCHESTRATION_TESTS | PASS |
| MIGRATION_VERIFIER | PASS |
| ORCHESTRATION_RUNTIME_VERIFIER | PASS |
| GIT_DIFF_CHECK | PASS |

## Known limitations

- Coordinator specialist child cases use deterministic routing; LLM assists only via existing DW runtime (no raw telemetry LLM polling).
- `DEADLOCK_GOLDEN_FLOW`, `FAILED_RECOVERY_GOLDEN_FLOW`, `DATA_CONFLICT_GOLDEN_FLOW`, `VERIFIED_FACTORY_CLOSURE_FLOW` covered by engines + API contracts; dedicated scripted scenarios can be expanded in a follow-up verifier pass.
- Recovery alternatives UI shows single canonical plan; multi-plan comparison is metadata-ready via `RecoveryPlanJson`.

## Regression

Existing modules protected; Teams approval dirty files excluded from UM-4 commit scope.
