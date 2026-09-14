# Runbook: API Down

## Symptoms

- `GET /health/live` or `GET /health/ready` returns non-200 or times out
- Web and desktop clients cannot reach `/api/*`
- Edge agents report `connectivityStatus: unavailable`

## Immediate actions

1. Confirm liveness: `curl http://127.0.0.1:5000/health/live`
2. Confirm readiness: `curl http://127.0.0.1:5000/health/ready`
3. Check API process logs for startup exceptions or port binding failures
4. Verify PostgreSQL and RabbitMQ are healthy if readiness depends on persistence

## Recovery

1. Restart the API host process or container
2. Wait for `/health/ready` to return 200
3. Validate anonymous version endpoint: `GET /api/system-status/version`
4. Validate authenticated workspace: `GET /api/system-status/workspace` with dev auth headers
5. Run `npm run verify:enterprise-reliability-um9-runtime` from `apps/web`

## Escalation

- If restart fails twice, capture diagnostic bundle via `GET /api/system-status/diagnostics`
- Review recent deployment and rollback per [deployment-rollback.md](./deployment-rollback.md)

## Related golden flow

`API_RESTART_GOLDEN_FLOW` — API returns healthy probes and version metadata after restart.
