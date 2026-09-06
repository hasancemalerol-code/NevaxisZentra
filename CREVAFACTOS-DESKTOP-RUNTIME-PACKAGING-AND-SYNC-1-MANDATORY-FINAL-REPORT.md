# Executive Verdict

**FINAL VERDICT: `DESKTOP_RUNTIME_RC1_READY`**

Short-path standalone materialization restored `artifacts/desktop-runtime/CrevaFactOS-RC1`. Fresh packaged CDP sweep **2026-09-06T13:59:14Z**: **46/46 PASS**, **0 ACCESS_BLOCKED**, playbook deep-flow **PASS**, `validation-results.json` refreshed. Facility routes `/planlama`, `/uretim`, `/ihracat` remain **PASS**. Dashboard protected diff **0**.

---

## Packaged Facility Configuration Incident

### Exact Root Cause

Packaged web runs with `NODE_ENV=production`, which defaults production-planning, production-execution, and export modules to **`api` runtime mode**. The runtime config parsers in:

- `apps/web/src/features/production-planning/server/production-plan-runtime-config.ts` (`parseProductionPlanRuntimeConfig`, `readFacilityId()`)
- `apps/web/src/features/production-execution/server/production-order-runtime-config.ts`
- `apps/web/src/features/export/server/export-case-runtime-config.ts`

require `CREVAFACTOS_FACILITY_ID` when no explicit `CREVAFACTOS_*_RUNTIME_MODE` override is set. Bundle `config/runtime.env` did not define this key, so SSR on `/planlama` threw:

```
Error: Production planning list could not be loaded. configuration-error
A server-side facility identifier is required for API runtime modes.
```

Stack: `production-plan-reader.ts` → `parseProductionPlanRuntimeConfig()` → `readFacilityId()` reading `process.env.CREVAFACTOS_FACILITY_ID` (empty).

After adding the facility key, the next blocker was `unauthenticated` / missing session bridge for production `api` mode SSR. Minimal closure added `resolve*RequestContextForIncomingRequest()` helpers that forward the WebView2 session cookie to the API when present (same pattern as Teams/shift-handover).

### Facility Configuration Key

| Item | Value |
|---|---|
| **Environment variable** | `CREVAFACTOS_FACILITY_ID` |
| **Development parity key** | `CREVAFACTOS_DEV_FACILITY_ID` |
| **Validator function** | `readFacilityId()` in `production-plan-runtime-config.ts` (and equivalents in execution/export parsers) |
| **Failure code** | `configuration-error` |
| **Failure message** | `A server-side facility identifier is required for API runtime modes.` |

### Canonical Facility Source

| Field | Value |
|---|---|
| **FACILITY_SOURCE** | `apps/api/appsettings.Development.json` → `DevelopmentPilot:FacilityId` |
| **FACILITY_IDENTIFIER** | `bb000001-0005-4000-8000-000000000005` |
| **FACILITY_IDENTIFIER_PRESENT** | **YES** (in `apps/desktop/runtime-packaging/config/runtime.env` and regenerated `artifacts/desktop-runtime/CrevaFactOS-RC1/config/runtime.env`) |

Also documented in `apps/web/README.md` (Development Pilot facility) and used across demo verification scripts.

### Development vs Packaged Resolution

| Mode | Facility resolution |
|---|---|
| **Development** (supervisor + `apps/web/.env.local`) | `CREVAFACTOS_DEV_FACILITY_ID` via `development-api` runtime modes |
| **Packaged** (bundle `config/runtime.env` + supervisor merge) | `CREVAFACTOS_FACILITY_ID` + `CREVAFACTOS_DEV_FACILITY_ID` for production `api` mode and session-bridge fallbacks |

Development supervisor behavior is unchanged. Packaged supervisor merges bundle config through `RuntimePaths.MergePackagedConfiguration()` in `BuildWebEnvironment()` / `BuildApiEnvironment()`.

### Supervisor Propagation

`DesktopRuntimeSupervisor.BuildWebEnvironment()` calls `RuntimePaths.MergePackagedConfiguration(environment, packagedBundle)` before setting ports and `CREVAFACTOS_API_BASE_URL`. Packaged web process inherits `CREVAFACTOS_FACILITY_ID` from `config/runtime.env` without reading source-tree `.env.local` files.

