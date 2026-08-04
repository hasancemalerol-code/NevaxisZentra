using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence;

public sealed class CrevaFactOSDbContext(DbContextOptions<CrevaFactOSDbContext> options)
    : DbContext(options)
{
    public const string MigrationsAssemblyName = "CrevaFactOS.Persistence.Migrations";
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
