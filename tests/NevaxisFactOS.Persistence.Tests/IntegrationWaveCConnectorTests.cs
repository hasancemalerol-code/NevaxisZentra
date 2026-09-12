using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.Integration.Common;
using NevaxisFactOS.Persistence.Integration.Database;
using NevaxisFactOS.Persistence.Integration.Erp;
using NevaxisFactOS.Persistence.Integration.Rest;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class IntegrationRestSsrfGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1/data", false, false)]
    [InlineData("http://127.0.0.1/data", true, true)]
    [InlineData("http://169.254.169.254/latest/meta-data", false, false)]
    [InlineData("ftp://example.com/data", false, false)]
    public void BlocksUnsafeDestinations(string url, bool allowLocalhost, bool expectedAllowed)
    {
        var result = IntegrationRestSsrfGuard.ValidateRequestUri(
            new Uri(url),
            allowLocalhost,
            isDevelopmentEnvironment: allowLocalhost);

        Assert.Equal(expectedAllowed, result.Allowed);
    }
}

public sealed class IntegrationReadOnlyDatabaseFetcherTests
{
    [Fact]
    public void GeneratedSqlIsSelectOnly()
    {
        var config = new IntegrationReadOnlyDatabaseConnectorConfiguration
        {
            AllowlistedSourceName = "materials_view",
            CheckpointColumn = "updated_at",
            BatchSize = 100,
        };

        var sql = IntegrationReadOnlyDatabaseFetcher.BuildSelectSql(config, "{\"cursor\":\"2026-01-01\"}");
        Assert.StartsWith("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Matches("(?i)^SELECT\\b", sql);
        Assert.DoesNotMatch("(?i)\\bINSERT\\b", sql);
        Assert.DoesNotMatch("(?i)\\bUPDATE\\b", sql);
        Assert.DoesNotMatch("(?i)\\bDELETE\\b", sql);
    }

    [Fact]
    public void RejectsUnsafeIdentifiers()
    {
        Assert.False(IntegrationReadOnlyDatabaseFetcher.IsSafeIdentifier("../escape"));
        Assert.True(IntegrationReadOnlyDatabaseFetcher.IsSafeIdentifier("materials_view"));
    }
}

public sealed class IntegrationWaveCContractTests
{
    [Fact]
    public void WaveCConnectorKindsAreRegisteredInConstants()
    {
        Assert.Equal("rest-api", IntegrationConnectorKinds.RestApi);
        Assert.Equal("webhook", IntegrationConnectorKinds.Webhook);
        Assert.Equal("read-only-database", IntegrationConnectorKinds.ReadOnlyDatabase);
        Assert.Equal("folder-watch", IntegrationConnectorKinds.FolderWatch);
        Assert.Equal("erp-adapter", IntegrationConnectorKinds.ErpAdapter);
    }

    [Fact]
    public void LogoAdapterMetadataIsArchitectureReadyOnly()
    {
        Assert.True(IntegrationLogoAdapterMetadata.ArchitectureReady);
        Assert.False(IntegrationLogoAdapterMetadata.RealProviderVerified);
        Assert.Equal("logo", IntegrationLogoAdapterMetadata.ProviderKey);
        Assert.NotEmpty(IntegrationLogoAdapterMetadata.SupportedCapabilities);
    }

    [Fact]
    public void WebhookConfigurationParsesIngressKey()
    {
        var config = IntegrationWebhookConnectorConfiguration.TryParse(
            "{\"ingressKey\":\"wh-test\",\"replayWindowSeconds\":300}");
        Assert.NotNull(config);
        Assert.Equal("wh-test", config!.IngressKey);
    }
}
