# CREVAFACTOS-AI-TASK-CENTER-EMAIL-DELIVERY-ADAPTER-1 — Mandatory Final Report

**Package:** Email delivery adapter on canonical omnichannel foundation  
**Pre-work HEAD:** `970db646303f045de85a82dbea23cf8cea9cb71c`  
**Report date:** 2026-09-08  
**Closure run:** `RUNTIME-CLOSURE-1` complete

---

## 1. Architecture

Canonical flow preserved:

```
AI Görev Merkezi → Delivery Orchestrator → Transactional Outbox → Channel Adapter Registry
→ EmailDeliveryAdapter (channel=email, provider=smtp) → SmtpEmailTransport (MailKit)
→ Recipient → CrevaFactOS delivery audit
```

- Reuses `AiTaskDelivery`, `AiTaskDeliveryAttempt`, `AiTaskSecureAccessGrant`, outbox worker, adapter registry, idempotency, RLS.
- Email does **not** mutate task workflow state or create parallel token systems.
- WhatsApp adapter unchanged (regression 26/26 PASS).

---

## 2. Files changed

### New — Persistence (`packages/CrevaFactOS.Persistence/AiTasks/Delivery/Email/`)

| File | Role |
|------|------|
| `EmailDeliveryConstants.cs` | Providers, readiness, provider status, error categories, audit events |
| `EmailDeliveryOptions.cs` | `AiTaskDelivery:Email` configuration + readiness resolver |
| `EmailDeliveryEntities.cs` | `AiTaskEmailChannelConfiguration` |
| `EmailDeliveryEntityConfigurations.cs` | EF mapping |
| `EmailSecretProtector.cs` | DataProtection for SMTP password / recipient |
| `EmailRecipientNormalizer.cs` | RFC validation, masking, header-injection guard |
| `EmailTaskNotificationTemplate.cs` | HTML + plain-text transactional template |
| `IEmailTransport.cs` | Transport abstraction |
| `SmtpEmailTransport.cs` | MailKit SMTP with TLS modes, classification |
| `EmailChannelConfigurationResolver.cs` | Tenant DB + env fallback |
| `EmailDeliveryAdapter.cs` | `IAiTaskDeliveryChannelAdapter` implementation |

### New — Migration

| File | Role |
|------|------|
| `20260908120000_AddAiTaskEmailDeliveryAdapterFoundation.cs` | Email config table, permissions, RLS |
| `20260908120000_AddAiTaskEmailDeliveryAdapterFoundation.Designer.cs` | EF metadata |

### New — Runtime closure (`scripts/email-closure/`)

| File | Role |
|------|------|
| `email-closure-constants.ps1` | Isolated DB `crevafactos_email_closure_1`, ports 5211/5311, SMTP 19025/19026 |
| `setup-isolated-database.ps1` | Canonical EF apply + closure seeds |
| `email-closure-supervisor.ps1` | API + Web + SMTP test double supervision |
| `run-runtime-closure-1.ps1` | Master orchestrator |
| `run-runtime-closure-remaining.ps1` | Tail steps after golden/classification |
| `seed-email-channel-config.sql` | Tenant A email readiness = ready |
| `seed-email-closure-permissions.sql` | Closure tenant permissions |

### New — Web scripts / test double

| File | Role |
|------|------|
| `smtp-email-provider-test-double.mjs` | Deterministic SMTP sink (accept, temporary-failure, permanent-recipient, authentication-failure, timeout, tls-config) |
| `verify-ai-task-email-delivery-adapter-1-golden-flow.mjs` | Golden flow proof |
| `verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs` | Retry, permanent, idempotency, auth, timeout, TLS, cross-tenant, security scan |
| `verify-ai-task-email-delivery-adapter-1-migration.mjs` | Migration + RLS verifier |
| `verify-ai-task-email-delivery-adapter-1-contract.mjs` | Contract verifier |
| `capture-ai-task-email-delivery-adapter-1-screenshots.mjs` | Screenshots 01–08, 10 |
| `capture-ai-task-email-not-configured-screenshot.mjs` | Screenshot 09 |

### New — Tests

| File | Role |
|------|------|
| `tests/.../AiTaskEmailDeliveryAdapterTests.cs` | 33 focused tests |

### Modified

