# Zentra Independent Audit — P1 Security Fix, Fresh-Clone Web Build Blocker, Release Gate Closure

Date: 2026-09-30
Worktree: `C:\Projects\NevaxisZentra-release-audit` (branch `zentra-release-audit-fix`)
Baseline: `origin/main` = `d392c1098ab66d4a3b7fd080d5b1a44d767f2d16`
Security fix source: `fb689e4` (`origin/claude/hopeful-pascal-t1rto7`), cherry-picked `-n` (uncommitted at audit start)

## 1. P1 — identity_subjects runtime enumeration

**Root cause.** Migration `20260808130000_AddProductionShiftResourceExecution` granted
`crevafactos_runtime` column-level `SELECT` on `identity_subjects` (no RLS on that table,
since identities are pre-tenant-context) so `ProductionOperatorRepository` could validate
facility membership. Any connection holding the runtime credential — not just the
legitimate code path — could then run `SELECT id, external_issuer, external_subject,
status FROM identity_subjects` and enumerate every tenant's identities.

**Fix (migration `20260930100000_HardenIdentitySubjectsRuntimeAccess`).**
- Added `public.is_active_facility_member_by_subject(tenant_id, facility_id,
  external_subject)`: `SECURITY DEFINER`, `SET search_path = pg_catalog, pg_temp`,
  returns a single `boolean`, requires `p_tenant_id = current_tenant_id()` (session GUC),
  no data columns exposed.
- `REVOKE ALL … FROM PUBLIC`, `GRANT EXECUTE … TO crevafactos_runtime` only.
- `REVOKE SELECT` (both the column-level grant and any table-level residue) on
  `identity_subjects` from `crevafactos_runtime`. `INSERT` (needed for signup) untouched.
- `ProductionOperatorRepository.IsFacilityMemberAsync` now calls the function via
  `dbContext.Database.SqlQuery<bool>` instead of joining `IdentitySubjects` directly.
- `AuthenticationMembershipVerifier` extended with direct-SELECT-denied assertions, a
  no-tenant-context-fails-closed assertion, and a cross-tenant-mismatch-fails-closed
  assertion (`SET`s tenant B, still asks about tenant A → false).

**Second, related finding — legacy tenant resolver.** Running the verifier against the
already-cherry-picked fix surfaced a second, independent enumeration path that predates
this audit entirely (confirmed via `git show origin/main:tools/…/AuthenticationMembershipVerifier.cs`):
`resolve_active_tenant(text)` — a `SECURITY DEFINER` function that returns
`tenant_id, tenant_key` for *any* tenant identifier, active-status only, with no tenant-context
check — had its `EXECUTE` grant to `crevafactos_runtime` revoked in migration
`20260804143343_AddAuthenticationMembershipFoundation`, then silently **re-granted** by
migration `20260814055239_AddSaasIdentityAccountAndSessionFoundation` for the SaaS sign-in
flow (`SaasIdentityRepository.SignInAsync`, the only real call site — a repo-wide grep for
`SaasSubscriptionRepository`/`SaasBillingRepository` call sites was a false positive from a
transient tool glitch, verified absent on re-check). This let the runtime role probe
tenant existence/activity by key or id outside any tenant context — a lower-severity but
real sibling of the identity_subjects bug.

Per user direction, fixed rather than deferred: migration
`20260930110000_HardenLegacyTenantResolverRuntimeAccess` adds
`public.resolve_active_tenant_for_signin(text)` (identical `SECURITY DEFINER` /
`search_path` contract to the original), grants it to `crevafactos_runtime`, and revokes
`EXECUTE` on the legacy `resolve_active_tenant(text)` from `crevafactos_runtime` for good.
`SaasIdentityRepository.SignInAsync` now calls the new function name. Verifier extended
with positive (`auth-a` by key and by id) and negative (`auth-unknown`, `auth-inactive`)
coverage for the replacement, plus the pre-existing legacy-resolver-denied assertion (which
now actually passes).

## 2. Security definer audit

