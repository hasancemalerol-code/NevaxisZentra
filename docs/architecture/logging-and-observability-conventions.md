\# CrevaFactOS Logging ve Gözlemlenebilirlik Standartları



\## Amaç



Bu belge CrevaFactOS uygulamalarında loglama, metrik, tracing, health check, alarm ve operasyonel gözlemlenebilirlik standartlarını tanımlar.



Amaçlar:



\- Sistem davranışını uçtan uca izleyebilmek

\- Hataları ve performans sorunlarını hızlı tespit etmek

\- API, Worker, AI Orchestrator, mobil ve Edge Agent işlemlerini ilişkilendirmek

\- Tenant izolasyonunu koruyarak operasyonel görünürlük sağlamak

\- Kritik üretim, makine, mesajlaşma ve AI süreçlerini izlemek

\- Güvenlik olaylarını tespit etmek

\- Kullanıcıya görünen sorunların teknik nedenine ulaşabilmek

\- Production operasyonlarını ölçülebilir hâle getirmek



\## Temel İlke



Gözlemlenebilirlik üç ana bileşenden oluşur:



```text

Logs

Metrics

Traces