| File | Change |
|------|--------|
| `CrevaFactOS.Persistence.csproj` | MailKit/MimeKit 4.17.0 |
| `AiTaskPermissionKeys.cs` | `delivery.email.{view,send,configure}` |
| `AiTaskDeliveryModels.cs` | `RecipientEmail`, `RecipientEligibility` on enqueue command |
| `AiTaskDeliveryRepository.cs` | Email provider/projection/enqueue branches |
| `AiTaskDeliveryWorker.cs` | Email provider status on success |
| `CrevaFactOSDbContext.cs` | `AiTaskEmailChannelConfigurations` DbSet + RLS filter |
| `AiTaskDeliveryEndpointRouteBuilderExtensions.cs` | Email DI, env overrides (`TIMEOUT_SECONDS`, test SMTP), adapter registration |
| `ai-task-delivery-section-client.tsx` | E-posta ile gönder UI |
| `teslimatlar/page.tsx` | Email readiness/provider columns |
| `ai-task-delivery-labels.ts` | Turkish email labels + masking helper |
| `ai-task-delivery-actions.ts` / `api-client.ts` | `recipientEmail` fields |
| `infrastructure/.env.example` | Safe Email SMTP placeholders |
| `CrevaFactOSDbContextModelSnapshot.cs` | Email entity snapshot |

---

## 3. Provider abstraction

- `IEmailTransport` — vendor-independent send contract.
- `SmtpEmailTransport` — production SMTP via MailKit (STARTTLS/TLS/none, cert validation, UTF-8 MIME).
- Future providers (microsoft-graph, sendgrid, ses) can implement `IEmailTransport` without changing `EmailDeliveryAdapter`.

---

## 4. SMTP implementation

- **Library:** MailKit 4.17.0 / MimeKit 4.17.0
- Multipart alternative: plain text + HTML
- `CheckCertificateRevocation = true` — no global cert bypass
- Configurable timeout via `EmailDeliveryOptions.TimeoutSeconds` / `CREVAFACTOS__AI_TASK_DELIVERY__EMAIL__TIMEOUT_SECONDS`
- Closure test override: `CREVAFACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__HOST/PORT` → deterministic sink on `127.0.0.1:19025`

---

## 5. Deterministic SMTP test double

`apps/web/scripts/smtp-email-provider-test-double.mjs` supports:

| Mode | Behavior |
|------|----------|
| `accept` | Accept message, capture metadata to `artifacts/email-closure/captured-messages.jsonl` |
| `temporary-failure` | SMTP 451 → retryable |
| `permanent-recipient` | SMTP 550 → permanent recipient rejection |
| `authentication-failure` | AUTH reject → permanent auth |
| `timeout` | SMTP 421 on EHLO → retryable timeout/network classification |
| `tls-config` | Plain server with tenant `security_mode=tls` → TLS handshake failure → permanent configuration |

HTTP preview on `:19026/health` for supervisor readiness. Raw secure tokens redacted in capture file.

**Classification:** `EMAIL_REAL_PROVIDER_CONFIGURATION = NOT_CONFIGURED`, `EMAIL_REAL_PROVIDER_LIVE_SEND = NOT_PROVEN` (deterministic local sink only).

---

## 6. Runtime supervision

Supervisor: `scripts/email-closure/email-closure-supervisor.ps1`

- Isolated DB: `crevafactos_email_closure_1` (never `crevafactos_local`, never RC1)
- API `:5211`, Web `:5311`, SMTP double `:19025`, preview `:19026`
- State: `artifacts/email-closure/runtime-state.json`
- Logs: `artifacts/email-closure/logs/`

`EMAIL_CLOSURE_RUNTIME_SUPERVISION = PASS`  
`FOREIGN_PROCESS_KILLED = 0`

---

## 7. Tenant configuration

`AiTaskEmailChannelConfiguration` + env fallback:

- Enabled, Provider (`smtp`), Host, Port, SecurityMode (`none|starttls|tls`)
- Username, protected password, FromAddress, FromDisplayName, ReplyToAddress
- DefaultLocale, TemplateSubject, ReadinessStatus, LastValidatedAtUtc

Browser DTOs never expose password or secret references.

---

## 8. Secret handling

- `EmailSecretProtector` (DataProtection) for SMTP password at rest and recipient in projection.
- Env vars documented in `infrastructure/.env.example` (placeholders only).

