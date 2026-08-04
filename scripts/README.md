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
