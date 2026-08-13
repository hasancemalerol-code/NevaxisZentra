using CrevaFactOS.Persistence.ShiftWorkforce;

namespace CrevaFactOS.Persistence.ShiftAssignment;

internal static class ShiftOperatorEligibilityEvaluator
{
    internal sealed record PersonEvaluationInput(
        ShiftWorkforcePerson Person,
        ShiftWorkforceCompetencyAssignment? MatchingCompetency,
        string CompetencyCodeLabel,
        string ShiftCode,
        DateTimeOffset NowUtc,
        bool HasApprovedOverlap);

    internal sealed record ReasonDraft(
        string ReasonCode,
        string Severity,
        string? RequiredValue,
        string? ObservedValue,
        string EvidenceSource,
        DateTimeOffset? EvidenceTimestampUtc,
        string? FreshnessState);

    internal sealed record PersonEvaluationOutput(
        string Outcome,
        string? PrimaryReasonCode,
        IReadOnlyList<ReasonDraft> Reasons,
        bool HasOverlapWarning);

    internal static PersonEvaluationOutput EvaluatePerson(
        PersonEvaluationInput input,
        bool missingRequiredCompetencyCode)
    {
        if (missingRequiredCompetencyCode)
        {
            return BuildInsufficient(
                ShiftAssignmentReasonCodes.RequiredDataMissing,
                [
                    new ReasonDraft(
                        ShiftAssignmentReasonCodes.RequiredDataMissing,
                        ShiftAssignmentReasonSeverities.Fail,
                        "requiredCompetencyCode",
                        null,
                        ShiftAssignmentEvidenceSources.EligibilityEngine,
                        null,
                        null),
                ]);
        }

        var reasons = new List<ReasonDraft>();
        var person = input.Person;

        if (!person.IsActive)
        {
            reasons.Add(FailReason(
                ShiftAssignmentReasonCodes.EmploymentInactive,
                "active",
                "false",
                person.SourceSystem,
                person.SourceUpdatedAtUtc,
                person.FreshnessState));
        }

        switch (person.AvailabilityState)
        {
            case ShiftWorkforceAvailabilityStates.Leave:
                reasons.Add(FailReason(
                    ShiftAssignmentReasonCodes.AvailabilityLeave,
                    ShiftWorkforceAvailabilityStates.Available,
                    ShiftWorkforceAvailabilityStates.Leave,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc,
                    person.FreshnessState));
                break;
            case ShiftWorkforceAvailabilityStates.Absent:
                reasons.Add(FailReason(
                    ShiftAssignmentReasonCodes.AvailabilityAbsent,
                    ShiftWorkforceAvailabilityStates.Available,
                    ShiftWorkforceAvailabilityStates.Absent,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc,
                    person.FreshnessState));
                break;
            case ShiftWorkforceAvailabilityStates.Unknown:
                reasons.Add(UnknownReason(
                    ShiftAssignmentReasonCodes.AvailabilityUnknown,
                    ShiftWorkforceAvailabilityStates.Available,
                    ShiftWorkforceAvailabilityStates.Unknown,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc,
                    person.FreshnessState));
                break;
            case ShiftWorkforceAvailabilityStates.Available:
                reasons.Add(PassReason(
                    "availability-available",
                    ShiftWorkforceAvailabilityStates.Available,
                    person.AvailabilityState,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc));
                break;
        }

        switch (person.FreshnessState)
        {
            case ShiftWorkforceFreshnessStates.Stale:
                reasons.Add(UnknownReason(
                    ShiftAssignmentReasonCodes.SourceStale,
                    ShiftWorkforceFreshnessStates.Fresh,
                    ShiftWorkforceFreshnessStates.Stale,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc,
                    person.FreshnessState));
                break;
            case ShiftWorkforceFreshnessStates.Unknown:
                reasons.Add(UnknownReason(
                    ShiftAssignmentReasonCodes.SourceUnknown,
                    ShiftWorkforceFreshnessStates.Fresh,
                    ShiftWorkforceFreshnessStates.Unknown,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc,
                    person.FreshnessState));
                break;
            case ShiftWorkforceFreshnessStates.Fresh:
                reasons.Add(PassReason(
                    "source-fresh",
                    ShiftWorkforceFreshnessStates.Fresh,
                    person.FreshnessState,
                    person.SourceSystem,
                    person.SourceUpdatedAtUtc));
                break;
        }

        if (input.MatchingCompetency is null)
        {
            reasons.Add(FailReason(
                ShiftAssignmentReasonCodes.CompetencyMissing,
                input.CompetencyCodeLabel,
                null,
                ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                person.SourceUpdatedAtUtc,
                person.FreshnessState));
        }
        else
        {
            var competency = input.MatchingCompetency;
            switch (competency.VerificationStatus)
            {
                case ShiftWorkforceVerificationStatuses.PendingVerification:
                    reasons.Add(WarnReason(
                        ShiftAssignmentReasonCodes.CompetencyPendingVerification,
                        ShiftWorkforceVerificationStatuses.Verified,
                        competency.VerificationStatus,
                        ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                        competency.VerifiedAtUtc ?? competency.UpdatedAtUtc,
                        person.FreshnessState));
                    break;
                case ShiftWorkforceVerificationStatuses.Expired:
                    reasons.Add(FailReason(
                        ShiftAssignmentReasonCodes.CompetencyExpired,
                        ShiftWorkforceVerificationStatuses.Verified,
                        competency.VerificationStatus,
                        ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                        competency.ValidUntilUtc,
                        person.FreshnessState));
                    break;
                case ShiftWorkforceVerificationStatuses.Rejected:
                    reasons.Add(FailReason(
                        ShiftAssignmentReasonCodes.CompetencyMissing,
                        ShiftWorkforceVerificationStatuses.Verified,
                        competency.VerificationStatus,
                        ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                        competency.VerifiedAtUtc,
                        person.FreshnessState));
                    break;
                case ShiftWorkforceVerificationStatuses.Verified:
                    reasons.Add(PassReason(
                        "competency-verified",
                        ShiftWorkforceVerificationStatuses.Verified,
                        competency.VerificationStatus,
                        ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                        competency.VerifiedAtUtc));
                    break;
            }

            if (competency.ValidUntilUtc.HasValue
                && competency.ValidUntilUtc.Value <= input.NowUtc.AddDays(30)
                && competency.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified)
            {
                reasons.Add(WarnReason(
                    ShiftAssignmentReasonCodes.CompetencyExpiring,
                    competency.ValidUntilUtc.Value.ToString("O"),
                    input.NowUtc.AddDays(30).ToString("O"),
                    ShiftAssignmentEvidenceSources.CompetencyEnrichment,
                    competency.ValidUntilUtc,
                    person.FreshnessState));
            }
        }

        if (!string.IsNullOrWhiteSpace(person.SourceShiftReference)
            && !string.Equals(
                person.SourceShiftReference.Trim(),
                input.ShiftCode.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(WarnReason(
                ShiftAssignmentReasonCodes.ShiftReferenceMismatch,
                input.ShiftCode,
                person.SourceShiftReference.Trim(),
                person.SourceSystem,
                person.SourceUpdatedAtUtc,
                person.FreshnessState));
        }

        if (input.HasApprovedOverlap)
        {
            reasons.Add(FailReason(
                ShiftAssignmentReasonCodes.AssignmentOverlap,
                "no-overlap",
                "overlap-detected",
                ShiftAssignmentEvidenceSources.AssignmentRegistry,
                input.NowUtc,
                null));
        }

        if (reasons.Count == 0 || reasons.All(r => r.Severity == ShiftAssignmentReasonSeverities.Pass))
        {
            reasons.Add(new ReasonDraft(
                ShiftAssignmentReasonCodes.AllCriteriaMet,
                ShiftAssignmentReasonSeverities.Pass,
                null,
                null,
                ShiftAssignmentEvidenceSources.EligibilityEngine,
                input.NowUtc,
                null));
        }

        var outcome = ResolveOutcome(reasons);
        var primaryReason = ResolvePrimaryReasonCode(reasons, outcome);
        return new PersonEvaluationOutput(
            outcome,
            primaryReason,
            reasons,
            input.HasApprovedOverlap);
    }

    internal static bool WindowsOverlap(
        DateTimeOffset startA,
        DateTimeOffset endA,
        DateTimeOffset startB,
        DateTimeOffset endB) =>
        startA < endB && startB < endA;

    private static string ResolveOutcome(IReadOnlyList<ReasonDraft> reasons)
    {
        if (reasons.Any(r => r.Severity == ShiftAssignmentReasonSeverities.Fail))
        {
            return ShiftAssignmentEligibilityOutcomes.Ineligible;
        }

        if (reasons.Any(r =>
                r.Severity == ShiftAssignmentReasonSeverities.Info
                && r.ReasonCode is ShiftAssignmentReasonCodes.AvailabilityUnknown
                    or ShiftAssignmentReasonCodes.SourceStale
                    or ShiftAssignmentReasonCodes.SourceUnknown
                    or ShiftAssignmentReasonCodes.RequiredDataMissing))
        {
            return ShiftAssignmentEligibilityOutcomes.InsufficientData;
        }

        if (reasons.Any(r => r.Severity == ShiftAssignmentReasonSeverities.Warn))
        {
            return ShiftAssignmentEligibilityOutcomes.Conditional;
        }

        if (reasons.All(r => r.Severity == ShiftAssignmentReasonSeverities.Pass))
        {
            return ShiftAssignmentEligibilityOutcomes.Eligible;
        }

        return ShiftAssignmentEligibilityOutcomes.InsufficientData;
    }

    private static string? ResolvePrimaryReasonCode(IReadOnlyList<ReasonDraft> reasons, string outcome)
    {
        if (outcome == ShiftAssignmentEligibilityOutcomes.Eligible)
        {
            return ShiftAssignmentReasonCodes.AllCriteriaMet;
        }

        var fail = reasons.FirstOrDefault(r => r.Severity == ShiftAssignmentReasonSeverities.Fail);
        if (fail is not null)
        {
            return fail.ReasonCode;
        }

        var unknown = reasons.FirstOrDefault(r =>
            r.Severity == ShiftAssignmentReasonSeverities.Info
            && r.ReasonCode is ShiftAssignmentReasonCodes.AvailabilityUnknown
                or ShiftAssignmentReasonCodes.SourceStale
                or ShiftAssignmentReasonCodes.SourceUnknown
                or ShiftAssignmentReasonCodes.RequiredDataMissing);
        if (unknown is not null)
        {
            return unknown.ReasonCode;
        }

        var warn = reasons.FirstOrDefault(r => r.Severity == ShiftAssignmentReasonSeverities.Warn);
        return warn?.ReasonCode;
    }

    private static PersonEvaluationOutput BuildInsufficient(string primaryCode, IReadOnlyList<ReasonDraft> reasons) =>
        new(
            ShiftAssignmentEligibilityOutcomes.InsufficientData,
            primaryCode,
            reasons,
            false);

    private static ReasonDraft PassReason(
        string code,
        string? required,
        string? observed,
        string source,
        DateTimeOffset? timestamp) =>
        new(code, ShiftAssignmentReasonSeverities.Pass, required, observed, source, timestamp, null);

    private static ReasonDraft WarnReason(
        string code,
        string? required,
        string? observed,
        string source,
        DateTimeOffset? timestamp,
        string? freshness) =>
        new(code, ShiftAssignmentReasonSeverities.Warn, required, observed, source, timestamp, freshness);

    private static ReasonDraft FailReason(
        string code,
        string? required,
        string? observed,
        string source,
        DateTimeOffset? timestamp,
        string? freshness) =>
        new(code, ShiftAssignmentReasonSeverities.Fail, required, observed, source, timestamp, freshness);

    private static ReasonDraft UnknownReason(
        string code,
        string? required,
        string? observed,
        string source,
        DateTimeOffset? timestamp,
        string? freshness) =>
        new(code, ShiftAssignmentReasonSeverities.Info, required, observed, source, timestamp, freshness);
}