| Function | Owner | `SECURITY DEFINER` | `search_path` | PUBLIC | runtime grant | Data returned |
|---|---|---|---|---|---|---|
| `is_active_facility_member_by_subject` | migration/admin role | yes | `pg_catalog, pg_temp` | revoked | EXECUTE only | boolean only |
| `resolve_active_tenant_for_signin` | migration/admin role | yes | `pg_catalog` | revoked | EXECUTE only | `tenant_id`, `tenant_key` for the tenant matching the caller-supplied identifier (same shape as the function it replaces — needed pre-authentication) |
| `resolve_active_tenant` (legacy) | migration/admin role | yes | `pg_catalog` | revoked (already) | **revoked** (was granted) | n/a — unreachable by runtime role now |

`crevafactos_runtime` role attributes confirmed on the audit database: no `SUPERUSER`, no
`BYPASSRLS`, no ownership of any object, `No inheritance` only. Neither new function is
schema-unqualified or reachable via a mutable `search_path`.

## 3. Reproduction (before/after)

Confirmed via the repo's own `AuthenticationMembershipVerifier` and a live psql session
against the audit database (`crevafactos_local` on the isolated container, see §9):

- **Without the fix** (verified on `origin/main` state, pre-cherry-pick): direct
  `SELECT id, external_issuer, external_subject, status FROM identity_subjects` as
  `crevafactos_runtime` succeeds.
- **With the fix**: `SELECT count(*) FROM identity_subjects` as `crevafactos_runtime` →
  `ERROR: permission denied for table identity_subjects`.
- **No tenant context**: `is_active_facility_member_by_subject` returns `false` (fails
  closed) rather than erroring or leaking a match.
- **Tenant A querying under Tenant B's session context**: returns `false` — cannot cross
  the boundary even by supplying Tenant A's id explicitly.
- **Legitimate facility-membership lookup** (matching tenant context, matching facility,
  matching subject): still resolves correctly — proven by
  `AuthenticationMembershipVerifier`'s existing positive-path assertions, unchanged.
- **Signup INSERT** into `identity_subjects`: untouched grant, still works (verifier's
  identity/membership seed fixtures insert successfully).

## 4. Gate results

| Gate | Result |
|---|---|
| `AuthenticationMembershipVerifier` | **PASS** (against isolated audit DB, port 5434) |
| Tenant isolation verifier | **PASS** |
| Permission authorization verifier | **PASS** |
| Direct `identity_subjects` SELECT (runtime role) | **DENIED** (permission denied) |
| RLS + FORCE RLS on `tenant_memberships`, `facility_memberships` | **CONFIRMED** (both `t`/`t`) |
| Migration discovery (`db:migrations:list`) | **PASS** — both new migrations discovered, applied, no orphans |
| ModelSnapshot consistency (`ef migrations has-pending-model-changes`) | **PASS** — "No changes have been made to the model since the last migration." |
| Idempotent migration script generation | **PASS** |
| Full solution build (`dotnet build -c Release`) | **PASS** — 0 warnings, 0 errors |
| TypeScript (`pnpm typecheck`) | **PASS** — 0 errors (after fresh-clone fix, §5) |
| Next.js production build (`pnpm build:web`) | **PASS** |
| Web tests (`pnpm test:web`, includes embedded Playwright visual QA) | **PASS** — 104/104 |
| Persistence tests | **PASS** — 1077/1077 |
| Smoke tests | **PASS** — 123/123 |
| Desktop tests | **PASS** — 51/51 |
| API `/health/live` | **200 Healthy** |
| API `/health/ready` | **200 Healthy** (after fixing the config-resolution bug in §7) |

## 5. Fresh-clone web build blocker

**Confirmed real**, but the independent audit's causal story needed correction in one
respect: the missing files were never a case of "committed then hidden by gitignore" —
they were **never committed at all**, because the overbroad `.gitignore` rule (`data/`,
no anchor, matches any directory named `data` at any depth) silently swallowed every
`git add` of these paths from day one. A normal developer's local worktree still builds
because the files exist on their disk (uncommitted); only a fresh clone/worktree — like
this one — lacks them entirely.

**Scope was larger than the one path named in the original report.** `pnpm typecheck`
surfaced four affected feature `data/` directories, not one:

| Feature | Missing file(s) | Status |
|---|---|---|
| `digital-workforce` | `digital-workforce-demo-catalog.ts` | recovered |
| `orders` | `order-draft-preview-data.ts` | recovered |
| `ai-task-center` | `ai-task-preview-data.ts` | recovered |
| `production-planning` | `production-plan-preview-data.ts` (sibling `production-plan-preview-repository.ts` was already tracked — force-added past the same bug previously) | recovered |

