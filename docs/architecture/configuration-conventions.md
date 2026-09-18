# NevaxisZentra Yapılandırma Standartları



## Amaç



Bu belge NevaxisZentra uygulamalarında kullanılan configuration kaynaklarını, isimlendirme kurallarını, environment ayrımını, secret yönetimini ve doğrulama ilkelerini tanımlar.



Amaçlar:



- Yapılandırmayı koddan ayırmak

- Ortamlar arasında güvenli geçiş sağlamak

- Secret değerlerinin repository içine girmesini engellemek

- Uygulama başlangıcında hatalı ayarları erken tespit etmek

- Web, API, Worker, AI Orchestrator, mobil ve Edge Agent arasında tutarlı yapılandırma yaklaşımı oluşturmak

- Production dışı ayarların production ortamına sızmasını önlemek

- Yapılandırma değişikliklerini izlenebilir hâle getirmek



## Temel İlke



Yapılandırma, iş kuralı değildir.



Yapılandırma aşağıdaki amaçlarla kullanılabilir:



- Servis adresleri

- Portlar

- Timeout değerleri

- Feature flag'ler

- Retry limitleri

- Sağlayıcı seçimi

- Log seviyeleri

- Ortam bilgisi

- Bağlantı ayarları

- Maliyet ve kullanım limitleri

- Teknik kapasite sınırları



Aşağıdakiler yalnızca configuration üzerinden belirlenmemelidir:



- Yetki kararı

- Tenant erişim kararı

- Onay zorunluluğu

- Finansal iş kuralları

- Kalite kabul kriterleri

- Makine güvenlik kuralları

- Domain durum geçişleri



Bu kurallar domain veya application katmanında uygulanmalıdır.



## Yapılandırma Kaynakları



Önerilen yapılandırma önceliği:



```text

Default configuration

  → Environment-specific configuration

  → Environment variables

  → Secret provider

  → Runtime override

```

Later sources override earlier sources only for allowlisted keys. Startup must fail with a clear error when required configuration is missing or invalid.

Secrets never use committed defaults, logs, client bundles, or exception payloads. Tenant connector credentials are isolated, rotated, and referenced through a secret provider.

Environment variables use a documented prefix and hierarchical mapping. Configuration changes that affect permissions, integrations, AI tools, or machine commands are audited and require controlled rollout.

