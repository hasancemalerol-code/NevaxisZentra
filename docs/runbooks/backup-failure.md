# Runbook: Backup Failure

## Symptoms

- `scripts/db/backup.ps1` exits with error or produces tiny backup file
- `backup_verification_runs` status not `completed`
- Restore verifier fails golden flow

## Immediate actions

1. Confirm PostgreSQL container is running
2. Verify credentials: user `crevafactos`, database `crevafactos_local`
3. Check disk space on host and inside container

## Recovery

1. Re-run backup: `pwsh scripts/db/backup.ps1`
2. Record verification via API: `POST /api/system-status/backup/verify`
3. Run restore verifier: `npm run verify:database-restore-um9`
4. Store backup artifact reference in operational log

## Validation checklist

- Backup file size > 1 KB
- Backup contains `CREATE TABLE` statements
- Restore to `crevafactos_um9_restore_verify` succeeds
- UM-9 tables present after restore

## Related golden flow

`BACKUP_RESTORE_GOLDEN_FLOW`
