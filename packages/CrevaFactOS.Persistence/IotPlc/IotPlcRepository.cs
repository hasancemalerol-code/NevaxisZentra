using CrevaFactOS.Persistence.Development;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.IotPlc;

public sealed class IotPlcRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IIotPlcRepository
{
    public Task<IotPlcWorkspaceReadModel> GetWorkspaceAsync(
        IotPlcOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var activeEndpoints = await dbContext.IotPlcEndpoints
                    .CountAsync(
                        e => e.TenantId == context.TenantId
                             && e.FacilityId == context.FacilityId
                             && e.EndpointStatus == IotPlcEndpointStatuses.Active,
                        ct);

                var activeSignals = await dbContext.IotPlcSignals
                    .CountAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.IsEnabled,
                        ct);

                var recentCutoff = DateTimeOffset.UtcNow.AddDays(-7);
                var recentObservations = await dbContext.IotPlcSignalObservations
                    .CountAsync(
                        o => o.TenantId == context.TenantId
                             && o.FacilityId == context.FacilityId
                             && o.ReceivedAtUtc >= recentCutoff,
                        ct);

                var hasPilot = await dbContext.IotPlcEndpoints
                    .AnyAsync(
                        e => e.TenantId == context.TenantId
                             && e.FacilityId == context.FacilityId
                             && e.Id == DevelopmentPilotIds.IotPlcPilotEndpointOpcId,
                        ct);

