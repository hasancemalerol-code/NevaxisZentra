# Zentra Technical Rename Inventory

**Package:** ZENTRA RENAME (post ULTRA C)  
**Baseline:** `d0cf1b5cb2f165641bf71ba0660e9ae9204d953e`  
**Date:** 2026-09-19

## Canonical Naming

| Role | Value |
|------|-------|
| Product display | Nevaxis Zentra |
| Short product | Zentra |
| Technical identifier | NevaxisZentra |
| .NET namespace | NevaxisZentra.* |
| Solution | NevaxisZentra.sln |
| Repository | NevaxisZentra |
| Environment prefix (canonical) | NEVAXIS_ZENTRA_ |
| Installer | Nevaxis Zentra |
| Release artifact prefix | Nevaxis-Zentra- |
| Descriptor | Production Intelligence & Autonomous Operations |

## Classification Legend

| Category | Rename? | Notes |
|----------|---------|-------|
| ACTIVE PRODUCT IDENTITY | Yes | UI, titles, About, installer display |
| ACTIVE TECHNICAL IDENTITY | Yes | Solution, projects, namespaces, scripts |
| BACKWARD COMPATIBILITY | Dual-read / alias | Legacy env, cookies, ProgramData paths |
| HISTORICAL MIGRATION | No (IDs) | MigrationId strings unchanged |
| IMMUTABLE EVIDENCE | No | Root `CREVAFACTOS-*` / `NEVAXISZENTRA-*` closure reports |
| LEGACY DOCUMENTATION | Annotate only | Historical PASS reports retain FactOS name |
| CUSTOMER DATA CONTRACT | Stable + alias | `crevafactos` target-system keys, cookie fallback |
| GENERATED ARTIFACT | Exclude | bin/, obj/, .next/, artifacts/, .visual-qa/ |

## Active Technical Identity (RENAME)

| Area | Occurrence | Target | Reason |
|------|------------|--------|--------|
| Solution | `NevaxisZentra.sln` | `NevaxisZentra.sln` | Canonical solution name |
| Projects | `NevaxisZentra.*.csproj` | `NevaxisZentra.*.csproj` | Assembly/project identity |
| Namespaces | `namespace NevaxisZentra.*` | `NevaxisZentra.*` | Active code identity |
| DI extensions | `AddNevaxisZentra*` | `AddNevaxisZentra*` | Service registration |
| DbContext | `NevaxisZentraDbContext` | `NevaxisZentraDbContext` | Active ORM entry |
| npm root | `nevaxiszentra` | `nevaxiszentra` | Workspace package name |
| Web package | `@nevaxiszentra/web` | `@nevaxiszentra/web` | Workspace filter |
| API title | `NevaxisZentra API` | `Nevaxis Zentra API` | OpenAPI/product identity |
| Docker compose name | `NevaxisZentra-local` | `NevaxisZentra-local` | Dev infrastructure label |
| Desktop window title | `FactOS` | `Zentra` | Active shell branding |
| Web brand module | `factos-brand.ts` | `zentra-brand.ts` | Brand asset module |
| Release MSI | `Nevaxis-FactOS-Setup` | `Nevaxis-Zentra-Setup` | New artifacts |
| Diagnostics zip | `Nevaxis-FactOS-Diagnostics` | `Nevaxis-Zentra-Diagnostics` | Support bundle |

## Backward Compatibility (DUAL-READ / RETAIN)

| Occurrence | Category | Policy |
|------------|----------|--------|
| `NEVAXIS_FACTOS_*` env vars | BACKWARD COMPATIBILITY | Read after `NEVAXIS_ZENTRA_*`; never merge conflicts |
| `CREVAFACTOS_*` env vars | BACKWARD COMPATIBILITY | Third-tier fallback |
| `crevafactos_local` DB default in existing installs | BACKWARD COMPATIBILITY | Connection strings unchanged; new default `nevaxis_zentra` |
| `crevafactos` target-system enum key | CUSTOMER DATA CONTRACT | Key stable; display label → Zentra |
| `NevaxisZentra.session` cookie | BACKWARD COMPATIBILITY | Accept on read; issue `NevaxisZentra.session` |
| WiX `UpgradeCode` | BACKWARD COMPATIBILITY | Unchanged for in-place MSI upgrade |
| Registry `Software\NevaxisZentra` | BACKWARD COMPATIBILITY | Retained for upgrade detection |
| `%ProgramData%\Nevaxis\FactOS` | BACKWARD COMPATIBILITY | Retained as primary data path (Strategy A) |
| `%LOCALAPPDATA%\Nevaxis\FactOS` | BACKWARD COMPATIBILITY | Retained; also probe `%LOCALAPPDATA%\Nevaxis\Zentra` |
| Docker volume `NevaxisZentra-local-postgres-data` | BACKWARD COMPATIBILITY | Volume name unchanged to preserve local data |
| Process name `crevafactos` (legacy) | BACKWARD COMPATIBILITY | Probe during desktop upgrade scripts |
| REST API routes | CUSTOMER DATA CONTRACT | No URL rename |
| Permission keys (`factory-case.*`, etc.) | CUSTOMER DATA CONTRACT | No rename |

