using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Erp;

public interface IErpMappingRepository
{
    Task<IReadOnlyList<ErpMappingReadModel>> ListMappingsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? entityType = null,
        CancellationToken cancellationToken = default);

    Task<ErpMappingReadModel?> GetMappingAsync(
        ErpOperationContext context,
        Guid mappingId,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> ConfirmAsync(
        ErpOperationContext context,
        ConfirmErpMappingCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> IgnoreAsync(
        ErpOperationContext context,
        IgnoreErpMappingCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> RemapAsync(
        ErpOperationContext context,
        RemapErpMappingCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpCustomerReconciliationReadModel>> ListCustomerReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpProductReconciliationReadModel>> ListProductReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);
}

public sealed class ErpMappingRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IErpMappingRepository
{
    public Task<IReadOnlyList<ErpMappingReadModel>> ListMappingsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? entityType = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(m => m.ConnectionProfileId == connectionProfileId.Value);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                var normalized = entityType.Trim().ToLowerInvariant();
                query = query.Where(m => m.EntityType == normalized);
            }

            var rows = await query
                .OrderBy(m => m.EntityType)
                .ThenBy(m => m.ExternalId)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpMappingReadModel>)rows.Select(MapMapping).ToList();
        }, cancellationToken);

    public Task<ErpMappingReadModel?> GetMappingAsync(
        ErpOperationContext context,
        Guid mappingId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.ErpEntityMappings.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == mappingId,
                    ct);

            return row is null ? null : MapMapping(row);
        }, cancellationToken);

    public Task<ErpWriteResult> ConfirmAsync(
        ErpOperationContext context,
        ConfirmErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (command.TargetEntityId == Guid.Empty)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Target entity id is required.");
                }

                var mapping = await dbContext.ErpEntityMappings
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == command.MappingId,
                        ct);

                if (mapping is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Mapping not found.");
                }

                if (mapping.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The mapping was modified (have {mapping.Version}, expected {command.ExpectedVersion}).");
                }

                if (mapping.Status == ErpMappingStatuses.Mapped
                    && mapping.InternalEntityId.HasValue
                    && mapping.InternalEntityId.Value != command.TargetEntityId)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        "Mapping already points to a different internal entity. Use remap.");
                }

                var conflict = await HasConflictingMappedExternalAsync(
                    context,
                    mapping,
                    command.TargetEntityId,
                    ct);
                if (conflict)
                {
                    mapping.Status = ErpMappingStatuses.Conflict;
                    mapping.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    mapping.Version = command.ExpectedVersion + 1;
                    await dbContext.SaveChangesAsync(ct);
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        "Same external record cannot map to conflicting internal ids.");
                }

                var now = DateTimeOffset.UtcNow;
                mapping.InternalEntityId = command.TargetEntityId;
                mapping.Status = ErpMappingStatuses.Mapped;
                mapping.SuggestedInternalEntityId = null;
                mapping.SuggestedReason = null;
                mapping.UpdatedAtUtc = now;
                mapping.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.MappingConfirmed,
                    ErpAuditEventTypes.MappingConfirmed,
                    "erp.mapping",
                    mapping.Id,
                    now,
                    new
                    {
                        mapping.Id,
                        mapping.EntityType,
                        mapping.ExternalId,
                        targetEntityId = command.TargetEntityId,
                        version = mapping.Version,
                    });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-mapping",
                    mapping.Id,
                    mapping.Version,
                    ErpOutboxMessageTypes.MappingConfirmed,
                    now,
                    new
                    {
                        mappingId = mapping.Id.ToString(),
                        entityType = mapping.EntityType,
                        externalId = mapping.ExternalId,
                        targetEntityId = command.TargetEntityId.ToString(),
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(mapping.Id, mapping.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> IgnoreAsync(
        ErpOperationContext context,
        IgnoreErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var mapping = await dbContext.ErpEntityMappings
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == command.MappingId,
                        ct);

                if (mapping is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Mapping not found.");
                }

                if (mapping.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The mapping was modified (have {mapping.Version}, expected {command.ExpectedVersion}).");
                }

                var now = DateTimeOffset.UtcNow;
                mapping.Status = ErpMappingStatuses.Ignored;
                mapping.UpdatedAtUtc = now;
                mapping.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.MappingIgnored,
                    ErpAuditEventTypes.MappingIgnored,
                    "erp.mapping",
                    mapping.Id,
                    now,
                    new { mapping.Id, mapping.EntityType, mapping.ExternalId, version = mapping.Version });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(mapping.Id, mapping.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> RemapAsync(
        ErpOperationContext context,
        RemapErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (command.TargetEntityId == Guid.Empty)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, "Target entity id is required.");
                }

                var mapping = await dbContext.ErpEntityMappings
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == command.MappingId,
                        ct);

                if (mapping is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Mapping not found.");
                }

                if (mapping.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The mapping was modified (have {mapping.Version}, expected {command.ExpectedVersion}).");
                }

                var conflict = await HasConflictingMappedExternalAsync(
                    context,
                    mapping,
                    command.TargetEntityId,
                    ct);
                if (conflict)
                {
                    mapping.Status = ErpMappingStatuses.Conflict;
                    mapping.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    mapping.Version = command.ExpectedVersion + 1;
                    await dbContext.SaveChangesAsync(ct);
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        "Same external record cannot map to conflicting internal ids.");
                }

                var now = DateTimeOffset.UtcNow;
                mapping.InternalEntityId = command.TargetEntityId;
                mapping.Status = ErpMappingStatuses.Mapped;
                mapping.SuggestedInternalEntityId = null;
                mapping.SuggestedReason = null;
                mapping.UpdatedAtUtc = now;
                mapping.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.MappingConfirmed,
                    ErpAuditEventTypes.MappingConfirmed,
                    "erp.mapping",
                    mapping.Id,
                    now,
                    new
                    {
                        mapping.Id,
                        mapping.EntityType,
                        mapping.ExternalId,
                        targetEntityId = command.TargetEntityId,
                        remapped = true,
                        version = mapping.Version,
                    });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-mapping",
                    mapping.Id,
                    mapping.Version,
                    ErpOutboxMessageTypes.MappingConfirmed,
                    now,
                    new
                    {
                        mappingId = mapping.Id.ToString(),
                        entityType = mapping.EntityType,
                        externalId = mapping.ExternalId,
                        targetEntityId = command.TargetEntityId.ToString(),
                        remapped = true,
                    });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(mapping.Id, mapping.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<ErpCustomerReconciliationReadModel>> ListCustomerReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var customersQuery = dbContext.CanonicalErpCustomers.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.ConnectionProfileId == connectionProfileId.Value);
            }

            var customers = await customersQuery.OrderBy(c => c.ExternalCode ?? c.ExternalId).ToListAsync(ct);
            var mappings = await dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.EntityType == ErpSyncEntityTypes.Customers)
                .ToListAsync(ct);

            ApplyDeterministicSuggestions(mappings, customers.Select(c => (c.ConnectionProfileId, c.ExternalId, c.ExternalCode)));

            return (IReadOnlyList<ErpCustomerReconciliationReadModel>)customers.Select(c =>
            {
                var mapping = mappings.FirstOrDefault(m =>
                    m.ConnectionProfileId == c.ConnectionProfileId
                    && m.ExternalId == c.ExternalId);

                var (suggestedId, suggestedReason, displayStatus) = ResolveSuggestion(mapping, mappings, c.ConnectionProfileId, c.ExternalId, c.ExternalCode);

                return new ErpCustomerReconciliationReadModel(
                    c.Id,
                    c.ConnectionProfileId,
                    c.Provider,
                    c.ExternalId,
                    c.ExternalCode,
                    c.Name,
                    c.TaxNumber,
                    displayStatus,
                    mapping?.Id,
                    mapping?.InternalEntityId,
                    suggestedId,
                    suggestedReason,
                    c.SourceVersion,
                    c.SourceUpdatedAtUtc,
                    c.UpdatedAtUtc,
                    mapping?.Version);
            }).ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<ErpProductReconciliationReadModel>> ListProductReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var productsQuery = dbContext.CanonicalErpProducts.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                productsQuery = productsQuery.Where(c => c.ConnectionProfileId == connectionProfileId.Value);
            }

            var products = await productsQuery.OrderBy(c => c.ExternalCode ?? c.ExternalId).ToListAsync(ct);
            var mappings = await dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.EntityType == ErpSyncEntityTypes.Products)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpProductReconciliationReadModel>)products.Select(p =>
            {
                var mapping = mappings.FirstOrDefault(m =>
                    m.ConnectionProfileId == p.ConnectionProfileId
                    && m.ExternalId == p.ExternalId);

                var (suggestedId, suggestedReason, displayStatus) = ResolveSuggestion(
                    mapping,
                    mappings,
                    p.ConnectionProfileId,
                    p.ExternalId,
                    p.ExternalCode);

                return new ErpProductReconciliationReadModel(
                    p.Id,
                    p.ConnectionProfileId,
                    p.Provider,
                    p.ExternalId,
                    p.ExternalCode,
                    p.Name,
                    p.Unit,
                    p.Barcode,
                    displayStatus,
                    mapping?.Id,
                    mapping?.InternalEntityId,
                    suggestedId,
                    suggestedReason,
                    p.SourceVersion,
                    p.SourceUpdatedAtUtc,
                    p.UpdatedAtUtc,
                    mapping?.Version);
            }).ToList();
        }, cancellationToken);

    private async Task<bool> HasConflictingMappedExternalAsync(
        ErpOperationContext context,
        ErpEntityMapping mapping,
        Guid targetEntityId,
        CancellationToken cancellationToken)
    {
        // Conflict: same external identity already mapped to a different internal id
        // (should be unique by external key, so this checks remapping inconsistency across duplicates if any).
        var other = await dbContext.ErpEntityMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.ConnectionProfileId == mapping.ConnectionProfileId
                    && m.EntityType == mapping.EntityType
                    && m.ExternalId == mapping.ExternalId
                    && m.Id != mapping.Id
                    && m.Status == ErpMappingStatuses.Mapped
                    && m.InternalEntityId.HasValue
                    && m.InternalEntityId.Value != targetEntityId,
                cancellationToken);

        return other is not null;
    }

    /// <summary>
    /// Deterministic suggestion only: exact external code already linked on another connection.
    /// Never auto-confirms. No AI / fuzzy matching.
    /// </summary>
    private static void ApplyDeterministicSuggestions(
        IReadOnlyList<ErpEntityMapping> mappings,
        IEnumerable<(Guid ConnectionProfileId, string ExternalId, string? ExternalCode)> entities)
    {
        _ = mappings;
        _ = entities;
        // Suggestions are computed read-time in ResolveSuggestion; no mutation.
    }

    private static (Guid? SuggestedId, string? SuggestedReason, string Status) ResolveSuggestion(
        ErpEntityMapping? mapping,
        IReadOnlyList<ErpEntityMapping> allMappings,
        Guid connectionProfileId,
        string externalId,
        string? externalCode)
    {
        if (mapping is not null
            && mapping.Status is ErpMappingStatuses.Mapped or ErpMappingStatuses.Ignored or ErpMappingStatuses.Conflict)
        {
            return (mapping.SuggestedInternalEntityId, mapping.SuggestedReason, mapping.Status);
        }

        if (mapping?.SuggestedInternalEntityId is not null)
        {
            return (
                mapping.SuggestedInternalEntityId,
                mapping.SuggestedReason ?? "exact-external-code",
                mapping.Status == ErpMappingStatuses.Unmapped ? ErpMappingStatuses.Suggested : mapping.Status);
        }

        Guid? suggested = null;
        string? reason = null;

        if (!string.IsNullOrWhiteSpace(externalCode))
        {
            var linked = allMappings.FirstOrDefault(m =>
                m.Status == ErpMappingStatuses.Mapped
                && m.InternalEntityId.HasValue
                && m.ConnectionProfileId != connectionProfileId
                && string.Equals(m.ExternalId, externalCode, StringComparison.OrdinalIgnoreCase));

            if (linked?.InternalEntityId is Guid linkedId)
            {
                suggested = linkedId;
                reason = "exact-external-code";
            }
        }

        if (suggested is null)
        {
            var linkedByExternalId = allMappings.FirstOrDefault(m =>
                m.Status == ErpMappingStatuses.Mapped
                && m.InternalEntityId.HasValue
                && m.ConnectionProfileId != connectionProfileId
                && string.Equals(m.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));

            if (linkedByExternalId?.InternalEntityId is Guid linkedId)
            {
                suggested = linkedId;
                reason = "exact-external-id";
            }
        }

        var status = mapping?.Status ?? ErpMappingStatuses.Unmapped;
        if (suggested.HasValue && status == ErpMappingStatuses.Unmapped)
        {
            status = ErpMappingStatuses.Suggested;
        }

        return (suggested, reason, status);
    }

    private static ErpMappingReadModel MapMapping(ErpEntityMapping m) =>
        new(
            m.Id,
            m.ConnectionProfileId,
            m.EntityType,
            m.ExternalId,
            m.CanonicalEntityId,
            m.InternalEntityId,
            m.SuggestedInternalEntityId,
            m.SuggestedReason,
            m.Status,
            m.CreatedAtUtc,
            m.UpdatedAtUtc,
            m.Version);
}

public sealed class UnavailableErpMappingRepository : IErpMappingRepository
{
    public Task<IReadOnlyList<ErpMappingReadModel>> ListMappingsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? entityType = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpMappingReadModel>>([]);

    public Task<ErpMappingReadModel?> GetMappingAsync(
        ErpOperationContext context,
        Guid mappingId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpMappingReadModel?>(null);

    public Task<ErpWriteResult> ConfirmAsync(
        ErpOperationContext context,
        ConfirmErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpWriteResult> IgnoreAsync(
        ErpOperationContext context,
        IgnoreErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpWriteResult> RemapAsync(
        ErpOperationContext context,
        RemapErpMappingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<IReadOnlyList<ErpCustomerReconciliationReadModel>> ListCustomerReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpCustomerReconciliationReadModel>>([]);

    public Task<IReadOnlyList<ErpProductReconciliationReadModel>> ListProductReconciliationAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpProductReconciliationReadModel>>([]);
}
