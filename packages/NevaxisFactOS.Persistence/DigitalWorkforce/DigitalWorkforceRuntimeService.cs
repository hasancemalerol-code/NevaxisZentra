using System.Text.Json;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceRuntimeService
{
    Task<DigitalWorkforceControlCenterSnapshot> GetControlCenterAsync(CancellationToken cancellationToken);
    Task<DigitalWorker?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken);
    Task UpdateAutonomyAsync(Guid workerId, UpdateAutonomyRequest request, string subjectId, CancellationToken cancellationToken);
    Task UpdatePolicyAsync(Guid workerId, UpdatePolicyRequest request, string subjectId, CancellationToken cancellationToken);
    Task PauseWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken);
    Task ResumeWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken);
    Task KillWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken);
    Task GlobalPauseAsync(string subjectId, CancellationToken cancellationToken);
    Task GlobalResumeAsync(string subjectId, CancellationToken cancellationToken);
    Task GlobalKillAsync(string subjectId, CancellationToken cancellationToken);
    Task GlobalSuggestModeAsync(string subjectId, CancellationToken cancellationToken);
    Task ApplyDefaultPoliciesAsync(string subjectId, CancellationToken cancellationToken);
    Task ResumeHumanControlAsync(Guid caseId, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkforceWorkerDetailSnapshot> GetWorkerDetailAsync(Guid workerId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> TriggerCaseAsync(TriggerWorkCaseRequest request, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> AdvanceCaseAsync(Guid caseId, AdvanceWorkCaseRequest request, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkCase> TakeoverCaseAsync(Guid caseId, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkApproval> DecideApprovalAsync(Guid approvalId, ApprovalDecisionRequest request, string subjectId, CancellationToken cancellationToken);
    Task<DigitalWorkHandoff> HandoffAsync(Guid caseId, HandoffRequest request, string subjectId, CancellationToken cancellationToken);
    Task RecordVerificationAsync(Guid caseId, RecordVerificationRequest request, string subjectId, CancellationToken cancellationToken);
    Task RecordAiUsageAsync(RecordAiUsageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkCaseSummary>> ListCasesAsync(string? status, Guid? workerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkActivitySummary>> ListActivityAsync(Guid? workerId, Guid? caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkApprovalSummary>> ListApprovalsAsync(string? status, CancellationToken cancellationToken);
    Task<DigitalWorkforceMetrics> GetMetricsAsync(CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceRuntimeService : IDigitalWorkforceRuntimeService
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly IDigitalWorkforceRepository repository;
    private readonly IDigitalWorkforceExecutionRuntimeService executionRuntime;
    private readonly IAuditWriter auditWriter;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public DigitalWorkforceRuntimeService(
        NevaxisFactOSDbContext dbContext,
        IDigitalWorkforceRepository repository,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        IAuditWriter auditWriter,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.repository = repository;
        this.executionRuntime = executionRuntime;
        this.auditWriter = auditWriter;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<DigitalWorkforceControlCenterSnapshot> GetControlCenterAsync(CancellationToken cancellationToken) =>
        repository.GetControlCenterSnapshotAsync(cancellationToken);

    public Task<DigitalWorker?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken) =>
        repository.GetWorkerAsync(workerId, cancellationToken);

    public async Task UpdateAutonomyAsync(
        Guid workerId, UpdateAutonomyRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        EnsureConcurrency(worker.ConcurrencyToken, request.ConcurrencyToken);

        if (!IsValidAutonomyMode(request.AutonomyMode))
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.ValidationFailed, "Invalid autonomy mode.");
        }

        worker.AutonomyMode = request.AutonomyMode;
        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        worker.ConcurrencyToken++;

        await WriteAuditAsync("digital-workforce.autonomy.changed", "update", "digital-worker",
            workerId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePolicyAsync(
        Guid workerId, UpdatePolicyRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        var policy = worker.Policy ?? throw new DigitalWorkforceException(
            DigitalWorkforceFailureCodes.NotFound, "Worker policy not found.");

        EnsureConcurrency(policy.ConcurrencyToken, request.ConcurrencyToken);

        policy.AllowedToolsJson = request.AllowedToolsJson;
        policy.AllowedActionsJson = request.AllowedActionsJson;
        policy.ProhibitedActionsJson = request.ProhibitedActionsJson;
        policy.ApprovalRequiredForJson = request.ApprovalRequiredForJson;
        policy.MaxTransactionValue = request.MaxTransactionValue;
        policy.MaxRiskLevel = request.MaxRiskLevel;
        policy.ExternalCommunicationPolicy = request.ExternalCommunicationPolicy;
        policy.AiBudgetDaily = request.AiBudgetDaily;
        policy.AiBudgetMonthly = request.AiBudgetMonthly;
        policy.UpdatedAtUtc = DateTimeOffset.UtcNow;
        policy.ConcurrencyToken++;

        if (worker.Budget is not null)
        {
            worker.Budget.DailyLimit = request.AiBudgetDaily;
            worker.Budget.MonthlyLimit = request.AiBudgetMonthly;
            worker.Budget.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await WriteAuditAsync("digital-workforce.policy.changed", "update", "digital-worker-policy",
            workerId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task PauseWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        EnsureConcurrency(worker.ConcurrencyToken, concurrencyToken);
        worker.IsPaused = true;
        worker.Status = DigitalWorkforceWorkerStatuses.Paused;
        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        worker.ConcurrencyToken++;
        await executionRuntime.PauseWorkerExecutionsAsync(workerId, cancellationToken);
        await WriteAuditAsync("digital-workforce.worker.paused", "pause", "digital-worker",
            workerId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);
        if (runtime.GlobalKillActive || worker.IsKilled)
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.KillSwitchActive, "Cannot resume while kill switch is active.");
        }

        EnsureConcurrency(worker.ConcurrencyToken, concurrencyToken);
        worker.IsPaused = false;
        worker.Status = DigitalWorkforceWorkerStatuses.Running;
        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        worker.ConcurrencyToken++;
        await WriteAuditAsync("digital-workforce.worker.resumed", "resume", "digital-worker",
            workerId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task KillWorkerAsync(Guid workerId, long concurrencyToken, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        EnsureConcurrency(worker.ConcurrencyToken, concurrencyToken);
        worker.IsKilled = true;
        worker.IsPaused = true;
        worker.Status = DigitalWorkforceWorkerStatuses.Killed;
        if (worker.Policy is not null)
        {
            worker.Policy.KillSwitch = true;
        }

        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        worker.ConcurrencyToken++;

        await EscalateActiveCasesForWorkerAsync(worker, subjectId, cancellationToken);
        await executionRuntime.CancelWorkerExecutionsAsync(workerId, "worker-killed", cancellationToken);
        await WriteAuditAsync("digital-workforce.worker.killed", "kill", "digital-worker",
            workerId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task GlobalPauseAsync(string subjectId, CancellationToken cancellationToken)
    {
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);
        runtime.GlobalPauseActive = true;
        runtime.UpdatedAtUtc = DateTimeOffset.UtcNow;
        runtime.ConcurrencyToken++;

        var workers = await repository.ListWorkersAsync(cancellationToken);
        foreach (var worker in workers)
        {
            worker.GlobalPauseActive = true;
            worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await WriteAuditAsync("digital-workforce.global.paused", "pause", "digital-workforce-runtime",
            "runtime", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task GlobalResumeAsync(string subjectId, CancellationToken cancellationToken)
    {
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);
        var recoveringFromKill = runtime.GlobalKillActive;
        runtime.GlobalKillActive = false;
        runtime.GlobalPauseActive = false;
        runtime.UpdatedAtUtc = DateTimeOffset.UtcNow;
        runtime.ConcurrencyToken++;

        var workers = await repository.ListWorkersAsync(cancellationToken);
        foreach (var worker in workers)
        {
            worker.GlobalPauseActive = false;
            worker.IsPaused = false;
            worker.IsKilled = false;
            if (worker.Policy is not null)
            {
                worker.Policy.KillSwitch = false;
            }

            worker.Status = DigitalWorkforceWorkerStatuses.Running;
            worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await WriteAuditAsync(
            recoveringFromKill ? "digital-workforce.global.recovered" : "digital-workforce.global.resumed",
            recoveringFromKill ? "recover" : "resume",
            "digital-workforce-runtime",
            "runtime",
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task GlobalKillAsync(string subjectId, CancellationToken cancellationToken)
    {
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);
        runtime.GlobalKillActive = true;
        runtime.GlobalPauseActive = true;
        runtime.UpdatedAtUtc = DateTimeOffset.UtcNow;
        runtime.ConcurrencyToken++;

        var workers = await repository.ListWorkersAsync(cancellationToken);
        foreach (var worker in workers)
        {
            worker.IsKilled = true;
            worker.IsPaused = true;
            worker.GlobalPauseActive = true;
            worker.Status = DigitalWorkforceWorkerStatuses.Killed;
            if (worker.Policy is not null)
            {
                worker.Policy.KillSwitch = true;
            }

            await EscalateActiveCasesForWorkerAsync(worker, subjectId, cancellationToken);
        }

        await WriteAuditAsync("digital-workforce.global.killed", "kill", "digital-workforce-runtime",
            "runtime", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task GlobalSuggestModeAsync(string subjectId, CancellationToken cancellationToken)
    {
        var workers = await repository.ListWorkersAsync(cancellationToken);
        foreach (var worker in workers.Where(w => !w.IsKilled))
        {
            worker.AutonomyMode = DigitalWorkforceAutonomyModes.Draft;
            worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
            worker.ConcurrencyToken++;
        }

        await WriteAuditAsync("digital-workforce.global.suggest-mode", "update", "digital-workforce-runtime",
            "runtime", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyDefaultPoliciesAsync(string subjectId, CancellationToken cancellationToken)
    {
        var workers = await repository.ListWorkersAsync(cancellationToken);
        foreach (var definition in DigitalWorkforceCanonicalWorkers.Definitions)
        {
            var worker = workers.FirstOrDefault(w => w.Id == definition.Id);
            if (worker?.Policy is null)
            {
                continue;
            }

            worker.AutonomyMode = definition.DefaultAutonomyMode;
            worker.Policy.AllowedToolsJson = System.Text.Json.JsonSerializer.Serialize(definition.DefaultTools);
            worker.Policy.AllowedActionsJson = System.Text.Json.JsonSerializer.Serialize(definition.DefaultTools);
            worker.Policy.ProhibitedActionsJson = System.Text.Json.JsonSerializer.Serialize(
                DigitalWorkforceToolCapabilities.MachineSafetyProhibitedActions);
            worker.Policy.UpdatedAtUtc = DateTimeOffset.UtcNow;
            worker.Policy.ConcurrencyToken++;
            worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
            worker.ConcurrencyToken++;
        }

        await WriteAuditAsync("digital-workforce.policies.applied", "update", "digital-workforce-runtime",
            "runtime", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeHumanControlAsync(Guid caseId, string subjectId, CancellationToken cancellationToken)
    {
        var workCase = await repository.GetCaseAsync(caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = await RequireWorkerAsync(workCase.PrimaryWorkerId, cancellationToken);

        workCase.IsHumanControlled = false;
        worker.IsHumanControlled = false;
        if (!worker.IsPaused && !worker.IsKilled)
        {
            worker.Status = DigitalWorkforceWorkerStatuses.Running;
        }

        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        workCase.ConcurrencyToken++;
        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;
        worker.ConcurrencyToken++;

        await AppendActivityAsync(worker.TenantId, caseId, worker.Id, "human.resume",
            "İnsan kontrolü sonlandırıldı; otonom akış devam edebilir.", subjectId, cancellationToken);
        await WriteAuditAsync("digital-workforce.human.resume", "resume", "digital-work-case",
            caseId.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<DigitalWorkforceWorkerDetailSnapshot> GetWorkerDetailAsync(
        Guid workerId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(workerId, cancellationToken);
        var cases = await repository.ListCasesAsync(null, workerId, cancellationToken);
        var activity = await repository.ListActivityAsync(workerId, null, cancellationToken);
        var approvals = await repository.ListApprovalsAsync(null, cancellationToken);
        var workerApprovals = approvals.Where(a => a.WorkerId == workerId).ToList();
        var openCases = cases.Where(c => c.Status != DigitalWorkCaseStatuses.Closed).ToList();
        var closedCases = cases.Where(c => c.Status == DigitalWorkCaseStatuses.Closed).ToList();
        var currentCase = openCases.FirstOrDefault();

        return new DigitalWorkforceWorkerDetailSnapshot(
            worker.Id,
            worker.Code,
            worker.Name,
            worker.Description,
            worker.Mission,
            worker.Status,
            worker.AutonomyMode,
            worker.IsPaused,
            worker.IsKilled,
            worker.IsHumanControlled,
            worker.SourceClassification,
            worker.ConcurrencyToken,
            worker.Policy,
            worker.Budget,
            currentCase,
            openCases,
            closedCases,
            activity,
            workerApprovals);
    }

    public async Task<DigitalWorkCase> TriggerCaseAsync(
        TriggerWorkCaseRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(request.PrimaryWorkerId, cancellationToken);
        await EnsureDispatchAllowedAsync(worker, cancellationToken);

        if (string.Equals(worker.AutonomyMode, DigitalWorkforceAutonomyModes.Observe, StringComparison.OrdinalIgnoreCase))
        {
            throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.AutonomyDenied,
                "Observe mode allows read-only.");
        }

        var now = DateTimeOffset.UtcNow;
        var workCase = new DigitalWorkCase
        {
            Id = Guid.NewGuid(),
            TenantId = worker.TenantId,
            FacilityId = request.FacilityId ?? worker.FacilityId,
            Title = request.Title,
            Description = request.Description,
            Source = request.Source,
            SourceClassification = request.SourceClassification,
            Severity = request.Severity,
            Status = DigitalWorkCaseStatuses.Detected,
            CurrentStage = DigitalWorkCaseStages.Detect,
            PrimaryWorkerId = worker.Id,
            AutonomyModeAtStart = worker.AutonomyMode,
            ClosureContractKey = DigitalWorkforceVerificationEngine.ResolveClosureContractKey(worker.Code),
            MetadataJson = request.MetadataJson ?? "{}",
            StartedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };

        workCase.Participants.Add(new DigitalWorkCaseParticipant
        {
            Id = Guid.NewGuid(),
            TenantId = worker.TenantId,
            CaseId = workCase.Id,
            WorkerId = worker.Id,
            Role = DigitalWorkforceParticipantRoles.Primary,
            JoinedAtUtc = now,
        });

        dbContext.DigitalWorkCases.Add(workCase);
        worker.Status = DigitalWorkforceWorkerStatuses.Running;
        worker.UpdatedAtUtc = now;

        await AppendActivityAsync(worker.TenantId, workCase.Id, worker.Id, "case.detected",
            request.Title, "detected", cancellationToken, request.SourceClassification);

        await repository.SaveChangesAsync(cancellationToken);
        return workCase;
    }

    public async Task<DigitalWorkCase> AdvanceCaseAsync(
        Guid caseId, AdvanceWorkCaseRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var workCase = await repository.GetCaseAsync(caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = workCase.PrimaryWorker ?? await RequireWorkerAsync(workCase.PrimaryWorkerId, cancellationToken);
        await EnsureDispatchAllowedAsync(worker, cancellationToken);

        if (workCase.IsHumanControlled)
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.Forbidden, "Case is under human control.");
        }

        if (!DigitalWorkCaseStateMachine.CanTransition(workCase.Status, request.TargetStatus, request.AllowEmergencyClose))
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.InvalidTransition,
                $"Transition {workCase.Status} -> {request.TargetStatus} denied.");
        }

        if (string.Equals(request.TargetStatus, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase))
        {
            if (DigitalWorkCaseStateMachine.RequiresVerificationForClosure(workCase.Status) &&
                !DigitalWorkforceVerificationEngine.CanCloseCase(workCase, workCase.Verifications))
            {
                throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.VerificationRequired,
                    "Verified closure required.");
            }
        }
        else if (RequiresOperationalPolicyCheck(request))
        {
            var policy = worker.Policy ?? throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.NotFound, "Worker policy not found.");
            var policyAction = ResolvePolicyAction(request);
            var policyEval = DigitalWorkforcePolicyEngine.EvaluateAction(
                worker, policy, policyAction, request.Tool, request.Amount, request.RiskLevel, workCase.FacilityId);
            if (!policyEval.Allowed)
            {
                throw new DigitalWorkforceException(policyEval.FailureCode ?? DigitalWorkforceFailureCodes.PolicyDenied,
                    policyEval.DenialReason ?? "Policy denied.");
            }

            var actionKind = ResolveActionKind(policyAction);
            var autonomyEval = DigitalWorkforceAutonomyEngine.Evaluate(worker, actionKind, policyEval.RequiresApproval);
            if (!autonomyEval.Allowed)
            {
                throw new DigitalWorkforceException(autonomyEval.FailureCode ?? DigitalWorkforceFailureCodes.AutonomyDenied,
                    autonomyEval.DenialReason ?? "Autonomy denied.");
            }

            if (autonomyEval.RequiresApproval || policyEval.RequiresApproval)
            {
                await CreatePendingApprovalAsync(workCase, worker, request, cancellationToken);
                request = request with { TargetStatus = DigitalWorkCaseStatuses.AwaitingApproval };
            }
        }

        workCase.Status = request.TargetStatus;
        workCase.CurrentStage = DigitalWorkCaseStateMachine.MapStatusToStage(request.TargetStatus);
        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        workCase.ConcurrencyToken++;

        if (string.Equals(request.TargetStatus, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase))
        {
            workCase.ClosedAtUtc = DateTimeOffset.UtcNow;
            workCase.VerificationState = DigitalWorkVerificationResults.Passed;
            workCase.VerifiedAtUtc = DateTimeOffset.UtcNow;
        }

        await AppendActivityAsync(worker.TenantId, workCase.Id, worker.Id, "case.transition",
            $"Durum {request.TargetStatus} olarak güncellendi.", request.TargetStatus, cancellationToken);

        await WriteAuditAsync("digital-workforce.case.transition", "transition", "digital-work-case",
            workCase.Id.ToString(), cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        if (string.Equals(request.TargetStatus, DigitalWorkCaseStatuses.Executing, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(request.Action))
        {
            await executionRuntime.EnqueueAsync(
                new EnqueueDigitalWorkExecutionRequest(
                    workCase.Id,
                    worker.Id,
                    request.Action!,
                    request.Tool,
                    JsonSerializer.Serialize(new
                    {
                        caseId = workCase.Id,
                        action = request.Action,
                        tool = request.Tool,
                        amount = request.Amount,
                        riskLevel = request.RiskLevel,
                    }),
                    request.Amount,
                    request.RiskLevel),
                subjectId,
                cancellationToken);
        }

        return workCase;
    }

    public async Task<DigitalWorkCase> TakeoverCaseAsync(Guid caseId, string subjectId, CancellationToken cancellationToken)
    {
        var workCase = await repository.GetCaseAsync(caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = await RequireWorkerAsync(workCase.PrimaryWorkerId, cancellationToken);

        workCase.IsHumanControlled = true;
        worker.IsHumanControlled = true;
        worker.Status = DigitalWorkforceWorkerStatuses.HumanControlled;
        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        workCase.ConcurrencyToken++;
        worker.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await AppendActivityAsync(worker.TenantId, workCase.Id, worker.Id, "human.takeover",
            "İş insan kontrolüne devredildi.", subjectId, cancellationToken);
        await WriteAuditAsync("digital-workforce.human.takeover", "takeover", "digital-work-case",
            workCase.Id.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return workCase;
    }

    public Task<DigitalWorkApproval> DecideApprovalAsync(
        Guid approvalId, ApprovalDecisionRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DecideApprovalCoreAsync(approvalId, request, subjectId, ct),
            cancellationToken);

    private async Task<DigitalWorkApproval> DecideApprovalCoreAsync(
        Guid approvalId, ApprovalDecisionRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var approval = await dbContext.DigitalWorkApprovals
            .Include(a => a.Case)
            .FirstOrDefaultAsync(a => a.Id == approvalId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Approval not found.");

        EnsureConcurrency(approval.ConcurrencyToken, request.ConcurrencyToken);
        var now = DateTimeOffset.UtcNow;

        if (approval.Case is not null &&
            (DigitalWorkforceApprovalGuard.IsExpired(approval, now) ||
             DigitalWorkforceApprovalGuard.IsStale(approval, approval.Case)))
        {
            approval.Status = DigitalWorkApprovalStatuses.Expired;
            approval.ConcurrencyToken++;
            await repository.SaveChangesAsync(cancellationToken);
            throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.ExecutionStaleApproval,
                "Onay süresi doldu veya vaka bağlamı değişti.");
        }

        approval.Status = request.Decision switch
        {
            "approved" => DigitalWorkApprovalStatuses.Approved,
            "rejected" => DigitalWorkApprovalStatuses.Rejected,
            "modified" => DigitalWorkApprovalStatuses.Modified,
            _ => throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.ValidationFailed, "Invalid decision."),
        };
        approval.Decision = request.Decision;
        approval.DecidedBySubjectId = subjectId;
        approval.DecidedAtUtc = DateTimeOffset.UtcNow;
        approval.ConcurrencyToken++;

        if (approval.Case is not null && approval.Status == DigitalWorkApprovalStatuses.Approved)
        {
            approval.Case.Status = DigitalWorkCaseStatuses.Executing;
            approval.Case.CurrentStage = DigitalWorkCaseStages.Act;
            approval.Case.RequiresApproval = false;
            approval.Case.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await WriteAuditAsync("digital-workforce.approval.decided", request.Decision, "digital-work-approval",
            approval.Id.ToString(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        if (approval.Status == DigitalWorkApprovalStatuses.Approved && approval.ExecutionId.HasValue)
        {
            await executionRuntime.ResumeAfterApprovalAsync(approval.Id, cancellationToken);
        }

        return approval;
    }

    public Task<DigitalWorkHandoff> HandoffAsync(
        Guid caseId, HandoffRequest request, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => HandoffCoreAsync(caseId, request, subjectId, ct),
            cancellationToken);

    private async Task<DigitalWorkHandoff> HandoffCoreAsync(
        Guid caseId, HandoffRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var workCase = await dbContext.DigitalWorkCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var fromWorker = await RequireWorkerAsync(workCase.PrimaryWorkerId, cancellationToken);
        var toWorker = await RequireWorkerAsync(request.ToWorkerId, cancellationToken);
        await EnsureDispatchAllowedAsync(fromWorker, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var handoff = new DigitalWorkHandoff
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = caseId,
            FromWorkerId = fromWorker.Id,
            ToWorkerId = toWorker.Id,
            Reason = request.Reason,
            Dependency = request.Dependency,
            Status = DigitalWorkforceHandoffStatuses.Accepted,
            CreatedAtUtc = now,
            CompletedAtUtc = now,
        };

        dbContext.DigitalWorkCaseParticipants.Add(new DigitalWorkCaseParticipant
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = caseId,
            WorkerId = toWorker.Id,
            Role = DigitalWorkforceParticipantRoles.Supporting,
            JoinedAtUtc = now,
            HandoffReason = request.Reason,
        });

        workCase.PrimaryWorkerId = toWorker.Id;
        workCase.UpdatedAtUtc = now;
        workCase.ConcurrencyToken++;
        dbContext.DigitalWorkHandoffs.Add(handoff);

        await AppendActivityAsync(workCase.TenantId, caseId, fromWorker.Id, "worker.handoff",
            $"Devir: {fromWorker.Code} -> {toWorker.Code}", request.Reason, cancellationToken);
        await WriteAuditAsync("digital-workforce.handoff", "handoff", "digital-work-case",
            caseId.ToString(), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return handoff;
    }

    public async Task RecordVerificationAsync(
        Guid caseId, RecordVerificationRequest request, string subjectId, CancellationToken cancellationToken)
    {
        var workCase = await repository.GetCaseAsync(caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");

        var verification = new DigitalWorkVerification
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = caseId,
            VerificationType = request.VerificationType,
            Source = request.Source,
            ExpectedState = request.ExpectedState,
            ObservedState = request.ObservedState,
            VerifiedBy = subjectId,
            VerifiedAtUtc = DateTimeOffset.UtcNow,
            Confidence = request.Confidence,
            Result = request.Result,
            SourceClassification = request.SourceClassification,
        };

        dbContext.DigitalWorkVerifications.Add(verification);
        workCase.Verifications.Add(verification);

        if (string.Equals(request.Result, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))
        {
            workCase.VerificationState = DigitalWorkVerificationResults.Passed;
            workCase.VerifiedAtUtc = DateTimeOffset.UtcNow;
        }

        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordAiUsageAsync(RecordAiUsageRequest request, CancellationToken cancellationToken)
    {
        var worker = await RequireWorkerAsync(request.WorkerId, cancellationToken);
        if (worker.Budget is not null)
        {
            var eval = DigitalWorkforceBudgetEngine.Evaluate(worker.Budget);
            if (!eval.AllowAutonomousAi)
            {
                throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.BudgetExceeded, "AI budget limit reached.");
            }

            DigitalWorkforceBudgetEngine.ApplyUsage(worker.Budget, request.EstimatedCost);
        }

        dbContext.DigitalWorkerUsages.Add(new DigitalWorkerUsage
        {
            Id = Guid.NewGuid(),
            TenantId = worker.TenantId,
            WorkerId = worker.Id,
            CaseId = request.CaseId,
            ModelTier = request.ModelTier,
            ModelName = request.ModelName,
            InputTokens = request.InputTokens,
            OutputTokens = request.OutputTokens,
            EstimatedCost = request.EstimatedCost,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        if (request.CaseId.HasValue)
        {
            var workCase = await repository.GetCaseAsync(request.CaseId.Value, cancellationToken);
            if (workCase is not null)
            {
                workCase.AiCost += request.EstimatedCost;
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DigitalWorkCaseSummary>> ListCasesAsync(
        string? status, Guid? workerId, CancellationToken cancellationToken) =>
        repository.ListCasesAsync(status, workerId, cancellationToken);

    public Task<IReadOnlyList<DigitalWorkActivitySummary>> ListActivityAsync(
        Guid? workerId, Guid? caseId, CancellationToken cancellationToken) =>
        repository.ListActivityAsync(workerId, caseId, cancellationToken);

    public Task<IReadOnlyList<DigitalWorkApprovalSummary>> ListApprovalsAsync(
        string? status, CancellationToken cancellationToken) =>
        repository.ListApprovalsAsync(status, cancellationToken);

    public Task<DigitalWorkforceMetrics> GetMetricsAsync(CancellationToken cancellationToken) =>
        repository.GetMetricsAsync(cancellationToken);

    private async Task<DigitalWorker> RequireWorkerAsync(Guid workerId, CancellationToken cancellationToken) =>
        await repository.GetWorkerAsync(workerId, cancellationToken)
        ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");

    private async Task EnsureDispatchAllowedAsync(DigitalWorker worker, CancellationToken cancellationToken)
    {
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);
        if (runtime.GlobalKillActive || worker.IsKilled || (worker.Policy?.KillSwitch ?? false))
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.KillSwitchActive, "Kill switch active.");
        }

        if (runtime.GlobalPauseActive || worker.IsPaused || worker.GlobalPauseActive)
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.Paused, "Worker dispatch paused.");
        }
    }

    private Task<bool> EscalateActiveCasesForWorkerAsync(
        DigitalWorker worker, string subjectId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EscalateActiveCasesForWorkerCoreAsync(worker, subjectId, ct),
            cancellationToken);

    private async Task<bool> EscalateActiveCasesForWorkerCoreAsync(
        DigitalWorker worker, string subjectId, CancellationToken cancellationToken)
    {
        var activeCases = await dbContext.DigitalWorkCases
            .Where(c => c.PrimaryWorkerId == worker.Id && c.Status != DigitalWorkCaseStatuses.Closed)
            .ToListAsync(cancellationToken);

        foreach (var workCase in activeCases)
        {
            workCase.Status = DigitalWorkCaseStatuses.Escalated;
            workCase.CurrentStage = DigitalWorkCaseStages.Act;
            workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await AppendActivityAsync(worker.TenantId, workCase.Id, worker.Id, "case.escalated",
                "Kill switch nedeniyle iş yükseltildi.", "escalated", cancellationToken);
        }

        return true;
    }

    private async Task CreatePendingApprovalAsync(
        DigitalWorkCase workCase, DigitalWorker worker, AdvanceWorkCaseRequest request, CancellationToken cancellationToken)
    {
        dbContext.DigitalWorkApprovals.Add(new DigitalWorkApproval
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = workCase.Id,
            WorkerId = worker.Id,
            Action = request.Action ?? "execute",
            BusinessReason = workCase.Title,
            Impact = workCase.BusinessImpact,
            RiskLevel = request.RiskLevel,
            Amount = request.Amount,
            Status = DigitalWorkApprovalStatuses.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DigitalWorkforceApprovalGuard.ResolveExpiresAt(DateTimeOffset.UtcNow),
            CaseConcurrencyTokenAtRequest = workCase.ConcurrencyToken,
            ConcurrencyToken = 1,
        });
        workCase.RequiresApproval = true;
    }

    private async Task AppendActivityAsync(
        Guid tenantId, Guid caseId, Guid workerId, string actionType, string description,
        string result, CancellationToken cancellationToken, string sourceClassification = DigitalWorkforceSourceClassifications.Real)
    {
        dbContext.DigitalWorkActivities.Add(new DigitalWorkActivity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            WorkerId = workerId,
            ActionType = actionType,
            Description = description,
            Result = result,
            SourceClassification = sourceClassification,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await Task.CompletedTask;
    }

    private static void EnsureConcurrency(long current, long expected)
    {
        if (current != expected)
        {
            throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.ConcurrencyConflict, "Concurrency conflict.");
        }
    }

    private static bool IsValidAutonomyMode(string mode) =>
        mode is DigitalWorkforceAutonomyModes.Observe or
        DigitalWorkforceAutonomyModes.Draft or
        DigitalWorkforceAutonomyModes.Limited or
        DigitalWorkforceAutonomyModes.Full;

    private Task<AuditWriteResult> WriteAuditAsync(
        string eventType, string action, string resourceType, string resourceId, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(
            new AuditWriteRequest(eventType, action, resourceType, AuditOutcome.Success, resourceId),
            cancellationToken);

    private static bool RequiresOperationalPolicyCheck(AdvanceWorkCaseRequest request) =>
        !string.IsNullOrWhiteSpace(request.Tool) ||
        (!string.IsNullOrWhiteSpace(request.Action) && request.Action.Contains('.', StringComparison.Ordinal));

    private static string ResolvePolicyAction(AdvanceWorkCaseRequest request) =>
        !string.IsNullOrWhiteSpace(request.Tool) ? request.Tool! : request.Action!;

    private static DigitalWorkforceActionKind ResolveActionKind(string action)
    {
        if (action.Contains("draft", StringComparison.OrdinalIgnoreCase) ||
            action is DigitalWorkforceToolCapabilities.ProcurementDraft or
            DigitalWorkforceToolCapabilities.EmailDraft or
            DigitalWorkforceToolCapabilities.WhatsappDraft)
        {
            return DigitalWorkforceActionKind.Draft;
        }

        if (action is DigitalWorkforceToolCapabilities.TeamsSend or
            DigitalWorkforceToolCapabilities.EmailDraft or
            DigitalWorkforceToolCapabilities.WhatsappDraft)
        {
            return DigitalWorkforceActionKind.External;
        }

        if (action.Contains(".read", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkforceActionKind.Read;
        }

        return DigitalWorkforceActionKind.Execute;
    }
}

public sealed class DigitalWorkforceException : Exception
{
    public string FailureCode { get; }

    public DigitalWorkforceException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
