# Zentra Clean-VM Acceptance Rerun Checklist

Use this checklist for the **corrected** `2026.09.22-production` installer with embedded `postgres/template`. Do not reuse the failed `2026.09.16-production` / `-SkipDatabaseTemplate` artifact.

## A. Create clean Windows 11 VM

- Fresh Windows 11 64-bit VM snapshot (no prior Zentra/FactOS installs).

## B. Verify VM has no engineering dependencies

- [ ] No NevaxisZentra git repo
- [ ] No Cursor / VS Code dev stack for this project
- [ ] No Node.js / pnpm
- [ ] No .NET SDK
- [ ] No Docker
- [ ] No standalone PostgreSQL
- [ ] No `C:\ProgramData\Nevaxis\FactOS` or `C:\ProgramData\Nevaxis\Zentra`

## C. Copy handoff package only

Copy `release/handoff/2026.09.22-production/` contents to the VM (USB or network share):

- `Nevaxis-Zentra-Setup-2026.09.22-production.msi`
- `SHA256SUMS.txt`
- `NEVAXISZENTRA-WINDOWS-RELEASE-MANIFEST.md`
- `README-INSTALL.txt`

## D. Verify SHA256

```powershell
Get-FileHash -Algorithm SHA256 .\Nevaxis-Zentra-Setup-2026.09.22-production.msi
```

Match against `SHA256SUMS.txt`.

## E. Run MSI (interactive UAC)

- [ ] Double-click MSI (do not rely on silent non-elevated install)
- [ ] Approve Windows UAC (**Yes**)
- [ ] Complete wizard; default per-machine `Program Files` install
- [ ] Capture `msiexec /L*v` log if install fails

## F. First launch

- [ ] Start **Nevaxis Zentra** from Start Menu or Desktop
- [ ] Wait for embedded PostgreSQL + API + web startup (bounded splash; no manual services)

## G. Core health

- [ ] Embedded Postgres data under `%ProgramData%\Nevaxis\FactOS\database`
- [ ] Schema version matches manifest (`20260919250000_GrantScenarioRecoveryRuntimeDelete`)
- [ ] API responds on packaged port (5000)
- [ ] Web UI loads (3001)

## H. Empty customer state

- [ ] FABRİKA empty (no MIX-LINE-01 / Golden Demo production rows)
- [ ] No synthetic orders/telemetry/ROI fixtures

## I. Machine/resource bulk import

- [ ] Veri Aktarım Merkezi opens
- [ ] Import bundled CSV templates from `customer-import-templates`

## J. Reimport idempotency

- [ ] Repeat import; no duplicate corruption

## K. Invalid row quarantine

- [ ] Invalid rows quarantined; no partial commit

## L. Close / restart app

- [ ] Data persists after relaunch

## M. Reboot Windows

- [ ] Full OS reboot

## N. Relaunch

- [ ] App + DB recover automatically

## O. Data persistence

- [ ] Imported data still present

## P. Release-safe verifiers (engineering scripts on VM **only if** release-safe tooling is copied)

Preferred: run `scripts/release/verify-packaged-database-engineering.ps1` on engineering machine against installed bundle path before VM test; on VM capture evidence via diagnostics package.

## Q. RLS / FORCE RLS

- [ ] Confirm tenant table FORCE RLS (support/diagnostics evidence)

## R. Unauthorized user test

- [ ] Non-member login denied

## S. Model-disabled mode

- [ ] Product behaves safely without external model keys

## T. Capture evidence

- [ ] Installer log, first-launch logs, screenshots, diagnostics ZIP
- [ ] Store under `.cursor-screenshots/clean-pc-acceptance/` **without** overwriting prior FAIL evidence

## Rejected prior artifact

- `2026.09.16-production` MSI built with `-SkipDatabaseTemplate` — **REJECTED / NOT FOR CUSTOMER RELEASE**
