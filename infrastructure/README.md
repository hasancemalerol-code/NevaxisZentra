# CrevaFactOS Local Development Infrastructure

FOUND-02 provides only the local PostgreSQL and RabbitMQ development dependencies. It does not create database schemas, migrations, queues, exchanges, policies, connectors, or product behavior.

## Prerequisites

- Docker Desktop with Docker Compose
- PowerShell 7 or Windows PowerShell 5.1
- pnpm configured according to the root repository prerequisites

Docker Desktop must be running before lifecycle or verification commands are used.

## Environment Setup

The Compose file has safe local defaults. To customize ports or credentials:

```powershell
Copy-Item infrastructure/.env.example infrastructure/.env
```

`infrastructure/.env` is ignored by Git. Values in `.env.example` are development-only examples and must never be reused for staging or production.

## Lifecycle Commands

Run from the repository root:

```powershell
pnpm infra:config
pnpm infra:start
pnpm infra:status
pnpm infra:verify
pnpm infra:logs
pnpm infra:stop
```

`infra:stop` stops containers without deleting their named volumes.

The following command is intentionally destructive and must be invoked explicitly:

```powershell
pnpm infra:reset
```

It removes the local containers, network, and both named data volumes. It is never part of normal shutdown.

## Local Endpoints

| Service | Host | Default port | Local identity |
| --- | --- | ---: | --- |
| PostgreSQL | `localhost` | `5432` | Database `crevafactos_local`, user `crevafactos` |
| RabbitMQ AMQP | `localhost` | `5672` | User `crevafactos` |
| RabbitMQ Management | `http://localhost:15672` | `15672` | User `crevafactos` |

The example password is `local_dev_only_change_me`. It is intentionally non-secret and suitable only for an isolated developer workstation.

## Health Verification

`pnpm infra:verify` waits up to 120 seconds for:

- PostgreSQL container health via `pg_isready`
- RabbitMQ container health via `rabbitmq-diagnostics ping`
- RabbitMQ management API health at `/api/health/checks/local-alarms`

The verifier fails clearly if Docker Desktop is unavailable, a service is absent or unhealthy, credentials do not match, or the management endpoint cannot be reached.

Application `/health/live` and `/health/ready` endpoints remain independent of these development services because no application uses PostgreSQL or RabbitMQ yet.

## Application Configuration Binding

`CrevaFactOS.Configuration` provides typed PostgreSQL and RabbitMQ endpoint options. It binds the `Infrastructure` configuration section and the documented `CREVAFACTOS_*` environment variables.

API, Worker, and Edge Agent currently register these options as optional. They neither require credentials nor open connections. A future package must explicitly declare a dependency before startup validation is activated.

## Port Conflicts

If `5432`, `5672`, or `15672` is already in use:

1. Copy `.env.example` to `.env`.
2. Change the corresponding host-side port.
3. Run `pnpm infra:config` to inspect the resolved configuration.
4. Restart with `pnpm infra:start`.

Container ports remain unchanged; only workstation ports are overridden.

## Current Boundary

- PostgreSQL is empty except for its standard initialization metadata.
- RabbitMQ uses only the minimal default local user and `/` virtual host created by the official image.
- No business tables, migrations, queues, exchanges, users, additional vhosts, or policies are created.
- No application database or RabbitMQ client package is installed.
- These services are development dependencies, not CrevaFactOS product modules.
