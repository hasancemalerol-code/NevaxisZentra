# APP-ACTION-AUDIT-1 — Application action inventory

## 1. Audit metadata and baseline

| Field | Value |
| --- | --- |
| Package | APP-ACTION-AUDIT-1 |
| Date | 2026-08-14 |
| Repository | `C:\Projects\CrevaFactOS` |
| Baseline commit | `6adcd89b7f5761f720307f65ebffea181569eff2` |
| Baseline subject | `feat(saas): add tenant and facility authorization foundation` |
| ADMIN-ACCESS-1 committed | Yes (this baseline) |
| Worktree at start | Clean |
| Product source changed | No |
| Database changed | No |
| Dependencies changed | No |
| Commit taken | No |
| Web runtime | `http://127.0.0.1:3001` (Next.js 16.3.0 / Turbopack) |
| API runtime | `http://127.0.0.1:5000` (`CREVAFACTOS__AUTHENTICATION__MODE=SessionCookie`) |
| Viewport | 1600×1000 (browser lock + ADMIN-ACCESS-1 verifier contract) |
| Route source | `apps/web/src/lib/navigation/primary-navigation.ts`, `apps/web/src/app/**/page.tsx`, catch-all `apps/web/src/app/(app)/[...sections]/page.tsx` |
| Session used | Tenant-owner (MFA completed via identity API, not printed). Restricted personas: machine-responsible, materials-sales, maintenance-team. |
| Browser note | MCP browser `/giris` React `onSubmit` did not prevent native submit. Authenticated route reachability used the same web origin with a session cookie. No tokens, passwords, or cookies are recorded here. |
| Credit-saving policy | Navigation registry → app routes → route-rendered components → directly referenced actions/clients only. No repo-wide scan, build, typecheck, ESLint, backend tests, or migrations. |

## 2. Route/module coverage

HTTP results are from authenticated GET to `http://127.0.0.1:3001` (redirect: manual). Headings are the first `<h1>` in the HTML. `forbidden()` pages return HTTP 403. Catch-all placeholders return HTTP 200 with EmptyState “Yer tutucu”. Unknown hrefs return HTTP 404.