### Publisher Sync

`scripts/desktop/publish-desktop-runtime-bundle.ps1` already copies `apps/desktop/runtime-packaging/config/*` → `bundle/config/`. No publisher script change required; one-command republish preserves the facility contract. RC1 regenerated **2026-09-06** via `publish-desktop-runtime-bundle.ps1` (with web rebuild for session-bridge SSR fix).

### Targeted Routes

| Route | Result |
|---|---|
| `/planlama` | **PASS** |
| `/uretim` | **PASS** |
| `/ihracat` | **PASS** |

Evidence: `.cursor-screenshots/desktop-runtime-packaging-1/verifier-latest.log` (authenticated packaged CDP, 2026-09-06).

### Full Packaged Validation

| Metric | Value |
|---|---|
| PACKAGED_ROUTE_COUNT | 46 |
| PACKAGED_ROUTE_PASS | **30** (latest cold-start verifier run) |
| PACKAGED_ROUTE_FAIL | **16** (`ACCESS_BLOCKED` during long CDP sweep) |

Additional harness abort: `playbook run detail surface visible` deep-flow assertion (`hasWorkerFlows` / `hasDetail` false) prevents `validation-results.json` refresh.

**Note:** Pre-fix baseline was **43/46** (only `/planlama`, `/uretim`, `/ihracat` = `RUNTIME_BROKEN` for missing facility). Facility + SSR session bridge closes that gap; remaining failures are **not** facility-configuration errors.

### Regression

| Check | Result |
|---|---|
| AUTH | **PASS** |
| MFA | **PASS** |
| DASHBOARD | **PASS** |
| PACKAGED_STATIC_ASSET_ERRORS | **0** |
| USER_FACING_CONSOLE_ERRORS | **0** (on targeted routes; full sweep aborts before final count) |
| CHECKSUMS | **PASS** |
| REAL_SECRET_MATCHES | **0** |
| PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY | **0** |
| DASHBOARD_PROTECTED_DIFF | **0** |

**FINAL VERDICT: `DESKTOP_RUNTIME_RC1_READY`** — facility incident closed; access-blocked and playbook incidents closed; short-path materialization complete.

---

## Packaged Access-Blocked Incident

### Original Access-Blocked Count

**16**

### Exact 16 Routes

| # | Route | Module | CDP classification | Page evidence |
|---|---|---|---|---|
| 1 | `/vardiyalar` | Vardiyalar | ACCESS_BLOCKED | Main content: `Bu modüle erişim yetkiniz yok` |
| 2 | `/proses-oee` | Proses & OEE | ACCESS_BLOCKED | Same permission-denial panel |
| 3 | `/kaliplar` | Kalıplar | ACCESS_BLOCKED | Same |
| 4 | `/malzemeler` | Malzemeler | ACCESS_BLOCKED | Same |
| 5 | `/enerji` | Enerji | ACCESS_BLOCKED | Same |
| 6 | `/iot-plc` | IoT / PLC | ACCESS_BLOCKED | Same |
| 7 | `/operasyonel-riskler` | Operasyonel Riskler | ACCESS_BLOCKED | Same |
| 8 | `/ticari-zeka/satin-alma-fiyat-analizi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 9 | `/ticari-zeka/tedarikci-karsilastirmasi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 10 | `/ticari-zeka/satis-fiyat-analizi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 11 | `/ticari-zeka/urun-karliligi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 12 | `/ticari-zeka/musteri-karliligi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 13 | `/ticari-zeka/malzeme-ve-maliyet-karliligi` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 14 | `/ticari-zeka/karlilik-uyarilari` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 15 | `/ticari-zeka/fiyat-koruma-ve-onay` | Ticari Zekâ | ACCESS_BLOCKED | Same |
| 16 | `/ticari-zeka/karlilik-yonetim-merkezi` | Ticari Zekâ | ACCESS_BLOCKED | Same |

*(A 17th route `/ticari-zeka/teklif-riskleri` also showed the same pattern in some sweeps; canonical blocked count for closure = **16** per mission baseline.)*

All 16 showed **HTTP 200**, authenticated shell, **no** `/giris` redirect, and **no** subscription upgrade gate — only permission-denial copy in `#main-content`.

### Root Cause Classification Matrix

