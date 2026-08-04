# CrevaFactOS Scripts



Bu klasör, CrevaFactOS geliştirme, doğrulama, bakım ve operasyon süreçlerinde kullanılan kontrollü yardımcı scriptleri içerir.



## Kullanım Alanları



Scriptler aşağıdaki işlemler için kullanılabilir:



- Yerel geliştirme ortamını hazırlama

- Repository doğrulama

- Bağımlılık kontrolü

- Kod kalite kontrolleri

- Veritabanı migration işlemleri

- Development seed işlemleri

- Docker servislerini yönetme

- Yedekleme ve geri yükleme

- Test verisi oluşturma

- Mesajlaşma altyapısını doğrulama

- Edge Agent bağlantı testleri

- CI yardımcı işlemleri



## Başlangıç Script Kategorileri



İlerleyen paketlerde aşağıdaki scriptler eklenebilir:



```text

scripts/

├── setup/

├── development/

├── database/

├── docker/

├── verification/

├── maintenance/

└── ci/

```

Scripts must be non-interactive by default where used in CI, validate destructive targets, avoid production secrets and customer data, and return a non-zero exit code on failure.

Repository scripts cannot bypass migrations, permissions, authoritative approvals, audit, connector idempotency, or Edge safety controls.

## Local Infrastructure Verification

`scripts/infrastructure/verify-local-infrastructure.ps1` checks only Docker availability, PostgreSQL and RabbitMQ container health, and RabbitMQ management API reachability. It does not create schemas, queues, users, vhosts, policies, or application connections.

## Database Lifecycle

`scripts/database/database.ps1` provides the explicit FOUND-03 configuration, connectivity, migration, SQL generation, rollback, and state commands exposed through root `pnpm db:*` scripts. It never starts Docker, runs from application startup, prints secrets, or deletes the PostgreSQL volume.

FOUND-04 adds `tenant-isolation-verify`. It uses separate migration and runtime credentials, creates temporary deterministic isolation fixtures outside migrations, verifies real RLS and pooled-connection behavior, and removes fixture rows. It does not create product data or expose a privileged application bypass.

FOUND-05 adds `authentication-membership-verify`. It performs explicit development-only fixture bootstrap, verifies active external identity mapping and tenant membership through the restricted runtime role, confirms pre-tenant tables cannot be enumerated, and removes fixtures in a `finally` boundary. It stores no password and creates no default account.

`scripts/authentication/verify-local-authentication.ps1` performs the matching end-to-end Development API check. It starts a hidden temporary API process, verifies authenticated membership selection and normalized failures, and always attempts process shutdown and fixture cleanup.

FOUND-06 adds `permission-authorization-verify` and `scripts/authorization/verify-local-authorization.ps1`. They create only temporary local tenant roles and assignments, verify exact permission grants, inactive and unassigned denials, database-enforced cross-tenant rejection, RLS and pooled/request isolation, then remove the fixtures. They create no default administrator, production role, product permission, or management endpoint.

FOUND-07 adds `audit-trail-verify` and `scripts/audit/verify-local-audit.ps1`. They create temporary tenant A/B actor and audit fixtures, prove append-only runtime grants, mutation and cross-tenant rejection, audit RLS, correlation propagation, and exact diagnostic permission behavior, then use the controlled migration boundary to remove fixtures. They do not create retention, export, event-bus, outbox, product-event, or AI Decision Ledger behavior.