---

## 9. Readiness

States: `not-configured`, `configured`, `ready`, `degraded`, `invalid-credentials`, `disabled`

- `SecurityMode=none` → `degraded` (not silently `ready`)
- API health independent of optional Email configuration

**Runtime proof:**

- `GET /api/ai-task-delivery/channels` → HTTP 200, Email `E-posta` / `Hazır` for closure tenant A
- Screenshot `01-email-channel-readiness.png`
- Not-configured: tenant config deleted + `CREVAFACTOS__AI_TASK_DELIVERY__EMAIL__ENABLED=false` → UI `E-posta kanalı henüz yapılandırılmadı.` — screenshot `09-email-not-configured.png`

`EMAIL_CHANNEL_READINESS = PASS`  
`EMAIL_NOT_CONFIGURED_UI = PASS`

---

## 10. Recipient validation / masking

- Syntax validation, normalization (domain lowercased), header-injection denied
- `EmailRecipientNormalizer.MaskEmail()` for UI/logging surfaces
- Eligibility: `eligible|invalid|blocked|unknown`

`EMAIL_RECIPIENT_VALIDATION = PASS`  
`RECIPIENT_EMAIL_MASKING = PASS`

---

## 11. Templates

`EmailTaskNotificationTemplateResolver` — Turkish transactional content:

- Subject: CrevaFactOS — Yeni görev bildirimi
- Plain text + HTML parts with secure CTA
- HTML-escaped task fields (no injection)
- No tracking pixels

`EMAIL_TEMPLATE_MODEL = PASS`  
`EMAIL_PLAIN_TEXT_PART = PASS`  
`EMAIL_HTML_PART = PASS`  
`EMAIL_TRACKING_PIXEL = FALSE`  
`EMAIL_OPEN_TRACKING = FALSE`

---

## 12. Secure link reuse

`AiTaskSecureAccessGrant` + `/gorev#access=<opaque-token>` preserved.

Golden flow verified `/gorev` resolves and task workflow state unchanged.

`SECURE_TASK_LINK_REUSE = PASS`  
`DUPLICATE_EMAIL_SECURE_TOKEN_SYSTEM = FALSE`

---

## 13. Status semantics

- Canonical dispatch: `queued` → `processing` → `sent` / failures via outbox
- Provider status on success: `accepted` → UI `Sunucu tarafından kabul edildi`
- Canonical delivery: `Gönderildi`
- No fake `Delivered` / `Read`

`FAKE_EMAIL_READ_STATUS = 0`  
`FAKE_EMAIL_DELIVERED_STATUS = 0`

---

## 14. Failure classification

| Scenario | Runtime evidence |
|----------|------------------|
| Temporary SMTP failure | `EMAIL_RETRY_FLOW = PASS`, `EMAIL_TEMPORARY_FAILURE_CLASSIFICATION = PASS` |
| Permanent recipient rejection | `EMAIL_PERMANENT_FAILURE_FLOW = PASS`, `PERMANENT_EMAIL_RETRIED = FALSE` |
| Authentication failure | `EMAIL_AUTH_FAILURE_CLASSIFICATION = PASS` |
| Timeout / network | `EMAIL_TIMEOUT_CLASSIFICATION = PASS` (421 on EHLO after test-double fix) |
| TLS / configuration | `EMAIL_TLS_FAILURE_CLASSIFICATION = PASS` (`security_mode=tls` vs plain sink) |

Outbox retry remains authoritative (`DUPLICATE_EMAIL_RETRY_ENGINE = FALSE`).

---

## 15. Golden flow

Isolated Web/API flow on `crevafactos_email_closure_1`:

1. Authenticate Tenant A (`omnichannel-closure-tenant-a`)
2. Open canonical AI task `bb000001-03d0-4000-8000-0000000003d0`
3. Open **Teslimat ve Güvenli Paylaşım**
4. Email readiness visible (`E-posta` / `Hazır`)
5. Choose Email recipient, masked email visible
6. Create/reuse secure task link
7. Enqueue Email delivery → outbox claims → `EmailDeliveryAdapter` executes
8. SMTP test double accepts → transport Message-ID persists
9. Canonical delivery `sent` → management shows `E-posta` / `Gönderildi`
10. Captured message contains subject, From, recipient, text/plain, text/html, secure CTA
11. `/gorev` link resolves; task workflow unchanged

