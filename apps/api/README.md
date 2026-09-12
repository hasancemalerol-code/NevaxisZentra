# NevaxisFactOS API

ASP.NET Core modular monolith backend for NevaxisFactOS.

## Development pilot — Production Planning read flow

This pilot seeds tenant, membership, role permission, facility, facility membership, and optional production plan data for local development only. It is **not production-ready** and must stay disabled outside Development.

### Prerequisites

1. Start local PostgreSQL (see [infrastructure/README.md](../../infrastructure/README.md)).
2. Apply migrations: `pnpm db:migrate` from the repository root.
3. Enable persistence on the API (`NEVAXIS_FACTOS__PERSISTENCE__ENABLED=true` and connection settings).

### Enable development bootstrap

In `apps/api/appsettings.Development.json` (local only — do not commit secrets):

```json
{
  "DevelopmentPilot": {
    "Enabled": true,
    "ProductionPlanSeedEnabled": true,
    "TenantKey": "dev-pilot-tenant",
    "DevelopmentIssuer": "crevafactos-dev",
    "DevelopmentSubject": "planning-pilot-user",
    "FacilityId": "bb000001-0005-4000-8000-000000000005"
  }
}
```

Defaults in `appsettings.json` keep `Enabled: false`. Production startup fails if `DevelopmentPilot:Enabled` is true outside Development.

The bootstrap assigns only `production.planning.read` to the pilot role.

### Start API

```bash
dotnet run --project apps/api/NevaxisFactOS.Api.csproj
```

On startup the hosted bootstrap runs once when enabled. It is idempotent: repeated runs do not create duplicates.

### Pilot identity headers

Development authentication (existing pattern):

- Issuer: `crevafactos-dev`
- Subject: `planning-pilot-user`
- Tenant header: `dev-pilot-tenant`
- Facility id: `bb000001-0005-4000-8000-000000000005`

### API endpoints

- List: `GET /production-planning/plans?facilityId={facilityId}`
- Detail: `GET /production-planning/plans/{planId}?facilityId={facilityId}`

### Production guard

`DevelopmentPilot:Enabled=true` in Staging or Production causes startup failure. Never enable pilot seed in deployed environments.
