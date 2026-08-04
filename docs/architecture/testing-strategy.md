\# CrevaFactOS Test Stratejisi



\## Amaç



Bu belge CrevaFactOS platformunda uygulanacak test yaklaşımını, test katmanlarını, kapsam kurallarını, ortamları ve kalite kapılarını tanımlar.



Amaçlar:



\- Domain iş kurallarını güvence altına almak

\- Tenant izolasyonunu doğrulamak

\- Kritik onay ve yetkilendirme akışlarını korumak

\- API, Worker, AI Orchestrator, mobil ve Edge Agent davranışlarını doğrulamak

\- Mesajlaşma ve transactional outbox güvenilirliğini test etmek

\- Plastik enjeksiyon ve kalıp sektörüne özel iş akışlarını doğrulamak

\- Regresyon riskini azaltmak

\- Hataları production öncesinde yakalamak

\- Küçük ve geri alınabilir geliştirme paketlerini desteklemek



\## Temel İlke



Testler yalnızca kod coverage üretmek için yazılmamalıdır.



Her test:



\- Gerçek bir davranışı doğrulamalıdır.

\- Açık bir risk veya iş kuralıyla ilişkili olmalıdır.

\- Deterministik olmalıdır.

\- Tekrarlanabilir olmalıdır.

\- Gereksiz dış bağımlılık taşımamalıdır.

\- Başarısız olduğunda nedenini açıkça göstermelidir.



\## Test Piramidi



CrevaFactOS aşağıdaki test dağılımını hedefler:



```text

&#x20;            End-to-End

&#x20;         Contract / System

&#x20;       Integration / Security

&#x20;     Application / Domain Tests

&#x20;            Unit Tests

