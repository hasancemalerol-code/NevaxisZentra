## STATUS

BASELINE_RUNTIME_RESTORED

## Root Cause

Authenticated `/dashboard` redirected to `/abonelik-erisimi?reason=service-unavailable` because the web subscription gate could not reach the CrevaFactOS API.

Evidence:

1. **Runtime before recovery**
   - Web was listening on `:3001` (stale process PID 40276).
   - **No process was listening on `:5000`** — the API was down.
   - Docker Desktop engine was unhealthy (`dockerDesktopLinuxEngine` HTTP 500 / pipe missing).
   - PostgreSQL on `:5432`/`:5433` accepted the port but connections timed out.
   - Prior API start attempt failed during `DevelopmentPilotBootstrapService.BootstrapAsync` with `NpgsqlException: The operation has timed out`.

2. **Failure mechanism (code path)**
   - `apps/web/src/features/saas-subscription/server/require-subscription-access.ts` calls `fetchSubscriptionAccessStatus()`.
   - `fetchSubscriptionAccessStatus()` uses `fetchTrustedApi("/api/saas-subscription/access-status?routePath=...")` in `saas-subscription-api-client.ts`.
   - When the API is unreachable, the client returns `status: "failure"` with `code: "network-error"` / message `"API bağlantısı kurulamadı."`.
   - `classifySubscriptionApiFailure()` maps non-401/403 failures to `service-unavailable`, producing redirect `/abonelik-erisimi?reason=service-unavailable`.

3. **Exact failing request**
   - `GET http://127.0.0.1:5000/api/saas-subscription/access-status?routePath=/dashboard` (proxied from web SSR via `fetchTrustedApi`)
   - **Status:** connection refused / network failure (API not listening)
   - **Not** a 404, 500, schema mismatch, entitlement denial, or auth failure.

## Runtime Before

| Component | State |
| --- | --- |
| Web PID | 40276 (`:3001`) |
| API PID | none (`:5000` closed) |
| `GET /health/ready` | unreachable |
| Subscription endpoint | FAIL — API unreachable → `service-unavailable` gate |

## Recovery Actions

1. Verified HEAD `0ca1bf061ba0d2e385cc711d2804ed68f2a65c1a` (unchanged).
2. Restarted Docker Desktop (`Docker Desktop.exe`); confirmed daemon ready (`docker_ready=29.6.2`).
3. Ran `pnpm infra:start` — `crevafactos-local-postgres-1` and `crevafactos-local-rabbitmq-1` running.
4. Confirmed DB state: **109 applied EF migrations**, user-visible table count 282.
5. Stopped stale web process (PID 40276).
6. Built and started API via canonical command `pnpm dev:api` (loads `infrastructure/.env`, `dotnet run --launch-profile http`).
7. Confirmed `GET http://127.0.0.1:5000/health/ready` → **200**.
8. Started web via `pnpm dev:web` with `CREVAFACTOS_API_BASE_URL=http://127.0.0.1:5000` (`.env.local` already present).
9. Ran `node apps/web/scripts/verify-local-subscription-recurrence-closure-2.mjs` — all subscription/access-status checks **PASS** including `/api/saas-subscription/access-status?routePath=/dashboard` → 200, `allowed: true`.
10. Captured authenticated dashboard browser proof (Chrome headless sign-in + MFA + `/dashboard` navigation) — no subscription gate redirect; AI Görev Merkezi and Teams visible.

## Database

- **Schema/history mismatch from abandoned Shift Handover migration:** already repaired in prior task (`20260901221521_AddShiftHandoverFoundation` removed; EF count 109).
- **No additional DB repair required** for this recovery — Postgres became reachable after Docker restart; API bootstrap completed successfully against current 109-migration schema.
- Pilot subscription/entitlement rows intact; `module.dashboard` access allowed for active dev-pilot tenant.

## Runtime After

| Check | Result |
| --- | --- |
| API PID | 40080 (`127.0.0.1:5000`) |
| Web PID | 34780 (`:3001`) |
| API health | **200** (`GET /health/ready`) |
| Subscription endpoint | **PASS** (`access-status?routePath=/dashboard` → 200, `allowed: true`) |
| `/dashboard` (authenticated) | **PASS** — no `/abonelik-erisimi?reason=service-unavailable`; command center, AI Görev Merkezi nav, Teams nav present |

## Dashboard Lock

dashboard source files changed = **0**

Verified no diffs under:
- `apps/web/src/app/(app)/dashboard/**`
- `apps/web/src/components/dashboard/**`
- `apps/web/src/features/dashboard/**`
- `apps/web/src/app/command-center.css`
- `apps/web/src/app/dashboard*.css`

No navigation or product module changes.

## Screenshot

`.cursor-screenshots/baseline-runtime-recovery-1-dashboard-authenticated.png` (466,890 bytes)

## Git

```
git rev-parse HEAD
0ca1bf061ba0d2e385cc711d2804ed68f2a65c1a

git status --short
?? .cursor-screenshots/capture-baseline-runtime-recovery-1-dashboard.mjs
?? CREVAFACTOS-SHIFT-HANDOVER-2-DATABASE-ROLLBACK-CLEANUP-1-MANDATORY-FINAL-REPORT.md
?? CREVAFACTOS-BASELINE-RUNTIME-RECOVERY-1-MANDATORY-FINAL-REPORT.md

git diff --check
(clean — no staged/unstaged tracked changes)

staged = 0
commit = NO
```

## FINAL VERDICT

**BASELINE_RUNTIME_RESTORED**