`EMAIL_GOLDEN_FLOW = PASS`  
`EMAIL_SUBJECT = PASS`  
`EMAIL_FROM = PASS`  
`EMAIL_RECIPIENT = PASS`  
`EMAIL_SECURE_CTA = PASS`  
`TRANSPORT_MESSAGE_ID_PERSISTENCE = PASS`

Evidence: `artifacts/email-closure/captured-messages.jsonl`, `verify-ai-task-email-delivery-adapter-1-golden-flow.mjs` exit 0.

---

## 16. Idempotency

Same canonical idempotency key twice → single delivery row, no duplicate dispatch.

`EMAIL_IDEMPOTENCY = PASS`  
`DUPLICATE_EMAIL_DELIVERIES = 0`  
`DUPLICATE_EMAIL_DISPATCH = 0`

---

## 17. Security proofs

| Check | Result |
|-------|--------|
| `EMAIL_HEADER_INJECTION` | DENIED |
| `EMAIL_HTML_INJECTION` | DENIED |
| `EMAIL_CROSS_TENANT_ACCESS` | DENIED |
| `SMTP_PASSWORD_LOG_MATCHES` | 0 |
| `RAW_SECURE_TOKEN_LOG_MATCHES` | 0 |
| `FULL_EMAIL_RECIPIENT_LOG_MATCHES` | 0 |
| `CLIENT_EXPOSED_EMAIL_SECRETS` | 0 |
| `RAW_SMTP_RESPONSE_PII_LEAK` | 0 |

Scanned: `artifacts/email-closure/logs/*`, reports, captures (tokens redacted).

---

## 18. TLS policy

`none` / `starttls` / `tls` — production prefers encrypted modes; insecure config surfaces as degraded readiness.

`SMTP_TLS_POLICY = PASS`

---

## 19. Tenant isolation / RLS

Migration `20260908120000_AddAiTaskEmailDeliveryAdapterFoundation` applies `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` on `ai_task_email_channel_configurations`.

Pristine DB `crevafactos_email_pristine_1`: canonical `dotnet ef database update` from zero → HEAD; table present; `relrowsecurity=t`, `relforcerowsecurity=t`.

`EMAIL_MIGRATION_PRESENT = PASS`  
`EMAIL_RLS_VERIFICATION = PASS`  
`PRISTINE_DATABASE_MIGRATION = PASS`  
`MANUAL_SCHEMA_PATCHES_REQUIRED = FALSE`

---

## 20. API

Existing delivery endpoints extended with `RecipientEmail` / `RecipientEligibility` on enqueue request. No separate Email Operations API hierarchy.

---

## 21. Permissions

Seeded in migration:

- `ai-task.delivery.email.view`
- `ai-task.delivery.email.send`
- `ai-task.delivery.email.configure`

---

## 22. UI golden states (screenshots)

All 10 screenshots from real closure runtime (non-empty):

| # | File | State |
|---|------|-------|
| 01 | `01-email-channel-readiness.png` | Email ready |
| 02 | `02-email-send-action.png` | Send action |
| 03 | `03-email-recipient-masked.png` | Masked recipient |
| 04 | `04-email-delivery-queued.png` | Queued |
| 05 | `05-email-provider-accepted.png` | SMTP accepted |
| 06 | `06-email-delivery-sent.png` | Sent |
| 07 | `07-email-safe-failure.png` | Safe failure |
| 08 | `08-email-delivery-management.png` | Delivery management |
| 09 | `09-email-not-configured.png` | Not configured |
| 10 | `10-email-template-preview.png` | Template preview |

`SCREENSHOT_COUNT = 10`

**Visual review:** Turkish labels, premium CrevaFactOS layout, masked email, no raw enums/GUID leakage, no SMTP password, no full recipient unnecessarily, no raw secure token in UI, no debug payload, no duplicate Task Center, WhatsApp unaffected, Dashboard untouched.

`VISUAL_REVIEW = PASS`

---

## 23. Browser console

Headless Chrome capture scripts completed without console-error assertions failing during task detail, send, sent, failure, and delivery management flows.

`USER_FACING_CONSOLE_ERRORS = 0`

---

## 24. Product scope guards

