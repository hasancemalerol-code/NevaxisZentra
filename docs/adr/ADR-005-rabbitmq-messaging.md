\# ADR-005: RabbitMQ Mesajlaşma Altyapısı



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Olay tabanlı entegrasyon ve arka plan mesajlaşma altyapısı



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, birbiriyle bağlantılı çok sayıda operasyonel modül içeren Factory Operating System platformudur.



Sistem aşağıdaki süreçlerde senkron olmayan mesajlaşmaya ihtiyaç duyacaktır:



\- Sipariş onaylandıktan sonra üretim planının hazırlanması

\- Üretim emri oluşturulduğunda MRP hesaplanması

\- Kritik stok oluştuğunda satın alma önerisi hazırlanması

\- Makine alarmı oluştuğunda bakım sürecinin başlatılması

\- Kalıp bakım süresi yaklaştığında görev oluşturulması

\- Kalite uygunsuzluğu oluştuğunda ilgili süreçlerin bilgilendirilmesi

\- Sevkiyat planı kesinleştiğinde müşteri bildirimi hazırlanması

\- AI Autopilot işlemlerinin yetkili onayı sonrasında uygulanması

\- E-posta, WhatsApp ve diğer dış sistem bildirimlerinin gönderilmesi

\- Audit ve entegrasyon kayıtlarının işlenmesi

\- Uzun süren rapor, dosya ve entegrasyon işlemleri

\- Edge Agent üzerinden gelen makine ve sensör olaylarının işlenmesi



Bu işlemlerin tamamının doğrudan HTTP isteği içinde yürütülmesi gecikme, hata zinciri, transaction süresi ve sistem dayanıklılığı açısından uygun değildir.



\## Karar



CrevaFactOS’un ana mesajlaşma altyapısı RabbitMQ olacaktır.



RabbitMQ aşağıdaki amaçlarla kullanılacaktır:



\- Integration event yayınlama

\- Worker görevleri

\- Modüller arası asenkron iletişim

\- Harici entegrasyon işlemleri

\- Bildirim işleri

\- Uzun süren arka plan görevleri

\- Retry ve dead-letter süreçleri

\- Edge ve bulut arasında güvenli olay aktarımı



RabbitMQ, domain içi transaction yönetiminin yerine geçmeyecektir.



Domain işlemleri öncelikle kendi transaction sınırları içinde tamamlanacak, dışarıya aktarılacak olaylar transactional outbox üzerinden yayınlanacaktır.



\## Mesaj Zarfı



Bütün integration event ve arka plan mesajları ortak bir mesaj zarfı kullanacaktır.



Zorunlu alanlar:



```text

messageId

messageType

occurredAt

tenantId

correlationId

causationId

schemaVersion

payload

