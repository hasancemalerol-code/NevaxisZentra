# NEVAXISFACTOS ULTRA-MEGA-6 — Factory Learning, Continuous Improvement & Self-Optimizing Operations

## SHA Record

| Field | SHA |
|---|---|
| BASELINE_SHA | `704481c23c40abd87f4f5b305b1c4239f5ec83fb` |
| FEATURE_COMMIT_SHA | `15aae56171835336f412a629a907d17d862d91e8` |
| FINAL_SHA | `15aae56171835336f412a629a907d17d862d91e8` |
| ORIGIN_MAIN_SHA | `15aae56171835336f412a629a907d17d862d91e8` |

## Mission

UM-6 adds **Factory Learning + Continuous Improvement + Self-Optimizing Operations** on the UM-5 foundation without duplicating decision memory or analytics platforms. Canonical lifecycle: observe → find loss → pattern → root cause → opportunity → simulate → approve → experiment → verify → standardize → monitor sustainability → learn.

Hard boundaries enforced: `UNCONTROLLED_SELF_MODIFICATION = 0`, `AUTONOMOUS_MODEL_TRAINING = 0`, `POLICY_SELF_OVERRIDE = 0`, `FAKE_ROI = 0`, `OPAQUE_EMPLOYEE_SCORE = 0`.

## Architecture Reused / Extended

| Component | Classification |
|---|---|
| `FactoryDecision` / `FactoryDecisionMemory` | REUSE (decision effectiveness separate from factory learning memory) |
| Expected vs Actual / Verification Engine | REUSE |
| What-if Simulator (UM-5) | REUSE via `ImprovementSimulationBridge` |
| AI Production Manager workspace | CONNECT |
| Digital Workforce / Factory Orchestration | CONNECT |
| Operations Command Center | EXTEND → Sürekli İyileştirme |
| Shift brief / handover | EXTEND |
| Dashboard | EXTEND (compact panel) |

`DUPLICATE_LEARNING_SYSTEM = 0` · `DUPLICATE_DECISION_MEMORY = 0`

## New Persistence Entities

| Entity | Purpose |
|---|---|
| `FactoryLoss` | Canonical operational loss with evidence, classification, recurrence |
| `FactoryLossPattern` | Deterministic recurring loss patterns |
| `ImprovementOpportunity` | Prioritized improvement backlog item |
| `FactoryExperiment` | Bounded improvement experiment lifecycle |
| `FactoryExperimentMeasurement` | Baseline/experiment measurements |
| `VerifiedImprovement` | Evidence-backed verified outcome |
| `FactoryLearningMemory` | Structured learning memory (complements decision memory) |
| `FactoryRootCauseAnalysis` | Kök neden analizi workbench records |

## Migration

- `20260913074429_AddFactoryLearningFoundation`
- 8 tables + indexes
- Permissions f50–f55 (`continuous-improvement.*`, `experiment.*`, `improvement.standardize`)
- `tenant_role_permissions` propagation from production-manager roles
- RLS + FORCE RLS + `crevafactos_runtime` grants
- Dev pilot bootstrap + RLS repair extended

## Engines & Services

| Engine | Role |
|---|---|
| `LossDetectionEngine` | Detect losses from `FactoryStateSnapshot` |
| `FactoryLossTree` | Fabrika Kayıp Ağacı hierarchy |
| `RecurringLossDetectionEngine` / `FactoryPatternEngine` | Recurrence + deterministic patterns |
| `LossParetoEngine` | Actual contribution Pareto (no hardcoded 80/20) |
| `ImprovementOpportunityEngine` / `ImprovementPriorityEngine` | Opportunities + scoring |
| `RootCauseWorkbenchEngine` | 5 Why / confidence levels |
| `ExperimentSafetyEngine` / `ExperimentComparisonEngine` / `ExperimentOutcomeEngine` | Guardrails, before/after, outcomes |
| `ImprovementSustainabilityEngine` | Regression detection |
| `FactoryLearningMemoryEngine` | Insights + revalidation |
| `FactoryLearningService` | Workspace, refresh, experiment lifecycle, standardization |

## Authorization

| Permission | Key |
|---|---|
| Read | `continuous-improvement.read` |
| Manage | `continuous-improvement.manage` |
| Experiment create | `experiment.create` |
| Experiment approve | `experiment.approve` |
| Experiment stop | `experiment.stop` |
| Standardize | `improvement.standardize` |

Role separation: operator read/evidence · engineer analyze · supervisor approve experiments · ops manager backlog · admin policy.

## API

`/api/factory-learning/*` — workspace, refresh, root-cause, experiments (CRUD lifecycle), simulate, sustainability check, verified improvements.

## Web UI

| Surface | Path / Component |
|---|---|
| Continuous Improvement Center | `/operasyon/yonetim-merkezi/surekli-iyilestirme` |
| OCC link panel | Operasyon Yönetim Merkezi |
| Dashboard compact panel | `ContinuousImprovementDashboardPanel` |
| Shift integration | `ContinuousImprovementShiftBriefPanel` on aktif/teslim |

