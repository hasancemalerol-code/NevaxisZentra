using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceExecutionRuntimeService
{
    Task<EnqueueDigitalWorkExecutionResult> EnqueueAsync(
        EnqueueDigitalWorkExecutionRequest request,
        string subjectId,
        CancellationToken cancellationToken);

    Task<DigitalWorkExecutionProcessingResult> ProcessBatchAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken);

    Task<DigitalWorkExecutionProcessingResult> ReclaimExpiredLeasesAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken);

    Task ResumeAfterApprovalAsync(Guid approvalId, CancellationToken cancellationToken);

    Task CancelWorkerExecutionsAsync(Guid workerId, string reason, CancellationToken cancellationToken);

    Task PauseWorkerExecutionsAsync(Guid workerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DigitalWorkExecutionSummary>> ListExecutionsAsync(
        Guid? caseId,
        Guid? workerId,
        string? status,
        int limit,
        CancellationToken cancellationToken);

    Task<DigitalWorkerHealthSnapshot> GetWorkerHealthAsync(Guid workerId, CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceExecutionRuntimeService(
    NevaxisFactOSDbContext dbContext,
    IDigitalWorkforceRepository repository,
    TenantDatabaseSession tenantDatabaseSession) : IDigitalWorkforceExecutionRuntimeService
{
    private const int DefaultBatchSize = 25;

    public Task<EnqueueDigitalWorkExecutionResult> EnqueueAsync(
        EnqueueDigitalWorkExecutionRequest request,
        string subjectId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EnqueueCoreAsync(request, subjectId, ct),
            cancellationToken);

    public Task<DigitalWorkExecutionProcessingResult> ProcessBatchAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ProcessBatchCoreAsync(owner, batchSize, ct),
            cancellationToken);

    public Task<DigitalWorkExecutionProcessingResult> ReclaimExpiredLeasesAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ReclaimExpiredLeasesCoreAsync(owner, batchSize, ct),
            cancellationToken);

    public Task ResumeAfterApprovalAsync(Guid approvalId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await ResumeAfterApprovalCoreAsync(approvalId, ct);
                return true;
            },
            cancellationToken);

    public Task CancelWorkerExecutionsAsync(Guid workerId, string reason, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await CancelWorkerExecutionsCoreAsync(workerId, reason, ct);
                return true;
            },
            cancellationToken);

    public Task PauseWorkerExecutionsAsync(Guid workerId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await PauseWorkerExecutionsCoreAsync(workerId, ct);
                return true;
            },
            cancellationToken);

    public Task<IReadOnlyList<DigitalWorkExecutionSummary>> ListExecutionsAsync(
        Guid? caseId,
        Guid? workerId,
        string? status,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListExecutionsCoreAsync(caseId, workerId, status, limit, ct),
            cancellationToken);

    public Task<DigitalWorkerHealthSnapshot> GetWorkerHealthAsync(
        Guid workerId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkerHealthCoreAsync(workerId, ct),
            cancellationToken);

    private async Task<EnqueueDigitalWorkExecutionResult> EnqueueCoreAsync(
        EnqueueDigitalWorkExecutionRequest request,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var workCase = await repository.GetCaseAsync(request.CaseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = await repository.GetWorkerAsync(request.WorkerId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);

        if (runtime.GlobalKillActive || worker.IsKilled || worker.Policy?.KillSwitch == true)
        {
            return new(false, false, null, DigitalWorkforceFailureCodes.KillSwitchActive, "Kill switch aktif.");
        }

        if (worker.FacilityId.HasValue && workCase.FacilityId.HasValue &&
            worker.FacilityId != workCase.FacilityId)
        {
            return new(false, false, null, DigitalWorkforceFailureCodes.ScopeViolation, "Tesis kapsamı ihlali.");
        }

        var idempotencyKey = request.IdempotencyKeyOverride
            ?? BuildIdempotencyKey(workCase.TenantId, request.CaseId, request.ActionType, request.Tool, request.InputContractJson);
        var existing = await dbContext.DigitalWorkExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == workCase.TenantId && e.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return new(true, true, existing.Id, null, null);
        }

        var classification = DigitalWorkforceActionClassifier.Classify(
            request.ActionType, request.Tool, request.Amount, request.RiskLevel);
        var now = DateTimeOffset.UtcNow;
        var execution = new DigitalWorkExecution
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            FacilityId = workCase.FacilityId ?? worker.FacilityId,
            CaseId = request.CaseId,
            WorkerId = request.WorkerId,
            ActionType = request.ActionType,
            ActionClassification = classification,
            Tool = request.Tool,
            InputContractJson = request.InputContractJson,
            Status = DigitalWorkExecutionStatuses.Queued,
            IdempotencyKey = idempotencyKey,
            CaseConcurrencyTokenAtEnqueue = workCase.ConcurrencyToken,
            MaxAttempts = worker.Policy?.RetryLimit ?? 3,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        dbContext.DigitalWorkExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true, false, execution.Id, null, null);
    }

    private async Task<DigitalWorkExecutionProcessingResult> ProcessBatchCoreAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var batch = await dbContext.DigitalWorkExecutions
            .Where(e => e.Status == DigitalWorkExecutionStatuses.Queued ||
                        (e.Status == DigitalWorkExecutionStatuses.RetryWait && e.NextRetryAtUtc <= now))
            .OrderBy(e => e.CreatedAtUtc)
            .Take(Math.Min(batchSize, DefaultBatchSize))
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
        var waitingApproval = 0;

        foreach (var execution in batch)
        {
            try
            {
                var result = await ExecuteOneAsync(execution, owner, cancellationToken);
                processed += result == DigitalWorkExecutionStatuses.Completed ? 1 : 0;
                failed += result == DigitalWorkExecutionStatuses.Failed ? 1 : 0;
                waitingApproval += result == DigitalWorkExecutionStatuses.WaitingApproval ? 1 : 0;
            }
            catch
            {
                failed++;
            }
        }

        return new(processed, failed, waitingApproval, 0);
    }

    private async Task<string> ExecuteOneAsync(
        DigitalWorkExecution execution,
        string owner,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var workCase = await repository.GetCaseAsync(execution.CaseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");
        var worker = await repository.GetWorkerAsync(execution.WorkerId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");
        var policy = worker.Policy ?? throw new DigitalWorkforceException(
            DigitalWorkforceFailureCodes.NotFound, "Worker policy not found.");
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);

        if (runtime.GlobalKillActive || worker.IsKilled || policy.KillSwitch || worker.IsPaused || runtime.GlobalPauseActive)
        {
            execution.Status = DigitalWorkExecutionStatuses.Paused;
            execution.CurrentBlocker = "worker-paused-or-killed";
            execution.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution.Status;
        }

        var hourStart = now.AddHours(-1);
        var actionsLastHour = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == worker.Id && e.FirstAttemptAtUtc >= hourStart, cancellationToken);
        var externalWrites = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == worker.Id &&
                 e.FirstAttemptAtUtc >= hourStart &&
                 e.ActionClassification == DigitalWorkActionClassifications.WriteExternal,
            cancellationToken);
        var running = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == worker.Id &&
                 (e.Status == DigitalWorkExecutionStatuses.Running || e.Status == DigitalWorkExecutionStatuses.Claimed),
            cancellationToken);
        var limitEval = DigitalWorkforceOperationalLimitEngine.Evaluate(
            actionsLastHour, externalWrites, running, actionClassification: execution.ActionClassification);
        if (!limitEval.Allowed)
        {
            execution.Status = DigitalWorkExecutionStatuses.Failed;
            execution.LastError = limitEval.Reason;
            execution.ErrorClassification = DigitalWorkExecutionErrorClassifications.Policy;
            execution.CurrentBlocker = limitEval.Reason;
            execution.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution.Status;
        }

        DigitalWorkforceExecutionLeaseEngine.ApplyClaim(execution, owner, now);
        execution.AttemptCount++;
        execution.FirstAttemptAtUtc ??= now;
        execution.LastAttemptAtUtc = now;
        execution.Status = DigitalWorkExecutionStatuses.Running;
        execution.UpdatedAtUtc = now;

        var policyEval = DigitalWorkforcePolicyEngine.EvaluateAction(
            worker, policy, execution.ActionType, execution.Tool, null, 2, workCase.FacilityId);
        execution.PolicyResultJson = JsonSerializer.Serialize(policyEval);
        if (!policyEval.Allowed)
        {
            execution.Status = DigitalWorkExecutionStatuses.Failed;
            execution.LastError = policyEval.DenialReason;
            execution.ErrorClassification = DigitalWorkExecutionErrorClassifications.Policy;
            execution.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution.Status;
        }

        var actionKind = DigitalWorkforceActionClassifier.ToActionKind(execution.ActionClassification);
        var autonomyEval = DigitalWorkforceAutonomyEngine.Evaluate(worker, actionKind, policyEval.RequiresApproval);
        var mandatoryApproval = DigitalWorkforceActionClassifier.RequiresMandatoryApproval(execution.ActionClassification);
        if (!autonomyEval.Allowed || autonomyEval.RequiresApproval || policyEval.RequiresApproval || mandatoryApproval)
        {
            var approval = await CreateApprovalForExecutionAsync(execution, workCase, worker, cancellationToken);
            execution.ApprovalId = approval.Id;
            execution.Status = DigitalWorkExecutionStatuses.WaitingApproval;
            execution.CurrentBlocker = "approval-required";
            execution.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution.Status;
        }

        return await CompleteExecutionAsync(execution, worker, workCase, cancellationToken);
    }

    private async Task<string> CompleteExecutionAsync(
        DigitalWorkExecution execution,
        DigitalWorker worker,
        DigitalWorkCase workCase,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(execution.Tool))
        {
            execution.Status = DigitalWorkExecutionStatuses.Completed;
            execution.ResultJson = """{"completed":true}""";
            execution.CompletedAtUtc = now;
            execution.UpdatedAtUtc = now;
            await AppendExecutionActivityAsync(execution, worker, workCase, "completed", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution.Status;
        }

        var toolDefinition = DigitalWorkforceToolRegistry.Require(execution.Tool);
        var toolResult = DigitalWorkforceToolExecutor.Execute(execution, toolDefinition);
        execution.ExternalActionStatus = toolResult.ExternalActionStatus;
        execution.ExternalReferenceId = toolResult.ExternalReferenceId;
        execution.ResultJson = toolResult.ResultJson;
        execution.EvidenceReference = toolResult.EvidenceReference;
        execution.IsIrreversible = toolResult.IsIrreversible;
        execution.CompensationActionJson = toolResult.CompensationActionJson;
        execution.Status = DigitalWorkExecutionStatuses.Completed;
        execution.CompletedAtUtc = now;
        execution.UpdatedAtUtc = now;

        await AppendExecutionActivityAsync(execution, worker, workCase, "completed", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return execution.Status;
    }

    private async Task<DigitalWorkApproval> CreateApprovalForExecutionAsync(
        DigitalWorkExecution execution,
        DigitalWorkCase workCase,
        DigitalWorker worker,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var approval = new DigitalWorkApproval
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = workCase.Id,
            WorkerId = worker.Id,
            ExecutionId = execution.Id,
            Action = execution.ActionType,
            BusinessReason = $"Çalışan {worker.Code} aksiyonu onay bekliyor.",
            Impact = workCase.BusinessImpact,
            RiskLevel = 3,
            EvidenceJson = execution.InputContractJson,
            Status = DigitalWorkApprovalStatuses.Pending,
            CreatedAtUtc = now,
            ExpiresAtUtc = DigitalWorkforceApprovalGuard.ResolveExpiresAt(now),
            CaseConcurrencyTokenAtRequest = workCase.ConcurrencyToken,
            ConcurrencyToken = 1,
        };
        dbContext.DigitalWorkApprovals.Add(approval);
        workCase.Status = DigitalWorkCaseStatuses.AwaitingApproval;
        workCase.CurrentStage = DigitalWorkCaseStages.Approve;
        workCase.RequiresApproval = true;
        workCase.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return approval;
    }

    private async Task ResumeAfterApprovalCoreAsync(Guid approvalId, CancellationToken cancellationToken)
    {
        var approval = await dbContext.DigitalWorkApprovals
            .Include(a => a.Case)
            .FirstOrDefaultAsync(a => a.Id == approvalId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Approval not found.");

        if (approval.Case is null || approval.ExecutionId is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (!DigitalWorkforceApprovalGuard.CanResumeExecution(approval, approval.Case, now))
        {
            approval.Status = DigitalWorkApprovalStatuses.Expired;
            throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.ExecutionStaleApproval,
                "Onay süresi doldu veya vaka bağlamı değişti.");
        }

        var execution = await dbContext.DigitalWorkExecutions
            .FirstOrDefaultAsync(e => e.Id == approval.ExecutionId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Execution not found.");

        if (execution.Status == DigitalWorkExecutionStatuses.Completed)
        {
            return;
        }

        var worker = await repository.GetWorkerAsync(execution.WorkerId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");

        execution.Status = DigitalWorkExecutionStatuses.Queued;
        execution.CurrentBlocker = null;
        execution.UpdatedAtUtc = now;
        approval.Case.Status = DigitalWorkCaseStatuses.Executing;
        approval.Case.CurrentStage = DigitalWorkCaseStages.Act;
        approval.Case.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        await ExecuteOneAsync(execution, "approval-resume", cancellationToken);
    }

    private async Task<DigitalWorkExecutionProcessingResult> ReclaimExpiredLeasesCoreAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = await dbContext.DigitalWorkExecutions
            .Where(e => e.Status == DigitalWorkExecutionStatuses.Claimed ||
                        e.Status == DigitalWorkExecutionStatuses.Running)
            .Where(e => e.LeaseExpiresAtUtc <= now)
            .Take(Math.Min(batchSize, DefaultBatchSize))
            .ToListAsync(cancellationToken);

        foreach (var execution in expired)
        {
            execution.Status = DigitalWorkExecutionStatuses.Queued;
            execution.ClaimOwner = null;
            execution.ClaimToken = null;
            execution.LeaseExpiresAtUtc = null;
            execution.CurrentBlocker = "lease-reclaimed";
            execution.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new(0, 0, 0, expired.Count);
    }

    private async Task CancelWorkerExecutionsCoreAsync(Guid workerId, string reason, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var executions = await dbContext.DigitalWorkExecutions
            .Where(e => e.WorkerId == workerId &&
                        e.Status != DigitalWorkExecutionStatuses.Completed &&
                        e.Status != DigitalWorkExecutionStatuses.Cancelled &&
                        e.Status != DigitalWorkExecutionStatuses.Verified)
            .ToListAsync(cancellationToken);

        foreach (var execution in executions)
        {
            execution.Status = DigitalWorkExecutionStatuses.Cancelled;
            execution.LastError = reason;
            execution.UpdatedAtUtc = now;
            execution.CompletedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PauseWorkerExecutionsCoreAsync(Guid workerId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var executions = await dbContext.DigitalWorkExecutions
            .Where(e => e.WorkerId == workerId &&
                        (e.Status == DigitalWorkExecutionStatuses.Queued ||
                         e.Status == DigitalWorkExecutionStatuses.RetryWait))
            .ToListAsync(cancellationToken);

        foreach (var execution in executions)
        {
            execution.Status = DigitalWorkExecutionStatuses.Paused;
            execution.CurrentBlocker = "worker-paused";
            execution.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DigitalWorkExecutionSummary>> ListExecutionsCoreAsync(
        Guid? caseId,
        Guid? workerId,
        string? status,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkExecutions.AsNoTracking();
        if (caseId.HasValue)
        {
            query = query.Where(e => e.CaseId == caseId.Value);
        }

        if (workerId.HasValue)
        {
            query = query.Where(e => e.WorkerId == workerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        var rows = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(Math.Min(limit, 100))
            .ToListAsync(cancellationToken);

        return rows.Select(e => new DigitalWorkExecutionSummary(
            e.Id, e.CaseId, e.WorkerId, e.ActionType, e.ActionClassification, e.Tool,
            e.Status, e.CurrentBlocker, e.AttemptCount, e.ExternalActionStatus,
            e.ExternalReferenceId, e.CreatedAtUtc, e.CompletedAtUtc)).ToList();
    }

    private async Task<DigitalWorkerHealthSnapshot> GetWorkerHealthCoreAsync(
        Guid workerId,
        CancellationToken cancellationToken)
    {
        var worker = await repository.GetWorkerAsync(workerId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Worker not found.");
        var runtime = await repository.GetOrCreateRuntimeStateAsync(cancellationToken);

        var queued = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == workerId && e.Status == DigitalWorkExecutionStatuses.Queued, cancellationToken);
        var running = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == workerId &&
                 (e.Status == DigitalWorkExecutionStatuses.Running || e.Status == DigitalWorkExecutionStatuses.Claimed),
            cancellationToken);
        var failed = await dbContext.DigitalWorkExecutions.CountAsync(
            e => e.WorkerId == workerId && e.Status == DigitalWorkExecutionStatuses.Failed, cancellationToken);
        var waitingApprovals = await dbContext.DigitalWorkApprovals.CountAsync(
            a => a.WorkerId == workerId && a.Status == DigitalWorkApprovalStatuses.Pending, cancellationToken);
        var lastActivity = await dbContext.DigitalWorkActivities
            .Where(a => a.WorkerId == workerId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var blocker = await dbContext.DigitalWorkExecutions
            .Where(e => e.WorkerId == workerId && e.CurrentBlocker != null)
            .OrderByDescending(e => e.UpdatedAtUtc)
            .Select(e => e.CurrentBlocker)
            .FirstOrDefaultAsync(cancellationToken);

        return DigitalWorkforceWorkerHealthEngine.Evaluate(
            worker, queued, running, failed, waitingApprovals, blocker,
            lastActivity == default ? null : lastActivity,
            runtime.GlobalKillActive, runtime.GlobalPauseActive);
    }

    private async Task AppendExecutionActivityAsync(
        DigitalWorkExecution execution,
        DigitalWorker worker,
        DigitalWorkCase workCase,
        string result,
        CancellationToken cancellationToken)
    {
        dbContext.DigitalWorkActivities.Add(new DigitalWorkActivity
        {
            Id = Guid.NewGuid(),
            TenantId = workCase.TenantId,
            CaseId = workCase.Id,
            WorkerId = worker.Id,
            ActionType = $"execution.{execution.ActionType}",
            Description = execution.ActionType,
            Reason = execution.ActionClassification,
            Result = result,
            EvidenceReference = execution.EvidenceReference,
            SourceClassification = workCase.SourceClassification,
            CorrelationId = execution.Id.ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await Task.CompletedTask;
    }

    public static string BuildIdempotencyKey(
        Guid tenantId,
        Guid caseId,
        string actionType,
        string? tool,
        string inputContractJson)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputContractJson))).ToLowerInvariant()[..16];
        return $"{tenantId:N}:{caseId:N}:{actionType}:{tool ?? "none"}:{hash}";
    }
}