## Historical Migration (DO NOT RENAME IDs)

| Occurrence | Category | Policy |
|------------|----------|--------|
| `20260804143343_AddAuthenticationMembershipFoundation` | HISTORICAL MIGRATION | MigrationId immutable |
| Migration class names (`AddXxxFoundation`) | HISTORICAL MIGRATION | Class name unchanged |
| Migration file names | HISTORICAL MIGRATION | Filename unchanged |
| `__EFMigrationsHistory` rows | HISTORICAL MIGRATION | Applied history untouched |
| Migration namespace | ACTIVE TECHNICAL IDENTITY | May update to `NevaxisZentra.Persistence.Migrations` (ID-only history) |

## Immutable Evidence (NO RENAME)

| Pattern | Examples |
|---------|----------|
| Root closure reports | `CREVAFACTOS-*-FINAL-REPORT.md`, `NEVAXISZENTRA-*-FINAL-REPORT.md` |
| Historical runbooks at root | `CREVAFACTOS-WEDNESDAY-SALES-DEMO-RUNBOOK.md` |
| `.cursor-screenshots/` | Validation artifacts from prior packages |

## Active Product Identity — Web UI (RENAME)

| File | Occurrence | Target |
|------|------------|--------|
| `apps/web/src/lib/brand/*` | FactOS | Zentra / Nevaxis Zentra |
| `apps/web/src/app/layout.tsx` | metadata description | Zentra descriptor |
| `apps/web/src/components/app-shell/sidebar.tsx` | `<strong>FactOS</strong>` | `<strong>Zentra</strong>` |
| `apps/web/src/config.ts` | `DEFAULT_APPLICATION_NAME` | `Zentra` |
| Golden demo copy | "FactOS Orchestrator" | "Zentra Orchestrator" |
| ERP bridge labels | "FactOS Netsis Bridge" | "Zentra Netsis Bridge" |
| Navigation | "FactOS Teams" | "Zentra Teams" |

## Database Strategy

| Item | New install | Existing install |
|------|-------------|------------------|
| Default database name | `nevaxis_zentra` | `crevafactos_local` / configured name continues |
| Schema/tables | Unchanged | Unchanged |
| Migration history | Unchanged | Unchanged |

## ProgramData / LocalAppData Strategy

**Selected: Strategy A (lowest risk)**  
Retain `C:\ProgramData\Nevaxis\FactOS\` and `%LOCALAPPDATA%\Nevaxis\FactOS\` as canonical runtime paths. Visible branding becomes Zentra; physical paths documented for upgrade continuity. Optional future migration to `...\Zentra\` is out of scope for this package.

## Install Path Strategy

| Scenario | Path |
|----------|------|
| New MSI install | `C:\Program Files\Nevaxis Zentra\` |
| Upgrade from FactOS | MajorUpgrade via preserved UpgradeCode; folder migration handled by installer custom action or retained subfolder |
| Existing FactOS folder | Recognized; not orphaned |

## Post-Rename Verification Targets

- `ACTIVE USER-VISIBLE FACTOS = 0` in web/desktop/login
- `ACTIVE NEW CODE NEVAXISZENTRA = 0` in non-excluded source
- `dotnet build NevaxisZentra.sln` = PASS
- Migration discovery = PASS
- Legacy env fallback tests = PASS

## Remaining Occurrences After Rename (Expected)

| Pattern | Expected category |
|---------|-------------------|
| `crevafactos` in enum keys | CUSTOMER DATA CONTRACT |
| `NEVAXIS_FACTOS_` in LegacyEnvironmentVariables | BACKWARD COMPATIBILITY |
| `CREVAFACTOS` in root reports | IMMUTABLE EVIDENCE |
| Migration filenames containing no FactOS | N/A |
| `Software\NevaxisZentra` in WiX | BACKWARD COMPATIBILITY |
