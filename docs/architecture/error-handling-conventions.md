\# CrevaFactOS Hata Yönetimi Standartları



\## Amaç



Bu belge CrevaFactOS uygulamalarında hata üretme, sınıflandırma, taşıma, loglama, kullanıcıya gösterme ve yeniden deneme kurallarını tanımlar.



Amaçlar:



\- Hataları tutarlı biçimde yönetmek

\- Domain ve teknik hataları ayırmak

\- Kullanıcıya anlaşılır ve güvenli mesajlar göstermek

\- Hassas sistem detaylarının dışarı sızmasını önlemek

\- API, Worker, AI Orchestrator, mobil ve Edge Agent arasında ortak davranış oluşturmak

\- Retry, idempotency ve kısmi başarı davranışlarını standartlaştırmak

\- Tenant ve correlation context bilgisini korumak

\- Operasyonel sorunların gözlemlenmesini kolaylaştırmak



\## Temel İlke



Her hata aynı değildir.



Bir hata en az aşağıdaki kategorilerden biriyle ilişkilendirilmelidir:



```text

validation

business\_rule

authorization

authentication

not\_found

conflict

concurrency

dependency

timeout

rate\_limit

infrastructure

integration

security

unexpected

