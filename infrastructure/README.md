\# CrevaFactOS Infrastructure



Bu klasör, CrevaFactOS platformunun yerel geliştirme, gözlemlenebilirlik, dağıtım ve altyapı tanımlarını içerir.



\## Klasörler



\### `docker`



Yerel geliştirme ve container yapılandırmalarını içerir.



Başlangıç kapsamı:



\- Web image

\- API image

\- Worker image

\- AI Orchestrator image

\- Edge Agent geliştirme image'ı

\- PostgreSQL

\- Redis

\- RabbitMQ

\- MinIO

\- Docker Compose yardımcı dosyaları



\### `database`



Veritabanı altyapısı ve operasyonel yardımcı dosyaları içerir.



Kapsam:



\- Yerel başlangıç scriptleri

\- Geliştirme seed hazırlıkları

\- Yedekleme ve geri yükleme yardımcıları

\- Migration operasyon notları

\- Veritabanı sağlık kontrolü

\- PostgreSQL extension tanımları



Ana schema değişiklikleri uygulama migration'ları üzerinden yapılmalıdır.



\### `monitoring`



Sistem gözlemleme altyapısını içerir.



Kapsam:



\- Log toplama

\- Metrik tanımları

\- Dashboard hazırlıkları

\- Alarm kuralları

\- API sağlık durumu

\- Worker sağlık durumu

\- RabbitMQ kuyrukları

\- PostgreSQL bağlantıları

\- Redis sağlık durumu

\- Edge Agent bağlantıları

\- Makine telemetrisi

\- AI görev metrikleri



\### `deployment`



Staging ve production dağıtım tanımlarını içerir.



İlerleyen aşamalarda:



\- Container deployment

\- Ortam yapılandırmaları

\- Secret referansları

\- Reverse proxy

\- TLS

\- Ölçekleme

\- Rollback

\- Health probe

\- Backup politikaları

\- Felaket kurtarma



tanımları burada tutulabilir.



\## Ortamlar



CrevaFactOS en az aşağıdaki ortamları destekleyecektir:



\- Local

\- Test

\- Staging

\- Production



Her ortam ayrı:



\- PostgreSQL veritabanı

\- Redis instance veya namespace

\- RabbitMQ virtual host

\- Object storage bucket

\- Secret

\- Log seviyesi

\- AI sağlayıcı yapılandırması

\- Entegrasyon credential'ları



kullanmalıdır.



\## Yerel Geliştirme Servisleri



Başlangıç servisleri:



| Servis | Port |

| --- | ---: |

| Web | 3000 |

| API | 5000 |

| PostgreSQL | 5432 |

| Redis | 6379 |

| RabbitMQ | 5672 |

| RabbitMQ Yönetim | 15672 |

| MinIO API | 9000 |

| MinIO Panel | 9001 |



Portlar yalnızca development varsayımlarıdır ve merkezi yapılandırma üzerinden değiştirilebilir olmalıdır.



\## Temel Kurallar



\- Production secret'ları repository içinde tutulmamalıdır.

\- Gerçek credential değerleri yalnızca güvenli secret yönetim sistemlerinde saklanmalıdır.

\- Repository içinde yalnızca `.env.example` bulunmalıdır.

\- Local geliştirme verileri production verisi içermemelidir.

\- Production schema değişiklikleri migration üzerinden yapılmalıdır.

\- Container image'ları tekrar üretilebilir olmalıdır.

\- Health check bulunmayan servis production'a alınmamalıdır.

\- Backup yalnızca alınmamalı, geri yükleme testi de yapılmalıdır.

\- Ortamlar arasında credential paylaşılmamalıdır.

\- Altyapı değişiklikleri kod incelemesine tabi olmalıdır.



\## Health Check Standardı



\### Liveness



Uygulama sürecinin çalışıp çalışmadığını kontrol eder.



```text

/health/live

