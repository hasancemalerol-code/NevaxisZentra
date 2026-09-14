# NEVAXISFACTOS Golden Demo — Customs File Digital Worker Final Report

## Package

`FACTOS-GOLDEN-DEMO-CUSTOMS-FILE-DIGITAL-WORKER`

Isolated Golden Demo route: `/golden-demo/gumruk-dosyasi`

Production Golden Demo preserved at `/golden-demo/uretim-kurtarma` (P1/P2/P2B/P3 unchanged).

## Existing Worker Architecture Reused

- Digital Workforce worker codes (`DigitalWorkforceConstants` / `GoldenDemoCustomsFileWorkerCodes`)
- Outcome-oriented workflow service pattern (P1 assign → evaluate → act → verify)
- Corrective work / task entities with completion vs outcome separation (P1/P3 philosophy)
- Activity/evidence trace projection (P2B-style stages)
- Approval persistence with permission keys and tenant RLS
- Deterministic verification engine with canonical result states
- Development pilot bootstrap seeding and isolated reset

No parallel worker engine, orchestration layer, or approval system was introduced.

## Worker Identity

| Field | Value |
| --- | --- |
| Code | `DW-EXP-001` |
| English | Customs File Digital Worker |
| Turkish | Gümrük Dosyası Dijital Çalışanı |
| Outcome ID | `cc000001-0010-4000-8000-000000000001` |

## Assigned Outcome

> Bu siparişin gümrük dosyasını hazırla ve gümrük müşavirine gönderime hazır hale getir.

## Demo Order

| Field | Value |
| --- | --- |
| Order | `EXP-DEMO-DE-001` |
| Customer | PackPro GmbH |
| Destination | Germany |
| Delivery | Industriestrasse 14, 70469 Stuttgart, Germany |
| Currency | EUR |
| Incoterm | DAP |
| Shipment | `SHP-DE-2026-0142` |
| Packages | 12 |
| Shipment net weight | 485.0 kg |
| Shipment gross weight | 510.0 kg |
| Products | PP-LID-240 (12000), PP-BOWL-240 (8000) with GTIP 3923.50.00, origin TR |

Trade requirements configured as **not required** for A.TR, EUR.1, Certificate of Origin in the default demo seed.

## Customs Clearance Package Model

Persisted entity `GoldenDemoCustomsFilePackage` with:

- Package ID and version
- Tenant / facility / outcome reference
- Manifest JSON (current valid documents, requirement results, approval evidence, verification reference)
- Artifact label: `FACTOS-CUSTOMS-PACKAGE-{ORDER}-v{VERSION}` (structured manifest artifact; no fake empty ZIP)

Supporting tables: outcomes, documents (versioned), blockers, corrective works, verification runs, activities.

## Requirement Logic

`GoldenDemoCustomsFileRequirementEngine` evaluates runtime requirements including:

- `ORDER_DATA_COMPLETE`, `CUSTOMER_DATA_COMPLETE`, `DESTINATION_COMPLETE`, `PRODUCT_DATA_COMPLETE`
- `GTIP_PRESENT_IF_REQUIRED`, `ORIGIN_PRESENT_IF_REQUIRED`
- `COMMERCIAL_INVOICE_READY`, `PACKING_LIST_READY`, `PACKING_DATA_CONSISTENT`
- `CONFIGURED_TRADE_DOCUMENTS_ADDRESSED`, `NO_BLOCKING_ISSUES`, `INTERNAL_APPROVAL_COMPLETE`

Each exposes requirement code, source/rule, required flag, status, evidence, blocking flag, explanation.

## Documents

| Document | Approach |
| --- | --- |
| Commercial Invoice | Persisted structured JSON artifact from order/shipment runtime data via `GoldenDemoCustomsFileDocumentFactory` |
| Packing List | Persisted structured JSON artifact; v1 seeded with inconsistent net weight during evaluation |
| A.TR / EUR.1 / CoO | Created only when configured in `TradeRequirementsJson` |

Binary PDF generation was not added; structured versioned artifacts are real persisted records inspectable in UI/API.

## Initial Blocker

Deterministic **Packing List net weight mismatch**:

- Shipment source: **485.0 kg** (outcome field)
- Packing List v1 net weight: **472.5 kg** (`ShipmentNetWeightKg - 12.5m` at document creation)
- Detected by `GoldenDemoCustomsFileConsistencyEngine` check `net-weight-consistency`
- Blocker code: `packing-list-net-weight-mismatch`

No hard-coded UI alert; mismatch is computed from persisted values.

