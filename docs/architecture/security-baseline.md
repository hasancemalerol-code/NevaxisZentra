# NevaxisZentra Güvenlik Temel Standardı



## Amaç



Bu belge NevaxisZentra platformu için bütün uygulama, modül, entegrasyon ve geliştirme paketlerinde uygulanması gereken minimum güvenlik gereksinimlerini tanımlar.



Bu standart aşağıdaki bileşenleri kapsar:



- Web

- API

- Worker

- AI Orchestrator

- Mobil uygulama

- Edge Agent

- PostgreSQL

- Redis

- RabbitMQ

- Object storage

- Harici entegrasyonlar

- Omnichannel sipariş kanalları

- Makine ve PLC bağlantıları

- AI tool erişimleri

- CI ve deployment süreçleri



Bu belgede tanımlanan kurallar başlangıç güvenlik tabanıdır.



Bir modül veya müşteri daha güçlü güvenlik gereksinimine sahip olabilir ancak bu tabanın altına inemez.



## Temel Güvenlik İlkeleri



NevaxisZentra aşağıdaki ilkeleri uygular:



```text

deny by default

least privilege

defense in depth

explicit authorization

tenant isolation

secure by default

fail securely

data minimization

complete mediation

auditability

separation of duties

human approval for critical actions

```

Non-negotiable controls include tenant isolation, permission checks at the trusted backend, authoritative AI Task Center approval, separation of duties, delegation expiry, stale-data revalidation, idempotent external execution, immutable audit evidence, secret isolation, and deny-by-default AI tools.

ERP integrations are read-only-first. Edge Agent uses allowlisted device contracts and cannot bypass local safety. Security failures fail closed and receive targeted negative tests.

