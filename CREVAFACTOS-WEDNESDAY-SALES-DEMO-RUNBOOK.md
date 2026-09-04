# CrevaFactOS Wednesday Sales Demo Runbook

Presentation target: **Wednesday, 9 September 2026**  
Environment: **Development Pilot Tenant** (`dev-pilot-tenant`)  
Account type: **Tenant Owner** (full customer demo profile)

## Pre-flight checklist

1. Docker Desktop running
2. Postgres container healthy
3. API listening on `http://127.0.0.1:5000`
4. Web listening on `http://localhost:3001`
5. Sales-demo bootstrap applied (entitlements + permissions)
6. Verifier PASS: `node apps/web/scripts/verify-sales-demo-all-modules-readiness-1.mjs`

## 1. Start infrastructure

```powershell
cd C:\Projects\CrevaFactOS
docker compose -f docker-compose.local.yml up -d postgres
```

Verify Postgres:

```powershell
docker ps --filter name=crevafactos-local-postgres-1
```

## 2. Apply Development Pilot sales-demo profile

```powershell
node apps/web/scripts/bootstrap-sales-demo-pilot-full-access-1.mjs
```

This ensures:

- Active subscription on `dev-pilot-local-commercial` monthly plan
- All `module.*` entitlements unlimited on the dev plan
- Tenant owner role has all non-platform customer permissions

## 3. Start API

```powershell
cd apps/api
dotnet run
```

Health check (anonymous):

```powershell
Invoke-WebRequest http://127.0.0.1:5000/health/ready -UseBasicParsing
```

Expected: HTTP 200

## 4. Start Web

In a second terminal:

```powershell
cd apps/web
npm run dev
```

Open: `http://localhost:3001`

## 5. Login (no secrets in this document)

| Field | Value |
| --- | --- |
| Organization | `dev-pilot-tenant` |
| Email | `tenant-owner@dev.local` |
| Password | Use local development identity secret (not stored in repo) |
| MFA | Use configured development TOTP secret |

URL: `http://localhost:3001/giris`

## 6. Presentation URLs

| Module | URL |
| --- | --- |
| AI Görev Merkezi | `http://localhost:3001/` |
| Dashboard | `http://localhost:3001/dashboard` |
| Playbooklar | `http://localhost:3001/ai-gorevleri/playbooklar` |
| Otomasyon | `http://localhost:3001/ai-gorevleri/otomasyon` |
| Üretim | `http://localhost:3001/uretim` |
| Planlama | `http://localhost:3001/planlama` |
| Makineler | `http://localhost:3001/makineler` |
| Proses & OEE | `http://localhost:3001/proses-oee` |
| Bakım | `http://localhost:3001/bakim` |
| Kalite | `http://localhost:3001/kalite` |
| Enerji | `http://localhost:3001/enerji` |
| Malzemeler | `http://localhost:3001/malzemeler` |
| Siparişler | `http://localhost:3001/siparisler` |
| Ticari Zekâ hub | `http://localhost:3001/ticari-zeka/karlilik-yonetim-merkezi` |
| Operasyonel Riskler | `http://localhost:3001/operasyonel-riskler` |
| Teams | `http://localhost:3001/teams` |
| Vardiya Devir | `http://localhost:3001/vardiya-devir` |
| ERP Entegrasyonları | `http://localhost:3001/erp-entegrasyonlari/baglanti` |
| Raporlama | `http://localhost:3001/raporlar` |

## 7. Recommended presentation sequence

1. **Dashboard** — operational overview (frozen module; test only)
2. **AI Görev Merkezi** — open prioritized task queue
3. **Task detail** — decision intelligence / evidence / closure actions
4. **Makineler** — connected machine context
5. **Bakım** — maintenance work linked to machine signal
6. **Üretim / Proses & OEE** — production impact and OEE narrative
7. **Kalite** — inspection / NCR follow-up
8. **Playbooklar** — orchestration library and active run detail
9. **Operasyon Yönetim Merkezi** — cross-module command view
10. **Ticari Zekâ / Operasyonel Riskler** — manager closure

## 8. Recovery procedures

### API not responding

1. Check `http://127.0.0.1:5000/health/ready`
2. Restart `dotnet run` in `apps/api`
3. Confirm Postgres container is up
4. Re-run bootstrap script if subscription/permission errors appear

### Web not loading / stale UI

1. Stop `npm run dev`
2. Delete `apps/web/.next` only if build is corrupted
3. Restart `npm run dev`
4. Hard refresh browser (Ctrl+Shift+R)

### Access denied / missing sidebar module

1. Re-run `node apps/web/scripts/bootstrap-sales-demo-pilot-full-access-1.mjs`
2. Sign out and sign in again as tenant owner
3. Confirm organization `dev-pilot-tenant`

### Verifier regression before demo

```powershell
node apps/web/scripts/bootstrap-sales-demo-pilot-full-access-1.mjs
node apps/web/scripts/verify-sales-demo-all-modules-readiness-1.mjs
```

## 9. Screenshot evidence

Authenticated screenshots: `.cursor-screenshots/sales-demo-readiness-1/`

Verifier JSON matrix: `.cursor-screenshots/sales-demo-readiness-1/results.json`
