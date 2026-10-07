# Zentra RC3 final closure report

Status: RC3 LOCKED (owner final approval granted).
Branch: `zentra-release-candidate` (based on `9baeaf3e8a32f8c2890e355493d57ad6aa4ff6d7`). The closure commit SHA is the tip of this branch; it cannot be written into the commit that creates it (see the local closure record kept with the build evidence).

## Accepted artifact

`Nevaxis-Zentra-Setup-2026.10.05-closure1-rc3-g.msi`
SHA256 `ff46849b36104581971c8bedb7d0fa159d90f5e078f0accdbefc8139fc78742c`
(MSI/ZIP binaries are not tracked: `release/artifacts` is git-ignored by repository policy.)

## Final gate totals (rc3-g)

| Gate | Result |
|---|---|
| Smoke (full, exact rc3-g source) | 162/162 (148 existing + 14 `RealFactoryPilotEndpointTests`), 0 failed, 0 skipped |
| Persistence | 1193/1193 |
| Desktop | 98/98 |
| Web | 157/157, lint clean, tsc clean |
| Packaged Production route sweep (fresh tenant) | 143/143, 0 failures (`docs/quality/ZENTRA-RC3G-ROUTE-SWEEP-MATRIX.md`) |
| Credential scan of MSI payload | 0 hits |
| Permission catalog gate (`scripts/release/verify-permission-catalog.mjs`) | PASS: 455/455 model tables readable by the runtime role, 100/100 web-referenced permission keys defined |
| Production real-factory pilot | `GET /ayarlar/entegrasyonlar/gercek-fabrika` is read-only; a fresh Production tenant stays empty (connectors/runs/pilot rows = 0 before and after page loads and the full sweep); `POST /initialize` is 404 in Production and available only in Development/Testing or DEMO/PRESENTATION channels with `real-factory-pilot.manage`, audited as `integration.pilot.initialized` |

## What RC3 closes

- Production first-run enrollment and secure machine credentials
- Persistent Data Protection key ring
- Packaged subscription/entitlement provisioning
- Trusted Production auth bridge on every shipped server loader/action (no-bridge resolvers banned by test)
- Shipped route entitlement parity (API and web maps; every shipped route classified)
- Factory Intelligence schema mapping repair
- Task Center permission/catalog repairs (playbook, automation, decision-intelligence schema + permissions; `factory_outcome_*` runtime grants)
- Production-safe real-factory pilot behavior
- Release guards and regression coverage (route inventory, authorization-vs-runtime classification, migration/catalog parity, permission-catalog gate, credential scan)

## Residuals carried into RC3 Final Verification (non-blocking)

1. `GET /api/production/orders/{unknown id}/health-summary` returns 503 instead of a clean 404.
2. `ThroughputOptimizer` observe for an unknown line throws "Line not found" (unhandled) instead of a clean 404.
3. 44 call sites still use the old null-returning `fetchCurrentEffectiveAccess()` (capped by a guard test; may only fall).
4. About 138 API clients over-map every 403 to "permission-denied" (a lapsed-subscription 403 on a non-layout call can read as a missing permission).
5. Fresh-tenant explained empty states for scenario recovery and the golden-demo pages (needs a line / no demo data).
6. Unknown-id pages use "yüklenemedi" wording.

External tooling incident (no product impact): Docker Desktop crashed during RC3; RC3 does not depend on Docker (template source is a local PostgreSQL).
