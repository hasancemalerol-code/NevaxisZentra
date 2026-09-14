# Runbook: Migration Failure

## Symptoms

- API fails startup with EF migration or schema mismatch errors
- `/health/ready` returns unhealthy after deployment
- New UM-9 tables missing (`enterprise_reliability_policies`, etc.)

## Immediate actions

1. Capture migration error output from API startup logs
2. Verify target database version and applied migrations history
3. Do not run destructive SQL without backup

## Recovery

1. Take backup: `scripts/db/backup.ps1`
2. Run migration verifier: `npm run verify:enterprise-reliability-um9-migration`
3. Apply migrations using the standard database tool for the environment
4. Restart API and confirm `/health/ready`
5. Validate UM-9 schema tables exist in restored verification database via `npm run verify:database-restore-um9`

## Rollback

- If migration partially applied, restore from last known good backup
- Follow [deployment-rollback.md](./deployment-rollback.md) for application rollback

## Related golden flow

`FAILED_DEPLOYMENT_GOLDEN_FLOW` — build SHA and diagnostics remain available for rollback decisions.
