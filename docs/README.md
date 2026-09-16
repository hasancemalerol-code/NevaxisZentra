# NevaxisFactOS Documentation



Bu klasör, NevaxisFactOS ürün, mimari, güvenlik ve teknik karar belgelerini içerir.



## Klasörler



### `architecture`



Sistem mimarisi, modül sınırları, veri akışları ve entegrasyon yapıları burada belgelenir.



Kapsam:



- Sistem bağlamı

- Uygulama sınırları

- Modüler monolit yapısı

- API, Worker ve Edge ilişkileri

- Mesajlaşma akışları

- Veri akışları

- Deployment görünümü

- Gözlemlenebilirlik yapısı

- Güvenlik sınırları



### `product`



Ürün kapsamı ve iş gereksinimleri burada tutulur.



Kapsam:



- Ürün vizyonu

- Plastik enjeksiyon ve kalıp sektörüne özel gereksinimler

- Modül tanımları

- Kullanıcı rolleri

- İş akışları

- Ekran tanımları

- Kabul kriterleri

- Roadmap ([Manufacturing Roadmap — MP1/MP2/MP3](product/manufacturing-roadmap.md))
- Industrial UI design system ([Surface Matrix](product/industrial-ui-hardening-surface-matrix.md))

- PRD belgeleri

- AI Autopilot davranışları

- Omnichannel sipariş toplama akışları



### `security`



Güvenlik politikaları ve uygulama standartları burada tutulur.



Kapsam:



- Tenant izolasyonu

- Kimlik doğrulama

- Rol ve yetki modeli

- AI tool erişim sınırları

- Onay mekanizmaları

- Audit

- Secret yönetimi

- Edge Agent güvenliği

- Veri sınıflandırması

- Yedekleme ve geri yükleme

- Güvenlik testleri



### `adr`



Architecture Decision Record belgelerini içerir.



Her önemli teknik karar ayrı ADR ile belgelenmelidir.



## ADR Standardı



Her ADR aşağıdaki bölümleri içermelidir:



- Başlık

- Durum

- Tarih

- Karar sahipleri

- Kapsam

- Bağlam

- Karar

- Alternatifler

- Gerekçe

- Olumlu sonuçlar

- Riskler

- Geri dönüş şartları



ADR durumları:



```text

Önerildi

Kabul edildi

Reddedildi

Yerine başka karar geçti

Kullanımdan kaldırıldı

```

ADR-011 and locked product documents define the current product boundary. Earlier ADRs remain valid only where they do not conflict with that boundary.

## Documentation Quality

Documentation changes must use valid Markdown, close code fences, state ownership explicitly, and link superseding decisions. No implementation package may silently change a locked product decision.