| Route | Module | HTTP | Heading | Implementation | Permission key | Active | Disabled | Broken | Unavailable feature |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `/giris` | Authentication | 200 unauth / 307→`/dashboard` when session exists | Oturum aç | Implemented | (public) | 2 | 0 | 1 (GET default) | 0 |
| `/mfa-dogrula` | Authentication | Not re-fetched after HTTP MFA | MFA title (source) | Implemented | pending MFA | 3 | 0 | 0 | 0 |
| `/sifremi-unuttum` | Authentication | 200 unauth / 307→`/dashboard` with session | Forgot-password title (source) | Implemented | (public) | 2 | 0 | 0 | 0 |
| `/sifre-sifirla` | Authentication | Static only | Reset title (source) | Implemented | token | 1 | 0 | 0 | 0 |
| `/davet/kabul` | Authentication | Static only | Invitation title (source) | Implemented | token | 1 | 0 | 0 | 0 |
| `/` | AI Task Center | 200 | Operasyon karar merkezi | Implemented (preview data) | none | 20 | 1 | 1 | 1 |
| `/ai-gorevleri/ai-task-001` | AI Task Center | 200 | Hammadde stoğu üretim planını karşılamıyor | Implemented (preview) | none | 2 | 6 | 0 | 6 |
| `/ai-gorevleri/yeni` | AI Task Center | 404 | (none) | Missing page | none | 0 | 0 | 1 | 0 |
| `/dashboard` | Dashboard | 200 | AI Görev Merkezi | Implemented (preview) | none | 18 | 12 | 3 | 4 |
| `/teams` | CrevaFactOS Teams | 200 | CrevaFactOS Teams | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/siparisler` | Orders | 200 | Siparişler | Implemented (preview) | none | 12 | 1 | 0 | 0 |
| `/siparisler/order-draft-001` | Orders | 200 | Ağustos enjeksiyon parça siparişi | Implemented (preview) | none | 4 | 2 | 0† | 2 |
| `/planlama` | Planning | 200 | Üretim Planlama | Implemented | none | 8 | 0 | 0 | 0 |
| `/planlama/yeni` | Planning | 200 | Yeni Plan Taslağı | Implemented | none | 5 | 0 | 0 | 0 |
| `/uretim` | Production | 200 | Üretim Emirleri | Implemented | none | 6 | 0 | 0 | 0 |
| `/ihracat` | Export | 200 | İhracat | Implemented | none | 6 | 0 | 0 | 0 |
| `/vardiyalar` | Shifts | 200 | Vardiyalar | Implemented | `shift-workforce.read` (nav only) | 10 | 0 | 0 | 0 |
| `/proses-oee` | Process & OEE | 200 | Proses & OEE | Implemented | `process-oee.read` (nav only) | 16 | 1 | 0 | 0 |
| `/makineler` | Machines | 200 owner / 403 denied | Makine & Edge | Implemented | `edge.read` | 10 | 0 | 0 | 0 |
| `/kaliplar` | Molds | 200 | Kalıplar | Implemented | `mold.read` (nav only) | 33 | 0 | 0 | 0 |
| `/malzemeler` | Materials | 200 owner / 403 denied | Malzemeler | Implemented | `material.read` | 19 | 0 | 0 | 0 |
| `/bakim` | Maintenance | 200 owner / 403 denied | Bakım & Arıza | Implemented | `maintenance.read` | 20 | 0 | 6 | 0 |
| `/bakim/planlama` | Maintenance | 200 (including denied roles) | Bakım Planlama | Implemented | not enforced on page | 9 | 0 | 2 | 0 |
| `/bakim/raporlama` | Maintenance | 200 | Bakım Güvenilirlik ve Analiz | Implemented | not enforced on page | 6 | 0 | 0 | 0 |
| `/bakim/durum-zekasi` | Maintenance | 200 | Kestirici Bakım | Implemented | not enforced on page | 9 | 0 | 0 | 0 |
| `/bakim/arizalar/yeni` | Maintenance | 404 | (none) | Missing | — | 0 | 0 | 1 | 0 |
| `/bakim/emirler/yeni` | Maintenance | 200 | Bakım Emri Detayı | Wrong route (`[id]=yeni`) | — | 2 | 0 | 1 | 0 |
| `/bakim/planlar/yeni` | Maintenance | 404 | (none) | Missing | — | 0 | 0 | 1 | 0 |
| `/kalite` | Quality | 200 | Kalite Kontrol | Implemented | none | 12 | 0 | 0 | 0 |
| `/kalite/ncr` | Quality | 200 | Uygunsuzluk Raporları | Implemented | none | 7 | 0 | 0 | 0 |
| `/kalite/raporlama` | Quality | 200 | Kalite Raporları | Implemented | none | 7 | 0 | 0 | 0 |
| `/kalite/kontroller/yeni` | Quality | 200 | Yeni Kalite Kontrolü | Implemented | none | 7 | 0 | 0 | 0 |
| `/kalite/planlar/yeni` | Quality | 200 | Yeni Kontrol Planı | Implemented | none | 7 | 0 | 0 | 0 |
| `/enerji` | Energy | 200 | Enerji | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/iot-plc` | IoT & PLC | 200 | IoT & PLC | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/malzeme-uygunlugu` | Material suitability | 200 | Malzeme Uygunluğu | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/tedarik` | Supply risks | 200 | Tedarik Riskleri | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/siparis-termin` | Order/delivery risks | 200 | Sipariş & Termin Riskleri | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/maliyet-sapmalari` | Cost deviations | 200 | Maliyet Sapmaları | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/kapasite` | Capacity risks | 200 | Kapasite Riskleri | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/riskler/kalite` | Quality risks | 200 | Kalite Riskleri | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/satin-alma-fiyat-analizi` | Purchase price analysis | 200 | Satın Alma Fiyat Analizi | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/tedarikci-karsilastirmasi` | Supplier comparison | 200 | Tedarikçi Karşılaştırması | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/satis-fiyat-analizi` | Sales price analysis | 200 | Satış Fiyat Analizi | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/urun-karliligi` | Product profitability | 200 | Ürün Kârlılığı | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/musteri-karliligi` | Customer profitability | 200 | Müşteri Kârlılığı | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ticari-zeka/teklif-riskleri` | Quote risks | 200 | Teklif Riskleri | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/erp-entegrasyonlari/baglanti` | ERP connection | 200 | ERP Entegrasyonları | Implemented | none | 8 | 0 | 0 | 0 |
| `/erp-entegrasyonlari/veri-eslestirme` | ERP | 200 | ERP Entegrasyonları | Implemented | none | 8 | 0 | 0 | 0 |
| `/erp-entegrasyonlari/senkronizasyonlar` | ERP | 200 | Senkronizasyon Merkezi | Implemented | none | 8 | 0 | 0 | 0 |
| `/erp-entegrasyonlari/hata-kuyrugu` | ERP | 200 | Entegrasyon Hata Kuyruğu | Implemented | none | 8 | 0 | 0 | 0 |
| `/erp-entegrasyonlari/saglik` | ERP | 200 | Bağlantı Sağlığı | Implemented | none | 8 | 0 | 0 | 0 |
| `/raporlar` | Reports | 200 | Raporlama & Analitik | Honest placeholder | none | 0* | 0 | 0 | 1 |
| `/ayarlar` | Settings | 200 | Ayarlar | Honest placeholder | none | 0* | 0 | 0 | 1 |
| Forbidden interrupt | Error | 403 | Erişim reddedildi | Implemented (`forbidden.tsx`) | page `forbidden()` | 0 | 0 | 0 | 0 |

\* Placeholder routes expose only app-shell controls (counted in App shell). Page body is EmptyState, not an active module CTA.

† Order approve/ERP buttons are disabled on current preview data; the enabled code path has no handler (see P1).

Routes returning 500: none observed.

Dynamic detail routes statically traced but not all HTTP-sampled: `/planlama/[planId]`, `/planlama/[planId]/duzenle`, `/uretim/[orderId]`, `/uretim/raporlama`, `/ihracat/[exportCaseId]`, `/ihracat/ulke-arastirma/*`, `/bakim/emirler/[id]` (sampled as `yeni`), `/bakim/varliklar/[assetReference]`, `/kalite/kontroller/[inspectionId]/*`, `/kalite/ncr/[ncrId]`, `/erp-entegrasyonlari/**` nested document/order pages. Parent links exist; mutating CTAs classified from source only.

## 3. Summary counts

Counts are unique control definitions (shell counted once). Per-route HTTP `button`/`a` totals include the repeated shell and are not summed into these unique totals.

| Classification | Count |
| --- | --- |
| Unique interactive controls inventoried | 168 |
| WORKING_NAVIGATION | 78 |
| WORKING_LOCAL_INTERACTION | 24 |
| WORKING_MUTATION_WIRED | 31 |
| PERMISSION_HIDDEN | 15 |
| DISABLED_WITH_REASON | 14 |
| BROKEN_NO_HANDLER | 6 |
| BROKEN_PLACEHOLDER_LINK (`href="#"` only) | 0 |
| BROKEN_WRONG_ROUTE | 1 |
| BROKEN_MISSING_ROUTE | 5 |
| BROKEN_MISSING_API | 0 |
| BROKEN_FAKE_SUCCESS | 0 |
| BROKEN_SILENT_FAILURE | 0 |
| BROKEN_STUCK_LOADING | 0 |
| BROKEN_PERMISSION_LEAK | 8 |
| UNIMPLEMENTED_BUT_ACTIVE | 4 |
| NOT_SAFE_TO_EXECUTE | 31 (same set as mutation-wired operational CTAs) |
| NOT_TESTED_BLOCKED | 6 (live MFA form, password reset, invitation accept, sign-out, forgot-password submit, unsampled dynamic IDs) |
| Future honest placeholders (excluded as broken) | 16 module routes |

## 4. P0 findings

| ID | Finding | Evidence | Batch |
| --- | --- | --- | --- |
| P0-01 | Sign-in form has no `method="post"`. When the React `onSubmit` listener does not attach, the browser issues GET and places `organizationContext`, `email`, and `password` in the query string. | `apps/web/src/features/saas-identity/components/sign-in-form.tsx` (`<form className="auth-form" onSubmit={handleSubmit} noValidate>`). Observed MCP URL `/giris?organizationContext=...&email=...&password=...`. `stripSensitiveQueryParam` exists in `saas-identity-csrf.ts` but is not used here. | APP-ACTION-WIRING-1A |
| P0-02 | Dashboard quick action “Bakım Talebi” stays active for roles that receive HTTP 403 on `/bakim`. | `quick-actions-panel.tsx` href `/bakim` with no permission filter. machine-responsible and materials-only: `/dashboard` 200, `/bakim` 403. ADMIN-ACCESS-1 left dashboard links ungated. | DASHBOARD-ACTION-WIRING-1 |
| P0-03 | `/bakim/planlama` does not call `forbidden()`. Roles without `maintenance.read` receive HTTP 200 and the same heading/control count as tenant-owner. | `bakim/page.tsx` uses `forbidden()`; `bakim/planlama/page.tsx` does not. HTTP: machine-responsible and materials-only `/bakim` 403, `/bakim/planlama` 200 “Bakım Planlama”, 9 buttons. | APP-ACTION-WIRING-1A |
| P0-04 | `/kaliplar` is sidebar-hidden without `mold.read` but the page has no `forbidden()`. machine-responsible (template: `edge.read` only) received HTTP 200 “Kalıplar” with 33 buttons (full registry, including create toggle). | `AdminAccessRoleTemplates.cs` machine-responsible `[EdgePermissionKeys.Read]`. `kaliplar/page.tsx` has no `forbidden()`. `mold-registry-view.tsx` create `onClick`. | APP-ACTION-WIRING-1B |
| P0-05 | `/vardiyalar` is sidebar-gated by `shift-workforce.read` but the page does not `forbidden()`. Restricted personas received HTTP 200 “Vardiyalar” with the same 10-button shell+workspace as owner. | `primary-navigation.ts` permissionKey; `vardiyalar/page.tsx` in-page unauthorized only if reader fails. HTTP 200 for machine-responsible / materials-only / maintenance-only. | APP-ACTION-WIRING-1B |
| P0-06 | `/proses-oee` same pattern as P0-05. Restricted personas HTTP 200 “Proses & OEE”, 17 buttons / 1 disabled — full workspace, not the unauthorized alert. | `proses-oee/page.tsx`; role templates omit `process-oee.read` for the three personas. | APP-ACTION-WIRING-1B |

No fake persisted success and no executed unauthorized mutation were observed. P0-04/05/06 are unauthorized **active UIs**; API write probes were not executed.

## 5. P1 findings

| ID | Finding | Evidence | Batch |
| --- | --- | --- | --- |
| P1-01 | “AI'ya Görev Ver” navigates to `/ai-gorevleri/yeni` which is not in `navigationItems` and has no `page.tsx`. Catch-all calls `notFound()`. HTTP 404. | `dashboard/page.tsx` L47-50; `app/(app)/page.tsx` L41-44; `[...sections]/page.tsx` L38-39. | DASHBOARD-ACTION-WIRING-1 |
| P1-02 | Dashboard “CrevaFact Teams” is an enabled `<button>` with no `onClick`. | `dashboard/page.tsx` L75-117. | DASHBOARD-ACTION-WIRING-1 |
| P1-03 | Maintenance CTAs “Yeni Arıza Kaydı” and empty-state `actionHref` target `/bakim/arizalar/yeni`. HTTP 404. | `maintenance-dashboard-view.tsx` L148, L429. | APP-ACTION-WIRING-1A |
| P1-04 | “Periyodik Bakım Planı” / “Yeni Periyodik Plan” target `/bakim/planlar/yeni`. HTTP 404. | `maintenance-dashboard-view.tsx` L162, L523; `maintenance-planning-view.tsx` L203, L262. | APP-ACTION-WIRING-1A |
| P1-05 | “Yeni Bakım Emri” targets `/bakim/emirler/yeni`. Dynamic `[id]` treats `yeni` as an id. HTTP 200 heading “Bakım Emri Detayı” with load-failure panel — not a create form. | `maintenance-dashboard-view.tsx` L155; `bakim/emirler/[id]/page.tsx` L52-79. | APP-ACTION-WIRING-1A |
| P1-06 | Order “Onaya gönder” / “ERP'ye aktar” have no `onClick`. Preview drafts are `needs-review`, so buttons are currently disabled with titles. When `canSubmitOrderDraftForApproval` / `canTransferOrderDraft` is true they become active no-ops. | `order-draft-action-panel.tsx` L151-175; `order-draft.ts` L167-186. | APP-ACTION-WIRING-1A |

## 6. P2/P3 findings

| ID | Sev | Finding | Evidence | Batch |
| --- | --- | --- | --- | --- |
| P2-01 | P2 | Workspace selector is an enabled button; aria-label admits the feature is future work. | `top-bar.tsx` L15 | DASHBOARD-ACTION-WIRING-1 |
| P2-02 | P2 | Search placeholder is an enabled button (`⌘ K`) with no handler. | `top-bar.tsx` L23 | DASHBOARD-ACTION-WIRING-1 |
| P2-03 | P2 | Notifications `IconButton` has no `onClick`. | `top-bar.tsx` L28 | DASHBOARD-ACTION-WIRING-1 |
| P2-04 | P2 | Account button has `aria-haspopup="menu"` and no menu/`onClick`. Sign-out beside it is wired. | `account-menu.tsx` L37-51 vs L52-61 | DASHBOARD-ACTION-WIRING-1 |
| P2-05 | P2 | Forgot-password and MFA forms also omit `method="post"` (same GET fallback class; MFA/forgot fields are less severe than password-in-URL but same defect). | `forgot-password-form.tsx` L51; `mfa-verify-form.tsx` L56 | APP-ACTION-WIRING-1A |
| P2-06 | P2 | Dashboard quick actions “Üretim Emri”, “Plan Revizyonu”, “Kalite Kaydı” navigate to list pages, not create/revise flows. Labels oversell. | `quick-actions-panel.tsx` L34-61 | DASHBOARD-ACTION-WIRING-1 |
| P2-07 | P2 | `/bakim/arizalar/${id}` and `?convert=1` row links have no matching pages (same catch-all 404 family as P1-03). | `maintenance-dashboard-view.tsx` L468, L497, L502 | APP-ACTION-WIRING-1A |
| P3-01 | P3 | AI summary footer “Tüm AI görevlerini görüntüle” is a non-interactive `div`. | `ai-task-center-summary.tsx` L488-489 | DASHBOARD-ACTION-WIRING-1 |
| P3-02 | P3 | Skip link `href="#main-content"` is valid (`id="main-content"` on the viewport). Not a placeholder hash. | `app-shell-frame.tsx` L30, L34 | (no batch) |
| P3-03 | P3 | In-page hashes `#ariza-kayitlari` and `#gecikenler` match real section ids. | `maintenance-dashboard-view.tsx` L425; `maintenance-planning-view.tsx` L221 | (no batch) |

## 7. Dashboard findings

Rendered by `apps/web/src/app/(app)/dashboard/page.tsx` using `dashboardPreviewData`.

| ID | Control | Type | Observed | Class | Sev | Batch |
| --- | --- | --- | --- | --- | --- | --- |
| CTL-D-01 | AI'ya Görev Ver | navigation | 404 `/ai-gorevleri/yeni` | BROKEN_MISSING_ROUTE | P1 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-02 | CrevaFact Teams | modal/panel | No handler | BROKEN_NO_HANDLER | P1 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-03 | Hızlı işlem: AI Görevi | navigation | `/` 200 | WORKING_NAVIGATION (label oversell P3) | P3 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-04 | Hızlı işlem: Üretim Emri | navigation | `/uretim` 200 | WORKING_NAVIGATION | P3 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-05 | Hızlı işlem: Plan Revizyonu | navigation | `/planlama` 200 | WORKING_NAVIGATION | P3 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-06 | Hızlı işlem: Bakım Talebi | navigation | `/bakim` 403 for denied roles | BROKEN_PERMISSION_LEAK | P0 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-07 | Hızlı işlem: Kalite Kaydı | navigation | `/kalite` 200, ungated | WORKING_NAVIGATION + broad-read gap | P2 | DASHBOARD-ACTION-WIRING-1 |
| CTL-D-08 | Hızlı işlem: Malzeme Riski | navigation | Honest placeholder 200 | WORKING_NAVIGATION | — | excluded (future module) |
| CTL-D-09 | Hızlı işlem: Termin Riski | navigation | Honest placeholder 200 | WORKING_NAVIGATION | — | excluded (future module) |
| CTL-D-10 | Hızlı işlem: ERP Senkronizasyonu | navigation | `/erp-entegrasyonlari/senkronizasyonlar` 200 | WORKING_NAVIGATION | — | DASHBOARD-ACTION-WIRING-1 (permission later) |
| CTL-D-11 | AI görevini incele (per card) | navigation | `disabled` + title | DISABLED_WITH_REASON | — | — |
| CTL-D-12 | Tüm üretim emirlerini görüntüle | navigation | `disabled` + title | DISABLED_WITH_REASON | — | — |
| CTL-D-13 | Bakım planını görüntüle | navigation | `disabled` + title | DISABLED_WITH_REASON | — | — |
| CTL-D-14 | Plan confidence CTA | navigation | `disabled` + title | DISABLED_WITH_REASON | — | — |
| CTL-D-15 | Footer “Tüm AI görevlerini görüntüle” | (not a control) | `div` | P3 affordance | P3 | DASHBOARD-ACTION-WIRING-1 |

`DashboardHeader` “Planı incele” (`dashboard-header.tsx`) is **not rendered** by the current dashboard page and is not counted.

Dashboard HTTP (owner): 13 buttons, 46 links, 12 disabled. Restricted personas: 13 buttons, 41 links (five permission-keyed sidebar items removed), 12 disabled — page CTAs including Bakım Talebi remain.

## 8. Module-by-module control inventory

### 8.1 App shell (all authenticated routes)

Source: `app/(app)/layout.tsx` → `resolvePermissionAwareNavigation()` → `AppShellFrame` / `Sidebar` / `TopBar` / `AccountMenu`.

| ID | Control | Type | Class | Permission | Notes |
| --- | --- | --- | --- | --- | --- |
| CTL-S-01 | Ana içeriğe geç | navigation | WORKING_LOCAL_INTERACTION | — | `#main-content` exists |
| CTL-S-02 | CrevaFactOS brand | navigation | WORKING_NAVIGATION | — | href `/` |
| CTL-S-03 | Kenar çubuğunu daralt/genişlet | local UI | WORKING_LOCAL_INTERACTION | — | `onClick={onToggle}` |
| CTL-S-04–38 | 35 primary nav links | navigation | WORKING_NAVIGATION or PERMISSION_HIDDEN | see §9 | Filter only when `permissionKey` set |
| CTL-S-39 | Şirket / tesis seçimi | local UI | UNIMPLEMENTED_BUT_ACTIVE | none | Enabled; future copy in aria-label |
| CTL-S-40 | Operasyonlarda ara | local UI | UNIMPLEMENTED_BUT_ACTIVE | none | Enabled; no handler |
| CTL-S-41 | Bildirimler | local UI | UNIMPLEMENTED_BUT_ACTIVE | none | Enabled; no handler |
| CTL-S-42 | Hesap: {name} | modal/panel | UNIMPLEMENTED_BUT_ACTIVE | session | `aria-haspopup` without menu |
| CTL-S-43 | Oturumu kapat | mutation | WORKING_MUTATION_WIRED | session | `signOutSaasIdentityBrowser` → `/api/identity/sign-out`. Not executed. |

### 8.2 Authentication

| ID | Control | Route | Class | API | Notes |
| --- | --- | --- | --- | --- | --- |
| CTL-A-01 | Oturum aç | `/giris` | WORKING_MUTATION_WIRED + P0-01 | POST `/api/identity/sign-in` | Handler is real; native GET is the defect |
| CTL-A-02 | Parolamı unuttum | `/giris` | WORKING_NAVIGATION | — | `/sifremi-unuttum` |
| CTL-A-03 | Forgot submit | `/sifremi-unuttum` | WORKING_MUTATION_WIRED | POST `/api/identity/password/forgot` | NOT_SAFE_TO_EXECUTE (email). Same missing `method`. |
| CTL-A-04 | MFA tabs | `/mfa-dogrula` | WORKING_LOCAL_INTERACTION | — | totp / recovery |
| CTL-A-05 | MFA submit | `/mfa-dogrula` | WORKING_MUTATION_WIRED | POST `/api/identity/mfa/verify` | NOT_TESTED_BLOCKED in MCP (API MFA used) |
| CTL-A-06 | Reset / invitation submits | auth pages | WORKING_MUTATION_WIRED | identity API | NOT_TESTED_BLOCKED (token flows) |

### 8.3 AI Task Center

| ID | Control | Class | Evidence |
| --- | --- | --- | --- |
| CTL-AI-01 | AI'ya Görev Ver | BROKEN_MISSING_ROUTE | `/ai-gorevleri/yeni` 404 |
| CTL-AI-02 | Veriyi yenile | DISABLED_WITH_REASON | title: gerçek veri bağlantısı yok |
| CTL-AI-03 | KPI links / filter bar / presets / reset / cards | WORKING_NAVIGATION + WORKING_LOCAL_INTERACTION | `ai-task-kpi-links.tsx`, `ai-task-workspace.tsx`, cards → `/ai-gorevleri/${id}` 200 for `ai-task-001` |
| CTL-AI-04 | İncelemeye gönder / Onayla / Uygula / Ertele / Reddet / Yeniden değerlendir | DISABLED_WITH_REASON | `ai-task-action-panel.tsx` titles: “Gerçek … servisi henüz bağlı değil.” |

### 8.4 CrevaFactOS Teams / Energy / IoT / Operational Risk / Commercial Intelligence / Reports / Settings

Catch-all `SectionPlaceholderPage` when `href` is in `navigationItems`. HTTP 200, badge “Yer tutucu”, EmptyState says content will come later. **Not classified as broken.** No page-level CTA. Shell only.

Dashboard Teams **button** (CTL-D-02) is a different control and is broken.

### 8.5 Orders

| ID | Control | Class | Evidence |
| --- | --- | --- | --- |
| CTL-O-01 | KPI / filters / draft cards | WORKING_NAVIGATION / local | `siparisler/page.tsx`, filtered workspace |
| CTL-O-02 | Draft detail | WORKING_NAVIGATION | `/siparisler/order-draft-001` 200 |
| CTL-O-03 | Onaya gönder | DISABLED_WITH_REASON now; BROKEN_NO_HANDLER when enabled | no `onClick` |
| CTL-O-04 | ERP'ye aktar | same as CTL-O-03 | no `onClick` |
| CTL-O-05 | Transfer command / readiness panels | WORKING_LOCAL_INTERACTION | local `createOrderTransferCommand` preview; not a live ERP write |

### 8.6 Planning

| ID | Control | Class | Notes |
| --- | --- | --- | --- |
| CTL-P-01 | List / yeni / detail / düzenle nav | WORKING_NAVIGATION | `/planlama`, `/planlama/yeni` 200 |
| CTL-P-02 | Draft create form | WORKING_MUTATION_WIRED | server actions in `production-plan-*` | NOT_SAFE_TO_EXECUTE |
| CTL-P-03 | Submit / revision / approve / reject | WORKING_MUTATION_WIRED | `production-plan-workflow-actions.tsx` + confirm dialogs + error state | NOT_SAFE_TO_EXECUTE (includes approve) |
| CTL-P-04 | Scheduling / handoff / transfer | WORKING_MUTATION_WIRED | dedicated action modules | NOT_SAFE_TO_EXECUTE |

### 8.7 Production

| ID | Control | Class | Notes |
| --- | --- | --- | --- |
| CTL-PR-01 | Order list / detail nav | WORKING_NAVIGATION | `/uretim` 200 |
| CTL-PR-02 | Start / pause / resume / complete | WORKING_MUTATION_WIRED | `production-order-lifecycle-actions.tsx` | NOT_SAFE_TO_EXECUTE |
| CTL-PR-03 | Operator assign | WORKING_MUTATION_WIRED | `production-operator-actions.tsx` | NOT_SAFE_TO_EXECUTE |

### 8.8 Export

| ID | Control | Class | Notes |
| --- | --- | --- | --- |
| CTL-E-01 | Export home / case / country research nav | WORKING_NAVIGATION | `/ihracat` 200 |
| CTL-E-02 | Document / AI / case writers | WORKING_MUTATION_WIRED | `export-case-writer.ts`, `export-4l-writer.ts` | NOT_SAFE_TO_EXECUTE |

### 8.9 Shifts / Process-OEE / Machines / Molds / Materials

| ID | Module | Controls | Class | Notes |
| --- | --- | --- | --- | --- |
| CTL-SH-01 | Shifts | sync / pagination / assignment / risk actions | WORKING_MUTATION_WIRED + local | `shift-*-actions.ts`. Page leak: P0-05 |
| CTL-OE-01 | Process-OEE | evaluate / filters | WORKING_MUTATION_WIRED + local | `process-oee-actions.ts`. Page leak: P0-06 |
| CTL-MCH-01 | Machines | workspace / detail | WORKING_NAVIGATION / local | `forbidden()` + `edge.read`. API denied for materials/maintenance personas (ADMIN-ACCESS-1) |
| CTL-MLD-01 | Molds | create toggle, filters, row select, history | WORKING_LOCAL + WORKING_MUTATION_WIRED | `mold-actions.ts` / `mold-writer.ts`. Page leak: P0-04. Mutations NOT_SAFE_TO_EXECUTE |
| CTL-MAT-01 | Materials | registry / create / filters | WORKING_MUTATION_WIRED + local | `forbidden()` + `material.read`. Mutations NOT_SAFE_TO_EXECUTE |

### 8.10 Maintenance

| ID | Control | Class | Target |
| --- | --- | --- | --- |
| CTL-MN-01 | Yeni Arıza Kaydı | BROKEN_MISSING_ROUTE | `/bakim/arizalar/yeni` 404 |
| CTL-MN-02 | Yeni Bakım Emri | BROKEN_WRONG_ROUTE | `/bakim/emirler/yeni` → detail error |
| CTL-MN-03 | Periyodik Bakım Planı | BROKEN_MISSING_ROUTE | `/bakim/planlar/yeni` 404 |
| CTL-MN-04 | Workspace nav (planlama / raporlama / kestirici) | WORKING_NAVIGATION | 200 for owner; planlama leaked (P0-03) |
| CTL-MN-05 | Work-order / asset row links that exist | WORKING_NAVIGATION | `/bakim/emirler/${id}`, `/bakim/varliklar/${ref}` |
| CTL-MN-06 | Arıza row / convert links | BROKEN_MISSING_ROUTE | `/bakim/arizalar/${id}` |
| CTL-MN-07 | In-page KPI hashes | WORKING_LOCAL_INTERACTION | `#ariza-kayitlari` |
| CTL-MN-08 | Generate next due / create WO from due | WORKING_MUTATION_WIRED | `maintenance-writer.ts` | NOT_SAFE_TO_EXECUTE |
| CTL-MN-09 | Notes / parts on WO detail | WORKING_MUTATION_WIRED | NOT_SAFE_TO_EXECUTE |

### 8.11 Quality

| ID | Control | Class | Notes |
| --- | --- | --- | --- |
| CTL-Q-01 | Home / NCR / reporting / new inspection / new plan | WORKING_NAVIGATION | all HTTP 200 |
| CTL-Q-02 | Start / record / complete / release / reject inspection | WORKING_MUTATION_WIRED | `quality-inspection-actions.tsx` error+refresh | NOT_SAFE_TO_EXECUTE |
| CTL-Q-03 | NCR actions | WORKING_MUTATION_WIRED | `quality-ncr-actions.tsx` | NOT_SAFE_TO_EXECUTE |

No `permissionKey` on `/kalite`. Restricted personas can open `/siparisler` and (by the same gap) quality if they navigate there. Broad-read gap, not a missing handler.

### 8.12 ERP

| ID | Control | Class | Notes |
| --- | --- | --- | --- |
| CTL-ERP-01 | Workspace nav among five sidebar ERP routes | WORKING_NAVIGATION | all 200 |
| CTL-ERP-02 | Writeback approve / execute / reject | WORKING_MUTATION_WIRED | `erp-writeback-action-panel.tsx` `onClick` + pending + errors | NOT_SAFE_TO_EXECUTE (ERP write / approve) |

### 8.13 Forbidden / empty / loading

| Surface | Evidence | Class |
| --- | --- | --- |
| `forbidden.tsx` | h1 “Erişim reddedildi” | WORKING (403 interrupt) |
| Catch-all EmptyState | honest unavailable | DISABLED_WITH_REASON / intentionally unavailable |
| Default Next 404 | `/ai-gorevleri/yeni`, `/bakim/arizalar/yeni`, `/bakim/planlar/yeni` | BROKEN_MISSING_ROUTE for the linking control |
| Suspense KPI/workspace fallbacks | `AiTaskKpiLoading`, `AiTaskWorkspaceLoading` | loading only; no stuck-loading control found |
| No `not-found.tsx` | framework default | recorded |

## 9. Permission and route/API enforcement gaps

Sidebar filter: `navigation-visibility.ts` hides an item only when `permissionKey` is set **and** `hasGrantedPermission` is false. Items without a key always render.

**Sidebar entries without `permissionKey` (29):** AI Görev Merkezi, Dashboard, Teams, Siparişler, Planlama, Üretim, İhracat, Kalite Kontrol, Enerji, IoT & PLC, all six Operational Risk items, all six Commercial Intelligence items, all five ERP items, Raporlama, Ayarlar.

**Sidebar entries with keys (6):** `/vardiyalar` `shift-workforce.read`, `/proses-oee` `process-oee.read`, `/makineler` `edge.read`, `/kaliplar` `mold.read`, `/malzemeler` `material.read`, `/bakim` `maintenance.read`.

**Pages that call `forbidden()`:** only `/makineler`, `/malzemeler`, `/bakim` (index). Confirmed HTTP 403 for denied personas on those three.

**Direct route / API mismatch**

| Route | Nav hidden? | Direct HTTP (denied persona) | Notes |
| --- | --- | --- | --- |
| `/makineler` | yes | 403 | Matches API deny (`/api/edge/machines` 403) — ADMIN-ACCESS-1 |
| `/malzemeler` | yes | 403 | Matches Materials API deny |
| `/bakim` | yes | 403 | Matches Maintenance API deny on index |
| `/bakim/planlama` | n/a (subroute) | **200** | Index 403 does not protect children |
| `/kaliplar` | yes | **200** full UI | No page `forbidden()` |
| `/vardiyalar` | yes | **200** full UI | In-page deny only if reader fails |
| `/proses-oee` | yes | **200** full UI | Same |
| `/siparisler`, `/planlama`, `/uretim`, `/kalite`, `/dashboard` | never hidden | 200 | No module key |

**Persona UI (this audit + ADMIN-ACCESS-1)**

| Persona | Sidebar Machines | Materials | Maintenance | Dashboard Bakım quick action | `/bakim/planlama` |
| --- | --- | --- | --- | --- | --- |
| machine-responsible (`edge.read` only) | visible | hidden | hidden | **active** | **200** |
| materials-only (`material.read` only) | hidden | visible | hidden | **active** | **200** |
| maintenance-only (`maintenance.read`+execute) | hidden | hidden | visible | active (allowed) | 200 |

Link counts: owner dashboard 46 links; each restricted persona 41 links (−5 keyed items). Dashboard **buttons stay at 13** including Teams and disabled inspect CTAs.

**MFA:** Tenant-owner mutations on `/api/admin-access` require recent MFA (ADMIN-ACCESS-1). This web app has **no admin-access UI**. Customer operational actions do not surface a “recent MFA required” message. Identity MFA page exists and is wired.

**Broad `read`/`manage`:** Existing catalog keys remain coarse (`material.read`, `edge.read`, `maintenance.read`, plus manage/execute variants). `own`/`assigned` scopes are not grantable. Honest: this audit does not claim fine-grained action coverage.

**Unauthorized active controls:** dashboard Bakım Talebi; full mold/shift/OEE/maintenance-planning UIs for personas that lack those keys; all ungated factory/commercial/ERP sidebar targets.

## 10. Controls intentionally disabled

| Control | Reason shown | Source |
| --- | --- | --- |
| Veriyi yenile | Gerçek veri bağlantısı sonraki entegrasyon paketlerinde | `app/(app)/page.tsx` |
| AI task decision buttons | Gerçek … servisi henüz bağlı değil | `ai-task-action-panel.tsx` |
| AI inspect on dashboard cards | Görev inceleme akışı sonraki paketlerde | `ai-task-center-summary.tsx` |
| Tüm üretim emirlerini görüntüle | Üretim emri detayları sonraki paketlerde | `production-orders-table.tsx` |
| Bakım planını görüntüle | Bakım ve arıza detayları sonraki paketlerde | `maintenance-status-panel.tsx` |
| Plan confidence CTA | Plan güvenilirliği detayları sonraki paketlerde | `plan-confidence-panel.tsx` |
| Order Onaya gönder / ERP'ye aktar (preview `needs-review`) | Title explains blocking/approval prerequisites | `order-draft-action-panel.tsx` |
| Catch-all placeholders | EmptyState + “Yer tutucu” | `[...sections]/page.tsx` |

No permanently disabled control without a `title` or EmptyState explanation was found in route-rendered dashboard/AI/order panels. Shell future controls are **enabled**, not disabled (P2-01–P2-04).

## 11. Controls blocked from safe execution

Not clicked / not submitted:

- Production start/pause/resume/complete and operator assign
- Planning approve / reject / submit / scheduling / handoff / transfer
- Maintenance work-order create, notes, parts, generate-next-due
- Quality start/complete/release/reject and NCR mutations
- ERP writeback approve/execute
- Export case writers
- Material/mold create-update-delete
- Shift assignment/risk mutations
- Process-OEE evaluate writes
- Forgot-password / reset / invitation / sign-out
- Live MCP MFA form (API MFA used instead)

These are classified `WORKING_MUTATION_WIRED` only when the component calls a real server action/API and handles failure (message + pending + refresh). They remain `NOT_SAFE_TO_EXECUTE` for this audit.

## 12. Recommended DASHBOARD-ACTION-WIRING-1 scope

1. Replace or hide `/ai-gorevleri/yeni` (CTL-D-01 / CTL-AI-01) with a real create route or disable-with-reason.
2. Wire or disable CrevaFact Teams (CTL-D-02).
3. Gate quick actions with the same permission keys as navigation; remove Bakım Talebi for denied roles (P0-02).
4. Align quick-action labels with actual target pages (list vs create).
5. Disable-or-implement workspace selector, search, notifications, and account menu (P2-01–P2-04).
6. Turn the AI footer into a real link to `/` or remove the affordance (P3-01).
7. Do **not** treat honest risk/commercial placeholder **pages** as dashboard defects; only the oversell/leak of their **entry points** if they imply an executable action.

Recommended control count for that package: **12** (D-01, D-02, D-06, D-03–D-05/D-07 label+gate, D-15, S-39–S-42).

## 13. Recommended APP-ACTION-WIRING-1 batches

### APP-ACTION-WIRING-1A — Auth GET hardening + Maintenance create/detail routes + Order HITL handlers

Shared surfaces: identity forms; `maintenance-dashboard-view` / `maintenance-planning-view`; `bakim/emirler/[id]`; `order-draft-action-panel`.

- P0-01, P2-05 (`method="post"` + strip sensitive query)
- P0-03 (`forbidden()` on all `/bakim/**` pages, not only index)
- P1-03, P1-04, P1-05, P2-07 (create/detail routes or honest disable)
- P1-06 (wire or disable-with-reason even when `canSubmit`/`canTransfer` is true)

### APP-ACTION-WIRING-1B — Page-level `forbidden()` for remaining keyed modules + honest ungated-module plan

Shared pattern: copy ADMIN-ACCESS-1 `forbidden()` + `hasGrantedPermission` onto `/kaliplar`, `/vardiyalar`, `/proses-oee`, and decide whether Orders/Planning/Production/Quality/Export/ERP get keys or stay explicitly public.

- P0-04, P0-05, P0-06
- Record remaining sidebar items without keys as a product decision, not silent leaks

No third batch: the split is route-family (maintenance/auth/orders vs permission wrappers), not file-count.

## 14. Explicitly unaudited/blocked routes

| Route / flow | Reason |
| --- | --- |
| `/mfa-dogrula` live form in MCP browser | React submit did not attach; MFA completed via identity API instead |
| `/sifre-sifirla`, `/davet/kabul` | Token-gated; not executed |
| `/planlama/[planId]`, `/uretim/[orderId]`, export/quality/maintenance asset detail IDs | Statically traced from parent; not every id HTTP-fetched |
| Nested ERP document/order/writeback detail pages | Nav family sampled; mutations not executed |
| Sign-out | Allowed only at end; not required after HTTP session |
| Admin-access UI | Out of scope (SAAS-ADMIN-1); no customer admin screens exist |

No route in the primary navigation was left unreached for tenant-owner HTTP GET.

## 15. Acceptance checklist

- [x] Every primary-nav route reached (HTTP) or classified as missing (404)
- [x] Broken candidates confirmed by component/action tracing, not text search alone
- [x] `href="#"` exact: 0 in `apps/web/src`
- [x] Permission personas reviewed (sidebar + direct route + dashboard leftovers)
- [x] Mutations classified from wiring, not executed when destructive
- [x] Future placeholders not marked broken merely for unfinished scope
- [x] No secrets in this artifact
- [x] Remediation assigned to DASHBOARD-ACTION-WIRING-1, APP-ACTION-WIRING-1A, APP-ACTION-WIRING-1B
- [x] Application action integrity is **FAIL** (broken and leaking controls remain)

## Evidence appendix — targeted static searches

| Probe | Result |
| --- | --- |
| `href="#"` / `href={"#"}` in `apps/web/src` | 0 matches |
| `console.log` in `components/`, `ai-task-center`, `orders/components` | 0 matches |
| `"use server"` on route-referenced writers | present for planning, production, export, quality, maintenance, materials, molds, shifts, OEE, ERP |
| Hardcoded success in sign-in / AI / order action panels | not found; forgot-password success follows a real API result |
)