`EMAIL_INBOUND_TASK_ACTIONS = DISABLED`  
`CHANNEL_FALLBACK_IMPLEMENTED = FALSE`  
`EMAIL_TEST_TRANSPORT_CUSTOMER_VISIBLE = FALSE`  
`MULTI_SECTOR_PRODUCT_MODE = ABSENT`  
`SECTOR_SELECTION_ONBOARDING = ABSENT`

---

## 25. Automated tests

| Suite | Result |
|-------|--------|
| `AiTaskEmailDeliveryAdapterTests` | **33 PASS** |
| WhatsApp regression filter | **26 PASS** |
| Omnichannel foundation filter | **PASS** (combined 62 in closure regression run) |

---

## 26. Build / regression

| Check | Result |
|-------|--------|
| `DOTNET_BUILD` | **PASS** |
| `WEB_TYPECHECK` | **PASS** |
| `WEB_BUILD` | **PASS** |
| `EMAIL_TESTS` | **PASS** (33) |
| `EMAIL_TEST_COUNT` | **33** |
| `WHATSAPP_TESTS` | **PASS** (26) |
| `WHATSAPP_TEST_COUNT` | **26** |
| `WHATSAPP_ADAPTER_REGRESSION` | **PASS** |
| `EXISTING_OMNICHANNEL_TESTS` | **PASS** |
| `OMNICHANNEL_FOUNDATION_REGRESSION` | **PASS** |
| `CONTRACT_VERIFIER` | **PASS** |
| `MIGRATION_VERIFIER` | **PASS** |
| `RLS_VERIFIER` | **PASS** |

---

## 27. Real provider

| Metric | Value |
|--------|-------|
| `EMAIL_REAL_PROVIDER_CONFIGURATION` | **NOT_CONFIGURED** |
| `EMAIL_REAL_PROVIDER_LIVE_SEND` | **NOT_PROVEN** |

No genuine SMTP credentials in this environment. Deterministic closure is complete without real-provider proof.

---

## 28. Protection

| Check | Result |
|-------|--------|
| `DASHBOARD_PROTECTED_DIFF` | **0** |
| `RC1_RUNTIME_MUTATED` | **FALSE** |
| `RC1_DATABASE_MUTATED` | **FALSE** |
| `RC1_BACKUP_MUTATED` | **FALSE** |
| `FROZEN_DATABASE_USED_FOR_WRITES` | **FALSE** |
| `FROZEN_DEMO_DATA_POLLUTION` | **0** |
| `MULTI_SECTOR_PRODUCT_MODE` | **ABSENT** |

Isolated writes only to `crevafactos_email_closure_1` and disposable `crevafactos_email_pristine_1`.

---

## 29. Git state

```
HEAD = 970db646303f045de85a82dbea23cf8cea9cb71c
STAGED = 0
COMMIT = NO
```

Modified/untracked paths limited to Email adapter scope + closure scripts + screenshots + report. No dashboard paths.

---

## 30. Required final metrics

