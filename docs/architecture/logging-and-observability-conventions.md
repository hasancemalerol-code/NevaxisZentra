# NevaxisFactOS Logging ve Gözlemlenebilirlik Standartları



## Amaç



Bu belge NevaxisFactOS uygulamalarında loglama, metrik, tracing, health check, alarm ve operasyonel gözlemlenebilirlik standartlarını tanımlar.



Amaçlar:



- Sistem davranışını uçtan uca izleyebilmek

- Hataları ve performans sorunlarını hızlı tespit etmek

- API, Worker, AI Orchestrator, mobil ve Edge Agent işlemlerini ilişkilendirmek

- Tenant izolasyonunu koruyarak operasyonel görünürlük sağlamak

- Kritik üretim, makine, mesajlaşma ve AI süreçlerini izlemek

- Güvenlik olaylarını tespit etmek

- Kullanıcıya görünen sorunların teknik nedenine ulaşabilmek

- Production operasyonlarını ölçülebilir hâle getirmek



## Temel İlke



Gözlemlenebilirlik üç ana bileşenden oluşur:



```text

Logs

Metrics

Traces

```

All three carry correlation and, where safe, tenant/facility scope. Logs never contain secrets, raw credentials, unrestricted prompts, or unnecessary personal/financial data.

Required operational signals include API and Worker health, outbox backlog, consumer retries, dead letters, connector freshness, AI Task Center latency, approval expiry, ERP write-back reconciliation, Edge connectivity, telemetry lag, and command outcomes.

Audit and AI Decision Ledger records are authoritative evidence and are not replaced by application logs.

