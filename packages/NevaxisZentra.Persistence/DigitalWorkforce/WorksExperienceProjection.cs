using System.Text.Json;

namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class WorksExperienceQueues
{
    public const string Assigned = "assigned";
    public const string Team = "team";
    public const string PendingApproval = "pending-approval";
    public const string Critical = "critical";
    public const string Blocked = "blocked";
    public const string InProgress = "in-progress";
    public const string Observing = "observing";
    public const string VerificationPending = "verification-pending";
    public const string CompletedToday = "completed-today";

    public static readonly IReadOnlyList<string> All =
    [
        Assigned,
        Team,
        PendingApproval,
        Critical,
        Blocked,
        InProgress,
        Observing,
        VerificationPending,
        CompletedToday,
    ];
}

public static class WorksDisplayStates
{
    public const string New = "YENİ";
    public const string Reviewing = "İNCELENİYOR";
    public const string Planning = "PLANLANIYOR";
    public const string AwaitingApproval = "ONAY BEKLİYOR";
    public const string Applying = "UYGULANIYOR";
    public const string Observing = "GÖZLEMLENİYOR";
    public const string Verifying = "DOĞRULANIYOR";
    public const string Completed = "TAMAMLANDI";
    public const string Blocked = "ENGELLENDİ";
    public const string Cancelled = "İPTAL";
}

