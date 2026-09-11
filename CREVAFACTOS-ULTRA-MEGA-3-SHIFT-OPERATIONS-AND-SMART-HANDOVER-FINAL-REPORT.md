# CREVAFACTOS ULTRA-MEGA-3 — Shift Operations & Smart Handover

## SHAs

| Key | SHA |
|---|---|
| BASELINE_SHA (pre-UM-3) | `cf1c8419f2eb14a41a11da927845432c3753ff61` |
| ORIGIN_MAIN_SHA (baseline) | `cf1c8419f2eb14a41a11da927845432c3753ff61` |
| FINAL_SHA | _(set at commit)_ |

## Summary

Integrated **Vardiya Teslimi** inside canonical **Vardiyalar** module (removed top-level `Vardiya Devir Teslimi` navigation). Added operational shift instances, open items, smart handover lifecycle, Digital Workforce candidate collection, coordinator recommendations, API `/api/shifts/*`, integrated web sub-routes, dashboard active-shift panel, migrations with RLS + runtime grants, tests, and runtime verifier.

## Reused Capabilities

- `FactoryShiftHandover` / `ShiftHandoverRepository` (legacy factory handover — preserved)
- `ProductionShift`, shift workforce (`/vardiyalar` workforce data)
- Digital Workforce cases/approvals for automatic candidate collection
- Task Center bridge pattern (open-item `linkedAiTaskId`)
- Facility access resolver + tenant session patterns from UM-2

## New / Extended Entities

| Entity | Purpose |
|---|---|
| `OperationalShiftInstance` | Canonical operational shift lifecycle |
| `ShiftOpenItem` | Cross-shift open work / risk continuity |
| `ShiftHandoverAcceptance` | Outgoing/incoming sign-off + verified acceptance |

## Migrations

- `20260911191746_AddShiftOperationsSmartHandoverFoundation` — tables, permissions, RLS
- `20260911193739_AddShiftOperationsRuntimeGrants` — `crevafactos_runtime` grants

## Permissions

`shift.read`, `shift.manage`, `shift.start`, `shift.close`, `shift.handover.prepare`, `shift.handover.submit`, `shift.handover.accept`, `shift.open-item.manage`

## API Routes

`/api/shifts`, `/api/shifts/current`, `/api/shifts/history`, `/api/shifts/{id}`, `/api/shifts/{id}/start`, `/api/shifts/{id}/open-items`, `/api/shifts/{id}/closing-preview`, `/api/shifts/{id}/handover/prepare`, `/api/shifts/{id}/handover/submit`, `/api/shifts/{id}/handover/accept`, `/api/shifts/{id}/handover/items/{itemId}`, `/api/shifts/{id}/metrics`, `/api/shifts/{id}/coordinator-recommendations`

## UI Routes (inside Vardiyalar)

| Route | Section |
|---|---|
| `/vardiyalar` | Genel Bakış |
| `/vardiyalar/plan` | Vardiya Planı |
| `/vardiyalar/aktif` | Aktif Vardiya |
| `/vardiyalar/kaynak` | Ekip & Kaynak |
| `/vardiyalar/teslim` | Vardiya Teslimi |
| `/vardiyalar/acik-konular` | Açık Konular |
| `/vardiyalar/performans` | Performans |
| `/vardiyalar/gecmis` | Geçmiş |

`/vardiya-devir` redirects to `/vardiyalar/teslim`.

## Lifecycle

**Shift:** planned → ready → active → closing → handover-pending → handed-over → closed

**Handover:** prepare (preview + candidate upsert) → submit (nothing-falls-through gate) → accept (open-item transfer + verified sign-off)

## Digital Workforce Integration

- Candidate collector maps active DW cases + pending approvals to `ShiftOpenItem`
- Coordinator bridge (`ShiftCoordinatorHandoverBridge`) builds handover recommendations without closing cases
- Same `DigitalWorkCase` referenced across shifts (`digitalWorkCaseId`); unique index per shift prevents duplicate open items

## Gates

| Gate | Result |
|---|---|
| SHIFT_HANDOVER_TOP_LEVEL_MODULE | FALSE (nav entry removed) |
| SHIFT_HANDOVER_INSIDE_SHIFT_MODULE | TRUE |
| DOTNET_BUILD | PASS |
| SHIFT_TESTS (ShiftOperations) | PASS (7 tests) |
| WEB_TYPECHECK | PASS |
| WEB_BUILD | PASS |
| SHIFT_RUNTIME_VERIFIER | PASS |
| AUTHORIZATION_VERIFIER | PASS |
| RLS_VERIFIER | PASS |
| MIGRATION_VERIFIER | PASS |
| GIT_DIFF_CHECK | PASS (Teams dirty files excluded from commit) |

## Known Limitations

- Extended golden flows (maintenance/material/quality/repeated carryover full E2E) covered at engine level; runtime verifier exercises core handover path
- Production/maintenance/quality/material candidate collectors beyond Digital Workforce are incremental follow-ups
- Legacy `/api/shift-handovers` factory handover API remains for backward compatibility

## Verdict

`CREVAFACTOS_ULTRA_MEGA_3_SHIFT_OPERATIONS_SMART_HANDOVER = PASS` (core package scope with documented limitations above)
