\# ADR-010: AI Tool Access Boundary



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: AI Autopilot araç erişimi, yetkilendirme ve insan onayı sınırları



\## Bağlam



CrevaFactOS, plastik enjeksiyon ve kalıp üreticileri için geliştirilen AI destekli bir Factory Operating System platformudur.



AI Autopilot aşağıdaki alanlarda aktif rol alacaktır:



\- Sipariş mesajlarını analiz etmek

\- WhatsApp ve e-posta taleplerinden sipariş formu hazırlamak

\- Üretim planı oluşturmak

\- MRP ve malzeme ihtiyacı hesaplamak

\- Satın alma siparişi hazırlamak

\- Makine ve kalıp bakım planı oluşturmak

\- Kalite risklerini analiz etmek

\- Sevkiyat planı hazırlamak

\- Finansal risk ve nakit akışı tahmini yapmak

\- Maliyet optimizasyonu önermek

\- Makine telemetrisini analiz etmek

\- Risk, anomali ve gecikmeleri tespit etmek

\- Yetkili onayından sonra işlemleri sistem içinde uygulamak



AI yalnızca metin üreten bir sohbet asistanı olmayacaktır.



AI, CrevaFactOS içindeki kontrollü araçları kullanarak taslak, öneri ve onaylanmış işlemler oluşturabilecektir.



Bu yetenek yüksek operasyonel değer sağlarken aşağıdaki riskleri doğurur:



\- Yetkisiz işlem

\- Yanlış tenant verisine erişim

\- Onay kapsamı dışında işlem yapma

\- Hatalı veriyle karar oluşturma

\- Prompt injection

\- Araç çağrısının manipüle edilmesi

\- Hassas verinin modele gönderilmesi

\- AI’ın kendi adına onay üretmesi

\- Kritik makine komutlarının kontrolsüz uygulanması

\- Kullanıcının görmediği ek işlemlerin gerçekleştirilmesi

\- Zincir hâlindeki işlemlerde kapsamın genişlemesi



\## Karar



CrevaFactOS AI sistemi açık bir Tool Access Boundary kullanacaktır.



AI hiçbir veritabanına, mesaj kuyruğuna, dosya alanına, harici entegrasyona veya makineye doğrudan erişmeyecektir.



Bütün AI işlemleri yalnızca CrevaFactOS tarafından tanımlanmış, sürümlenmiş, doğrulanan ve yetkilendirilen araç sözleşmeleri üzerinden gerçekleştirilecektir.



Standart akış:



1\. Kullanıcı veya sistem bir AI görevi oluşturur.

2\. AI gerekli verileri yalnızca yetkili read tool’lar üzerinden alır.

3\. AI analiz ve işlem önerisi oluşturur.

4\. Sistem öneriyi doğrular.

5\. Güven seviyesi, gerekçe, risk, mali etki ve kaynaklar kullanıcıya gösterilir.

6\. Gerekli işlemler yetkili onayına sunulur.

7\. Yetkili kullanıcı kapsamı onaylar veya reddeder.

8\. Sistem onayın geçerliliğini ve kapsamını tekrar doğrular.

9\. AI veya Worker yalnızca onaylanmış write tool’ları çağırır.

10\. Sonuç doğrulanır.

11\. Audit ve AI Karar Defteri kaydı oluşturulur.

12\. Kullanıcı ve ilgili taraflar bilgilendirilir.



\## AI Yetki İlkesi



AI bağımsız bir kullanıcı veya yönetici değildir.



AI aşağıdaki yetkilere sahip olmayacaktır:



\- Kendi rolünü belirlemek

\- Kendi yetkisini yükseltmek

\- Kullanıcı adına onay vermek

\- Onay kaydı üretmek

\- Tenant seçmek veya değiştirmek

\- Yetki sınırını aşmak

\- Gizli sistem araçlarını keşfetmek

\- Doğrudan SQL çalıştırmak

\- Doğrudan RabbitMQ mesajı yayınlamak

\- Doğrudan object storage dosyasına erişmek

\- Doğrudan makine veya PLC komutu göndermek

\- Güvenlik politikalarını değiştirmek

\- Audit kayıtlarını silmek veya değiştirmek



AI çağrıları, işlemi başlatan kullanıcı ve tenant context kapsamında yürütülecektir.



\## Tool Sınıfları



AI araçları risk seviyelerine göre sınıflandırılacaktır.



\### Read Tools



Yalnızca veri okur.



Örnekler:



```text

get\_order

list\_open\_orders

get\_machine\_status

get\_mold\_lifecycle

get\_material\_stock

get\_supplier\_quotes

get\_maintenance\_history

get\_quality\_summary

get\_cashflow\_forecast

search\_documents

