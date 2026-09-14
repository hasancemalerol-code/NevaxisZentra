# Runbook: Connector Down

## Symptoms

- Integration health shows circuit state `open`
- Connector outcome failures accumulate (`integration.circuit-open`)
- Technical alert `connector.circuit-open` appears in system status

## Immediate actions

1. Open system status workspace and inspect `integrations` health
2. Record connector outcome diagnostics via `POST /api/system-status/connectors/outcome`
3. Check external ERP or file-watch endpoint availability

## Recovery

1. Resolve upstream dependency (credentials, network, rate limits)
2. Wait for circuit cooldown (`DefaultCooldown = 5 minutes`) or manually reset connector state after fix
3. Post successful connector outcome to close circuit
4. Confirm integration UI status returns to `Bağlı`
5. Run `npm run verify:enterprise-reliability-um9-failure-injection`

## Bounded failure policy

- Circuit opens after 5 consecutive failures (`IntegrationCircuitBreakerEngine.DefaultFailureThreshold`)
- Retries are bounded to 5 attempts (`BoundedRetryPolicyEngine.MaxAttempts`)

## Related golden flow

`CONNECTOR_FAILURE_GOLDEN_FLOW` — circuit breaker opens, blocks unsafe execution, and recovers after success.