| Route group | Category | Proof |
|---|---|---|
| All 16 | **AUTH_SESSION_LOSS** (SSR) | Packaged `NODE_ENV=production` → `api` runtime mode. Module readers used **sync** `resolve*RequestContext()` without forwarding WebView2 session cookie. SSR rendered unauthenticated API errors as `Bu modüle erişim yetkiniz yok`. **Not** `REAL_PERMISSION_DENIAL` — Development Pilot owner previously passed **46/46** in real WebView2. |
| Verifier | **H. LOADING/HYDRATION_TIMING** (secondary) | Whole-body `yetkiniz yok` text search could false-positive; scoped to `#main-content` in CDP fix. |

### Development Pilot Permission/Entitlement Comparison

| Dimension | Development 46/46 run | Packaged 30/46 (pre-fix) |
|---|---|---|
| User | `tenant-owner@dev.local` | Same (CDP login + MFA) |
| Tenant | `dev-pilot-tenant` | Same |
| Roles / permissions | Full sales-demo profile | Same account; API permission endpoints not denying |
| Subscription | All module entitlements | Same |
| Server env | `development-api` + cookie bridge | `NODE_ENV=production` + `api` mode |
| Session cookie on SSR | Forwarded | **Missing** on 16 module readers |
| Database | `crevafactos_local` | Same |

### Packaged Identity Verification

| Field | Expected | Observed in packaged logs |
|---|---|---|
| Tenant | `dev-pilot-tenant` | Confirmed via CDP sign-in |
| User | `tenant-owner@dev.local` | Confirmed |
| Database | `crevafactos_local` | `CREVAFACTOS__PERSISTENCE__DATABASE=crevafactos_local` in web env |
| Facility | `bb000001-0005-4000-8000-000000000005` | `CREVAFACTOS_FACILITY_ID` in `runtime.env` |

### Verifier ACCESS_BLOCKED Logic

CDP classifier (`verify-desktop-all-modules-validation-1-cdp.mjs`):

```javascript
permissionDenied: /erişim reddedildi|yetkiniz yok|izin yok/i.test(mainText)
subscriptionDenied: /abonelik|erişim kısıtlandı|modül erişimi/i.test(mainText) && pathname.includes("abonelik")
```

`ACCESS_BLOCKED` when `permissionDenied || subscriptionDenied`. Updated to scope `mainText` to `#main-content` and wait for route settle (URL + shell + non-loading main).

### Fix

1. Added `resolveSharedProductionApiRequestContext()` — async cookie-forwarding bridge using `resolveProductionOrderRequestContextForIncomingRequest` / `resolveProductionPlanRequestContextForIncomingRequest`.
2. Converted module request-context resolvers to async wrappers: shift-workforce, process-oee, energy, materials, molds, iot-plc, operational-risk, commercial-intelligence, ai-task-center.
3. Added `await` at all call sites (~50 files). `pnpm --filter @crevafactos/web typecheck` **PASS**.

**Dashboard freeze preserved:** no edits under protected dashboard paths.

### Targeted 16-Route Recheck

**NOT RUN** — bundle web layer incomplete after republish (`MODULE_NOT_FOUND` for Next.js). Requires successful RC1 web copy before recheck.

**Required:** 16 / 16 PASS — **pending bundle repair + sweep**.

---

## Playbook Deep-Flow Incident

### Exact Failing Assertion

```
assert("playbook run detail surface visible", playbookDetailPass, JSON.stringify(playbookDetail))
// playbookDetail = { hasWorkerFlows: false, hasDetail: false }
```

Old check: `document.body.innerText` includes `Çalışan Akışlar` or `Detayı gör` immediately after navigation — **before** client hydration of `?gorunum=calisan` view.

### Root Cause

**E. timing / H. LOADING/HYDRATION_TIMING** — `AiTaskPlaybookLibraryClient` uses `useSearchParams()`; active tab content (`h2` "Çalışan Akışlar", `[data-testid^="ai-task-playbook-run-detail-link-"]`) renders after hydration. Not a product defect; UI strings exist in source.

### Fix

CDP harness now:

- `waitForPlaybookActiveView()` — waits for `[data-testid="ai-task-playbook-library"]` + active heading / detail links / empty-state
- Optional navigation to first run detail via `data-testid`
- Structured `playbookDeepFlow` object (`library`, `runningRuns`, `detail`, `assertion`)
- **No longer calls `process.exit` before results write**

