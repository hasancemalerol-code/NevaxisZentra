# Nevaxis FactOS — Golden Demo Sales Runbook

Combined sales demo: **Production Recovery** + **Customs File Digital Worker** on localhost Web + API.

## A. PRE-DEMO STARTUP

### Prerequisites

1. Infrastructure: `pnpm infra:start`
2. Database: `pnpm db:migrations:apply` then `pnpm db:verify`

### Start services

```text
pnpm dev:api    → http://127.0.0.1:5000
pnpm dev:web    → http://localhost:3000 (or 3001 for existing verifiers)
```

Health checks:

| Service | URL |
|---------|-----|
| API | `GET /health/live` |
| Web | `GET /health` |

Set `NEVAXIS_FACTOS_WEB_BASE_URL` if Web runs on a non-default port.

### Automated pre-check (recommended)

```text
pnpm --filter @nevaxisfactos/web verify:golden-demo-final-combined-freeze
```

### Reset both demos before presenting

| Demo | Route | Reset action |
|------|-------|--------------|
| Production | `/golden-demo/uretim-kurtarma` | **Altın Demo Sıfırla** → confirm |
| Customs | `/golden-demo/gumruk-dosyasi` | **Gümrük Demo Sıfırla** → confirm |

Navigation: **Çalışma alanı** → **Altın Demo — Üretim Kurtarma** / **Altın Demo — Gümrük Dosyası**

## B. PART 1 — PRODUCTION GOLDEN DEMO

**Route:** `/golden-demo/uretim-kurtarma`

| Step | Screen | Action | Say |
|------|--------|--------|-----|
| 1 | Command Center / KPI risk | Show hedef gerçekleşme | "Hedef altında üretim riski erken yakalanıyor." |
| 2 | Machine Intelligence | Machine metrics | "Makine zekası runtime verisinden okuyor." |
| 3 | Cycle Loss Decomposition | Lost seconds breakdown | "Kayıp saniyeler ayrıştırıldı." |
| 4 | Recovery Opportunity | +X iyi parça / vardiya | "Kurtarılabilir kapasite hesaplandı." |
| 5 | Digital Factory Team | Agent cards → evidence | "Her uzman aynı kanıtı analiz etti." |
| 6 | Recovery Plan | Walk plan actions | "Önerilen plan operasyonel işe dönüştü." |
| 7 | Human Approval | **Planı Onayla** (reviewer) | "Kritik adım yetkili onay ile ilerliyor." |
| 8 | Technical Intervention | Başlat → Tamamla | "FactOS koordine ediyor; fiziksel iş ekip tarafından." |
| 9 | Validation Run | Doğrulama koşusu | "Müdahale sonrası doğrulama penceresi açıldı." |
| 10 | Before / After | Metric comparison | "Baseline ile doğrulama penceresi karşılaştırılıyor." |
| 11 | Outcome Verification | **Sonucu Doğrula** (reviewer) | "FactOS sonucu kanıtla doğruluyor." |
| 12 | **PRODUCTION RECOVERED** | Outcome hero | "Üretim kurtarıldı — kanıtla doğrulandı." |
| 13 | **+X GOOD PARTS / SHIFT** | Hero KPI | Runtime hesabı — sabit demo sayısı değil |
| 14 | Energy Impact | Secondary layer | ÖLÇÜLMÜŞ / HESAPLANMIŞ sınıflandırması |
| 15 | Business Impact | Brief mention | TAHMİNİ — birincil kanıt üretim kurtarması |

**Transition sentence:**

> "FactOS Dijital Çalışanları yalnızca üretim hattında çalışmıyor."

## C. PART 2 — CUSTOMS GOLDEN DEMO

**Route:** `/golden-demo/gumruk-dosyasi`

| Step | Screen | Action | Say |
|------|--------|--------|-----|
| 16 | Outcome assignment | Assign: "Bu siparişin gümrük dosyasını hazırla." | "FactOS'a soru sormuyorsunuz; iş veriyorsunuz." |
| 17 | Order / shipment context | Worker loads EXP-DEMO-DE-001 | "Sipariş ve sevkiyat bağlamı yüklendi." |
| 18 | Documents prepared | Commercial Invoice + Packing List | "Operasyonel belgeler hazırlandı." |
| 19 | Blocker detected | Packing List net weight mismatch | "Tutarsızlık deterministik kontrol ile bulundu." |
| 20 | Corrective work | Worker creates task | "Dijital çalışan düzeltici iş oluşturdu." |
| 21 | Correction completed | Complete corrective work | "Düzeltme uygulandı — kanıt henüz yeniden kontrol edilmedi." |
| 22 | Evidence re-check | Re-check evidence | "Kanıt yeniden değerlendirildi." |
| 23 | Packing List v2 | New valid version | "Güncel Packing List doğrulandı." |
| 24 | Human approval | Approve customs package (reviewer) | "Gümrük paketi onaylandı." |
| 25 | Package assembled | Customs Clearance Package | "Gümrük dosyası birleştirildi." |
| 26 | **GÜMRÜK DOSYASI HAZIR** | VERIFIED READY hero | "Gümrük müşavirine gönderime hazır." |

**Closing line:**

> "İş, gümrük müşavirine gönderime gerçekten hazır olduğunda kapanıyor."

## D. CLAIM BOUNDARIES

**Do say:**

- Production recovered (after verification)
- Customs clearance package prepared
- Ready for customs broker handoff

**Do not say:**

- Direct PLC / machine control (unless real connector connected)
- Customs cleared / declaration submitted / declaration registered
- Government portal integration
- Guaranteed ROI or audited financial realization

## E. RECOVERY

| Symptom | Action |
|---------|--------|
| Stale UI | Browser refresh (F5) — state is server-backed |
| Wrong production stage | Production reset → refresh |
| Wrong customs stage | Customs reset → refresh |
| Cross-demo confusion | Reset **both** demos independently |
| API error | Restart `pnpm dev:api` |
| Web error | Restart `pnpm dev:web` |
| Database error | `pnpm db:verify`; restart infra if needed |

**Do not:** use developer tools to manually edit API state during a live demo.

## F. POST-DEMO

1. Optional: reset both demos for next session
2. Stop Web and API processes safely
3. `git status` should remain clean (no demo edits committed during presentation)
