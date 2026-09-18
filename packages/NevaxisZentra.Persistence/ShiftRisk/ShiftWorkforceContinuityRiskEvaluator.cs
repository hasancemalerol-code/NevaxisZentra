using System.Globalization;
using NevaxisZentra.Persistence.ShiftAssignment;
using NevaxisZentra.Persistence.ShiftWorkforce;

namespace NevaxisZentra.Persistence.ShiftRisk;

internal static class ShiftWorkforceContinuityRiskEvaluator
{
    internal sealed record PersonContext(
        Guid PersonId,
        string EmployeeCode,
        string DisplayName,
        bool IsActive,
        string AvailabilityState,
        string FreshnessState,
        DateTimeOffset? SourceUpdatedAtUtc,
        bool IsPilotData);

    internal sealed record CompetencyContext(
        Guid AssignmentId,
        string CompetencyCode,
        string CompetencyLabel,
        string VerificationStatus,
        DateTimeOffset? ValidFromUtc,
        DateTimeOffset? ValidUntilUtc,
        bool IsPilotData);

    internal sealed record AssignmentContext(
        Guid AssignmentId,
        Guid WorkforcePersonId,
        string Status,
        DateTimeOffset PlannedStartUtc,
        DateTimeOffset PlannedEndUtc,
        string? ProductionOrderReference,
        Guid MachineAssetId,
        string? RequiredCompetencyCode,
        bool IsPilotData);

    internal sealed record CoverageRequirementContext(
        Guid RequirementId,
        string? ProductionOrderReference,
        Guid? MachineAssetId,
        string RequiredCompetencyCode,
        int MinimumApprovedOperatorCount,
        bool IsCriticalRole);

    internal sealed record DriverDraft(
        string ReasonCode,
        string Category,
        string Severity,
        string? RequiredValue,
        string? ObservedValue,
        string? GapValue,
        Guid? WorkforcePersonId,
        Guid? ProductionShiftId,
        string? ProductionOrderReference,
        Guid? MachineAssetId,
        string? RequiredCompetencyCode,
        string EvidenceSource,
        string? EvidenceReference,
        DateTimeOffset? EvidenceTimestampUtc,
        string? FreshnessState);

    internal sealed record EvaluationOutput(
        string Outcome,
        int ApprovedAssignmentCount,
        int PlannedAssignmentMinutes,
        int TrustworthyAvailableCount,
        int CoverageGap,
        string? LeadingDriverCategory,
        string? LeadingReasonCode,
        IReadOnlyList<DriverDraft> Drivers,
        IReadOnlyList<AssignmentContext> ConsideredAssignments);

    internal static EvaluationOutput Evaluate(
        PersonContext person,
        IReadOnlyList<CompetencyContext> competencies,
        IReadOnlyList<AssignmentContext> approvedAssignments,
        IReadOnlyList<CoverageRequirementContext> coverageRequirements,
        Guid productionShiftId,
        DateOnly shiftDate,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        string? requiredCompetencyCode,
        DateTimeOffset nowUtc)
    {
        var drivers = new List<DriverDraft>();
        var considered = approvedAssignments
            .Where(a => a.Status == ShiftAssignmentStatuses.Approved
                        && ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                            a.PlannedStartUtc,
                            a.PlannedEndUtc,
                            windowStartUtc,
                            windowEndUtc))
            .ToList();

        var plannedMinutes = considered.Sum(a =>
            (int)Math.Max(0, (a.PlannedEndUtc - a.PlannedStartUtc).TotalMinutes));

        EvaluatePersonState(person, productionShiftId, requiredCompetencyCode, nowUtc, drivers);
        EvaluateCompetencies(person, competencies, requiredCompetencyCode, nowUtc, drivers);
        EvaluateAssignmentOverlaps(person, considered, productionShiftId, nowUtc, drivers);
        EvaluateWorkloadFacts(person, considered, plannedMinutes, drivers);
        EvaluateCoverageImpact(
            person,
            competencies,
            considered,
            coverageRequirements,
            requiredCompetencyCode,
            productionShiftId,
            nowUtc,
            drivers);

