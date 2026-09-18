namespace NevaxisZentra.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceWorkerFlowEngine
{
    Task<DigitalWorkCase> RunDeterministicFlowAsync(
        Guid caseId,
        string eventType,
        string subjectId,
        CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceWorkerFlowEngine : IDigitalWorkforceWorkerFlowEngine
{
    private readonly IDigitalWorkforceRuntimeService runtime;
    private readonly IDigitalWorkforceTaskCenterBridge taskCenterBridge;
    private readonly IDigitalWorkforceRepository repository;
    private readonly DigitalWorkforceModelRouterUsage meter;

    public DigitalWorkforceWorkerFlowEngine(
        IDigitalWorkforceRuntimeService runtime,
        IDigitalWorkforceTaskCenterBridge taskCenterBridge,
        IDigitalWorkforceRepository repository,
        DigitalWorkforceModelRouterUsage meter)
    {
        this.runtime = runtime;
        this.taskCenterBridge = taskCenterBridge;
        this.repository = repository;
        this.meter = meter;
    }

    public async Task<DigitalWorkCase> RunDeterministicFlowAsync(
        Guid caseId,
        string eventType,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var route = DigitalWorkforceModelRouter.Route(
            DigitalWorkforceRoutingPurpose.EventTriage);
        await meter.RecordIfNeededAsync(route, caseId, null, cancellationToken);

        var workCase = await repository.GetCaseAsync(caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = workCase.PrimaryWorker
            ?? await repository.GetWorkerAsync(workCase.PrimaryWorkerId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");

        await StepAsync(caseId, DigitalWorkCaseStatuses.Analyzing, "analyze", null, subjectId, cancellationToken);
        await StepAsync(caseId, DigitalWorkCaseStatuses.Deciding, "decide", null, subjectId, cancellationToken);
        await StepAsync(caseId, DigitalWorkCaseStatuses.Planning, "plan", worker.Code switch
        {
            DigitalWorkforceWorkerCodes.MaintenanceResolution => DigitalWorkforceToolCapabilities.MaintenanceCreate,
            DigitalWorkforceWorkerCodes.MaterialShortage => DigitalWorkforceToolCapabilities.ProcurementDraft,
            DigitalWorkforceWorkerCodes.QualityNcr => null,
            DigitalWorkforceWorkerCodes.MoldLifecycle => DigitalWorkforceToolCapabilities.MoldManage,
            DigitalWorkforceWorkerCodes.SupplierRecovery => DigitalWorkforceToolCapabilities.ProcurementDraft,
            DigitalWorkforceWorkerCodes.FactoryCoordinator => null,
            DigitalWorkforceWorkerCodes.ThroughputOptimizer => DigitalWorkforceToolCapabilities.ThroughputSimulate,
            _ => DigitalWorkforceToolCapabilities.ProductionPlan,
        }, subjectId, cancellationToken);

        if (RequiresApproval(eventType, worker))
        {
            await StepAsync(caseId, DigitalWorkCaseStatuses.AwaitingApproval, "approve", null, subjectId, cancellationToken);
            workCase = await repository.GetCaseAsync(caseId, cancellationToken);
            if (workCase is not null)
            {
                await taskCenterBridge.EnsureLinkedTaskAsync(
                    workCase,
                    worker,
                    workCase.FacilityId ?? worker.FacilityId ?? Guid.Empty,
                    cancellationToken);

                var pending = (await repository.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken))
                    .FirstOrDefault(a => a.CaseId == caseId);
                if (pending is not null &&
                    string.Equals(workCase.SourceClassification, DigitalWorkforceSourceClassifications.Demo, StringComparison.OrdinalIgnoreCase))
                {
                    await runtime.DecideApprovalAsync(
                        pending.Id,
                        new ApprovalDecisionRequest("approved", "Demo flow auto-approval", pending.ConcurrencyToken),
                        subjectId,
                        cancellationToken);
                }
            }
        }

        await StepAsync(
            caseId,
            DigitalWorkCaseStatuses.Executing,
            "execute",
            ResolveExecuteTool(worker.Code),
            subjectId,
            cancellationToken);
        await RecordVerificationForWorkerAsync(caseId, worker.Code, subjectId, cancellationToken);
        await StepAsync(caseId, DigitalWorkCaseStatuses.Verifying, "verify", null, subjectId, cancellationToken);
        if (string.Equals(worker.Code, DigitalWorkforceWorkerCodes.FactoryCoordinator, StringComparison.OrdinalIgnoreCase))
        {
            var coordinatorCase = await repository.GetCaseAsync(caseId, cancellationToken);
            if (coordinatorCase is not null)
            {
                coordinatorCase.MetadataJson = """{"allSubcasesResolved":true}""";
                await repository.SaveChangesAsync(cancellationToken);
            }
        }

        return await StepAsync(caseId, DigitalWorkCaseStatuses.Closed, "close", null, subjectId, cancellationToken);
    }

    private static bool RequiresApproval(string eventType, DigitalWorker worker) =>
        worker.AutonomyMode is DigitalWorkforceAutonomyModes.Draft or DigitalWorkforceAutonomyModes.Limited ||
        eventType.Contains("procurement", StringComparison.OrdinalIgnoreCase) ||
        eventType.Contains("quality", StringComparison.OrdinalIgnoreCase);

    private static string ResolveExecuteTool(string workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.MaintenanceResolution => DigitalWorkforceToolCapabilities.MaintenanceCreate,
        DigitalWorkforceWorkerCodes.MaterialShortage => DigitalWorkforceToolCapabilities.ProcurementDraft,
        DigitalWorkforceWorkerCodes.QualityNcr => DigitalWorkforceToolCapabilities.QualityManage,
        DigitalWorkforceWorkerCodes.MoldLifecycle => DigitalWorkforceToolCapabilities.MoldManage,
        DigitalWorkforceWorkerCodes.ThroughputOptimizer => DigitalWorkforceToolCapabilities.ThroughputSimulate,
        _ => DigitalWorkforceToolCapabilities.ProductionPlan,
    };

    private async Task RecordVerificationForWorkerAsync(
        Guid caseId, string workerCode, string subjectId, CancellationToken cancellationToken)
    {
        var verificationType = workerCode switch
        {
            DigitalWorkforceWorkerCodes.ProductionRecovery => "production-plan-validated",
            DigitalWorkforceWorkerCodes.MaintenanceResolution => "machine-operational",
            DigitalWorkforceWorkerCodes.MaterialShortage => "material-available",
            DigitalWorkforceWorkerCodes.QualityNcr => "quality-outcome-verified",
            DigitalWorkforceWorkerCodes.SupplierRecovery => "supply-risk-resolved",
            DigitalWorkforceWorkerCodes.MoldLifecycle => "mold-production-ready",
            DigitalWorkforceWorkerCodes.FactoryCoordinator => "coordinator-subcases-resolved",
            DigitalWorkforceWorkerCodes.ThroughputOptimizer => "throughput-target-met",
            _ => "outcome-verified",
        };

        await runtime.RecordVerificationAsync(
            caseId,
            new RecordVerificationRequest(
                verificationType,
                "deterministic-flow",
                "resolved",
                "resolved",
                DigitalWorkforceSourceClassifications.Demo,
                0.99m,
                DigitalWorkVerificationResults.Passed),
            subjectId,
            cancellationToken);
    }

    private async Task<DigitalWorkCase> StepAsync(
        Guid caseId,
        string targetStatus,
        string action,
        string? tool,
        string subjectId,
        CancellationToken cancellationToken) =>
        await runtime.AdvanceCaseAsync(
            caseId,
            new AdvanceWorkCaseRequest(targetStatus, action, tool, null, 2, false),
            subjectId,
            cancellationToken);
}

public sealed class DigitalWorkforceModelRouterUsage
{
    private readonly IDigitalWorkforceRuntimeService runtime;

    public DigitalWorkforceModelRouterUsage(IDigitalWorkforceRuntimeService runtime) =>
        this.runtime = runtime;

    public async Task RecordIfNeededAsync(
        DigitalWorkforceModelRoute route,
        Guid? caseId,
        Guid? workerId,
        CancellationToken cancellationToken)
    {
        if (!route.RequiresAiInvocation || workerId is null)
        {
            return;
        }

        await runtime.RecordAiUsageAsync(
            new RecordAiUsageRequest(
                workerId.Value,
                caseId,
                route.Tier,
                route.ModelName,
                120,
                40,
                route.Tier switch
                {
                    DigitalWorkforceModelTiers.Fast => 0.01m,
                    DigitalWorkforceModelTiers.Advanced => 0.08m,
                    _ => 0.03m,
                }),
            cancellationToken);
    }
}
