\# ADR-004: PostgreSQL Ana Veritabanı



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Ana operasyonel veritabanı ve veri bütünlüğü



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen çoklu tenant destekli bir Factory Operating System platformudur.



Sistem aşağıdaki kritik verileri yönetecektir:



\- Tenant, şirket, fabrika ve tesis kayıtları

\- Kullanıcı, rol, yetki ve onay kayıtları

\- Müşteri, teklif ve siparişler

\- Parça, ürün, reçete ve BOM

\- Kalıp, kavite ve kalıp yaşam döngüsü

\- Enjeksiyon makineleri ve yardımcı ekipmanlar

\- Üretim planları ve üretim emirleri

\- Hammadde, masterbatch, katkı ve regranül stokları

\- Lot ve seri izlenebilirliği

\- Satın alma ve tedarikçi kayıtları

\- Kalite ve uygunsuzluk kayıtları

\- Bakım ve arıza kayıtları

\- Sevkiyat ve lojistik işlemleri

\- Finans, maliyet ve kârlılık kayıtları

\- AI tarafından hazırlanan karar ve işlem taslakları

\- Yetkili onayları ve audit geçmişi



Bu veriler yüksek bütünlük, ilişkisel sorgulama, transaction desteği, güçlü constraint yapısı, concurrency kontrolü ve güvenli tenant izolasyonu gerektirir.



\## Karar



CrevaFactOS’un ana operasyonel veritabanı PostgreSQL olacaktır.



PostgreSQL aşağıdaki uygulamalar tarafından kullanılacaktır:



\- ASP.NET Core API

\- Worker

\- AI Orchestrator

\- Edge senkronizasyon süreçleri

\- Raporlama ve entegrasyon işlemleri



Veri erişiminin ana yöntemi Entity Framework Core olacaktır.



Performans açısından kritik ve ölçülmüş sorgularda kontrollü biçimde doğrudan SQL veya uygun sorgu araçları kullanılabilir.



\## Ana Veri İlkeleri



\### Multi-tenant ayrımı



Bütün iş verileri tenant kapsamına bağlı olacaktır.



Tenant kapsamlı tablolarda uygun olduğu durumlarda aşağıdaki alan bulunacaktır:



```text

tenant\_id

