# Nevaxis FactOS — Golden Demo Freeze Manifest

## Product

**Nevaxis FactOS**

## Artifact

**Golden Demo** — Production Recovery (INJ-001 pilot)

## Canonical route

`/golden-demo/uretim-kurtarma`

## Primary runtime

**localhost Web + API**

- Web: `pnpm dev:web` → `http://localhost:3000`
- API: `pnpm dev:api` → `http://localhost:5000`

## Accepted feature packages

| Package | Scope |
|---------|--------|
| **P1** | Machine intelligence, cycle loss, production recovery workspace |
| **P2** | Reset engine, presentation orchestration |
| **P2B** | Digital Factory Team evidence & orchestration |
| **P3** | Verified production recovery, energy, business impact |

## Accepted P3 baseline before freeze

```
fe54de6d08cfccbe81787177008c43cf0d05a87e
```

## Freeze state

**SALES READY / FEATURE FROZEN**

## Primary KPI

**+X GOOD PARTS / SHIFT** (runtime-calculated, verification-gated display)

## Primary story

Machine + Production Recovery

## Secondary

Energy Impact

## Final commercial layer

Business Impact (estimated — must not dominate hero)

## Desktop

**Not** Tuesday primary demo

## Demo chain (canonical)

Production Target at Risk → Machine Intelligence → Cycle Loss Decomposition → Production Recovery Opportunity → Digital Factory Team → Orchestrator → Approval → Technical Intervention → Validation Run → Before / After → Outcome Verification → PRODUCTION RECOVERED → +X GOOD PARTS / SHIFT → Energy Impact → Business Impact

## Allowed changes after freeze

- Bug fixes, demo reliability, presentation clarity, reset/smoke verifiers, runbook updates
- Demo-blocking defect fixes only

## Forbidden after freeze

- New modules, agents, engines, analytics products
- Architecture rewrites, fake demo values, demo auth bypass
- P4 or speculative expansion until after Tuesday sales demo

## No-fake-demo standard

All outcomes must remain **MEASURED / CALCULATED / ESTIMATED / VERIFIED** with real runtime/replay-backed behavior.

## Security (unchanged)

Authentication, authorization, tenant isolation, PostgreSQL RLS, audit integrity, approval requirements — **no demo bypass**.

## Verification

- `verify:golden-demo-sales-ready`
- P1 / P2 / P2B / P3 runtime + live flow verifiers
- Golden Demo persistence tests
- RLS / tenant isolation scripts as documented in infrastructure README

## Sales materials

| Document | Purpose |
|----------|---------|
| `NEVAXISFACTOS-GOLDEN-DEMO-SALES-RUNBOOK.md` | Operator startup & live path |
| `NEVAXISFACTOS-GOLDEN-DEMO-PRESENTER-CARD.md` | One-page presenter cues |
| `NEVAXISFACTOS-GOLDEN-DEMO-BACKUP-RECORDING-PLAN.md` | Backup clip plan |
| `NEVAXISFACTOS-GOLDEN-DEMO-SALES-READY-FREEZE-FINAL-REPORT.md` | Closure evidence |

## Feature changes after freeze

Not allowed until sales demo is completed unless required to resolve a **demo-blocking defect**.
