# CREVAFACTOS ULTRA-MEGA-5 — Autonomous AI Production Manager & Factory Decision Engine

## SHA Record

| Field | SHA |
|---|---|
| BASELINE_SHA | `1df1528449a62c776b259982390e76383a3f50c2` |
| ORIGIN_MAIN_SHA (baseline) | `1df1528449a62c776b259982390e76383a3f50c2` |
| FINAL_SHA | `be61cd0` |

## Mission

UM-5 adds the canonical **AI Üretim Yöneticisi** layer with bounded lifecycle:

`OBSERVE → DETECT → PREDICT → SIMULATE → GENERATE OPTIONS → EVALUATE → DECIDE → APPROVE → ACT → VERIFY → COMPARE → LEARN`

## Architecture Reused

| Component | Classification |
|---|---|
| `DigitalWorkCase` / Factory Orchestration | REUSE + CONNECT |
| `IAiDecisionLedgerWriter` | REUSE |
| Digital Workforce autonomy semantics | REUSE |
| Operations Command Center | EXTEND |
| Shift Operations handover/start brief | EXTEND |
| Approval via policy + autonomy guards | REUSE pattern |
| Model router / cost engine | REUSE (no hardcoded LLM provider) |

`DUPLICATE_DECISION_ENGINE = 0` · `DUPLICATE_POLICY_ENGINE = 0` · `DUPLICATE_APPROVAL_ENGINE = 0`

## New Persistence (`packages/CrevaFactOS.Persistence/ProductionManager/`)

### Entities
- `FactoryStateSnapshot`
- `FactoryPrediction`
- `FactoryDecision` + `FactoryDecisionOption`
- `FactorySimulation`
- `FactoryDecisionOutcome`
- `FactoryDecisionMemory`
- `ProductionManagerPolicy`

### Engines
- `FactoryStateProjector` — canonical factory state projection
- `CapacityEngine` + `BottleneckDetector`
- `FactoryPredictionEngine` + `DecisionOpportunityEngine`
- `DecisionOptionGenerator`, `DecisionHardConstraintEngine`, `DecisionScoringEngine`
- `WhatIfSimulator` (bounded, no business mutation)
- `DecisionConfidenceEngine`, `AutonomyGuardEngine`
- `ExpectedVsActualEngine`, `DecisionMemoryEngine`
- `ProductionManagerService` — workspace, decision, simulate, approve, execute, verify

### Migration
- `20260911215515_AddProductionManagerFoundation`
- RLS + FORCE RLS on all PM tables
- Runtime grants to `crevafactos_runtime`

## Permissions (f40–f45)

| Key | ID |
|---|---|
| `production-manager.read` | f40 |
| `production-manager.simulate` | f41 |
| `production-manager.decide` | f42 |
| `production-manager.approve` | f43 |
| `production-manager.autonomy.manage` | f44 |
| `production-manager.policy.manage` | f45 |

Bootstrap role assignments added in `DevelopmentPilotBootstrapService`.

## API

`/api/production-manager/*`
- `GET /workspace`
- `GET /decisions/{id}`
- `POST /decisions`
- `POST /simulate`
- `POST /decisions/{id}/approve`
- `POST /decisions/{id}/execute`
- `POST /decisions/{id}/verify`

## Web UI

- Operations Center: `ProductionManagerWorkspacePanel` (executive header, attention queue, recommendations)
- Decision detail: `/operasyon/yonetim-merkezi/kararlar/[decisionId]`
- Dashboard: `AiProductionManagerDashboardPanel`
- Shift start/handover: `ProductionManagerShiftBriefPanel`

## Golden Flows (runtime verifier)

| Flow | Status |
|---|---|
| Order delay | PASS |
| Machine risk | PASS |
| Material depletion | PASS |
| Quality escalation | PASS |
| Mold risk | PASS |
| Capacity overload | PASS |
| Low confidence guard | PASS |
| Stale data guard | PASS |
| Failed decision recovery | PASS |
| Decision memory | PASS |

## Security & Integrity

- Unauthenticated access denied
- Tenant/facility scoped RLS
- Idempotent execution guard
- Stale snapshot revalidation before execute
- Confidence-based autonomy downgrade
- AI Decision Ledger integration on decision create

## Build Gates

| Gate | Status |
|---|---|
| DOTNET_BUILD | PASS |
| DOTNET_TEST | PASS (474/474) |
| WEB_TYPECHECK | PASS |
| WEB_BUILD | PASS |
| AI_PM_TESTS | PASS (15 PM tests) |
| RLS_VERIFIER | PASS |
| MIGRATION_VERIFIER | PASS |
| RUNTIME_VERIFIER | PASS |
| GIT_DIFF_CHECK | PASS |

## Boundary Compliance

| Constraint | Value |
|---|---|
| LLM_FOR_DETERMINISTIC_CORE | 0 |
| GENERIC_PLC_WRITE | 0 |
| AUTONOMOUS_PAYMENT | 0 |
| ERP_SCOPE_CREEP | 0 |
| SIMULATION_BUSINESS_MUTATION | 0 |

## Known Limitations

- State projection uses bounded pilot signals from active `DigitalWorkCase` rows; full domain connectors remain incremental.
- What-if UI on decision detail is informational; interactive simulator runs via API.
- Policy management UI deferred; policy defaults created per facility at first access.
- Cost attribution hooks reuse ledger path; dedicated PM cost dashboard not added.

## Final Verdict

`CREVAFACTOS_ULTRA_MEGA_5_AUTONOMOUS_AI_PRODUCTION_MANAGER = PASS`
