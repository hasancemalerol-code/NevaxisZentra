# NevaxisZentra Mikro Bridge

Read-only HTTP transport host for Mikro ERP integration on the Mikro-installed / authorized PC.

## Purpose

Expose a narrow, authenticated, read-only JSON surface so NevaxisZentra API can reach Mikro through a private bridge without public SQL exposure, copied Mikro passwords in the web app, or generic query execution.

## Supported read-only endpoints

- `GET /health`
- `GET /context`
- `GET /customers?pageSize=&cursor=`
- `GET /products?pageSize=&cursor=`

No mutation routes. No generic SQL endpoint. No arbitrary command execution.

## Required configuration

Set `MikroBridge:AuthToken` in environment or local configuration. Never commit secrets.

Example local development:

```json
{
  "MikroBridge": {
    "AuthToken": "development-only-bridge-token"
  }
}
```

NevaxisZentra API resolves bridge credentials through `CredentialReference` and `MikroIntegration:Credentials:{reference}` configuration — not plaintext in ERP connection profiles.

## Credential rule

- Bridge token stays on the bridge host and in approved secret configuration.
- ERP stores only `CredentialReference`.
- Health responses do not return secrets.

## Network recommendation

Deploy on a private network or VPN segment reachable only from NevaxisZentra API / integration subnet. Do not expose SQL Server publicly. A later installation may require a narrow inbound bridge port rule; do not use broad ANY/ANY firewall rules.

## Health test

```bash
curl -H "Authorization: Bearer <token>" http://127.0.0.1:<port>/health
```

## Pointing NevaxisZentra to the bridge

Create a Mikro ERP connection profile with:

- `provider`: `mikro`
- `connectionMode`: `api`
- `baseEndpoint`: bridge base URL
- `credentialReference`: configured reference key
- explicit `externalCompanyId` / `externalCompanyCode`

## Explicit non-goals

- No public SQL requirement
- No ERP write operations
- No Windows installer / auto-update in ERP-MIKRO-1
