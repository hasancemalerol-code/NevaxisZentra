# NevaxisFactOS Applications



Bu klasör, NevaxisFactOS platformunun çalıştırılabilir uygulamalarını içerir.



## Uygulamalar



### `web`



Next.js tabanlı kurumsal web uygulamasıdır.



Ana kullanım alanları:



- AI Görev Merkezi

- Yönetim dashboard'u

- Üretim planlama

- Makine ve kalıp yönetimi

- Sipariş taslakları, malzeme riskleri, kalite ve bakım ekranları

- NevaxisFactOS Teams ve authoritative AI Görev Merkezi etkileşimi

- Platform ve tenant yönetimi



### `api`



ASP.NET Core tabanlı ana backend uygulamasıdır.



Modüler monolit yapısında çalışır ve NevaxisFactOS iş kurallarının ana uygulama sınırını oluşturur.



### `worker`



Arka plan işlemlerini yürütür.



Başlıca sorumlulukları:



- RabbitMQ mesaj tüketimi

- Transactional outbox yayınlama

- Zamanlanmış görevler

- Entegrasyon işlemleri

- Uzun süren operasyonlar

- Retry ve dead-letter süreçleri



### `ai-orchestrator`



AI modelleri, araç sözleşmeleri ve AI Autopilot iş akışlarını yönetir.



AI, bu uygulama üzerinden kontrollü araçlara erişir. İş kuralları ve yetkilendirme API tarafında uygulanmaya devam eder.

AI doğrudan veritabanına, RabbitMQ'ya, ERP'ye, Edge Agent'a veya makinelere erişmez.



### `mobile`



Flutter tabanlı mobil uygulamadır.



Yönetici, operatör, depo, bakım, kalite ve sevkiyat ekipleri için görev odaklı mobil deneyimler sağlar.

### `teams`

NevaxisFactOS Teams; internal messaging, routing, task interaction, approval viewing ve shift communication uygulamasıdır.

Teams bağımsız task veya approval state tutmaz; Web ile aynı authoritative AI Görev Merkezi kayıtlarını kullanır.



### `edge-agent`



Fabrika içindeki enjeksiyon makineleri, PLC sistemleri, yardımcı ekipmanlar ve yerel cihazlarla haberleşen .NET uygulamasıdır.



OPC UA, Modbus, MQTT, seri port, dosya izleme ve üretici API adaptörlerini destekleyecek şekilde geliştirilecektir.



## Kurallar



- Her uygulamanın sorumluluğu açık olmalıdır.

- Uygulamalar birbirlerinin iç kodlarına doğrudan bağımlı olmamalıdır.

- Paylaşılması gereken sözleşmeler `packages` altında tutulmalıdır.

- Domain iş kuralları frontend veya AI uygulamasına kopyalanmamalıdır.

- Tenant, yetki, audit ve correlation kuralları bütün uygulamalarda korunmalıdır.

- Web, mobil ve NevaxisFactOS Teams aynı authoritative AI Görev Merkezi task ve approval kayıtlarını kullanmalıdır.

- ERP erişimi read-only-first; write-back permission-bound, approval-bound, revalidated, idempotent ve audited olmalıdır.

- Uygulamalar finance, accounting, official stock ledger, payroll veya general purchasing ERP system-of-record modülleri oluşturamaz.

- Production secret'ları hiçbir uygulama klasöründe kaynak kontrolüne alınmamalıdır.


