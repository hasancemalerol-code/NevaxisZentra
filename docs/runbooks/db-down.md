# Runbook: Database Down

## Symptoms

- `/health/ready` may fail when persistence health checks are enabled
- System status shows `operatingMode: degraded-read-only`
- Mutations blocked by `DegradedModeEngine.BlocksUnsafeMutation`

## Immediate actions

1. Check PostgreSQL container: `docker ps --filter name=postgres`
2. Verify connectivity: `docker exec NevaxisFactOS-local-postgres-1 pg_isready -U crevafactos -d crevafactos_local`
3. Review PostgreSQL logs for disk, connection, or corruption errors

## Recovery

1. Restart PostgreSQL service or container
2. Confirm database accepts connections
3. Run pending migrations if schema drift is suspected
4. Validate read-only degraded mode clears: workspace `operatingMode` should not remain `degraded-read-only` once DB is healthy
5. Run `npm run verify:enterprise-reliability-um9-data-integrity`

## Data protection

- Take backup before destructive recovery: `scripts/db/backup.ps1`
- Verify restore path: `npm run verify:database-restore-um9`

## Related golden flow

`DB_OUTAGE_GOLDEN_FLOW` — platform enters degraded read-only mode and recovers without duplicate business effects.
