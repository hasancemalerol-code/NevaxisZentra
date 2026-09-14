# Nevaxis FactOS — Golden Demo Sales Runbook

Canonical Tuesday sales demo for production recovery on **INJ-001**.

## A. STARTUP

### Prerequisites

1. Local infrastructure running: `pnpm infra:start`
2. Database migrated and verified: `pnpm db:migrations:apply` then `pnpm db:verify`

### Start backend/API

```text
pnpm dev:api
```

- URL: `http://localhost:5000`
- Health: `GET /health/live` and `GET /health/ready`

### Start Web

```text
pnpm dev:web
```

- Default URL: `http://localhost:3000`
- Health: `GET /health`
- Golden Demo route: `http://localhost:3000/golden-demo/uretim-kurtarma`

If automated verifiers are used, set `NEVAXIS_FACTOS_WEB_BASE_URL=http://localhost:3000` (or run Web on port 3001 to match existing scripts).

### Optional worker (not required for Golden Demo)

```text
pnpm dev:worker
```

## B. PRE-DEMO CHECK

Run in order:

| Check | Command / Action | Expected |
|-------|------------------|----------|
| API healthy | Browser or curl `http://localhost:5000/health/live` | HTTP 200 |
| Web healthy | Browser or curl `http://localhost:3000/health` | HTTP 200 |
| Database healthy | `pnpm db:verify` | PASS |
| Golden Demo reset | Open route → **Altın Demo Sıfırla** → confirm | “Altın Demo sıfırlandı…” |
| Browser route | `/golden-demo/uretim-kurtarma` | Workspace loads |
| Browser zoom | 100% (125% on small laptop if needed) | KPIs readable |
| Clean worktree | `git status` | No uncommitted demo changes |
| Starting state | After reset | Event **Aktif — hedef risk altında** |

Automated pre-check (API + Web must be running):

```text
pnpm --filter @nevaxisfactos/web verify:golden-demo-sales-ready
```

## C. LIVE CLICK PATH

Open: **`/golden-demo/uretim-kurtarma`**

### STEP 01 — Production Target at Risk

| | |
|---|---|
| **SCREEN** | KPI grid: hedef gerçekleşme, vardiya hedefi, projeksiyon |
| **ACTION** | Scroll to production target / risk evidence |
| **WHAT TO SAY** | “Hedef altında üretim riski görünüyor; FactOS bunu erken yakalıyor.” |
| **WHAT NOT TO SAY** | “Sistem otomatik durdurdu” / “PLC’ye komut gönderdik” |

### STEP 02 — Machine Intelligence

| | |
|---|---|
| **SCREEN** | Makine durumu, cycle, mikro-duruş, enerji yoğunluğu |
| **ACTION** | Point to machine metrics panel |
| **WHAT TO SAY** | “Makine zekası gerçek runtime verisinden okuyor.” |
| **WHAT NOT TO SAY** | “Canlı fabrika bağlantısı” (unless actually connected) |

### STEP 03 — Cycle Loss Decomposition

| | |
|---|---|
| **SCREEN** | Cycle loss breakdown |
| **ACTION** | Show lost seconds by category |
| **WHAT TO SAY** | “Kayıp saniyeler ayrıştırıldı; nerede zaman kaybettiğimiz net.” |
| **WHAT NOT TO SAY** | “Tahmini cycle” as measured fact |

### STEP 04 — Production Recovery Opportunity

| | |
|---|---|
| **SCREEN** | +X İYİ PARÇA / VARDİYA KPI |
| **ACTION** | Highlight recovery opportunity card |
| **WHAT TO SAY** | “Kurtarılabilir kapasite hesaplandı.” |
| **WHAT NOT TO SAY** | Hard-coded recovery number |

### STEP 05 — Digital Factory Team

| | |
|---|---|
| **SCREEN** | Dijital Fabrika Ekibi — agent cards |
| **ACTION** | Click one agent for evidence drawer |
| **WHAT TO SAY** | “Her uzman aynı runtime verisini analiz etti.” |
| **WHAT NOT TO SAY** | “AI fiziksel müdahale yaptı” |

### STEP 06 — Orchestrator Assessment

| | |
|---|---|
| **SCREEN** | FactOS Orchestrator panel |
| **ACTION** | Read what / why / next |
| **WHAT TO SAY** | “Orchestrator tüm kanıtları birleştirip sonraki adımı tanımlıyor.” |
| **WHAT NOT TO SAY** | “Otomatik onay” |

### STEP 07 — Recommended Recovery Plan

| | |
|---|---|
| **SCREEN** | Önerilen Kurtarma Planı |
| **ACTION** | Walk plan actions |
| **WHAT TO SAY** | “Önerilen plan operasyonel işe dönüştü.” |
| **WHAT NOT TO SAY** | “Plan henüz yok” (after reset) |

### STEP 08 — Human Approval

| | |
|---|---|
| **SCREEN** | Approval Required sheet |
| **ACTION** | Click **Planı Onayla** (reviewer role) |
| **WHAT TO SAY** | “Kritik adım yetkili insan onayı ile ilerliyor.” |
| **WHAT NOT TO SAY** | “FactOS kendi kendine onayladı” |

