# RPO / RTO — Enterprise Reliability (UM-9)

Recovery objectives for NevaxisFactOS enterprise reliability capabilities.

## Definitions

| Term | Definition |
|------|------------|
| **RPO** (Recovery Point Objective) | Maximum acceptable data loss measured in time |
| **RTO** (Recovery Time Objective) | Maximum acceptable downtime to restore service |

## Targets by component

| Component | RPO | RTO | Notes |
|-----------|-----|-----|-------|
| PostgreSQL (primary) | 15 minutes | 60 minutes | pg_dump backups + WAL archiving in production |
| API | 0 (stateless) | 15 minutes | Redeploy or restart; no business data on API host |
| Edge buffer | 0 for critical events | 30 minutes | Local buffer drains on reconnect; critical events never silently dropped |
| Integration connectors | 5 minutes | 30 minutes | Circuit breaker cooldown; replay from quarantine |
| AI provider | N/A (non-authoritative) | 60 minutes | AI tasks queue; core ops continue in degraded mode |
| RabbitMQ | 5 minutes | 30 minutes | Outbox replay after broker recovery |

## Local development

| Component | RPO | RTO |
|-----------|-----|-----|
| Local PostgreSQL | Last backup (`scripts/db/backup.ps1`) | Container restart (~2 minutes) |
| Local API | N/A | Process restart (~1 minute) |
| Edge agent buffer | Last persisted JSON snapshot | Agent restart (~1 minute) |

## Verification

- Backup golden flow: `npm run verify:database-restore-um9`
- Full recovery golden flow: `npm run verify:enterprise-reliability-um9-runtime`

## Recovery sequence (full platform)

1. Restore database from latest verified backup (RPO bound)
2. Restart API and confirm `/health/ready` (RTO bound)
3. Restart edge agents and drain buffers
4. Run reconciliation for any backlog (`POST /api/system-status/reconcile`)
5. Verify `operatingMode: fully-operational`
6. Run full UM-9 verification suite

## Assumptions

- Production backups are taken on a schedule ≤ RPO interval
- Backup verification runs are recorded via `POST /api/system-status/backup/verify`
- Runbooks in `docs/runbooks/` are followed for each failure class