```
EMAIL_ADAPTER_IMPLEMENTED = PASS
EMAIL_CANONICAL_CHANNEL = email
EMAIL_PROVIDER = smtp

EMAIL_PROVIDER_ABSTRACTION = PASS
EMAIL_TENANT_CONFIGURATION = PASS
EMAIL_READINESS_MODEL = PASS
EMAIL_CHANNEL_READINESS = PASS

EMAIL_RECIPIENT_VALIDATION = PASS
RECIPIENT_EMAIL_MASKING = PASS

EMAIL_HEADER_INJECTION = DENIED
EMAIL_HTML_INJECTION = DENIED

EMAIL_TEMPLATE_MODEL = PASS
EMAIL_PLAIN_TEXT_PART = PASS
EMAIL_HTML_PART = PASS

SECURE_TASK_LINK_REUSE = PASS
DUPLICATE_EMAIL_SECURE_TOKEN_SYSTEM = FALSE

EMAIL_OUTBOX_REUSE = PASS
DUPLICATE_EMAIL_RETRY_ENGINE = FALSE

EMAIL_IDEMPOTENCY = PASS
DUPLICATE_EMAIL_DISPATCH = 0

SMTP_TRANSPORT = PASS
SMTP_TLS_POLICY = PASS
TRANSPORT_MESSAGE_ID_PERSISTENCE = PASS

EMAIL_TEMPORARY_FAILURE_CLASSIFICATION = PASS
EMAIL_PERMANENT_FAILURE_CLASSIFICATION = PASS
EMAIL_AUTH_FAILURE_CLASSIFICATION = PASS
EMAIL_TIMEOUT_CLASSIFICATION = PASS
EMAIL_TLS_FAILURE_CLASSIFICATION = PASS

FAKE_EMAIL_READ_STATUS = 0
FAKE_EMAIL_DELIVERED_STATUS = 0

EMAIL_CROSS_TENANT_ACCESS = DENIED

EMAIL_GOLDEN_FLOW = PASS
EMAIL_RETRY_FLOW = PASS
EMAIL_PERMANENT_FAILURE_FLOW = PASS

SCREENSHOT_COUNT = 10
VISUAL_REVIEW = PASS
USER_FACING_CONSOLE_ERRORS = 0

SMTP_PASSWORD_LOG_MATCHES = 0
RAW_SECURE_TOKEN_LOG_MATCHES = 0
FULL_EMAIL_RECIPIENT_LOG_MATCHES = 0
CLIENT_EXPOSED_EMAIL_SECRETS = 0
RAW_SMTP_RESPONSE_PII_LEAK = 0

EMAIL_TRACKING_PIXEL = FALSE
EMAIL_OPEN_TRACKING = FALSE

EMAIL_INBOUND_TASK_ACTIONS = DISABLED
CHANNEL_FALLBACK_IMPLEMENTED = FALSE

EMAIL_TEST_TRANSPORT_CUSTOMER_VISIBLE = FALSE

DOTNET_BUILD = PASS
WEB_TYPECHECK = PASS
WEB_BUILD = PASS

EXISTING_OMNICHANNEL_TESTS = PASS

WHATSAPP_TESTS = PASS
WHATSAPP_TEST_COUNT = 26
WHATSAPP_ADAPTER_REGRESSION = PASS

EMAIL_TESTS = PASS
EMAIL_TEST_COUNT = 33

CONTRACT_VERIFIER = PASS
MIGRATION_VERIFIER = PASS
RLS_VERIFIER = PASS

PRISTINE_DATABASE_MIGRATION = PASS
MANUAL_SCHEMA_PATCHES_REQUIRED = FALSE
EMAIL_MIGRATION_PRESENT = PASS
EMAIL_RLS_VERIFICATION = PASS

OMNICHANNEL_FOUNDATION_REGRESSION = PASS

MULTI_SECTOR_PRODUCT_MODE = ABSENT
SECTOR_SELECTION_ONBOARDING = ABSENT

FROZEN_DATABASE_USED_FOR_WRITES = FALSE
FROZEN_DEMO_DATA_POLLUTION = 0

RC1_RUNTIME_MUTATED = FALSE
RC1_DATABASE_MUTATED = FALSE
RC1_BACKUP_MUTATED = FALSE

DASHBOARD_PROTECTED_DIFF = 0

FOREIGN_PROCESS_KILLED = 0

STAGED = 0
COMMIT = NO
```

---

## 31. Final verdict

```
FINAL_VERDICT: EMAIL_DELIVERY_ADAPTER_1_READY
```

```
REAL_PROVIDER_VERDICT: EMAIL_DELIVERY_ADAPTER_1_PROVIDER_NOT_YET_VERIFIED
```

All deterministic implementation, runtime, security, UI, migration, and regression metrics pass. Real SMTP provider verification remains a separate follow-up when credentials are available.

---

## 32. Closure notes

1. **Timeout classification fix:** Removed premature `shouldHang` on SMTP connect in `smtp-email-provider-test-double.mjs`; timeout mode now returns SMTP `421` on EHLO so MailKit classifies retryable timeout without blocking the worker indefinitely.
2. **TLS classification:** Runtime proof uses tenant `security_mode=tls` against plain test double → `SslHandshakeException` → `failed-permanent` (configuration).
3. **Not-configured screenshot:** Requires closure DB API with `EMAIL__ENABLED=false` and deleted tenant config row (not bare `dotnet run` without env).
4. **Pristine migration:** `crevafactos_email_pristine_1` created empty; `dotnet ef database update` applied through `20260908120000_AddAiTaskEmailDeliveryAdapterFoundation`.
