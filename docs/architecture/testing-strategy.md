# CrevaFactOS Test Stratejisi



## Amaç



Bu belge CrevaFactOS platformunda uygulanacak test yaklaşımını, test katmanlarını, kapsam kurallarını, ortamları ve kalite kapılarını tanımlar.



Amaçlar:



- Domain iş kurallarını güvence altına almak

- Tenant izolasyonunu doğrulamak

- Kritik onay ve yetkilendirme akışlarını korumak

- API, Worker, AI Orchestrator, mobil ve Edge Agent davranışlarını doğrulamak

- Mesajlaşma ve transactional outbox güvenilirliğini test etmek

- Plastik enjeksiyon ve kalıp sektörüne özel iş akışlarını doğrulamak

- Regresyon riskini azaltmak

- Hataları production öncesinde yakalamak

- Küçük ve geri alınabilir geliştirme paketlerini desteklemek



## Temel İlke



Testler yalnızca kod coverage üretmek için yazılmamalıdır.



Her test:



- Gerçek bir davranışı doğrulamalıdır.

- Açık bir risk veya iş kuralıyla ilişkili olmalıdır.

- Deterministik olmalıdır.

- Tekrarlanabilir olmalıdır.

- Gereksiz dış bağımlılık taşımamalıdır.

- Başarısız olduğunda nedenini açıkça göstermelidir.



## Test Piramidi



CrevaFactOS aşağıdaki test dağılımını hedefler:



```text

             End-to-End

          Contract / System

        Integration / Security

      Application / Domain Tests

             Unit Tests

```

Targeted package validation covers the changed behavior and its principal risks. Mandatory cross-cutting suites include tenant isolation, permission denial, approval state/expiry/delegation, stale-data revalidation, outbox and consumer idempotency, ERP write-back reconciliation, audit linkage, AI tool boundaries, and Edge safety rejection.

Tests use synthetic or anonymized data. Documentation-only packages run documentation checks rather than application builds when no application exists.

