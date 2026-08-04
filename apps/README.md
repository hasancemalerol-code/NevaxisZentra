\# CrevaFactOS Applications



Bu klasör, CrevaFactOS platformunun çalıştırılabilir uygulamalarını içerir.



\## Uygulamalar



\### `web`



Next.js tabanlı kurumsal web uygulamasıdır.



Ana kullanım alanları:



\- AI Görev Merkezi

\- Yönetim dashboard'u

\- Üretim planlama

\- Makine ve kalıp yönetimi

\- Sipariş, stok, kalite ve bakım ekranları

\- Platform ve tenant yönetimi



\### `api`



ASP.NET Core tabanlı ana backend uygulamasıdır.



Modüler monolit yapısında çalışır ve CrevaFactOS iş kurallarının ana uygulama sınırını oluşturur.



\### `worker`



Arka plan işlemlerini yürütür.



Başlıca sorumlulukları:



\- RabbitMQ mesaj tüketimi

\- Transactional outbox yayınlama

\- Zamanlanmış görevler

\- Entegrasyon işlemleri

\- Uzun süren operasyonlar

\- Retry ve dead-letter süreçleri



\### `ai-orchestrator`



AI modelleri, araç sözleşmeleri ve AI Autopilot iş akışlarını yönetir.



AI, bu uygulama üzerinden kontrollü araçlara erişir. İş kuralları ve yetkilendirme API tarafında uygulanmaya devam eder.



\### `mobile`



Flutter tabanlı mobil uygulamadır.



Yönetici, operatör, depo, bakım, kalite ve sevkiyat ekipleri için görev odaklı mobil deneyimler sağlar.



\### `edge-agent`



Fabrika içindeki enjeksiyon makineleri, PLC sistemleri, yardımcı ekipmanlar ve yerel cihazlarla haberleşen .NET uygulamasıdır.



OPC UA, Modbus, MQTT, seri port, dosya izleme ve üretici API adaptörlerini destekleyecek şekilde geliştirilecektir.



\## Kurallar



\- Her uygulamanın sorumluluğu açık olmalıdır.

\- Uygulamalar birbirlerinin iç kodlarına doğrudan bağımlı olmamalıdır.

\- Paylaşılması gereken sözleşmeler `packages` altında tutulmalıdır.

\- Domain iş kuralları frontend veya AI uygulamasına kopyalanmamalıdır.

\- Tenant, yetki, audit ve correlation kuralları bütün uygulamalarda korunmalıdır.

\- Production secret'ları hiçbir uygulama klasöründe kaynak kontrolüne alınmamalıdır.



