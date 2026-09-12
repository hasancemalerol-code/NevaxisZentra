# NevaxisFactOS Logo Bridge

Read-only HTTP transport host for Logo ERP integration on the Logo-installed / authorized PC.

## Purpose

Expose a narrow, authenticated, read-only JSON surface so NevaxisFactOS API can reach Logo through a private bridge without public SQL exposure, copied Logo passwords in the web app, or generic query execution.

## Supported read-only endpoints

- `GET /health`
- `GET /context`
- `GET /customers?pageSize=&cursor=`
- `GET /products?pageSize=&cursor=`

No mutation routes. No generic SQL endpoint. No arbitrary command execution.

## Required configuration

Set `LogoBridge:AuthToken` in environment or local configuration. Never commit secrets.

Example local development:

```json
{
  "LogoBridge": {
    "AuthToken": "development-only-bridge-token"
  }
}
```

NevaxisFactOS API resolves bridge credentials through `CredentialReference` and `LogoIntegration:Credentials:{reference}` configuration — not plaintext in ERP connection profiles.

## Credential rule

- Bridge token stays on the bridge host and in approved secret configuration.
- ERP stores only `CredentialReference`.
- Health responses do not return secrets.

## Network recommendation

Deploy on a private network or VPN segment reachable only from NevaxisFactOS API / integration subnet. Do not expose SQL Server publicly. A later installation may require a narrow inbound bridge port rule; do not use broad ANY/ANY firewall rules.

## Health test

```bash
curl -H "Authorization: Bearer <token>" http://127.0.0.1:<port>/health
```

## Pointing NevaxisFactOS to the bridge

Create a Logo ERP connection profile with:

- `provider`: `logo`
- `connectionMode`: `api`
- `baseEndpoint`: bridge base URL
- `credentialReference`: configured reference key
- explicit `externalCompanyId` / `externalCompanyCode`

## Explicit non-goals

- No public SQL requirement
- No ERP write operations
- No Windows installer / auto-update in ERP-LOGO-1
