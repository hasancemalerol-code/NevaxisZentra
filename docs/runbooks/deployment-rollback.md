# Runbook: Deployment Rollback

## Symptoms

- Post-deploy `/health/ready` fails
- New endpoints return 500 errors
- Build SHA in `/api/system-status/version` does not match expected release

## Immediate actions

1. Capture current build SHA from `GET /api/system-status/version`
2. Export diagnostics: `GET /api/system-status/diagnostics`
3. Stop further rollout traffic to the failing revision

## Rollback steps

1. Redeploy previous known-good API and web artifacts
2. Confirm `/health/live` and `/health/ready` return 200
3. If schema changed, restore database from pre-deploy backup before restarting API
4. Run full UM-9 verification suite:
   - `npm run verify:enterprise-reliability-um9-migration`
   - `npm run verify:enterprise-reliability-um9-runtime`
   - `npm run verify:enterprise-reliability-um9-security`

## Verification

- `operatingMode` should return to `fully-operational` when dependencies are healthy
- No duplicate replay audits for the same `replayIdempotencyKey`
- Edge agents reconnect and drain backlog

## Related golden flow

`FAILED_DEPLOYMENT_GOLDEN_FLOW` and `FULL_RECOVERY_GOLDEN_FLOW`