All four are synthetic demo/fixture data (fictional Turkish company/case names, `(demo)`/
`provenance: "demo"` markers throughout) — no secrets, no real customer data. Content was
read (with explicit per-file user authorization) from the untracked, on-disk copies in the
reference-only worktree `C:\Projects\NevaxisZentra` and written unmodified into this
worktree; nothing in that worktree was changed.

**Gitignore fix.** Removed the bare `data/` line from `.gitignore`. Verified no repo
convention actually depends on it (no root-level `./data` volume mount in
`infrastructure/compose.yaml`, no other reference anywhere in tracked docs/config); the
existing specific entries (`postgres-data/`, `redis-data/`, `rabbitmq-data/`,
`minio-data/`) already cover the real generated/runtime data directories. Nothing
customer-facing, secret, or generated was unignored by this change.

**Clean-source proof.** `git check-ignore` on the recovered files now exits 1 (not
ignored); `pnpm typecheck` → 0 errors; `pnpm build:web` → exit 0, full route manifest
generated. Both ran against tracked source only, from a working tree whose `git status`
shows exactly the files this report lists as staged (§8).

## 6. Persistence / Desktop / Smoke tests

`dotnet test NevaxisZentra.sln -c Release`, full run, no filters:

| Project | Result | Count | Matches prior baseline |
|---|---|---|---|
| Persistence | **PASS** | 1077 / 1077 | yes (1077) |
| Smoke | **PASS** | 123 / 123 | yes (123) |
| Desktop | **PASS** | 51 / 51 | yes (51) |

Zero failures across all three. No count decreased from the prior baseline; none needed
explanation.

## 7. Live API

`pnpm dev:api` against the isolated audit database:

- `/health/live` → `200 Healthy`.
- `/health/ready` → **initially 503 Unhealthy**. Root cause: the `development-pilot-bootstrap`
  health check depends on `DevelopmentPilotBootstrapDbContextFactory`, which builds its
  admin connection from `NEVAXIS_FACTOS__MIGRATIONS__*` environment variables **only** —
  it does not check `NEVAXIS_ZENTRA__MIGRATIONS__*` first the way the shared
  `LegacyEnvironmentVariables.GetSectioned` helper (used everywhere else, including the
  DatabaseTool CLI) does. It also silently rewrote a configured username of `NevaxisZentra`
  to `crevafactos` via a hardcoded `NormalizeMigrationUsername` special case. Since this
  worktree's isolated container has no `crevafactos` role at all (only `NevaxisZentra` and
  `crevafactos_runtime`), every bootstrap step failed with `28P01: password authentication
  failed for user "crevafactos"`, and the readiness health check reported unhealthy.
  This is the same root-cause *class* as §8's worktree DB isolation defect (inconsistent,
  duplicated env-var-prefix resolution across the codebase) surfacing in a different code
  path, so it was fixed under the same standing authorization: `DevelopmentPilotBootstrapDbContextFactory`
  now calls `LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", …)` like everything
  else, and the username rewrite was deleted. Re-verified after rebuild: `/health/live`
  → `200 Healthy`, `/health/ready` → **200 Healthy** (dev-pilot bootstrap log shows no
  further `28P01` authentication failures).
  - **Not fixed (out of scope, noted only):** the same narrow-prefix pattern
    (`NEVAXIS_FACTOS__…` only, no `NEVAXIS_ZENTRA__` precedence) also exists in
    `apps/api/Authentication/AuthenticationServiceCollectionExtensions.cs`,
    `apps/api/AiTasks/AiTaskDeliveryEndpointRouteBuilderExtensions.cs`,
    `apps/api/MarketRates/MarketRatesServiceCollectionExtensions.cs`, and
    `apps/api/SaasBilling/Payments/SaasPaymentServiceCollectionExtensions.cs`. None of
    these block a mandatory gate today; each should eventually be switched to the shared
    helper for consistency, but that is unrelated feature-area work beyond this audit's
    scope.

## 8. WORKTREE DB ISOLATION DEFECT / TOOLING RISK (finding, with fix)

**What happened.** This worktree (`…-release-audit`) never had its own
`infrastructure/.env`. Without one, `scripts/database/database.ps1` (and, transitively,
every `pnpm db:*` script) falls back to hardcoded defaults — host `127.0.0.1`, port
`5432`, database `crevafactos_local` — which is exactly where a **different** worktree's
PostgreSQL container (`nevaxisfactos-local-postgres-1`, backing `C:\Projects\NevaxisZentra`)
happens to be published. This worktree's own container is published on `5434`. Docker
health checks passed throughout (that container is healthy) — they just weren't the
container being talked to.

