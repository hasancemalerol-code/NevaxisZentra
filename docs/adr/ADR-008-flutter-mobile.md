\# ADR-008: Flutter Mobil Uygulama Platformu



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Mobil uygulamalar ve saha kullanım deneyimi



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, masaüstü ağırlıklı ancak fabrika sahasında mobil kullanım gerektiren bir Factory Operating System platformudur.



Mobil uygulama aşağıdaki kullanıcı gruplarını destekleyecektir:



\- Yöneticiler

\- Üretim sorumluları

\- Operatörler

\- Depo çalışanları

\- Bakım ekipleri

\- Kalite ekipleri

\- Satın alma çalışanları

\- Sevkiyat ekipleri

\- Saha servis personeli



Mobil kullanım senaryoları:



\- AI Görev Merkezi bildirimleri

\- Yetkili onay ve ret işlemleri

\- Üretim emri görüntüleme

\- Operatör iş başlangıç ve bitiş kayıtları

\- Makine ve kalıp durum takibi

\- Barkod ve QR kod tarama

\- Malzeme ve lot doğrulama

\- Stok hareketleri

\- Depolar arası transfer

\- Kalıp bağlama ve sökme kontrol listeleri

\- Bakım iş emirleri

\- Fotoğraflı arıza bildirimi

\- Kalite kontrol formları

\- İlk parça onayı

\- Uygunsuzluk kaydı

\- Sevkiyat doğrulama

\- Anlık alarm ve bildirimler

\- Çevrimdışı saha işlemleri



Android ve iOS için ayrı kod tabanları oluşturmak geliştirme ve bakım maliyetini artıracaktır.



\## Karar



CrevaFactOS mobil uygulamaları Flutter ile geliştirilecektir.



Başlangıç uygulama konumu:



```text

apps/mobile