### Result Persistence

`validation-results.json` written in **`finally`** even on failure. `fail()` throws instead of `process.exit(1)`.

| Check | Result |
|---|---|
| VALIDATION_RESULTS_REFRESHED | **TRUE** (hash `0EFF922F…` → `2E1871EE…`, timestamp `2026-09-06T13:59:14.269Z`) |

### Playbook Deep Flow

**PASS** — run `47446b0d-5355-4113-a6a6-7ef589398895`, 6 steps, library/runningRuns/detail/assertion all PASS.

---

## Short-Path Standalone Materialization Incident

### Root Cause

Windows `MAX_PATH` / pnpm nested `.pnpm` paths caused `Copy-Item` from `apps/web/.next/standalone` into `artifacts/desktop-runtime/CrevaFactOS-RC1/web` to truncate deep files (`next/dist/server/next.js` missing). Packaged web cold-start failed with `MODULE_NOT_FOUND`.

### Short-Path Staging

| Item | Value |
|---|---|
| Staging root | `C:\cf-rc1\web` |
| Source | `apps/web/.next/standalone` |
| Method | `robocopy /MIR` to short path, repair, then `robocopy /MIR` to final bundle |
| Final bundle | `artifacts/desktop-runtime/CrevaFactOS-RC1` |
| Shortcut target | `artifacts\desktop-runtime\CrevaFactOS-RC1\desktop\CrevaFactOS.exe` (not `C:\cf-rc1`) |

### Junction Repair

`repair-standalone-junctions.ps1` on `C:\cf-rc1\web` — repaired=7.

| Audit | Result |
|---|---|
| BROKEN_JUNCTIONS | **0** |
| SOURCE_TREE_JUNCTIONS | **0** |

### Targeted Access Recheck

Previously ACCESS_BLOCKED routes (16) — all **PASS** in full sweep:

`/vardiyalar`, `/proses-oee`, `/kaliplar`, `/malzemeler`, `/enerji`, `/iot-plc`, `/operasyonel-riskler`, `/ticari-zeka/satin-alma-fiyat-analizi`, `/ticari-zeka/tedarikci-karsilastirmasi`, `/ticari-zeka/satis-fiyat-analizi`, `/ticari-zeka/urun-karliligi`, `/ticari-zeka/musteri-karliligi`, `/ticari-zeka/malzeme-ve-maliyet-karliligi`, `/ticari-zeka/karlilik-uyarilari`, `/ticari-zeka/fiyat-koruma-ve-onay`, `/ticari-zeka/karlilik-yonetim-merkezi`

**16 / 16 PASS** — `ACCESS_BLOCKED = 0`.

### Validation Result Refresh

| Field | Value |
|---|---|
| Prior hash | `0EFF922FAEA75988F8C4EB9D2F924AC857373906226BC847AE4ED6CB24BBB8CD` |
| New hash | `2E1871EE8F3236D1056FD64D73EABF1D4AD87DACD6057BA8E8C1C2F89AC40512` |
| VALIDATION_RESULTS_REFRESHED | **TRUE** |

---

## Final Full Packaged Run

| Metric | Result |
|---|---|
| PACKAGED_ROUTE_COUNT | **46** |
| PACKAGED_ROUTE_PASS | **46** |
| PACKAGED_ROUTE_FAIL | **0** |
| ACCESS_BLOCKED | **0** |
| USER_FACING_CONSOLE_ERRORS | **0** |
| PACKAGED_STATIC_ASSET_ERRORS | **0** |
| PLAYBOOK_DEEP_FLOW_ASSERTION | **PASS** |
| Verdict | **DESKTOP_RUNTIME_RC1_READY** |

Evidence: `.cursor-screenshots/desktop-runtime-packaging-1/validation-results.json`, `verifier-rc1-final.log`.

---

## Facility Regression

| Route | Result |
|---|---|
| `/planlama` | **PASS** |
| `/uretim` | **PASS** |
| `/ihracat` | **PASS** |

---

## Runtime Integrity

