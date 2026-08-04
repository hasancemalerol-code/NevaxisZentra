\# CrevaFactOS Security Documentation



Bu klasör, CrevaFactOS güvenlik mimarisini, politikalarını, tehdit sınırlarını ve doğrulama gereksinimlerini içerir.



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, çoklu tenant destekli ve AI Autopilot özellikleri bulunan kurumsal bir Factory Operating System platformudur.



Sistem aşağıdaki kritik alanları yönettiği için güvenlik ürünün temel mimari bileşenidir:



\- Müşteri ve sipariş verileri

\- Üretim planları ve üretim emirleri

\- Kalıp ve makine kayıtları

\- Proses parametreleri

\- Hammadde ve stok hareketleri

\- Kalite ve izlenebilirlik kayıtları

\- Bakım ve arıza bilgileri

\- Finans ve maliyet verileri

\- Personel ve yetki kayıtları

\- AI tarafından hazırlanan işlemler

\- Yetkili onayları

\- Makine, PLC ve Edge Agent bağlantıları

\- WhatsApp ve e-posta sipariş içerikleri



\## Güvenlik İlkeleri



CrevaFactOS aşağıdaki temel güvenlik ilkelerini uygular:



\- Minimum yetki

\- Varsayılan olarak reddet

\- Katmanlı güvenlik

\- Tenant izolasyonu

\- Açık yetkilendirme

\- İnsan onaylı kritik işlemler

\- Değiştirilemez audit geçmişi

\- Secret minimizasyonu

\- Güvenli varsayılanlar

\- Sürümlenmiş sözleşmeler

\- Veri minimizasyonu

\- Güvenli hata yönetimi

\- Gözlemlenebilirlik

\- Geri alınabilir veya telafi edilebilir işlemler



\## Güvenlik Alanları



\### Tenant İzolasyonu



Her müşteri verisi ayrı tenant güvenlik sınırında tutulur.



Tenant izolasyonu aşağıdaki katmanlarda uygulanır:



\- Kimlik doğrulama

\- Üyelik

\- Tenant context

\- API yetkilendirmesi

\- Veri erişim katmanı

\- PostgreSQL Row-Level Security

\- Mesajlaşma

\- Redis cache

\- Object storage

\- AI ve RAG

\- Edge Agent

\- Audit



Bir tenant başka tenant verisini okuyamaz, değiştiremez veya dolaylı biçimde çıkaramaz.



\### Kimlik Doğrulama



Kimlik doğrulama sistemi aşağıdaki özellikleri desteklemelidir:



\- Güvenli oturum açma

\- Çok faktörlü kimlik doğrulama hazırlığı

\- Access token

\- Refresh token

\- Oturum süresi

\- Oturum iptali

\- Cihaz oturumu

\- Şüpheli giriş tespiti

\- Parola politikası

\- Hesap kilitleme

\- Audit kaydı



Kimlik doğrulama ile yetkilendirme ayrı sorumluluklar olarak ele alınmalıdır.



\### Yetkilendirme



Yetkilendirme permission bazlı olacaktır.



Roller yalnızca permission paketleri olarak kullanılabilir.



Örnek permission alanları:



```text

orders.read

orders.create

orders.approve

production.read

production.release

machines.read

machines.command

molds.maintain

inventory.transfer

purchasing.approve

quality.quarantine

finance.approve

ai\_tasks.approve

audit.read

