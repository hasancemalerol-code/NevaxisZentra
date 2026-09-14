# Nevaxis FactOS — Final Combined Sales Freeze Manifest

**Package:** FACTOS-FINAL-COMBINED-SALES-READY-FREEZE  
**Date:** 2026-09-14  
**Branch:** `main`

## Baseline

| Field | Value |
|-------|-------|
| Starting commit | `886977632cad2cd1484c9890dbaa73fbdb5c0633` |
| Starting message | `feat: add customs file digital worker golden outcome` |
| Historical note | Prior sales freeze `5fcc8b5` superseded by this combined baseline |

## Included Golden Demos

| Demo | Route | Worker / Engine |
|------|-------|-----------------|
| Production Recovery | `/golden-demo/uretim-kurtarma` | P1 + P2/P2B + P3 |
| Customs File Digital Worker | `/golden-demo/gumruk-dosyasi` | DW-EXP-001 |

## Accepted Module Baselines

- P1 Machine Intelligence / Cycle Loss / Production Recovery
- P2/P2B Digital Factory Team / evidence / orchestration / approval
- P3 Verified Production Recovery / before-after / energy / business impact
- Customs clearance package workflow (documents, blocker, corrective work, approval, verification)

## Verifier Results

| Verifier | Result |
|----------|--------|
| `verify-golden-demo-p1-runtime` | PASS |
| `verify-golden-demo-p2-runtime` | PASS |
| `verify-golden-demo-p2b-runtime` | PASS |
| `verify-golden-demo-p3-runtime` | PASS |
| `verify-golden-demo-sales-ready` | PASS (65 checks, 3 cycles) |
| `verify-golden-demo-customs-file-runtime` | PASS (2 cycles + P1 regression) |
| `verify-golden-demo-final-combined-freeze` | PASS (143 checks, 3 combined cycles) |
| Golden Demo persistence tests | PASS (36 tests) |

## Security / RLS

| Check | Result |
|-------|--------|
| Authentication (dev headers + permission keys) | PASS |
| Authorization (unauthorized customs approve blocked) | PASS |
| Tenant isolation (`db:tenant-isolation:verify`) | PASS |
| RLS on Golden Demo tables | PASS |
| Demo bypass introduced | **No** |
| Global reset introduced | **No** |

Note: `pnpm authorization:verify` blocked during freeze run — dev API process held DLL lock. No auth/RLS changes in this package.

## External Dependency Status

| Dependency | Status |
|------------|--------|
| Local PostgreSQL | Required |
| Localhost API (5000) | Required |
| Localhost Web (3000/3001) | Required |
| Internet / CDN fonts | **Not required** for Golden Demo routes |
| External AI/model calls | **Not required** for Golden Demo flow |
| Government/customs portals | **Not integrated** |

Critical demo path remains operational on localhost without external Internet.

## Independent Reset Behavior

| Reset | Scope | Cross-contamination |
|-------|-------|---------------------|
| Production (`POST .../golden-demo-p2/events/{id}/reset`) | Production demo only | Does not affect customs |
| Customs (`POST .../golden-demo-customs-file/outcomes/{id}/reset`) | Customs demo only | Does not affect production |

Verified across 3 combined cycles.

## Known Limitations

- Localhost Web + API development model (desktop packaging not in this freeze)
- Customs documents are structured persisted artifacts (no binary PDF/ZIP in demo)
- No customs declaration submission or government integration
- Estimated financial values in P3 are TAHMİNİ / estimated
- Authorization full-suite verify requires stopping dev API to release DLL lock

## Next Permitted Package

**FACTOS-FULL-CURRENT-WINDOWS-RELEASE**

No feature package permitted between this freeze and Windows release.

## HARD RELEASE GATE (next package)

The Windows installer MUST NOT ship until:

| Gate | Required |
|------|----------|
| SOURCE-CODE EXPOSURE & SECRET LEAK AUDIT | PASS |
| No `.git` / repository source tree in installer | PASS |
| No `.env` / API keys / real passwords / production secrets | PASS |
| Production source maps disabled | PASS |
| DevTools disabled in production desktop | PASS |
| Frontend production/minified | PASS |
| .NET Release publish | PASS |
| No exposed GitHub/repository metadata | PASS |
| No debug endpoints | PASS |
| No sensitive logs | PASS |
| Installer content file-by-file scan | PASS |
| Clean second-PC installation test | PASS |
