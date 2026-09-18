using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FacilityProductionPlanningFoundationTests
{
    [Fact]
    public void FacilityCodeUniqueIndexIsTenantScoped()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Facility));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["Code", "TenantId"], StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void ProductionPlanReferenceCodeUniqueIndexIsTenantAndFacilityScoped()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductionPlan));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .SequenceEqual(["FacilityId", "ReferenceCode", "TenantId"], StringComparer.Ordinal));

        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void ProductionPlanUsesCompositeTenantFacilityForeignKey()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductionPlan));
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
    public void ProductionPlanRequiresTenantAndFacilityColumns()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductionPlan));
        Assert.NotNull(entityType);

        Assert.False(entityType.FindProperty(nameof(ProductionPlan.TenantId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(ProductionPlan.FacilityId))!.IsNullable);
    }

    [Fact]
    public void FacilityRequiresTenantColumn()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Facility));
        Assert.NotNull(entityType);

        Assert.False(entityType.FindProperty(nameof(Facility.TenantId))!.IsNullable);
    }

    [Theory]
    [InlineData(ProductionPlanStatus.NeedsReview, "needs-review")]
    [InlineData(ProductionPlanStatus.InProgress, "in-progress")]
    [InlineData(ProductionPlanOrigin.ApprovedOrder, "approved-order")]
    [InlineData(ProductionPlanConfidence.High, "high")]
    [InlineData(ProductionPlanApprovalStatus.NotRequired, "not-required")]
    [InlineData(ProductionPlanTransferStatus.NotStarted, "not-started")]
    [InlineData(ProductionPlanTargetSystem.NevaxisZentra, "crevafactos")]
    public void EnumStorageRoundTripPreservesFrontendValues<TEnum>(
        TEnum enumValue,
        string storageValue)
        where TEnum : struct, Enum
    {
        var roundTrip = enumValue switch
        {
            ProductionPlanStatus status => (Enum)ProductionPlanValueConversions.StatusFromStorage(
                ProductionPlanValueConversions.ToStorage(status)),
            ProductionPlanOrigin origin => ProductionPlanValueConversions.OriginFromStorage(
                ProductionPlanValueConversions.ToStorage(origin)),
            ProductionPlanConfidence confidence => ProductionPlanValueConversions.ConfidenceFromStorage(
                ProductionPlanValueConversions.ToStorage(confidence)),
            ProductionPlanApprovalStatus approvalStatus => ProductionPlanValueConversions.ApprovalStatusFromStorage(
                ProductionPlanValueConversions.ToStorage(approvalStatus)),
            ProductionPlanTransferStatus transferStatus => ProductionPlanValueConversions.TransferStatusFromStorage(
                ProductionPlanValueConversions.ToStorage(transferStatus)),
            ProductionPlanTargetSystem targetSystem => ProductionPlanValueConversions.TargetSystemFromStorage(
                ProductionPlanValueConversions.ToStorage(targetSystem)),
            _ => throw new InvalidOperationException("Unsupported enum value."),
        };

        Assert.Equal(enumValue, roundTrip);
        Assert.Equal(storageValue, GetStorageValue(enumValue));
    }

    [Fact]
    public void InvalidEnumStorageValueIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionPlanValueConversions.StatusFromStorage("unknown-status"));

        Assert.Contains("Unsupported ProductionPlanStatus", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlannedWindowTimestampsUseDateTimeOffsetColumns()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductionPlan));
        Assert.NotNull(entityType);

        Assert.Equal(typeof(DateTimeOffset), entityType.FindProperty(nameof(ProductionPlan.PlannedStartAtUtc))!.ClrType);
        Assert.Equal(typeof(DateTimeOffset), entityType.FindProperty(nameof(ProductionPlan.PlannedEndAtUtc))!.ClrType);
    }

    [Fact]
    public void VersionColumnIsConfiguredAsConcurrencyToken()
    {
        using var context = CreateContext();
        var facilityVersion = context.Model.FindEntityType(typeof(Facility))!
            .FindProperty(nameof(Facility.Version));
        var planVersion = context.Model.FindEntityType(typeof(ProductionPlan))!
            .FindProperty(nameof(ProductionPlan.Version));

        Assert.True(facilityVersion!.IsConcurrencyToken);
        Assert.True(planVersion!.IsConcurrencyToken);
    }

    [Fact]
    public void AddedProductionPlanReceivesCurrentTenantIdentifier()
    {
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(tenantId, "tenant-a", Guid.NewGuid()));
        using var context = CreateContext(accessor);
        var plan = CreateProductionPlan(Guid.NewGuid());
        context.Add(plan);

        var preparedTenantId = context.PrepareTenantScopedChanges();

        Assert.Equal(tenantId, preparedTenantId);
        Assert.Equal(tenantId, plan.TenantId);
    }

    [Fact]
    public void CrossTenantProductionPlanAttachmentIsRejected()
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a", Guid.NewGuid()));
        using var context = CreateContext(accessor);
        var plan = CreateProductionPlan(Guid.NewGuid());
        plan.TenantId = Guid.NewGuid();
        context.Add(plan);

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.PrepareTenantScopedChanges());

        Assert.Contains("another tenant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationIncludesFacilityAndProductionPlanningFoundation()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Contains(
            migrations,
            migration => migration.EndsWith("_AddFacilityAndProductionPlanningFoundation", StringComparison.Ordinal));
    }

    private static string GetStorageValue<TEnum>(TEnum enumValue)
        where TEnum : struct, Enum =>
        enumValue switch
        {
            ProductionPlanStatus status => ProductionPlanValueConversions.ToStorage(status),
            ProductionPlanOrigin origin => ProductionPlanValueConversions.ToStorage(origin),
            ProductionPlanConfidence confidence => ProductionPlanValueConversions.ToStorage(confidence),
            ProductionPlanApprovalStatus approvalStatus => ProductionPlanValueConversions.ToStorage(approvalStatus),
            ProductionPlanTransferStatus transferStatus => ProductionPlanValueConversions.ToStorage(transferStatus),
            ProductionPlanTargetSystem targetSystem => ProductionPlanValueConversions.ToStorage(targetSystem),
            _ => throw new InvalidOperationException("Unsupported enum value."),
        };

    private static ProductionPlan CreateProductionPlan(Guid facilityId) => new()
    {
        Id = Guid.NewGuid(),
        FacilityId = facilityId,
        ReferenceCode = "production-plan-001",
        Title = "Test plan",
        Status = ProductionPlanStatus.Draft,
        Origin = ProductionPlanOrigin.Manual,
        AiGenerated = false,
        Confidence = ProductionPlanConfidence.Medium,
        PlannedStartAtUtc = new DateTimeOffset(2026, 8, 6, 8, 0, 0, TimeSpan.Zero),
        PlannedEndAtUtc = new DateTimeOffset(2026, 8, 7, 17, 0, 0, TimeSpan.Zero),
        ApprovalStatus = ProductionPlanApprovalStatus.NotRequired,
        TransferStatus = ProductionPlanTransferStatus.NotStarted,
        CreatedBy = "test-user",
        CreatedAtUtc = new DateTimeOffset(2026, 8, 6, 7, 0, 0, TimeSpan.Zero),
        UpdatedAtUtc = new DateTimeOffset(2026, 8, 6, 7, 0, 0, TimeSpan.Zero),
    };

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
