# CrevaFactOS Hata Yönetimi Standartları



## Amaç



Bu belge CrevaFactOS uygulamalarında hata üretme, sınıflandırma, taşıma, loglama, kullanıcıya gösterme ve yeniden deneme kurallarını tanımlar.



Amaçlar:



- Hataları tutarlı biçimde yönetmek

- Domain ve teknik hataları ayırmak

- Kullanıcıya anlaşılır ve güvenli mesajlar göstermek

- Hassas sistem detaylarının dışarı sızmasını önlemek

- API, Worker, AI Orchestrator, mobil ve Edge Agent arasında ortak davranış oluşturmak

- Retry, idempotency ve kısmi başarı davranışlarını standartlaştırmak

- Tenant ve correlation context bilgisini korumak

- Operasyonel sorunların gözlemlenmesini kolaylaştırmak



## Temel İlke



Her hata aynı değildir.



Bir hata en az aşağıdaki kategorilerden biriyle ilişkilendirilmelidir:



```text

validation

business_rule

authorization

authentication

not_found

conflict

concurrency

dependency

timeout

rate_limit

infrastructure

integration

security

unexpected

```

Errors expose a stable code, safe user message, correlation identifier, and appropriate retry guidance. Internal stack traces, SQL, credentials, connector secrets, and cross-tenant identifiers are never returned to clients.

Retries are allowed only for classified transient operations and must respect idempotency. Authorization, approval expiry, stale-data, validation, and local-safety rejection errors fail closed and are not converted into automatic success paths.