                return new IotPlcWorkspaceReadModel(
                    activeEndpoints,
                    activeSignals,
                    recentObservations,
                    IotPlcConstants.DevelopmentPilotProvenance,
                    hasPilot);
            },
            cancellationToken);

    public Task<IotPlcEndpointListReadModel> ListEndpointsAsync(
        IotPlcOperationContext context,
        IotPlcEndpointListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = Math.Max(1, filters.Page);
                var pageSize = Math.Clamp(filters.PageSize, 1, 100);
                var query = dbContext.IotPlcEndpoints
                    .AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.ProtocolType))
                {
                    query = query.Where(e => e.ProtocolType == filters.ProtocolType.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.EndpointStatus))
                {
                    query = query.Where(e => e.EndpointStatus == filters.EndpointStatus.Trim());
                }

                var totalCount = await query.CountAsync(ct);
                var endpoints = await query
                    .OrderBy(e => e.EndpointCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var signalCounts = await dbContext.IotPlcSignals
                    .Where(s => endpoints.Select(e => e.Id).Contains(s.EndpointId))
                    .GroupBy(s => s.EndpointId)
                    .Select(g => new { EndpointId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EndpointId, x => x.Count, ct);

                var items = endpoints
                    .Select(e => MapEndpoint(e, signalCounts.GetValueOrDefault(e.Id)))
                    .ToList();

                return new IotPlcEndpointListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<IotPlcEndpointDetailReadModel?> GetEndpointByReferenceAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var endpoint = await FindEndpointAsync(context, reference, ct);
                if (endpoint is null)
                {
                    return null;
                }

                var signals = await dbContext.IotPlcSignals
                    .AsNoTracking()
                    .Where(s => s.EndpointId == endpoint.Id)
                    .OrderBy(s => s.SignalCode)
                    .ToListAsync(ct);

                return new IotPlcEndpointDetailReadModel(
                    MapEndpoint(endpoint, signals.Count),
                    endpoint.ConnectionProfileJson,
                    signals.Select(MapSignal).ToList());
            },
            cancellationToken);

    public Task<IotPlcSignalListReadModel> ListSignalsAsync(
        IotPlcOperationContext context,
        IotPlcSignalListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = Math.Max(1, filters.Page);
                var pageSize = Math.Clamp(filters.PageSize, 1, 100);
                var query = dbContext.IotPlcSignals
                    .AsNoTracking()
                    .Include(s => s.Endpoint)
                    .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.EndpointReference))
                {
                    var endpointRef = filters.EndpointReference.Trim();
                    query = query.Where(s => s.Endpoint != null && s.Endpoint.EndpointCode == endpointRef);
                }

                if (!string.IsNullOrWhiteSpace(filters.MappedAssetType))
                {
                    query = query.Where(s => s.MappedAssetType == filters.MappedAssetType.Trim());
                }

                if (filters.MappedCanonicalAssetId is not null)
                {
                    query = query.Where(s => s.MappedCanonicalAssetId == filters.MappedCanonicalAssetId);
                }

                if (filters.IsEnabled is not null)
                {
                    query = query.Where(s => s.IsEnabled == filters.IsEnabled.Value);
                }

                var totalCount = await query.CountAsync(ct);
                var signals = await query
                    .OrderBy(s => s.SignalCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new IotPlcSignalListReadModel(
                    signals.Select(MapSignal).ToList(),
                    totalCount,
                    page,
                    pageSize);
            },
            cancellationToken);

    public Task<IotPlcSignalObservationListReadModel> ListSignalObservationsAsync(
        IotPlcOperationContext context,
        string signalReference,
        IotPlcSignalObservationListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var signal = await FindSignalAsync(context, signalReference, ct);
                if (signal is null)
                {
                    return new IotPlcSignalObservationListReadModel([], 0, 1, filters.PageSize);
                }

                var page = Math.Max(1, filters.Page);
                var pageSize = Math.Clamp(filters.PageSize, 1, 100);
                var query = dbContext.IotPlcSignalObservations
                    .AsNoTracking()
                    .Include(o => o.Signal)
                    .Where(o => o.SignalDefinitionId == signal.Id);

                if (filters.ObservedFromUtc is not null)
                {
                    query = query.Where(o => o.ObservedAtUtc >= filters.ObservedFromUtc.Value);
                }

                if (filters.ObservedToUtc is not null)
                {
                    query = query.Where(o => o.ObservedAtUtc <= filters.ObservedToUtc.Value);
                }

                var totalCount = await query.CountAsync(ct);
                var observations = await query
                    .OrderByDescending(o => o.ObservedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new IotPlcSignalObservationListReadModel(
                    observations.Select(MapObservation).ToList(),
                    totalCount,
                    page,
                    pageSize);
            },
            cancellationToken);

    public Task<IotPlcConnectionHealthListReadModel> ListConnectionHealthAsync(
        IotPlcOperationContext context,
        string endpointReference,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var endpoint = await FindEndpointAsync(context, endpointReference, ct);
                if (endpoint is null)
                {
                    return new IotPlcConnectionHealthListReadModel([], 0, 1, pageSize);
                }

                var normalizedPage = Math.Max(1, page);
                var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
                var query = dbContext.IotPlcConnectionHealthObservations
                    .AsNoTracking()
                    .Where(h => h.EndpointId == endpoint.Id);

                var totalCount = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(h => h.ObservedAtUtc)
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToListAsync(ct);

                return new IotPlcConnectionHealthListReadModel(
                    items.Select(MapHealth).ToList(),
                    totalCount,
                    normalizedPage,
                    normalizedPageSize);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> CreateEndpointAsync(
        IotPlcOperationContext context,
        CreateIotPlcEndpointCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEndpointCreate(command);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var exists = await dbContext.IotPlcEndpoints.AnyAsync(
                    e => e.TenantId == context.TenantId
                         && e.FacilityId == context.FacilityId
                         && e.EndpointCode == command.EndpointCode.Trim(),
                    ct);
                if (exists)
                {
                    return IotPlcWriteResult.Failure(
                        IotPlcFailureCodes.ValidationFailed,
                        "Endpoint code already exists for this facility.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var endpoint = new IotPlcEndpoint
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    EndpointCode = command.EndpointCode.Trim(),
                    DisplayName = command.DisplayName.Trim(),
                    ProtocolType = command.ProtocolType.Trim(),
                    ConnectionDirection = command.ConnectionDirection.Trim(),
                    EndpointStatus = command.EndpointStatus.Trim(),
                    ConfigurationProvenance = command.ConfigurationProvenance.Trim(),
                    ExternalSecretReference = TrimOrNull(command.ExternalSecretReference),
                    ConnectionProfileJson = string.IsNullOrWhiteSpace(command.ConnectionProfileJson) ? "{}" : command.ConnectionProfileJson.Trim(),
                    LastObservedConnectionState = null,
                    LastObservedAtUtc = null,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.IotPlcEndpoints.Add(endpoint);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(endpoint.Id, endpoint.Version);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> UpdateEndpointAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcEndpointCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var endpoint = await FindEndpointAsync(context, reference, ct, tracked: true);
                if (endpoint is null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.EndpointNotFound, "Endpoint not found.");
                }

                if (endpoint.Version != command.ExpectedVersion)
                {
                    return IotPlcWriteResult.Failure(
                        IotPlcFailureCodes.ConcurrencyConflict,
                        "Endpoint was modified by another request.");
                }

                var validationError = ValidateEndpointUpdate(command);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                endpoint.DisplayName = command.DisplayName.Trim();
                endpoint.ProtocolType = command.ProtocolType.Trim();
                endpoint.ConnectionDirection = command.ConnectionDirection.Trim();
                endpoint.EndpointStatus = command.EndpointStatus.Trim();
                endpoint.ConfigurationProvenance = command.ConfigurationProvenance.Trim();
                endpoint.ExternalSecretReference = TrimOrNull(command.ExternalSecretReference);
                endpoint.ConnectionProfileJson = string.IsNullOrWhiteSpace(command.ConnectionProfileJson) ? "{}" : command.ConnectionProfileJson.Trim();
                endpoint.UpdatedAtUtc = now;
                endpoint.UpdatedBy = actor;
                endpoint.Version++;

                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(endpoint.Id, endpoint.Version);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> CreateSignalAsync(
        IotPlcOperationContext context,
        CreateIotPlcSignalCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateSignalCreate(command);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var endpoint = await FindEndpointAsync(context, command.EndpointReference, ct);
                if (endpoint is null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.EndpointNotFound, "Endpoint not found.");
                }

                var scopeError = await ValidateAssetMappingAsync(context, command.MappedAssetType, command.MappedCanonicalAssetId, ct);
                if (scopeError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.MachineScopeDenied, scopeError);
                }

                var exists = await dbContext.IotPlcSignals.AnyAsync(
                    s => s.EndpointId == endpoint.Id && s.SignalCode == command.SignalCode.Trim(),
                    ct);
                if (exists)
                {
                    return IotPlcWriteResult.Failure(
                        IotPlcFailureCodes.ValidationFailed,
                        "Signal code already exists for this endpoint.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var signal = new IotPlcSignal
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    EndpointId = endpoint.Id,
                    SignalCode = command.SignalCode.Trim(),
                    DisplayName = command.DisplayName.Trim(),
                    SourceAddress = command.SourceAddress.Trim(),
                    DataType = command.DataType.Trim(),
                    EngineeringUnit = TrimOrNull(command.EngineeringUnit),
                    SemanticType = command.SemanticType.Trim(),
                    MappedAssetType = command.MappedAssetType.Trim(),
                    MappedCanonicalAssetId = command.MappedCanonicalAssetId,
                    IsEnabled = command.IsEnabled,
                    SamplingExpectation = TrimOrNull(command.SamplingExpectation),
                    SourceProvenance = command.SourceProvenance.Trim(),
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.IotPlcSignals.Add(signal);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(signal.Id, signal.Version);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> UpdateSignalAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcSignalCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var signal = await FindSignalAsync(context, reference, ct, tracked: true);
                if (signal is null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.SignalNotFound, "Signal not found.");
                }

                if (signal.Version != command.ExpectedVersion)
                {
                    return IotPlcWriteResult.Failure(
                        IotPlcFailureCodes.ConcurrencyConflict,
                        "Signal was modified by another request.");
                }

                var validationError = ValidateSignalUpdate(command);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var scopeError = await ValidateAssetMappingAsync(context, command.MappedAssetType, command.MappedCanonicalAssetId, ct);
                if (scopeError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.MachineScopeDenied, scopeError);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                signal.DisplayName = command.DisplayName.Trim();
                signal.SourceAddress = command.SourceAddress.Trim();
                signal.DataType = command.DataType.Trim();
                signal.EngineeringUnit = TrimOrNull(command.EngineeringUnit);
                signal.SemanticType = command.SemanticType.Trim();
                signal.MappedAssetType = command.MappedAssetType.Trim();
                signal.MappedCanonicalAssetId = command.MappedCanonicalAssetId;
                signal.IsEnabled = command.IsEnabled;
                signal.SamplingExpectation = TrimOrNull(command.SamplingExpectation);
                signal.SourceProvenance = command.SourceProvenance.Trim();
                signal.UpdatedAtUtc = now;
                signal.UpdatedBy = actor;
                signal.Version++;

                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(signal.Id, signal.Version);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> IngestObservationAsync(
        IotPlcOperationContext context,
        IngestIotPlcObservationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var signal = await FindSignalAsync(context, command.SignalReference, ct);
                if (signal is null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.SignalNotFound, "Signal not found.");
                }

                var validationError = ValidateObservation(command, signal.DataType);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = IotPlcIdempotencyFingerprint.ComputeObservationFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existingByKey = await dbContext.IotPlcSignalObservations
                        .FirstOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                 && o.IdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existingByKey is not null)
                    {
                        if (string.IsNullOrEmpty(existingByKey.PayloadFingerprint))
                        {
                            return IotPlcWriteResult.Failure(
                                IotPlcFailureCodes.IdempotencyLegacyUnverifiable,
                                "Legacy observation cannot be verified for idempotent replay.");
                        }

                        if (!string.Equals(existingByKey.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return IotPlcWriteResult.Failure(
                                IotPlcFailureCodes.IdempotencyConflict,
                                "Idempotency key was already used with a different payload.");
                        }

                        return IotPlcWriteResult.Success(existingByKey.Id, idempotentReplay: true);
                    }
                }

                if (!string.IsNullOrWhiteSpace(command.SourceSequenceReference))
                {
                    var existingBySource = await dbContext.IotPlcSignalObservations
                        .FirstOrDefaultAsync(
                            o => o.SignalDefinitionId == signal.Id
                                 && o.SourceSequenceReference == command.SourceSequenceReference!.Trim(),
                            ct);

                    if (existingBySource is not null)
                    {
                        return IotPlcWriteResult.Failure(
                            IotPlcFailureCodes.SourceReferenceConflict,
                            "Source sequence reference already exists for this signal.");
                    }
                }

                var duplicateTimestamp = await dbContext.IotPlcSignalObservations
                    .AnyAsync(
                        o => o.SignalDefinitionId == signal.Id
                             && o.ObservedAtUtc == command.ObservedAtUtc,
                        ct);
                if (duplicateTimestamp)
                {
                    return IotPlcWriteResult.Failure(
                        IotPlcFailureCodes.DuplicateObservationTimestamp,
                        "An observation already exists for this signal at the same observed timestamp.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var observation = new IotPlcSignalObservation
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    EndpointId = signal.EndpointId,
                    SignalDefinitionId = signal.Id,
                    MappedAssetType = signal.MappedAssetType,
                    MappedCanonicalAssetId = signal.MappedCanonicalAssetId,
                    ObservedAtUtc = command.ObservedAtUtc,
                    ReceivedAtUtc = now,
                    BooleanValue = command.BooleanValue,
                    IntegerValue = command.IntegerValue,
                    DecimalValue = command.DecimalValue,
                    TextValue = TrimOrNull(command.TextValue),
                    TimestampValue = command.TimestampValue,
                    QualityState = NormalizeQuality(command.QualityState),
                    EvidenceState = NormalizeEvidence(command.EvidenceState),
                    SourceSequenceReference = TrimOrNull(command.SourceSequenceReference),
                    IdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    PayloadFingerprint = fingerprint,
                    SourceProvenance = command.SourceProvenance.Trim(),
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.IotPlcSignalObservations.Add(observation);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(observation.Id);
            },
            cancellationToken);

    public Task<IotPlcWriteResult> IngestConnectionHealthAsync(
        IotPlcOperationContext context,
        IngestIotPlcConnectionHealthCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var endpoint = await FindEndpointAsync(context, command.EndpointReference, ct);
                if (endpoint is null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.EndpointNotFound, "Endpoint not found.");
                }

                var validationError = ValidateConnectionHealth(command);
                if (validationError is not null)
                {
                    return IotPlcWriteResult.Failure(IotPlcFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = IotPlcIdempotencyFingerprint.ComputeHealthFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.IotPlcConnectionHealthObservations
                        .FirstOrDefaultAsync(
                            h => h.TenantId == context.TenantId
                                 && h.IdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        if (string.IsNullOrEmpty(existing.PayloadFingerprint))
                        {
                            return IotPlcWriteResult.Failure(
                                IotPlcFailureCodes.IdempotencyLegacyUnverifiable,
                                "Legacy health observation cannot be verified for idempotent replay.");
                        }

                        if (!string.Equals(existing.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return IotPlcWriteResult.Failure(
                                IotPlcFailureCodes.IdempotencyConflict,
                                "Idempotency key was already used with a different payload.");
                        }

                        return IotPlcWriteResult.Success(existing.Id, idempotentReplay: true);
                    }
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var health = new IotPlcConnectionHealthObservation
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    EndpointId = endpoint.Id,
                    ObservedAtUtc = command.ObservedAtUtc,
                    ReceivedAtUtc = now,
                    HealthState = command.HealthState.Trim(),
                    Source = command.Source.Trim(),
                    EvidenceState = NormalizeEvidence(command.EvidenceState),
                    ReasonCode = command.ReasonCode.Trim(),
                    LatencyMs = command.LatencyMs,
                    LastSuccessfulObservationAtUtc = command.LastSuccessfulObservationAtUtc,
                    IdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    PayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.IotPlcConnectionHealthObservations.Add(health);
                await dbContext.SaveChangesAsync(ct);
                return IotPlcWriteResult.Success(health.Id);
            },
            cancellationToken);

    private async Task<IotPlcEndpoint?> FindEndpointAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken ct,
        bool tracked = false)
    {
        var query = tracked
            ? dbContext.IotPlcEndpoints.AsQueryable()
            : dbContext.IotPlcEndpoints.AsNoTracking();

        if (Guid.TryParse(reference, out var endpointId))
        {
            return await query.FirstOrDefaultAsync(
                e => e.TenantId == context.TenantId
                     && e.FacilityId == context.FacilityId
                     && e.Id == endpointId,
                ct);
        }

        return await query.FirstOrDefaultAsync(
            e => e.TenantId == context.TenantId
                 && e.FacilityId == context.FacilityId
                 && e.EndpointCode == reference.Trim(),
            ct);
    }

    private async Task<IotPlcSignal?> FindSignalAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken ct,
        bool tracked = false)
    {
        var query = tracked
            ? dbContext.IotPlcSignals.Include(s => s.Endpoint).AsQueryable()
            : dbContext.IotPlcSignals.Include(s => s.Endpoint).AsNoTracking();

        if (Guid.TryParse(reference, out var signalId))
        {
            return await query.FirstOrDefaultAsync(
                s => s.TenantId == context.TenantId
                     && s.FacilityId == context.FacilityId
                     && s.Id == signalId,
                ct);
        }

        return await query.FirstOrDefaultAsync(
            s => s.TenantId == context.TenantId
                 && s.FacilityId == context.FacilityId
                 && s.SignalCode == reference.Trim(),
            ct);
    }

    private async Task<string?> ValidateAssetMappingAsync(
        IotPlcOperationContext context,
        string mappedAssetType,
        Guid? mappedCanonicalAssetId,
        CancellationToken ct)
    {
        var assetType = mappedAssetType.Trim();
        if (assetType == IotPlcAssetTypes.Unmapped)
        {
            return mappedCanonicalAssetId is not null
                ? "Unmapped signals cannot reference a canonical asset."
                : null;
        }

        if (mappedCanonicalAssetId is null)
        {
            return "Mapped signals require a canonical asset identifier.";
        }

        if (assetType == IotPlcAssetTypes.Machine)
        {
            var machine = await dbContext.MachineRegistries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == mappedCanonicalAssetId.Value
                         && m.TenantId == context.TenantId
                         && m.FacilityId == context.FacilityId,
                    ct);
            if (machine is null)
            {
                return "Mapped machine must belong to the same tenant and facility.";
            }
        }
        else if (assetType == IotPlcAssetTypes.AuxiliaryEquipment)
        {
            var energyPoint = await dbContext.EnergyMeasurementPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == mappedCanonicalAssetId.Value
                         && p.TenantId == context.TenantId
                         && p.FacilityId == context.FacilityId
                         && p.MeasurementScope == Energy.EnergyMeasurementScopes.AuxiliaryEquipment,
                    ct);
            if (energyPoint is null)
            {
                return "Mapped auxiliary equipment must belong to the same tenant and facility.";
            }
        }

        return null;
    }

    private static IotPlcEndpointReadModel MapEndpoint(IotPlcEndpoint endpoint, int signalCount) =>
        new(
            endpoint.Id,
            endpoint.EndpointCode,
            endpoint.DisplayName,
            endpoint.ProtocolType,
            endpoint.ConnectionDirection,
            endpoint.EndpointStatus,
            endpoint.ConfigurationProvenance,
            endpoint.LastObservedConnectionState,
            endpoint.LastObservedAtUtc,
            endpoint.Version,
            signalCount,
            endpoint.ConfigurationProvenance.Contains("Deterministic local Development", StringComparison.Ordinal));

    private static IotPlcSignalReadModel MapSignal(IotPlcSignal signal) =>
        new(
            signal.Id,
            signal.EndpointId,
            signal.Endpoint?.EndpointCode ?? string.Empty,
            signal.SignalCode,
            signal.DisplayName,
            signal.SourceAddress,
            signal.DataType,
            signal.EngineeringUnit,
            signal.SemanticType,
            signal.MappedAssetType,
            signal.MappedCanonicalAssetId,
            signal.IsEnabled,
            signal.SamplingExpectation,
            signal.SourceProvenance,
            signal.Version,
            signal.SourceProvenance.Contains("Deterministic local Development", StringComparison.Ordinal));

    private static IotPlcSignalObservationReadModel MapObservation(IotPlcSignalObservation observation) =>
        new(
            observation.Id,
            observation.SignalDefinitionId,
            observation.Signal?.SignalCode ?? string.Empty,
            observation.MappedAssetType,
            observation.MappedCanonicalAssetId,
            observation.ObservedAtUtc,
            observation.ReceivedAtUtc,
            ExtractValue(observation),
            observation.QualityState,
            observation.EvidenceState,
            observation.SourceSequenceReference,
            observation.SourceProvenance);

    private static object? ExtractValue(IotPlcSignalObservation observation)
    {
        if (observation.BooleanValue.HasValue) return observation.BooleanValue.Value;
        if (observation.IntegerValue.HasValue) return observation.IntegerValue.Value;
        if (observation.DecimalValue.HasValue) return observation.DecimalValue.Value;
        if (observation.TextValue is not null) return observation.TextValue;
        if (observation.TimestampValue.HasValue) return observation.TimestampValue.Value;
        return null;
    }

    private static IotPlcConnectionHealthReadModel MapHealth(IotPlcConnectionHealthObservation health) =>
        new(
            health.Id,
            health.EndpointId,
            health.ObservedAtUtc,
            health.ReceivedAtUtc,
            health.HealthState,
            health.Source,
            health.EvidenceState,
            health.ReasonCode,
            health.LatencyMs,
            health.LastSuccessfulObservationAtUtc);

    private static string? ValidateEndpointCreate(CreateIotPlcEndpointCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EndpointCode)
            || string.IsNullOrWhiteSpace(command.DisplayName)
            || string.IsNullOrWhiteSpace(command.ProtocolType)
            || string.IsNullOrWhiteSpace(command.ConnectionDirection)
            || string.IsNullOrWhiteSpace(command.EndpointStatus)
            || string.IsNullOrWhiteSpace(command.ConfigurationProvenance))
        {
            return "Required endpoint fields are missing.";
        }

        if (!IsValidProtocol(command.ProtocolType) || !IsValidDirection(command.ConnectionDirection)
            || !IsValidEndpointStatus(command.EndpointStatus))
        {
            return "Invalid protocol, direction or status.";
        }

        return null;
    }

    private static string? ValidateEndpointUpdate(UpdateIotPlcEndpointCommand command) =>
        ValidateEndpointCreate(new CreateIotPlcEndpointCommand(
            "code",
            command.DisplayName,
            command.ProtocolType,
            command.ConnectionDirection,
            command.EndpointStatus,
            command.ConfigurationProvenance,
            command.ExternalSecretReference,
            command.ConnectionProfileJson));

    private static string? ValidateSignalCreate(CreateIotPlcSignalCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EndpointReference)
            || string.IsNullOrWhiteSpace(command.SignalCode)
            || string.IsNullOrWhiteSpace(command.DisplayName)
            || string.IsNullOrWhiteSpace(command.SourceAddress)
            || string.IsNullOrWhiteSpace(command.DataType)
            || string.IsNullOrWhiteSpace(command.SemanticType)
            || string.IsNullOrWhiteSpace(command.MappedAssetType)
            || string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Required signal fields are missing.";
        }

        if (!IsValidDataType(command.DataType) || !IsValidAssetType(command.MappedAssetType))
        {
            return "Invalid data type or asset type.";
        }

        return null;
    }

    private static string? ValidateSignalUpdate(UpdateIotPlcSignalCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName)
            || string.IsNullOrWhiteSpace(command.SourceAddress)
            || string.IsNullOrWhiteSpace(command.DataType)
            || string.IsNullOrWhiteSpace(command.SemanticType)
            || string.IsNullOrWhiteSpace(command.MappedAssetType)
            || string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Required signal fields are missing.";
        }

        if (!IsValidDataType(command.DataType) || !IsValidAssetType(command.MappedAssetType))
        {
            return "Invalid data type or asset type.";
        }

        return null;
    }

    private static string? ValidateObservation(IngestIotPlcObservationCommand command, string dataType)
    {
        if (string.IsNullOrWhiteSpace(command.SignalReference)
            || string.IsNullOrWhiteSpace(command.QualityState)
            || string.IsNullOrWhiteSpace(command.EvidenceState)
            || string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Required observation fields are missing.";
        }

        if (command.ObservedAtUtc.Offset != TimeSpan.Zero)
        {
            return "Observed timestamp must be UTC.";
        }

        if (!HasCompatibleValue(command, dataType))
        {
            return "Observation value is incompatible with the signal data type.";
        }

        return null;
    }

    private static string? ValidateConnectionHealth(IngestIotPlcConnectionHealthCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EndpointReference)
            || string.IsNullOrWhiteSpace(command.HealthState)
            || string.IsNullOrWhiteSpace(command.Source)
            || string.IsNullOrWhiteSpace(command.EvidenceState)
            || string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return "Required connection health fields are missing.";
        }

        if (command.ObservedAtUtc.Offset != TimeSpan.Zero)
        {
            return "Observed timestamp must be UTC.";
        }

        if (!IsValidHealthState(command.HealthState))
        {
            return "Invalid health state.";
        }

        return null;
    }

    private static bool HasCompatibleValue(IngestIotPlcObservationCommand command, string dataType) =>
        dataType switch
        {
            IotPlcDataTypes.Boolean => command.BooleanValue is not null
                && command.IntegerValue is null
                && command.DecimalValue is null
                && command.TextValue is null
                && command.TimestampValue is null,
            IotPlcDataTypes.Integer => command.IntegerValue is not null
                && command.BooleanValue is null
                && command.DecimalValue is null
                && command.TextValue is null
                && command.TimestampValue is null,
            IotPlcDataTypes.Decimal => command.DecimalValue is not null
                && command.BooleanValue is null
                && command.IntegerValue is null
                && command.TextValue is null
                && command.TimestampValue is null,
            IotPlcDataTypes.Text => !string.IsNullOrWhiteSpace(command.TextValue)
                && command.BooleanValue is null
                && command.IntegerValue is null
                && command.DecimalValue is null
                && command.TimestampValue is null,
            IotPlcDataTypes.Timestamp => command.TimestampValue is not null
                && command.TimestampValue.Value.Offset == TimeSpan.Zero
                && command.BooleanValue is null
                && command.IntegerValue is null
                && command.DecimalValue is null
                && command.TextValue is null,
            _ => false,
        };

    private static bool IsValidProtocol(string value) =>
        value.Trim() is IotPlcProtocolTypes.OpcUa
            or IotPlcProtocolTypes.ModbusTcp
            or IotPlcProtocolTypes.Mqtt
            or IotPlcProtocolTypes.HttpPush
            or IotPlcProtocolTypes.EdgeAgent
            or IotPlcProtocolTypes.ManualDevelopmentSimulator;

    private static bool IsValidDirection(string value) =>
        value.Trim() is IotPlcConnectionDirections.ReadOnlyPull
            or IotPlcConnectionDirections.InboundPush
            or IotPlcConnectionDirections.EdgeRelay;

    private static bool IsValidEndpointStatus(string value) =>
        value.Trim() is IotPlcEndpointStatuses.Draft
            or IotPlcEndpointStatuses.Active
            or IotPlcEndpointStatuses.Suspended
            or IotPlcEndpointStatuses.Retired;

    private static bool IsValidDataType(string value) =>
        value.Trim() is IotPlcDataTypes.Boolean
            or IotPlcDataTypes.Integer
            or IotPlcDataTypes.Decimal
            or IotPlcDataTypes.Text
            or IotPlcDataTypes.Timestamp;

    private static bool IsValidAssetType(string value) =>
        value.Trim() is IotPlcAssetTypes.Machine
            or IotPlcAssetTypes.AuxiliaryEquipment
            or IotPlcAssetTypes.Facility
            or IotPlcAssetTypes.Line
            or IotPlcAssetTypes.Unmapped;

    private static bool IsValidHealthState(string value) =>
        value.Trim() is IotPlcConnectionStates.Online
            or IotPlcConnectionStates.Degraded
            or IotPlcConnectionStates.Offline
            or IotPlcConnectionStates.Unknown;

    private static string NormalizeQuality(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeEvidence(string value) => value.Trim().ToLowerInvariant();

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
