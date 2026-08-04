# Persistence Foundation

This project contains only the PostgreSQL/EF Core infrastructure baseline. `CrevaFactOSDbContext` intentionally has no `DbSet` properties or entity mappings.

Persistence is disabled by default. When enabled, configuration is validated before the API starts. Migrations are held in `CrevaFactOS.Persistence.Migrations`, use the default `public` schema, and deliberately retain EF Core's standard `__EFMigrationsHistory` table. Applications never apply migrations automatically.

Connection strings and passwords must not be logged. Use `PostgreSqlConnectionFactory.GetSafeEndpointDescription` for diagnostics; it omits the username, password, and complete connection string.