        drivers = DedupeDriversByReasonCode(drivers);
        var trustworthyCount = ComputeTrustworthyAvailable(person, competencies, requiredCompetencyCode, nowUtc);
        var relevantRequirements = FilterRequirements(coverageRequirements, requiredCompetencyCode);
        var maxGap = relevantRequirements.Count == 0
            ? 0
            : relevantRequirements.Max(r => Math.Max(0, r.MinimumApprovedOperatorCount - trustworthyCount));

        var hasFailOrWarn = drivers.Any(d =>
            d.Severity is ShiftRiskDriverSeverities.Fail or ShiftRiskDriverSeverities.Warn);
        var hasUnknownAvailability = drivers.Any(d =>
            d.Category == ShiftWorkforceContinuityDriverCategories.AvailabilityUnknown);
        var hasStaleSource = drivers.Any(d =>
            d.Category == ShiftWorkforceContinuityDriverCategories.SourceStale);

        if (!hasFailOrWarn
            && !hasUnknownAvailability
            && !hasStaleSource
            && considered.Count == 0)
        {
            drivers.Add(new DriverDraft(
                ShiftWorkforceContinuityReasonCodes.ContinuityClear,
                ShiftWorkforceContinuityDriverCategories.CoverageImpact,
                ShiftRiskDriverSeverities.Pass,
                "trustworthy-available",
                trustworthyCount.ToString(CultureInfo.InvariantCulture),
                "0",
                person.PersonId,
                productionShiftId,
                null,
                null,
                requiredCompetencyCode,
                ShiftRiskEvidenceSources.RiskEngine,
                person.PersonId.ToString(),
                nowUtc,
                person.FreshnessState));
        }

        var outcome = ResolveOutcome(drivers, relevantRequirements.Any(r => r.IsCriticalRole), considered.Count);
        var leading = ResolveLeadingDriver(drivers, outcome);

