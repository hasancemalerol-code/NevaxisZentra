using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NevaxisFactOS.Persistence.Development;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;
using NevaxisFactOS.Persistence.FactoryIntelligence;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryDecisionCenter;

public interface IFactoryDecisionCenterService
{
    Task<FactoryDecisionCenterWorkspaceSnapshot> GetWorkspaceAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryDecisionCaseDetailSnapshot> GetCaseDetailAsync(Guid caseId, CancellationToken cancellationToken);
    Task<FactoryDecisionCaseDetailSnapshot> DecideApprovalAsync(
        Guid caseId, Guid approvalId, FactoryDecisionCenterApprovalRequest request, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecisionCaseDetailSnapshot> AdvanceExecutionAsync(
        Guid caseId, long concurrencyToken, string subjectId, CancellationToken cancellationToken);
    Task<FactoryDecisionCaseDetailSnapshot> VerifyOutcomeAsync(
        Guid caseId, FactoryDecisionCenterVerifyRequest request, string subjectId, CancellationToken cancellationToken);
    Task ResetScenarioAsync(Guid facilityId, string subjectId, CancellationToken cancellationToken);
    Task<FactoryRecoveryHistoricalCaseDetail> GetHistoricalLearningCaseAsync(
        Guid learningRecordId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryRecoveryHistoricalCaseDetail>> SearchHistoricalLearningCasesAsync(
        Guid facilityId, FactoryRecoveryLearningSearchFilters filters, CancellationToken cancellationToken);
}

public sealed class FactoryDecisionCenterService : IFactoryDecisionCenterService
{
    private readonly IFactoryDecisionCenterRepository repository;
    private readonly IFactoryDecisionCenterAiTaskSyncService aiTaskSync;
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IHostEnvironment environment;
    private readonly IFactoryOutcomeExecutionService outcomeExecution;
    private readonly IFactoryRecoveryIntelligenceService recoveryIntelligence;
    private readonly IFactoryIntelligenceService factoryIntelligence;

    public FactoryDecisionCenterService(
        IFactoryDecisionCenterRepository repository,
        IFactoryDecisionCenterAiTaskSyncService aiTaskSync,
        IDigitalWorkforceRuntimeService runtime,
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        IHostEnvironment environment,
        IFactoryOutcomeExecutionService outcomeExecution,
        IFactoryRecoveryIntelligenceService recoveryIntelligence,
        IFactoryIntelligenceService factoryIntelligence)
    {
        this.repository = repository;
        this.aiTaskSync = aiTaskSync;
        this.runtime = runtime;
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.environment = environment;
        this.outcomeExecution = outcomeExecution;
        this.recoveryIntelligence = recoveryIntelligence;
        this.factoryIntelligence = factoryIntelligence;
    }

    public Task<FactoryDecisionCenterWorkspaceSnapshot> GetWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryDecisionCaseDetailSnapshot> GetCaseDetailAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCaseDetailCoreAsync(caseId, ct),
            cancellationToken);

    public Task<FactoryDecisionCaseDetailSnapshot> DecideApprovalAsync(
        Guid caseId, Guid approvalId, FactoryDecisionCenterApprovalRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DecideApprovalCoreAsync(caseId, approvalId, request, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecisionCaseDetailSnapshot> AdvanceExecutionAsync(
        Guid caseId, long concurrencyToken, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => AdvanceExecutionCoreAsync(caseId, concurrencyToken, subjectId, ct),
            cancellationToken);

    public Task<FactoryDecisionCaseDetailSnapshot> VerifyOutcomeAsync(
        Guid caseId, FactoryDecisionCenterVerifyRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => VerifyOutcomeCoreAsync(caseId, request, subjectId, ct),
            cancellationToken);

    public Task ResetScenarioAsync(Guid facilityId, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!environment.IsDevelopment() || facilityId != DevelopmentPilotIds.FacilityId)
                {
                    throw new FactoryDecisionCenterException(
                        FactoryDecisionCenterFailureCodes.DevelopmentOnly,
                        "Bu işlem yalnızca geliştirme ortamında kullanılabilir.");
                }

                await DevelopmentPilotFactoryDecisionCenterSeed.ResetCriticalOrderRecoveryAsync(
                    dbContext, DevelopmentPilotIds.TenantId, facilityId, subjectId, DateTimeOffset.UtcNow, ct);
                await aiTaskSync.ResetLinkedTasksAsync(
                    DevelopmentPilotIds.TenantId, facilityId, subjectId, DateTimeOffset.UtcNow, ct);
                return true;
            },
            cancellationToken);

    private async Task<FactoryDecisionCenterWorkspaceSnapshot> GetWorkspaceCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var cases = await repository.ListDecisionCasesAsync(facilityId, cancellationToken);
        var queue = new List<FactoryDecisionCaseQueueItem>();
        var childrenByRoot = new Dictionary<Guid, IReadOnlyList<DigitalWorkCase>>();

        foreach (var rootCase in cases)
        {
            var children = await repository.ListChildCasesAsync(rootCase.Id, cancellationToken);
            childrenByRoot[rootCase.Id] = children;
            queue.Add(FactoryDecisionCenterProjectionEngine.ProjectQueueItem(rootCase, children));
        }

        var earlyWarningCards = await factoryIntelligence.GetEarlyWarningCardsAsync(facilityId, cancellationToken);

        return new FactoryDecisionCenterWorkspaceSnapshot(
            FactoryDecisionCenterProjectionEngine.BuildSummary(cases),
            queue,
            FactoryDecisionCenterProjectionEngine.BuildLiveQueues(
                cases,
                rootCase => FactoryDecisionCenterProjectionEngine.ProjectQueueItem(
                    rootCase,
                    childrenByRoot.GetValueOrDefault(rootCase.Id, []))),
            FactoryIntelligenceDecisionCenterProjection.MapQueues(earlyWarningCards),
            DateTimeOffset.UtcNow);
    }

