using System.Globalization;
using CrevaFactOS.Persistence.ShiftAssignment;
using CrevaFactOS.Persistence.ShiftWorkforce;

namespace CrevaFactOS.Persistence.ShiftRisk;

internal static class ShiftCoverageRiskEvaluator
{
    internal sealed record AssignmentEvidence(
        Guid AssignmentId,
        Guid WorkforcePersonId,
        string EmployeeCode,
        string DisplayName,
        string Status,
        DateTimeOffset PlannedStartUtc,
        DateTimeOffset PlannedEndUtc,
        string? RequiredCompetencyCode,
        Guid MachineAssetId,
        string? ProductionOrderReference,
        Guid EligibilityResultId,
        string? EligibilityOutcome,
        string? EligibilityPrimaryReasonCode,
        bool IsPilotData);

    internal sealed record PersonEvidence(
        Guid PersonId,
        string EmployeeCode,
        string DisplayName,
        bool IsActive,
        string AvailabilityState,
        string FreshnessState,
        DateTimeOffset? SourceUpdatedAtUtc,
        bool IsPilotData);

    internal sealed record RequirementSnapshot(
        Guid Id,
        Guid ProductionShiftId,
        DateOnly ShiftDate,
        DateTimeOffset PlannedStartUtc,
        DateTimeOffset PlannedEndUtc,
        string? ProductionOrderReference,
        Guid? MachineAssetId,
        string? MachineFamilyScope,
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
        int ApprovedEligibleCount,
        int CoverageGap,
        string? LeadingDriverCategory,
        string? LeadingReasonCode,
        IReadOnlyList<DriverDraft> Drivers,
        IReadOnlyList<AssignmentEvidence> CountedAssignments,
        IReadOnlyList<(AssignmentEvidence Assignment, string ExclusionReasonCode)> ExcludedAssignments);

  internal static EvaluationOutput Evaluate(
        RequirementSnapshot? requirement,
        ShiftRiskEvaluationContext context,
        IReadOnlyList<AssignmentEvidence> assignments,
        IReadOnlyDictionary<Guid, PersonEvidence> persons,
        DateTimeOffset nowUtc)
    {
        if (requirement is null)
        {
            var missingDriver = new DriverDraft(
                ShiftRiskReasonCodes.RequirementMissing,
                ShiftRiskDriverCategories.RequirementMissing,
                ShiftRiskDriverSeverities.Info,
                "explicit-requirement",
                null,
                null,
                context.ProductionShiftId,
                context.ProductionOrderReference,
                context.MachineAssetId,
                context.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.RiskEngine,
                null,
                nowUtc,
                null);

            return new EvaluationOutput(
                ShiftRiskOutcomes.InsufficientData,
                0,
                0,
                ShiftRiskDriverCategories.RequirementMissing,
                ShiftRiskReasonCodes.RequirementMissing,
                [missingDriver],
                [],
                []);
        }

        var drivers = new List<DriverDraft>();
        var counted = new List<AssignmentEvidence>();
        var excluded = new List<(AssignmentEvidence, string)>();
        var countedPersonIds = new HashSet<Guid>();
        var overlapPairs = new List<(AssignmentEvidence First, AssignmentEvidence Second)>();

        foreach (var assignment in assignments)
        {
            var exclusion = GetExclusionReason(requirement, context, assignment, countedPersonIds);
            if (exclusion is not null)
            {
                excluded.Add((assignment, exclusion));
                continue;
            }

            var person = persons.GetValueOrDefault(assignment.WorkforcePersonId);
            if (person is null)
            {
                excluded.Add((assignment, ShiftRiskReasonCodes.EligibilityInsufficient));
                continue;
            }

            var eligibilityIssue = EvaluatePersonEligibility(person, assignment, nowUtc, drivers);
            if (eligibilityIssue is not null)
            {
                excluded.Add((assignment, eligibilityIssue));
                if (eligibilityIssue != ShiftRiskReasonCodes.SourceStale)
                {
                    drivers.Add(BuildAssignmentIneligibleDriver(requirement, assignment, person, eligibilityIssue, nowUtc));
                }
                continue;
            }

            foreach (var countedAssignment in counted)
            {
                if (ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                        assignment.PlannedStartUtc,
                        assignment.PlannedEndUtc,
                        countedAssignment.PlannedStartUtc,
                        countedAssignment.PlannedEndUtc)
                    && assignment.WorkforcePersonId == countedAssignment.WorkforcePersonId)
                {
                    overlapPairs.Add((countedAssignment, assignment));
                }
            }

            countedPersonIds.Add(assignment.WorkforcePersonId);
            counted.Add(assignment);
        }