        return new EvaluationOutput(
            outcome,
            considered.Count,
            plannedMinutes,
            trustworthyCount,
            maxGap,
            leading?.Category,
            leading?.ReasonCode,
            drivers,
            considered);
    }

    private static void EvaluatePersonState(
        PersonContext person,
        Guid productionShiftId,
        string? requiredCompetencyCode,
        DateTimeOffset nowUtc,
        List<DriverDraft> drivers)
    {
        if (!person.IsActive)
        {
            drivers.Add(BuildPersonDriver(
                ShiftWorkforceContinuityReasonCodes.InactiveEmployment,
                ShiftWorkforceContinuityDriverCategories.AbsenceImpact,
                ShiftRiskDriverSeverities.Fail,
                "active-employment",
                "inactive",
                person,
                productionShiftId,
                requiredCompetencyCode,
                nowUtc));
            return;
        }

        if (person.AvailabilityState is ShiftWorkforceAvailabilityStates.Leave
            or ShiftWorkforceAvailabilityStates.Absent)
        {
            drivers.Add(BuildPersonDriver(
                ShiftWorkforceContinuityReasonCodes.AbsenceImpact,
                ShiftWorkforceContinuityDriverCategories.AbsenceImpact,
                ShiftRiskDriverSeverities.Fail,
                "available",
                person.AvailabilityState,
                person,
                productionShiftId,
                requiredCompetencyCode,
                nowUtc));
            return;
        }

        if (person.AvailabilityState == ShiftWorkforceAvailabilityStates.Unknown)
        {
            drivers.Add(BuildPersonDriver(
                ShiftWorkforceContinuityReasonCodes.AvailabilityUnknown,
                ShiftWorkforceContinuityDriverCategories.AvailabilityUnknown,
                ShiftRiskDriverSeverities.Info,
                "known-availability",
                person.AvailabilityState,
                person,
                productionShiftId,
                requiredCompetencyCode,
                nowUtc));
        }

        if (person.FreshnessState == ShiftWorkforceFreshnessStates.Stale)
        {
            drivers.Add(BuildPersonDriver(
                ShiftWorkforceContinuityReasonCodes.SourceStale,
                ShiftWorkforceContinuityDriverCategories.SourceStale,
                ShiftRiskDriverSeverities.Info,
                ShiftWorkforceFreshnessStates.Fresh,
                person.FreshnessState,
                person,
                productionShiftId,
                requiredCompetencyCode,
                person.SourceUpdatedAtUtc ?? nowUtc));
        }

        if (person.AvailabilityState == ShiftWorkforceAvailabilityStates.Unknown
            && person.FreshnessState == ShiftWorkforceFreshnessStates.Unknown)
        {
            drivers.Add(BuildPersonDriver(
                ShiftWorkforceContinuityReasonCodes.EvidenceConflict,
                ShiftWorkforceContinuityDriverCategories.EvidenceConflict,
                ShiftRiskDriverSeverities.Info,
                "known-state",
                $"{person.AvailabilityState}|{person.FreshnessState}",
                person,
                productionShiftId,
                requiredCompetencyCode,
                person.SourceUpdatedAtUtc ?? nowUtc));
        }
    }

    private static void EvaluateCompetencies(
        PersonContext person,
        IReadOnlyList<CompetencyContext> competencies,
        string? requiredCompetencyCode,
        DateTimeOffset nowUtc,
        List<DriverDraft> drivers)
    {
        var relevant = string.IsNullOrWhiteSpace(requiredCompetencyCode)
            ? competencies
            : competencies
                .Where(c => string.Equals(c.CompetencyCode, requiredCompetencyCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (relevant.Count == 0 && !string.IsNullOrWhiteSpace(requiredCompetencyCode))
        {
            return;
        }

        foreach (var competency in relevant)
        {
            if (competency.VerificationStatus == ShiftWorkforceVerificationStatuses.PendingVerification)
            {
                drivers.Add(BuildCompetencyDriver(
                    ShiftWorkforceContinuityReasonCodes.CompetencyPendingVerification,
                    ShiftWorkforceContinuityDriverCategories.CompetencyPendingVerification,
                    ShiftRiskDriverSeverities.Warn,
                    "verified",
                    competency.VerificationStatus,
                    person,
                    competency,
                    nowUtc));
            }

            if (!competency.ValidUntilUtc.HasValue)
            {
                continue;
            }

            var validUntil = competency.ValidUntilUtc.Value;
            if (validUntil < nowUtc)
            {
                drivers.Add(BuildCompetencyDriver(
                    ShiftWorkforceContinuityReasonCodes.CompetencyExpired,
                    ShiftWorkforceContinuityDriverCategories.CompetencyExpired,
                    ShiftRiskDriverSeverities.Fail,
                    validUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    nowUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    person,
                    competency,
                    nowUtc));
                continue;
            }

            var daysUntil = (validUntil - nowUtc).TotalDays;
            if (daysUntil <= ShiftWorkforceContinuityConstants.UrgentExpiryDays)
            {
                drivers.Add(BuildCompetencyDriver(
                    ShiftWorkforceContinuityReasonCodes.CompetencyExpiringUrgent,
                    ShiftWorkforceContinuityDriverCategories.CompetencyExpiring,
                    ShiftRiskDriverSeverities.Warn,
                    ShiftWorkforceContinuityConstants.UrgentExpiryDays.ToString(CultureInfo.InvariantCulture),
                    Math.Ceiling(daysUntil).ToString(CultureInfo.InvariantCulture),
                    person,
                    competency,
                    nowUtc));
            }
            else if (daysUntil <= ShiftWorkforceContinuityConstants.UpcomingExpiryDays)
            {
                drivers.Add(BuildCompetencyDriver(
                    ShiftWorkforceContinuityReasonCodes.CompetencyExpiringUpcoming,
                    ShiftWorkforceContinuityDriverCategories.CompetencyExpiring,
                    ShiftRiskDriverSeverities.Info,
                    ShiftWorkforceContinuityConstants.UpcomingExpiryDays.ToString(CultureInfo.InvariantCulture),
                    Math.Ceiling(daysUntil).ToString(CultureInfo.InvariantCulture),
                    person,
                    competency,
                    nowUtc));
            }
        }
    }

    private static void EvaluateAssignmentOverlaps(
        PersonContext person,
        List<AssignmentContext> assignments,
        Guid productionShiftId,
        DateTimeOffset nowUtc,
        List<DriverDraft> drivers)
    {
        for (var i = 0; i < assignments.Count; i++)
        {
            for (var j = i + 1; j < assignments.Count; j++)
            {
                var first = assignments[i];
                var second = assignments[j];
                if (!ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                        first.PlannedStartUtc,
                        first.PlannedEndUtc,
                        second.PlannedStartUtc,
                        second.PlannedEndUtc))
                {
                    continue;
                }

                drivers.Add(new DriverDraft(
                    ShiftWorkforceContinuityReasonCodes.AssignmentOverlap,
                    ShiftWorkforceContinuityDriverCategories.AssignmentOverlap,
                    ShiftRiskDriverSeverities.Warn,
                    "no-overlap",
                    $"{first.AssignmentId},{second.AssignmentId}",
                    null,
                    person.PersonId,
                    productionShiftId,
                    first.ProductionOrderReference ?? second.ProductionOrderReference,
                    first.MachineAssetId,
                    first.RequiredCompetencyCode ?? second.RequiredCompetencyCode,
                    ShiftRiskEvidenceSources.AssignmentRegistry,
                    $"{first.AssignmentId}|{second.AssignmentId}",
                    nowUtc,
                    null));
            }
        }
    }

    private static void EvaluateWorkloadFacts(
        PersonContext person,
        List<AssignmentContext> assignments,
        int plannedMinutes,
        List<DriverDraft> drivers)
    {
        drivers.Add(new DriverDraft(
            ShiftWorkforceContinuityReasonCodes.AssignmentLoadFacts,
            ShiftWorkforceContinuityDriverCategories.AssignmentLoadPressure,
            ShiftRiskDriverSeverities.Info,
            "approved-assignment-count",
            assignments.Count.ToString(CultureInfo.InvariantCulture),
            plannedMinutes.ToString(CultureInfo.InvariantCulture),
            person.PersonId,
            null,
            null,
            null,
            null,
            ShiftRiskEvidenceSources.AssignmentRegistry,
            person.PersonId.ToString(),
            DateTimeOffset.UtcNow,
            null));

        drivers.Add(new DriverDraft(
            ShiftWorkforceContinuityReasonCodes.AssignmentLoadThresholdMissing,
            ShiftWorkforceContinuityDriverCategories.AssignmentLoadPressure,
            ShiftRiskDriverSeverities.Info,
            "authoritative-capacity-threshold",
            null,
            null,
            person.PersonId,
            null,
            null,
            null,
            null,
            ShiftRiskEvidenceSources.RiskEngine,
            person.PersonId.ToString(),
            DateTimeOffset.UtcNow,
            null));
    }

    private static void EvaluateCoverageImpact(
        PersonContext person,
        IReadOnlyList<CompetencyContext> competencies,
        List<AssignmentContext> assignments,
        IReadOnlyList<CoverageRequirementContext> requirements,
        string? requiredCompetencyCode,
        Guid productionShiftId,
        DateTimeOffset nowUtc,
        List<DriverDraft> drivers)
    {
        var relevant = FilterRequirements(requirements, requiredCompetencyCode);
        if (relevant.Count == 0)
        {
            return;
        }

        var trustworthy = ComputeTrustworthyAvailable(person, competencies, requiredCompetencyCode, nowUtc);
        foreach (var requirement in relevant)
        {
            var gap = Math.Max(0, requirement.MinimumApprovedOperatorCount - trustworthy);
            if (gap <= 0)
            {
                continue;
            }

            drivers.Add(new DriverDraft(
                ShiftWorkforceContinuityReasonCodes.CoverageImpact,
                ShiftWorkforceContinuityDriverCategories.CoverageImpact,
                requirement.IsCriticalRole ? ShiftRiskDriverSeverities.Fail : ShiftRiskDriverSeverities.Warn,
                requirement.MinimumApprovedOperatorCount.ToString(CultureInfo.InvariantCulture),
                trustworthy.ToString(CultureInfo.InvariantCulture),
                gap.ToString(CultureInfo.InvariantCulture),
                person.PersonId,
                productionShiftId,
                requirement.ProductionOrderReference,
                requirement.MachineAssetId,
                requirement.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.CoverageRequirement,
                requirement.RequirementId.ToString(),
                nowUtc,
                null));
        }

        _ = assignments;
    }

    private static int ComputeTrustworthyAvailable(
        PersonContext person,
        IReadOnlyList<CompetencyContext> competencies,
        string? requiredCompetencyCode,
        DateTimeOffset nowUtc)
    {
        if (!person.IsActive)
        {
            return 0;
        }

        if (person.AvailabilityState is ShiftWorkforceAvailabilityStates.Leave
            or ShiftWorkforceAvailabilityStates.Absent
            or ShiftWorkforceAvailabilityStates.Unknown)
        {
            return 0;
        }

        if (person.FreshnessState == ShiftWorkforceFreshnessStates.Stale)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(requiredCompetencyCode))
        {
            var competency = competencies.FirstOrDefault(c =>
                string.Equals(c.CompetencyCode, requiredCompetencyCode, StringComparison.OrdinalIgnoreCase));
            if (competency is null)
            {
                return 0;
            }

            if (competency.VerificationStatus == ShiftWorkforceVerificationStatuses.PendingVerification)
            {
                return 0;
            }

            if (competency.ValidUntilUtc.HasValue && competency.ValidUntilUtc.Value < nowUtc)
            {
                return 0;
            }
        }

        return 1;
    }

    private static List<CoverageRequirementContext> FilterRequirements(
        IReadOnlyList<CoverageRequirementContext> requirements,
        string? requiredCompetencyCode) =>
        string.IsNullOrWhiteSpace(requiredCompetencyCode)
            ? requirements.ToList()
            : requirements
                .Where(r => string.Equals(
                    r.RequiredCompetencyCode,
                    requiredCompetencyCode,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

    private static DriverDraft BuildPersonDriver(
        string reasonCode,
        string category,
        string severity,
        string requiredValue,
        string observedValue,
        PersonContext person,
        Guid productionShiftId,
        string? requiredCompetencyCode,
        DateTimeOffset evidenceTimestampUtc) =>
        new(
            reasonCode,
            category,
            severity,
            requiredValue,
            observedValue,
            null,
            person.PersonId,
            productionShiftId,
            null,
            null,
            requiredCompetencyCode,
            ShiftRiskEvidenceSources.ShiftWorkforce,
            person.PersonId.ToString(),
            evidenceTimestampUtc,
            person.FreshnessState);

    private static DriverDraft BuildCompetencyDriver(
        string reasonCode,
        string category,
        string severity,
        string requiredValue,
        string observedValue,
        PersonContext person,
        CompetencyContext competency,
        DateTimeOffset nowUtc) =>
        new(
            reasonCode,
            category,
            severity,
            requiredValue,
            observedValue,
            null,
            person.PersonId,
            null,
            null,
            null,
            competency.CompetencyCode,
            ShiftRiskEvidenceSources.ShiftWorkforce,
            competency.AssignmentId.ToString(),
            competency.ValidUntilUtc ?? nowUtc,
            null);

    private static List<DriverDraft> DedupeDriversByReasonCode(List<DriverDraft> drivers)
    {
        var preferredByReason = new Dictionary<string, DriverDraft>(StringComparer.Ordinal);
        foreach (var driver in drivers)
        {
            if (!preferredByReason.TryGetValue(driver.ReasonCode, out var existing)
                || SeverityRank(driver.Severity) > SeverityRank(existing.Severity))
            {
                preferredByReason[driver.ReasonCode] = driver;
            }
        }

        var seenReasons = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<DriverDraft>();
        foreach (var driver in drivers)
        {
            if (seenReasons.Contains(driver.ReasonCode))
            {
                continue;
            }

            deduped.Add(preferredByReason[driver.ReasonCode]);
            seenReasons.Add(driver.ReasonCode);
        }

        return deduped;
    }

    private static int SeverityRank(string severity) =>
        severity switch
        {
            ShiftRiskDriverSeverities.Fail => 4,
            ShiftRiskDriverSeverities.Warn => 3,
            ShiftRiskDriverSeverities.Info => 2,
            ShiftRiskDriverSeverities.Pass => 1,
            _ => 0,
        };

    private static string ResolveOutcome(List<DriverDraft> drivers, bool hasCriticalRequirement, int approvedAssignmentCount)
    {
        if (drivers.Any(d => d.Category == ShiftWorkforceContinuityDriverCategories.AvailabilityUnknown)
            && !drivers.Any(d => d.Severity is ShiftRiskDriverSeverities.Fail or ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d =>
                d.Category is ShiftWorkforceContinuityDriverCategories.SourceStale
                    && d.Severity == ShiftRiskDriverSeverities.Info)
            && !drivers.Any(d => d.Severity is ShiftRiskDriverSeverities.Fail or ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.Category == ShiftWorkforceContinuityDriverCategories.CompetencyExpired))
        {
            return hasCriticalRequirement ? ShiftRiskOutcomes.Critical : ShiftRiskOutcomes.Attention;
        }

        if (drivers.Any(d => d.Severity == ShiftRiskDriverSeverities.Fail))
        {
            return hasCriticalRequirement ? ShiftRiskOutcomes.Critical : ShiftRiskOutcomes.Attention;
        }

        if (drivers.Any(d => d.Severity == ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.Attention;
        }

        if (drivers.Any(d => d.Category == ShiftWorkforceContinuityDriverCategories.CompetencyExpiring))
        {
            return ShiftRiskOutcomes.Attention;
        }

        if (drivers.Any(d =>
                d.ReasonCode == ShiftWorkforceContinuityReasonCodes.AssignmentLoadThresholdMissing)
            && approvedAssignmentCount > 0
            && !drivers.Any(d => d.Severity is ShiftRiskDriverSeverities.Fail or ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.ReasonCode == ShiftWorkforceContinuityReasonCodes.ContinuityClear))
        {
            return ShiftRiskOutcomes.Clear;
        }

        if (drivers.Count == 0
            || drivers.All(d => d.Severity is ShiftRiskDriverSeverities.Pass or ShiftRiskDriverSeverities.Info))
        {
            return ShiftRiskOutcomes.Clear;
        }

        return ShiftRiskOutcomes.Attention;
    }

    private static DriverDraft? ResolveLeadingDriver(List<DriverDraft> drivers, string outcome)
    {
        if (outcome == ShiftRiskOutcomes.Clear)
        {
            return drivers.FirstOrDefault(d => d.ReasonCode == ShiftWorkforceContinuityReasonCodes.ContinuityClear);
        }

        foreach (var category in ShiftWorkforceContinuityLeadingRiskPriority.CategoryOrder)
        {
            var match = drivers.FirstOrDefault(d => d.Category == category);
            if (match is not null)
            {
                return match;
            }
        }

        return drivers.Count > 0 ? drivers[0] : null;
    }
}