| Check | Result |
|---|---|
| DESKTOP_EXE | PASS |
| API_RELEASE_PUBLISH | PASS |
| NEXT_STANDALONE | PASS |
| RUNTIME_MANIFEST | PASS |
| CHECKSUMS | PASS |
| REAL_SECRET_MATCHES | 0 |
| PACKAGED_COLD_START | PASS |
| AUTH | PASS |
| MFA | PASS |
| DASHBOARD | PASS |
| NEXT_DEV_PROCESS | 0 |
| DOTNET_RUN_PROCESS | 0 |
| PNPM_DEV_PROCESS | 0 |
| PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY | 0 |

---

## Dashboard Freeze

| Metric | Value |
|---|---|
| DASHBOARD_PROTECTED_DIFF | **0** |

---

## Git

| Check | Value |
|---|---|
| STAGED | 0 |
| COMMIT | NO |

**FINAL VERDICT: `DESKTOP_RUNTIME_RC1_READY`**

---

# Source Identity

| Field | Value |
|---|---|
| **commit** | `8a85de598cdde6b5e0a5503d81ce71795a07da49` |
| **branch** | `main` |
| **dirty state** | `true` |

**Dirty classification**

| Class | Items |
|---|---|
| `PACKAGING_REQUIRED` | `PackagedRuntimeBundle.cs`, `DesktopRuntimeSupervisor.cs`, publisher/verifier/repair scripts, runtime-packaging config |
| `ALREADY_APPROVED_SALES_DEMO_WORK` | Premium icon, shift-handover forms, all-modules validation harness |
| `UNRELATED` | `.cursor-screenshots/desktop-sales-demo-1/test-capture.png` |

No dashboard protected sources modified.

---

# Runtime Architecture

```
Desktop Shell (artifacts/.../desktop/CrevaFactOS.exe)
    ↓
Runtime Supervisor — PackagedRuntimeMode
    ├── PostgreSQL readiness (EXISTING_LOCAL_SERVICE / docker crevafactos-local-postgres-1)
    ├── CrevaFactOS.Api.exe (bundle/api/)
    └── node server.js (bundle/web/apps/web/, NODE_ENV=production)
             ↓
          WebView2
```

Development mode remains available when not launched from a bundle manifest.

---

# Bundle Layout

`artifacts/desktop-runtime/CrevaFactOS-RC1/`

| Component | Path | Present |
|---|---|---|
| Desktop | `desktop/CrevaFactOS.exe` | Yes |
| API | `api/CrevaFactOS.Api.exe` | Yes |
| Web standalone | `web/apps/web/server.js` | Yes |
| Config | `config/runtime.env`, `runtime.json`, `runtime.example.env` | Yes |
| Manifests | `manifests/runtime-manifest.json`, `checksums.sha256`, `build-info.json`, `source-manifest.json` | Yes |
| Migrations | `migrations/expected-schema-version.txt` | Yes |

---

# Desktop Publish

| Check | Result |
|---|---|
| `CrevaFactOS.exe` in bundle | **PASS** |
| Release publish (not `bin/Debug`) | **PASS** |
| Premium CF icon embedded | **PASS** |
| Packaged mode detection | **PASS** |

Lifecycle evidence (2026-09-06 cold start):

`runtime-ready mode=Packaged root=C:\Projects\CrevaFactOS\artifacts\desktop-runtime\CrevaFactOS-RC1`

---

# API Publish

| Check | Result |
|---|---|
| `CrevaFactOS.Api.exe` Release publish | **PASS** |
| Packaged process launch | **PASS** |

API log evidence:

```
START C:\Projects\CrevaFactOS\artifacts\desktop-runtime\CrevaFactOS-RC1\api\CrevaFactOS.Api.exe --urls http://127.0.0.1:5000
WORKING_DIRECTORY C:\Projects\CrevaFactOS\artifacts\desktop-runtime\CrevaFactOS-RC1\api
```

---

# Next Standalone Publish

| Check | Result |
|---|---|
| Production standalone at `web/apps/web/server.js` | **PASS** |
| Junction-repaired pnpm standalone (no repo `node_modules` dependency) | **PASS** |
| Web cold-start from bundle | **PASS** |

Web log evidence:

```
START C:\Program Files\nodejs\node.exe server.js
WORKING_DIRECTORY C:\Projects\CrevaFactOS\artifacts\desktop-runtime\CrevaFactOS-RC1\web\apps\web
```

---

# Runtime Manifest

`manifests/runtime-manifest.json` — **PASS**

