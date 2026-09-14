# Runbook: Edge Agent Offline

## Symptoms

- Edge agent heartbeat stops updating `edge_agent_registrations`
- System status `operatingMode` may become `degraded-buffering`
- Local edge buffer backlog grows (`bufferBacklogCount`)

## Immediate actions

1. Check edge agent process on plant network
2. Review edge agent logs for API connectivity failures
3. Inspect local buffer health via `EdgeAgentHealthReporter` log entries

## Recovery

1. Restore network path from edge host to API (`NEVAXIS_FACTOS_API_URL`)
2. Restart edge agent: `dotnet run --project apps/edge-agent`
3. Confirm heartbeat posts to `/api/system-status/edge/heartbeat`
4. Allow `EdgeDeliveryWorker` to drain buffered events in order
5. Run reconciliation if backlog persisted: `POST /api/system-status/reconcile`
6. Run `npm run verify:enterprise-reliability-um9-runtime`

## Buffer rules

- Critical and safety events cannot be silently dropped
- Disposable telemetry may be dropped only when buffer capacity is exceeded
- Duplicate events are blocked by `IdempotencyKey`

## Related golden flow

`EDGE_OFFLINE_GOLDEN_FLOW` and `BACKLOG_RECOVERY_GOLDEN_FLOW`
