# NevaxisZentra Shared Packages



Bu klasör, NevaxisZentra uygulamaları arasında kontrollü biçimde paylaşılacak ortak paketleri içerir.



## Paketler



### `design-tokens`



NevaxisZentra tasarım sisteminin temel görsel değerlerini içerir.



Kapsam:



- Renkler

- Tipografi

- Spacing

- Radius

- Shadow

- Z-index

- Durum renkleri

- Tema değişkenleri



Ana dashboard için kilitlenen kurumsal ERP/MES görünümü bu paket üzerinden korunacaktır.



### `web-ui`



Next.js web uygulamasında kullanılacak ortak UI bileşenlerini içerir.



Örnek bileşenler:



- Button

- Input

- Select

- Dialog

- Drawer

- Table

- DataGrid

- MetricCard

- StatusBadge

- FilterBar

- ApprovalPanel

- AuditTimeline

- PermissionBoundary



Bu paket domain iş kuralları içermemelidir.



### `api-contracts`



Web, mobil, AI Orchestrator ve harici istemciler tarafından kullanılacak API sözleşmelerini içerir.



Kapsam:



- Request ve response tipleri

- Problem Details modelleri

- Pagination sözleşmeleri

- Filter ve sorting modelleri

- Sürümlenmiş API tipleri

- Ortak DTO sınırları



Backend domain entity'leri doğrudan bu pakete taşınmamalıdır.



### `shared-schemas`



Uygulamalar arasında paylaşılması gereken doğrulama şemalarını içerir.



Kapsam:



- Form doğrulama

- Ortak veri biçimleri

- Ölçü birimleri

- Para ve tarih formatları

- Mesaj zarfı şemaları

- AI tool input ve output şemaları



Şemalar sürümlenebilir ve geriye dönük uyumlu tasarlanmalıdır.



### `localization`



NevaxisZentra metinleri ve yerelleştirme altyapısını içerir.



Başlangıç dilleri:



- Türkçe

- İngilizce



İleride müşteri ve pazar gereksinimlerine göre yeni diller eklenebilir.



## Paylaşım Kuralları



- Ortak paketler yalnızca gerçekten birden fazla uygulama tarafından kullanılan kodları içermelidir.

- Uygulamaya özel kod erken aşamada ortak pakete taşınmamalıdır.

- Domain iş kuralları frontend paketlerinde tutulmamalıdır.

- Paketler arasında döngüsel bağımlılık oluşmamalıdır.

- Sürümlenmesi gereken sözleşmeler açık biçimde ayrılmalıdır.

- Production secret'ları hiçbir pakette bulunmamalıdır.

- Tenant, yetki ve güvenlik sınırları ortak yardımcılar üzerinden zayıflatılmamalıdır.

- Ortak paketler küçük, tek sorumluluklu ve test edilebilir olmalıdır.



## Bağımlılık Yönü



Önerilen bağımlılık yönü:



```text

apps

  ↓

packages

```

Applications may depend on packages; packages must not depend on application internals. Shared code cannot become a route around tenant, permission, approval, audit, or integration boundaries.

No shared package may introduce classic ERP domain ownership contrary to ADR-011.

