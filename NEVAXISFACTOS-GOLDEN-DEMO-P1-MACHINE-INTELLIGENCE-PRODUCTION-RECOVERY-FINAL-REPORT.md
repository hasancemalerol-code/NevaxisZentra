# NEVAXIS FACTOS — GOLDEN DEMO P1 FINAL REPORT

## Machine Intelligence + Cycle Loss Decomposition + Production Recovery Engine

**Package:** FACTOS-GOLDEN-DEMO-P1  
**Mode:** localhost Web + API (no desktop changes)  
**Status:** IMPLEMENTED — localhost runtime closure **PASS** (2026-09-14)

---

## A. Implemented Features

### Persistence (`packages/NevaxisFactOS.Persistence/GoldenDemoP1/`)
- **Entities:** production events, cycle baselines, observations, recovery plans/actions, agent findings, verification runs, digital work case links
- **Engines (calculated, not hard-coded):**
  - `CycleLossDecompositionEngine` — phase loss = current − reference; favorable variance separate; micro-stops separate
  - `RootContributorAnalysisEngine` — ranked contributors with evidence-safe language
  - `ProductionRecoveryEngine` — remaining shift × yield × availability; restores validated band (no unsafe acceleration)
  - `WhatIfDecisionEngine` — scenarios A/B/C with evidence-based RECOMMENDED
  - `OutcomeVerificationEngine` — VERIFIED / PARTIALLY VERIFIED / NOT VERIFIED / INSUFFICIENT DATA
  - `BeforeAfterComparisonEngine` — post-intervention comparison
  - `GoldenDemoAgentOrchestrationEngine` — 7 digital specialists + orchestrator
  - `SafeLanguage` — production-safe messaging (no “run faster”)
- **Service:** workspace build, approve, execute, complete-intervention, verify workflows
- **Repository + authorization keys** (`golden-demo-p1.read|manage|approve|execute|verify`)

### Migration
- `20260914083341_AddGoldenDemoP1Foundation` — 8 tables, RLS, runtime grants, permissions f76–f7a

### Development seed
- `DevelopmentPilotGoldenDemoP1Seed` — INJ-001 event with reference 28.0s vs current 31.2s, recovery plan actions, bootstrap hook

### API (`apps/api/GoldenDemoP1/`)
- `GET /api/golden-demo-p1/workspace`
- `POST /api/golden-demo-p1/events/{id}/refresh|approve|execute|complete-intervention|verify`

### Web UI
- Route: `/golden-demo/uretim-kurtarma`
- Nav: **Altın Demo — Üretim Kurtarma**
- Entry from Operasyon Yönetim Merkezi
- Full demo flow panel: machine intelligence → cycle loss → recovery → agents → what-if → plan → approval → intervention → before/after → +X good parts/shift

### Tests & verifiers
- `GoldenDemoP1FoundationTests` — 10 tests PASS
- `verify-golden-demo-p1-migration.mjs` — PASS
- `verify-golden-demo-p1-runtime.mjs` — PASS
- `verify-golden-demo-p1-live-flow.mjs` — **PASS** (API + Web + workflow + page render)
- Web `tsc --noEmit` — PASS

---

## B. Reused Existing Capabilities

| Capability | Reuse |
|---|---|
| INJ-001 edge machine | `DevelopmentPilotIds.EdgePilotPressMachineId` |
| Digital workforce worker codes | ProductionRecovery, MaintenanceResolution, MoldLifecycle, FactoryCoordinator |
| Facility access + tenant RLS | Same pattern as Production Manager / UM-9 |
| Operasyon Yönetim Merkezi | Demo entry link |
| Process OEE / shift context | Compatible pilot facility seed |
| Authorization model | Permission definitions + role inheritance SQL |

---

## C. Golden Demo Flow

1. `/dashboard` or `/operasyon/yonetim-merkezi` → Altın Demo link
2. `/golden-demo/uretim-kurtarma`
3. Shift target at risk (event title + attainment KPIs)
4. Machine Intelligence metrics
5. Cycle Loss Decomposition (+ micro-stop separate)
6. Production Recovery Opportunity (**+X good parts/shift** from API)
7. Digital Factory Team findings
8. What-If scenarios (RECOMMENDED when evidence supports)
9. Recovery plan + real action entities
10. Approve → Execute → Complete intervention → Verify
11. Before/After + verification state
12. Secondary energy/business impact (ESTIMATED vs VERIFIED separated)

---

## D. Calculations

### Reference cycle selection
Priority: stable historical window (seeded baseline for same machine/mold/product). If missing → `INSUFFICIENT BASELINE`.

### Cycle loss decomposition
`phase_loss = current_phase − reference_phase` (not clamped). Micro-stop duration tracked separately as equivalent loss.