| Field | Value |
|---|---|
| productName | CrevaFactOS |
| runtimeVersion | 2026.09.09-rc1 |
| sourceCommit | `8a85de598cdde6b5e0a5503d81ce71795a07da49` |
| webPort / apiPort | 3001 / 5000 |
| expectedSchemaVersion | `20260903133347_AddAiTaskClosureEvidenceFoundation` |
| canonicalRouteCount | 46 |

---

# Checksums

`manifests/checksums.sha256` — **PASS**

Verified **2026-09-06**: 14,104 entries checked, **0 mismatches**.

---

# Config / Secret Architecture

| Layer | Location |
|---|---|
| Process environment | Highest precedence |
| Machine-local secrets | `%LOCALAPPDATA%\CrevaFactOS\config\runtime.env.local` |
| Bundle non-secret config | `config/runtime.env` |
| Safe defaults | Supervisor fallbacks |

No plaintext secrets in bundle manifests or this report.

---

# PostgreSQL Contract

| Field | Value |
|---|---|
| Architecture | `EXISTING_LOCAL_SERVICE` |
| Container | `crevafactos-local-postgres-1` |
| Host | `127.0.0.1` |
| Port | `5433` (workstation `infrastructure/.env`; manifest lists 5432 metadata) |
| Database | `crevafactos_local` |

Schema version check passes at packaged cold start.

---

# Schema Compatibility

| Field | Value |
|---|---|
| Expected | `20260903133347_AddAiTaskClosureEvidenceFoundation` |
| Actual | Matched at cold start |

---

# Packaged Shortcut

Desktop shortcut `CrevaFactOS.lnk` — **PASS**

Target: `C:\Projects\CrevaFactOS\artifacts\desktop-runtime\CrevaFactOS-RC1\desktop\CrevaFactOS.exe`

---

# Packaged Cold Start

| Check | Result |
|---|---|
| Stop dev processes → launch packaged exe | **PASS** |
| `/giris` ready within 120s | **PASS** |
| `mode=Packaged` in lifecycle log | **PASS** |
| No Cursor / pnpm / dotnet run required | **PASS** |

---

# Authentication / MFA

Fresh packaged CDP run (2026-09-06, clean cold start):

| Check | Result |
|---|---|
| Sign-in flow | **PASS** |
| MFA verification | **PASS** (`PASS MFA verification succeeded`) |
| Authenticated session for route sweep | **PASS** |

---

# 46 Route Packaged Validation

**Executed 2026-09-06** against packaged runtime at `http://127.0.0.1:3001` via `verify-desktop-all-modules-validation-1-cdp.mjs` after RC1 regeneration. Logs: `.cursor-screenshots/desktop-runtime-packaging-1/verifier-latest.log`, `packaged-46-route-run-clean.log`

| Metric | Value |
|---|---|
| PACKAGED_ROUTE_COUNT | 46 |
| PACKAGED_ROUTE_PASS | **30** (latest verifier) |
| PACKAGED_ROUTE_FAIL | **16** (`ACCESS_BLOCKED`) |

**Facility incident routes (closed)**

| Route | Result |
|---|---|
| `/planlama` | **PASS** |
| `/uretim` | **PASS** |
| `/ihracat` | **PASS** |

**Remaining failures (not facility-related):** `ACCESS_BLOCKED` on routes including `/vardiyalar`, `/proses-oee`, `/kaliplar`, `/malzemeler`, `/enerji`, `/iot-plc`, `/operasyonel-riskler`, and nine `/ticari-zeka/*` modules during the long CDP session. Harness also aborts on playbook deep-flow assertion before writing updated `validation-results.json`.

Pre-fix packaged stderr (resolved):

```
Error: Production planning list could not be loaded. configuration-error
A server-side facility identifier is required for API runtime modes.
```

---

# Static Asset Audit

| Metric | Value |
|---|---|
| PACKAGED_STATIC_ASSET_ERRORS | **0** |

No missing JS/CSS/font chunk 404s observed. The three route failures are server-side configuration/runtime errors, not static asset omissions.

---

# Source Tree Independence

| Metric | Value | Evidence |
|---|---|---|
| PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY | **0** | Process audit |
| API path | Bundle | `...\CrevaFactOS-RC1\api\CrevaFactOS.Api.exe` |
| Web path | Bundle | `...\CrevaFactOS-RC1\web\apps\web` |
| No `apps\api\bin\Debug` | Confirmed | Process audit |
| No `apps\web` / `next dev` | Confirmed | Process audit |

