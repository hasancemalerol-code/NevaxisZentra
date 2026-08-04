\# CrevaFactOS Güvenlik Temel Standardı



\## Amaç



Bu belge CrevaFactOS platformu için bütün uygulama, modül, entegrasyon ve geliştirme paketlerinde uygulanması gereken minimum güvenlik gereksinimlerini tanımlar.



Bu standart aşağıdaki bileşenleri kapsar:



\- Web

\- API

\- Worker

\- AI Orchestrator

\- Mobil uygulama

\- Edge Agent

\- PostgreSQL

\- Redis

\- RabbitMQ

\- Object storage

\- Harici entegrasyonlar

\- Omnichannel sipariş kanalları

\- Makine ve PLC bağlantıları

\- AI tool erişimleri

\- CI ve deployment süreçleri



Bu belgede tanımlanan kurallar başlangıç güvenlik tabanıdır.



Bir modül veya müşteri daha güçlü güvenlik gereksinimine sahip olabilir ancak bu tabanın altına inemez.



\## Temel Güvenlik İlkeleri



CrevaFactOS aşağıdaki ilkeleri uygular:



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

