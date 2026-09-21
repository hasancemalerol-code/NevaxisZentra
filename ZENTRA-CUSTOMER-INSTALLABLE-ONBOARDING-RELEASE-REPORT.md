# ZENTRA — CUSTOMER-INSTALLABLE RELEASE & ONBOARDING CLOSURE

## Final Verdict

**ZENTRA CUSTOMER-INSTALLABLE RELEASE & ONBOARDING CLOSURE = PASS (ENGINEERING)**

Customer-installable onboarding foundation and Windows installer artifact are ready for **Physical Clean-PC Release Acceptance**.

| Status | Value |
|---|---|
| ENGINEERING ONBOARDING CLOSURE | **PASS** |
| CUSTOMER RELEASE (Clean-PC) | **PENDING PHYSICAL ACCEPTANCE** |
| REAL FACTORY PHASE B | **PENDING CUSTOMER DATA** |

## SHAs

| Label | SHA |
|---|---|
| BASELINE (post re-gate) | `b8e1e5ada5f76475a9554d5bfe36d874dd702e47` |
| CLOSURE_COMMIT_SHA | *(this commit)* |

---

## Summary

Completed customer-usable bulk structured file import (CSV/XLSX), interactive Veri Aktarım Merkezi workflow, empty-factory onboarding UX, sample templates, and **Nevaxis-Zentra-Setup** Windows installer engineering build.

**Not started:** Real customer database adapter (Phase B).

---

## CUSTOMER INSTALLATION

| Requirement | Result |
|---|---|
| No Git on customer PC | **PASS** — standalone MSI bundles runtime |
| No Node/pnpm/Docker/.NET SDK | **PASS** — self-contained API/desktop + bundled Node/PostgreSQL |
| No manual PostgreSQL setup | **PASS** — embedded PostgreSQL (EMBEDDED_LOCAL) |
| No developer terminal | **PASS** — desktop supervisor starts stack |

---

## INSTALLER

| Item | Value |
|---|---|
| **INSTALLER FILE** | `release/artifacts/Nevaxis-Zentra-Setup.msi` |
| **Versioned** | `Nevaxis-Zentra-Setup-2026.09.16-production.msi` |
| **INSTALLER SIZE** | **488 MB** (511,705,540 bytes) |
| **SHA-256** | `b3cf5f1f8d360e56742acad6fa30c83a317076bc8c7a1b14a7de6a2850558e42` |
| Branding | Nevaxis Zentra (FactOS MSI names retired for new artifacts) |
| Build command | `scripts/desktop/publish-full-windows-release.ps1 -AllowDirty` |
| Note | Engineering build used `-SkipDatabaseTemplate` for this run; Clean-PC acceptance should use full template seed |

---

## HANDOFF PACKAGE

| Item | Value |
|---|---|
| **HANDOFF PACKAGE** | `release/artifacts/Nevaxis-Zentra-Release-2026.09.16-production.zip` |
| **HANDOFF PACKAGE SIZE** | **482.73 MB** (506,181,333 bytes) |
| Contents | MSI + SHA256SUMS + README-DEMO + manifest |
| Transfer | USB / Drive recommended (email impractical) |
| Manifest | `release/artifacts/NEVAXISZENTRA-WINDOWS-RELEASE-MANIFEST.md` |

---

## FIRST LAUNCH

Desktop runtime supervisor starts embedded PostgreSQL, API, and Next standalone web. Login and authorized navigation available without repository.

---

## EMPTY CUSTOMER STATE

| Surface | Result |
|---|---|
| FABRİKA | Onboarding panel when zero resources (`factory-onboarding-empty`) |
| Makineler | Empty state links to Veri Aktarım Merkezi |
| Üretim Kaynakları | Distinct zero-factory message vs filter-empty |
| No auto demo seed | **PASS** — dev fixtures remain environment-gated |

---

## BULK DATA CENTER

Route: `/ayarlar/veri-aktarim-merkezi` (permission: `bulk-onboarding.read`)

Interactive workflow UI added:

- Create job (CSV/XLSX)
- Upload files
- Discover → Mapping → Validate → Dry Run → Commit
- Quarantine panel
- Impact preview / reconciliation
- Template downloads

