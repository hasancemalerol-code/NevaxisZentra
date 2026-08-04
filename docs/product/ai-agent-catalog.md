\# CrevaFactOS AI Ajan Kataloğu



\- Durum: Kilitli

\- Tarih: 2026-08-04

\- Ürün: CrevaFactOS

\- Kategori: AI Factory Operations Assistant

\- Sektör: Plastik enjeksiyon ve kalıp üretimi



\## Amaç



Bu belge CrevaFactOS içinde kullanılacak başlangıç AI ajanlarını, görev sınırlarını, veri kaynaklarını, üretebilecekleri çıktıları, kullanabilecekleri tool'ları ve Human-in-the-Loop gereksinimlerini tanımlar.



CrevaFactOS ajanları:



\- Mevcut ERP'nin yerine geçmez.

\- Kendi başına kritik işlem onaylayamaz.

\- Doğrudan veritabanına erişemez.

\- Doğrudan ERP'ye yazamaz.

\- Doğrudan PLC veya makineye bağlanamaz.

\- Yetkili kullanıcının permission sınırını aşamaz.

\- Tenant ve facility kapsamını değiştiremez.

\- Eksik veriyi uyduramaz.

\- Güvenlik sınırlarını kaldıramaz.

\- Audit kayıtlarını değiştiremez.



Ajanlar yalnızca sürümlenmiş, izinli ve audit edilen tool sözleşmeleri üzerinden çalışır.



\## Temel Ajan Modeli



Her AI ajanı aşağıdaki ortak sözleşmeye uymalıdır:



```text

Görev isteğini al

&#x20; → Kullanıcı ve tenant context'ini doğrula

&#x20; → Gerekli verileri izinli tool'larla getir

&#x20; → Veri güncelliği ve kalitesini değerlendir

&#x20; → Analiz veya taslak oluştur

&#x20; → Gerekçe, güven, risk ve etkiyi açıkla

&#x20; → Gerekirse Human-in-the-Loop onay iste

&#x20; → Onay sonrası mevcut durumu yeniden doğrula

&#x20; → İzinli execution tool'unu çağır

&#x20; → Sonucu doğrula

&#x20; → Audit ve AI Karar Defteri kaydı oluştur

