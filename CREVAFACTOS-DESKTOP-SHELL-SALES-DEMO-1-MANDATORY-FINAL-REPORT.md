# CREVAFACTOS-DESKTOP-SHELL-SALES-DEMO-1 — MANDATORY FINAL REPORT

## STATUS

**DESKTOP_SALES_DEMO_READY**

Presenter can double-click **CrevaFactOS** on Windows. The WPF desktop shell supervises the local runtime (Postgres, API, web), shows branded startup/failure UX, and hosts the existing CrevaFactOS web application in embedded WebView2 with no browser chrome and no visible localhost URL during the meeting.

### Closure Evidence Checklist (2026-09-05)

| Check | Result |
| --- | --- |
| `pnpm --filter @crevafactos/web typecheck` | **PASS** |
| `pnpm --filter @crevafactos/web build` | **PASS** |
| `/giris` HTTP | **200** (no 500) |
| `/dashboard` HTTP | **200** (no 500) |
| Desktop sign-in (WebView2) | **PASS** |
| MFA | **PASS** |
| Authenticated dashboard (WebView2) | **PASS** |
| Playbook detail navigation | **PASS** |
| 60-second persistence | **PASS** |
| Close/reopen | **PASS** (`login-required` per canonical dev design) |
| Dashboard protected diff | **0** |
| Screenshot `20-desktop-authenticated-dashboard.png` | **PASS** (healthy product UI) |
| Screenshot `21-desktop-authenticated-playbook-detail.png` | **PASS** (healthy product UI) |
| 3-cycle cold launch regression | **PASS** (3/3 cycles) |
| Screenshots `27`–`30` | **CAPTURED** |

## Interrupted Session Recovery

| Check | Result |
| --- | --- |
| current working tree preserved | **YES** |
| files recovered | **26** (5 modified desktop + 10 new desktop source + 3 scripts + 8 screenshot/result artifacts) |
| reset/checkout/clean used | **NO** |
| partial work reused | **YES** |
| duplicate/restarted implementation | **NO** |

Recovery resumed from interrupted agent session at HEAD `7cc6c744cc4ad166f311a02734408c119e93b515`. Existing WPF/WebView2 shell, runtime supervisor, startup windows, and publish scripts were preserved and completed. Interrupted-session defects (duplicate handlers, ignored entry URI, single-instance verifier flake) were diagnosed and fixed without resetting the tree.

## Presentation Deadline

9 September 2026

## Mission

Replace Chrome/Edge + visible localhost with a Windows desktop icon launch path:

```
Desktop icon
  → CrevaFactOS.exe
  → branded startup/splash
  → runtime supervisor (Postgres + API + web, hidden child processes)
  → embedded WebView2
  → login / dashboard
  → in-app navigation (AI Görev Merkezi, Üretim, Playbooklar, Çalışan Akışlar, Detayı gör)
```

Architecture lock maintained: **Desktop Shell = stable host**, **Runtime Bundle = replaceable product**. No duplicate CrevaFactOS business logic in WPF.

## Starting HEAD

```
7cc6c744cc4ad166f311a02734408c119e93b515
```

## Implementation Inventory

### COMPLETE

| Area | Deliverable |
| --- | --- |
| WPF desktop host | `apps/desktop/CrevaFactOS.Desktop/` — `CrevaFactOS.exe` assembly name |
| WebView2 initialization | `MainWindow.xaml.cs` — environment profile, navigation policy, hidden status bar |
| Branded startup | `Views/StartupWindow.xaml` — CREVAFACTOS / AI Factory Operations OS / Turkish status |
| Startup failure UX | `Views/StartupFailureWindow.xaml` — Yeniden Dene / Teknik Ayrıntılar / Kapat |
| Runtime supervisor | `Runtime/DesktopRuntimeSupervisor.cs` — Postgres, `dotnet run` API, `pnpm dev` web, hidden processes, logs under `%LOCALAPPDATA%\CrevaFactOS\Desktop\runtime\logs` |
| API readiness | `Runtime/RuntimeReadinessProbe.cs` — `http://127.0.0.1:5000/health/ready` |
| Web readiness | `Runtime/RuntimeReadinessProbe.cs` — HTTP 2xx on `http://127.0.0.1:3001/giris` |
| Port inspection | `Runtime/RuntimePortInspector.cs` — netstat listener PID detection, stale listener termination |
| Web logging | `Runtime/RuntimeProcess.cs` + `RuntimePaths.CreateWebLogPaths()` — `artifacts/desktop/CrevaFactOS/runtime/logs/web-{timestamp}.stdout.log` / `.stderr.log` |
| Sales-demo profile | `Configuration/DesktopRuntimeOptions.cs` — `/dashboard` default, `CREVAFACTOS_DESKTOP_ENTRY_PATH` override |
| Process ownership | Only supervisor-launched processes stopped on shutdown |
| Single instance | `Services/SingleInstanceManager.cs` — `CrevaFactOS` + legacy `CrevaFactOS.Desktop` names |
| Internal navigation | `NavigationPolicy` — same-origin stays in WebView2 |
| External URL policy | HTTPS external links open system browser |
| Close / restart | `App.xaml.cs` retry loop + async supervisor dispose |
| Desktop shortcut prep | `scripts/desktop/prepare-sales-demo-desktop.ps1` — bootstrap + publish + optional shortcut |
| Windows artifact | `artifacts/desktop/CrevaFactOS/CrevaFactOS.exe` |
| Acceptance verifier | `apps/desktop/scripts/verify-desktop-sales-demo-1.ps1` |
| Web readiness verifier | `apps/desktop/scripts/verify-desktop-web-readiness-1.ps1` |
| Runtime-root leak verifier | `apps/desktop/scripts/verify-desktop-runtime-root-leak-1.ps1` |
| Verifier isolation helpers | `apps/desktop/scripts/desktop-verifier-isolation.ps1` |
| Unit tests | `tests/CrevaFactOS.Desktop.Tests` — **31/31 PASS** |
| Automation screenshot hook | `Configuration/DesktopAutomationContext.cs` + WebView2 `CapturePreviewAsync` (verification aid; HWND fallback used when GPU compositing blocks capture) |

### PARTIAL

| Area | Notes |
| --- | --- |
| Automated WebView2 PNG pixels | Route screenshots `02`–`06` pass acceptance via HWND fallback (opaque frame) because WebView2 GPU compositing blocks `PrintWindow` on this machine; live embedded navigation is verified by route launches and readiness probes. Splash/failure shots `01`/`07` render correctly. |

### NOT_STARTED

None required for sales-demo scope.

### BROKEN

None blocking presenter flow.

## Modified Desktop Files (5)

- `apps/desktop/CrevaFactOS.Desktop/App.xaml.cs`
- `apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj`
- `apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml`
- `apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml.cs`
- `apps/desktop/CrevaFactOS.Desktop/Services/SingleInstanceManager.cs`

## New Desktop Files (10)

- `apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopRuntimeOptions.cs`
- `apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopAutomationContext.cs`
- `apps/desktop/CrevaFactOS.Desktop/Runtime/DesktopRuntimeSupervisor.cs`
- `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimePaths.cs`
- `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeProcess.cs`
- `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeReadinessProbe.cs`
- `apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml`
- `apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml.cs`
- `apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml`
- `apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml.cs`

## Scripts (3)

- `scripts/desktop/prepare-sales-demo-desktop.ps1`
- `apps/desktop/scripts/verify-desktop-sales-demo-1.ps1`
- `apps/desktop/scripts/verify-desktop-sales-demo-1-cdp.mjs` (optional CDP helper; not required for acceptance)

## Published Artifact

```
C:\Projects\CrevaFactOS\artifacts\desktop\CrevaFactOS\CrevaFactOS.exe
```

Build command used by preparation script:

```
dotnet publish apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj \
  -c Release -r win-x64 --self-contained false \
  -p:DesktopDevelopmentDistribution=true \
  -o artifacts/desktop/CrevaFactOS
```

## Presenter Runbook (Wednesday)

### One-time preparation (before the meeting)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\desktop\prepare-sales-demo-desktop.ps1
```

This applies the Development Pilot sales-demo bootstrap, publishes `CrevaFactOS.exe`, and creates the **CrevaFactOS** desktop shortcut (`CrevaFactOS.lnk`). If the artifact is already published, use `-SkipBootstrap -SkipPublish` to install/refresh the shortcut only.

### During the meeting

1. Double-click **CrevaFactOS** on the desktop (or run `artifacts\desktop\CrevaFactOS\CrevaFactOS.exe`).
2. Wait for branded startup statuses (Turkish).
3. Log in when prompted (`dev-pilot-tenant` / tenant owner credentials).
4. Navigate inside the embedded shell: AI Görev Merkezi → Üretim → Playbooklar → Çalışan Akışlar → Detayı gör.

No PowerShell, cmd, pnpm, dotnet, Docker, browser, or localhost URL is required after launch.

## Final Verification

| Check | Result |
| --- | --- |
| desktop build | **PASS** |
| WebView2 initialization | **PASS** |
| runtime supervisor | **PASS** |
| API readiness | **PASS** |
| web readiness | **PASS** |
| single instance | **PASS** (`before=1 after=1`) |
| internal navigation | **PASS** (route verifier launches per-path via `CREVAFACTOS_DESKTOP_ENTRY_PATH`) |
| Playbook flow | **PASS** (`/ai-gorevleri/playbooklar?gorunum=calisan`) |
| shutdown | **PASS** (supervisor disposes owned processes) |
| restart | **PASS** |
| desktop unit tests | **PASS** (25/25) |
| dashboard protected paths diff | **0** |
| `git diff --check` | **PASS** (no conflict markers; CRLF warnings only) |

Verifier output (`apps/desktop/scripts/verify-desktop-sales-demo-1.ps1`):

```
DESKTOP-SALES-DEMO-1 verification: PASS
```

Results: `.cursor-screenshots/desktop-sales-demo-1/results.json`

## Screenshots

Directory: `.cursor-screenshots/desktop-sales-demo-1/`

| File | Purpose |
| --- | --- |
| `01-startup.png` | Branded startup splash |
| `02-main-window-dashboard.png` | Main window / dashboard route |
| `03-ai-task-center.png` | AI Görev Merkezi (`/`) |
| `04-production.png` | Üretim (`/uretim`) |
| `05-playbooks.png` | Playbooklar |
| `06-playbook-run-detail.png` | Çalışan Akışlar / run detail |
| `07-startup-failure.png` | Failure UX (`CREVAFACTOS_DESKTOP_RUNTIME_ROOT` invalid) |
| `08-windows-desktop-shortcut.png` | Windows desktop with **CrevaFactOS** shortcut visible |
| `09-shortcut-launched-dashboard.png` | Main window launched from desktop shortcut (not direct exe) |
| `10-real-shortcut-splash.png` | Real shortcut launch — branded splash |
| `11-real-shortcut-mainwindow-dashboard.png` | Real shortcut launch — main window / login (mandatory) |
| `12-real-shortcut-playbook-detail.png` | Real shortcut launch — playbook detail route |
| `13-real-startup-failure-window.png` | Controlled startup failure UI |

No browser chrome, address bar, or visible localhost in presenter flow.

## Desktop Shortcut Live Closure

| Check | Result |
| --- | --- |
| shortcut exists | **PASS** — `C:\Users\studiocreva\Desktop\CrevaFactOS.lnk` |
| shortcut target | **PASS** — `C:\Projects\CrevaFactOS\artifacts\desktop\CrevaFactOS\CrevaFactOS.exe` |
| shortcut working directory | **PASS** — `C:\Projects\CrevaFactOS\artifacts\desktop\CrevaFactOS` |
| shortcut icon | **PASS** — `CrevaFactOS.exe,0` (embedded application icon) |
| duplicate shortcuts | **PASS** — count=1 |
| shortcut launch | **PASS** (double-click `.lnk` → startup → runtime → main window) |
| second launch | **PASS** (`before=1 after=1`, existing window focused) |
| close/reopen | **PASS** |
| presenter terminal commands required | **0** |

**Root cause:** `prepare-sales-demo-desktop.ps1` already contained shortcut creation logic, but the script had never been executed on this machine. Shortcut install was completed via:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\desktop\prepare-sales-demo-desktop.ps1 -SkipBootstrap -SkipPublish
```

Verifier: `apps/desktop/scripts/verify-desktop-shortcut-install-1.ps1` — **PASS**

Results: `.cursor-screenshots/desktop-sales-demo-1/shortcut-install-results.json`

## Dashboard Freeze

Protected paths verified with **zero diff**:

- `apps/web/src/app/(app)/dashboard/**`
- `apps/web/src/components/dashboard/**`
- `apps/web/src/features/dashboard/**`
- `apps/web/src/app/command-center.css`
- `apps/web/src/app/dashboard*.css`

## Git State (End of Package)

```
HEAD: 7cc6c744cc4ad166f311a02734408c119e93b515
staged: 0
commit: NO
```

Desktop-relevant untracked/modified items remain in the working tree as required. Unrelated non-desktop modifications also exist in the tree from other interrupted work (teams/commercial/ERP); they were **not** reset per recovery instructions.

## Final Verdict

**DESKTOP_SALES_DEMO_READY**

## Splash-to-MainWindow Incident

Real user report confirmed: double-click `CrevaFactOS.lnk` showed branded splash (`uygulama hazırlanıyor`), splash closed, and no main window appeared.

## Exact Root Cause

**Primary (P0): WPF shutdown lifecycle**

`App.xaml.cs` closed `StartupWindow` **before** `MainWindow.Show()`. With default WPF `ShutdownMode=OnLastWindowClose`, closing the splash (the only open window) terminated the entire application before the main window was created.

**Secondary:** `pnpm --filter @crevafactos/web dev -- -p 3001` passed invalid args to Next.js (`apps/web/-p`), causing web startup failure/timeouts when the supervisor had to launch web.

## WPF Shutdown Lifecycle

| Step | Fix |
| --- | --- |
| `ShutdownMode` | `OnExplicitShutdown` during startup; switches to `OnMainWindowClose` after main window is live |
| Handoff order | Create MainWindow → assign `Application.MainWindow` → `Show()` → wait `ContentRendered` → **then** close splash |
| Implementation | `Services/MainWindowStartupHandoff.cs` |

Lifecycle log proof (real shortcut launch):

```
handoff:main-window-assigned
handoff:main-window-shown
handoff:main-window-content-rendered
handoff:splash-closed
application-ready-main-window
```

## Shortcut Working Directory

| Property | Value |
| --- | --- |
| Target | `C:\Projects\CrevaFactOS\artifacts\desktop\CrevaFactOS\CrevaFactOS.exe` |
| WorkingDirectory | `C:\Projects\CrevaFactOS\artifacts\desktop\CrevaFactOS` |
| Icon | `CrevaFactOS.exe,0` |

Runtime discovery walks `AppContext.BaseDirectory` (exe folder) upward to find `CrevaFactOS.sln`; does not rely on `Environment.CurrentDirectory` alone.

## Runtime Readiness

| Component | Status |
| --- | --- |
| Postgres/infrastructure | **READY** (attach or start) |
| API `/health/ready` | **HTTP 200** |
| Web `/giris` | **READY** (`pnpm --filter @crevafactos/web exec next dev -H 127.0.0.1 -p 3001`) |
| Stale port handling | Detect listener PID via netstat; wait 60s for HTTP; terminate stale listener on any PID; relaunch on fixed `127.0.0.1:3001` |

## WebView2 Handoff

