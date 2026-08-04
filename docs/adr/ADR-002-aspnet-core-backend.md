\# ADR-002: ASP.NET Core Backend Platformu



\- Durum: Kabul edildi

\- Tarih: 2026-08-04

\- Karar sahipleri: CrevaFactOS Product \& Development

\- Kapsam: API, Worker ve Edge backend teknolojisi



\## Bağlam



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, yüksek veri bütünlüğü, güvenlik, izlenebilirlik ve uzun süreli bakım gerektiren kurumsal bir Factory Operating System platformudur.



Backend aşağıdaki sorumlulukları taşıyacaktır:



\- Multi-tenant veri izolasyonu

\- Şirket, fabrika, şube ve tesis yönetimi

\- Sipariş, üretim, stok, kalıp, makine, bakım ve kalite süreçleri

\- Finansal ve operasyonel kayıtlar

\- İnsan onaylı AI Autopilot işlemleri

\- Transactional outbox ve mesajlaşma

\- RabbitMQ tabanlı olay işleme

\- Worker süreçleri

\- Edge Agent ve makine entegrasyonları

\- PLC, OPC UA, Modbus, MQTT ve üretici API bağlantıları

\- Audit ve güvenlik kayıtları

\- Uzun süren arka plan işlemleri

\- Harici ERP, muhasebe, e-posta ve WhatsApp entegrasyonları



Backend platformu; performans, tip güvenliği, uzun vadeli destek, gözlemlenebilirlik, test edilebilirlik ve kurumsal entegrasyon yetenekleri açısından güçlü olmalıdır.



\## Karar



CrevaFactOS’un ana backend platformu ASP.NET Core olacaktır.



Aşağıdaki uygulamalar .NET ile geliştirilecektir:



\- Web API

\- Background Worker

\- Edge Agent

\- Mesaj tüketicileri

\- Zamanlanmış işler

\- Entegrasyon servisleri

\- Outbox publisher

\- Makine ve cihaz bağlantı adaptörleri



Ana hedef framework:



```text

net10.0

