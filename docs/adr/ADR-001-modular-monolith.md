\# ADR-001: Modüler Monolit Mimarisi



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Backend uygulama mimarisi



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, üretimden bakıma, kalıptan makineye, stoktan finansa kadar geniş bir operasyon alanını yöneten Factory Operating System platformudur.



Ürün aşağıdaki özelliklere sahip olacaktır:



\- Çoklu tenant ve şirket desteği

\- Plastik enjeksiyon üretim yönetimi

\- Kalıp yaşam döngüsü yönetimi

\- Makine, yardımcı ekipman ve Edge bağlantıları

\- Stok, satın alma, kalite, bakım ve maliyet yönetimi

\- Olay tabanlı süreçler

\- Bağımsız Worker uygulaması

\- AI Autopilot ve insan onaylı işlem uygulama modeli

\- Harici ERP, muhasebe, e-posta, WhatsApp, PLC ve IoT entegrasyonları



İlk aşamada doğrudan mikroservis mimarisi kullanmak; dağıtım, gözlemleme, veri tutarlılığı, hata ayıklama ve geliştirme maliyetini gereksiz şekilde artıracaktır.



Bununla birlikte, bütün sistemin katmansız ve sınırları belirsiz tek bir uygulama olarak geliştirilmesi de uzun vadede sürdürülemez olacaktır.



\## Karar



CrevaFactOS backend sistemi modüler monolit mimarisiyle geliştirilecektir.



Her iş alanı bağımsız bir modül sınırına sahip olacaktır. Modüller kendi domain, application, infrastructure ve API bileşenlerini açık biçimde ayıracaktır.



Başlangıçta tek bir ASP.NET Core API uygulaması ve ortak PostgreSQL altyapısı kullanılacaktır. Worker, AI Orchestrator ve Edge Agent ayrı uygulamalar olarak çalışacaktır.



Modüller birbirlerinin iç tablolarına veya uygulama servislerine doğrudan erişemeyecektir.



Modüller arası iletişim aşağıdaki yöntemlerle sağlanacaktır:



\- Açık uygulama sözleşmeleri

\- Domain ve integration event’leri

\- Transactional outbox

\- RabbitMQ

\- Sürümlenmiş mesaj şemaları



Her modül ileride bağımsız servise ayrılabilecek sınırlar içinde tasarlanacaktır.



\## Başlangıç Modül Sınırları



İlk platform modülleri:



\- Platform

\- Organizations

\- Identity

\- Audit



İlerleyen aşamalarda eklenecek ana domain modülleri:



\- Orders

\- Customers

\- Products

\- Molds

\- Machines

\- Production

\- Planning

\- Materials

\- Inventory

\- Purchasing

\- Quality

\- Maintenance

\- Logistics

\- Finance

\- Workforce

\- Documents

\- Integrations

\- AI Operations



Bu liste geliştirme aşamalarına göre genişletilebilir; ancak modül sınırları açık mimari kararlarla değiştirilmelidir.



\## Alternatifler



\### Dağıtık mikroservis mimarisi



Değerlendirildi ancak başlangıç için reddedildi.



Nedenleri:



\- Yüksek operasyon ve DevOps maliyeti

\- Dağıtık işlem karmaşıklığı

\- Daha zor hata ayıklama

\- Çok sayıda servis ve deployment yönetimi

\- İlk ürün aşamasında gereksiz ölçekleme yükü



\### Katmansız monolit



Değerlendirildi ancak reddedildi.



Nedenleri:



\- Domain sınırlarının zamanla kaybolması

\- Modüller arası sıkı bağımlılık

\- Doğrudan tablo erişiminin yaygınlaşması

\- Test ve bakım maliyetinin artması

\- İleride servis ayrıştırmasının zorlaşması



\### Baştan bağımsız servisler ve ayrı veritabanları



Değerlendirildi ancak erken aşama için reddedildi.



Nedenleri:



\- Veri tutarlılığı ve raporlama karmaşıklığı

\- Geliştirme hızının düşmesi

\- Ürün-domain sınırlarının henüz doğrulanmamış olması

\- Yüksek altyapı ve izleme maliyeti



\## Gerekçe



Modüler monolit yaklaşımı:



\- İlk ürün geliştirme hızını korur.

\- Domain sınırlarını açık tutar.

\- Tek deployment ile operasyonu sadeleştirir.

\- Transactional veri tutarlılığını kolaylaştırır.

\- Plastik enjeksiyon ve kalıp iş akışlarının birlikte çalışmasını destekler.

\- Gerektiğinde seçili modüllerin bağımsız servise ayrılmasına olanak verir.

\- AI Autopilot, Worker ve Edge süreçlerinin kontrollü biçimde ayrıştırılmasını sağlar.



\## Olumlu Sonuçlar



\- Daha sade geliştirme ve dağıtım süreci

\- Modül bazlı sahiplik ve test edilebilirlik

\- Ortak transaction kullanımının mümkün olması

\- Daha düşük başlangıç altyapı maliyeti

\- Domain kurallarının belirgin sınırlar içinde tutulması

\- Kademeli ölçekleme imkânı

\- Daha kolay yerel geliştirme ortamı



\## Riskler



\- Modüller arasında zamanla doğrudan bağımlılık oluşması

\- Ortak veritabanının yanlış kullanılması

\- Modül sınırlarının yalnızca klasör seviyesinde kalması

\- Büyük deployment paketinin zamanla yavaşlaması

\- Bağımsız ölçekleme gerektiren alanların geç ayrıştırılması



Bu riskler aşağıdaki kurallarla azaltılacaktır:



\- Modüller arası doğrudan tablo erişimi yasaktır.

\- Her modül kendi şema ve veri erişim sınırına sahip olmalıdır.

\- Integration event sözleşmeleri sürümlenmelidir.

\- Mimari bağımlılık testleri uygulanmalıdır.

\- Yeni çapraz modül bağımlılıkları ADR ile değerlendirilmelidir.



\## Geri Dönüş ve Ayrıştırma Şartları



Bir modül aşağıdaki koşullardan biri oluştuğunda bağımsız servise ayrılmak üzere değerlendirilir:



\- Diğer modüllerden bağımsız ölçekleme ihtiyacı

\- Farklı erişilebilirlik veya performans gereksinimi

\- Ayrı deployment döngüsü ihtiyacı

\- Yüksek mesaj veya işlem hacmi

\- Güvenlik ve veri izolasyonu gereksinimi

\- Bağımsız ekip sahipliği

\- Teknoloji veya altyapı ayrışması ihtiyacı



Ayrıştırma kararı yeni bir ADR ile belgelenmelidir.

