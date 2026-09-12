using System.Text;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.Integration.FileImport;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class IntegrationPermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("integration.read", IntegrationPermissionKeys.Read);
        Assert.Equal("integration.manage", IntegrationPermissionKeys.Manage);
        Assert.Equal("integration.run", IntegrationPermissionKeys.Run);
        Assert.Equal("integration.mapping.manage", IntegrationPermissionKeys.MappingManage);
        Assert.Equal("integration.quarantine.manage", IntegrationPermissionKeys.QuarantineManage);
        Assert.Equal("integration.secret.configure", IntegrationPermissionKeys.SecretConfigure);
    }
}

public sealed class SimulatedIntegrationConnectorTests
{
    private static IntegrationConnector CreateConnector(string environment) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        FacilityId = Guid.NewGuid(),
        Name = "Simulated file import",
        ConnectorKind = IntegrationConnectorKinds.FileImport,
        SourceSystemType = "file",
        SourceSystemName = "development-fixture",
        Environment = environment,
        ConfigurationJson = "{}",
        ObservabilityJson = "{}",
        OwnerSubjectId = "subject",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task DevelopmentEnvironmentConnectionTestSucceeds()
    {
        var implementation = new SimulatedIntegrationConnectorImplementation();
        var result = await implementation.TestConnectionAsync(
            new IntegrationConnectorContext(CreateConnector(IntegrationEnvironments.Development), "subject"));

        Assert.True(result.Success);
        Assert.Equal(IntegrationHealthStatuses.Healthy, result.HealthStatus);
    }

    [Fact]
    public async Task ProductionEnvironmentBlocksSimulatedConnector()
    {
        var implementation = new SimulatedIntegrationConnectorImplementation();
        var result = await implementation.TestConnectionAsync(
            new IntegrationConnectorContext(CreateConnector(IntegrationEnvironments.Production), "subject"));

        Assert.False(result.Success);
        Assert.Equal(IntegrationHealthStatuses.Misconfigured, result.HealthStatus);
    }
}

public sealed class IntegrationConnectorContractTests
{
    [Fact]
    public void ConnectorKindsCoverCanonicalSpineFamilies()
    {
        Assert.Contains(IntegrationConnectorKinds.FileImport, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.FolderWatch, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.RestApi, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.Webhook, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.ReadOnlyDatabase, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.ErpAdapter, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.LocalConnector, GetAllKinds());
        Assert.Contains(IntegrationConnectorKinds.IotPlcBridge, GetAllKinds());
    }

    [Fact]
    public void EvidenceClassesDistinguishSimulatedFromMeasured()
    {
        Assert.NotEqual(IntegrationEvidenceClasses.Simulated, IntegrationEvidenceClasses.Measured);
        Assert.NotEqual(IntegrationEvidenceClasses.Simulated, IntegrationEvidenceClasses.Imported);
        Assert.Equal("source-conflict", IntegrationEvidenceClasses.SourceConflict);
    }

    private static string[] GetAllKinds() =>
    [
        IntegrationConnectorKinds.FileImport,
        IntegrationConnectorKinds.FolderWatch,
        IntegrationConnectorKinds.RestApi,
        IntegrationConnectorKinds.Webhook,
        IntegrationConnectorKinds.ReadOnlyDatabase,
        IntegrationConnectorKinds.ErpAdapter,
        IntegrationConnectorKinds.LocalConnector,
        IntegrationConnectorKinds.IotPlcBridge,
        IntegrationConnectorKinds.EdgeBridge,
    ];
}

public sealed class IntegrationFileImportTests
{
    [Fact]
    public void CsvPreviewParsesHeadersAndRows()
    {
        var csv = "order_id,material,qty\nPO-1,MAT-A,10\nPO-2,MAT-B,5";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var service = new IntegrationFileImportService();

        var preview = service.Preview(new IntegrationFileImportPreviewRequest(
            "orders.csv",
            stream.Length,
            stream,
            null,
            true,
            "utf-8"));

        Assert.True(preview.Success);
        Assert.Equal("csv", preview.FileKind);
        Assert.Equal(["order_id", "material", "qty"], preview.DetectedHeaders);
        Assert.Equal(2, preview.TotalPreviewRowCount);
        Assert.Equal(IntegrationEvidenceClasses.Imported, preview.EvidenceClass);
    }

    [Fact]
    public void OversizedUploadRejected()
    {
        var service = new IntegrationFileImportService();
        using var stream = new MemoryStream("a"u8.ToArray());

        var preview = service.Preview(new IntegrationFileImportPreviewRequest(
            "orders.csv",
            IntegrationFileIngestionLimits.MaxUploadBytes + 1,
            stream,
            null,
            true,
            "utf-8"));

        Assert.False(preview.Success);
        Assert.Equal("integration.file.validation-failed", preview.ErrorCode);
    }

    [Fact]
    public void PathTraversalFileNameRejected()
    {
        var service = new IntegrationFileImportService();
        using var stream = new MemoryStream("a,b\n1,2"u8.ToArray());

        var preview = service.Preview(new IntegrationFileImportPreviewRequest(
            "..\\orders.csv",
            stream.Length,
            stream,
            null,
            true,
            "utf-8"));

        Assert.False(preview.Success);
    }
}

public sealed class IntegrationConnectorImplementationResolverTests
{
    [Fact]
    public void ResolverReturnsRegisteredImplementationByKind()
    {
        var implementation = new SimulatedIntegrationConnectorImplementation();
        var resolver = new IntegrationConnectorImplementationResolver([implementation]);

        var resolved = resolver.Resolve(IntegrationConnectorKinds.FileImport);

        Assert.NotNull(resolved);
        Assert.Same(implementation, resolved);
    }

    [Fact]
    public void ResolverReturnsNullForUnknownKind()
    {
        var resolver = new IntegrationConnectorImplementationResolver([new SimulatedIntegrationConnectorImplementation()]);

        Assert.Null(resolver.Resolve(IntegrationConnectorKinds.Webhook));
    }
}