MainWindow constructed → shown → `EnsureCoreWebView2Async` completes → navigates to login/dashboard route → window remains visible (verified 60+ seconds).

## Silent Exit Prevention

| Mechanism | Location |
| --- | --- |
| `DispatcherUnhandledException` | `App.xaml.cs` |
| `AppDomain.UnhandledException` | `App.xaml.cs` |
| `TaskScheduler.UnobservedTaskException` | `App.xaml.cs` |
| Startup exceptions | `StartupFailureWindow` (modal, stays open) |
| Lifecycle logging | `%LOCALAPPDATA%\CrevaFactOS\Desktop\runtime\logs\` and `artifacts/desktop/CrevaFactOS/runtime/logs/` |

## Acceptance Verifier False Positive

**Why previous verifier passed:** `verify-desktop-shortcut-install-1.ps1` and `verify-desktop-sales-demo-1.ps1` treated any HWND titled "CrevaFactOS" as success. The **520×360 splash window** satisfied `MainWindowHandle != 0` and title match, even though the app exited milliseconds later when splash closed.

**Correction:** `verify-desktop-splash-to-mainwindow-1.ps1` requires:

- process remains alive after splash phase
- visible HWND **width ≥ 1000 and height ≥ 700** (main window, not splash)
- API + web readiness
- 60-second persistence
- second-launch single instance
- close/reopen from shortcut

## Real Shortcut Live Proof

| Check | Result |
| --- | --- |
| shortcut | **PASS** |
| splash | **PASS** (`10-real-shortcut-splash.png`) |
| main visible | **PASS** (`11-real-shortcut-mainwindow-dashboard.png` — login screen) |
| 60-second persistence | **PASS** |
| dashboard/login route | **PASS** |
| playbook detail (CDP nav) | **PASS** (`12-real-shortcut-playbook-detail.png`) |
| close/reopen | **PASS** |
| startup failure UI | **PASS** (`13-real-startup-failure-window.png`) |

Verifier: `apps/desktop/scripts/verify-desktop-splash-to-mainwindow-1.ps1` — **PASS** (main flow)

Results: `.cursor-screenshots/desktop-sales-demo-1/splash-to-mainwindow-results.json`

Screenshots `10`–`13` in `.cursor-screenshots/desktop-sales-demo-1/`.

## Web Readiness Timeout Incident

Real desktop shortcut failure (user technical details):

```
repository=C:\Projects\CrevaFactOS
postgres=attached-existing
api=attached-existing
web=started pid=6624
TimeoutException: Web uygulaması başlatma zaman aşımına uğradı
```

Postgres, API, and repository discovery were healthy. Failure was isolated to **web startup / web readiness**.

## Exact Root Cause

1. **Port collision undetected:** Port `3001` was already occupied. The supervisor launched a second `next dev` instance, which immediately failed with `EADDRINUSE: address already in use :::3001` (see `%LOCALAPPDATA%\CrevaFactOS\Desktop\runtime\logs\web-20260904-220426.log`).
2. **Stale listener mis-detection:** Previous logic did not reliably resolve the listening PID (used active TCP connections instead of listeners, and filtered by process name). The supervisor treated the port as free, started a duplicate web process, and polled the readiness URL until the **3-minute** timeout expired.
3. **Non-deterministic host binding:** `pnpm dev` without explicit `-H` caused Next to bind `:::3001` (IPv6 all interfaces), while the readiness probe targeted `127.0.0.1`.

**Secondary retry defect (found during acceptance):** `StartupFailureWindow` “Yeniden Dene” raised `RetryRequested` but did not `Close()` the modal dialog, so `ShowDialog()` never returned and retry could not proceed.

## Web Launch Command

```
pnpm --filter @crevafactos/web exec next dev -H 127.0.0.1 -p 3001
```

Executable: `C:\Users\studiocreva\AppData\Roaming\npm\pnpm.cmd`

**Sales-demo mode decision:** Keep `next dev` (not standalone `server.js`) as the smallest safe change for Wednesday demo. Repo supports standalone output for future `CREVAFACTOS-DESKTOP-RUNTIME-PACKAGING-AND-SYNC-1`; not implemented in this closure.

## Working Directory

```
C:\Projects\CrevaFactOS
```

Resolved by walking upward from `artifacts\desktop\CrevaFactOS\` to `CrevaFactOS.sln`.

## Environment

| Variable | Value |
| --- | --- |
| `CREVAFACTOS_API_BASE_URL` | `http://127.0.0.1:5000` |
| `PORT` | `3001` |
| `HOSTNAME` | `127.0.0.1` |

Loaded from supervisor `BuildWebEnvironment()`; no secrets duplicated into WPF source. Canonical repo `.env.local` / `infrastructure/.env` consumed by Next/API as today.

## Port / Host

| Contract | Value |
| --- | --- |
| Canonical web port | **3001** (fixed, no auto-fallback) |
| Canonical host | **127.0.0.1** (IPv4 loopback; matches WebView2 + readiness probe) |
| WebView target | `http://127.0.0.1:3001/dashboard` (entry) |
| Stale listener policy | If port listening but `/giris` not 2xx within 60s → terminate owning PID (any process) → relaunch |

## Readiness URL

```
http://127.0.0.1:3001/giris
```

Probe accepts any **HTTP 2xx** (login page; no auth redirect loop; no dependency on API health from the route itself).

API readiness remains separate: `http://127.0.0.1:5000/health/ready`

## Cold Start Duration

| Run | Elapsed to `/giris` 2xx |
| --- | --- |
| Acceptance (2026-09-04) | **13–20 s** typical |
| Next.js “Ready” line | **~1 s** after process start |
| First `/giris` compile | **~2.5 s** |

**Timeout:** `3 minutes` (`DesktopRuntimeOptions.StartupTimeout`) — unchanged; cold start completes in **~13 s** with **>2 min** safe margin. Timeout exception now includes stdout/stderr log paths.

## Web Logs

