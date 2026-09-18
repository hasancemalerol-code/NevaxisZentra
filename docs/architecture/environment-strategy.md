# NevaxisZentra Ortam Stratejisi



## Amaç



Bu belge NevaxisZentra için local, test, staging ve production ortamlarının nasıl ayrılacağını, yapılandırılacağını ve işletileceğini tanımlar.



Amaçlar:



- Ortamlar arasında veri ve yetki izolasyonu sağlamak

- Local geliştirmeyi kolaylaştırmak

- Testlerin deterministik çalışmasını desteklemek

- Staging ortamını production'a mümkün olduğunca yakın tutmak

- Secret ve credential kullanımını güvenli hâle getirmek

- Dağıtım ve rollback süreçlerini standartlaştırmak

- AI, mesajlaşma, veritabanı ve Edge Agent bağımlılıklarını kontrollü biçimde yönetmek

- Ortam kaynaklı hataları azaltmak



## Desteklenen Ortamlar



NevaxisZentra en az aşağıdaki ortamları destekleyecektir:



```text

local

test

staging

production

```

Each environment has separate databases, RabbitMQ virtual hosts, object storage, secrets, connector credentials, AI-provider configuration, and Edge identities. Production customer data is not copied to local or test environments.

Configuration is promoted through reviewable deployment artifacts; secrets and environment-specific values are supplied externally. Staging should exercise migrations, connector failure behavior, outbox recovery, authorization, and tenant isolation before production rollout.