## Consistency Checks

Deterministic checks include order/invoice/packing quantity alignment, product-line consistency, package count, net/gross weight, customer, destination, currency, Incoterm, required customs data presence, configured document presence.

## Corrective Execution

On mismatch detection the worker creates persisted `GoldenDemoCustomsFileCorrectiveWork`:

- Title: correct Packing List weight data
- Linked blocker, evidence, document reference
- Completing work creates **Packing List v2** with corrected net weight aligned to shipment
- **Blocker remains open** until explicit evidence re-check (`RecheckEvidenceAsync`)

Rule enforced: **WORK COMPLETED ≠ OUTCOME ACHIEVED**.

## Document Versioning

- Packing List v1: `IsCurrent=false` after correction, status preserved as invalid evidence
- Packing List v2: current valid version referenced by manifest and verification
- Commercial Invoice v1 remains current valid throughout

## Approval

`CUSTOMS PACKAGE APPROVAL` via `ApprovePackageAsync`:

- Permission: `golden-demo-customs-file.approve`
- Reviewer subject persisted on outcome (`ApprovedBySubjectId`, `ApprovedAtUtc`)
- Unauthorized approval rejected by API authorization

## Final Package Assembly

After blockers resolved and approval complete, `AssemblePackageAsync` persists package with manifest referencing current valid documents, requirement results, approval evidence, and shipment summary.

## Verification Rules

`GoldenDemoCustomsFileVerificationEngine` results:

| Result | Meaning |
| --- | --- |
| `verified-ready` | All mandatory evidence, checks, approval, assembly satisfied |
| `not-ready` | Blocking gaps remain |
| `partially-ready` | Partial progress |
| `insufficient-data` | Missing baseline data |

`VERIFIED_READY` requires zero open blockers, consistency pass, approval complete, package assembled, required documents ready — not merely reaching the final workflow step.

## Reset

`ResetPilotOutcomeAsync` clears customs scenario tables for the pilot outcome only (documents, blockers, corrective works, packages, verification runs, activities) and restores outcome seed state. Does not affect production Golden Demo tables or unrelated tenant data.

## Security / RLS

- Permissions: `golden-demo-customs-file.{read,manage,approve,execute,verify,reset}`
- Tenant RLS policies on all customs tables (`ApplyTenantRls` + delete policies for reset)
- Runtime grants to `crevafactos_runtime` via migrations `20260914131802` and `20260914143000`
- Dev auth headers required; no demo bypass

## API

| Method | Route |
| --- | --- |
| GET | `/api/golden-demo-customs-file/workspace` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/assign` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/evaluate` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/corrective-work/{workId}/complete` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/recheck` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/approve` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/assemble` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/verify` |
| POST | `/api/golden-demo-customs-file/outcomes/{id}/reset` |

## UI

- Route: `/golden-demo/gumruk-dosyasi`
- Workspace: worker/outcome, order/shipment, documents, requirements, blockers, corrective work, approval, package, hero, activity trace
- Demo controls drive real API workflow (45–75 second story)

## Tests

`GoldenDemoCustomsFileFoundationTests` (5 tests):

- Permission key stability
- Net weight mismatch detection
- Completed work alone does not clear blocker until recheck
- Verification states (verified-ready / not-ready / insufficient-data)
- Requirement evaluation for configured vs not-required trade documents

## Runtime Verifier

`apps/web/scripts/verify-golden-demo-customs-file-runtime.mjs`

Proves API reachability, route render, worker reuse, mismatch computation, blocker/corrective work, completion-without-clear, recheck resolution, versioning, approval, assembly, verified-ready hero, runtime-derived counts, two full cycles, P1 regression.

Result: **PASS**

## Production Regressions

| Check | Result |
| --- | --- |
| P1 runtime | PASS |
| P2 runtime | PASS |
| P2B runtime | PASS |
| P3 runtime | PASS |
| Sales-ready freeze | PASS |

## Claim Boundaries

**Supported claims:**

- Customs Clearance Package Prepared
- Ready for Customs Broker Handoff

**Not claimed (no integration):**

- Declaration Submitted / Registered
- Customs Cleared / Shipment Cleared
- BİLGE / government portal integration

## Unsupported External Integrations

- Customs declaration submission
- BİLGE
- Government portals
- Customs broker transmission
- Freight booking / logistics ERP

FactOS closes at verified broker handoff readiness.

## Git Closure

Commit: `feat: add customs file digital worker golden outcome`

Includes persistence, migrations, API, web UI, tests, runtime verifier, and this report.