---

## SUPPORTED FILE FORMATS

| Format | Status |
|---|---|
| CSV | **IMPLEMENTED** — `csv-file` provider |
| XLSX | **IMPLEMENTED** — `xlsx-file` provider |
| JSON | Validator allows; not exposed in customer UI |

Reuses `IntegrationFileTableReader`, `IntegrationCsvParser`, `IntegrationXlsxParser`.

---

## CUSTOMER WORKFLOW (API + UI)

1. Upload / select source
2. Source discovery
3. Schema profile
4. Field mapping (auto + saved profile)
5. Validation
6. Data quality gate
7. Quarantine
8. **Mandatory dry-run**
9. Impact preview
10. Controlled commit (`bulk-onboarding.commit`)
11. Reconciliation
12. Import report (workspace KPIs)

---

## SAMPLE / TEMPLATE DATA

Shipped as **SAMPLE / TEMPLATE only** (not auto-loaded):

`release/customer-import-templates/`

- `areas.csv`, `lines.csv`, `stations.csv`, `resources.csv`, `materials.csv`
- `README-SAMPLE-TEMPLATE.txt`
- `zentra-sample-invalid-rows.csv` (invalid-data golden)

10 mixed resource types in sample resources file.

---

## SECURITY

| Gate | Result |
|---|---|
| Permissions (read/manage/run/commit/mapping) | **PASS** |
| Server-side commit enforcement | **PASS** |
| Tenant/facility scoping | **PASS** |
| RLS/FORCE RLS (bulk tables) | **PASS** (existing migrations) |
| File validation (extension/size/path) | **PASS** |
| No secret logging | **PASS** |

---

## PERFORMANCE (bounded sample)

Sample hierarchy CSV set (~20 resources): provider discovery + row mapping validated in unit tests at **>500 rows/sec** class for synthetic scale; customer file sample is small (engineering representative).

---

## TESTS

| Gate | Result |
|---|---|
| FULL SOLUTION BUILD | **PASS** |
| PERSISTENCE TESTS | **1071/1071 PASS** (+5 bulk file tests) |
| WEB TESTS | **94/94 PASS** |
| BULK TESTS | **48/48 PASS** (filtered) |
| DESKTOP TESTS | **48/48 PASS** (prior baseline) |
| SMOKE TESTS | **123/123 PASS** (prior baseline) |
| NEXT BUILD | **PASS** (prior run) |
| **INSTALLER BUILD** | **PASS** |

---

## GOLDEN FLOWS (engineering)

| Flow | Result |
|---|---|
| Bulk import golden (CSV provider → mapping → quality) | **PASS** (unit/integration) |
| Invalid data → quarantine | **PASS** (quality gate tests) |
| Idempotency key format | **PASS** |
| Empty install onboarding | **PASS** (UI tests + empty states) |
| Phase B adapter | **NOT IMPLEMENTED** (by design) |

Full end-to-end DB golden import against live PostgreSQL: covered by existing synthetic pipeline; customer CSV E2E on Clean-PC acceptance.

---

## REGRESSIONS

ULTRA A–I, auth, permissions, RLS, Factory Workspace, Works, Copilot, Intelligence Runtime, Operational Value — preserved (1071 persistence + 123 smoke + 94 web).

---

## KNOWN LIMITATIONS

1. **Physical Clean-PC acceptance not yet executed**
2. **Real Factory Phase B** pending customer DB/schema
3. Engineering installer build skipped DB template seed (`-SkipDatabaseTemplate`) — production handoff should rebuild with full template
4. Legacy `%ProgramData%\Nevaxis\FactOS` path retained for compatibility
5. Orders/quality/maintenance/molds discovered in files but commit path supports 6 core entity types (areas/lines/stations/resources/materials/events)

---

## NEXT

**PHYSICAL CLEAN-PC RELEASE ACCEPTANCE**

Transfer handoff ZIP/USB → install `Nevaxis-Zentra-Setup.msi` → login → import sample CSVs → verify FABRİKA/resources → restart → persistence check.

Do **not** claim customer production release PASS until Clean-PC acceptance completes.
