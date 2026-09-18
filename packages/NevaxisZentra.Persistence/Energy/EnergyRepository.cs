using System.Text.Json;
using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Energy;

public interface IEnergyRepository
{
    Task<EnergyWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default);

    Task<EnergyMeasurementPointListReadModel> ListMeasurementPointsAsync(
        EnergyOperationContext context,
        EnergyMeasurementPointListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyMeasurementPointReadModel?> GetMeasurementPointByReferenceAsync(
        EnergyOperationContext context,
        string reference,
        CancellationToken cancellationToken = default);

    Task<EnergyConsumptionListReadModel> ListConsumptionWindowsAsync(
        EnergyOperationContext context,
        EnergyConsumptionListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyAllocationListReadModel> ListAllocationsAsync(
        EnergyOperationContext context,
        EnergyAllocationListFilters filters,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> IngestReadingAsync(
        EnergyOperationContext context,
        IngestEnergyReadingCommand command,
        CancellationToken cancellationToken = default);

    Task<EnergyWriteResult> EvaluateConsumptionAsync(
        EnergyOperationContext context,
        EvaluateEnergyConsumptionCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class EnergyRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IEnergyRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<EnergyWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var activePoints = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                    .CountAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Status == EnergyMeasurementPointStatuses.Active,
                        ct);

                var recentWindows = await dbContext.EnergyConsumptionWindows.AsNoTracking()
                    .CountAsync(
                        w => w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId
                             && w.CalculatedAtUtc >= DateTimeOffset.UtcNow.AddDays(-30),
                        ct);

                var hasPilot = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                    .AnyAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.SourceProvenance.Contains("Development energy data"),
                        ct);

                return new EnergyWorkspaceReadModel(
                    activePoints,
                    recentWindows,
                    EnergyConstants.RuleSetVersion,
                    hasPilot ? EnergyConstants.DevelopmentPilotProvenance : string.Empty,
                    hasPilot);
            },
            cancellationToken);

    public Task<EnergyMeasurementPointListReadModel> ListMeasurementPointsAsync(
        EnergyOperationContext context,
        EnergyMeasurementPointListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyMeasurementPoints.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.MeasurementScope))
                {
                    query = query.Where(p => p.MeasurementScope == filters.MeasurementScope.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.EquipmentCategory))
                {
                    query = query.Where(p => p.EquipmentCategory == filters.EquipmentCategory.Trim());
                }

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(p => p.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (filters.IsActive.HasValue)
                {
                    var status = filters.IsActive.Value
                        ? EnergyMeasurementPointStatuses.Active
                        : EnergyMeasurementPointStatuses.Inactive;
                    query = query.Where(p => p.Status == status);
                }

                var totalCount = await query.CountAsync(ct);
                var points = await query
                    .OrderBy(p => p.PointCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                var items = points.Select(p => MapPoint(p, machineLookup)).ToList();
                return new EnergyMeasurementPointListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyMeasurementPointReadModel?> GetMeasurementPointByReferenceAsync(
        EnergyOperationContext context,
        string reference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var normalized = reference.Trim();
                var point = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.PointCode == normalized,
                        ct);

                if (point is null)
                {
                    return null;
                }

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                return MapPoint(point, machineLookup);
            },
            cancellationToken);

    public Task<EnergyConsumptionListReadModel> ListConsumptionWindowsAsync(
        EnergyOperationContext context,
        EnergyConsumptionListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyConsumptionWindows.AsNoTracking()
                    .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.MeasurementPointReference))
                {
                    var pointCode = filters.MeasurementPointReference.Trim();
                    var pointIds = await dbContext.EnergyMeasurementPoints.AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId
                                    && p.FacilityId == context.FacilityId
                                    && p.PointCode == pointCode)
                        .Select(p => p.Id)
                        .ToListAsync(ct);
                    query = query.Where(w => pointIds.Contains(w.MeasurementPointId));
                }

                if (filters.WindowStartUtc.HasValue)
                {
                    query = query.Where(w => w.WindowStartUtc >= filters.WindowStartUtc.Value);
                }

                if (filters.WindowEndUtc.HasValue)
                {
                    query = query.Where(w => w.WindowEndUtc <= filters.WindowEndUtc.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.EvidenceStatus))
                {
                    query = query.Where(w => w.EvidenceStatus == filters.EvidenceStatus.Trim());
                }

                var totalCount = await query.CountAsync(ct);
                var windows = await query
                    .OrderByDescending(w => w.WindowStartUtc)
                    .ThenByDescending(w => w.CalculatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var pointLookup = await LoadPointLookupAsync(context, ct);
                var items = windows.Select(w => MapWindow(w, pointLookup)).ToList();
                return new EnergyConsumptionListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyAllocationListReadModel> ListAllocationsAsync(
        EnergyOperationContext context,
        EnergyAllocationListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.EnergyAllocations.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (filters.ConsumptionWindowId.HasValue)
                {
                    query = query.Where(a => a.ConsumptionWindowId == filters.ConsumptionWindowId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.TargetType))
                {
                    query = query.Where(a => a.TargetType == filters.TargetType.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.TargetReference))
                {
                    var reference = filters.TargetReference.Trim();
                    query = query.Where(a => a.TargetReference == reference);
                }

                var totalCount = await query.CountAsync(ct);
                var allocations = await query
                    .OrderBy(a => a.TargetType)
                    .ThenBy(a => a.TargetReference)
                    .ThenByDescending(a => a.CalculatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var items = allocations.Select(MapAllocation).ToList();
                return new EnergyAllocationListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);

    public Task<EnergyWriteResult> IngestReadingAsync(
        EnergyOperationContext context,
        IngestEnergyReadingCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateReading(command);
                if (validationError is not null)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, validationError);
                }

                var point = await FindPointByReferenceAsync(context, command.MeasurementPointReference.Trim(), ct);
                if (point is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.MeasurementPointNotFound,
                        "Measurement point not found.");
                }

                var fingerprint = EnergyIdempotencyFingerprint.ComputeReadingFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existing = await dbContext.EnergyReadings
                        .FirstOrDefaultAsync(
                            r => r.TenantId == context.TenantId
                                 && r.IdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existing is not null)
                    {
                        if (string.IsNullOrEmpty(existing.PayloadFingerprint))
                        {
                            return EnergyWriteResult.Failure(
                                EnergyFailureCodes.IdempotencyLegacyUnverifiable,
                                "Legacy reading cannot be verified for idempotent replay.");
                        }

                        if (!string.Equals(existing.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return EnergyWriteResult.Failure(
                                EnergyFailureCodes.IdempotencyConflict,
                                "Idempotency key was already used with a different payload.");
                        }

                        return EnergyWriteResult.Success(existing.Id, idempotentReplay: true);
                    }
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var evidenceState = NormalizeEvidenceState(command.EvidenceState);

                var reading = new EnergyReading
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MeasurementPointId = point.Id,
                    OccurredAtUtc = command.OccurredAtUtc,
                    ReceivedAtUtc = now,
                    CumulativeEnergyKwh = command.CumulativeEnergyKwh,
                    IntervalEnergyKwh = command.IntervalEnergyKwh,
                    ActivePowerKw = command.ActivePowerKw,
                    MaximumDemandKw = command.MaximumDemandKw,
                    SourceType = command.SourceType.Trim(),
                    SourceReference = TrimOrNull(command.SourceReference),
                    FreshnessState = NormalizeFreshness(command.FreshnessState),
                    EvidenceState = evidenceState,
                    IdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    PayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.EnergyReadings.Add(reading);
                AddAuditEvent(
                    context,
                    reading.Id,
                    "energy-reading",
                    EnergyAuditEventTypes.ReadingIngested,
                    actor,
                    now,
                    new { readingId = reading.Id, pointCode = point.PointCode });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(reading.Id);
            },
            cancellationToken);

    public Task<EnergyWriteResult> EvaluateConsumptionAsync(
        EnergyOperationContext context,
        EvaluateEnergyConsumptionCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEvaluate(command);
                if (validationError is not null)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, validationError);
                }

                var point = await FindPointByReferenceAsync(context, command.MeasurementPointReference.Trim(), ct);
                if (point is null)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.MeasurementPointNotFound,
                        "Measurement point not found.");
                }

                var fingerprint = EnergyIdempotencyFingerprint.ComputeEvaluateFingerprint(context, command);
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var existingWindow = await dbContext.EnergyConsumptionWindows
                        .Include(w => w.Allocations)
                        .FirstOrDefaultAsync(
                            w => w.TenantId == context.TenantId
                                 && w.EvaluateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (existingWindow is not null)
                    {
                        if (string.IsNullOrEmpty(existingWindow.EvaluateIdempotencyPayloadFingerprint))
                        {
                            return EnergyWriteResult.Failure(
                                EnergyFailureCodes.IdempotencyLegacyUnverifiable,
                                "Legacy consumption window cannot be verified for idempotent replay.");
                        }

                        if (!string.Equals(existingWindow.EvaluateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            return EnergyWriteResult.Failure(
                                EnergyFailureCodes.IdempotencyConflict,
                                "Idempotency key was already used with a different evaluation payload.");
                        }

                        return EnergyWriteResult.Success(existingWindow.Id, idempotentReplay: true);
                    }
                }

                var overlapping = await dbContext.EnergyConsumptionWindows.AsNoTracking()
                    .AnyAsync(
                        w => w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId
                             && w.MeasurementPointId == point.Id
                             && w.RuleSetVersion == EnergyConstants.RuleSetVersion
                             && w.WindowStartUtc == command.WindowStartUtc
                             && w.WindowEndUtc == command.WindowEndUtc,
                        ct);

                if (overlapping)
                {
                    return EnergyWriteResult.Failure(
                        EnergyFailureCodes.OverlappingWindow,
                        "A consumption window already exists for this measurement point, window boundaries and rule set.");
                }

                EnergyTariffSnapshot? tariff = null;
                if (command.TariffSnapshotId.HasValue)
                {
                    tariff = await dbContext.EnergyTariffSnapshots.AsNoTracking()
                        .FirstOrDefaultAsync(
                            t => t.Id == command.TariffSnapshotId.Value
                                 && t.TenantId == context.TenantId
                                 && t.FacilityId == context.FacilityId
                                 && t.ValidFromUtc <= command.WindowStartUtc
                                 && t.ValidToUtc >= command.WindowEndUtc,
                            ct);
                }

                var readings = await dbContext.EnergyReadings.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.MeasurementPointId == point.Id
                                && r.OccurredAtUtc >= command.WindowStartUtc
                                && r.OccurredAtUtc <= command.WindowEndUtc)
                    .ToListAsync(ct);

                var readingInputs = readings
                    .Select(r => new EnergyReadingInput(
                        r.Id,
                        r.OccurredAtUtc,
                        r.CumulativeEnergyKwh,
                        r.IntervalEnergyKwh,
                        r.MaximumDemandKw,
                        r.EvidenceState,
                        r.FreshnessState,
                        r.SourceReference))
                    .ToList();

                var calculation = EnergyCalculator.CalculateConsumption(
                    command.WindowStartUtc,
                    command.WindowEndUtc,
                    readingInputs,
                    tariff);

                if (calculation.CalculationMethod == EnergyCalculationMethods.Unavailable
                    && calculation.EvidenceStatus == EnergyEvidenceStates.InsufficientData
                    && calculation.ReadingCount == 0
                    && command.WindowEndUtc <= command.WindowStartUtc)
                {
                    return EnergyWriteResult.Failure(EnergyFailureCodes.InvalidWindow, calculation.MissingDataExplanation);
                }

                var allocations = command.Allocations ?? [];
                if (allocations.Count > 0)
                {
                    var conservation = EnergyCalculator.ValidateAllocations(calculation.ConsumedKwh, allocations);
                    if (!conservation.IsConserved)
                    {
                        return EnergyWriteResult.Failure(
                            EnergyFailureCodes.ValidationFailed,
                            conservation.FailureReason ?? "Allocation conservation failed.");
                    }
                }

                var productionEvidence = await LoadProductionEvidenceAsync(
                    context,
                    command.WindowStartUtc,
                    command.WindowEndUtc,
                    point.MachineAssetId,
                    ct);

                var intensity = EnergyCalculator.CalculateIntensityMetrics(
                    calculation.ConsumedKwh,
                    command.WindowStartUtc,
                    command.WindowEndUtc,
                    productionEvidence);

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();

                var window = new EnergyConsumptionWindow
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MeasurementPointId = point.Id,
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    ConsumedKwh = calculation.ConsumedKwh,
                    AveragePowerKw = calculation.AveragePowerKw,
                    MaximumDemandKw = calculation.MaximumDemandKw,
                    CalculationMethod = calculation.CalculationMethod,
                    EvidenceStatus = calculation.EvidenceStatus,
                    ReadingCount = calculation.ReadingCount,
                    FreshnessState = calculation.FreshnessState,
                    SourceReferencesJson = JsonSerializer.Serialize(calculation.SourceReferences, JsonOptions),
                    RuleSetVersion = EnergyConstants.RuleSetVersion,
                    CostAmount = calculation.CostAmount,
                    CostCurrencyCode = calculation.CostCurrencyCode,
                    TariffSnapshotId = calculation.TariffSnapshotId,
                    MissingDataExplanation = calculation.MissingDataExplanation,
                    EvaluateIdempotencyKey = TrimOrNull(command.IdempotencyKey),
                    EvaluateIdempotencyPayloadFingerprint = fingerprint,
                    CalculatedAtUtc = now,
                    CreatedAtUtc = now,
                    CreatedBy = actor,
                };

                dbContext.EnergyConsumptionWindows.Add(window);

                foreach (var allocationInput in allocations)
                {
                    dbContext.EnergyAllocations.Add(new EnergyAllocation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ConsumptionWindowId = window.Id,
                        TargetType = allocationInput.TargetType.Trim(),
                        TargetReference = TrimOrNull(allocationInput.TargetReference),
                        AllocatedKwh = EnergyCalculator.RoundEnergy(allocationInput.AllocatedKwh),
                        AllocationMethod = allocationInput.AllocationMethod.Trim(),
                        EvidenceSource = allocationInput.EvidenceSource.Trim(),
                        ConfidenceStatus = allocationInput.ConfidenceStatus.Trim(),
                        RuleSetVersion = EnergyConstants.RuleSetVersion,
                        CalculatedAtUtc = now,
                        CreatedAtUtc = now,
                    });
                }

                AddAuditEvent(
                    context,
                    window.Id,
                    "energy-consumption-window",
                    EnergyAuditEventTypes.ConsumptionEvaluated,
                    actor,
                    now,
                    new
                    {
                        windowId = window.Id,
                        consumedKwh = window.ConsumedKwh,
                        evidenceStatus = window.EvidenceStatus,
                        intensityMetrics = intensity.Metrics,
                    });

                await dbContext.SaveChangesAsync(ct);
                return EnergyWriteResult.Success(window.Id);
            },
            cancellationToken);

    private async Task<EnergyMeasurementPoint?> FindPointByReferenceAsync(
        EnergyOperationContext context,
        string reference,
        CancellationToken cancellationToken) =>
        await dbContext.EnergyMeasurementPoints
            .FirstOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.PointCode == reference,
                cancellationToken);

    private async Task<EnergyProductionEvidence?> LoadProductionEvidenceAsync(
        EnergyOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        if (machineAssetId is null)
        {
            return null;
        }

        var cycles = await dbContext.MachineObservedCycles.AsNoTracking()
            .Where(c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.MachineId == machineAssetId.Value
                        && c.SourceTimestampUtc >= windowStartUtc
                        && c.SourceTimestampUtc <= windowEndUtc
                        && !c.IsDuplicate)
            .ToListAsync(cancellationToken);

        if (cycles.Count == 0)
        {
            return new EnergyProductionEvidence(windowStartUtc, windowEndUtc, null, null, null);
        }

        var totalParts = cycles.Count;
        var goodParts = cycles.Count(c =>
            string.Equals(c.Quality, TelemetryQualities.Good, StringComparison.OrdinalIgnoreCase));

        return new EnergyProductionEvidence(
            windowStartUtc,
            windowEndUtc,
            null,
            totalParts > 0 ? totalParts : null,
            goodParts > 0 ? goodParts : null);
    }

    private async Task<Dictionary<Guid, string>> LoadMachineLookupAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken)
    {
        var machines = await dbContext.MachineRegistries.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
            .Select(m => new { m.Id, m.MachineCode, m.Name })
            .ToListAsync(cancellationToken);

        return machines.ToDictionary(
            m => m.Id,
            m => $"{m.MachineCode} — {m.Name}");
    }

    private async Task<Dictionary<Guid, string>> LoadPointLookupAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken)
    {
        var points = await dbContext.EnergyMeasurementPoints.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .Select(p => new { p.Id, p.PointCode })
            .ToListAsync(cancellationToken);

        return points.ToDictionary(p => p.Id, p => p.PointCode);
    }

    private static EnergyMeasurementPointReadModel MapPoint(
        EnergyMeasurementPoint point,
        Dictionary<Guid, string> machineLookup) =>
        new(
            point.Id,
            point.PointCode,
            point.DisplayName,
            point.MeasurementScope,
            point.MachineAssetId,
            point.MachineAssetId.HasValue ? machineLookup.GetValueOrDefault(point.MachineAssetId.Value) : null,
            point.EdgeAssetReference,
            point.EquipmentCategory,
            point.MeasurementUnit,
            point.Status,
            point.SourceProvenance,
            point.Version,
            point.SourceProvenance.Contains("Development energy data", StringComparison.Ordinal));

    private static EnergyConsumptionWindowReadModel MapWindow(
        EnergyConsumptionWindow window,
        Dictionary<Guid, string> pointLookup) =>
        new(
            window.Id,
            window.MeasurementPointId,
            pointLookup.GetValueOrDefault(window.MeasurementPointId, string.Empty),
            window.WindowStartUtc,
            window.WindowEndUtc,
            window.ConsumedKwh,
            window.AveragePowerKw,
            window.MaximumDemandKw,
            window.CalculationMethod,
            window.EvidenceStatus,
            window.ReadingCount,
            window.FreshnessState,
            window.RuleSetVersion,
            window.CostAmount,
            window.CostCurrencyCode,
            window.MissingDataExplanation,
            window.CalculatedAtUtc,
            window.SourceReferencesJson.Contains("Development", StringComparison.Ordinal));

    private static EnergyAllocationReadModel MapAllocation(EnergyAllocation allocation) =>
        new(
            allocation.Id,
            allocation.ConsumptionWindowId,
            allocation.TargetType,
            allocation.TargetReference,
            allocation.AllocatedKwh,
            allocation.AllocationMethod,
            allocation.EvidenceSource,
            allocation.ConfidenceStatus,
            allocation.RuleSetVersion,
            allocation.CalculatedAtUtc);

    private void AddAuditEvent(
        EnergyOperationContext context,
        Guid entityId,
        string entityType,
        string eventType,
        string actor,
        DateTimeOffset occurredAtUtc,
        object payload)
    {
        dbContext.EnergyAuditEvents.Add(new EnergyAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actor,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = context.CorrelationId,
        });
    }

    private static string? ValidateReading(IngestEnergyReadingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MeasurementPointReference))
        {
            return "MeasurementPointReference is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceType))
        {
            return "SourceType is required.";
        }

        if (command.CumulativeEnergyKwh is null && command.IntervalEnergyKwh is null)
        {
            return "At least one of CumulativeEnergyKwh or IntervalEnergyKwh is required.";
        }

        if (command.IntervalEnergyKwh is <= 0 && command.CumulativeEnergyKwh is null)
        {
            return "IntervalEnergyKwh must be positive when provided without cumulative reading.";
        }

        return null;
    }

    private static string? ValidateEvaluate(EvaluateEnergyConsumptionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MeasurementPointReference))
        {
            return "MeasurementPointReference is required.";
        }

        if (command.WindowEndUtc <= command.WindowStartUtc)
        {
            return "WindowEndUtc must be after WindowStartUtc.";
        }

        return null;
    }

    private static string NormalizeEvidenceState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EnergyEvidenceStates.Measured;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            EnergyEvidenceStates.Measured => EnergyEvidenceStates.Measured,
            EnergyEvidenceStates.Calculated => EnergyEvidenceStates.Calculated,
            EnergyEvidenceStates.Estimated => EnergyEvidenceStates.Estimated,
            EnergyEvidenceStates.Simulated => EnergyEvidenceStates.Simulated,
            EnergyEvidenceStates.InsufficientData => EnergyEvidenceStates.InsufficientData,
            EnergyEvidenceStates.SourceConflict => EnergyEvidenceStates.SourceConflict,
            _ => EnergyEvidenceStates.Measured,
        };
    }

    private static string NormalizeFreshness(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EnergyFreshnessStates.Unknown;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            EnergyFreshnessStates.Fresh => EnergyFreshnessStates.Fresh,
            EnergyFreshnessStates.Stale => EnergyFreshnessStates.Stale,
            _ => EnergyFreshnessStates.Unknown,
        };
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableEnergyRepository : IEnergyRepository
{
    public Task<EnergyWorkspaceReadModel> GetWorkspaceAsync(
        EnergyOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyWorkspaceReadModel(0, 0, EnergyConstants.RuleSetVersion, string.Empty, false));

    public Task<EnergyMeasurementPointListReadModel> ListMeasurementPointsAsync(
        EnergyOperationContext context,
        EnergyMeasurementPointListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyMeasurementPointListReadModel([], 0, 1, 25));

    public Task<EnergyMeasurementPointReadModel?> GetMeasurementPointByReferenceAsync(
        EnergyOperationContext context,
        string reference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<EnergyMeasurementPointReadModel?>(null);

    public Task<EnergyConsumptionListReadModel> ListConsumptionWindowsAsync(
        EnergyOperationContext context,
        EnergyConsumptionListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyConsumptionListReadModel([], 0, 1, 25));

    public Task<EnergyAllocationListReadModel> ListAllocationsAsync(
        EnergyOperationContext context,
        EnergyAllocationListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EnergyAllocationListReadModel([], 0, 1, 25));

    public Task<EnergyWriteResult> IngestReadingAsync(
        EnergyOperationContext context,
        IngestEnergyReadingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));

    public Task<EnergyWriteResult> EvaluateConsumptionAsync(
        EnergyOperationContext context,
        EvaluateEnergyConsumptionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EnergyWriteResult.Failure(EnergyFailureCodes.ValidationFailed, "Persistence unavailable."));
}
