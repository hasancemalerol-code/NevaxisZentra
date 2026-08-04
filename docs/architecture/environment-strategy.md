\# CrevaFactOS Ortam Stratejisi



\## Amaç



Bu belge CrevaFactOS için local, test, staging ve production ortamlarının nasıl ayrılacağını, yapılandırılacağını ve işletileceğini tanımlar.



Amaçlar:



\- Ortamlar arasında veri ve yetki izolasyonu sağlamak

\- Local geliştirmeyi kolaylaştırmak

\- Testlerin deterministik çalışmasını desteklemek

\- Staging ortamını production'a mümkün olduğunca yakın tutmak

\- Secret ve credential kullanımını güvenli hâle getirmek

\- Dağıtım ve rollback süreçlerini standartlaştırmak

\- AI, mesajlaşma, veritabanı ve Edge Agent bağımlılıklarını kontrollü biçimde yönetmek

\- Ortam kaynaklı hataları azaltmak



\## Desteklenen Ortamlar



CrevaFactOS en az aşağıdaki ortamları destekleyecektir:



```text

local

test

staging

production