        foreach (var (first, second) in overlapPairs)
        {
            drivers.Add(new DriverDraft(
                ShiftRiskReasonCodes.AssignmentConflict,
                ShiftRiskDriverCategories.AssignmentConflict,
                requirement.IsCriticalRole ? ShiftRiskDriverSeverities.Fail : ShiftRiskDriverSeverities.Warn,
                "no-overlap",
                $"{first.AssignmentId},{second.AssignmentId}",
                null,
                requirement.ProductionShiftId,
                requirement.ProductionOrderReference,
                requirement.MachineAssetId,
                requirement.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.AssignmentRegistry,
                $"{first.AssignmentId}|{second.AssignmentId}",
                nowUtc,
                null));
        }

        DetectDraftApprovedOverlapConflicts(
            requirement,
            context,
            assignments,
            overlapPairs,
            drivers,
            nowUtc);

        var approvedEligibleCount = counted.Count;
        var gap = Math.Max(0, requirement.MinimumApprovedOperatorCount - approvedEligibleCount);

        if (gap > 0)
        {
            if (requirement.IsCriticalRole)
            {
                drivers.Add(new DriverDraft(
                    ShiftRiskReasonCodes.CriticalRoleGap,
                    ShiftRiskDriverCategories.CriticalRoleGap,
                    ShiftRiskDriverSeverities.Fail,
                    requirement.MinimumApprovedOperatorCount.ToString(CultureInfo.InvariantCulture),
                    approvedEligibleCount.ToString(CultureInfo.InvariantCulture),
                    gap.ToString(CultureInfo.InvariantCulture),
                    requirement.ProductionShiftId,
                    requirement.ProductionOrderReference,
                    requirement.MachineAssetId,
                    requirement.RequiredCompetencyCode,
                    ShiftRiskEvidenceSources.CoverageRequirement,
                    requirement.Id.ToString(),
                    nowUtc,
                    null));
            }
            else
            {
                drivers.Add(new DriverDraft(
                    ShiftRiskReasonCodes.CoverageGap,
                    ShiftRiskDriverCategories.CoverageGap,
                    ShiftRiskDriverSeverities.Warn,
                    requirement.MinimumApprovedOperatorCount.ToString(CultureInfo.InvariantCulture),
                    approvedEligibleCount.ToString(CultureInfo.InvariantCulture),
                    gap.ToString(CultureInfo.InvariantCulture),
                    requirement.ProductionShiftId,
                    requirement.ProductionOrderReference,
                    requirement.MachineAssetId,
                    requirement.RequiredCompetencyCode,
                    ShiftRiskEvidenceSources.CoverageRequirement,
                    requirement.Id.ToString(),
                    nowUtc,
                    null));
            }
        }
        else if (drivers.All(d => d.Severity is ShiftRiskDriverSeverities.Pass or ShiftRiskDriverSeverities.Info))
        {
            drivers.Add(new DriverDraft(
                ShiftRiskReasonCodes.CoverageSatisfied,
                ShiftRiskDriverCategories.CoverageGap,
                ShiftRiskDriverSeverities.Pass,
                requirement.MinimumApprovedOperatorCount.ToString(CultureInfo.InvariantCulture),
                approvedEligibleCount.ToString(CultureInfo.InvariantCulture),
                "0",
                requirement.ProductionShiftId,
                requirement.ProductionOrderReference,
                requirement.MachineAssetId,
                requirement.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.RiskEngine,
                requirement.Id.ToString(),
                nowUtc,
                null));
        }

        drivers = DedupeDriversByReasonCode(drivers);

        var outcome = ResolveOutcome(drivers, requirement.IsCriticalRole);
        var leading = ResolveLeadingDriver(drivers, outcome);

