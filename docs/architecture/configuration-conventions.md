\# CrevaFactOS Yapılandırma Standartları



\## Amaç



Bu belge CrevaFactOS uygulamalarında kullanılan configuration kaynaklarını, isimlendirme kurallarını, environment ayrımını, secret yönetimini ve doğrulama ilkelerini tanımlar.



Amaçlar:



\- Yapılandırmayı koddan ayırmak

\- Ortamlar arasında güvenli geçiş sağlamak

\- Secret değerlerinin repository içine girmesini engellemek

\- Uygulama başlangıcında hatalı ayarları erken tespit etmek

\- Web, API, Worker, AI Orchestrator, mobil ve Edge Agent arasında tutarlı yapılandırma yaklaşımı oluşturmak

\- Production dışı ayarların production ortamına sızmasını önlemek

\- Yapılandırma değişikliklerini izlenebilir hâle getirmek



\## Temel İlke



Yapılandırma, iş kuralı değildir.



Yapılandırma aşağıdaki amaçlarla kullanılabilir:



\- Servis adresleri

\- Portlar

\- Timeout değerleri

\- Feature flag'ler

\- Retry limitleri

\- Sağlayıcı seçimi

\- Log seviyeleri

\- Ortam bilgisi

\- Bağlantı ayarları

\- Maliyet ve kullanım limitleri

\- Teknik kapasite sınırları



Aşağıdakiler yalnızca configuration üzerinden belirlenmemelidir:



\- Yetki kararı

\- Tenant erişim kararı

\- Onay zorunluluğu

\- Finansal iş kuralları

\- Kalite kabul kriterleri

\- Makine güvenlik kuralları

\- Domain durum geçişleri



Bu kurallar domain veya application katmanında uygulanmalıdır.



\## Yapılandırma Kaynakları



Önerilen yapılandırma önceliği:



```text

Default configuration

&#x20; → Environment-specific configuration

&#x20; → Environment variables

&#x20; → Secret provider

&#x20; → Runtime override