public static class WorksExperienceProjection
{
    public static string MapDisplayState(DigitalWorkCase workCase, string? scenarioPhase = null)
    {
        if (workCase.AdministrativelyClosed &&
            !string.Equals(workCase.Status, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase))
        {
            return WorksDisplayStates.Cancelled;
        }

        if (IsBlocked(workCase))
        {
            return WorksDisplayStates.Blocked;
        }

        if (string.Equals(scenarioPhase, "OBSERVING", StringComparison.OrdinalIgnoreCase))
        {
            return WorksDisplayStates.Observing;
        }

        return workCase.Status switch
        {
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.Completed,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.AwaitingApproval, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.AwaitingApproval,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Verifying, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.Verifying,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Executing, StringComparison.OrdinalIgnoreCase)
                || string.Equals(workCase.Status, DigitalWorkCaseStatuses.Escalated, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.Applying,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Planning, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.Planning,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Analyzing, StringComparison.OrdinalIgnoreCase)
                || string.Equals(workCase.Status, DigitalWorkCaseStatuses.Deciding, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.Reviewing,
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Detected, StringComparison.OrdinalIgnoreCase)
                => WorksDisplayStates.New,
            _ => WorksDisplayStates.Reviewing,
        };
    }

    public static string MapWorkTypeLabel(DigitalWorkCase workCase) =>
        workCase.PrimaryDomain switch
        {
            "scenario-recovery" => "Üretim Kurtarma",
            "throughput" => "Verimlilik",
            "production" => "Üretim",
            "maintenance" => "Bakım",
            "quality" => "Kalite",
            "material" => "Malzeme",
            "supplier" => "Tedarik",
            "mold" => "Kalıp",
            "planning" => "Planlama",
            _ => "İnceleme",
        };

    public static string MapPriorityLabel(string severity) => severity switch
    {
        "critical" => "Kritik",
        "high" => "Yüksek",
        "medium" => "Orta",
        "low" => "Düşük",
        _ => "Orta",
    };

    public static string MapOutcomeLabel(string outcome, string verificationState)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            if (string.Equals(verificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))
            {
                return "DOĞRULANMIŞ İYİLEŞME";
            }

            if (string.Equals(verificationState, DigitalWorkVerificationResults.Failed, StringComparison.OrdinalIgnoreCase))
            {
                return "DOĞRULANAMADI";
            }

            return string.Empty;
        }

        if (outcome.Contains("VERIFIED_SUCCESS", StringComparison.OrdinalIgnoreCase) ||
            outcome.Contains("verified-improvement", StringComparison.OrdinalIgnoreCase))
        {
            return "DOĞRULANMIŞ İYİLEŞME";
        }

        if (outcome.Contains("PARTIAL", StringComparison.OrdinalIgnoreCase) ||
            outcome.Contains("partial", StringComparison.OrdinalIgnoreCase))
        {
            return "KISMI İYİLEŞME";
        }

        if (outcome.Contains("REGRESSION", StringComparison.OrdinalIgnoreCase) ||
            outcome.Contains("regression", StringComparison.OrdinalIgnoreCase))
        {
            return "GERİLEME";
        }

        if (outcome.Contains("INCONCLUSIVE", StringComparison.OrdinalIgnoreCase) ||
            outcome.Contains("inconclusive", StringComparison.OrdinalIgnoreCase))
        {
            return "BELİRSİZ";
        }

        return outcome;
    }

    public static bool IsBlocked(DigitalWorkCase workCase) =>
        !string.IsNullOrWhiteSpace(workCase.CurrentBlockerCode) &&
        !string.Equals(workCase.CurrentBlockerCode, "none", StringComparison.OrdinalIgnoreCase);

    public static bool HasPendingApproval(DigitalWorkCase workCase, IEnumerable<DigitalWorkApproval> approvals) =>
        string.Equals(workCase.Status, DigitalWorkCaseStatuses.AwaitingApproval, StringComparison.OrdinalIgnoreCase) ||
        approvals.Any(a => string.Equals(a.Status, DigitalWorkApprovalStatuses.Pending, StringComparison.OrdinalIgnoreCase));

    public static bool MatchesQueue(
        DigitalWorkCase workCase,
        string queue,
        DateTimeOffset nowUtc,
        bool assignedToCurrentSubject,
        string? scenarioPhase = null)
    {
        var isClosed = string.Equals(workCase.Status, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase);
        var isCritical = string.Equals(workCase.Severity, "critical", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(workCase.Severity, "high", StringComparison.OrdinalIgnoreCase);
        var blocked = IsBlocked(workCase);
        var pendingApproval = string.Equals(workCase.Status, DigitalWorkCaseStatuses.AwaitingApproval, StringComparison.OrdinalIgnoreCase) ||
                              workCase.RequiresApproval && !isClosed;
        var verifying = string.Equals(workCase.Status, DigitalWorkCaseStatuses.Verifying, StringComparison.OrdinalIgnoreCase) ||
                        (string.Equals(workCase.VerificationState, DigitalWorkVerificationResults.Pending, StringComparison.OrdinalIgnoreCase) &&
                         !isClosed);
        var observing = string.Equals(scenarioPhase, "OBSERVING", StringComparison.OrdinalIgnoreCase);
        var completedToday = isClosed &&
                             ((workCase.ClosedAtUtc.HasValue && workCase.ClosedAtUtc.Value.Date == nowUtc.Date) ||
                              (workCase.VerifiedAtUtc.HasValue && workCase.VerifiedAtUtc.Value.Date == nowUtc.Date));
        var inProgress = !isClosed &&
                         !pendingApproval &&
                         !blocked &&
                         !verifying &&
                         !observing;

        return queue switch
        {
            WorksExperienceQueues.Assigned => assignedToCurrentSubject && !isClosed,
            WorksExperienceQueues.Team => !isClosed,
            WorksExperienceQueues.PendingApproval => pendingApproval && !isClosed,
            WorksExperienceQueues.Critical => isCritical && !isClosed,
            WorksExperienceQueues.Blocked => blocked && !isClosed,
            WorksExperienceQueues.InProgress => inProgress,
            WorksExperienceQueues.Observing => observing && !isClosed,
            WorksExperienceQueues.VerificationPending => verifying && !isClosed,
            WorksExperienceQueues.CompletedToday => completedToday,
            _ => !isClosed,
        };
    }

    public static string ResolveNextStep(DigitalWorkCase workCase, bool hasPendingApproval)
    {
        if (IsBlocked(workCase))
        {
            return "Engeli giderin veya bağımlılığı tamamlayın.";
        }

        if (hasPendingApproval ||
            string.Equals(workCase.Status, DigitalWorkCaseStatuses.AwaitingApproval, StringComparison.OrdinalIgnoreCase))
        {
            return "Onay kararı verin.";
        }

        return workCase.Status switch
        {
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Verifying, StringComparison.OrdinalIgnoreCase)
                => "Doğrulama sonucunu inceleyin.",
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Executing, StringComparison.OrdinalIgnoreCase)
                => "Uygulama ilerlemesini izleyin.",
            _ when string.Equals(workCase.Status, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase)
                => "Kapanış sonucunu ve kanıtları inceleyin.",
            _ => "Durumu inceleyin ve gerekli aksiyonu alın.",
        };
    }

    public static IReadOnlyList<WorksTimelineEntry> BuildTimeline(DigitalWorkCase workCase)
    {
        var entries = new List<WorksTimelineEntry>
        {
            new("created", "İş oluşturuldu", workCase.CreatedAtUtc, workCase.Title),
        };

        if (workCase.StartedAtUtc.HasValue)
        {
            entries.Add(new("started", "İnceleme başladı", workCase.StartedAtUtc.Value, workCase.CurrentStage));
        }

        foreach (var approval in workCase.Approvals.OrderBy(a => a.CreatedAtUtc))
        {
            entries.Add(new(
                approval.Status == DigitalWorkApprovalStatuses.Pending ? "approval-requested" : $"approval-{approval.Status}",
                approval.Status == DigitalWorkApprovalStatuses.Pending ? "Onay istendi" : $"Onay: {approval.Status}",
                approval.CreatedAtUtc,
                approval.BusinessReason));
        }

        foreach (var activity in workCase.Activities.OrderBy(a => a.CreatedAtUtc).Take(20))
        {
            entries.Add(new(
                $"activity-{activity.ActionType}",
                activity.Description,
                activity.CreatedAtUtc,
                activity.Result));
        }

        if (workCase.VerifiedAtUtc.HasValue)
        {
            entries.Add(new("verified", "Doğrulama tamamlandı", workCase.VerifiedAtUtc.Value, workCase.VerificationState));
        }

        if (workCase.ClosedAtUtc.HasValue)
        {
            entries.Add(new("closed", "İş kapatıldı", workCase.ClosedAtUtc.Value, workCase.Outcome));
        }

        return entries.OrderBy(e => e.OccurredAtUtc).ToList();
    }

    public static WorksVerificationSummary? ParseVerificationSummary(DigitalWorkCase workCase)
    {
        if (string.IsNullOrWhiteSpace(workCase.OutcomeVerificationJson) ||
            workCase.OutcomeVerificationJson == "{}")
        {
            if (string.IsNullOrWhiteSpace(workCase.Outcome) &&
                !string.Equals(workCase.VerificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(workCase.VerificationState, DigitalWorkVerificationResults.Failed, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new WorksVerificationSummary(
                MapOutcomeLabel(workCase.Outcome, workCase.VerificationState),
                null,
                null,
                workCase.VerificationState,
                workCase.VerifiedAtUtc,
                workCase.PrimaryWorker?.Name);
        }

        try
        {
            using var document = JsonDocument.Parse(workCase.OutcomeVerificationJson);
            var root = document.RootElement;
            var before = root.TryGetProperty("Baseline", out var baseline)
                ? baseline.TryGetProperty("GoodThroughputPerHour", out var b) ? b.GetDecimal() : (decimal?)null
                : null;
            var after = root.TryGetProperty("Actual", out var actual)
                ? actual.TryGetProperty("GoodThroughputPerHour", out var a) ? a.GetDecimal() : (decimal?)null
                : null;
            var outcome = root.TryGetProperty("Outcome", out var outcomeEl) ? outcomeEl.GetString() : workCase.Outcome;
            return new WorksVerificationSummary(
                MapOutcomeLabel(outcome ?? string.Empty, workCase.VerificationState),
                before,
                after,
                workCase.VerificationState,
                workCase.VerifiedAtUtc,
                workCase.PrimaryWorker?.Name);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record WorksTimelineEntry(
    string Code,
    string Label,
    DateTimeOffset OccurredAtUtc,
    string Detail);

public sealed record WorksVerificationSummary(
    string OutcomeLabel,
    decimal? BeforeValue,
    decimal? AfterValue,
    string VerificationState,
    DateTimeOffset? VerifiedAtUtc,
    string? VerifiedByLabel);
