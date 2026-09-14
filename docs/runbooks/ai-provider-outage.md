# Runbook: AI Provider Outage

## Symptoms

- AI-dependent features degrade or queue work
- System status dependency `aiProvider` shows `unavailable` or `degraded`
- Digital workforce or AI task flows stall without hard platform failure

## Immediate actions

1. Check system status workspace `dependencies.aiProvider`
2. Review AI provider health dashboards and rate limits
3. Confirm non-AI operational flows still function (read-only degraded mode if DB healthy)

## Recovery

1. Restore AI provider connectivity or switch to fallback model configuration
2. Resume queued AI tasks after provider health returns
3. Validate metrics endpoint: `GET /api/system-status/metrics`
4. Run `npm run verify:enterprise-reliability-um9-runtime` for `AI_OUTAGE_GOLDEN_FLOW`

## Operating constraints

- Platform remains available for non-AI mutations when only AI provider is degraded
- Unsafe mutations remain blocked if database or integration circuits are also unhealthy

## Related golden flow

`AI_OUTAGE_GOLDEN_FLOW`
