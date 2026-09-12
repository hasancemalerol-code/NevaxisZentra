using System.Text.Json;
using NevaxisFactOS.Persistence.Edge;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ProcessOee;

public interface IProcessOeeRepository
{
    Task<ProcessOeeWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcessOeeWindowSummaryReadModel>> ListWindowsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeWindowListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWindowDetailReadModel?> GetWindowByIdAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcessOeeAssessmentSummaryReadModel>> ListAssessmentsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeAssessmentDetailReadModel?> GetCurrentAssessmentForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWriteResult> CreateWindowAsync(
        ProcessOeeOperationContext context,
        CreateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWriteResult> UpdateWindowAsync(
        ProcessOeeOperationContext context,
        UpdateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWriteResult> EvaluateWindowAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWriteResult> EvaluateBoundedAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeBoundedCommand command,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeLossOverviewReadModel> GetLossOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeLossDetailReadModel?> GetLossDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeLossDetailReadModel?> GetLossDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeStabilityOverviewReadModel> GetStabilityOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeDowntimeParetoReadModel?> GetDowntimeParetoForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeShiftComparisonReadModel> GetShiftComparisonAsync(
        ProcessOeeOperationContext context,
        Guid? machineAssetId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<ProcessOeeWriteResult> EvaluateShiftComparisonAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeShiftComparisonCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessOeeRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ProcessOeeEvidenceCollector evidenceCollector,
    ProcessOeeStabilityDataCollector stabilityDataCollector) : IProcessOeeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<ProcessOeeWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var windows = await dbContext.ProcessOeeEvaluationWindows.AsNoTracking()
                    .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId)
                    .CountAsync(ct);

                var assessments = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                return new ProcessOeeWorkspaceSummaryReadModel(
                    windows,
                    assessments.Count,
                    assessments.Count(a => a.Status == ProcessOeeAssessmentStatuses.Calculated),
                    assessments.Count(a => a.Status == ProcessOeeAssessmentStatuses.InsufficientData),
                    assessments.Count(a => a.Status == ProcessOeeAssessmentStatuses.ReviewRequired),
                    assessments.Count(a => a.Status == ProcessOeeAssessmentStatuses.StaleData),
                    assessments
                        .Where(a => a.OeePercent.HasValue)
                        .OrderByDescending(a => a.EvaluatedAtUtc)
                        .Select(a => a.OeePercent)
                        .FirstOrDefault());
            },
            cancellationToken);

    public Task<IReadOnlyList<ProcessOeeWindowSummaryReadModel>> ListWindowsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeWindowListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.ProcessOeeEvaluationWindows.AsNoTracking()
                    .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId);

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(w => w.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(w => w.ProductionOrderReference == reference);
                }

                if (!string.IsNullOrWhiteSpace(filters.MoldReference))
                {
                    var mold = filters.MoldReference.Trim();
                    query = query.Where(w => w.MoldReference == mold);
                }

                if (filters.WindowStartFromUtc.HasValue)
                {
                    query = query.Where(w => w.WindowStartUtc >= filters.WindowStartFromUtc.Value);
                }

                if (filters.WindowEndToUtc.HasValue)
                {
                    query = query.Where(w => w.WindowEndUtc <= filters.WindowEndToUtc.Value);
                }

                var windows = await query.ToListAsync(ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);

                return (IReadOnlyList<ProcessOeeWindowSummaryReadModel>)windows
                    .OrderByDescending(w => w.WindowEndUtc)
                    .ThenByDescending(w => w.UpdatedAtUtc)
                    .ThenByDescending(w => w.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(w => MapWindowSummary(w, machineLookup))
                    .ToList();
            },
            cancellationToken);
    }

    public Task<ProcessOeeWindowDetailReadModel?> GetWindowByIdAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var window = await dbContext.ProcessOeeEvaluationWindows.AsNoTracking()
                    .FirstOrDefaultAsync(
                        w => w.Id == windowId
                             && w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId,
                        ct);

                if (window is null)
                {
                    return null;
                }

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                return MapWindowDetail(window, machineLookup);
            },
            cancellationToken);

    public Task<IReadOnlyList<ProcessOeeAssessmentSummaryReadModel>> ListAssessmentsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeAssessmentListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.ProcessOeeAssessments.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId);

                if (filters.MachineAssetId.HasValue)
                {
                    query = query.Where(a => a.MachineAssetId == filters.MachineAssetId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.ProductionOrderReference))
                {
                    var reference = filters.ProductionOrderReference.Trim();
                    query = query.Where(a => a.ProductionOrderReference == reference);
                }

                if (!string.IsNullOrWhiteSpace(filters.MoldReference))
                {
                    var mold = filters.MoldReference.Trim();
                    query = query.Where(a => a.MoldReference == mold);
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    query = query.Where(a => a.Status == filters.Status.Trim());
                }

                if (filters.EvaluatedFromUtc.HasValue)
                {
                    query = query.Where(a => a.EvaluatedAtUtc >= filters.EvaluatedFromUtc.Value);
                }

                if (filters.EvaluatedToUtc.HasValue)
                {
                    query = query.Where(a => a.EvaluatedAtUtc <= filters.EvaluatedToUtc.Value);
                }

                var assessments = await query.ToListAsync(ct);
                var machineLookup = await LoadMachineLookupAsync(context, ct);

                return (IReadOnlyList<ProcessOeeAssessmentSummaryReadModel>)assessments
                    .OrderByDescending(a => a.WindowEndUtc)
                    .ThenByDescending(a => a.EvaluatedAtUtc)
                    .ThenByDescending(a => a.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => MapAssessmentSummary(a, machineLookup))
                    .ToList();
            },
            cancellationToken);
    }

    public Task<ProcessOeeAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assessment is null)
                {
                    return null;
                }

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                return MapAssessmentDetail(assessment, machineLookup);
            },
            cancellationToken);

    public Task<ProcessOeeAssessmentDetailReadModel?> GetCurrentAssessmentForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .Where(a => a.EvaluationWindowId == windowId
                                && a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId)
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .ThenByDescending(a => a.Id)
                    .FirstOrDefaultAsync(ct);

                if (assessment is null)
                {
                    return null;
                }

                var machineLookup = await LoadMachineLookupAsync(context, ct);
                return MapAssessmentDetail(assessment, machineLookup);
            },
            cancellationToken);

    public Task<ProcessOeeWriteResult> CreateWindowAsync(
        ProcessOeeOperationContext context,
        CreateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = ProcessOeeValidator.ValidateCreateWindow(command);
                if (validationErrors.Count > 0)
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                if (!await MachineExistsAsync(context, command.MachineAssetId, ct))
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.NotFound,
                        "Machine asset not found for tenant and facility.");
                }

                var fingerprint = ProcessOeeIdempotencyFingerprint.ComputeWindowFingerprint(context, command);
                var existing = await dbContext.ProcessOeeEvaluationWindows
                    .FirstOrDefaultAsync(
                        w => w.TenantId == context.TenantId
                             && w.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    if (existing.CreateIdempotencyPayloadFingerprint is null)
                    {
                        return ProcessOeeWriteResult.Fail(
                            ProcessOeeFailureCodes.IdempotencyLegacyUnverifiable,
                            "The idempotency key belongs to a legacy window that cannot be verified safely.");
                    }

                    if (!ProcessOeeIdempotencyFingerprint.WindowPayloadMatches(existing, command, fingerprint))
                    {
                        return ProcessOeeWriteResult.Fail(
                            ProcessOeeFailureCodes.IdempotencyConflict,
                            "The idempotency key was already used with a different command payload.");
                    }

                    return ProcessOeeWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
                }

                var actor = context.IdentitySubjectId.ToString();
                var now = DateTimeOffset.UtcNow;
                var window = new ProcessOeeEvaluationWindow
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MachineAssetId = command.MachineAssetId,
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    MoldReference = TrimOrNull(command.MoldReference),
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    PlannedProductionSeconds = command.PlannedProductionSeconds,
                    IdealCycleTimeSeconds = command.IdealCycleTimeSeconds,
                    EffectiveCavityCount = command.EffectiveCavityCount,
                    SourceType = ProcessOeeValueConversions.NormalizeSourceType(command.SourceType),
                    SourceReference = TrimOrNull(command.SourceReference),
                    DataRecordedAtUtc = command.DataRecordedAtUtc,
                    CreateIdempotencyKey = command.IdempotencyKey.Trim(),
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = actor,
                    UpdatedBySubjectId = actor,
                    Version = 1,
                };

                dbContext.ProcessOeeEvaluationWindows.Add(window);
                await dbContext.SaveChangesAsync(ct);
                return ProcessOeeWriteResult.Success(window.Id, window.Version);
            },
            cancellationToken);

    public Task<ProcessOeeWriteResult> UpdateWindowAsync(
        ProcessOeeOperationContext context,
        UpdateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = ProcessOeeValidator.ValidateUpdateWindow(command);
                if (validationErrors.Count > 0)
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                var window = await dbContext.ProcessOeeEvaluationWindows
                    .FirstOrDefaultAsync(
                        w => w.Id == command.WindowId
                             && w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId,
                        ct);

                if (window is null)
                {
                    return ProcessOeeWriteResult.Fail(ProcessOeeFailureCodes.NotFound, "Evaluation window not found.");
                }

                if (window.Version != command.ExpectedVersion)
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.ConcurrencyConflict,
                        "The evaluation window was modified by another user.");
                }

                var actor = context.IdentitySubjectId.ToString();
                window.ProductionOrderReference = TrimOrNull(command.ProductionOrderReference);
                window.MoldReference = TrimOrNull(command.MoldReference);
                window.WindowStartUtc = command.WindowStartUtc;
                window.WindowEndUtc = command.WindowEndUtc;
                window.PlannedProductionSeconds = command.PlannedProductionSeconds;
                window.IdealCycleTimeSeconds = command.IdealCycleTimeSeconds;
                window.EffectiveCavityCount = command.EffectiveCavityCount;
                window.SourceType = ProcessOeeValueConversions.NormalizeSourceType(command.SourceType);
                window.SourceReference = TrimOrNull(command.SourceReference);
                window.DataRecordedAtUtc = command.DataRecordedAtUtc;
                window.UpdatedAtUtc = DateTimeOffset.UtcNow;
                window.UpdatedBySubjectId = actor;
                window.Version++;

                await dbContext.SaveChangesAsync(ct);
                return ProcessOeeWriteResult.Success(window.Id, window.Version);
            },
            cancellationToken);

    public Task<ProcessOeeWriteResult> EvaluateWindowAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var window = await dbContext.ProcessOeeEvaluationWindows
                    .FirstOrDefaultAsync(
                        w => w.Id == command.EvaluationWindowId
                             && w.TenantId == context.TenantId
                             && w.FacilityId == context.FacilityId,
                        ct);

                if (window is null)
                {
                    return ProcessOeeWriteResult.Fail(ProcessOeeFailureCodes.NotFound, "Evaluation window not found.");
                }

                return await EvaluateAndPersistAsync(context, window, command.IdempotencyKey, ct, persistEphemeralWindow: false);
            },
            cancellationToken);

    public Task<ProcessOeeWriteResult> EvaluateBoundedAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeBoundedCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationErrors = ProcessOeeValidator.ValidateEvaluateBounded(command);
                if (validationErrors.Count > 0)
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.ValidationFailed,
                        string.Join(" ", validationErrors));
                }

                if (!await MachineExistsAsync(context, command.MachineAssetId, ct))
                {
                    return ProcessOeeWriteResult.Fail(
                        ProcessOeeFailureCodes.NotFound,
                        "Machine asset not found for tenant and facility.");
                }

                var actor = context.IdentitySubjectId.ToString();
                var now = DateTimeOffset.UtcNow;
                var window = new ProcessOeeEvaluationWindow
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MachineAssetId = command.MachineAssetId,
                    ProductionOrderReference = TrimOrNull(command.ProductionOrderReference),
                    MoldReference = TrimOrNull(command.MoldReference),
                    WindowStartUtc = command.WindowStartUtc,
                    WindowEndUtc = command.WindowEndUtc,
                    PlannedProductionSeconds = command.PlannedProductionSeconds,
                    IdealCycleTimeSeconds = command.IdealCycleTimeSeconds,
                    EffectiveCavityCount = command.EffectiveCavityCount,
                    SourceType = ProcessOeeSourceTypes.Manual,
                    SourceReference = "bounded-evaluation",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = actor,
                    UpdatedBySubjectId = actor,
                    Version = 1,
                };

                dbContext.ProcessOeeEvaluationWindows.Add(window);
                await dbContext.SaveChangesAsync(ct);

                return await EvaluateAndPersistAsync(context, window, command.IdempotencyKey, ct, persistEphemeralWindow: false);
            },
            cancellationToken);

    private async Task<ProcessOeeWriteResult> EvaluateAndPersistAsync(
        ProcessOeeOperationContext context,
        ProcessOeeEvaluationWindow window,
        string idempotencyKey,
        CancellationToken cancellationToken,
        bool persistEphemeralWindow = true)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ProcessOeeWriteResult.Fail(ProcessOeeFailureCodes.ValidationFailed, "IdempotencyKey is required.");
        }

        var evidence = await evidenceCollector.CollectAsync(context, window, cancellationToken);
        var calculation = ProcessOeeCalculator.Calculate(window, evidence);

        var existingByFingerprint = await dbContext.ProcessOeeAssessments
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.MachineAssetId == window.MachineAssetId
                     && a.InputFingerprint == calculation.InputFingerprint,
                cancellationToken);

        if (existingByFingerprint is not null)
        {
            await EnsureLossDriversPersistedAsync(
                context,
                window,
                evidence,
                calculation,
                existingByFingerprint,
                cancellationToken);
            await ProcessOeeStabilityPersistence.EnsureStabilityPersistedAsync(
                dbContext,
                context,
                window,
                evidence,
                calculation,
                existingByFingerprint,
                stabilityDataCollector,
                cancellationToken);
            return ProcessOeeWriteResult.Success(
                existingByFingerprint.Id,
                existingByFingerprint.Version,
                idempotentReplay: true);
        }

        var existingByKey = await dbContext.ProcessOeeAssessments
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.MachineAssetId == window.MachineAssetId
                     && a.IdempotencyKey == idempotencyKey.Trim(),
                cancellationToken);

        if (existingByKey is not null)
        {
            if (existingByKey.InputFingerprint != calculation.InputFingerprint)
            {
                return ProcessOeeWriteResult.Fail(
                    ProcessOeeFailureCodes.AssessmentIdempotencyConflict,
                    "The idempotency key was already used with a different evaluation input.");
            }

            await EnsureLossDriversPersistedAsync(
                context,
                window,
                evidence,
                calculation,
                existingByKey,
                cancellationToken);
            await ProcessOeeStabilityPersistence.EnsureStabilityPersistedAsync(
                dbContext,
                context,
                window,
                evidence,
                calculation,
                existingByKey,
                stabilityDataCollector,
                cancellationToken);
            return ProcessOeeWriteResult.Success(existingByKey.Id, existingByKey.Version, idempotentReplay: true);
        }

        if (persistEphemeralWindow && window.CreateIdempotencyKey is null)
        {
            dbContext.ProcessOeeEvaluationWindows.Add(window);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var actor = context.IdentitySubjectId.ToString();
        var assessment = new ProcessOeeAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MachineAssetId = window.MachineAssetId,
            ProductionOrderReference = window.ProductionOrderReference,
            MoldReference = window.MoldReference,
            EvaluationWindowId = window.Id,
            Status = calculation.Status,
            RuleSetVersion = ProcessOeeConstants.RuleSetVersion,
            InputFingerprint = calculation.InputFingerprint,
            WindowStartUtc = window.WindowStartUtc,
            WindowEndUtc = window.WindowEndUtc,
            PlannedProductionSeconds = calculation.PlannedProductionSeconds,
            OperatingSeconds = calculation.OperatingSeconds,
            DowntimeSeconds = calculation.DowntimeSeconds,
            UnknownSeconds = calculation.UnknownSeconds,
            ObservedCycleCount = calculation.ObservedCycleCount,
            DerivedShotCount = calculation.DerivedShotCount,
            TotalPieceCount = calculation.TotalPieceCount,
            GoodPieceCount = calculation.GoodPieceCount,
            ScrapPieceCount = calculation.ScrapPieceCount,
            AvailabilityPercent = calculation.AvailabilityPercent,
            PerformanceRawPercent = calculation.PerformanceRawPercent,
            PerformancePercent = calculation.PerformancePercent,
            QualityPercent = calculation.QualityPercent,
            OeePercent = calculation.OeePercent,
            StructuredDrivers = JsonSerializer.Serialize(calculation.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(new
            {
                calculation.Evidence,
                calculation.MissingFields,
                calculation.Conflicts,
                calculation.SourceSystem,
                calculation.SourceFreshnessUtc,
                calculation.IsSimulatorDerived,
                honestyDisclaimer = ProcessOeeConstants.CalculationHonestyDisclaimer,
                simulatorDisclaimer = calculation.IsSimulatorDerived
                    ? ProcessOeeConstants.SimulatorHonestyDisclaimer
                    : null,
            }, JsonOptions),
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            Version = 1,
        };

        dbContext.ProcessOeeAssessments.Add(assessment);

        var lossAnalysis = ProcessOeeLossCalculator.Analyze(window, evidence, calculation);
        PersistLossDrivers(context, assessment, lossAnalysis, actor);

        var stabilityData = await stabilityDataCollector.CollectAsync(context, window, evidence, cancellationToken);
        var stabilityAnalysis = ProcessOeeStabilityCalculator.Analyze(window, evidence, calculation, stabilityData);
        ProcessOeeStabilityPersistence.PersistStability(context, assessment, stabilityAnalysis, actor, dbContext);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var replay = await dbContext.ProcessOeeAssessments
                .FirstOrDefaultAsync(
                    a => a.TenantId == context.TenantId
                         && a.MachineAssetId == window.MachineAssetId
                         && a.InputFingerprint == calculation.InputFingerprint,
                    cancellationToken);

            if (replay is not null)
            {
                return ProcessOeeWriteResult.Success(replay.Id, replay.Version, idempotentReplay: true);
            }

            throw;
        }

        return ProcessOeeWriteResult.Success(assessment.Id, assessment.Version);
    }

    public Task<ProcessOeeLossOverviewReadModel> GetLossOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var drivers = await dbContext.ProcessOeeLossDrivers.AsNoTracking()
                    .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                var assessmentIds = drivers.Select(d => d.AssessmentId).Distinct().Count();
                return new ProcessOeeLossOverviewReadModel(
                    assessmentIds,
                    drivers.Count(d => d.Category == ProcessOeeLossCategories.AvailabilityLoss),
                    drivers.Count(d => d.Category == ProcessOeeLossCategories.PerformanceLoss),
                    drivers.Count(d => d.Category == ProcessOeeLossCategories.QualityLoss),
                    drivers.Count(d => d.Category == ProcessOeeLossCategories.InsufficientData),
                    drivers.Count(d => d.Category == ProcessOeeLossCategories.SourceConflict),
                    ProcessOeeLossConstants.RuleSetVersion,
                    ProcessOeeLossConstants.LeadingLossMethod);
            },
            cancellationToken);

    public Task<ProcessOeeLossDetailReadModel?> GetLossDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);

                if (assessment is null)
                {
                    return null;
                }

                return await BuildLossDetailAsync(context, assessment, ct);
            },
            cancellationToken);

    public Task<ProcessOeeLossDetailReadModel?> GetLossDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .Where(a => a.EvaluationWindowId == windowId
                                && a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId)
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (assessment is null)
                {
                    return null;
                }

                return await BuildLossDetailAsync(context, assessment, ct);
            },
            cancellationToken);

    private async Task<ProcessOeeLossDetailReadModel?> BuildLossDetailAsync(
        ProcessOeeOperationContext context,
        ProcessOeeAssessment assessment,
        CancellationToken cancellationToken)
    {
        var drivers = await dbContext.ProcessOeeLossDrivers.AsNoTracking()
            .Where(d => d.AssessmentId == assessment.Id
                        && d.TenantId == context.TenantId
                        && d.FacilityId == context.FacilityId)
            .OrderByDescending(d => d.IsLeadingLoss)
            .ThenByDescending(d => d.OperationalPriority)
            .ThenBy(d => d.ReasonCode)
            .ToListAsync(cancellationToken);

        var detail = MapAssessmentDetail(assessment, await LoadMachineLookupAsync(context, cancellationToken));
        var mappedDrivers = drivers.Select(MapLossDriver).ToList();

        return new ProcessOeeLossDetailReadModel(
            assessment.Id,
            assessment.EvaluationWindowId,
            assessment.Status,
            ProcessOeeLossConstants.RuleSetVersion,
            ProcessOeeLossConstants.LeadingLossMethod,
            mappedDrivers.FirstOrDefault(d => d.IsLeadingLoss),
            mappedDrivers,
            detail.Evidence,
            detail.MissingFields,
            detail.Conflicts,
            detail.SourceSystem,
            detail.SourceFreshnessUtc,
            detail.IsSimulatorDerived,
            detail.CalculationHonestyDisclaimer,
            detail.SimulatorHonestyDisclaimer);
    }

    public Task<ProcessOeeStabilityOverviewReadModel> GetStabilityOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessments = await dbContext.ProcessOeeStabilityAssessments.AsNoTracking()
                    .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                return new ProcessOeeStabilityOverviewReadModel(
                    assessments.Count,
                    assessments.Count(s => s.ResultState == ProcessOeeStabilityStates.Stable),
                    assessments.Count(s => s.ResultState == ProcessOeeStabilityStates.Variable),
                    assessments.Count(s => s.ResultState == ProcessOeeStabilityStates.Drifting),
                    assessments.Count(s => s.ResultState == ProcessOeeStabilityStates.ReviewRequired),
                    assessments.Count(s => s.ResultState == ProcessOeeStabilityStates.InsufficientData),
                    ProcessOeeStabilityConstants.RuleSetVersion);
            },
            cancellationToken);

    public Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);
                return assessment is null ? null : await BuildStabilityDetailAsync(context, assessment, ct);
            },
            cancellationToken);

    public Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .Where(a => a.EvaluationWindowId == windowId
                                && a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId)
                    .OrderByDescending(a => a.EvaluatedAtUtc)
                    .FirstOrDefaultAsync(ct);
                return assessment is null ? null : await BuildStabilityDetailAsync(context, assessment, ct);
            },
            cancellationToken);

    public Task<ProcessOeeDowntimeParetoReadModel?> GetDowntimeParetoForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assessment = await dbContext.ProcessOeeAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.Id == assessmentId
                             && a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId,
                        ct);
                if (assessment is null)
                {
                    return null;
                }

                var stability = await dbContext.ProcessOeeStabilityAssessments.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.AssessmentId == assessment.Id && s.TenantId == context.TenantId, ct);
                if (stability is null)
                {
                    return null;
                }

                var rows = await dbContext.ProcessOeeDowntimeBreakdownRows.AsNoTracking()
                    .Where(r => r.AssessmentId == assessment.Id && r.TenantId == context.TenantId)
                    .OrderBy(r => r.SortRank)
                    .ToListAsync(ct);

                var total = rows.Sum(r => r.DurationSeconds);
                return new ProcessOeeDowntimeParetoReadModel(
                    assessment.Id,
                    assessment.WindowStartUtc,
                    assessment.WindowEndUtc,
                    total,
                    stability.ClassifiedDowntimeSeconds,
                    stability.UnclassifiedDowntimeSeconds,
                    ProcessOeeStabilityConstants.RuleSetVersion,
                    rows.Select(r => ProcessOeeStabilityPersistence.MapBreakdownRow(r, total)).ToList());
            },
            cancellationToken);

    public Task<ProcessOeeShiftComparisonReadModel> GetShiftComparisonAsync(
        ProcessOeeOperationContext context,
        Guid? machineAssetId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => BuildShiftComparisonAsync(context, machineAssetId, periodStartUtc, periodEndUtc, ct),
            cancellationToken);

    public Task<ProcessOeeWriteResult> EvaluateShiftComparisonAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeShiftComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return ProcessOeeWriteResult.Fail(ProcessOeeFailureCodes.ValidationFailed, "IdempotencyKey is required.");
                }

                var existing = await dbContext.ProcessOeeShiftComparisonBatches
                    .FirstOrDefaultAsync(
                        b => b.TenantId == context.TenantId
                             && b.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);
                if (existing is not null)
                {
                    return ProcessOeeWriteResult.Success(existing.Id, 1, idempotentReplay: true);
                }

                var comparison = await BuildShiftComparisonAsync(
                    context,
                    command.MachineAssetId,
                    command.PeriodStartUtc,
                    command.PeriodEndUtc,
                    ct);

                var actor = context.IdentitySubjectId.ToString();
                var now = DateTimeOffset.UtcNow;
                var batch = new ProcessOeeShiftComparisonBatch
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MachineAssetId = command.MachineAssetId,
                    ComparisonPeriodStartUtc = command.PeriodStartUtc,
                    ComparisonPeriodEndUtc = command.PeriodEndUtc,
                    IsComparable = comparison.IsComparable,
                    ComparabilityWarningCode = comparison.ComparabilityWarningCode,
                    SummaryJson = JsonSerializer.Serialize(comparison.Summary, JsonOptions),
                    EvidenceFingerprint = ComputeShiftComparisonFingerprint(comparison),
                    RuleSetVersion = comparison.RuleSetVersion,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    CreatedAtUtc = now,
                    CreatedBySubjectId = actor,
                };

                dbContext.ProcessOeeShiftComparisonBatches.Add(batch);
                foreach (var row in comparison.Rows)
                {
                    dbContext.ProcessOeeShiftComparisonRows.Add(new ProcessOeeShiftComparisonRow
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        BatchId = batch.Id,
                        AssessmentId = row.AssessmentId,
                        ShiftCode = row.ShiftCode,
                        ShiftLabel = row.ShiftLabel,
                        WindowStartUtc = row.WindowStartUtc,
                        WindowEndUtc = row.WindowEndUtc,
                        PlannedProductionSeconds = row.PlannedProductionSeconds,
                        OperatingSeconds = row.OperatingSeconds,
                        AvailabilityPercent = row.AvailabilityPercent,
                        PerformancePercent = row.PerformancePercent,
                        QualityPercent = row.QualityPercent,
                        OeePercent = row.OeePercent,
                        StabilityResultState = row.StabilityResultState,
                        DowntimeSeconds = row.DowntimeSeconds,
                        MicroStopCount = row.MicroStopCount,
                        DataSufficiency = row.DataSufficiency,
                        FreshnessState = row.FreshnessState,
                        IsComparable = row.IsComparable,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actor,
                    });
                }

                await dbContext.SaveChangesAsync(ct);
                return ProcessOeeWriteResult.Success(batch.Id, 1);
            },
            cancellationToken);

    private async Task<ProcessOeeStabilityDetailReadModel?> BuildStabilityDetailAsync(
        ProcessOeeOperationContext context,
        ProcessOeeAssessment assessment,
        CancellationToken cancellationToken)
    {
        var stability = await dbContext.ProcessOeeStabilityAssessments.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AssessmentId == assessment.Id
                     && s.TenantId == context.TenantId
                     && s.FacilityId == context.FacilityId,
                cancellationToken);
        if (stability is null)
        {
            return null;
        }

        var drivers = await dbContext.ProcessOeeStabilityDrivers.AsNoTracking()
            .Where(d => d.AssessmentId == assessment.Id && d.TenantId == context.TenantId)
            .OrderByDescending(d => d.OperationalPriority)
            .ThenBy(d => d.ReasonCode)
            .ToListAsync(cancellationToken);

        var breakdown = await dbContext.ProcessOeeDowntimeBreakdownRows.AsNoTracking()
            .Where(r => r.AssessmentId == assessment.Id && r.TenantId == context.TenantId)
            .OrderBy(r => r.SortRank)
            .ToListAsync(cancellationToken);

        var total = breakdown.Sum(r => r.DurationSeconds);
        var detail = MapAssessmentDetail(assessment, await LoadMachineLookupAsync(context, cancellationToken));

        return new ProcessOeeStabilityDetailReadModel(
            assessment.Id,
            assessment.EvaluationWindowId,
            assessment.Status,
            stability.ResultState,
            stability.RuleSetVersion,
            stability.MedianCycleMs,
            stability.MadCycleMs,
            stability.SampleCount,
            stability.DriftDirection,
            stability.MicroStopCount,
            stability.MicroStopTotalSeconds,
            stability.MicroStopMedianSeconds,
            stability.ClassifiedDowntimeSeconds,
            stability.UnclassifiedDowntimeSeconds,
            drivers.Select(ProcessOeeStabilityPersistence.MapStabilityDriver).ToList(),
            breakdown.Select(r => ProcessOeeStabilityPersistence.MapBreakdownRow(r, total)).ToList(),
            detail.Evidence,
            detail.MissingFields,
            detail.Conflicts,
            detail.SourceSystem,
            detail.SourceFreshnessUtc,
            detail.IsSimulatorDerived,
            detail.CalculationHonestyDisclaimer,
            detail.SimulatorHonestyDisclaimer);
    }

    private async Task<ProcessOeeShiftComparisonReadModel> BuildShiftComparisonAsync(
        ProcessOeeOperationContext context,
        Guid? machineAssetId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken)
    {
        var assessmentsQuery = dbContext.ProcessOeeAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.WindowStartUtc >= periodStartUtc
                        && a.WindowEndUtc <= periodEndUtc);

        if (machineAssetId.HasValue)
        {
            assessmentsQuery = assessmentsQuery.Where(a => a.MachineAssetId == machineAssetId.Value);
        }

        var assessments = await assessmentsQuery.ToListAsync(cancellationToken);
        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var stabilityMap = await dbContext.ProcessOeeStabilityAssessments.AsNoTracking()
            .Where(s => assessmentIds.Contains(s.AssessmentId) && s.TenantId == context.TenantId)
            .ToDictionaryAsync(s => s.AssessmentId, cancellationToken);

        var inputs = new List<ProcessOeeShiftComparisonInput>();
        foreach (var assessment in assessments)
        {
            if (string.IsNullOrWhiteSpace(assessment.ProductionOrderReference))
            {
                continue;
            }

            var order = await dbContext.ProductionOrders.AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.TenantId == context.TenantId
                         && o.FacilityId == context.FacilityId
                         && o.ReferenceCode == assessment.ProductionOrderReference.Trim(),
                    cancellationToken);
            if (order is null)
            {
                continue;
            }

            var assignment = await dbContext.ProductionOrderShiftAssignments.AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.ProductionOrderId == order.Id
                            && a.ShiftStartAtUtc < assessment.WindowEndUtc
                            && a.ShiftEndAtUtc > assessment.WindowStartUtc)
                .OrderByDescending(a => a.AssignedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (assignment is null)
            {
                continue;
            }

            var shift = await dbContext.ProductionShifts.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == assignment.ProductionShiftId
                         && s.TenantId == context.TenantId
                         && s.FacilityId == context.FacilityId,
                    cancellationToken);
            var shiftLabel = shift is not null ? $"{shift.Code} — {shift.Name}" : assignment.ShiftCode;
            stabilityMap.TryGetValue(assessment.Id, out var stability);

            var isSufficient = assessment.Status == ProcessOeeAssessmentStatuses.Calculated
                               || assessment.Status == ProcessOeeAssessmentStatuses.StaleData;
            var isStale = assessment.Status == ProcessOeeAssessmentStatuses.StaleData;

            inputs.Add(new ProcessOeeShiftComparisonInput(
                assessment.FacilityId,
                assessment.Id,
                assignment.ShiftCode,
                shiftLabel,
                assessment.WindowStartUtc,
                assessment.WindowEndUtc,
                assessment.PlannedProductionSeconds,
                assessment.OperatingSeconds,
                assessment.AvailabilityPercent,
                assessment.PerformancePercent,
                assessment.QualityPercent,
                assessment.OeePercent,
                stability?.ResultState,
                assessment.DowntimeSeconds,
                stability?.MicroStopCount ?? 0,
                isSufficient ? "sufficient" : "insufficient",
                isStale ? ProcessOeeStabilityFreshnessStates.Stale : ProcessOeeStabilityFreshnessStates.Fresh,
                isSufficient,
                isStale));
        }

        var result = ProcessOeeStabilityCalculator.CompareShifts(context, periodStartUtc, periodEndUtc, inputs);
        return new ProcessOeeShiftComparisonReadModel(
            result.PeriodStartUtc,
            result.PeriodEndUtc,
            result.IsComparable,
            result.ComparabilityWarningCode,
            result.RuleSetVersion,
            result.Summary is null
                ? null
                : new ProcessOeeShiftComparisonSummaryReadModel(
                    result.Summary.HighestOeeShiftLabel,
                    result.Summary.LowestAvailabilityShiftLabel,
                    result.Summary.HighestDowntimeShiftLabel,
                    result.Summary.HighestVariabilityShiftLabel),
            result.Rows.Select(r => new ProcessOeeShiftComparisonRowReadModel(
                r.AssessmentId,
                r.ShiftCode,
                r.ShiftLabel,
                r.WindowStartUtc,
                r.WindowEndUtc,
                r.PlannedProductionSeconds,
                r.OperatingSeconds,
                r.AvailabilityPercent,
                r.PerformancePercent,
                r.QualityPercent,
                r.OeePercent,
                r.StabilityResultState,
                r.DowntimeSeconds,
                r.MicroStopCount,
                r.DataSufficiency,
                r.FreshnessState,
                r.IsComparable)).ToList());
    }

    private static string ComputeShiftComparisonFingerprint(ProcessOeeShiftComparisonReadModel comparison) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(new
                    {
                        comparison.PeriodStartUtc,
                        comparison.PeriodEndUtc,
                        comparison.IsComparable,
                        rows = comparison.Rows.Select(r => r.AssessmentId).OrderBy(id => id),
                        comparison.RuleSetVersion,
                    })))).ToLowerInvariant();

    private async Task EnsureLossDriversPersistedAsync(
        ProcessOeeOperationContext context,
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        ProcessOeeAssessment assessment,
        CancellationToken cancellationToken)
    {
        var hasDrivers = await dbContext.ProcessOeeLossDrivers.AsNoTracking()
            .AnyAsync(d => d.TenantId == context.TenantId && d.AssessmentId == assessment.Id, cancellationToken);
        if (hasDrivers)
        {
            return;
        }

        var actor = context.IdentitySubjectId.ToString();
        var lossAnalysis = ProcessOeeLossCalculator.Analyze(window, evidence, calculation);
        PersistLossDrivers(context, assessment, lossAnalysis, actor);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void PersistLossDrivers(
        ProcessOeeOperationContext context,
        ProcessOeeAssessment assessment,
        ProcessOeeLossAnalysisResult lossAnalysis,
        string actor)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var draft in lossAnalysis.Drivers)
        {
            dbContext.ProcessOeeLossDrivers.Add(new ProcessOeeLossDriver
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssessmentId = assessment.Id,
                ReasonCode = draft.ReasonCode,
                Category = draft.Category,
                Severity = draft.Severity,
                OperationalPriority = draft.OperationalPriority,
                ObservedValue = draft.ObservedValue,
                ExpectedValue = draft.ExpectedValue,
                AbsoluteLossValue = draft.AbsoluteLossValue,
                AffectedDurationSeconds = draft.AffectedDurationSeconds,
                AffectedQuantity = draft.AffectedQuantity,
                SourceReferencesJson = draft.SourceReferencesJson,
                EvidenceTimestampUtc = draft.EvidenceTimestampUtc,
                FreshnessState = draft.FreshnessState,
                ExplanationCode = draft.ExplanationCode,
                DataLimitationsJson = draft.DataLimitationsJson,
                RuleSetVersion = lossAnalysis.RuleSetVersion,
                IsLeadingLoss = draft.IsLeadingLoss,
                NormalizedImpactPoints = draft.NormalizedImpactPoints,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
            });
        }
    }

    private static ProcessOeeLossDriverReadModel MapLossDriver(ProcessOeeLossDriver driver)
    {
        var sourceReferences = JsonSerializer.Deserialize<List<string>>(driver.SourceReferencesJson, JsonOptions) ?? [];
        var limitations = JsonSerializer.Deserialize<List<string>>(driver.DataLimitationsJson, JsonOptions) ?? [];
        return new ProcessOeeLossDriverReadModel(
            driver.Id,
            driver.ReasonCode,
            driver.Category,
            driver.Severity,
            driver.OperationalPriority,
            driver.ObservedValue,
            driver.ExpectedValue,
            driver.AbsoluteLossValue,
            driver.AffectedDurationSeconds,
            driver.AffectedQuantity,
            sourceReferences,
            driver.EvidenceTimestampUtc,
            driver.FreshnessState,
            driver.ExplanationCode,
            limitations,
            driver.RuleSetVersion,
            driver.IsLeadingLoss,
            driver.NormalizedImpactPoints);
    }

    private async Task<bool> MachineExistsAsync(
        ProcessOeeOperationContext context,
        Guid machineAssetId,
        CancellationToken cancellationToken) =>
        await dbContext.MachineRegistries.AsNoTracking()
            .AnyAsync(
                m => m.Id == machineAssetId
                     && m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId,
                cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, MachineRegistry>> LoadMachineLookupAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken)
    {
        var machines = await dbContext.MachineRegistries.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
            .ToListAsync(cancellationToken);

        return machines.ToDictionary(m => m.Id);
    }

    private static ProcessOeeWindowSummaryReadModel MapWindowSummary(
        ProcessOeeEvaluationWindow window,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        machineLookup.TryGetValue(window.MachineAssetId, out var machine);
        return new ProcessOeeWindowSummaryReadModel(
            window.Id,
            window.MachineAssetId,
            machine?.MachineCode,
            machine?.Name,
            window.ProductionOrderReference,
            window.MoldReference,
            window.WindowStartUtc,
            window.WindowEndUtc,
            window.PlannedProductionSeconds,
            window.SourceType,
            window.SourceReference,
            window.UpdatedAtUtc,
            window.Version);
    }

    private static ProcessOeeWindowDetailReadModel MapWindowDetail(
        ProcessOeeEvaluationWindow window,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        machineLookup.TryGetValue(window.MachineAssetId, out var machine);
        return new ProcessOeeWindowDetailReadModel(
            window.Id,
            window.MachineAssetId,
            machine?.MachineCode,
            machine?.Name,
            window.ProductionOrderReference,
            window.MoldReference,
            window.WindowStartUtc,
            window.WindowEndUtc,
            window.PlannedProductionSeconds,
            window.IdealCycleTimeSeconds,
            window.EffectiveCavityCount,
            window.SourceType,
            window.SourceReference,
            window.DataRecordedAtUtc,
            window.CreatedAtUtc,
            window.UpdatedAtUtc,
            window.Version);
    }

    private static ProcessOeeAssessmentSummaryReadModel MapAssessmentSummary(
        ProcessOeeAssessment assessment,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        machineLookup.TryGetValue(assessment.MachineAssetId, out var machine);
        return new ProcessOeeAssessmentSummaryReadModel(
            assessment.Id,
            assessment.MachineAssetId,
            machine?.MachineCode,
            machine?.Name,
            assessment.ProductionOrderReference,
            assessment.MoldReference,
            assessment.WindowStartUtc,
            assessment.WindowEndUtc,
            assessment.Status,
            assessment.AvailabilityPercent,
            assessment.PerformancePercent,
            assessment.QualityPercent,
            assessment.OeePercent,
            assessment.EvaluatedAtUtc);
    }

    private static ProcessOeeAssessmentDetailReadModel MapAssessmentDetail(
        ProcessOeeAssessment assessment,
        IReadOnlyDictionary<Guid, MachineRegistry> machineLookup)
    {
        machineLookup.TryGetValue(assessment.MachineAssetId, out var machine);
        var drivers = JsonSerializer.Deserialize<List<ProcessOeeDriverReadModel>>(assessment.StructuredDrivers, JsonOptions) ?? [];
        var evidencePayload = JsonSerializer.Deserialize<JsonElement>(assessment.StructuredEvidence, JsonOptions);
        var evidence = evidencePayload.TryGetProperty("evidence", out var evidenceNode)
            ? JsonSerializer.Deserialize<List<ProcessOeeEvidenceReadModel>>(evidenceNode.GetRawText(), JsonOptions) ?? []
            : [];
        var missingFields = evidencePayload.TryGetProperty("missingFields", out var missingNode)
            ? JsonSerializer.Deserialize<List<string>>(missingNode.GetRawText(), JsonOptions) ?? []
            : [];
        var conflicts = evidencePayload.TryGetProperty("conflicts", out var conflictsNode)
            ? JsonSerializer.Deserialize<List<string>>(conflictsNode.GetRawText(), JsonOptions) ?? []
            : [];
        var sourceSystem = evidencePayload.TryGetProperty("sourceSystem", out var sourceNode) && sourceNode.ValueKind != JsonValueKind.Null
            ? sourceNode.GetString()
            : null;
        DateTimeOffset? sourceFreshness = evidencePayload.TryGetProperty("sourceFreshnessUtc", out var freshnessNode)
                                          && freshnessNode.ValueKind != JsonValueKind.Null
            ? freshnessNode.GetDateTimeOffset()
            : null;
        var isSimulator = evidencePayload.TryGetProperty("isSimulatorDerived", out var simNode) && simNode.GetBoolean();
        var simulatorDisclaimer = evidencePayload.TryGetProperty("simulatorDisclaimer", out var simDiscNode)
                                  && simDiscNode.ValueKind == JsonValueKind.String
            ? simDiscNode.GetString()
            : null;

        return new ProcessOeeAssessmentDetailReadModel(
            assessment.Id,
            assessment.EvaluationWindowId,
            assessment.MachineAssetId,
            machine?.MachineCode,
            machine?.Name,
            assessment.ProductionOrderReference,
            assessment.MoldReference,
            assessment.WindowStartUtc,
            assessment.WindowEndUtc,
            assessment.Status,
            assessment.RuleSetVersion,
            assessment.PlannedProductionSeconds,
            assessment.OperatingSeconds,
            assessment.DowntimeSeconds,
            assessment.UnknownSeconds,
            assessment.ObservedCycleCount,
            assessment.DerivedShotCount,
            assessment.TotalPieceCount,
            assessment.GoodPieceCount,
            assessment.ScrapPieceCount,
            assessment.AvailabilityPercent,
            assessment.PerformanceRawPercent,
            assessment.PerformancePercent,
            assessment.QualityPercent,
            assessment.OeePercent,
            drivers,
            evidence,
            missingFields,
            conflicts,
            sourceSystem,
            sourceFreshness,
            isSimulator,
            ProcessOeeConstants.CalculationHonestyDisclaimer,
            simulatorDisclaimer,
            assessment.EvaluatedAtUtc);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableProcessOeeRepository : IProcessOeeRepository
{
    private static ProcessOeeWriteResult Unavailable() =>
        ProcessOeeWriteResult.Fail("process-oee.unavailable", "Process OEE persistence is unavailable.");

    public Task<ProcessOeeWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProcessOeeWorkspaceSummaryReadModel(0, 0, 0, 0, 0, 0, null));

    public Task<IReadOnlyList<ProcessOeeWindowSummaryReadModel>> ListWindowsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeWindowListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProcessOeeWindowSummaryReadModel>>([]);

    public Task<ProcessOeeWindowDetailReadModel?> GetWindowByIdAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeWindowDetailReadModel?>(null);

    public Task<IReadOnlyList<ProcessOeeAssessmentSummaryReadModel>> ListAssessmentsAsync(
        ProcessOeeOperationContext context,
        ProcessOeeAssessmentListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProcessOeeAssessmentSummaryReadModel>>([]);

    public Task<ProcessOeeAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeAssessmentDetailReadModel?>(null);

    public Task<ProcessOeeAssessmentDetailReadModel?> GetCurrentAssessmentForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeAssessmentDetailReadModel?>(null);

    public Task<ProcessOeeWriteResult> CreateWindowAsync(
        ProcessOeeOperationContext context,
        CreateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ProcessOeeWriteResult> UpdateWindowAsync(
        ProcessOeeOperationContext context,
        UpdateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ProcessOeeWriteResult> EvaluateWindowAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeWindowCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ProcessOeeWriteResult> EvaluateBoundedAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeBoundedCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ProcessOeeLossOverviewReadModel> GetLossOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProcessOeeLossOverviewReadModel(0, 0, 0, 0, 0, 0, ProcessOeeLossConstants.RuleSetVersion, ProcessOeeLossConstants.LeadingLossMethod));

    public Task<ProcessOeeLossDetailReadModel?> GetLossDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeLossDetailReadModel?>(null);

    public Task<ProcessOeeLossDetailReadModel?> GetLossDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeLossDetailReadModel?>(null);

    public Task<ProcessOeeStabilityOverviewReadModel> GetStabilityOverviewAsync(
        ProcessOeeOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProcessOeeStabilityOverviewReadModel(0, 0, 0, 0, 0, 0, ProcessOeeStabilityConstants.RuleSetVersion));

    public Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeStabilityDetailReadModel?>(null);

    public Task<ProcessOeeStabilityDetailReadModel?> GetStabilityDetailForWindowAsync(
        ProcessOeeOperationContext context,
        Guid windowId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeStabilityDetailReadModel?>(null);

    public Task<ProcessOeeDowntimeParetoReadModel?> GetDowntimeParetoForAssessmentAsync(
        ProcessOeeOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProcessOeeDowntimeParetoReadModel?>(null);

    public Task<ProcessOeeShiftComparisonReadModel> GetShiftComparisonAsync(
        ProcessOeeOperationContext context,
        Guid? machineAssetId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProcessOeeShiftComparisonReadModel(
            periodStartUtc,
            periodEndUtc,
            false,
            "unavailable",
            ProcessOeeStabilityConstants.RuleSetVersion,
            null,
            []));

    public Task<ProcessOeeWriteResult> EvaluateShiftComparisonAsync(
        ProcessOeeOperationContext context,
        EvaluateProcessOeeShiftComparisonCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
