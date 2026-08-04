\# ADR-006: Transactional Outbox



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Veritabanı işlemleri ile mesaj yayınlama arasındaki tutarlılık



\## Bağlam



CrevaFactOS; sipariş, üretim, stok, satın alma, kalite, bakım, sevkiyat, finans, AI Autopilot ve makine entegrasyonları arasında olay tabanlı iş akışları çalıştıracaktır.



Örnek süreçler:



\- Sipariş onaylandığında üretim planlama başlatılması

\- Üretim emri oluşturulduğunda MRP hesaplanması

\- Kritik stok oluştuğunda satın alma önerisi hazırlanması

\- Makine alarmı oluştuğunda bakım görevi oluşturulması

\- Kalıp bakım zamanı geldiğinde bakım planının hazırlanması

\- Kalite uygunsuzluğu oluştuğunda ilgili lotların karantinaya alınması

\- AI görevi onaylandığında işlemin Worker tarafından uygulanması

\- Sevkiyat kesinleştiğinde müşteri bildiriminin gönderilmesi

\- Muhasebe veya ERP entegrasyonuna kayıt aktarılması



Bir domain işlemi veritabanında başarılı olduğu hâlde ilgili mesaj RabbitMQ’ya gönderilemezse sistem tutarsız duruma düşebilir.



Tersi durumda, veritabanı işlemi başarısız olduğu hâlde mesaj yayınlanırsa gerçekte oluşmamış bir işlem diğer modüller tarafından uygulanabilir.



Veritabanı ile RabbitMQ arasında dağıtık transaction kullanılmayacaktır.



\## Karar



CrevaFactOS, veritabanı işlemleri ile integration event yayınlama arasındaki tutarlılığı sağlamak için Transactional Outbox desenini kullanacaktır.



Domain işlemi ve outbox kaydı aynı PostgreSQL transaction’ı içinde oluşturulacaktır.



RabbitMQ’ya mesaj gönderimi ayrı bir Worker süreci tarafından gerçekleştirilecektir.



Standart akış:



1\. Uygulama domain işlemini başlatır.

2\. Domain değişiklikleri hazırlanır.

3\. Yayınlanacak integration event’ler oluşturulur.

4\. Domain verileri ve outbox kayıtları aynı transaction içinde kaydedilir.

5\. Transaction başarıyla tamamlanır.

6\. Outbox Worker yayınlanmamış kayıtları okur.

7\. Mesajları RabbitMQ’ya gönderir.

8\. Başarılı yayın sonrası kayıt işlendi olarak işaretlenir.

9\. Başarısız yayınlarda retry uygulanır.

10\. Sürekli başarısız kayıtlar operasyonel incelemeye alınır.



API veya domain servisi, kritik domain transaction içinde doğrudan RabbitMQ’ya mesaj yayınlamayacaktır.



\## Outbox Kayıt Yapısı



Başlangıç outbox kaydı aşağıdaki alanları içermelidir:



```text

id

tenant\_id

message\_id

message\_type

schema\_version

occurred\_at

correlation\_id

causation\_id

payload

headers

created\_at

published\_at

attempt\_count

next\_attempt\_at

last\_error

status

