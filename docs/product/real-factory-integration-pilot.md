# Real Factory Integration & Pilot

Productionization layer after MP1 / MP2 / MP3. This is **not** MP4.

## Architecture

```
SOURCE SYSTEM → CONNECTOR → RAW INGESTION → NORMALIZATION → CANONICAL FACTORY EVENT
  → OPERATIONAL STATE → CONTEXT → INTELLIGENCE → DIGITAL WORKFORCE
```

Reuses:

- Integration Spine (`IntegrationConnector`, mapping versions, health)
- Factory Event Spine (canonical event ingest + idempotency)
- MP1 Mixed Production canonical events
- MP2 Throughput Intelligence
- MP3 Throughput Optimizer (DW-FACT-008)
- Context & Knowledge, Model Gateway, Digital Workforce approval runtime

## Synthetic Pilot

Canonical fixture:

- `FACTORY-PILOT-01`
- `LINE-PILOT-01`
- `PILOT-PLC-SOURCE` (machine/PLC-like)
- `PILOT-MES-SOURCE` (MES/operator-like)

Golden acceptance: `apps/web/scripts/verify-real-factory-pilot-runtime.mjs`

Status label: **SYNTHETIC PILOT PASS** — not REAL FACTORY VERIFIED.

## Shadow Mode

Rollout: OBSERVE ONLY → SHADOW INTELLIGENCE → RECOMMENDATION → CONTROLLED APPROVAL → LIMITED ACTION → VERIFIED OPERATION

Shadow blocks Class C/D physical/process-changing actions until explicitly released.

## Action Safety Classes

| Class | Examples | Default autonomous |
|-------|----------|-------------------|
| A | notifications, guidance | allowed |
| B | tasks, replenishment requests | policy-dependent |
| C | reassignment, redistribution | approval required |
| D | PLC write, parameter change | forbidden |

## Real Factory Onboarding

CREATE FACILITY → AREA → LINE → STATIONS → RESOURCES → CONNECT → DISCOVER → MAP → VALIDATE → SHADOW
