using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceWorksService
{
    Task<WorksWorkspaceSnapshot> GetWorkspaceAsync(
        string? queue,
        string? search,
        int limit,
        string? subjectId,
        CancellationToken cancellationToken = default);

    Task<WorksCaseDetailSnapshot> GetCaseDetailAsync(
        Guid caseId,
        string? subjectId,
        bool canDecideApproval,
        CancellationToken cancellationToken = default);
}

public sealed class DigitalWorkforceWorksService(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IDigitalWorkforceWorksService
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public Task<WorksWorkspaceSnapshot> GetWorkspaceAsync(
        string? queue,
        string? search,
        int limit,
        string? subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(queue, search, limit, subjectId, ct),
            cancellationToken);

    public Task<WorksCaseDetailSnapshot> GetCaseDetailAsync(
        Guid caseId,
        string? subjectId,
        bool canDecideApproval,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetCaseDetailCoreAsync(caseId, subjectId, canDecideApproval, ct),
            cancellationToken);

    private async Task<WorksWorkspaceSnapshot> GetWorkspaceCoreAsync(
        string? queue,
        string? search,
        int limit,
        string? subjectId,
        CancellationToken cancellationToken)
    {
        var activeQueue = NormalizeQueue(queue);
        var boundedLimit = Math.Clamp(limit, 1, 100);
        var nowUtc = DateTimeOffset.UtcNow;
        var cases = await LoadCasesAsync(search, cancellationToken);
        var scenarioPhases = await LoadScenarioPhasesAsync(cases.Select(c => c.Id).ToList(), cancellationToken);

        var items = cases
            .Select(c => ProjectListItem(c, scenarioPhases.GetValueOrDefault(c.Id), nowUtc))
            .ToList();

        var counts = new WorksQueueCounts(
            Assigned: CountQueue(cases, WorksExperienceQueues.Assigned, nowUtc, subjectId, scenarioPhases),
            Team: CountQueue(cases, WorksExperienceQueues.Team, nowUtc, subjectId, scenarioPhases),
            PendingApproval: CountQueue(cases, WorksExperienceQueues.PendingApproval, nowUtc, subjectId, scenarioPhases),
            Critical: CountQueue(cases, WorksExperienceQueues.Critical, nowUtc, subjectId, scenarioPhases),
            Blocked: CountQueue(cases, WorksExperienceQueues.Blocked, nowUtc, subjectId, scenarioPhases),
            InProgress: CountQueue(cases, WorksExperienceQueues.InProgress, nowUtc, subjectId, scenarioPhases),
            Observing: CountQueue(cases, WorksExperienceQueues.Observing, nowUtc, subjectId, scenarioPhases),
            VerificationPending: CountQueue(cases, WorksExperienceQueues.VerificationPending, nowUtc, subjectId, scenarioPhases),
            CompletedToday: CountQueue(cases, WorksExperienceQueues.CompletedToday, nowUtc, subjectId, scenarioPhases));

        var searchCaseId = Guid.TryParse(search?.Trim(), out var parsedCaseId) ? parsedCaseId : (Guid?)null;

        var filtered = cases
            .Where(c => WorksExperienceProjection.MatchesQueue(
                c,
                activeQueue,
                nowUtc,
                IsAssignedToSubject(c, subjectId),
                scenarioPhases.GetValueOrDefault(c.Id)) ||
                (searchCaseId.HasValue && c.Id == searchCaseId.Value))
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(boundedLimit)
            .Select(c => ProjectListItem(c, scenarioPhases.GetValueOrDefault(c.Id), nowUtc))
            .ToList();

        return new WorksWorkspaceSnapshot(counts, filtered, activeQueue, items.Count);
    }

    private async Task<WorksCaseDetailSnapshot> GetCaseDetailCoreAsync(
        Guid caseId,
        string? subjectId,
        bool canDecideApproval,
        CancellationToken cancellationToken)
    {
        var workCase = await dbContext.DigitalWorkCases
            .Include(c => c.Participants)
            .Include(c => c.Verifications)
            .Include(c => c.Approvals)
            .Include(c => c.Activities)
            .Include(c => c.Handoffs)
            .Include(c => c.PrimaryWorker)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Work case not found.");

        var nowUtc = DateTimeOffset.UtcNow;
        var scenarioRun = await dbContext.ScenarioRecoveryRunRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.DigitalWorkCaseId == caseId, cancellationToken);
        var scenarioPhase = scenarioRun?.CurrentPhase;
        var summary = ProjectListItem(workCase, scenarioPhase, nowUtc);
        var pendingApproval = workCase.Approvals
            .Where(a => string.Equals(a.Status, DigitalWorkApprovalStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new WorksApprovalPanelItem(
                a.Id,
                a.Action,
                a.BusinessReason,
                a.Impact,
                a.RiskLevel,
                a.Status,
                a.ConcurrencyToken))
            .FirstOrDefault();

        var blocker = WorksExperienceProjection.IsBlocked(workCase)
            ? new WorksBlockerPanel(
                workCase.CurrentBlockerCode,
                ResolveBlockerLabel(workCase.CurrentBlockerCode),
                workCase.BlockerSinceUtc,
                workCase.BusinessImpact)
            : null;

        var dependencies = ParseDependencies(workCase.DependencyGraphJson);
        var relatedWork = await LoadRelatedWorkAsync(workCase, cancellationToken);
        var timeline = WorksExperienceProjection.BuildTimeline(workCase);
        var verification = WorksExperienceProjection.ParseVerificationSummary(workCase);
        var scenarioContext = BuildScenarioContext(workCase, scenarioRun);
        if (verification is null && scenarioContext is not null)
        {
            verification = new WorksVerificationSummary(
                WorksExperienceProjection.MapOutcomeLabel(
                    scenarioContext.VerificationOutcome ?? workCase.Outcome,
                    workCase.VerificationState),
                scenarioContext.BaselineThroughput,
                scenarioContext.ActualThroughput,
                workCase.VerificationState,
                workCase.VerifiedAtUtc,
                workCase.PrimaryWorker?.Name);
        }
        var evidence = BuildEvidence(workCase, verification);
        var permittedActions = ResolvePermittedActions(workCase, pendingApproval, canDecideApproval);

        return new WorksCaseDetailSnapshot(
            summary,
            workCase.Description,
            string.IsNullOrWhiteSpace(workCase.BusinessImpact) ? summary.ProblemSummary : workCase.BusinessImpact,
            WorksExperienceProjection.ResolveNextStep(workCase, pendingApproval is not null),
            pendingApproval?.Action,
            pendingApproval,
            blocker,
            dependencies,
            relatedWork,
            timeline,
            verification,
            scenarioContext,
            evidence,
            permittedActions,
            workCase.FacilityId.HasValue ? "/fabrika" : null,
            workCase.PrimaryDomain is "planning" or "production" ? "/planlama" : null,
            scenarioContext?.ScenarioHref,
            $"/dijital-is-gucu/{workCase.PrimaryWorkerId:N}?case={workCase.Id:N}");
    }

    private async Task<IReadOnlyList<DigitalWorkCase>> LoadCasesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkCases.AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Include(c => c.Approvals)
            .Include(c => c.Participants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (Guid.TryParse(term, out var caseId))
            {
                query = query.Where(c => c.Id == caseId);
            }
            else
            {
                query = query.Where(c =>
                    c.Title.Contains(term) ||
                    c.Description.Contains(term) ||
                    (c.CorrelationKey != null && c.CorrelationKey.Contains(term)));
            }
        }

        return await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> LoadScenarioPhasesAsync(
        List<Guid> caseIds,
        CancellationToken cancellationToken)
    {
        if (caseIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await dbContext.ScenarioRecoveryRunRecords.AsNoTracking()
            .Where(r => r.DigitalWorkCaseId.HasValue && caseIds.Contains(r.DigitalWorkCaseId.Value))
            .Select(r => new { r.DigitalWorkCaseId, r.CurrentPhase })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => r.DigitalWorkCaseId.HasValue)
            .GroupBy(r => r.DigitalWorkCaseId!.Value)
            .ToDictionary(g => g.Key, g => g.First().CurrentPhase);
    }

    private static int CountQueue(
        IReadOnlyList<DigitalWorkCase> cases,
        string queue,
        DateTimeOffset nowUtc,
        string? subjectId,
        IReadOnlyDictionary<Guid, string> scenarioPhases) =>
        cases.Count(c => WorksExperienceProjection.MatchesQueue(
            c,
            queue,
            nowUtc,
            IsAssignedToSubject(c, subjectId),
            scenarioPhases.GetValueOrDefault(c.Id)));

    private static bool IsAssignedToSubject(DigitalWorkCase workCase, string? subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return false;
        }

        if (workCase.IsHumanControlled)
        {
            return true;
        }

        if (workCase.MetadataJson.Contains(subjectId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return workCase.Participants.Any(p =>
            p.Role is DigitalWorkforceParticipantRoles.Primary or DigitalWorkforceParticipantRoles.Coordinator);
    }

    private static WorksListItem ProjectListItem(DigitalWorkCase workCase, string? scenarioPhase, DateTimeOffset nowUtc)
    {
        var pendingApproval = WorksExperienceProjection.HasPendingApproval(workCase, workCase.Approvals);
        var displayState = WorksExperienceProjection.MapDisplayState(workCase, scenarioPhase);
        var slaLabel = workCase.TargetAtUtc.HasValue
            ? workCase.TargetAtUtc.Value < nowUtc
                ? $"Gecikmiş · {workCase.TargetAtUtc.Value.ToString("g", TrCulture)}"
                : workCase.TargetAtUtc.Value.ToString("g", TrCulture)
            : null;

        return new WorksListItem(
            workCase.Id,
            workCase.Title,
            string.IsNullOrWhiteSpace(workCase.Description) ? workCase.Title : workCase.Description,
            displayState,
            WorksExperienceProjection.MapPriorityLabel(workCase.Severity),
            WorksExperienceProjection.MapWorkTypeLabel(workCase),
            workCase.FacilityId.HasValue ? "Pilot tesis" : "Tesis",
            workCase.IsHumanControlled ? "İnsan kontrolü" : "Operasyon ekibi",
            workCase.PrimaryWorker?.Name ?? "Dijital çalışan",
            slaLabel,
            pendingApproval,
            WorksExperienceProjection.IsBlocked(workCase),
            WorksExperienceProjection.IsBlocked(workCase) ? ResolveBlockerLabel(workCase.CurrentBlockerCode) : null,
            string.IsNullOrWhiteSpace(workCase.BusinessImpact) ? null : workCase.BusinessImpact,
            workCase.UpdatedAtUtc.ToString("g", TrCulture),
            workCase.UpdatedAtUtc,
            workCase.Status,
            workCase.VerificationState,
            WorksExperienceProjection.MapOutcomeLabel(workCase.Outcome, workCase.VerificationState),
            workCase.Severity,
            workCase.PrimaryDomain,
            workCase.ConcurrencyToken);
    }

    private static string ResolveBlockerLabel(string blockerCode) => blockerCode switch
    {
        "dependency-wait" => "Bağımlılık bekleniyor",
        "approval-wait" => "Onay bekleniyor",
        "resource-conflict" => "Kaynak çakışması",
        _ => blockerCode,
    };

    private static List<WorksDependencyItem> ParseDependencies(string dependencyGraphJson)
    {
        if (string.IsNullOrWhiteSpace(dependencyGraphJson) || dependencyGraphJson == "{}")
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(dependencyGraphJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement.EnumerateArray()
                .Select(item => new WorksDependencyItem(
                    item.TryGetProperty("label", out var label) ? label.GetString() ?? "Bağımlılık" : "Bağımlılık",
                    item.TryGetProperty("relation", out var relation) ? relation.GetString() ?? "requires" : "requires",
                    item.TryGetProperty("detail", out var detail) ? detail.GetString() ?? string.Empty : string.Empty))
                .Take(10)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<WorksRelatedWorkItem>> LoadRelatedWorkAsync(
        DigitalWorkCase workCase,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DigitalWorkCases.AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Where(c => c.Id != workCase.Id);

        if (!string.IsNullOrWhiteSpace(workCase.CorrelationKey))
        {
            query = query.Where(c => c.CorrelationKey == workCase.CorrelationKey);
        }
        else if (workCase.ParentCaseId.HasValue)
        {
            query = query.Where(c =>
                c.Id == workCase.ParentCaseId ||
                c.ParentCaseId == workCase.Id ||
                c.OrchestrationRootCaseId == workCase.OrchestrationRootCaseId);
        }
        else
        {
            query = query.Where(c => c.PrimaryDomain == workCase.PrimaryDomain && c.PrimaryWorkerId == workCase.PrimaryWorkerId);
        }

        var related = await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(8)
            .ToListAsync(cancellationToken);

        return related
            .Select(c => new WorksRelatedWorkItem(
                c.Id,
                c.Title,
                c.ParentCaseId == workCase.Id ? "Bağımlı iş" : "İlgili iş",
                WorksExperienceProjection.MapDisplayState(c)))
            .ToList();
    }

    private static WorksScenarioContextPanel? BuildScenarioContext(
        DigitalWorkCase workCase,
        ScenarioRecoveryRunRecord? scenarioRun)
    {
        if (scenarioRun is null && workCase.PrimaryDomain != "scenario-recovery")
        {
            return null;
        }

        if (scenarioRun is null)
        {
            return null;
        }

        ScenarioRecoveryVerificationSnapshot? verification = null;
        if (!string.IsNullOrWhiteSpace(scenarioRun.VerificationJson) && scenarioRun.VerificationJson != "{}")
        {
            verification = JsonSerializer.Deserialize<ScenarioRecoveryVerificationSnapshot>(scenarioRun.VerificationJson);
        }

        return new WorksScenarioContextPanel(
            scenarioRun.Id,
            scenarioRun.LineId.ToString(),
            scenarioRun.CurrentPhase,
            scenarioRun.SelectedOptionLabel,
            verification?.Outcome,
            verification?.Baseline.GoodThroughputPerHour,
            verification?.PredictedGoodThroughputPerHour,
            verification?.Actual?.GoodThroughputPerHour,
            $"/senaryo-kurtarma?entryPoint=ISLER&runId={scenarioRun.Id:N}");
    }

    private static List<WorksEvidenceItem> BuildEvidence(
        DigitalWorkCase workCase,
        WorksVerificationSummary? verification)
    {
        var evidence = workCase.Verifications
            .OrderByDescending(v => v.VerifiedAtUtc ?? DateTimeOffset.MinValue)
            .Take(10)
            .Select(v => new WorksEvidenceItem(
                v.VerificationType,
                "Doğrulama kanıtı",
                $"{v.ExpectedState} → {v.ObservedState}",
                v.VerifiedAtUtc))
            .ToList();

        foreach (var activity in workCase.Activities.OrderByDescending(a => a.CreatedAtUtc).Take(5))
        {
            evidence.Add(new WorksEvidenceItem(
                activity.ActionType,
                activity.Description,
                activity.Result,
                activity.CreatedAtUtc));
        }

        if (verification?.BeforeValue is not null || verification?.AfterValue is not null)
        {
            evidence.Insert(0, new WorksEvidenceItem(
                "measurement",
                "Operasyon ölçümü",
                $"{verification.BeforeValue?.ToString("F0", TrCulture) ?? "—"} → {verification.AfterValue?.ToString("F0", TrCulture) ?? "—"} GOOD/saat",
                verification.VerifiedAtUtc));
        }

        return evidence;
    }

    private static List<string> ResolvePermittedActions(
        DigitalWorkCase workCase,
        WorksApprovalPanelItem? pendingApproval,
        bool canDecideApproval)
    {
        var actions = new List<string> { "open-factory-context" };

        if (workCase.PrimaryDomain == "scenario-recovery")
        {
            actions.Add("open-scenario");
        }

        if (pendingApproval is not null && canDecideApproval)
        {
            actions.AddRange(["approve", "reject", "request-change"]);
        }

        if (string.Equals(workCase.Status, DigitalWorkCaseStatuses.Verifying, StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("verify");
        }

        if (WorksExperienceProjection.IsBlocked(workCase))
        {
            actions.Add("escalate");
        }

        return actions;
    }

    private static string NormalizeQueue(string? queue)
    {
        if (string.IsNullOrWhiteSpace(queue))
        {
            return WorksExperienceQueues.Team;
        }

        return WorksExperienceQueues.All.Contains(queue, StringComparer.OrdinalIgnoreCase)
            ? queue
            : WorksExperienceQueues.Team;
    }
}
