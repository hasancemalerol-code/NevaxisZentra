\# ADR-007: Multi-Tenant Veri İzolasyonu



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Tenant, şirket, fabrika ve veri erişim güvenliği



\## Bağlam



CrevaFactOS birden fazla müşteriye hizmet veren çoklu tenant destekli bir Factory Operating System platformudur.



Her müşteri aşağıdaki yapılardan bir veya daha fazlasına sahip olabilir:



\- Şirket

\- Fabrika

\- Tesis

\- Şube

\- Depo

\- Üretim alanı

\- Kalıp atölyesi

\- Fason operasyon birimi



Sistem aşağıdaki kritik verileri yönetecektir:



\- Siparişler

\- Müşteriler

\- Ürünler ve reçeteler

\- Kalıplar

\- Makineler

\- Üretim emirleri

\- Hammadde ve stoklar

\- Kalite kayıtları

\- Bakım ve arıza kayıtları

\- Finans ve maliyet kayıtları

\- AI görevleri

\- Yetkili onayları

\- Audit kayıtları

\- Makine telemetrisi

\- Omnichannel sipariş mesajları



Bir tenant hiçbir koşulda başka tenant’a ait verileri görememeli, değiştirememeli veya dolaylı biçimde çıkaramamalıdır.



\## Karar



CrevaFactOS shared application ve shared PostgreSQL modeliyle çalışacaktır.



Tenant izolasyonu aşağıdaki katmanlarda birlikte uygulanacaktır:



1\. Kimlik doğrulama

2\. Kullanıcı üyeliği

3\. Tenant context

4\. Application katmanı

5\. Veri erişim katmanı

6\. Veritabanı constraint ve politikaları

7\. Mesajlaşma

8\. Cache

9\. Dosya ve object storage

10\. Audit ve gözlemlenebilirlik



Tek bir katmana güvenilmeyecektir.



\## Tenant Modeli



Tenant, CrevaFactOS içindeki en üst müşteri izolasyon sınırıdır.



Bir tenant altında birden fazla şirket veya fabrika bulunabilir.



Örnek:



```text

Tenant

├── Company

│   ├── Factory

│   ├── Branch

│   └── Warehouse

└── Company

&#x20;   ├── Factory

&#x20;   └── Mold Workshop

