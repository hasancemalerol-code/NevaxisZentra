# NevaxisZentra Netsis Bridge

Read-only HTTP bridge between NevaxisZentra API and an authorized Netsis integration interface.

## Architecture

```
NevaxisZentra API → (Bearer token) → NevaxisZentra Netsis Bridge → Netsis integration interface
```

The browser never calls the bridge directly. The API resolves the bridge token via `CredentialReference`.

## Endpoints (GET only)

- `GET /health` — transport health and company context
- `GET /context` — product family, version, company, period
- `GET /customers?pageSize=N&cursor=TOKEN` — paginated customer/cari read
- `GET /products?pageSize=N&cursor=TOKEN` — paginated product/material read

## Security

- All endpoints require `Authorization: Bearer <token>`
- Token is configured via `NetsisBridge:AuthToken` (environment variable or secrets)
- Only GET requests are accepted (405 for all others)
- No SQL exposure, no generic query endpoint, no write routes
- Deploy on the Netsis integration PC on a private LAN or VPN — do NOT expose publicly

## Configuration

```json
{
  "NetsisBridge": {
    "AuthToken": "<configure-in-environment-secrets>"
  }
}
```

## Notes

- Real Netsis integration is deferred to ERP-NETSIS-2 once a Netsis installation is available
- This bridge currently serves the test fixture for development validation
- No guessed Netsis schema, tables, or stored procedures
