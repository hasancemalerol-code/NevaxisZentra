# NevaxisFactOS Bağımlılık Kuralları



## Amaç



Bu belge NevaxisFactOS içindeki uygulamalar, backend modülleri, katmanlar, ortak paketler ve harici sistemler arasındaki bağımlılık kurallarını tanımlar.



Amaçlar:



- Modüler monolit sınırlarını korumak

- Döngüsel bağımlılıkları engellemek

- Domain iş kurallarını altyapı detaylarından ayırmak

- Tenant ve güvenlik sınırlarının atlanmasını önlemek

- Uygulamaların birbirlerinin iç koduna bağlanmasını engellemek

- Gelecekte bazı modüllerin ayrı servislere ayrılabilmesini kolaylaştırmak

- Test edilebilir ve sürdürülebilir bir kod tabanı oluşturmak



## Temel İlke



Bağımlılıklar içe doğru yönelmelidir.



İş kuralları, framework ve altyapı detaylarına bağımlı olmamalıdır.



Genel yön:



```text

Api

  ↓

Application

  ↓

Domain

```

Infrastructure implements inward-facing contracts. Domain code does not depend on HTTP, database, message broker, AI provider, ERP SDK, or device protocols.

Modules do not access another module's internal tables. Cross-module work uses explicit application contracts or versioned events. Web, mobile, Teams, Worker, AI Orchestrator, and Edge Agent cannot bypass API authorization, authoritative AI Task Center state, or audit boundaries.

Architecture tests should reject forbidden project references and cycles.