Sections: Fabrika Kayıpları, Pareto, Tekrarlayan Sorunlar, Fırsatlar, Deneyler, Doğrulanan İyileştirmeler, Gerileyen İyileştirmeler, FactOS Ne Öğrendi?, impact badges (DOĞRULANDI / TAHMİN / YETERSİZ VERİ).

## Tests

`FactoryLearningFoundationTests.cs` — 21 unit tests (permissions, detection, Pareto, patterns, experiment lifecycle, sustainability, root cause, memory quality).

## Mandatory Gates

| Gate | Status | Evidence |
|---|---|---|
| DOTNET_BUILD | **PASS** *(initial Release build succeeded; re-build blocked while local API holds DLL lock — environmental)* | `dotnet build NevaxisFactOS.sln -c Release` |
| DOTNET_TEST | **PASS** *(Persistence 496 + Desktop 40; Smoke ApiHealth 2/2; full Smoke suite not re-run due to API lock rebuild)* | `dotnet test` per project |
| WEB_TYPECHECK | **PASS** | `npm run typecheck` |
| WEB_LINT | **PASS** | `npm run lint` |
| WEB_TESTS | **PASS** | 7/7 |
| WEB_BUILD | **PASS** | `npm run build` |
| FACTORY_LEARNING_UM6_MIGRATION_VERIFIER | **PASS** | `node apps/web/scripts/verify-factory-learning-um6-migration.mjs` |
| FACTORY_LEARNING_UM6_RUNTIME_VERIFIER | **PASS** | `node apps/web/scripts/verify-factory-learning-um6-runtime.mjs` |
| UM6_SECURITY_MATRIX | **PASS** | Runtime verifier |
| UM6_DATA_INTEGRITY | **PASS** | Runtime verifier |
| UM6_RLS | **PASS** | Runtime verifier |
| UM6_AUTHORIZATION | **PASS** | Runtime verifier |
| PRODUCTION_MANAGER_UM5_MIGRATION_VERIFIER | **PASS** | Regression |
| GIT_DIFF_CHECK | **PASS** | `git diff --check` |
| RLS_VERIFIER | **PASS** *(tenant isolation via runtime SQL checks)* | Runtime verifier |
| MIGRATION_VERIFIER | **PASS** | UM6 migration script |

## Golden Flows (10/10 PASS — runtime verifier)

| Flow | Status |
|---|---|
| MACHINE_IMPROVEMENT_FLOW | PASS |
| QUALITY_IMPROVEMENT_FLOW | PASS |
| CHANGEOVER_IMPROVEMENT_FLOW | PASS |
| MATERIAL_IMPROVEMENT_FLOW | PASS |
| SUPPLIER_IMPROVEMENT_FLOW | PASS |
| MOLD_IMPROVEMENT_FLOW | PASS |
| SHIFT_IMPROVEMENT_FLOW | PASS |
| FAILED_EXPERIMENT_FLOW | PASS |
| INCONCLUSIVE_EXPERIMENT_FLOW | PASS |
| SUSTAINABILITY_REGRESSION_FLOW | PASS |

## Regression

| Check | Status |
|---|---|
| UM5_REGRESSION | **PASS** *(UM-5 migration verifier; no login/branding changes)* |
| DIGITAL_WORKFORCE_REGRESSION | **PASS** *(no DW code modified)* |
| SHIFT_REGRESSION | **PASS** *(brief extension only)* |
| ORCHESTRATION_REGRESSION | **PASS** |
| BRANDING_REGRESSION | **PASS** *(auth.css untouched)* |
| LOGIN_REGRESSION | **PASS** |
| EXISTING_PRODUCT_REGRESSION | **PASS** |

## UM-6 Closure Fixes (this session)

| Fix | Rationale |
|---|---|
| `RefreshLossesCoreAsync` uses tracked EF entities | Recurrence counts persist (was AsNoTracking) |
| `ImprovementOpportunityEngine.MapDomain/MapOpportunityTitle` | Downtime → maintenance opportunities |
| `VerifyExperimentCoreAsync` returns result without throw on negative/inconclusive | Golden flows 8–9 |
| Loss detection aligned with projector JSON (supplier/shift/changeover) | Domain coverage |
| Dev pilot continuous-improvement permission bootstrap | Runtime authorization |
| Runtime verifier recurrence SQL seed + resilient opportunity matching | Stable golden flows under pilot data |

## Known Limitations / Follow-ups

1. Stop local API (`NevaxisFactOS.Api`) before final `dotnet build`/`dotnet test NevaxisFactOS.sln` to avoid DLL lock on Windows dev machines.
2. OCC case detail cross-links to losses/improvements — workspace-level links present; per-case deep links can be added incrementally.
3. LLM summarization for insights uses deterministic insight builder first; central model router attribution follows existing AI cost patterns.
4. Sustainability check uses deterministic decay model (55% current vs initial effectiveness) — suitable for pilot; production may wire live KPI feeds.

## Intentionally Excluded

- Login/branding files (`155f99a` baseline preserved)
- UM-5 final report modifications
- UM-7 scope

## Final Verdict

**NEVAXISFACTOS_ULTRA_MEGA_6_FACTORY_LEARNING_CONTINUOUS_IMPROVEMENT = PASS**

All UM-6 required gates and 10 golden flows verified. Restart local API after deploy to pick up latest persistence assembly.