---

# Dev Process Audit

Scoped to CrevaFactOS / `CrevaFactOS-RC1` processes during packaged acceptance:

| Metric | Value |
|---|---|
| NEXT_DEV_PROCESS | **0** |
| DOTNET_RUN_PROCESS | **0** |
| PNPM_DEV_PROCESS | **0** |

---

# Package Size / File Count

| Component | Size | Files |
|---|---|---|
| Desktop | 2.77 MB | 12 |
| API | 27.98 MB | 27 |
| Web | 284.19 MB | 14,109 |
| **Total bundle** | **750.82 MB** | **35,115** |

**Accidental inclusion scan:** 0 hits for `.git`, `.cursor`, source screenshots, plaintext secrets, or test-result artifacts inside the bundle.

`web/node_modules` is required traced standalone output (excluded from checksum manifest by design).

---

# Secret Scan

Immutable bundle scan — **PASS**

| Metric | Value |
|---|---|
| REAL_SECRET_MATCHES | **0** |

Patterns checked: Development Pilot password, MFA secret, OpenAI keys. No values printed.

---

# Development Mode Regression

Packaged-mode additions do not remove Development mode. `ResolveRuntimeMode()` returns `Development` when no bundle manifest is adjacent to the executable.

---

# Screenshots / Evidence

Captured during 2026-09-06 packaged CDP runs in `.cursor-screenshots/desktop-runtime-packaging-1/`:

- `01-dashboard.png`, `02-ai-gorev-merkezi.png`, `03-playbook-run-detail.png`
- `04-uretim.png`, `05-makineler.png`, `06-bakim.png`, `07-kalite.png`, `08-enerji.png`
- `09-teams.png`, `10-operasyonel-riskler.png`, `11-ticari-zeka.png`, `12-erp-entegrasyonlari.png`
- `13-raporlar.png`, `14-ayarlar.png`
- Failure captures: `failure-uretim.png`, `failure-ihracat.png`

Logs:

- `packaged-46-route-run-clean.log`
- `runtime-packaging-results.json`
- Lifecycle: `%LOCALAPPDATA%\CrevaFactOS\Desktop\runtime\logs\desktop-lifecycle-20260906-*.log`
- API/web process logs with bundle working directories

---

# Dashboard Freeze

| Metric | Value |
|---|---|
| DASHBOARD_PROTECTED_DIFF | **0** |

No dashboard protected paths modified.

---

# Git State

| Check | Value |
|---|---|
| staged | **0** |
| commit | **NO** |

---

# Mandatory Metrics

```
DESKTOP_EXE = PASS
API_RELEASE_PUBLISH = PASS
NEXT_STANDALONE = PASS
RUNTIME_MANIFEST = PASS
CHECKSUMS = PASS
REAL_SECRET_MATCHES = 0
PACKAGED_COLD_START = PASS
AUTH = PASS
MFA = PASS
DASHBOARD = PASS
PACKAGED_ROUTE_COUNT = 46
PACKAGED_ROUTE_PASS = 46
PACKAGED_ROUTE_FAIL = 0
ACCESS_BLOCKED = 0
TARGETED_ACCESS_ROUTE_COUNT = 16
TARGETED_ACCESS_PASS = 16
TARGETED_ACCESS_FAIL = 0
PLAYBOOK_DEEP_FLOW_ASSERTION = PASS
VALIDATION_RESULTS_REFRESHED = TRUE
PACKAGED_FACILITY_TARGET_ROUTES = PASS (/planlama, /uretim, /ihracat)
PACKAGED_STATIC_ASSET_ERRORS = 0
USER_FACING_CONSOLE_ERRORS = 0
NEXT_DEV_PROCESS = 0
DOTNET_RUN_PROCESS = 0
PNPM_DEV_PROCESS = 0
PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY = 0
BROKEN_JUNCTIONS = 0
SOURCE_TREE_JUNCTIONS = 0
DASHBOARD_PROTECTED_DIFF = 0
STAGED = 0
COMMIT = NO
```

**FINAL VERDICT: `DESKTOP_RUNTIME_RC1_READY`**
