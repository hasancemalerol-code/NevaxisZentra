\# CrevaFactOS Bağımlılık Kuralları



\## Amaç



Bu belge CrevaFactOS içindeki uygulamalar, backend modülleri, katmanlar, ortak paketler ve harici sistemler arasındaki bağımlılık kurallarını tanımlar.



Amaçlar:



\- Modüler monolit sınırlarını korumak

\- Döngüsel bağımlılıkları engellemek

\- Domain iş kurallarını altyapı detaylarından ayırmak

\- Tenant ve güvenlik sınırlarının atlanmasını önlemek

\- Uygulamaların birbirlerinin iç koduna bağlanmasını engellemek

\- Gelecekte bazı modüllerin ayrı servislere ayrılabilmesini kolaylaştırmak

\- Test edilebilir ve sürdürülebilir bir kod tabanı oluşturmak



\## Temel İlke



Bağımlılıklar içe doğru yönelmelidir.



İş kuralları, framework ve altyapı detaylarına bağımlı olmamalıdır.



Genel yön:



```text

Api

&#x20; ↓

Application

&#x20; ↓

Domain

