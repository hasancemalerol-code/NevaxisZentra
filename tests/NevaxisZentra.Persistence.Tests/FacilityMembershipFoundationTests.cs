using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FacilityMembershipFoundationTests
{
    [Fact]
    public void TenantFacilityMembershipCombinationIsUnique()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(
                        ["FacilityId", "TenantId", "TenantMembershipId"],
                        StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void SameMembershipCanReferenceMultipleFacilities()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        var membershipScopedIndexes = entityType.GetIndexes()
            .Where(index => index.Properties.Any(property => property.Name == nameof(FacilityMembership.TenantMembershipId)))
            .ToArray();

        Assert.Contains(
            membershipScopedIndexes,
            index => !index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["Status", "TenantId", "TenantMembershipId"], StringComparer.Ordinal));
    }

    [Fact]
    public void CompositeTenantFacilityForeignKeyUsesTenantScopedPrincipal()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys()
            .Single(key =>
                key.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["FacilityId", "TenantId"], StringComparer.Ordinal));

        Assert.Equal(typeof(Facility), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void CompositeTenantMembershipForeignKeyUsesTenantScopedPrincipal()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys()
            .Single(key =>
                key.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["TenantId", "TenantMembershipId"], StringComparer.Ordinal));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void FacilityMembershipRequiresTenantFacilityAndMembershipColumns()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        Assert.False(entityType.FindProperty(nameof(FacilityMembership.TenantId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(FacilityMembership.FacilityId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(FacilityMembership.TenantMembershipId))!.IsNullable);
    }

    [Fact]
    public void FacilityMembershipStatusEnumIncludesActiveAndInactive()
    {
        Assert.Equal(FacilityMembershipStatus.Active, Enum.Parse<FacilityMembershipStatus>("Active"));
        Assert.Equal(FacilityMembershipStatus.Inactive, Enum.Parse<FacilityMembershipStatus>("Inactive"));
    }

    [Fact]
    public void FacilityMembershipHasTenantQueryFilter()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void FacilityMembershipUsesVersionConcurrencyToken()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FacilityMembership));
        Assert.NotNull(entityType);

        var versionProperty = entityType.FindProperty(nameof(FacilityMembership.Version));
        Assert.NotNull(versionProperty);
        Assert.True(versionProperty.IsConcurrencyToken);
    }

    [Fact]
    public void MigrationIncludesFacilityMembershipAndPlanningReadPermissionFoundation()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Contains(
            migrations,
            migration => migration.EndsWith(
                "_AddFacilityMembershipAndPlanningReadPermission",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionPlanningReadPermissionKeyIsRegisteredExactlyOnce()
    {
        Assert.Equal("production.planning.read", ProductionPlanningPermissionKeys.Read);
    }

    private static NevaxisZentraDbContext CreateContext(ITenantContextAccessor? tenantContextAccessor = null)
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions()),
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options, tenantContextAccessor);
    }

    private static PersistenceOptions CreateOptions() => new()
    {
        Enabled = true,
        Host = "db.example.test",
        Port = 5544,
        Database = "foundation",
        Username = "foundation-user",
        Password = "not-for-logs",
        SslMode = "Disable",
        ConnectionTimeoutSeconds = 12,
        CommandTimeoutSeconds = 34,
        Pooling = false,
    };
}
