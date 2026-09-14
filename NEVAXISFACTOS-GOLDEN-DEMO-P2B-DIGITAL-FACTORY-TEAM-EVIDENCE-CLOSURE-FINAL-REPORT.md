# NEVAXISFACTOS Golden Demo P2B — Digital Factory Team Evidence Closure Final Report

## Package

**FACTOS-GOLDEN-DEMO-P2B** — Digital Factory Team Execution & Evidence Closure

**Route:** `/golden-demo/uretim-kurtarma` (unchanged)

**Baseline preserved:** P1 machine intelligence / recovery engine, P2 reset & presentation orchestration

---

## Files Changed

### Persistence (read-model projection)
- `packages/NevaxisFactOS.Persistence/GoldenDemoP2B/GoldenDemoP2BModels.cs`
- `packages/NevaxisFactOS.Persistence/GoldenDemoP2B/GoldenDemoP2BEngines.cs`
- `packages/NevaxisFactOS.Persistence/GoldenDemoP2B/GoldenDemoP2BService.cs`
- `packages/NevaxisFactOS.Persistence/GoldenDemoP2B/GoldenDemoP2BServiceCollectionExtensions.cs`
- `packages/NevaxisFactOS.Persistence/PersistenceServiceCollectionExtensions.cs`

### API
- `apps/api/GoldenDemoP2B/GoldenDemoP2BEndpointRouteBuilderExtensions.cs`
- `apps/api/GoldenDemoP2B/GoldenDemoP2BEndpointHelpers.cs`
- `apps/api/Program.cs`

### Web
- `apps/web/src/features/golden-demo-p2b/components/golden-demo-p2b-digital-factory-panel.tsx`
- `apps/web/src/features/golden-demo-p2b/server/golden-demo-p2b-api-client.ts`
- `apps/web/src/features/golden-demo-p2b/server/golden-demo-p2b-reader.ts`
- `apps/web/src/features/golden-demo-p2b/server/golden-demo-p2b-server-boundary.ts`
- `apps/web/src/features/golden-demo-p1/components/golden-demo-p1-workspace-view.tsx`
- `apps/web/src/app/(app)/golden-demo/uretim-kurtarma/page.tsx`
- `apps/web/package.json`
- `apps/web/scripts/verify-golden-demo-p2b-runtime.mjs`
- `apps/web/scripts/verify-golden-demo-p2b-live-flow.mjs`

### Tests
- `tests/NevaxisFactOS.Persistence.Tests/GoldenDemoP2BFoundationTests.cs`

---

## Architecture Reused

- **P1 workspace service** (`IGoldenDemoP1Service.GetWorkspaceAsync`) as single source of runtime truth
- **Existing `GoldenDemoAgentFinding` records** — not duplicated; P2B projects evidence at read time
- **P1 approval / execute / verify endpoints** — approval remains persisted workflow state
- **P2 reset engine** — live-flow verifier resets demo before approval persistence check
- **Authorization:** `golden-demo-p1.read` for P2B read endpoint (no new permission, no RLS weakening)
- **Tenant database session** pattern from P1/P2 API endpoints

---

## Agent Evidence Implementation

- `GoldenDemoP2BAgentEvidenceEngine` builds `AgentEvidenceDetail` from persisted findings + live workspace metrics
- Runtime cycle values for INJ-001: reference **28.0 s**, current **31.2 s**, loss **+3.2 s**
- Evidence classification, confidence, data inputs, calculation inputs/results, resulting action
- Insufficient evidence states: `INSUFFICIENT BASELINE`, `INSUFFICIENT MOLD EVIDENCE`, `ENERGY DATA INSUFFICIENT`
- UI: clickable agent cards open **Agent Evidence** drawer with real values (no static placeholders)

---

## Activity Source Model

- `GoldenDemoP2BActivityProjector` projects timeline from:
  - agent finding timestamps
  - recovery plan creation / approval state
  - production event status transitions
  - verification run records
- No decorative activity table; events omitted if they did not occur
- Ordered by real timestamps only

---

## Orchestrator Behavior

- `GoldenDemoP2BOrchestratorEngine` synthesizes sales-visible assessment from P1 recovery + contributors + plan
- Lists **inputs used** (specialist agent labels from actual findings)
- Surfaces **insufficient specialist evidence** explicitly (does not fabricate certainty)
- Production consequence uses P1 recovery numbers (+42 good parts / shift at pilot baseline)
- Required control states human approval + machine-safe language (no direct PLC claims)

---

## Approval Sheet Behavior

- `GoldenDemoP2BApprovalSheetEngine` exposes full approval context before action
- **APPROVE** button calls existing `POST /api/golden-demo-p1/events/{id}/approve` (persisted)
- After approval: approved by, approved at, status, audit reference (plan ID)
- Refresh preserves approval state (verified in live-flow)

---

## Work Traceability

- `GoldenDemoP2BPlanTraceabilityEngine` maps plan actions to source agents via action codes
- Each action shows evidence required, validation criteria, technical finding, work reference ID

---

## Intervention Execution Timeline

- `GoldenDemoP2BInterventionTimelineEngine` stages derived from plan + event status + verification
- Machine-safe execution language in UI panel footer

---

## Runtime Evidence (INJ-001 localhost)

| Check | Result |
|-------|--------|
| P2B API `/api/golden-demo-p2b/digital-factory-team` | PASS |
| Agent evidence cycle loss 3.2 s | PASS |
| Activity timeline (real events) | PASS |
| Orchestrator inputs + recovery KPI | PASS |
| Approval persistence after reset → approve → refresh | PASS |
| UI panels rendered on `/golden-demo/uretim-kurtarma` | PASS |
| P1 runtime verifier regression | PASS |
| P2 runtime verifier regression | PASS |

---

## Tests

- **GoldenDemoP2BFoundationTests** (9 tests): agent evidence, insufficient evidence, orchestrator, activity, approval, traceability, intervention timeline
- **GoldenDemoP1FoundationTests** regression: PASS (22 Golden Demo tests total)
- **Verifiers:** `verify-golden-demo-p2b-runtime.mjs`, `verify-golden-demo-p2b-live-flow.mjs`

---

## Security Status

- Tenant isolation via existing `TenantDatabaseSession` + facility access resolver
- P2B endpoint requires `golden-demo-p1.read` + facility scope
- No new DELETE grants; no demo-only security bypass

---

## Remaining Gaps

- Approval comment from request body is not persisted to a dedicated column (audit reference uses plan ID; optional future migration)
- `GoldenDemoDigitalWorkCaseLink` not yet written on execute (work references use plan-action IDs)
- Web dev on port 3001 may require restart after deploy to pick up hot-reload edge cases

---

## External Machine Execution Statement

FactOS **does not** claim direct PLC / machine parameter changes. The demo uses honest language: FactOS coordinates technical intervention, creates and tracks recovery work, and authorized operator/maintenance roles execute physical work. No unsupported machine connector claims are made.

---

**FACTOS-GOLDEN-DEMO-P2B RUNTIME CLOSURE: PASS**
