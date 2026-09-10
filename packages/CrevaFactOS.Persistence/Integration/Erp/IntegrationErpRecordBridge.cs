using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Integration.Common;
using CrevaFactOS.Persistence.Integration.FileImport;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Integration.Erp;

public sealed record IntegrationErpFetchResult(
    bool Success,
    IntegrationFileTable? Table,
    string? SafeMessage);

public interface IIntegrationErpRecordBridge
{
    Task<IntegrationErpFetchResult> FetchAsync(
        IntegrationConnector connector,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationErpRecordBridge(
    CrevaFactOSDbContext dbContext,
    IErpAdapterResolver erpAdapterResolver) : IIntegrationErpRecordBridge
{
    public async Task<IntegrationErpFetchResult> FetchAsync(
        IntegrationConnector connector,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationErpAdapterBridgeConfiguration.TryParse(connector.ConfigurationJson);
        if (config is null || config.ErpConnectionProfileId is null)
        {
            return new IntegrationErpFetchResult(
                false,
                null,
                "ERP adapter bridge configuration is invalid.");
        }

        var profile = await dbContext.ErpConnectionProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == config.ErpConnectionProfileId.Value, cancellationToken);
        if (profile is null)
        {
            return new IntegrationErpFetchResult(false, null, "ERP connection profile not found.");
        }

        IErpAdapter adapter;
        try
        {
            adapter = erpAdapterResolver.Resolve(profile.Provider);
        }
        catch (InvalidOperationException)
        {
            return new IntegrationErpFetchResult(false, null, "ERP adapter is not registered.");
        }

        var capability = string.IsNullOrWhiteSpace(config.Capability)
            ? ErpCapabilities.ProductsRead
            : config.Capability;
        if (!adapter.Capabilities.Contains(capability))
        {
            return new IntegrationErpFetchResult(
                false,
                null,
                "ERP adapter capability is not supported.");
        }

        var connection = new ErpAdapterConnectionContext(
            profile.Id,
            profile.Provider,
            profile.ConnectionMode,
            profile.ExternalCompanyId,
            profile.ExternalCompanyCode,
            profile.BaseEndpoint,
            profile.CredentialReference);

        var headers = new List<string> { "code", "name" };
        var rows = new List<IReadOnlyDictionary<string, string>>();

        if (string.Equals(capability, ErpCapabilities.ProductsRead, StringComparison.OrdinalIgnoreCase))
        {
            var products = await adapter.ReadProductsAsync(connection, cancellationToken);
            foreach (var product in products)
            {
                rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["code"] = product.ExternalCode ?? product.ExternalId,
                    ["name"] = product.Name,
                });
            }
        }
        else
        {
            return new IntegrationErpFetchResult(
                false,
                null,
                "ERP bridge capability is read-only and limited to products in Wave C.");
        }

        return new IntegrationErpFetchResult(
            true,
            new IntegrationFileTable(IntegrationFileKinds.Json, headers, rows),
            null);
    }
}

public static class IntegrationErpAdapterCapabilities
{
    public const string MasterReferenceData = "master-reference-data";
    public const string ProductionDemandOrders = "production-demand-orders";
    public const string MaterialAvailability = "material-availability";
    public const string PurchasingStatus = "purchasing-status";
    public const string SupplierStatus = "supplier-status";
    public const string ProductionSourceRecords = "production-source-records";
    public const string Products = "products";
}

public static class IntegrationLogoAdapterMetadata
{
    public const string ProviderKey = "logo";
    public const bool ArchitectureReady = true;
    public const bool RealProviderVerified = false;

    public static IReadOnlyList<string> SupportedCapabilities { get; } =
    [
        IntegrationErpAdapterCapabilities.Products,
        IntegrationErpAdapterCapabilities.MasterReferenceData,
        IntegrationErpAdapterCapabilities.MaterialAvailability,
        IntegrationErpAdapterCapabilities.ProductionDemandOrders,
    ];
}
