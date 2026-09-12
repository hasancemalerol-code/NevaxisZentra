using System.Text.Json;
using NevaxisFactOS.Persistence.Audit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NevaxisFactOS.Persistence.Authorization;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class AuditFoundationTests
{
    [Fact]
    public void MetadataRedactsSensitiveKeysAndKeepsSafePrimitives()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["operation"] = "diagnostic",
            ["password"] = "must-not-survive",
            ["api-key"] = "must-not-survive",
            ["count"] = 2,
        };

        var accepted = AuditMetadataSanitizer.TrySanitize(metadata, out var json, out var failure);
        using var document = JsonDocument.Parse(json);

        Assert.True(accepted);
        Assert.Null(failure);
        Assert.Equal("diagnostic", document.RootElement.GetProperty("operation").GetString());
        Assert.Equal(AuditMetadataSanitizer.RedactedValue, document.RootElement.GetProperty("password").GetString());
        Assert.Equal(AuditMetadataSanitizer.RedactedValue, document.RootElement.GetProperty("api-key").GetString());
        Assert.DoesNotContain("must-not-survive", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MetadataRejectsNestedObjectsAndOversizedValues()
    {
        var nested = new Dictionary<string, object?>
        {
            ["nested"] = new Dictionary<string, object?> { ["value"] = "unsafe-shape" },
        };
        var oversized = new Dictionary<string, object?>
        {
            ["value"] = new string('x', AuditMetadataSanitizer.MaximumStringLength + 1),
        };

        Assert.False(AuditMetadataSanitizer.TrySanitize(nested, out _, out var nestedFailure));
        Assert.Equal("audit.metadata.invalid_value", nestedFailure);
        Assert.False(AuditMetadataSanitizer.TrySanitize(oversized, out _, out var oversizedFailure));
        Assert.Equal("audit.metadata.invalid_value", oversizedFailure);
    }

    [Fact]
    public void MetadataEnforcesKeyAndArrayLimits()
    {
        var tooMany = Enumerable.Range(0, AuditMetadataSanitizer.MaximumKeyCount + 1)
            .ToDictionary(index => $"key{index}", index => (object?)index, StringComparer.Ordinal);
        var longArray = new Dictionary<string, object?>
        {
            ["items"] = Enumerable.Range(0, AuditMetadataSanitizer.MaximumArrayLength + 1)
                .Cast<object?>()
                .ToArray(),
        };

        Assert.False(AuditMetadataSanitizer.TrySanitize(tooMany, out _, out var keyFailure));
        Assert.Equal("audit.metadata.too_many_keys", keyFailure);
        Assert.False(AuditMetadataSanitizer.TrySanitize(longArray, out _, out var arrayFailure));
        Assert.Equal("audit.metadata.invalid_value", arrayFailure);
    }

    [Fact]
    public void CorrelationContextIsRequiredAndSetOnlyOnce()
    {
        var context = new CorrelationContext();
        Assert.False(context.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => _ = context.CorrelationId);

        var id = Guid.NewGuid();
        context.Set(id, "request-1");

        Assert.True(context.IsAvailable);
        Assert.Equal(id, context.CorrelationId);
        Assert.Equal("request-1", context.RequestId);
        Assert.Throws<InvalidOperationException>(() => context.Set(Guid.NewGuid(), "request-2"));
    }

    [Fact]
    public void CorrelationContextDoesNotLeakAcrossScopes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Persistence:Enabled"] = "false" })
            .Build();
        var services = new ServiceCollection();
        services.AddNevaxisFactOSPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var firstScope = provider.CreateScope();
        firstScope.ServiceProvider.GetRequiredService<CorrelationContext>().Set(Guid.NewGuid(), "one");
        using var secondScope = provider.CreateScope();

        Assert.True(firstScope.ServiceProvider.GetRequiredService<CorrelationContext>().IsAvailable);
        Assert.False(secondScope.ServiceProvider.GetRequiredService<CorrelationContext>().IsAvailable);
    }

    [Fact]
    public async Task WriterRejectsInvalidRequiredFieldsAndNonUtcTimestampsBeforePersistence()
    {
        var writer = CreateWriter(withCorrelation: true);

        var missingResource = await writer.WriteAsync(new AuditWriteRequest(
            AuditEventTypes.AuthorizationDenied,
            AuditActions.Authorize,
            string.Empty,
            AuditOutcome.Denied));
        var nonUtc = await writer.WriteAsync(new AuditWriteRequest(
            AuditEventTypes.AuthorizationDenied,
            AuditActions.Authorize,
            "infrastructure.audit",
            AuditOutcome.Denied,
            OccurredAtUtc: new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.FromHours(3))));

        Assert.Equal("audit.resource_type.invalid", missingResource.FailureCode);
        Assert.Equal("audit.timestamp.not_utc", nonUtc.FailureCode);
    }

    [Fact]
    public async Task WriterRequiresCorrelationAndRejectsTenantNeutralSystemSource()
    {
        var withoutCorrelation = CreateWriter(withCorrelation: false);
        var request = new AuditWriteRequest(
            AuditEventTypes.AuthorizationDenied,
            AuditActions.Authorize,
            "infrastructure.audit",
            AuditOutcome.Denied);

        var missingCorrelation = await withoutCorrelation.WriteAsync(request);
        var systemBoundary = await CreateWriter(withCorrelation: true).WriteAsync(
            request with { Source = AuditSource.System });

        Assert.Equal("audit.context.unavailable", missingCorrelation.FailureCode);
        Assert.Equal("audit.system_boundary.required", systemBoundary.FailureCode);
    }

    private static PostgreSqlAuditWriter CreateWriter(bool withCorrelation)
    {
        var correlation = new CorrelationContext();
        if (withCorrelation)
        {
            correlation.Set(Guid.NewGuid(), "request");
        }

        return new PostgreSqlAuditWriter(
            Options.Create(new PersistenceOptions()),
            new StubAuthorizationContext(),
            correlation);
    }

    private sealed class StubAuthorizationContext : IAuthorizationContext
    {
        public bool IsAvailable => true;

        public AuthorizationContext Current { get; } =
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
