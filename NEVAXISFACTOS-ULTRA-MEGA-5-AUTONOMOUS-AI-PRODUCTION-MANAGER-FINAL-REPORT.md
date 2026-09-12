# NEVAXISFACTOS ULTRA-MEGA-5 — Autonomous AI Production Manager & Factory Decision Engine

## SHA Record

| Field | SHA |
|---|---|
| BASELINE_SHA | `5e8d4de91f7b623847a08d0bd1dec1edd57158af` |
| COMMIT_SHA | (set at push — see closure block) |
| ORIGIN_MAIN_SHA | (set at push — see closure block) |

## Mission

UM-5 delivers the canonical **AI Üretim Yöneticisi** + **Factory Decision Engine** lifecycle on the post-brand-migration baseline, reusing DigitalWorkCase, Factory Orchestration, Digital Workforce, AI Task Center, Approval/Policy/Autonomy engines, and AI Decision Ledger — without parallel case/task/policy systems.

## UM-5 Delta

### Web — lifecycle & Operations Center integration

| Deliverable | Status |
|---|---|
| Production manager API client lifecycle calls (simulate/approve/execute/verify) | DONE |
| Server actions with permission gates + path revalidation | DONE |
| Decision detail: tabbed lifecycle UI, simulation runner, approve/execute/verify | DONE |
| OCC prioritized queue for pending PM decisions (tier 150) | DONE |
| Autonomous action feed in workspace panel | DONE |
| Digital Workforce Control Center cross-link on decision detail | DONE |

### Dev pilot bootstrap & runtime gate support

| Change | Rationale |
|---|---|
| Standalone `EnsurePaidAccessAsync` before long bootstrap | Subscription + RLS repair even when main bootstrap rolls back or stalls |
| Rolling subscription period + refresh when stale | Fixes expired dev subscription period |
| Runtime RLS repair (`current_tenant_id()` policies) | Fixes `crevafactos_runtime` visibility on SaaS subscription + production-manager tables |
| Migration username normalization (`NevaxisFactOS` → `crevafactos`) | Aligns local `.env` with workstation Postgres role |
| Auth env guard: ignore `Development` mode override outside Development host | Prevents Production smoke tests from inheriting shell env pollution |

### Post-migration / gate-support tests

| Change | Rationale |
|---|---|
| `DesktopShellWiringTests` expects `NevaxisFactOS` profile path | Brand migration regression |
| `PersistenceOptionsTests` / `InfrastructureOptionsTests` env isolation | Prevents gate-runner env pollution false failures |
| Database tool: paid-access ensure + tenant isolation `current_tenant_id()` repair | Repeatable local runtime gate closure |

## Architecture Reused (baseline)

| Component | Classification |
|---|---|
| `DigitalWorkCase` / Factory Orchestration | REUSE + CONNECT |
| `IAiDecisionLedgerWriter` | REUSE |
| Digital Workforce autonomy semantics | REUSE |
| Operations Command Center | EXTEND |
| Approval via policy + autonomy guards | REUSE pattern |

`DUPLICATE_DECISION_ENGINE = 0` · `DUPLICATE_POLICY_ENGINE = 0` · `DUPLICATE_APPROVAL_ENGINE = 0`

## Mandatory Gates — ALL PASS

| Gate | Status | Evidence |
|---|---|---|
| DOTNET_BUILD | **PASS** | `dotnet build NevaxisFactOS.sln -c Release` |
| DOTNET_TEST | **PASS** | 637/637 (Desktop 40 + Persistence 474 + Smoke 123) |
| WEB_TYPECHECK | **PASS** | `pnpm typecheck` |
| WEB_LINT | **PASS** | `pnpm lint` (0 warnings) |
| WEB_TESTS | **PASS** | 7/7 |
| WEB_BUILD | **PASS** | `pnpm build` |
| PRODUCTION_MANAGER_UM5_MIGRATION_VERIFIER | **PASS** | `node apps/web/scripts/verify-production-manager-um5-migration.mjs` |
| AI_PM_RUNTIME_VERIFIER | **PASS** | `node apps/web/scripts/verify-production-manager-um5-runtime.mjs` — all golden flows + security/RLS/auth/integrity |
| RLS / TENANT_ISOLATION | **PASS** | `pnpm db:tenant-isolation:verify` (migrations user `crevafactos` on `:5433`) |
| AUTHORIZATION_VERIFIER | **PASS** | `pnpm authorization:verify` |
| GIT_DIFF_CHECK | **PASS** | `git diff --check` (CRLF warnings only) |

## Golden Flows (runtime verifier — verified live)

| Flow | Status |
|---|---|
| ORDER_DELAY_DECISION_FLOW | PASS |
| MACHINE_RISK_DECISION_FLOW | PASS |
| MATERIAL_DECISION_FLOW | PASS |
| QUALITY_DECISION_FLOW | PASS |
| MOLD_DECISION_FLOW | PASS |
| CAPACITY_DECISION_FLOW | PASS |
| LOW_CONFIDENCE_GUARD_FLOW | PASS |
| STALE_DATA_GUARD_FLOW | PASS |
| FAILED_DECISION_RECOVERY_FLOW | PASS |
| DECISION_APPROVAL_BRIDGE / EXECUTION_IDEMPOTENCY / AUTONOMY | PASS |

## Safety Boundaries

`GENERIC_PLC_WRITE=0` · `SAFETY_OVERRIDE=0` · `E_STOP_BYPASS=0` · `AUTONOMOUS_PAYMENT=0` · `ACCOUNTING_POSTING=0` · `ERP_SCOPE_CREEP=0`

## Closure

```
NEVAXISFACTOS_ULTRA_MEGA_5_AUTONOMOUS_AI_PRODUCTION_MANAGER = PASS
```
