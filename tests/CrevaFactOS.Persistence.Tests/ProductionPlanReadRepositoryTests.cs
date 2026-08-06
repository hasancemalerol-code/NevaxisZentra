using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Tests;

public sealed class ProductionPlanReadRepositoryTests
{
    private static readonly Guid TenantA = Guid.Parse("88000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("88000000-0000-4000-8000-000000000002");
    private static readonly Guid FacilityA = Guid.Parse("88000000-0000-4000-8000-000000000010");
    private static readonly Guid FacilityB = Guid.Parse("88000000-0000-4000-8000-000000000011");
    private static readonly Guid PlanA = Guid.Parse("88000000-0000-4000-8000-000000000020");
    private static readonly Guid PlanB = Guid.Parse("88000000-0000-4000-8000-000000000021");

    [Fact]
    public void ApplyListFiltersScopesByTenantAndFacility()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, null, 50))
            .ToList();

        Assert.All(filtered, plan => Assert.Equal(TenantA, plan.TenantId));
        Assert.All(filtered, plan => Assert.Equal(FacilityA, plan.FacilityId));
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void ApplyListFiltersExcludesCrossTenantRecords()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, null, 50))
            .ToList();

        Assert.DoesNotContain(filtered, plan => plan.TenantId == TenantB);
    }

    [Fact]
    public void ApplyListFiltersExcludesCrossFacilityRecords()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, null, 50))
            .ToList();

        Assert.DoesNotContain(filtered, plan => plan.FacilityId == FacilityB);
    }

    [Fact]
    public void ApplyListFiltersSupportsStatusFilter()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(
                    TenantA,
                    FacilityA,
                    ProductionPlanStatus.Draft,
                    null,
                    50))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal(ProductionPlanStatus.Draft, filtered[0].Status);
    }

    [Fact]
    public void ApplyListFiltersSupportsReferenceCodeSearch()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, "plan-a", 50))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal("plan-a", filtered[0].ReferenceCode);
    }

    [Fact]
    public void ApplyListFiltersSupportsTitleSearch()
    {
        var plans = CreatePlans();
        var filtered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, "second", 50))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal("Second plan", filtered[0].Title);
    }

    [Fact]
    public void ApplyListFiltersUsesDeterministicOrdering()
    {
        var plans = CreatePlans();
        var ordered = ProductionPlanReadRepository.ApplyListFilters(
                plans.AsQueryable(),
                new ProductionPlanListReadInput(TenantA, FacilityA, null, null, 50))
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .ThenBy(plan => plan.Id)
            .Select(plan => plan.Id)
            .ToArray();

        Assert.Equal([PlanB, PlanA], ordered);
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(0, 1)]
    [InlineData(500, 200)]
    [InlineData(25, 25)]
    public void NormalizeLimitClampsToAllowedRange(int? input, int expected) =>
        Assert.Equal(expected, ProductionPlanReadRepository.NormalizeLimit(input));

    [Fact]
    public async Task ListAsyncRejectsInvalidScope()
    {
        using var context = CreateContext();
        var repository = new ProductionPlanReadRepository(context, CreateTenantSession(context));
        var result = await repository.ListAsync(
            new ProductionPlanListReadInput(Guid.Empty, FacilityA, null, null, 50));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductionPlanReadFailureCodes.InvalidScope, result.Failure!.Code);
    }

    [Fact]
    public async Task ListAsyncRejectsInvalidLimit()
    {
        using var context = CreateContext();
        var repository = new ProductionPlanReadRepository(context, CreateTenantSession(context));
        var result = await repository.ListAsync(
            new ProductionPlanListReadInput(TenantA, FacilityA, null, null, 0));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductionPlanReadFailureCodes.InvalidRequest, result.Failure!.Code);
    }

    [Fact]
    public async Task FindByIdAsyncRejectsMissingProductionPlanId()
    {
        using var context = CreateContext();
        var repository = new ProductionPlanReadRepository(context, CreateTenantSession(context));
        var result = await repository.FindByIdAsync(
            new ProductionPlanDetailReadInput(TenantA, FacilityA, Guid.Empty));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductionPlanReadFailureCodes.InvalidRequest, result.Failure!.Code);
    }

    [Fact]
    public async Task UnavailableRepositoryReturnsSourceUnavailable()
    {
        var repository = new UnavailableProductionPlanReadRepository();
        var result = await repository.FindByIdAsync(
            new ProductionPlanDetailReadInput(TenantA, FacilityA, PlanA));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductionPlanReadFailureCodes.SourceUnavailable, result.Failure!.Code);
    }

    private static List<ProductionPlan> CreatePlans()
    {
        var earlier = new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2026, 8, 6, 8, 0, 0, TimeSpan.Zero);

        return
        [
            CreatePlan(PlanA, TenantA, FacilityA, "plan-a", "First plan", ProductionPlanStatus.NeedsReview, earlier),
            CreatePlan(PlanB, TenantA, FacilityA, "plan-b", "Second plan", ProductionPlanStatus.Draft, later),
            CreatePlan(Guid.NewGuid(), TenantB, FacilityA, "other-tenant", "Other tenant", ProductionPlanStatus.Draft, later),
            CreatePlan(Guid.NewGuid(), TenantA, FacilityB, "other-facility", "Other facility", ProductionPlanStatus.Draft, later),
        ];
    }

    private static ProductionPlan CreatePlan(
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string referenceCode,
        string title,
        ProductionPlanStatus status,
        DateTimeOffset updatedAtUtc) => new()
    {
        Id = id,
        TenantId = tenantId,
        FacilityId = facilityId,
        ReferenceCode = referenceCode,
        Title = title,
        Status = status,
        Origin = ProductionPlanOrigin.Manual,
        AiGenerated = false,
        Confidence = ProductionPlanConfidence.Medium,
        PlannedStartAtUtc = updatedAtUtc.AddHours(-1),
        PlannedEndAtUtc = updatedAtUtc.AddHours(8),
        ApprovalStatus = ProductionPlanApprovalStatus.NotRequired,
        TransferStatus = ProductionPlanTransferStatus.NotStarted,
        CreatedBy = "test",
        CreatedAtUtc = updatedAtUtc.AddDays(-1),
        UpdatedAtUtc = updatedAtUtc,
    };

    private static TenantDatabaseSession CreateTenantSession(CrevaFactOSDbContext context) =>
        new(context, new UnavailableTenantContextAccessor());

    private sealed class UnavailableTenantContextAccessor : ITenantContextAccessor
    {
        public bool IsAvailable => false;

        public TenantContext? Current => null;

        public Guid RequireTenantId() =>
            throw new InvalidOperationException("Tenant context is unavailable in this unit test.");
    }

    private static CrevaFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CrevaFactOSDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions()),
                npgsql => npgsql.MigrationsAssembly(CrevaFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new CrevaFactOSDbContext(options);
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
