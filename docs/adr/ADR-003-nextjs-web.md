\# ADR-003: Next.js Web Platformu



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Web uygulaması teknolojisi ve frontend mimarisi



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, masaüstü kullanım ağırlıklı kurumsal bir Factory Operating System platformudur.



Web uygulaması aşağıdaki alanları destekleyecektir:



\- AI Görev Merkezi

\- Yönetim dashboard'u

\- Üretim planlama

\- Üretim emirleri

\- Makine ve kalıp yönetimi

\- MRP ve malzeme planlama

\- Stok ve depo

\- Satın alma

\- Kalite

\- Bakım ve arıza

\- Sipariş ve CRM

\- Finans ve maliyet

\- İnsan kaynakları

\- Doküman ve revizyon

\- Müşteri ve tedarikçi portalları

\- Sistem yönetimi

\- Tenant, rol ve yetki yönetimi



Uygulama büyük veri tabloları, filtreleme, detay ekranları, iş akışları, grafikler, canlı durum bilgileri ve yetkili onay süreçleri içerecektir.



Dashboard tasarımı kurumsal ERP ve MES görünümünde olacaktır. AI özellikleri güçlü olmakla birlikte görsel sistemin tamamı yapay zekâ ürünü gibi tasarlanmayacaktır.



\## Karar



CrevaFactOS web uygulaması Next.js, React ve TypeScript kullanılarak geliştirilecektir.



Aşağıdaki yaklaşım kullanılacaktır:



\- Next.js App Router

\- TypeScript strict mode

\- React Server Components

\- Gerektiği yerde Client Components

\- Server Actions yalnızca uygun ve kontrollü kullanım alanlarında

\- Merkezi API istemcisi

\- Ortak tasarım token'ları

\- Paylaşılan UI bileşenleri

\- Şema tabanlı form doğrulama

\- Yetki kontrollü navigasyon

\- Tenant kapsamlı veri erişimi

\- Desktop-first responsive tasarım



Web uygulamasının başlangıç konumu:



```text

apps/web

