using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Erp;
using NevaxisFactOS.Persistence.Erp.Logo;
using NevaxisFactOS.Persistence.Erp.Mikro;
using NevaxisFactOS.Persistence.Erp.Netsis;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.Erp;

public static class ErpEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/erp").RequireAuthorization();

        group.MapGet("/connections", ListConnectionsAsync);
        group.MapPost("/connections", CreateConnectionAsync);
        group.MapGet("/connections/{connectionId:guid}", GetConnectionAsync);
        group.MapPut("/connections/{connectionId:guid}", UpdateConnectionAsync);
        group.MapPost("/connections/{connectionId:guid}/health-check", HealthCheckAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/customers", SyncCustomersAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/products", SyncProductsAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/orders", SyncOrdersAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/warehouses", SyncWarehousesAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/inventory", SyncInventoryAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/purchasing", SyncPurchasingAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/production", SyncProductionAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/documents", SyncDocumentsAsync);

        group.MapGet("/sync-runs", ListSyncRunsAsync);
        group.MapGet("/sync-runs/{syncRunId:guid}", GetSyncRunAsync);
        group.MapPost("/sync-runs/{syncRunId:guid}/retry", RetrySyncRunAsync);

        group.MapGet("/customers", ListCustomersAsync);
        group.MapGet("/products", ListProductsAsync);
        group.MapGet("/orders", ListOrdersAsync);
        group.MapGet("/orders/{orderId:guid}", GetOrderAsync);
        group.MapGet("/orders/{orderId:guid}/readiness", GetOrderReadinessAsync);
        group.MapGet("/orders/{orderId:guid}/material-availability", GetOrderMaterialAvailabilityAsync);
        group.MapGet("/warehouses", ListWarehousesAsync);
        group.MapGet("/warehouses/{warehouseId:guid}", GetWarehouseAsync);
        group.MapGet("/inventory", ListInventoryAsync);
        group.MapGet("/inventory/availability", ListInventoryAvailabilityAsync);
        group.MapGet("/purchase-orders", ListPurchaseOrdersAsync);
        group.MapGet("/purchase-orders/{purchaseOrderId:guid}", GetPurchaseOrderAsync);
        group.MapGet("/production-orders", ListProductionOrdersAsync);
        group.MapGet("/production-orders/{productionOrderId:guid}", GetProductionOrderAsync);
        group.MapGet("/documents", ListDocumentsAsync);
        group.MapGet("/documents/{documentId:guid}", GetDocumentAsync);
        group.MapGet("/writebacks", ListWritebacksAsync);
        group.MapGet("/writebacks/{writebackId:guid}", GetWritebackAsync);
        group.MapPost("/writebacks", CreateWritebackAsync);
        group.MapPost("/writebacks/{writebackId:guid}/submit", SubmitWritebackAsync);
        group.MapPost("/writebacks/{writebackId:guid}/approve", ApproveWritebackAsync);
        group.MapPost("/writebacks/{writebackId:guid}/reject", RejectWritebackAsync);
        group.MapPost("/writebacks/{writebackId:guid}/execute", ExecuteWritebackAsync);
        group.MapGet("/sync-reliability", ListSyncReliabilityAsync);
        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);

        group.MapGet("/mappings", ListMappingsAsync);
        group.MapGet("/mappings/{mappingId:guid}", GetMappingAsync);
        group.MapPost("/mappings/{mappingId:guid}/confirm", ConfirmMappingAsync);
        group.MapPost("/mappings/{mappingId:guid}/ignore", IgnoreMappingAsync);
        group.MapPost("/mappings/{mappingId:guid}/remap", RemapMappingAsync);

        group.MapGet("/reconciliation/customers", ListCustomerReconciliationAsync);
        group.MapGet("/reconciliation/products", ListProductReconciliationAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSErp(this IServiceCollection services)
    {
        services.AddSingleton<LogoReadTransportOptions>();
        services.AddSingleton<LogoReadTransportTestFixture>();
        services.AddSingleton<LogoUnconfiguredReadTransport>();
        services.AddHttpClient<LogoBridgeReadTransport>();
        services.AddSingleton<ILogoCredentialResolver, ConfigurationLogoCredentialResolver>();
        services.AddSingleton<LogoTransportResolver>();

        services.AddSingleton<MikroReadTransportOptions>();
        services.AddSingleton<MikroReadTransportTestFixture>();
        services.AddSingleton<MikroUnconfiguredReadTransport>();
        services.AddHttpClient<MikroBridgeReadTransport>();
        services.AddSingleton<IMikroCredentialResolver, ConfigurationMikroCredentialResolver>();
        services.AddSingleton<MikroTransportResolver>();

        services.AddSingleton<NetsisReadTransportOptions>();
        services.AddSingleton<NetsisReadTransportTestFixture>();
        services.AddSingleton<NetsisUnconfiguredReadTransport>();
        services.AddHttpClient<NetsisBridgeReadTransport>();
        services.AddSingleton<INetsisCredentialResolver, ConfigurationNetsisCredentialResolver>();
        services.AddSingleton<NetsisTransportResolver>();

        services.AddSingleton<IErpAdapter, SimulatorErpAdapter>();
        services.AddSingleton<IErpAdapter, LogoErpAdapter>();
        services.AddSingleton<IErpAdapter, MikroErpAdapter>();
        services.AddSingleton<IErpAdapter, NetsisErpAdapter>();
        services.AddSingleton<IErpAdapterResolver, ErpAdapterResolver>();

        services.AddScoped<IErpConnectionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpConnectionRepository()
                : new ErpConnectionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        services.AddScoped<IErpInventoryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpInventoryRepository()
                : new ErpInventoryRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        services.AddScoped<IErpSyncRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpSyncRepository()
                : new ErpSyncRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        services.AddScoped<IErpMappingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpMappingRepository()
                : new ErpMappingRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IErpProductionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpProductionRepository()
                : new ErpProductionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        services.AddScoped<IErpWritebackRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpWritebackRepository()
                : new ErpWritebackRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpWritebackAdapterResolver>());
        });

        services.AddSingleton<IErpWritebackAdapter, SimulatorWritebackAdapter>();
        services.AddSingleton<IErpWritebackAdapterResolver, ErpWritebackAdapterResolver>();

        return services;
    }

    private static async Task<IResult> ListConnectionsAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await connectionRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(rows.Select(MapConnection).ToList());
    }

    private static async Task<IResult> GetConnectionAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await connectionRepository.GetByIdAsync(context, connectionId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Connection profile not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MapConnection(row));
    }

    private static async Task<IResult> CreateConnectionAsync(
        CreateErpConnectionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectionRepository.CreateAsync(
            context,
            new CreateErpConnectionCommand(
                request.Provider,
                request.Name,
                request.ExternalCompanyId,
                request.ExternalCompanyCode,
                request.BaseEndpoint,
                request.ConnectionMode,
                request.CredentialReference,
                request.Status ?? ErpConnectionStatuses.Configured),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateConnectionAsync(
        Guid connectionId,
        UpdateErpConnectionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectionRepository.UpdateAsync(
            context,
            new UpdateErpConnectionCommand(
                connectionId,
                request.ExpectedVersion,
                request.Name,
                request.ExternalCompanyId,
                request.ExternalCompanyCode,
                request.BaseEndpoint,
                request.ConnectionMode,
                request.CredentialReference,
                request.Status),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> HealthCheckAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var existing = await connectionRepository.GetByIdAsync(context, connectionId, cancellationToken);
        if (existing is null)
        {
            return TypedResults.Problem(
                title: "Connection profile not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        var health = await connectionRepository.CheckHealthAsync(context, connectionId, cancellationToken);
        if (health is null)
        {
            return TypedResults.Problem(
                title: "Health check failed.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.SourceUnavailable));
        }

        return TypedResults.Ok(health);
    }

    private static async Task<IResult> SyncCustomersAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.SyncCustomersAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncProductsAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.SyncProductsAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncOrdersAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.SyncOrdersAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncWarehousesAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await inventoryRepository.SyncWarehousesAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncInventoryAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await inventoryRepository.SyncInventoryAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncPurchasingAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await inventoryRepository.SyncPurchasingAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncProductionAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await productionRepository.SyncProductionAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncDocumentsAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await productionRepository.SyncDocumentsAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> ListSyncRunsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListSyncRunsAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetSyncRunAsync(
        Guid syncRunId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await syncRepository.GetSyncRunAsync(context, syncRunId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Sync run not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> RetrySyncRunAsync(
        Guid syncRunId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        IErpInventoryRepository inventoryRepository,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var existing = await syncRepository.GetSyncRunAsync(context, syncRunId, cancellationToken);
        if (existing is null)
        {
            return ErpEndpointHelpers.MapSyncResult(
                ErpSyncResult.Fail(ErpFailureCodes.NotFound, "Sync run not found."));
        }

        if (existing.Status is not (ErpSyncRunStatuses.Failed or ErpSyncRunStatuses.CompletedWithErrors))
        {
            return ErpEndpointHelpers.MapSyncResult(ErpSyncResult.Fail(
                ErpFailureCodes.ValidationFailed,
                "Only failed or completed-with-errors sync runs can be retried."));
        }

        var result = existing.EntityType switch
        {
            ErpSyncEntityTypes.Customers or ErpSyncEntityTypes.Products or ErpSyncEntityTypes.Orders =>
                await syncRepository.RetrySyncRunAsync(context, syncRunId, cancellationToken),
            ErpSyncEntityTypes.Warehouses => await inventoryRepository.SyncWarehousesAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Inventory => await inventoryRepository.SyncInventoryAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Purchasing => await inventoryRepository.SyncPurchasingAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Production => await productionRepository.SyncProductionAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Documents => await productionRepository.SyncDocumentsAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            _ => ErpSyncResult.Fail(ErpFailureCodes.ValidationFailed, "Unsupported entity type for retry."),
        };

        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> ListCustomersAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListCustomersAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListProductsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListProductsAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListOrdersAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListOrdersAsync(context, connectionProfileId, status, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetOrderAsync(
        Guid orderId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await syncRepository.GetOrderAsync(context, orderId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> GetOrderReadinessAsync(
        Guid orderId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await syncRepository.GetOrderReadinessAsync(context, orderId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> GetOrderMaterialAvailabilityAsync(
        Guid orderId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await inventoryRepository.GetOrderMaterialAvailabilityAsync(context, orderId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ListWarehousesAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await inventoryRepository.ListWarehousesAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetWarehouseAsync(
        Guid warehouseId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await inventoryRepository.GetWarehouseAsync(context, warehouseId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Warehouse not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ListInventoryAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? productExternalId,
        string? warehouseExternalId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await inventoryRepository.ListInventoryBalancesAsync(
            context, connectionProfileId, productExternalId, warehouseExternalId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListInventoryAvailabilityAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? productExternalId,
        string? warehouseExternalId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await inventoryRepository.ListInventoryAvailabilityAsync(
            context, connectionProfileId, productExternalId, warehouseExternalId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListPurchaseOrdersAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await inventoryRepository.ListPurchaseOrdersAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetPurchaseOrderAsync(
        Guid purchaseOrderId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await inventoryRepository.GetPurchaseOrderAsync(context, purchaseOrderId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Purchase order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ListProductionOrdersAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? status,
        string? productExternalId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await productionRepository.ListProductionOrdersAsync(
            context, connectionProfileId, status, productExternalId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetProductionOrderAsync(
        Guid productionOrderId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await productionRepository.GetProductionOrderAsync(context, productionOrderId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Production order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ListDocumentsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? documentType,
        string? relatedEntityType,
        string? relatedExternalEntityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await productionRepository.ListDocumentsAsync(
            context,
            connectionProfileId,
            documentType,
            relatedEntityType,
            relatedExternalEntityId,
            cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetDocumentAsync(
        Guid documentId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpProductionRepository productionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await productionRepository.GetDocumentAsync(context, documentId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Document not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ListWritebacksAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await writebackRepository.ListWritebacksAsync(context, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetWritebackAsync(
        Guid writebackId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await writebackRepository.GetWritebackAsync(context, writebackId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Writeback request not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> CreateWritebackAsync(
        CreateErpWritebackRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.WritebackRequest,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await writebackRepository.CreateWritebackAsync(
            context,
            new CreateErpWritebackCommand(
                request.ConnectionProfileId,
                request.CommandType,
                request.PayloadJson,
                request.IdempotencyKey,
                request.TargetEntityType,
                request.TargetExternalId),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> SubmitWritebackAsync(
        Guid writebackId,
        SubmitErpWritebackRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.WritebackRequest,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await writebackRepository.SubmitWritebackAsync(
            context,
            new SubmitErpWritebackCommand(writebackId, request.ExpectedVersion),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ApproveWritebackAsync(
        Guid writebackId,
        ApproveErpWritebackRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.WritebackApprove,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await writebackRepository.ApproveWritebackAsync(
            context,
            new ApproveErpWritebackCommand(writebackId, request.ExpectedVersion, request.Comment),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RejectWritebackAsync(
        Guid writebackId,
        RejectErpWritebackRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.WritebackApprove,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await writebackRepository.RejectWritebackAsync(
            context,
            new RejectErpWritebackCommand(writebackId, request.ExpectedVersion, request.Comment),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ExecuteWritebackAsync(
        Guid writebackId,
        ExecuteErpWritebackRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpWritebackRepository writebackRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.WritebackExecute,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await writebackRepository.ExecuteWritebackAsync(
            context,
            new ExecuteErpWritebackCommand(writebackId, request.ExpectedVersion),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListSyncReliabilityAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpInventoryRepository inventoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await inventoryRepository.ListSyncReliabilityAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListMappingsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        string? entityType,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await mappingRepository.ListMappingsAsync(context, connectionProfileId, entityType, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetMappingAsync(
        Guid mappingId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await mappingRepository.GetMappingAsync(context, mappingId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Mapping not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> ConfirmMappingAsync(
        Guid mappingId,
        ConfirmErpMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await mappingRepository.ConfirmAsync(
            context,
            new ConfirmErpMappingCommand(mappingId, request.TargetEntityId, request.ExpectedVersion),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> IgnoreMappingAsync(
        Guid mappingId,
        IgnoreErpMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await mappingRepository.IgnoreAsync(
            context,
            new IgnoreErpMappingCommand(mappingId, request.ExpectedVersion),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RemapMappingAsync(
        Guid mappingId,
        ConfirmErpMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await mappingRepository.RemapAsync(
            context,
            new RemapErpMappingCommand(mappingId, request.TargetEntityId, request.ExpectedVersion),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListCustomerReconciliationAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await mappingRepository.ListCustomerReconciliationAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListProductReconciliationAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpMappingRepository mappingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await mappingRepository.ListProductReconciliationAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var summary = await connectionRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static object MapConnection(ErpConnectionSummaryReadModel c) => new
    {
        c.Id,
        c.FacilityId,
        c.Provider,
        c.Name,
        c.ExternalCompanyId,
        c.ExternalCompanyCode,
        c.ConnectionMode,
        c.Status,
        c.LastSuccessfulConnectionAtUtc,
        c.LastFailedConnectionAtUtc,
        c.LastHealthCheckAtUtc,
        c.LastHealthLatencyMs,
        c.LastErrorCode,
        c.LastErrorMessage,
        c.LastSuccessfulSyncAtUtc,
        c.CreatedAtUtc,
        c.UpdatedAtUtc,
        c.Version,
        hasCredentialReference = !string.IsNullOrWhiteSpace(c.CredentialReference),
        providerDisplayName = c.Provider switch
        {
            ErpProviderKeys.Simulator => SimulatorErpAdapter.DisplayName,
            ErpProviderKeys.Logo => LogoErpAdapter.DisplayName,
            _ => c.Provider,
        },
    };
}

internal sealed record CreateErpConnectionRequest(
    Guid FacilityId,
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string? Status);

internal sealed record UpdateErpConnectionRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);

internal sealed record ConfirmErpMappingRequest(
    Guid FacilityId,
    Guid TargetEntityId,
    uint ExpectedVersion);

internal sealed record IgnoreErpMappingRequest(
    Guid FacilityId,
    uint ExpectedVersion);

internal sealed record CreateErpWritebackRequest(
    Guid FacilityId,
    Guid ConnectionProfileId,
    string CommandType,
    string PayloadJson,
    string IdempotencyKey,
    string? TargetEntityType,
    string? TargetExternalId);

internal sealed record SubmitErpWritebackRequest(
    Guid FacilityId,
    uint ExpectedVersion);

internal sealed record ApproveErpWritebackRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string? Comment);

internal sealed record RejectErpWritebackRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string? Comment);

internal sealed record ExecuteErpWritebackRequest(
    Guid FacilityId,
    uint ExpectedVersion);
