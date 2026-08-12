using System.Text.Json;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProcessOee;

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
}

public sealed class ProcessOeeRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ProcessOeeEvidenceCollector evidenceCollector) : IProcessOeeRepository
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
}
