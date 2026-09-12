using System.Text.Json;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceRepository
{
    Task<DigitalWorkforceControlCenterSnapshot> GetControlCenterSnapshotAsync(CancellationToken cancellationToken);
    Task<DigitalWorker?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorker>> ListWorkersAsync(CancellationToken cancellationToken);
    Task<DigitalWorkforceRuntimeState> GetOrCreateRuntimeStateAsync(CancellationToken cancellationToken);
    Task<DigitalWorkCase?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkCaseSummary>> ListCasesAsync(string? status, Guid? workerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkActivitySummary>> ListActivityAsync(Guid? workerId, Guid? caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkApprovalSummary>> ListApprovalsAsync(string? status, CancellationToken cancellationToken);
    Task<DigitalWorkforceMetrics> GetMetricsAsync(CancellationToken cancellationToken);
    Task EnsureCanonicalWorkersAsync(Guid tenantId, Guid facilityId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceRepository : IDigitalWorkforceRepository
{
    private static readonly string[] DefaultApprovalRequiredActions =
        ["procurement.draft", "quality.manage", "production.plan"];

    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public DigitalWorkforceRepository(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task EnsureCanonicalWorkersAsync(Guid tenantId, Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            await EnsureCanonicalWorkersCoreAsync(tenantId, facilityId, ct);
            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

    private async Task EnsureCanonicalWorkersCoreAsync(Guid tenantId, Guid facilityId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var definition in DigitalWorkforceCanonicalWorkers.Definitions)
        {
            var existing = await dbContext.DigitalWorkers
                .Include(w => w.Policy)
                .Include(w => w.Budget)
                .FirstOrDefaultAsync(w => w.Id == definition.Id, cancellationToken);

            if (existing is not null)
            {
                if (!string.Equals(existing.AutonomyMode, definition.DefaultAutonomyMode, StringComparison.OrdinalIgnoreCase))
                {
                    existing.AutonomyMode = definition.DefaultAutonomyMode;
                    existing.UpdatedAtUtc = now;
                }

                continue;
            }

            var worker = new DigitalWorker
            {
                Id = definition.Id,
                TenantId = tenantId,
                FacilityId = facilityId,
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                Mission = definition.Mission,
                Status = DigitalWorkforceWorkerStatuses.Idle,
                AutonomyMode = definition.DefaultAutonomyMode,
                Enabled = true,
                SourceClassification = DigitalWorkforceSourceClassifications.Demo,
                DefaultPolicyProfileJson = "{}",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ConcurrencyToken = 1,
                Policy = CreateDefaultPolicy(definition, tenantId, now),
                Budget = new DigitalWorkerBudget
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    WorkerId = definition.Id,
                    DailyLimit = 50m,
                    MonthlyLimit = 500m,
                    WarningThreshold = 0.8m,
                    UpdatedAtUtc = now,
                },
            };

            worker.Budget!.WorkerId = worker.Id;
            dbContext.DigitalWorkers.Add(worker);
        }

        var runtimeState = await dbContext.DigitalWorkforceRuntimeStates
            .FirstOrDefaultAsync(cancellationToken);
        if (runtimeState is null)
        {
            dbContext.DigitalWorkforceRuntimeStates.Add(new DigitalWorkforceRuntimeState
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UpdatedAtUtc = now,
                ConcurrencyToken = 1,
            });
        }
    }

    public Task<DigitalWorkforceControlCenterSnapshot> GetControlCenterSnapshotAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => GetControlCenterSnapshotCoreAsync(ct), cancellationToken);

    private async Task<DigitalWorkforceControlCenterSnapshot> GetControlCenterSnapshotCoreAsync(CancellationToken cancellationToken)
    {
        var workers = await dbContext.DigitalWorkers
            .AsNoTracking()
            .Include(w => w.Policy)
            .Include(w => w.Budget)
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);

        var runtime = await GetOrCreateRuntimeStateAsync(cancellationToken);
        var utcNow = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var caseStats = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Open = g.Count(c => c.Status != DigitalWorkCaseStatuses.Closed),
                Escalated = g.Count(c => c.Status == DigitalWorkCaseStatuses.Escalated),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var workerSummaries = new List<DigitalWorkerSummary>();
        foreach (var worker in workers)
        {
            var activeCases = await dbContext.DigitalWorkCases.CountAsync(
                c => c.PrimaryWorkerId == worker.Id && c.Status != DigitalWorkCaseStatuses.Closed,
                cancellationToken);
            var queueCases = await dbContext.DigitalWorkCases.CountAsync(
                c => c.PrimaryWorkerId == worker.Id && c.Status == DigitalWorkCaseStatuses.Detected,
                cancellationToken);
            var completedToday = await dbContext.DigitalWorkCases.CountAsync(
                c => c.PrimaryWorkerId == worker.Id &&
                     c.Status == DigitalWorkCaseStatuses.Closed &&
                     c.ClosedAtUtc >= today,
                cancellationToken);
            var completedMonth = await dbContext.DigitalWorkCases.CountAsync(
                c => c.PrimaryWorkerId == worker.Id &&
                     c.Status == DigitalWorkCaseStatuses.Closed &&
                     c.ClosedAtUtc >= monthStart,
                cancellationToken);
            var verified = await dbContext.DigitalWorkCases.CountAsync(
                c => c.PrimaryWorkerId == worker.Id &&
                     c.VerificationState == DigitalWorkVerificationResults.Passed,
                cancellationToken);
            var pendingApprovals = await dbContext.DigitalWorkApprovals.CountAsync(
                a => a.WorkerId == worker.Id && a.Status == DigitalWorkApprovalStatuses.Pending,
                cancellationToken);
            var lastActivity = await dbContext.DigitalWorkActivities
                .AsNoTracking()
                .Where(a => a.WorkerId == worker.Id)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => (DateTimeOffset?)a.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var aiCostToday = await dbContext.DigitalWorkerUsages
                .AsNoTracking()
                .Where(u => u.WorkerId == worker.Id && u.CreatedAtUtc >= today)
                .SumAsync(u => (decimal?)u.EstimatedCost, cancellationToken) ?? 0m;

            workerSummaries.Add(new DigitalWorkerSummary(
                worker.Id, worker.Code, worker.Name, worker.Description, worker.Mission,
                worker.Status, worker.AutonomyMode, worker.Enabled, worker.IsPaused, worker.IsKilled,
                worker.IsHumanControlled, worker.SourceClassification,
                activeCases, queueCases, completedToday, completedMonth, verified, pendingApprovals,
                lastActivity?.ToString("O"), aiCostToday,
                worker.IsKilled ? "Acil Durdur" : worker.IsPaused || runtime.GlobalPauseActive ? "Duraklatıldı" : "Aktif",
                worker.ConcurrencyToken));
        }

        var totalClosed = await dbContext.DigitalWorkCases.CountAsync(
            c => c.Status == DigitalWorkCaseStatuses.Closed, cancellationToken);
        var totalVerified = await dbContext.DigitalWorkCases.CountAsync(
            c => c.VerificationState == DigitalWorkVerificationResults.Passed, cancellationToken);
        var verificationRate = totalClosed == 0 ? 0m : (decimal)totalVerified / totalClosed;
        var totalAiToday = await dbContext.DigitalWorkerUsages
            .AsNoTracking()
            .Where(u => u.CreatedAtUtc >= today)
            .SumAsync(u => (decimal?)u.EstimatedCost, cancellationToken) ?? 0m;
        var totalAiMonth = await dbContext.DigitalWorkerUsages
            .AsNoTracking()
            .Where(u => u.CreatedAtUtc >= monthStart)
            .SumAsync(u => (decimal?)u.EstimatedCost, cancellationToken) ?? 0m;
        var pendingTotal = await dbContext.DigitalWorkApprovals.CountAsync(
            a => a.Status == DigitalWorkApprovalStatuses.Pending, cancellationToken);

        return new DigitalWorkforceControlCenterSnapshot(
            workerSummaries,
            runtime.GlobalPauseActive,
            runtime.GlobalKillActive,
            caseStats?.Open ?? 0,
            caseStats?.Escalated ?? 0,
            pendingTotal,
            verificationRate,
            totalAiToday,
            totalAiMonth);
    }

    public Task<DigitalWorker?> GetWorkerAsync(Guid workerId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.DigitalWorkers
                .Include(w => w.Policy)
                .Include(w => w.Budget)
                .FirstOrDefaultAsync(w => w.Id == workerId, ct), cancellationToken);

    public Task<IReadOnlyList<DigitalWorker>> ListWorkersAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<DigitalWorker>>(async ct =>
            await dbContext.DigitalWorkers
                .Include(w => w.Policy)
                .Include(w => w.Budget)
                .OrderBy(w => w.Code)
                .ToListAsync(ct), cancellationToken);

    public Task<DigitalWorkforceRuntimeState> GetOrCreateRuntimeStateAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(GetOrCreateRuntimeStateCoreAsync, cancellationToken);

    private async Task<DigitalWorkforceRuntimeState> GetOrCreateRuntimeStateCoreAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.DigitalWorkforceRuntimeStates.FirstOrDefaultAsync(cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = new DigitalWorkforceRuntimeState
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContextAccessor.RequireTenantId(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ConcurrencyToken = 1,
        };
        dbContext.DigitalWorkforceRuntimeStates.Add(state);
        return state;
    }

    public Task<DigitalWorkCase?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.DigitalWorkCases
                .Include(c => c.Participants)
                .Include(c => c.Verifications)
                .Include(c => c.Approvals)
                .Include(c => c.Activities)
                .Include(c => c.Handoffs)
                .Include(c => c.PrimaryWorker)
                .FirstOrDefaultAsync(c => c.Id == caseId, ct), cancellationToken);

    public Task<IReadOnlyList<DigitalWorkCaseSummary>> ListCasesAsync(
        string? status, Guid? workerId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => ListCasesCoreAsync(status, workerId, ct), cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkCaseSummary>> ListCasesCoreAsync(
        string? status, Guid? workerId, CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkCases.AsNoTracking().Include(c => c.PrimaryWorker).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (workerId.HasValue)
        {
            query = query.Where(c => c.PrimaryWorkerId == workerId.Value);
        }

        var cases = await query.OrderByDescending(c => c.UpdatedAtUtc).Take(200).ToListAsync(cancellationToken);
        return cases.Select(c => new DigitalWorkCaseSummary(
            c.Id, c.Title, c.Description, c.Status, c.CurrentStage, c.Severity,
            c.PrimaryWorkerId, c.PrimaryWorker?.Code ?? string.Empty, c.PrimaryWorker?.Name ?? string.Empty,
            c.RequiresApproval, c.IsHumanControlled, c.VerificationState, c.SourceClassification,
            c.StartedAtUtc, c.ClosedAtUtc, c.VerifiedAtUtc, c.Outcome, c.ConcurrencyToken)).ToList();
    }

    public Task<IReadOnlyList<DigitalWorkActivitySummary>> ListActivityAsync(
        Guid? workerId, Guid? caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => ListActivityCoreAsync(workerId, caseId, ct), cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkActivitySummary>> ListActivityCoreAsync(
        Guid? workerId, Guid? caseId, CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkActivities.AsNoTracking().Include(a => a.Worker).AsQueryable();
        if (workerId.HasValue)
        {
            query = query.Where(a => a.WorkerId == workerId.Value);
        }

        if (caseId.HasValue)
        {
            query = query.Where(a => a.CaseId == caseId.Value);
        }

        var items = await query.OrderByDescending(a => a.CreatedAtUtc).Take(200).ToListAsync(cancellationToken);
        return items.Select(a => new DigitalWorkActivitySummary(
            a.Id, a.CaseId, a.WorkerId, a.Worker?.Code ?? string.Empty,
            a.ActionType, a.Description, a.Result, a.SourceClassification, a.CreatedAtUtc)).ToList();
    }

    public Task<IReadOnlyList<DigitalWorkApprovalSummary>> ListApprovalsAsync(
        string? status, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => ListApprovalsCoreAsync(status, ct), cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkApprovalSummary>> ListApprovalsCoreAsync(
        string? status, CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkApprovals.AsNoTracking().Include(a => a.Worker).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var items = await query.OrderByDescending(a => a.CreatedAtUtc).Take(200).ToListAsync(cancellationToken);
        return items.Select(a => new DigitalWorkApprovalSummary(
            a.Id, a.CaseId, a.WorkerId, a.Worker?.Code ?? string.Empty,
            a.Action, a.BusinessReason, a.Impact, a.RiskLevel, a.Amount,
            a.Status, a.CreatedAtUtc, a.ExpiresAtUtc, a.ConcurrencyToken)).ToList();
    }

    public Task<DigitalWorkforceMetrics> GetMetricsAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(GetMetricsCoreAsync, cancellationToken);

    private async Task<DigitalWorkforceMetrics> GetMetricsCoreAsync(CancellationToken cancellationToken)
    {
        var completed = await dbContext.DigitalWorkCases.CountAsync(
            c => c.Status == DigitalWorkCaseStatuses.Closed, cancellationToken);
        var verified = await dbContext.DigitalWorkCases.CountAsync(
            c => c.VerificationState == DigitalWorkVerificationResults.Passed, cancellationToken);
        var open = await dbContext.DigitalWorkCases.CountAsync(
            c => c.Status != DigitalWorkCaseStatuses.Closed, cancellationToken);
        var escalated = await dbContext.DigitalWorkCases.CountAsync(
            c => c.Status == DigitalWorkCaseStatuses.Escalated, cancellationToken);

        var durations = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Where(c => c.Status == DigitalWorkCaseStatuses.Closed && c.StartedAtUtc != null && c.ClosedAtUtc != null)
            .Select(c => (c.ClosedAtUtc!.Value - c.StartedAtUtc!.Value).TotalHours)
            .ToListAsync(cancellationToken);

        double? avg = durations.Count == 0 ? null : durations.Average();
        var rate = completed == 0 ? 0m : (decimal)verified / completed;

        return new DigitalWorkforceMetrics(completed, verified, rate, open, escalated, avg);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

    private static DigitalWorkerPolicy CreateDefaultPolicy(CanonicalWorkerDefinition definition, Guid tenantId, DateTimeOffset now)
    {
        var allowedActions = definition.DefaultTools.ToList();
        return new DigitalWorkerPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkerId = definition.Id,
            AllowedToolsJson = JsonSerializer.Serialize(definition.DefaultTools),
            AllowedActionsJson = JsonSerializer.Serialize(allowedActions),
            ProhibitedActionsJson = JsonSerializer.Serialize(DigitalWorkforceToolCapabilities.MachineSafetyProhibitedActions),
            ApprovalRequiredForJson = JsonSerializer.Serialize(DefaultApprovalRequiredActions),
            MaxTransactionValue = 10000m,
            MaxRiskLevel = 3,
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };
    }
}