As a direct result, `pnpm db:migrations:apply` and the three DB verifiers were run once
against the wrong (other-worktree) database before this was caught (see full incident
timeline, evidence, and restoration proof in the session record — not reproduced here to
keep this report focused, but available on request). **Remediation, completed before any
further work**:
1. Recorded exact pre-rollback state on the other database (migration history, grants on
   `identity_subjects`/`resolve_active_tenant`, active connections — none).
2. Rolled back *only* the two corrective migrations there via a targeted
   `dotnet ef database update 20260921090000_AddOperationalValueFoundation` (never the
   destructive `migrations-rollback`/`database update 0` path).
3. Proved restoration: migration history back to `…_AddOperationalValueFoundation`;
   `identity_subjects` grants back to the original 4-column `SELECT` + 5-column `INSERT`;
   `resolve_active_tenant` EXECUTE restored; both new functions absent; zero active
   connections throughout.
4. Created `infrastructure/.env` for this worktree, pointed at its actual container
   (port `5434`, database `crevafactos_local`, admin role `NevaxisZentra` — this
   container has no separate `crevafactos` role; migrations were originally bootstrapped
   directly as the compose superuser here).
5. Applied the two migrations and re-ran all three verifiers against the *correct*
   database — all PASS (§4).

**Tooling fix (so this cannot silently recur).** `Assert-PostgreSqlContainerHealthy` in
`scripts/database/database.ps1` now also calls a new `Assert-ResolvedPortMatchesCompose`:
it resolves the actual `docker compose port postgres 5432` for *this* compose file and
compares it against the effective `PERSISTENCE`/`MIGRATIONS` port (checking
`NEVAXIS_ZENTRA__` before `NEVAXIS_FACTOS__`, matching the C# resolution order) — throwing
a clear, actionable error before any DB command runs if they don't match. Verified: passes
silently under the correct `.env`; throws
`Resolved MIGRATIONS port (5432) does not match this worktree's compose-published
PostgreSQL port (5434)…` when the mismatch is deliberately reproduced.

## 9. Isolated audit database identity (verified before every gate in §4/§6/§7)

host=`127.0.0.1`, port=`5434`, database=`crevafactos_local`, container=
`nevaxiszentra-local-postgres-1`, migration/admin role=`NevaxisZentra`, runtime role=
`crevafactos_runtime`. Confirmed via `pnpm db:config` and direct `docker exec … psql`
identity queries before use.

## 10. Deliberately not run / not claimed

Per instruction, these remain separate and are **not** marked PASS by this audit:

- Manual full customer journey
- Veri Aktarım Merkezi import / re-import
- Fresh MSI install
- Cross-tenant browser/UI test
- Performance review

## 11. Files changed (staged for review, not yet committed)

- `.gitignore` — removed overbroad `data/` rule
- `packages/NevaxisZentra.Persistence.Migrations/Migrations/20260930100000_HardenIdentitySubjectsRuntimeAccess.{cs,Designer.cs}`
- `packages/NevaxisZentra.Persistence.Migrations/Migrations/20260930110000_HardenLegacyTenantResolverRuntimeAccess.{cs,Designer.cs}`
- `packages/NevaxisZentra.Persistence/Production/ProductionOperatorRepository.cs`
- `packages/NevaxisZentra.Persistence/SaasIdentity/SaasIdentityRepository.cs`
- `packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapDbContextFactory.cs`
- `tools/NevaxisZentra.DatabaseTool/AuthenticationMembershipVerifier.cs`
- `scripts/database/database.ps1`
- `apps/web/src/features/digital-workforce/data/digital-workforce-demo-catalog.ts` (new)
- `apps/web/src/features/orders/data/order-draft-preview-data.ts` (new)
- `apps/web/src/features/ai-task-center/data/ai-task-preview-data.ts` (new)
- `apps/web/src/features/production-planning/data/production-plan-preview-data.ts` (new)
- This report

Not committed: `infrastructure/.env` (gitignored, machine-local by design), migration
`artifacts/database/NevaxisZentra.idempotent.sql` (gitignored build artifact).