        return new EvaluationOutput(
            outcome,
            approvedEligibleCount,
            gap,
            leading?.Category,
            leading?.ReasonCode,
            drivers,
            counted,
            excluded);
    }

    private static void DetectDraftApprovedOverlapConflicts(
        RequirementSnapshot requirement,
        ShiftRiskEvaluationContext context,
        IReadOnlyList<AssignmentEvidence> assignments,
        List<(AssignmentEvidence First, AssignmentEvidence Second)> overlapPairs,
        List<DriverDraft> drivers,
        DateTimeOffset nowUtc)
    {
        var approved = assignments
            .Where(a => a.Status == ShiftAssignmentStatuses.Approved)
            .ToList();
        var drafts = assignments
            .Where(a => a.Status == ShiftAssignmentStatuses.Draft)
            .ToList();

        foreach (var draft in drafts)
        {
            if (!MatchesRequirementScope(requirement, draft, context))
            {
                continue;
            }

            foreach (var approvedAssignment in approved)
            {
                if (draft.WorkforcePersonId != approvedAssignment.WorkforcePersonId)
                {
                    continue;
                }

                if (!ShiftOperatorEligibilityEvaluator.WindowsOverlap(
                        draft.PlannedStartUtc,
                        draft.PlannedEndUtc,
                        approvedAssignment.PlannedStartUtc,
                        approvedAssignment.PlannedEndUtc))
                {
                    continue;
                }

                var pairKey = $"{draft.AssignmentId}|{approvedAssignment.AssignmentId}";
                if (overlapPairs.Any(p =>
                        (p.First.AssignmentId == draft.AssignmentId && p.Second.AssignmentId == approvedAssignment.AssignmentId)
                        || (p.First.AssignmentId == approvedAssignment.AssignmentId && p.Second.AssignmentId == draft.AssignmentId)))
                {
                    continue;
                }

                if (drivers.Any(d =>
                        d.ReasonCode == ShiftRiskReasonCodes.AssignmentConflict
                        && d.EvidenceReference == pairKey))
                {
                    continue;
                }

                drivers.Add(new DriverDraft(
                    ShiftRiskReasonCodes.AssignmentConflict,
                    ShiftRiskDriverCategories.AssignmentConflict,
                    requirement.IsCriticalRole ? ShiftRiskDriverSeverities.Fail : ShiftRiskDriverSeverities.Warn,
                    "no-overlap",
                    $"{draft.AssignmentId},{approvedAssignment.AssignmentId}",
                    null,
                    requirement.ProductionShiftId,
                    requirement.ProductionOrderReference,
                    requirement.MachineAssetId,
                    requirement.RequiredCompetencyCode,
                    ShiftRiskEvidenceSources.AssignmentRegistry,
                    pairKey,
                    nowUtc,
                    null));
            }
        }
    }

    private static string? GetExclusionReason(
        RequirementSnapshot requirement,
        ShiftRiskEvaluationContext context,
        AssignmentEvidence assignment,
        HashSet<Guid> countedPersonIds)
    {
        if (assignment.Status == ShiftAssignmentStatuses.Draft)
        {
            return ShiftRiskReasonCodes.AssignmentExcludedDraft;
        }

        if (assignment.Status == ShiftAssignmentStatuses.Cancelled)
        {
            return ShiftRiskReasonCodes.AssignmentExcludedCancelled;
        }

        if (assignment.Status != ShiftAssignmentStatuses.Approved)
        {
            return ShiftRiskReasonCodes.AssignmentExcludedDraft;
        }

        if (!WindowsOverlapRequirement(requirement, assignment))
        {
            return ShiftRiskReasonCodes.AssignmentExcludedWindow;
        }

        if (!MatchesRequirementScope(requirement, assignment, context))
        {
            return ShiftRiskReasonCodes.CompetencyMismatch;
        }

        if (countedPersonIds.Contains(assignment.WorkforcePersonId))
        {
            return ShiftRiskReasonCodes.AssignmentExcludedDuplicatePerson;
        }

        return null;
    }

    private static bool WindowsOverlapRequirement(RequirementSnapshot requirement, AssignmentEvidence assignment) =>
        ShiftOperatorEligibilityEvaluator.WindowsOverlap(
            requirement.PlannedStartUtc,
            requirement.PlannedEndUtc,
            assignment.PlannedStartUtc,
            assignment.PlannedEndUtc);

    private static bool MatchesRequirementScope(
        RequirementSnapshot requirement,
        AssignmentEvidence assignment,
        ShiftRiskEvaluationContext context)
    {
        if (requirement.MachineAssetId.HasValue
            && assignment.MachineAssetId != requirement.MachineAssetId.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(requirement.ProductionOrderReference)
            && !string.Equals(
                requirement.ProductionOrderReference.Trim(),
                assignment.ProductionOrderReference?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(requirement.RequiredCompetencyCode)
            && !string.IsNullOrWhiteSpace(assignment.RequiredCompetencyCode)
            && !string.Equals(
                requirement.RequiredCompetencyCode.Trim(),
                assignment.RequiredCompetencyCode.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(context.RequiredCompetencyCode)
            && !string.IsNullOrWhiteSpace(assignment.RequiredCompetencyCode)
            && !string.Equals(
                context.RequiredCompetencyCode.Trim(),
                assignment.RequiredCompetencyCode.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string? EvaluatePersonEligibility(
        PersonEvidence person,
        AssignmentEvidence assignment,
        DateTimeOffset nowUtc,
        List<DriverDraft> drivers)
    {
        if (!person.IsActive)
        {
            return ShiftRiskReasonCodes.AssignmentIneligible;
        }

        if (person.AvailabilityState == ShiftWorkforceAvailabilityStates.Leave
            || person.AvailabilityState == ShiftWorkforceAvailabilityStates.Absent)
        {
            return ShiftRiskReasonCodes.AssignmentIneligible;
        }

        if (person.FreshnessState == ShiftWorkforceFreshnessStates.Stale)
        {
            drivers.Add(new DriverDraft(
                ShiftRiskReasonCodes.SourceStale,
                ShiftRiskDriverCategories.SourceStale,
                ShiftRiskDriverSeverities.Info,
                ShiftWorkforceFreshnessStates.Fresh,
                person.FreshnessState,
                null,
                null,
                assignment.ProductionOrderReference,
                assignment.MachineAssetId,
                assignment.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.ShiftWorkforce,
                person.PersonId.ToString(),
                person.SourceUpdatedAtUtc ?? nowUtc,
                person.FreshnessState));
        }

        if (person.AvailabilityState == ShiftWorkforceAvailabilityStates.Unknown
            || person.FreshnessState == ShiftWorkforceFreshnessStates.Unknown)
        {
            drivers.Add(new DriverDraft(
                ShiftRiskReasonCodes.EvidenceConflict,
                ShiftRiskDriverCategories.EvidenceConflict,
                ShiftRiskDriverSeverities.Info,
                "known-state",
                $"{person.AvailabilityState}|{person.FreshnessState}",
                null,
                null,
                assignment.ProductionOrderReference,
                assignment.MachineAssetId,
                assignment.RequiredCompetencyCode,
                ShiftRiskEvidenceSources.ShiftWorkforce,
                person.PersonId.ToString(),
                person.SourceUpdatedAtUtc ?? nowUtc,
                person.FreshnessState));
        }

        if (assignment.EligibilityOutcome == ShiftAssignmentEligibilityOutcomes.InsufficientData
            && string.Equals(
                assignment.EligibilityPrimaryReasonCode,
                ShiftAssignmentReasonCodes.SourceStale,
                StringComparison.OrdinalIgnoreCase))
        {
            if (person.FreshnessState != ShiftWorkforceFreshnessStates.Stale)
            {
                drivers.Add(new DriverDraft(
                    ShiftRiskReasonCodes.SourceStale,
                    ShiftRiskDriverCategories.SourceStale,
                    ShiftRiskDriverSeverities.Info,
                    ShiftWorkforceFreshnessStates.Fresh,
                    ShiftAssignmentReasonCodes.SourceStale,
                    null,
                    null,
                    assignment.ProductionOrderReference,
                    assignment.MachineAssetId,
                    assignment.RequiredCompetencyCode,
                    ShiftRiskEvidenceSources.AssignmentRegistry,
                    assignment.AssignmentId.ToString(),
                    nowUtc,
                    person.FreshnessState));
            }

            return ShiftRiskReasonCodes.SourceStale;
        }

        if (assignment.EligibilityOutcome is ShiftAssignmentEligibilityOutcomes.Ineligible
            or ShiftAssignmentEligibilityOutcomes.InsufficientData)
        {
            return ShiftRiskReasonCodes.EligibilityInsufficient;
        }

        if (person.FreshnessState == ShiftWorkforceFreshnessStates.Stale)
        {
            return ShiftRiskReasonCodes.SourceStale;
        }

        if (person.AvailabilityState == ShiftWorkforceAvailabilityStates.Unknown
            || person.FreshnessState == ShiftWorkforceFreshnessStates.Unknown)
        {
            return ShiftRiskReasonCodes.EvidenceConflict;
        }

        return null;
    }

    private static DriverDraft BuildAssignmentIneligibleDriver(
        RequirementSnapshot requirement,
        AssignmentEvidence assignment,
        PersonEvidence person,
        string exclusionReason,
        DateTimeOffset nowUtc) =>
        new(
            exclusionReason == ShiftRiskReasonCodes.SourceStale
                ? ShiftRiskReasonCodes.SourceStale
                : ShiftRiskReasonCodes.AssignmentIneligible,
            exclusionReason == ShiftRiskReasonCodes.SourceStale
                ? ShiftRiskDriverCategories.SourceStale
                : ShiftRiskDriverCategories.AssignmentIneligible,
            ShiftRiskDriverSeverities.Fail,
            "eligible-approved-assignment",
            exclusionReason,
            null,
            requirement.ProductionShiftId,
            assignment.ProductionOrderReference,
            assignment.MachineAssetId,
            assignment.RequiredCompetencyCode,
            ShiftRiskEvidenceSources.AssignmentRegistry,
            assignment.AssignmentId.ToString(),
            person.SourceUpdatedAtUtc ?? nowUtc,
            person.FreshnessState);

    private static List<DriverDraft> DedupeDriversByReasonCode(List<DriverDraft> drivers)
    {
        var preferredByReason = new Dictionary<string, DriverDraft>(StringComparer.Ordinal);
        foreach (var driver in drivers)
        {
            if (!preferredByReason.TryGetValue(driver.ReasonCode, out var existing))
            {
                preferredByReason[driver.ReasonCode] = driver;
                continue;
            }

            preferredByReason[driver.ReasonCode] = SelectPreferredDriver(existing, driver);
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

    private static DriverDraft SelectPreferredDriver(DriverDraft existing, DriverDraft candidate)
    {
        if (existing.ReasonCode == ShiftRiskReasonCodes.SourceStale)
        {
            if (existing.Severity == ShiftRiskDriverSeverities.Info)
            {
                return existing;
            }

            if (candidate.Severity == ShiftRiskDriverSeverities.Info)
            {
                return candidate;
            }
        }

        return SeverityRank(candidate.Severity) > SeverityRank(existing.Severity) ? candidate : existing;
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

    private static string ResolveOutcome(List<DriverDraft> drivers, bool isCriticalRequirement)
    {
        if (drivers.Any(d => d.Category == ShiftRiskDriverCategories.CriticalRoleGap))
        {
            return ShiftRiskOutcomes.Critical;
        }

        if (drivers.Any(d => d.Category == ShiftRiskDriverCategories.RequirementMissing))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d =>
                d.Category is ShiftRiskDriverCategories.EvidenceConflict
                    && d.Severity == ShiftRiskDriverSeverities.Info)
            && !drivers.Any(d => d.Severity is ShiftRiskDriverSeverities.Fail or ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d =>
                d.Category is ShiftRiskDriverCategories.SourceStale
                    && d.Severity == ShiftRiskDriverSeverities.Info))
        {
            return ShiftRiskOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.Severity == ShiftRiskDriverSeverities.Fail))
        {
            return isCriticalRequirement ? ShiftRiskOutcomes.Critical : ShiftRiskOutcomes.Attention;
        }

        if (drivers.Any(d => d.Severity == ShiftRiskDriverSeverities.Warn))
        {
            return ShiftRiskOutcomes.Attention;
        }

        return ShiftRiskOutcomes.Clear;
    }

    private static DriverDraft? ResolveLeadingDriver(List<DriverDraft> drivers, string outcome)
    {
        if (outcome == ShiftRiskOutcomes.Clear)
        {
            return drivers.FirstOrDefault(d => d.ReasonCode == ShiftRiskReasonCodes.CoverageSatisfied);
        }

        var priority = new[]
        {
            ShiftRiskDriverCategories.CriticalRoleGap,
            ShiftRiskDriverCategories.AssignmentConflict,
            ShiftRiskDriverCategories.CoverageGap,
            ShiftRiskDriverCategories.AssignmentIneligible,
            ShiftRiskDriverCategories.EvidenceConflict,
            ShiftRiskDriverCategories.SourceStale,
            ShiftRiskDriverCategories.RequirementMissing,
        };

        foreach (var category in priority)
        {
            for (var i = 0; i < drivers.Count; i++)
            {
                if (drivers[i].Category == category)
                {
                    return drivers[i];
                }
            }
        }

        return drivers.Count > 0 ? drivers[0] : null;
    }
}
