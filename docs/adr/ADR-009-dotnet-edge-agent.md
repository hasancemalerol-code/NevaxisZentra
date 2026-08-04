\# ADR-009: .NET Edge Agent



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: Fabrika içi makine, PLC, cihaz ve yerel ağ entegrasyonları



\## Bağlam



CrevaFactOS, plastik enjeksiyon ve kalıp üreticileri için geliştirilen bulut merkezli bir Factory Operating System platformudur.



Fabrika sahasında kullanılacak enjeksiyon makineleri, PLC sistemleri, yardımcı ekipmanlar, enerji sayaçları, barkod yazıcıları, tartılar, kalite cihazları ve yerel dosya paylaşım alanları çoğunlukla doğrudan internet erişimine açık değildir.



Bu cihazlar ve sistemler aşağıdaki bağlantı yöntemlerinden birini veya birkaçını kullanabilir:



\- OPC UA

\- Modbus TCP

\- Modbus RTU

\- MQTT

\- Seri port

\- TCP socket

\- Üretici API’si

\- Dosya aktarımı

\- Klasör izleme

\- CSV veya XML çıktısı

\- Dijital giriş ve çıkışlar

\- Harici IoT sensörleri



CrevaFactOS’un aşağıdaki verileri güvenli biçimde toplaması gerekebilir:



\- Makine çalışma durumu

\- Üretim sayacı

\- Çevrim süresi

\- Alarm ve hata kodları

\- Duruş nedenleri

\- Enjeksiyon basıncı

\- Kalıp sıcaklığı

\- Eriyik sıcaklığı

\- Enerji tüketimi

\- Bakım sayaçları

\- Sensör ölçümleri

\- Proses parametreleri

\- Kalıp bağlama ve sökme bilgileri

\- Üretim emri durumu

\- Yardımcı ekipman durumu



Bulut API’nin fabrika ağına doğrudan bağlanması güvenlik, ağ erişimi, NAT, firewall ve operasyonel sürdürülebilirlik açısından uygun değildir.



\## Karar



CrevaFactOS, fabrika içi makine ve cihaz entegrasyonları için ayrı bir .NET Edge Agent uygulaması kullanacaktır.



Edge Agent aşağıdaki konumda geliştirilecektir:



```text

apps/edge-agent