    private async Task<FactoryDecisionCaseDetailSnapshot> GetCaseDetailCoreAsync(
        Guid caseId, CancellationToken cancellationToken)
    {
        var rootCase = await repository.GetDecisionCaseAsync(caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");

        if (!FactoryDecisionCenterProjectionEngine.IsDecisionCenterCase(rootCase))
        {
            throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");
        }

        var children = await repository.ListChildCasesAsync(caseId, cancellationToken);
        var verifications = await repository.ListVerificationsAsync(caseId, cancellationToken);
        var pendingApproval = await repository.GetPendingApprovalAsync(caseId, cancellationToken);
        var facilityLabel = await repository.GetFacilityLabelAsync(rootCase.FacilityId ?? DevelopmentPilotIds.FacilityId, cancellationToken)
            ?? "Pilot Tesis";

        var intelligence = await recoveryIntelligence.BuildForCaseAsync(rootCase, children, cancellationToken);

        return FactoryDecisionCenterProjectionEngine.ProjectDetail(
            rootCase, children, verifications, pendingApproval, facilityLabel, intelligence);
    }

    public Task<FactoryRecoveryHistoricalCaseDetail> GetHistoricalLearningCaseAsync(
        Guid learningRecordId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => recoveryIntelligence.GetHistoricalCaseAsync(learningRecordId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryRecoveryHistoricalCaseDetail>> SearchHistoricalLearningCasesAsync(
        Guid facilityId, FactoryRecoveryLearningSearchFilters filters, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => recoveryIntelligence.SearchHistoricalCasesAsync(facilityId, filters, ct),
            cancellationToken);

    private async Task<FactoryDecisionCaseDetailSnapshot> DecideApprovalCoreAsync(
        Guid caseId, Guid approvalId, FactoryDecisionCenterApprovalRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var rootCase = await repository.GetDecisionCaseAsync(caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");

        if (rootCase.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        var pendingApproval = await repository.GetPendingApprovalAsync(caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Bekleyen onay bulunamadı.");

        if (pendingApproval.Id != approvalId)
        {
            throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Bekleyen onay bulunamadı.");
        }

        await runtime.DecideApprovalAsync(
            approvalId,
            new ApprovalDecisionRequest(
                request.Approved ? "approved" : "rejected",
                request.Note,
                pendingApproval.ConcurrencyToken),
            subjectId,
            cancellationToken);

        rootCase = await repository.GetDecisionCaseAsync(caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");

        if (request.Approved)
        {
            rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.None;
            rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            rootCase.ConcurrencyToken++;
            await dbContext.SaveChangesAsync(cancellationToken);
            await aiTaskSync.SyncAfterApprovalDecisionAsync(rootCase, true, subjectId, cancellationToken);
        }
        else
        {
            await aiTaskSync.SyncAfterApprovalDecisionAsync(rootCase, false, subjectId, cancellationToken);
        }

        return await GetCaseDetailCoreAsync(caseId, cancellationToken);
    }

    private async Task<FactoryDecisionCaseDetailSnapshot> AdvanceExecutionCoreAsync(
        Guid caseId, long concurrencyToken, string subjectId, CancellationToken cancellationToken)
    {
        var rootCase = await dbContext.DigitalWorkCases
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");

        if (rootCase.ConcurrencyToken != concurrencyToken)
        {
            throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        if (rootCase.Status == DigitalWorkCaseStatuses.AwaitingApproval ||
            rootCase.Approvals.Any(a => a.Status == DigitalWorkApprovalStatuses.Pending))
        {
            throw new FactoryDecisionCenterException(
                FactoryDecisionCenterFailureCodes.ApprovalRequired,
                "Kurtarma planı onaylanmadan yürütme başlatılamaz.");
        }

        if (IsOutcomeExecutionCase(rootCase))
        {
            await outcomeExecution.ExecutePendingActionsAsync(caseId, subjectId, cancellationToken);
            return await GetCaseDetailCoreAsync(caseId, cancellationToken);
        }

        var children = await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == caseId && c.Id != caseId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var child in children.Where(c => c.Status != DigitalWorkCaseStatuses.Closed))
        {
            child.Status = DigitalWorkCaseStatuses.Executing;
            child.CurrentStage = DigitalWorkCaseStages.Act;
            child.UpdatedAtUtc = DateTimeOffset.UtcNow;
            child.ConcurrencyToken++;

            await runtime.RecordVerificationAsync(
                child.Id,
                new RecordVerificationRequest(
                    ResolveVerificationType(child.PrimaryWorker?.Code),
                    "factory-decision-center",
                    "completed",
                    "completed",
                    child.SourceClassification,
                    0.95m,
                    DigitalWorkVerificationResults.Passed),
                subjectId,
                cancellationToken);

            child.Status = DigitalWorkCaseStatuses.Closed;
            child.VerificationState = DigitalWorkVerificationResults.Passed;
            child.ClosedAtUtc = DateTimeOffset.UtcNow;
            child.UpdatedAtUtc = DateTimeOffset.UtcNow;
            child.ConcurrencyToken++;
        }

        rootCase.Status = DigitalWorkCaseStatuses.Verifying;
        rootCase.CurrentStage = DigitalWorkCaseStages.Verify;
        rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingMachineVerification;
        rootCase.MetadataJson = UpdateMetadataForExecution(rootCase.MetadataJson);
        rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        rootCase.ConcurrencyToken++;

        await dbContext.SaveChangesAsync(cancellationToken);
        await aiTaskSync.SyncAfterExecutionProgressAsync(rootCase, children, subjectId, cancellationToken);

        return await GetCaseDetailCoreAsync(caseId, cancellationToken);
    }

    private async Task<FactoryDecisionCaseDetailSnapshot> VerifyOutcomeCoreAsync(
        Guid caseId, FactoryDecisionCenterVerifyRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var rootCase = await dbContext.DigitalWorkCases
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.NotFound, "Karar vakası bulunamadı.");

        if (rootCase.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new FactoryDecisionCenterException(FactoryDecisionCenterFailureCodes.ConcurrencyConflict, "Eşzamanlılık çakışması.");
        }

        var children = await dbContext.DigitalWorkCases
            .Where(c => c.OrchestrationRootCaseId == caseId && c.Id != caseId)
            .ToListAsync(cancellationToken);

        if (children.Any(c => c.Status != DigitalWorkCaseStatuses.Closed))
        {
            throw new FactoryDecisionCenterException(
                FactoryDecisionCenterFailureCodes.TasksNotComplete,
                "Tüm kurtarma görevleri tamamlanmadan sonuç doğrulanamaz.");
        }

        if (IsOutcomeExecutionCase(rootCase))
        {
            try
            {
                await outcomeExecution.VerifyAndCloseAsync(
                    caseId,
                    request.VerifiedProjection,
                    request.ConcurrencyToken,
                    subjectId,
                    cancellationToken);
            }
            catch (FactoryOutcomeExecutionException ex) when (
                ex.FailureCode == FactoryOutcomeExecutionFailureCodes.VerificationFailed)
            {
                throw new FactoryDecisionCenterException(
                    FactoryDecisionCenterFailureCodes.OutcomeNotVerified,
                    ex.Message);
            }

            return await GetCaseDetailCoreAsync(caseId, cancellationToken);
        }

        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(rootCase.MetadataJson);
        var required = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.RequiredQuantity, out var requiredValue) &&
                       requiredValue.TryGetInt32(out var requiredQty)
            ? requiredQty
            : FactoryDecisionCenterConstants.RequiredQuantity;
        var verifiedProjection = FactoryDecisionCenterConstants.RecoveredProjectedQuantity;

        if (verifiedProjection < required)
        {
            throw new FactoryDecisionCenterException(
                FactoryDecisionCenterFailureCodes.OutcomeNotVerified,
                "Doğrulanan projeksiyon gerekli çıktıyı karşılamıyor.");
        }

        rootCase.MetadataJson = UpdateMetadataForVerification(rootCase.MetadataJson, verifiedProjection);
        rootCase.VerificationState = DigitalWorkVerificationResults.Passed;
        rootCase.VerifiedAtUtc = DateTimeOffset.UtcNow;
        rootCase.Status = DigitalWorkCaseStatuses.Closed;
        rootCase.CurrentStage = DigitalWorkCaseStages.Close;
        rootCase.ClosedAtUtc = DateTimeOffset.UtcNow;
        rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.None;
        rootCase.Outcome = "verified";
        rootCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        rootCase.ConcurrencyToken++;

        await runtime.RecordVerificationAsync(
            caseId,
            new RecordVerificationRequest(
                "order-delivery-protected",
                "factory-decision-center",
                required.ToString(CultureInfo.InvariantCulture),
                verifiedProjection.ToString(CultureInfo.InvariantCulture),
                rootCase.SourceClassification,
                0.96m,
                DigitalWorkVerificationResults.Passed),
            subjectId,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await aiTaskSync.SyncAfterOutcomeVerifiedAsync(rootCase, subjectId, cancellationToken);

        return await GetCaseDetailCoreAsync(caseId, cancellationToken);
    }

    private static bool IsOutcomeExecutionCase(DigitalWorkCase rootCase) =>
        string.Equals(rootCase.Source, FactoryOutcomeExecutionConstants.SourceModule, StringComparison.OrdinalIgnoreCase);

    private static string ResolveVerificationType(string? workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.ProductionRecovery => "production-plan-validated",
        DigitalWorkforceWorkerCodes.MaintenanceResolution => "machine-operational",
        DigitalWorkforceWorkerCodes.MaterialShortage => "material-available",
        _ => "outcome-verified",
    };

    private static string UpdateMetadataForExecution(string metadataJson)
    {
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(metadataJson);
        var dict = metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
        dict[FactoryDecisionCenterMetadataKeys.ProjectedQuantity] = JsonSerializer.SerializeToElement(456);
        dict["allSubcasesResolved"] = JsonSerializer.SerializeToElement(false);
        return JsonSerializer.Serialize(dict);
    }

    private static string UpdateMetadataForVerification(string metadataJson, int verifiedProjection)
    {
        var metadata = FactoryDecisionCenterProjectionEngine.ParseMetadata(metadataJson);
        var dict = metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
        dict[FactoryDecisionCenterMetadataKeys.VerifiedProjection] = JsonSerializer.SerializeToElement(verifiedProjection);
        dict[FactoryDecisionCenterMetadataKeys.ProjectedQuantity] = JsonSerializer.SerializeToElement(verifiedProjection);
        dict[FactoryDecisionCenterMetadataKeys.GapQuantity] = JsonSerializer.SerializeToElement(0);
        dict[FactoryDecisionCenterMetadataKeys.DeliveryProtected] = JsonSerializer.SerializeToElement(true);
        dict[FactoryDecisionCenterMetadataKeys.RecoveryGain] =
            JsonSerializer.SerializeToElement(verifiedProjection - FactoryDecisionCenterConstants.RequiredQuantity);
        dict["allSubcasesResolved"] = JsonSerializer.SerializeToElement(true);
        return JsonSerializer.Serialize(dict);
    }
}
