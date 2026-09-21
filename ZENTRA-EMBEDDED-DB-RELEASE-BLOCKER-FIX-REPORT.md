# Zentra Embedded DB Release Blocker Fix Report

**Date:** 2026-09-22  
**Package:** Customer release candidate `2026.09.22-production`  
**Prior acceptance:** `ZENTRA-PHYSICAL-CLEAN-PC-RELEASE-ACCEPTANCE.md` remains **FAIL** (preserved, not overwritten)

## INITIAL STATUS

- Physical clean-PC acceptance: **FAIL** (missing `postgres/template` in shipped MSI)
- Customer release: **NOT READY**
- Root engineering build used `-SkipDatabaseTemplate`

## ROOT CAUSE

Engineering publish at `5972cd43` invoked `scripts/desktop/publish-full-windows-release.ps1` with **`-SkipDatabaseTemplate`**, producing an MSI whose staging payload had an empty `postgres/template` directory. First launch fell back to raw `initdb` without `__EFMigrationsHistory`, failing schema validation against `expected-schema-version.txt`.

## SOURCE FIX

- Customer builds **require** embedded template; `-SkipDatabaseTemplate` allowed only with **`-EngineeringArtifact`**
- `build-release-database-template.ps1` rebuilt: schema-only + migration history + system `permission_definitions` seeds, hardened runtime role, empty business data checks
- `validate-release-staging.ps1` release gate before MSI
- `EmbeddedPostgresTemplateReadiness` + startup preflight template validation
- `verify-packaged-database-engineering.ps1` for engineering smoke + tenant/permission verifiers
- Registered EF migration ID resolution (Designer-backed only) for `expected-schema-version.txt`
- Packaged migration DB user unified to `nevaxiszentra` in `runtime.env`

## DATABASE TEMPLATE

| Check | Result |
|--------|--------|
| Generation | **PASS** |
| Path | `release/staging/postgres/template/` |
| Files | 3762 |
| Bytes | 92622661 (~88.4 MB) |
| Schema version | `20260919250000_GrantScenarioRecoveryRuntimeDelete` |
| Demo/customer rows (tenants, MIX-LINE-01, golden_demo_*) | **0** |

## ENGINEERING PRECHECKS (packaged staging)

| Gate | Result |
|------|--------|
| Packaged Postgres start | **PASS** |
| Tenant isolation verifier | **PASS** |
| Permission authorization verifier | **PASS** |
| RLS / FORCE RLS (tenants) | **PASS** |

## NEW INSTALLER

| Field | Value |
|-------|--------|
| Version | `2026.09.22-production` |
| MSI | `release/artifacts/Nevaxis-Zentra-Setup-2026.09.22-production.msi` |
| Size | ~488 MB (511705540 bytes) |
| Handoff | `release/handoff/2026.09.22-production/` |

SHA256 (`Nevaxis-Zentra-Setup-2026.09.22-production.msi`): `b3cf5f1f8d360e56742acad6fa30c83a317076bc8c7a1b14a7de6a2850558e42`

## REJECTED ARTIFACT

- Prior `2026.09.16-production` / `-SkipDatabaseTemplate` MSI — **REJECTED / NOT FOR CUSTOMER RELEASE**
- Evidence preserved: `.cursor-screenshots/clean-pc-acceptance/*`, `ZENTRA-PHYSICAL-CLEAN-PC-RELEASE-ACCEPTANCE.md`

## CLEAN-VM RERUN

Checklist: `ZENTRA-CLEAN-VM-RERUN-CHECKLIST.md`

## KNOWN LIMITATIONS

- `20260921090000_AddOperationalValueFoundation.cs` exists without EF Designer registration; not applied until properly registered via `dotnet ef migrations add` workflow
- MSI product version warning (WiX WIX1148) for dotted version string — functional but should be normalized in a follow-up
- Physical clean-PC acceptance **not re-run in this task** (requires fresh VM + UAC)

## RELEASE BLOCKER FIX

**PASS** (engineering package with embedded template + corrected installer)

## PHYSICAL CLEAN-PC RELEASE ACCEPTANCE

**PENDING RERUN** on fresh Windows 11 VM

## CUSTOMER RELEASE

**NOT READY** until clean-VM pass
