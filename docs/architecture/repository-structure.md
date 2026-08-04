\# CrevaFactOS Repository Yapısı



\## Amaç



Bu belge CrevaFactOS monorepo yapısını, uygulama sınırlarını, ortak paketleri ve temel bağımlılık kurallarını tanımlar.



CrevaFactOS; plastik enjeksiyon ve kalıp üreticileri için geliştirilen, AI Autopilot destekli bir Factory Operating System platformudur.



Repository yapısı aşağıdaki hedefleri desteklemelidir:



\- Web, API, Worker, AI, mobil ve Edge uygulamalarının birlikte geliştirilmesi

\- Modüler monolit backend mimarisi

\- Kontrollü ortak paket kullanımı

\- Plastik enjeksiyon ve kalıp sektörüne özgü domain sınırları

\- Tenant izolasyonu

\- Olay tabanlı entegrasyon

\- Tekrarlanabilir local development

\- CI kalite kapıları

\- Uzun vadeli servis ayrıştırma imkânı



\## Ana Repository Yapısı



```text

crevafactos/

├── apps/

│   ├── web/

│   ├── api/

│   ├── worker/

│   ├── ai-orchestrator/

│   ├── mobile/

│   └── edge-agent/

│

├── packages/

│   ├── design-tokens/

│   ├── web-ui/

│   ├── api-contracts/

│   ├── shared-schemas/

│   └── localization/

│

├── infrastructure/

│   ├── docker/

│   ├── database/

│   ├── monitoring/

│   └── deployment/

│

├── docs/

│   ├── architecture/

│   ├── product/

│   ├── security/

│   └── adr/

│

├── scripts/

├── .github/

│   └── workflows/

│

├── docker-compose.yml

├── .editorconfig

├── .gitattributes

├── .gitignore

├── .nvmrc

├── .npmrc

├── global.json

├── Directory.Build.props

├── package.json

├── pnpm-workspace.yaml

├── README.md

└── CrevaFactOS.sln

