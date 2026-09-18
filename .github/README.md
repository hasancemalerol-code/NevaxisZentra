# NevaxisZentra GitHub Configuration



Bu klasör, NevaxisZentra repository'sinin GitHub tabanlı geliştirme ve kalite süreçlerini içerir.



## Klasörler



### `workflows`



GitHub Actions pipeline dosyalarını içerir.



Başlangıçta aşağıdaki kalite kapıları kurulacaktır:



- Web lint

- TypeScript typecheck

- Next.js production build

- .NET restore

- .NET build

- .NET test

- Docker image build doğrulaması

- Dependency güvenlik kontrolü

- Secret tarama

- Test sonuçlarının saklanması



## Pull Request Kuralları



Her pull request aşağıdaki kontrollerden geçmelidir:



1. İlgili branch güncel olmalıdır.

2. Web lint başarılı olmalıdır.

3. TypeScript typecheck başarılı olmalıdır.

4. Next.js build başarılı olmalıdır.

5. .NET build başarılı olmalıdır.

6. Testler başarılı olmalıdır.

7. Docker image üretilebilmelidir.

8. Kritik güvenlik açığı bulunmamalıdır.

9. Repository içinde gerçek secret bulunmamalıdır.

10. Kapsam dışı değişiklik yapılmamalıdır.



Bozuk build veya başarısız test içeren değişiklikler ana branch'e alınmamalıdır.



## Branch Standardı



Ana branch:



```text

main

```

Feature branches use the `codex/` prefix by default. Pull requests should remain small, identify the package objective, list targeted validation, and call out product-boundary or security impact.

Required checks will be added with application scaffolding. Documentation-only changes run Markdown and contract-consistency checks and do not fabricate application build results.