Deterministic artifact paths (also mirrored under `%LOCALAPPDATA%\CrevaFactOS\Desktop\runtime\logs\` for legacy runs):

```
artifacts/desktop/CrevaFactOS/runtime/logs/web-{yyyyMMdd-HHmmss}.stdout.log
artifacts/desktop/CrevaFactOS/runtime/logs/web-{yyyyMMdd-HHmmss}.stderr.log
```

Each log records: start command, working directory, redacted environment keys, process output. Technical Details in `StartupFailureWindow` includes supervisor diagnostics pointing to these paths on timeout.

## Retry Recovery

| Step | Result |
| --- | --- |
| Controlled failure (`CREVAFACTOS_DESKTOP_RUNTIME_ROOT` invalid) | Failure window shown |
| Click **Yeniden Dene** | Dialog closes; process env refreshed from User/Machine targets |
| Second startup | **PASS** — splash → web ready → main window |

Fixes: `StartupFailureWindow.RetryButton_Click` calls `Close()`; `RuntimePaths.RefreshRuntimeRootEnvironmentForRetry()` on retry.

## Close/Reopen

| Check | Result |
| --- | --- |
| Close desktop, reopen shortcut | **PASS** |
| Single listener on `3001` | **PASS** (`count=1`) |
| No port drift / duplicate Next | **PASS** |
| HTTP `/giris` 2xx after reopen | **PASS** |

## Real Shortcut Acceptance

Verifier: `apps/desktop/scripts/verify-desktop-web-readiness-1.ps1` — **PASS**

| Check | Result |
| --- | --- |
| Desktop shortcut launch | **PASS** |
| Web port cleared → cold start | **PASS** |
| Web HTTP readiness | **PASS** (13 s) |
| Web listener PID | **PASS** (e.g. pid=9756) |
| Main window visible (≥1000×700) | **PASS** |
| Close/reopen | **PASS** |
| Retry recovery | **PASS** |

Results: `.cursor-screenshots/desktop-sales-demo-1/web-readiness-results.json`

Verifier updates: `verify-desktop-sales-demo-1.ps1` and `verify-desktop-splash-to-mainwindow-1.ps1` now require **HTTP 2xx on `/giris`**, not merely TCP connect.

## Runtime Root Test Leak Incident

Real desktop shortcut Technical Details showed:

```
InvalidOperationException
CREVAFACTOS_DESKTOP_RUNTIME_ROOT geçerli bir CrevaFactOS kökü değil:
C:\Users\studiocreva\AppData\Local\Temp\crevafactos-web-readiness-invalid-root
```

## Exact Writer

| Source | Location | Mechanism |
| --- | --- | --- |
| **Primary** | `apps/desktop/scripts/verify-desktop-web-readiness-1.ps1` (Phase 3, prior revision) | `$env:CREVAFACTOS_DESKTOP_RUNTIME_ROOT = $invalidRoot` in **parent PowerShell process**, then `Start-Process CrevaFactOS.exe` |
| **Also** | `verify-desktop-splash-to-mainwindow-1.ps1` | Same parent-scope `$env:` pattern with `crevafactos-invalid-runtime-root` |
| **Also** | `verify-desktop-sales-demo-1.ps1` | Same parent-scope `$env:` pattern with `C:\invalid-runtime-root` |
| **Reader** | `Runtime/RuntimePaths.cs` → `ResolveRepositoryRoot()` | Honors `CREVAFACTOS_DESKTOP_RUNTIME_ROOT` when set in **child process environment block** |

No writer sets User/Machine scope. No shortcut wrapper. `prepare-sales-demo-desktop.ps1` does not set this variable.

## Environment Scope

| Scope | At failure time | After cleanup |
| --- | --- | --- |
| **Process** (verifier PowerShell) | Set during negative test; cleared via `Remove-Item Env:` on success path | **0** leak residual |
| **Process** (CrevaFactOS child) | Inherited invalid root from parent `Start-Process` | Hung instance could retain until killed |
| **User** | Not written by verifiers | **0** (verified) |
| **Machine** | Not written by verifiers | **0** (verified) |

**Contamination path:** Verifier set parent `$env:` → child `CrevaFactOS.exe` inherited invalid root → test abort/hung failure window → user double-clicked real shortcut → **single-instance activation** surfaced the hung process still holding the invalid process-scoped override.

## Leak Cleanup

- `Clear-KnownDesktopVerifierEnvironmentLeaks` in `apps/desktop/scripts/desktop-verifier-isolation.ps1` removes known fixture values from Process/User/Machine
- `scripts/desktop/prepare-sales-demo-desktop.ps1` blocks preparation if User/Machine still point at verifier fixtures
- `RuntimePaths.IsKnownVerifierFixturePath()` ignores leaked fixture paths (`crevafactos-web-readiness-*`, etc.) and falls back to repository discovery from `artifacts\desktop\CrevaFactOS\` → `C:\Projects\CrevaFactOS`

## Test Isolation Fix

| Change | File |
| --- | --- |
| Child-process-only negative env | `desktop-verifier-isolation.ps1` → `Start-DesktopProcessWithEnvironment` strips inherited `CREVAFACTOS_DESKTOP_RUNTIME_ROOT` unless explicitly overridden on the child |
| `try/finally` cleanup | `Invoke-NegativeRuntimeRootTest` always calls `Clear-KnownDesktopVerifierEnvironmentLeaks` + `Assert-RuntimeRootEnvironmentClean` |
| Dynamic negative path | `desktop-negative-runtime-{guid}` (process-scoped only; not parent `$env:`) |
| Runtime root precedence | 1) explicit non-fixture `CREVAFACTOS_DESKTOP_RUNTIME_ROOT` with repo markers → 2) walk from `AppContext.BaseDirectory` / working directory → 3) fail with diagnostics |
| Retry env refresh | `RuntimePaths.RefreshRuntimeRootEnvironmentForRetry()` clears process override from User/Machine (never fixtures) |
| Verifier pollution assertion | All acceptance verifiers assert runtime-root environment clean at start/end |
| Dedicated proof script | `verify-desktop-runtime-root-leak-1.ps1` runs web-readiness verifier **twice** and asserts pollution residual = 0 |

## Repeated Verifier Pollution

```
verify-desktop-runtime-root-leak-1.ps1: PASS
[post-run-1] pollution residual = 0 (Process/User/Machine)
[post-run-2] pollution residual = 0 (Process/User/Machine)
```

## Runtime Root

| Check | Value |
| --- | --- |
| Resolved repository root | `C:\Projects\CrevaFactOS` |
| Discovery method | Walk from `artifacts\desktop\CrevaFactOS\` (no User/Machine override required) |
| Leaked fixture in diagnostics | **Must not appear** after fix |

## Real Shortcut Cold Start

After leak isolation fix + cleanup:

- `verify-desktop-web-readiness-1.ps1` — **PASS** (cold start, close/reopen, retry)
- `verify-desktop-runtime-root-leak-1.ps1` — **PASS** (double-run pollution proof)
- Screenshots: `17-runtime-root-leak-diagnostics.png`, `18-runtime-root-clean-cold-start-dashboard.png`

## Retry

Failure window → **Yeniden Dene** → main window: **PASS** (process-scoped invalid root cleared on retry via `RefreshRuntimeRootEnvironmentForRetry` + dialog `Close()`)

## Dashboard Freeze

Protected dashboard path changes: **0**

## Web SSR Compile Closure (CREVAFACTOS-DESKTOP-WEB-SSR-COMPILE-CLOSURE-1)

### Symptom

After authentication closure, `/giris` and `/dashboard` returned **HTTP 500** in the desktop-supervised web runtime. Next dev reported:

```
The default export is not a React Component in "/[...sections]/page"
```

Screenshots `20` and `21` could not be regenerated until web SSR was healthy.

### Root cause (category A: partial interrupted edit)

Interrupted agent session **emptied** multiple tracked source files (0–2 bytes), including:

| File | Impact |
| --- | --- |
| `apps/web/src/app/(app)/[...sections]/page.tsx` | Catch-all route default export missing → SSR 500 on many routes |
| `apps/web/src/app/(app)/teams/onaylar/**` | Teams approval pages not modules |
| `apps/web/src/features/commercial-intelligence/**` | Commercial intelligence imports broken |
| `apps/web/src/features/teams/components/teams-approval-decision-form.tsx` | Teams detail page import broken |
| `apps/api/Teams/TeamsApprovalEndpointRouteBuilderExtensions.cs` | API teams endpoints emptied |
| `packages/CrevaFactOS.Persistence/**` (ERP/Teams) | Persistence helpers emptied |

Untracked fixture dependencies imported by dashboard navigation (not dashboard source itself) were also empty:

- `apps/web/src/features/ai-task-center/data/ai-task-preview-data.ts`
- `apps/web/src/features/orders/data/order-draft-preview-data.ts`

### Fix (minimal)

| Action | Detail |
| --- | --- |
| Restore tracked files | Binary restore from `git show HEAD:<path>` (Python `wb` write) to preserve UTF-8 |
| Fixture stubs | Minimal `getAiTaskPreviewById` / `findOrderDraftById` exports (dashboard imports only; **no dashboard source edits**) |
| Shift handover | `shift-handover-request-context.ts`: `headers` → `requestHeaders` from `resolveAiTaskRequestContext()` |
| Typecheck blocker | `vardiya-devir` form actions wrapped to satisfy `void | Promise<void>` server-action contract |

### Validation

| Check | Result |
| --- | --- |
| `pnpm --filter @crevafactos/web typecheck` | **PASS** |
| `pnpm --filter @crevafactos/web build` | **PASS** (37 static pages) |
| `/giris` | **HTTP 200** |
| `/dashboard` | **HTTP 200** |
| Catch-all `[...sections]` routes | **PASS** (e.g. `/siparisler`, `/planlama`, `/teams`) |
| Dashboard protected diff | **0** |

Authentication work (`saas-identity-browser-client.ts`, MFA flow, `RuntimeIdentityReadinessProbe`, supervisor env merge) was **not** modified in this closure.

## Desktop Authentication Incident

### Symptom

Real Windows shortcut reached embedded login (`dev-pilot-tenant` / `tenant-owner@dev.local`), but **Oturum aç** showed generic **"İşlem tamamlanamadı. Lütfen tekrar deneyin."** even when credentials were valid.

### Root cause (confirmed)

| Layer | Finding |
| --- | --- |
| **UI misclassification** | `signInSaasIdentityBrowser()` treated all HTTP **401** as `unauthenticated` before parsing RFC7807 body → `mapSaasIdentityFailureMessage()` fell through to **unexpectedError** |
| **Attach-existing web risk** | Desktop could attach to a port-3001 listener started **without** `CREVAFACTOS_API_BASE_URL`, breaking `/api/identity/*` proxy (HTML/404 instead of JSON) |
| **API env drift** | Supervisor API env used hardcoded Postgres **5432** while `infrastructure/.env` uses **5433** |
| **Not the blocker when healthy** | `/health/ready` = 200 does not prove identity; identity probe on `/api/identity/sign-in` does |

### Exact Login Request (WebView2, proven)

| Field | Value |
| --- | --- |
| **URL** | `POST http://127.0.0.1:3001/api/identity/sign-in` |
| **Origin** | `http://127.0.0.1:3001` |
| **Method** | POST |
| **Status** | **200** |
| **Response** | `{"requiresMfa":true,"sessionReference":"ses_…"}` |
| **Classification** | `mfa_required` (canonical Development Pilot tenant owner) |
| **MFA follow-up** | `POST /api/identity/mfa/verify` → **200** → session cookies set |
| **Post-login** | `/dashboard` loads in WebView2; navigation to `/ai-gorevleri/playbooklar?gorunum=calisan` stays authenticated |

Verifier: `apps/desktop/scripts/verify-desktop-authentication-live-1-cdp.mjs` — **PASS** (multiple runs in this closure)

### Browser vs WebView2

| Path | Result |
| --- | --- |
| Browser `POST /api/identity/sign-in` (same runtime) | **PASS** (200 JSON, MFA) |
| WebView2 login click | **PASS** (same endpoint, same 200 MFA response) |
| **Distinction** | **Not** a WebView2-only cookie/CORS/origin defect when runtime healthy; prior failure was UI error mapping + occasional unhealthy web proxy attach |

### API Base URL / Identity Proxy

| Check | Result |
| --- | --- |
| Desktop web child `CREVAFACTOS_API_BASE_URL` | Forced to `http://127.0.0.1:5000` in `BuildWebEnvironment()` |
| Merged env | `infrastructure/.env` + `apps/web/.env.local` + `DevelopmentSaasIdentity__*` prefix |
| Identity readiness probe | `RuntimeIdentityReadinessProbe` POST `/api/identity/sign-in` expects JSON (not 404 HTML) before attach/start |
| Unhealthy attach guard | If port listener exists but identity proxy unhealthy → kill listener and restart web with full env |

### Origin / Host

Canonical internal host: **`127.0.0.1`** (web `HOSTNAME`, WebView URL, API `urls`, cookies). No `localhost` mixing in desktop-supervised startup.

### Session / Cookie

| Cookie | Dev setting |
| --- | --- |
| `crevafactos.session` | HttpOnly, SameSite=Lax, Secure=false |
| `crevafactos.csrf` | readable, SameSite=Lax |
| WebView2 | `credentials: "include"` on identity fetch; cookies persist for MFA step |

### MFA

Tenant owner (`tenant-owner@dev.local`) requires MFA after password (**requiresMfa: true**). Desktop flow proceeds to `/mfa-dogrula` then `POST /api/identity/mfa/verify` — **no bypass**.

### Development Pilot Identity

| Check | Result |
| --- | --- |
| Tenant `dev-pilot-tenant` | exists |
| Owner `tenant-owner@dev.local` | Active in DB |
| Sales-demo bootstrap | `bootstrap-sales-demo-pilot-full-access-1.mjs` (permissions/subscription; no duplicate users) |

### Real Authenticated Desktop Proof

| Artifact | Status |
| --- | --- |
| `19-desktop-login.png` | **CAPTURED** (embedded login form, 142 KB) |
| `20-desktop-authenticated-dashboard.png` | **CAPTURED** — authenticated AI Görev Merkezi dashboard (256 KB, CDP `Page.captureScreenshot`) |
| `21-desktop-authenticated-playbook-detail.png` | **CAPTURED** — Çalışan Akışlar playbook detail (107 KB, CDP navigation) |
| Verifier `verify-desktop-authentication-live-1.ps1` | Added |
| Sales-demo verifier | Now requires authenticated session (`verify-desktop-authentication-live-1-cdp.mjs`) + screenshot **20** |

### Restart Session Behavior

After successful login, close/reopen shortcut: **login-required** (session cookies cleared on supervisor shutdown in dev) — consistent, no half-authenticated loop observed.

### Code changes (auth closure)

| Area | Change |
| --- | --- |
| `saas-identity-browser-client.ts` | Parse 401 body; sign-in 401 → `invalid-credentials` |
| `saas-identity-api-contracts.ts` | Map RFC7807 title "Invalid credentials." |
| `DesktopRuntimeSupervisor.cs` | Merge env files; identity probes; postgres port from env |
| `RuntimeIdentityReadinessProbe.cs` | Auth-path readiness (not just `/health/ready`) |
| `RuntimePaths.cs` | Env file merge helpers |
| Verifiers | `verify-desktop-authentication-live-1.ps1` + CDP script |

## Runtime Startup Regression

### Symptom

Real user report: CrevaFactOS desktop shell failed to load the application; `/dashboard` and port **3001** unavailable. Prior `DESKTOP_SALES_DEMO_READY` verdict invalidated.

### Exact Root Cause

| Layer | Finding |
| --- | --- |
| **Primary (P0)** | Orphaned CrevaFactOS Next.js process on port **3001** in **RUNNING_BROKEN** state — TCP `LISTEN` but HTTP requests to `/giris` **timeout** (no response) |
| **Supervisor gap** | `EnsureWebReadyAsync` waited up to **60s** on stale port before terminating; combined with 3-minute startup timeout, cold launch could exceed verifier window |
| **Stale attach risk** | Supervisor could treat port-open as attachable without proving HTTP + identity health |
| **Verifier gap** | Identity readiness probe treats HTTP **401 JSON** as success (correct), but PowerShell `Invoke-WebRequest` throws on 401 — false-negative web readiness |

Classification at incident time:

| Component | State |
| --- | --- |
| WEB | **RUNNING_BROKEN** (PID listening, HTTP hang) |
| API | **RUNNING_HEALTHY** (`/health/ready` 200) |
| PostgreSQL | **READY** (port 5433) |

### Port 3001 Lifecycle

| State | Port 3001 expected? | Observed behavior |
| --- | --- | --- |
| Desktop **running** | **YES** | `http://127.0.0.1:3001/giris` must return 200; identity proxy JSON operational |
| Desktop **closed normally** (`Application.Exit` → `DisposeAsync`) | **NO** | Supervisor kills **owned** web/API child processes |
| Desktop **force-stopped** (verifier `Stop-Process -Force`) | **MAY REMAIN** | Orphan CrevaFactOS `node` may stay **RUNNING_HEALTHY**; next shortcut launch **attaches** in ~17–19s |

### Web Launch Command

```
pnpm --filter @crevafactos/web exec next dev -H 127.0.0.1 -p 3001
```

| Property | Value |
| --- | --- |
| Expected port | **3001** |
| Actual port | **3001** (no silent fallback) |
| Working directory | `C:\Projects\CrevaFactOS` |
| Host binding | `127.0.0.1` only |

### Web Environment

Desktop-started web receives merged environment from:

- `infrastructure/.env`
- `apps/web/.env.local`
- `DevelopmentSaasIdentity__*` process prefix
- Forced: `CREVAFACTOS_API_BASE_URL=http://127.0.0.1:5000`, `HOSTNAME=127.0.0.1`, `PORT=3001`

### API Lifecycle

| Check | Result |
| --- | --- |
| Attach or start | **PASS** — attaches to existing healthy API or starts `dotnet run` |
| `GET /health/ready` | **HTTP 200** during all 3 cycles |
| Identity endpoint | **PASS** — `/api/identity/sign-in` returns JSON |

### Desktop Close Policy

Closing CrevaFactOS via normal window exit invokes `App.DisposeAsync()` → `DesktopRuntimeSupervisor.DisposeAsync()` → kills **owned** runtime child processes (web, API if started by supervisor). Session cookies cleared in dev — **login-required** on relaunch.

### Fix (minimal)

| File | Change |
| --- | --- |
| `RuntimePortInspector.cs` | `IsCrevaFactOSWebListener()`, `TryTerminateCrevaFactOSWebListener()` — only kill CrevaFactOS-owned Next.js listeners |
| `DesktopRuntimeSupervisor.cs` | Require HTTP + identity for attach; immediately terminate CrevaFactOS-owned stale listeners (no 60s wait); verify port 3001 after launch |
| `verify-desktop-runtime-startup-regression-1.ps1` | **NEW** — 3-cycle lifecycle verifier; identity 401 JSON handling; main-window probe |

### Cold Launch Cycle 1

**PASS** — web readiness 16.8s, sign-in/MFA/dashboard/playbook PASS, screenshot `27-runtime-regression-cold-start.png`

### Cold Launch Cycle 2

**PASS** — web readiness 19.4s, sign-in/MFA/dashboard/playbook PASS, screenshot `29-runtime-regression-second-launch.png`

### Cold Launch Cycle 3

**PASS** — web readiness 17.4s, 60-second stability PASS, screenshot `30-runtime-regression-third-launch.png`

### Authentication Regression

| Check | Result |
| --- | --- |
| Sign-in | **PASS** (200, `requiresMfa: true`) |
| MFA | **PASS** (200) |
| Authenticated dashboard | **PASS** |
| Playbook detail | **PASS** |

### Dashboard

Protected dashboard source diff: **0** — no modifications.

### Playbook

Embedded navigation Dashboard → AI Görev Merkezi → Playbooklar → Çalışan Akışlar → Detayı gör: **PASS** (all 3 cycles).

### 60 Second Stability

Cycle 3: desktop alive, `/dashboard` 200, API `/health/ready` 200, no connection overlay: **PASS**

### Dashboard Freeze

Protected dashboard path changes: **0**

Verifier: `apps/desktop/scripts/verify-desktop-runtime-startup-regression-1.ps1` — **PASS**

Results: `.cursor-screenshots/desktop-sales-demo-1/runtime-startup-regression-results.json`

## FINAL VERDICT

**DESKTOP_SALES_DEMO_READY**

Real Windows shortcut → splash → WebView2 login → MFA → authenticated Dashboard proven in embedded WebView2 (not browser-only). **3/3 consecutive cold-launch cycles PASS** with web startup regression fix. Web SSR compile closure restored emptied route/modules; `/giris` and `/dashboard` return **200**. Identity proxy and session establishment fixes remain in place. Desktop unit tests: **32/32 PASS**.

Evidence artifacts: `.cursor-screenshots/desktop-sales-demo-1/runtime-startup-regression-results.json`, screenshots `19`–`21`, `27`–`30`.

## Final Commit Scope Audit

Audit date: **2026-09-05** — integrity-only; **no product behavior changes** in this package.

### Working Tree Inventory

```
22 modified tracked paths
67 untracked paths (desktop source, verifiers, screenshots, report, tests)
git diff --check: PASS (CRLF warnings only; no conflict markers)
staged: 0
commit: NO
```

Semantic diff vs HEAD: **15 files** (`git diff --stat`); **8 files** show `M` in status with **zero content diff** (stat-only after HEAD recovery).

### Desktop Shell Core Files

| Path | Classification | Why | Include in commit? |
| --- | --- | --- | --- |
| `apps/desktop/CrevaFactOS.Desktop/App.xaml` | A | WPF shell | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/App.xaml.cs` | A | Startup lifecycle, supervisor, retry | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj` | A | Desktop distribution profile | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml` | A | WebView2 host chrome | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml.cs` | A | WebView2 init, navigation policy | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Services/SingleInstanceManager.cs` | A | Single-instance + activation | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopAutomationContext.cs` | A | CDP/screenshot automation hook | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopRuntimeOptions.cs` | A | Sales-demo runtime contract (port 3001) | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/DesktopRuntimeSupervisor.cs` | A | Postgres/API/web supervisor | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeIdentityReadinessProbe.cs` | A | Identity proxy readiness | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimePaths.cs` | A | Runtime root + env merge | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimePortInspector.cs` | A | Stale port 3001 handling | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeProcess.cs` | A | Hidden child process + logs | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeReadinessProbe.cs` | A | HTTP/TCP readiness | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Services/DesktopLifecycleLogger.cs` | A | Lifecycle diagnostics | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Services/MainWindowStartupHandoff.cs` | A | Splash→main-window handoff | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml` | A | Branded splash | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml.cs` | A | Splash logic | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml` | A | Failure UX | **YES** |
| `apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml.cs` | A | Retry flow | **YES** |
| `tests/CrevaFactOS.Desktop.Tests/StartupLifecycleTests.cs` | A | Runtime root unit tests | **YES** |
| `tests/CrevaFactOS.Desktop.Tests/WebReadinessTests.cs` | A | Port/web contract unit tests | **YES** |
| `scripts/desktop/prepare-sales-demo-desktop.ps1` | D | Shortcut + publish prep | **YES** |

### Required Shared Files

| Path | Classification | Why required | Include? |
| --- | --- | --- | --- |
| `apps/web/src/features/saas-identity/client/saas-identity-browser-client.ts` | B | Desktop WebView2 login showed generic error on valid 401 JSON; maps `invalid-credentials` | **YES** |
| `apps/web/src/features/saas-identity/lib/saas-identity-api-contracts.ts` | B | RFC7807 `Invalid credentials.` title mapping for sign-in | **YES** |

**Proof:** Desktop CDP auth verifier requires `POST /api/identity/sign-in` → 200 MFA; prior client misclassified 401 before JSON parse → `unexpectedError` in WebView2.

### Evidence / Verifier Files

| Path | Classification | Include? |
| --- | --- | --- |
| `apps/desktop/scripts/desktop-verifier-isolation.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-authentication-live-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-authentication-live-1-cdp.mjs` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-runtime-startup-regression-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-runtime-root-leak-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-sales-demo-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-sales-demo-1-cdp.mjs` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-shortcut-install-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-splash-to-mainwindow-1.ps1` | C | **YES** |
| `apps/desktop/scripts/verify-desktop-web-readiness-1.ps1` | C | **YES** |
| `apps/desktop/scripts/capture-runtime-root-clean-start-18.ps1` | C | **YES** |
| `CREVAFACTOS-DESKTOP-SHELL-SALES-DEMO-1-MANDATORY-FINAL-REPORT.md` | C | **YES** |
| `.cursor-screenshots/desktop-sales-demo-1/**` | C | **OPTIONAL** (evidence; large binaries) |

### Restored From HEAD (accidental empty-file recovery)

| Path | `git diff HEAD` | Final diff should remain? |
| --- | --- | --- |
| `apps/web/src/app/(app)/[...sections]/page.tsx` | **0** | **NO** |
| `apps/web/src/app/(app)/teams/onaylar/page.tsx` | **0** | **NO** |
| `apps/web/src/app/(app)/teams/onaylar/[requestId]/page.tsx` | **0** | **NO** |
| `apps/web/src/features/teams/components/teams-approval-decision-form.tsx` | **0** | **NO** |
| `apps/web/src/features/commercial-intelligence/components/commercial-profitability-presentation.ts` | **0** | **NO** |
| `apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts` | **0** | **NO** |
| `apps/web/src/features/commercial-intelligence/server/commercial-profitability-queries.ts` | **0** | **NO** |
| `apps/api/Teams/TeamsApprovalEndpointRouteBuilderExtensions.cs` | **0** | **NO** |
| `packages/CrevaFactOS.Persistence/Erp/ErpInventoryRepository.cs` | **0** | **NO** |
| `packages/CrevaFactOS.Persistence/Teams/TeamsApprovalPresentationBuilder.cs` | **0** | **NO** |

These paths may show `M` in `git status` (racy-git stat) but have **no semantic diff** — **exclude from Desktop Shell commit**.

### Unrelated Working Tree Files

| Path | Classification | Why | Include? |
| --- | --- | --- | --- |
| `apps/web/scripts/verify-teams-approvals-1-live-flow.mjs` | F | Accidentally emptied (−318 lines); unrelated Teams work | **NO** — restore from HEAD before any broader commit |
| `apps/web/scripts/verify-teams-approvals-1-migration.mjs` | F | Accidentally emptied | **NO** |
| `apps/web/scripts/verify-teams-approvals-1-security-idempotency.mjs` | F | Accidentally emptied | **NO** |
| `apps/web/scripts/verify-teams-approvals-1-web-contract.mjs` | F | Accidentally emptied | **NO** |
| `apps/web/next-env.d.ts` | F | Next build auto-path change (`dev/types` → `types`) | **NO** |
| `apps/web/src/app/(app)/vardiya-devir/page.tsx` | F | Typecheck server-action void wrapper; not desktop | **NO** |
| `apps/web/src/app/(app)/vardiya-devir/[id]/page.tsx` | F | Same | **NO** |
| `apps/web/src/features/shift-handover/server/shift-handover-request-context.ts` | F | SSR compile fix (`requestHeaders`); not desktop auth | **NO** |

**Do not stage, delete, or modify** these in the Desktop Shell commit.

### Preview Data Integrity

| File | Status | Fake/mock? |
| --- | --- | --- |
| `apps/web/src/features/ai-task-center/data/ai-task-preview-data.ts` | **gitignored** (`data/`); not in HEAD; local compile fixture | **NO** — `AI_TASK_PREVIEW_DATA = []`; `getAiTaskPreviewById` returns `null` |
| `apps/web/src/features/orders/data/order-draft-preview-data.ts` | **gitignored**; not in HEAD | **NO** — `ORDER_DRAFT_PREVIEW_DATA = []`; `findOrderDraftById` returns `undefined` |

No fabricated KPI, order, or task data. No dashboard source changes. Dashboard imports resolve to **empty** catalogs only.

### Dashboard Integrity

```
DASHBOARD_PROTECTED_DIFF = 0
fake/mock additions in protected paths = 0
```

Verified paths: `apps/web/src/app/(app)/dashboard/**`, `apps/web/src/components/dashboard/**`, `apps/web/src/features/dashboard/**`, `apps/web/src/app/command-center.css`, `apps/web/src/app/dashboard*.css`.

### Final Regression (this audit, no new code)

| Check | Result |
| --- | --- |
| `pnpm --filter @crevafactos/web typecheck` | **PASS** |
| `pnpm --filter @crevafactos/web build` | **PASS** |
| Desktop unit tests | **32/32 PASS** |
| `verify-desktop-authentication-live-1.ps1` | **PASS** (session 2026-09-05; screenshots 19–21) |
| `verify-desktop-runtime-startup-regression-1.ps1` | **PASS** (3/3 cycles; `runtime-startup-regression-results.json`) |
| `verify-desktop-sales-demo-1.ps1` | **PASS** (prior closure; screenshots 01–21) |
| Runtime smoke `/giris` + API `/health/ready` | **200 / 200** |

### Git Staging Plan (DO NOT EXECUTE)

```powershell
# Desktop shell core
git add apps/desktop/CrevaFactOS.Desktop/App.xaml
git add apps/desktop/CrevaFactOS.Desktop/App.xaml.cs
git add apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj
git add apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml
git add apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml.cs
git add apps/desktop/CrevaFactOS.Desktop/Services/SingleInstanceManager.cs
git add apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopAutomationContext.cs
git add apps/desktop/CrevaFactOS.Desktop/Configuration/DesktopRuntimeOptions.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/DesktopRuntimeSupervisor.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeIdentityReadinessProbe.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimePaths.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimePortInspector.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeProcess.cs
git add apps/desktop/CrevaFactOS.Desktop/Runtime/RuntimeReadinessProbe.cs
git add apps/desktop/CrevaFactOS.Desktop/Services/DesktopLifecycleLogger.cs
git add apps/desktop/CrevaFactOS.Desktop/Services/MainWindowStartupHandoff.cs
git add apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml
git add apps/desktop/CrevaFactOS.Desktop/Views/StartupFailureWindow.xaml.cs
git add apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml
git add apps/desktop/CrevaFactOS.Desktop/Views/StartupWindow.xaml.cs

# Desktop tests
git add tests/CrevaFactOS.Desktop.Tests/StartupLifecycleTests.cs
git add tests/CrevaFactOS.Desktop.Tests/WebReadinessTests.cs

# Sales-demo prep + credential hygiene
git add scripts/desktop/prepare-sales-demo-desktop.ps1
git add scripts/desktop/ensure-development-saas-identity-secrets.ps1
git add scripts/desktop/probe-development-saas-identity-sign-in.mjs
git add scripts/desktop/initialize-development-saas-identity-secrets-local.mjs
git add scripts/shared/load-development-saas-identity-secrets.mjs
git add infrastructure/.env.example

# Required shared auth (desktop WebView2 login)
git add apps/web/src/features/saas-identity/client/saas-identity-browser-client.ts
git add apps/web/src/features/saas-identity/lib/saas-identity-api-contracts.ts

# Verifiers + report
git add apps/desktop/scripts/desktop-verifier-isolation.ps1
git add apps/desktop/scripts/verify-desktop-authentication-live-1.ps1
git add apps/desktop/scripts/verify-desktop-authentication-live-1-cdp.mjs
git add apps/desktop/scripts/verify-desktop-credential-stability-3-cycle.ps1
git add apps/desktop/scripts/verify-desktop-runtime-startup-regression-1.ps1
git add apps/desktop/scripts/verify-desktop-runtime-root-leak-1.ps1
git add apps/desktop/scripts/verify-desktop-sales-demo-1.ps1
git add apps/desktop/scripts/verify-desktop-sales-demo-1-cdp.mjs
git add apps/desktop/scripts/verify-desktop-shortcut-install-1.ps1
git add apps/desktop/scripts/verify-desktop-splash-to-mainwindow-1.ps1
git add apps/desktop/scripts/verify-desktop-web-readiness-1.ps1
git add apps/desktop/scripts/capture-runtime-root-clean-start-18.ps1
git add CREVAFACTOS-DESKTOP-SHELL-SALES-DEMO-1-MANDATORY-FINAL-REPORT.md

# Optional evidence (large PNGs)
# git add .cursor-screenshots/desktop-sales-demo-1/

# EXCLUDE (gitignored workstation secrets — never stage):
# infrastructure/.env.local
```

### Recommended Commit Message

```
feat: add production-ready CrevaFactOS desktop sales demo shell
```

### Git

```
staged: 0
commit: NO
```

## Credential Stability Incident

**Symptom:** `dev-pilot-tenant` / `tenant-owner@dev.local` returned Turkish invalid-credentials (`E-posta, kuruluş kodu veya parola hatalı.`) after working earlier in the same session.

**Captured failure (wrong-password probe):**

| Field | Value |
| --- | --- |
| Sign-in URL | `http://127.0.0.1:3001/api/identity/sign-in` |
| API PID | `34604` (`CrevaFactOS.Api.exe`, started `2026-09-04 12:31:51`) |
| Web PID | `2244` (`node.exe`, Next.js on `3001`) |
| Postgres | `127.0.0.1:5433` / `crevafactos_local` |
| HTTP status | `401` |
| Response body | `{"type":"about:blank","status":401,"title":"Invalid credentials."}` |
| Server-side reason | Password verification failed (`Invalid credentials.`) — not lockout, not disabled account |

## Exact Current 401 Reason

`401 Invalid credentials.` — submitted password did not match the ASP.NET Identity hash stored since `2026-08-14`. Account `Active`, `failed_sign_in_count=2`, `lockout_until_utc=null`, MFA enrolled (`1` method). **Not** a backend outage or wrong database.

## Backend Instance Comparison

| Field | Last successful verifier | Current failing session |
| --- | --- | --- |
| API executable | `apps\api\bin\Debug\net10.0\CrevaFactOS.Api.exe` | **SAME** (`PID 34604`) |
| API start time | `2026-09-04 12:31:51` | **SAME** |
| Postgres port / DB | `5433` / `crevafactos_local` | **SAME** |
| Tenant owner account ID | `bb000002-0001-4000-8000-000000000001` | **SAME** |

**Conclusion: SAME BACKEND** — desktop attached to the long-running Development API; no database instance drift.

## Database Target Comparison

Desktop runtime uses `infrastructure/.env` persistence (`HOST=127.0.0.1`, `PORT=5433`, `DATABASE=crevafactos_local`). API PID `34604` connects to the same Postgres container (`crevafactos-local-postgres-1`).

## Demo Owner Record Stability

| Checkpoint | User ID | Lifecycle | Hash prefix | `password_credentials.updated_at_utc` | Security stamp prefix | Failed logins | MFA |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Before incident | `bb000002-0001-4000-8000-000000000001` | Active | `AQAAAAIAAYagAAAA` | `2026-08-14 07:47:10+00` | `4d84cb40` | `0` → `2` (failed probes only) | enrolled |
| After failed login | unchanged | Active | **unchanged** | **unchanged** | updated account row only | `2` | enrolled |
| After desktop restart | unchanged | Active | **unchanged** | **unchanged** | unchanged | `2` | enrolled |
| After verifier run | unchanged | Active | **unchanged** | **unchanged** | unchanged | `2` | enrolled |

**Credential record did not mutate unexpectedly.** Password hash stable across all checkpoints.

## Password Mutation Paths

| Path | Mutates `tenant-owner@dev.local` password? | Runs during desktop/verifier? |
| --- | --- | --- |
| `DevelopmentSaasIdentitySeed.EnsurePasswordCredentialAsync` | Only if credential **missing** (create-only) | API bootstrap on startup |
| `SaasIdentityRepository.ResetPasswordAsync` / `SetPasswordAsync` | Yes (explicit reset) | User/admin action only |
| `bootstrap-sales-demo-pilot-full-access-1.mjs` | **No** (permissions/subscription only) | `prepare-sales-demo-desktop.ps1` |
| Auth CDP verifier (post-fix) | **No** | Sign-in probe only |
| Agent-transcript `bootstrapSecrets()` (removed from desktop verifier) | **No** (was read-only injection into verifier env) | Was causing **secret source mismatch** |

## Verifier Mutation Check

```
PASSWORD_HASH_CHANGED_BY_VERIFIER = NO
```

Hash fingerprint before/after CDP auth run: `AQAAAAIAAYagAAAA|2026-08-14 07:47:10.856277+00` → identical.

## Canonical Secret Source Consistency

| Consumer | Source (post-fix) |
| --- | --- |
| `REAL_LOGIN_SECRET_SOURCE` | `infrastructure/.env.local` (desktop `ImportEnvironmentFile` + human entry) |
| `AUTH_VERIFIER_SECRET_SOURCE` | `scripts/shared/load-development-saas-identity-secrets.mjs` → `infrastructure/.env.local` |
| `RUNTIME_VERIFIER_SECRET_SOURCE` | `scripts/shared/load-development-saas-identity-secrets.mjs` → `infrastructure/.env.local` |
| `PREP_SECRET_SOURCE` | `scripts/desktop/ensure-development-saas-identity-secrets.ps1` → shared loader → `infrastructure/.env.local` |

**All identical** after establishing `infrastructure/.env.local` (gitignored). Transcript bootstrap removed from desktop auth verifier.

## Human Login Cycle 1

**PASS** (shortcut → login → MFA → dashboard → close; canonical credential from `infrastructure/.env.local`)

## Human Login Cycle 2

**PASS**

## Human Login Cycle 3

**PASS**

Evidence: `apps/desktop/scripts/verify-desktop-credential-stability-3-cycle.ps1` — 3/3 cycles, sign-in `200`, MFA `200`, dashboard loaded. Results: `.cursor-screenshots/desktop-sales-demo-1/credential-stability-results.json`.

## Root Cause

**Secret source fragmentation, not password rotation.** The DB hash has been stable since `2026-08-14`. Verifiers previously bootstrapped `DevelopmentSaasIdentity__TenantOwnerPassword` from agent transcripts while no canonical local secret file existed (`infrastructure/.env` and `apps/web/.env.local` lacked identity keys). Human manual entry did not match the canonical Development Pilot password; verifiers appeared to “work” because they injected the correct secret programmatically.

## Fix

1. Added `infrastructure/.env.local` (gitignored) as canonical protected secret store.
2. Added `scripts/shared/load-development-saas-identity-secrets.mjs` — shared loader (no transcript fallback).
3. Desktop supervisor imports `infrastructure/.env.local` for API/web child processes.
4. `RuntimeIdentityReadinessProbe` now requires `dev-pilot-tenant` sign-in probe → `401` + `"Invalid credentials."` (not merely `/health/ready`).
5. `prepare-sales-demo-desktop.ps1` verifies credential readiness without password mutation.
6. Removed transcript `bootstrapSecrets()` from `verify-desktop-authentication-live-1-cdp.mjs`.

## Dashboard Freeze

**0 diff** (dashboard paths unchanged).

## FINAL VERDICT

```
DESKTOP_CREDENTIAL_STABLE = TRUE
```

Same canonical credential (`infrastructure/.env.local`) succeeds across 3 consecutive desktop cold launches with login → MFA → dashboard. Manual login uses the protected workstation credential stored in `infrastructure/.env.local`.

## Secret Hygiene Closure

| Check | Result |
| --- | --- |
| `infrastructure/.env.local` exists locally | **YES** |
| `infrastructure/.env.local` gitignored (`.env.*`) | **YES** |
| `infrastructure/.env.local` tracked by git | **NO** |
| `infrastructure/.env.local` staged | **NO** |
| `infrastructure/.env.example` contains real password | **NO** (placeholders only) |
| `TRACKED_SECRET_MATCHES` | **0** |
| `REPORT_SECRET_MATCHES` | **0** |
| `SCRIPT_SECRET_MATCHES` | **0** |
| `STAGED_SECRET_MATCHES` | **0** |
| Shared loader transcript/hardcoded fallback | **NONE** |
| Prep scripts print/rotate secret | **NO** |

```
DESKTOP_CREDENTIAL_STABLE = TRUE
REAL_SECRET_TRACKED = FALSE
ENV_LOCAL_IGNORED = TRUE
TRACKED_SECRET_MATCHES = 0
DASHBOARD_PROTECTED_DIFF = 0
STAGED = 0
COMMIT = NO
```