### Production recovery
```
remaining_seconds = shift_end − now
yield = good / (good + reject)
availability_factor = availability% / 100
current_expected = floor((remaining / current_cycle) × availability × yield)
recovered_cycle = max(reference, current − sum(positive phase losses))
recovered_expected = floor((remaining / recovered_cycle) × availability × yield)
recovery_good_parts = max(0, recovered_expected − current_expected)
```

### Verification criteria
Cycle ↓, throughput ↑, micro-stops ↓, reject stable/improved, availability stable/improved, energy stable/improved (when data exists), sustained validation window.

---

## E. Real vs Estimated

| Output | Classification |
|---|---|
| Cycle times, phase durations, good/reject counts | **Measured** (seed/replay observation) |
| Cycle loss, recovery good parts, what-if projections | **Calculated** |
| Business TRY impact (125 TRY/part model) | **Estimated** |
| Verified TRY after VERIFIED outcome | **Verified** (only when verification passes) |
| Agent summaries | **Calculated** from engine outputs |
| injection_pressure | **Unavailable** (explicit in UI) |

Primary KPI **+X GOOD PARTS / SHIFT** = `recovery.productionRecoveryGoodParts` from API — never hard-coded in UI.

---

## F. Tests

```
GoldenDemoP1FoundationTests: 10/10 PASS
verify:golden-demo-p1-migration: PASS
verify:golden-demo-p1-runtime: PASS
verify-golden-demo-p1-live-flow.mjs: PASS
Web tsc: PASS
```

### Localhost runtime closure (2026-09-14)

| Step | Result |
|---|---|
| `dotnet ef database update` (`20260914083341_AddGoldenDemoP1Foundation`) | **Applied** |
| API `http://127.0.0.1:5000/health` | **200** |
| Web `http://localhost:3001` | **Ready** |
| Route `/golden-demo/uretim-kurtarma` | **Rendered** (recovery KPI in HTML) |
| Workflow approve → execute → complete → verify | **PASS** |

#### Recorded INJ-001 runtime values (initial workspace)

| Metric | Value |
|---|---|
| Machine | **INJ-001** |
| Reference cycle | **28.0 s** |
| Current cycle | **31.2 s** |
| Cycle loss | **+3.2 s** |
| Micro-stop equivalent loss (separate) | **120 s** |
| Reference source | Aynı makine / aynı kalıp / stabil geçmiş üretim penceresi |
| Current expected shift good parts | **371** |
| Recovered expected shift good parts | **413** |
| **+X good parts / shift** | **42** |

#### Phase loss decomposition (calculated)

| Phase | Loss (s) |
|---|---|
| Soğutma (cooling) | 1.5 |
| Vida Geri Çekme (screw-recovery) | 0.8 |
| Kalıp Hareketi (mold-movement) | 0.7 |
| Diğer Proses (other-process) | 0.2 |

#### Post-intervention + verification

- Before/after cycle improvement confirmed after complete-intervention
- Verification result: **verified**
- Verified recovered good parts per shift: **122**

#### Digital Factory Team (execution evidence, not decorative UI)

| Agent | Evidence | Runtime summary |
|---|---|---|
| Production Performance Agent | calculated | Shift gap analysis: +42 recoverable good parts estimated |
| Machine Performance Agent | calculated | Cycle loss 3.2s vs reference 28.0s |
| Process Optimization Agent | calculated | Top process contributor: Soğutma (1.5s) |
| Mold Health A | calculated | Mold/tool cycle and maintenance context included in recovery assessment |

Additional orchestrated agents present at runtime: maintenance, energy, orchestrator (all with measured/calculated/verified evidence classifications).

---

## G. Regressions

- No desktop packaging changes
- Tenant RLS on all new tables
- Permission keys f76–f7a with role inheritance
- Existing UM-9 / security baselines not modified
- Migration applied locally; permission SQL column names corrected before apply

---

## H. Remaining Gaps

- Live PLC phase telemetry for all process parameters (injection pressure marked unavailable)
- MES order linkage for shift target (uses seeded event target)
- Digital work case links seeded optionally — not required for PASS demo path
- Golden demo reset: bootstrap re-seeds active event state on pilot refresh; full intervention reset relies on re-bootstrap / new event cycle
- Live-flow verifier resets pilot event via SQL before each run; production reset UX not yet exposed in UI

---

## Primary Product Message

FactOS identifies why INJ-001 is below shift target, decomposes lost seconds by phase, calculates recoverable good parts within the validated operating band, coordinates technical recovery actions, and verifies post-intervention improvement — with **+X GOOD PARTS / SHIFT** computed from real observation and engine data.
