using System.Reflection;
using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;
using Xunit;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// RC3 residuals: an unknown line id leaked an unhandled exception (HTTP 500) from the throughput optimizer, and the
/// production health summary answered 503 for every order because it read outside the tenant database session.
/// </summary>
public sealed class NotFoundClassificationTests
{
    private static readonly MethodInfo ObserveCore = typeof(ThroughputOptimizerService)
        .GetMethod("ObserveLineCoreAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public async Task UnknownLineIsAClassifiedNotFoundNotALeakedIntelligenceException()
    {
        var service = CreateService(new ThroughputIntelligenceException(ThroughputIntelligenceFailureCodes.NotFound, "Line not found."));

        var ex = await Assert.ThrowsAsync<ThroughputOptimizerException>(() => Observe(service));

        Assert.Equal(ThroughputOptimizerFailureCodes.NotFound, ex.FailureCode);
    }

    [Fact]
    public async Task OtherIntelligenceFailuresAreNotDisguisedAsNotFound()
    {
        var service = CreateService(new ThroughputIntelligenceException(ThroughputIntelligenceFailureCodes.ValidationFailed, "bad window"));

        var ex = await Assert.ThrowsAsync<ThroughputIntelligenceException>(() => Observe(service));

        Assert.Equal(ThroughputIntelligenceFailureCodes.ValidationFailed, ex.FailureCode);
    }

    [Fact]
    public async Task RuntimeFailuresStillPropagateAsRuntimeFailures()
    {
        var service = CreateService(new InvalidOperationException("database unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Observe(service));
    }

    [Fact]
    public void EveryTenantScopedProductionOperationsReadRunsInsideTheTenantDatabaseSession()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "packages", "NevaxisZentra.Persistence", "Production", "ProductionOperationsRepository.cs"));
        var end = source.IndexOf("class UnavailableProductionOperationsRepository", StringComparison.Ordinal);
        var real = end > 0 ? source[..end] : source;

        var methods = Regex.Split(real, @"\n    public (?=async Task<)").Skip(1).ToList();
        Assert.True(methods.Count >= 10, "operations repository methods parsed");
        var unscoped = methods
            .Where(method => method.Contains("dbContext.", StringComparison.Ordinal)
                             && !method.Contains("tenantDatabaseSession.Execute", StringComparison.Ordinal))
            .Select(method => method[..Math.Min(80, method.IndexOf('(', StringComparison.Ordinal))].Trim())
            .ToList();
        Assert.True(unscoped.Count == 0, "queries outside the tenant session (RLS context missing): " + string.Join(", ", unscoped));
    }

    [Fact]
    public void ProductionOperationsQueriesDoNotCallContainsOnAnInMemorySetEfCannotTranslate()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "packages", "NevaxisZentra.Persistence", "Production", "ProductionOperationsRepository.cs"));

        // IReadOnlySet<T>.Contains is not translatable by EF Core; materialise to an array first (health-summary 503).
        Assert.DoesNotContain("ProductionExceptionStatuses.Unresolved.Contains(", source, StringComparison.Ordinal);
    }

    private static Task Observe(ThroughputOptimizerService service) =>
        (Task)ObserveCore.Invoke(service, [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "subject", CancellationToken.None, null])!;

    private static ThroughputOptimizerService CreateService(Exception overviewFailure)
    {
        var intelligence = DispatchProxy.Create<IThroughputIntelligenceService, ThrowingIntelligenceProxy>();
        ((ThrowingIntelligenceProxy)(object)intelligence).Failure = overviewFailure;
        // Only the intelligence dependency is touched before the failure surfaces.
        return new ThroughputOptimizerService(null!, null!, intelligence, null!, null!, null!, null!);
    }

    public class ThrowingIntelligenceProxy : DispatchProxy
    {
        public Exception Failure { get; set; } = new InvalidOperationException();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw Failure;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
