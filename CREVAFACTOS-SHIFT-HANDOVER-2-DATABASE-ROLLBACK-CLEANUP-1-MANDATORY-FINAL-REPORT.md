## STATUS

PASS (database cleanup complete)

API/app live regression **BLOCKED** at validation time — Docker Desktop engine returned HTTP 500 (`dockerDesktopLinuxEngine`) after cleanup; PostgreSQL/API could not be reached for restart checks.

## Starting HEAD

`0ca1bf061ba0d2e385cc711d2804ed68f2a65c1a`

Pre-flight: `git status --short` was **EMPTY**.

## Database Before Cleanup

**Migration history**
- `"__EFMigrationsHistory"."MigrationId" = 20260901221521_AddShiftHandoverFoundation` — **present**

**Tables found**
- `factory_shift_handovers` (5 rows)
- `factory_shift_handover_items`
- `factory_shift_handover_events`

**Permissions found**
| id | key | created_at_utc |
|----|-----|----------------|
| …0a2 | shift-handover.acknowledge | 2026-09-01 18:00:00+00 (pre-existing) |
| …0a3 | shift-handover.manage | 2026-09-02 00:00:00+00 (migration-added) |
| …0b0 | shift-handover.read | 2026-09-01 18:00:00+00 (pre-existing) |
| …0b1 | shift-handover.prepare | 2026-09-01 18:00:00+00 (pre-existing) |

**RLS policies** (8 on `factory_shift_*` tables): `rls_sh_*`, `rls_shi_*`, `rls_she_*`

**FK dependencies**
- Only internal `factory_shift_*` child→parent and facility FKs; no unrelated modules referenced `factory_shift_*`.

**Subscription entitlements (live-closure side effect)**
- `module.shift-workforce` had been changed to `unlimited / true` during abandoned package live testing (not in migration source).

**Canonical tooling note**
- EF rollback **not used** — migration source absent from `0ca1bf0` baseline.

## Cleanup Actions

Executed one scoped transaction via Docker Postgres (`crevafactos` / `crevafactos_local`):

```sql
BEGIN;

-- Reverse migration-granted tenant_role_permissions
DELETE FROM tenant_role_permissions trp
USING permission_definitions pd
WHERE trp.permission_definition_id = pd.id
  AND pd.key = 'shift-handover.read'
  AND EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    JOIN permission_definitions source_perm
      ON source_perm.id = existing.permission_definition_id
     AND source_perm.key = 'shift-workforce.read'
    WHERE existing.tenant_id = trp.tenant_id
      AND existing.tenant_role_id = trp.tenant_role_id
  );

DELETE FROM tenant_role_permissions trp
USING permission_definitions pd
WHERE trp.permission_definition_id = pd.id
  AND pd.key = 'shift-handover.manage';

DELETE FROM tenant_role_permissions trp
USING permission_definitions pd
WHERE trp.permission_definition_id = pd.id
  AND pd.key = 'shift-handover.acknowledge'
  AND EXISTS (
    SELECT 1 FROM tenant_role_permissions existing
    JOIN permission_definitions source_perm
      ON source_perm.id = existing.permission_definition_id
     AND source_perm.key = 'shift-workforce.read'
    WHERE existing.tenant_id = trp.tenant_id
      AND existing.tenant_role_id = trp.tenant_role_id
  );

-- Remove migration-added permission only
DELETE FROM permission_definitions
WHERE key = 'shift-handover.manage'
  AND id = '10000000-0000-4000-8000-0000000000a3';

-- Restore dev subscription entitlements changed during abandoned live closure
UPDATE saas_commercial_plan_version_entitlements
SET semantics = 'unavailable', boolean_value = false
WHERE entitlement_definition_id = 'bb000001-00e7-4000-8000-0000000000e7'
  AND semantics = 'unlimited' AND boolean_value = true;

DROP TABLE IF EXISTS factory_shift_handover_events;
DROP TABLE IF EXISTS factory_shift_handover_items;
DROP TABLE IF EXISTS factory_shift_handovers;

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260901221521_AddShiftHandoverFoundation';

COMMIT;
```

**Transaction results:** `DELETE 8` read grants, `DELETE 4` manage grants, `DELETE 8` acknowledge grants, `DELETE 1` manage permission, `UPDATE 2` subscription rows, `DROP TABLE` ×3, `DELETE 1` migration row.

## Database After Cleanup

| Check | Result |
|-------|--------|
| Migration history row | **absent** |
| `factory_shift_handovers` | **absent** |
| `factory_shift_handover_items` | **absent** |
| `factory_shift_handover_events` | **absent** |
| `shift-handover.manage` permission | **absent** (0 rows) |
| Pre-existing permissions | **retained**: read, acknowledge, prepare (2026-09-01 timestamps) |
| `module.shift-workforce` entitlements | **restored** to `unavailable / false` |
| Applied EF migration count (`pnpm db:state`) | **109** (was 110) |
| User-visible table count | **282** (unchanged) |

## Unrelated Data Safety

| Object | Status |
|--------|--------|
| `ai_tasks` | **89 rows** — intact |
| `teams_notifications` | **22 rows** — intact |
| Legacy `shift_handovers` | **7 rows** — untouched (pre-existing orphan schema, not from abandoned migration) |
| `teams_shift_handovers` | present — untouched |
| `production_shift_handovers` | present — untouched |
| Other `__EFMigrationsHistory` rows | untouched |

Orphaned runtime data from live testing (not schema): 4 `ai_tasks` with `source_module = shift-handover` and 5 `teams_notifications` with `factory-shift-handover*` types remain; these do not block reimplementation and were not dropped to avoid touching unrelated notification/task history.

## API

| Step | Result |
|------|--------|
| build | `dotnet build apps/api` — **PASS** (after stopping stale PID 8840) |
| listener | **none** on `:5000` after restart attempt |
| `/health/ready` | **not reached** — API bootstrap failed: `NpgsqlException: The operation has timed out` (PostgreSQL unreachable) |

Restart attempt: stopped stale API (PID 8840), build succeeded, `pnpm dev:api` failed during `DevelopmentPilotBootstrapService` DB connect. Subsequent Docker commands returned engine HTTP 500.

## Regression

| Surface | Result |
|---------|--------|
| `/dashboard` | **NOT RUN** — API/DB unavailable; web listener on `:3001` may be stale |
| AI Görev Merkezi | **NOT RUN** — API unavailable |
| Teams | **NOT RUN** — API unavailable |

## Dashboard Lock

dashboard files changed = **0**

No source files modified; only report artifact added.

## Git

```
git diff --check
(clean)

git status --short
?? CREVAFACTOS-SHIFT-HANDOVER-2-DATABASE-ROLLBACK-CLEANUP-1-MANDATORY-FINAL-REPORT.md

staged = 0
commit = NO
```

## FINAL VERDICT

**READY_FOR_SHIFT_HANDOVER_REIMPLEMENTATION**

Database artifacts from `20260901221521_AddShiftHandoverFoundation` are fully removed and migration history is consistent with the `0ca1bf0` codebase (109 applied migrations). Re-run API restart and `/dashboard` + AI Görev Merkezi + Teams smoke checks once Docker Desktop/PostgreSQL is healthy again.