### STEP 09 — Technical Intervention

| | |
|---|---|
| **SCREEN** | Intervention timeline + **Müdahaleyi Başlat** |
| **ACTION** | Start → Complete intervention |
| **WHAT TO SAY** | “FactOS koordine ediyor; fiziksel iş yetkili ekip tarafından uygulanıyor.” |
| **WHAT NOT TO SAY** | “Uzaktan kalıbı değiştirdik” |

### STEP 10 — Validation Run

| | |
|---|---|
| **SCREEN** | Doğrulama Koşusu |
| **ACTION** | Show validation window status |
| **WHAT TO SAY** | “Müdahale sonrası doğrulama penceresi açıldı.” |
| **WHAT NOT TO SAY** | “Sonuç garanti” |

### STEP 11 — Before / After

| | |
|---|---|
| **SCREEN** | Önce / Sonra comparison table |
| **ACTION** | Cycle time, throughput, micro-stops |
| **WHAT TO SAY** | “Baseline ile doğrulama penceresi karşılaştırılıyor.” |
| **WHAT NOT TO SAY** | Raw enum values to customer |

### STEP 12 — Outcome Verification

| | |
|---|---|
| **SCREEN** | Sonuç Doğrulama + **Sonucu Doğrula** |
| **ACTION** | Click verify (reviewer) |
| **WHAT TO SAY** | “FactOS sonucu kanıtla doğruluyor.” |
| **WHAT NOT TO SAY** | Success before verify completes |

### STEP 13 — PRODUCTION RECOVERED

| | |
|---|---|
| **SCREEN** | Outcome hero (only when verified) |
| **ACTION** | Pause on headline |
| **WHAT TO SAY** | “Üretim kurtarıldı — kanıtla doğrulandı.” |
| **WHAT NOT TO SAY** | “PRODUCTION RECOVERED” if outcome is NOT_VERIFIED |

### STEP 14 — +X GOOD PARTS / SHIFT

| | |
|---|---|
| **SCREEN** | Hero KPI: +GOOD PARTS / SHIFT |
| **ACTION** | Primary proof point |
| **WHAT TO SAY** | “Vardiya başına kurtarılan iyi parça runtime hesabı.” |
| **WHAT NOT TO SAY** | Fixed demo number |

### STEP 15 — Energy Impact

| | |
|---|---|
| **SCREEN** | Enerji Etkisi |
| **ACTION** | Show classification (ÖLÇÜLMÜŞ / HESAPLANMIŞ) |
| **WHAT TO SAY** | “Enerji etkisi iyi parça başına ve sınıflandırmasıyla gösteriliyor.” |
| **WHAT NOT TO SAY** | Guaranteed savings |

### STEP 16 — Estimated Business Impact

| | |
|---|---|
| **SCREEN** | Tahmini İş Etkisi |
| **ACTION** | Brief secondary mention |
| **WHAT TO SAY** | “Ticari katman tahmini; birincil kanıt üretim kurtarması.” |
| **WHAT NOT TO SAY** | Let finance dominate the story |

## D. PRIMARY SALES STORY

> FactOS does not only tell us that the machine is below target. It identifies where production time is being lost, calculates the recoverable production capacity, coordinates the technical intervention, and then verifies whether production actually improved.

Turkish presenter variant:

> FactOS bize sadece makinenin hedefin altında olduğunu söylemiyor. Üretim zamanının nerede kaybedildiğini buluyor, kurtarılabilir kapasiteyi hesaplıyor, teknik müdahaleyi koordine ediyor ve üretimin gerçekten iyileşip iyileşmediğini doğruluyor.

## E. FINAL PROOF

1. **PRODUCTION RECOVERED**
2. **+X GOOD PARTS / SHIFT** (runtime-calculated, verification-gated hero)

## F. IMPORTANT CLAIM BOUNDARIES

- FactOS does **not** claim direct PLC control unless a real connector is connected.
- Replay / pilot data is labeled as runtime-backed replay, not live MES unless connected.
- Digital agents coordinate **workflow and evidence**, not physical machine motion.
- Authorized humans execute physical maintenance / operator work.
- Estimated financial impact is **TAHMİNİ / estimated**, not audited revenue.
- Verification uses **actual available production evidence** in the validation window.
- Classifications: **ÖLÇÜLMÜŞ**, **HESAPLANMIŞ**, **TAHMİNİ**, **DOĞRULANMIŞ**.

## G. FAILURE RECOVERY

| Symptom | Recovery |
|---------|----------|
| Stale UI after action | Browser refresh (F5) — state is server-backed |
| Wrong demo stage | **Altın Demo Sıfırla** → confirm → refresh |
| API error banner | Restart API: stop process, `pnpm dev:api` |
| Web blank / 500 | Restart Web: `pnpm dev:web` |
| Database error | `pnpm db:verify`; restart infra if needed |
| Demo fallback | Re-run reset + smoke test; use backup recording clips if live demo fails |

Reset control location: top of Golden Demo page — **Altın Demo Kontrolü** panel.
