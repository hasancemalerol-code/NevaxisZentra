using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NevaxisZentra.Persistence.IotPlc;

internal static partial class IotPlcIntelligenceCalculator
{
    private static readonly Regex NumericSequenceRegex = NumericSequencePattern();

    internal sealed record DriverCandidate(
        string ReasonCode,
        int OutcomeSeverity,
        decimal ThresholdVariance,
        int AffectedObservationCount,
        string Explanation,
        string StructuredEvidenceJson);

    internal sealed record SignalEvaluationInput(
        IotPlcQualityProfile? Profile,
        string EvidenceMode,
        DateTimeOffset EvaluationTimeUtc,
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc,
        IReadOnlyList<IotPlcSignalObservation> Observations);

    internal sealed record SignalEvaluationResult(
        string Outcome,
        IReadOnlyList<DriverCandidate> Drivers,
        int ActualObservationCount,
        int? ExpectedObservationCount,
        decimal? CompletenessRatio,
        int GoodCount,
        int UncertainCount,
        int BadCount,
        int StaleCount,
        int SourceConflictCount,
        decimal? GoodRatio,
        decimal? UncertainRatio,
        decimal? BadRatio,
        decimal? StaleRatio,
        decimal? SourceConflictRatio,
        DateTimeOffset? LatestValidObservedAtUtc,
        int? FreshnessSeconds,
        int? MaxClockSkewSeconds);

    internal sealed record ConnectivityEvaluationInput(
        IotPlcQualityProfile? Profile,
        string EvidenceMode,
        DateTimeOffset EvaluationTimeUtc,
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc,
        IReadOnlyList<IotPlcConnectionHealthObservation> HealthObservations,
        IReadOnlyList<IotPlcSignalObservation> EndpointObservations);

    internal sealed record ConnectivityEvaluationResult(
        string Outcome,
        IReadOnlyList<DriverCandidate> Drivers,
        string? LatestHealthState,
        decimal? HealthObservationAvailabilityRatio,
        int? FreshnessSeconds);

    internal static SignalEvaluationResult EvaluateSignalQuality(SignalEvaluationInput input)
    {
        var drivers = new List<DriverCandidate>();

        if (input.WindowEndUtc <= input.WindowStartUtc)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.InsufficientObservations, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, 0, "Invalid evaluation window."));
            return BuildSignalResult(IotPlcIntelligenceOutcomes.InsufficientData, drivers, [], input.EvaluationTimeUtc, input.Profile);
        }

        if (input.Profile is null || input.Profile.Status != IotPlcQualityProfileStatuses.Approved)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.MissingQualityProfile, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, 0, "No compatible approved quality profile."));
            return BuildSignalResult(IotPlcIntelligenceOutcomes.InsufficientData, drivers, [], input.EvaluationTimeUtc, input.Profile);
        }

        var inWindow = input.Observations
            .Where(o => o.ObservedAtUtc >= input.WindowStartUtc && o.ObservedAtUtc <= input.WindowEndUtc)
            .ToList();

        if (HasMixedEvidenceModes(inWindow, input.EvidenceMode))
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceConflict, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), 1, inWindow.Count, "Simulated and measured evidence cannot be mixed."));
            return BuildSignalResult(IotPlcIntelligenceOutcomes.SourceConflict, drivers, inWindow, input.EvaluationTimeUtc, input.Profile);
        }

        var accepted = inWindow
            .Where(o => MatchesEvidenceMode(o.EvidenceState, input.EvidenceMode))
            .GroupBy(o => o.ObservedAtUtc)
            .Select(g => g.OrderBy(o => o.ReceivedAtUtc).First())
            .OrderBy(o => o.ObservedAtUtc)
            .ToList();

        var duplicateCount = inWindow.Count - accepted.Count;
        if (duplicateCount > 0)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.DuplicateObservationEvidence, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), 0, duplicateCount, "Duplicate timestamp observations excluded from completeness."));
        }

        if (accepted.Count < input.Profile.MinimumExpectedObservationCount)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.InsufficientObservations, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, accepted.Count, "Insufficient distinct observations for profile minimum."));
            return BuildSignalResult(IotPlcIntelligenceOutcomes.InsufficientData, drivers, accepted, input.EvaluationTimeUtc, input.Profile);
        }

        var good = accepted.Count(o => o.QualityState == IotPlcQualityStates.Good);
        var uncertain = accepted.Count(o => o.QualityState == IotPlcQualityStates.Uncertain);
        var bad = accepted.Count(o => o.QualityState == IotPlcQualityStates.Bad);
        var stale = accepted.Count(o => o.QualityState == IotPlcQualityStates.Stale);
        var conflict = accepted.Count(o => o.QualityState == IotPlcQualityStates.SourceConflict || o.EvidenceState == IotPlcEvidenceStates.SourceConflict);
        var denominator = accepted.Count;

        decimal? goodRatio = Ratio(good, denominator);
        decimal? uncertainRatio = Ratio(uncertain, denominator);
        decimal? badRatio = Ratio(bad, denominator);
        decimal? staleRatio = Ratio(stale, denominator);
        decimal? conflictRatio = Ratio(conflict, denominator);

        if (conflict > 0)
        {
            var variance = conflictRatio!.Value - input.Profile.MaximumSourceConflictRatio;
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceConflict, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), Math.Max(0, variance), conflict, "Conflicting source evidence detected."));
        }
        else if (conflictRatio > input.Profile.MaximumSourceConflictRatio)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceConflictRatioExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), conflictRatio.Value - input.Profile.MaximumSourceConflictRatio, conflict, "Source conflict ratio exceeded profile maximum."));
        }

        if (badRatio > input.Profile.MaximumBadQualityRatio)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.BadQualityRatioExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.Degraded), badRatio.Value - input.Profile.MaximumBadQualityRatio, bad, "Bad quality ratio exceeded profile maximum."));
        }

        if (uncertainRatio > input.Profile.MaximumUncertainQualityRatio)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.UncertainQualityRatioExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), uncertainRatio.Value - input.Profile.MaximumUncertainQualityRatio, uncertain, "Uncertain quality ratio exceeded profile maximum."));
        }

        if (goodRatio < input.Profile.MinimumGoodQualityRatio)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ObservationGapDetected, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), input.Profile.MinimumGoodQualityRatio - goodRatio.Value, good, "Good quality ratio below profile minimum."));
        }

        var latestValid = accepted.MaxBy(o => o.ObservedAtUtc);
        var freshnessSeconds = latestValid is null
            ? (int?)null
            : (int)Math.Max(0, (input.EvaluationTimeUtc - latestValid.ObservedAtUtc).TotalSeconds);

        if (latestValid is null)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.NoRecentObservation, OutcomeSeverity(IotPlcIntelligenceOutcomes.Unavailable), 0, 0, "No valid observations in evaluation window."));
        }
        else if (freshnessSeconds > input.Profile.StaleThresholdSeconds)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ObservationStale, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), freshnessSeconds.Value - input.Profile.StaleThresholdSeconds, 1, "Latest observation exceeds stale threshold."));
        }
        else if (freshnessSeconds > input.Profile.FreshnessToleranceSeconds)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ObservationFreshnessExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), freshnessSeconds.Value - input.Profile.FreshnessToleranceSeconds, 1, "Latest observation exceeds freshness tolerance."));
        }

        int? maxSkew = null;
        foreach (var obs in accepted)
        {
            var skew = (int)Math.Abs((obs.ReceivedAtUtc - obs.ObservedAtUtc).TotalSeconds);
            maxSkew = maxSkew is null ? skew : Math.Max(maxSkew.Value, skew);
            if (skew > input.Profile.MaximumClockSkewToleranceSeconds)
            {
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ClockSkewExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), skew - input.Profile.MaximumClockSkewToleranceSeconds, 1, "Clock skew between observed and received timestamps exceeded tolerance."));
            }

            if (obs.ObservedAtUtc > input.EvaluationTimeUtc.AddSeconds(input.Profile.MaximumClockSkewToleranceSeconds))
            {
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ClockSkewExceeded, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), 0, 1, "Future-dated observation beyond clock-skew tolerance."));
            }
        }

        int? expectedCount = null;
        decimal? completenessRatio = null;
        if (input.Profile.ExpectedObservationIntervalSeconds > 0)
        {
            var windowSeconds = (input.WindowEndUtc - input.WindowStartUtc).TotalSeconds;
            expectedCount = Math.Max(1, (int)Math.Floor(windowSeconds / input.Profile.ExpectedObservationIntervalSeconds));
            completenessRatio = expectedCount > 0 ? Math.Round((decimal)accepted.Count / expectedCount.Value, 4) : null;
            if (accepted.Count < expectedCount)
            {
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ExpectedCadenceMissed, OutcomeSeverity(IotPlcIntelligenceOutcomes.Degraded), expectedCount.Value - accepted.Count, accepted.Count, "Expected cadence not satisfied for evaluation window."));
            }
        }

        if (input.Profile.SequenceGapEvaluationEnabled)
        {
            EvaluateSequenceGaps(accepted, drivers);
        }

        var outcome = DetermineOutcome(drivers);
        if (outcome == IotPlcIntelligenceOutcomes.Healthy)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.WithinQualityProfile, OutcomeSeverity(IotPlcIntelligenceOutcomes.Healthy), 0, accepted.Count, "All configured thresholds satisfied."));
        }

        return new SignalEvaluationResult(
            outcome,
            drivers,
            accepted.Count,
            expectedCount,
            completenessRatio,
            good,
            uncertain,
            bad,
            stale,
            conflict,
            goodRatio,
            uncertainRatio,
            badRatio,
            staleRatio,
            conflictRatio,
            latestValid?.ObservedAtUtc,
            freshnessSeconds,
            maxSkew);
    }

    internal static ConnectivityEvaluationResult EvaluateConnectivity(ConnectivityEvaluationInput input)
    {
        var drivers = new List<DriverCandidate>();

        if (input.WindowEndUtc <= input.WindowStartUtc)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.InsufficientObservations, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, 0, "Invalid evaluation window."));
            return new ConnectivityEvaluationResult(IotPlcIntelligenceOutcomes.InsufficientData, drivers, null, null, null);
        }

        if (input.Profile is null || input.Profile.Status != IotPlcQualityProfileStatuses.Approved)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.MissingQualityProfile, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, 0, "No compatible approved quality profile."));
            return new ConnectivityEvaluationResult(IotPlcIntelligenceOutcomes.InsufficientData, drivers, null, null, null);
        }

        var healthInWindow = input.HealthObservations
            .Where(h => h.ObservedAtUtc >= input.WindowStartUtc && h.ObservedAtUtc <= input.WindowEndUtc)
            .Where(h => MatchesEvidenceMode(h.EvidenceState, input.EvidenceMode))
            .OrderByDescending(h => h.ObservedAtUtc)
            .ToList();

        if (HasMixedHealthEvidence(input.HealthObservations.Where(h => h.ObservedAtUtc >= input.WindowStartUtc && h.ObservedAtUtc <= input.WindowEndUtc), input.EvidenceMode))
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceConflict, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), 1, healthInWindow.Count, "Mixed evidence modes in health observations."));
            return new ConnectivityEvaluationResult(IotPlcIntelligenceOutcomes.SourceConflict, drivers, null, null, null);
        }

        decimal? availabilityRatio = null;
        if (input.Profile.ExpectedObservationIntervalSeconds > 0 && healthInWindow.Count > 0)
        {
            var expectedSlots = Math.Max(1, (int)Math.Floor((input.WindowEndUtc - input.WindowStartUtc).TotalSeconds / input.Profile.ExpectedObservationIntervalSeconds));
            availabilityRatio = (decimal)healthInWindow.Count / expectedSlots;
        }

        var latest = healthInWindow.FirstOrDefault();
        int? freshnessSeconds = latest is null
            ? null
            : (int)Math.Max(0, (input.EvaluationTimeUtc - latest.ObservedAtUtc).TotalSeconds);

        if (latest is null)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.NoRecentObservation, OutcomeSeverity(IotPlcIntelligenceOutcomes.Unavailable), 0, 0, "No compatible health observations in evaluation window."));
            return new ConnectivityEvaluationResult(IotPlcIntelligenceOutcomes.Unavailable, drivers, null, availabilityRatio, freshnessSeconds);
        }

        if (freshnessSeconds > input.Profile.StaleThresholdSeconds)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.ObservationStale, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), freshnessSeconds.Value - input.Profile.StaleThresholdSeconds, 1, "Latest health evidence is stale and cannot prove current online state."));
        }

        switch (latest.HealthState)
        {
            case IotPlcConnectionStates.Offline:
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.EndpointOffline, OutcomeSeverity(IotPlcIntelligenceOutcomes.Unavailable), 1, 1, "Endpoint health evidence reports offline."));
                break;
            case IotPlcConnectionStates.Degraded:
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.EndpointHealthDegraded, OutcomeSeverity(IotPlcIntelligenceOutcomes.Degraded), 1, 1, "Endpoint health evidence reports degraded connectivity."));
                break;
            case IotPlcConnectionStates.Unknown:
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.EndpointHealthUnknown, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), 0, 1, "Endpoint health state is unknown."));
                break;
        }

        var outcome = DetermineOutcome(drivers);
        if (outcome == IotPlcIntelligenceOutcomes.Healthy && latest.HealthState == IotPlcConnectionStates.Online)
        {
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.WithinQualityProfile, OutcomeSeverity(IotPlcIntelligenceOutcomes.Healthy), 0, healthInWindow.Count, "Connectivity evidence within approved profile."));
        }
        else if (outcome == IotPlcIntelligenceOutcomes.Healthy)
        {
            outcome = IotPlcIntelligenceOutcomes.Attention;
            drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.EndpointHealthUnknown, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), 0, 1, "Cannot conclude healthy without online health evidence."));
        }

        return new ConnectivityEvaluationResult(outcome, drivers, latest.HealthState, availabilityRatio, freshnessSeconds);
    }

    internal static DriverCandidate SelectLeadingDriver(IReadOnlyList<DriverCandidate> drivers)
    {
        if (drivers.Count == 0)
        {
            return Driver(IotPlcIntelligenceDriverReasonCodes.InsufficientObservations, OutcomeSeverity(IotPlcIntelligenceOutcomes.InsufficientData), 0, 0, "No drivers available.");
        }

        return drivers
            .OrderByDescending(d => d.OutcomeSeverity)
            .ThenByDescending(d => d.ThresholdVariance)
            .ThenByDescending(d => d.AffectedObservationCount)
            .ThenBy(d => d.ReasonCode, StringComparer.Ordinal)
            .First();
    }

    internal static int OutcomeSeverity(string outcome) => outcome switch
    {
        IotPlcIntelligenceOutcomes.SourceConflict => 6,
        IotPlcIntelligenceOutcomes.Unavailable => 5,
        IotPlcIntelligenceOutcomes.Degraded => 4,
        IotPlcIntelligenceOutcomes.InsufficientData => 3,
        IotPlcIntelligenceOutcomes.Attention => 2,
        IotPlcIntelligenceOutcomes.Healthy => 1,
        _ => 0,
    };

    private static string DetermineOutcome(IReadOnlyList<DriverCandidate> drivers)
    {
        if (drivers.Any(d => d.ReasonCode is IotPlcIntelligenceDriverReasonCodes.SourceConflict or IotPlcIntelligenceDriverReasonCodes.SourceConflictRatioExceeded))
        {
            return IotPlcIntelligenceOutcomes.SourceConflict;
        }

        if (drivers.Any(d => d.ReasonCode == IotPlcIntelligenceDriverReasonCodes.EndpointOffline || d.ReasonCode == IotPlcIntelligenceDriverReasonCodes.NoRecentObservation))
        {
            return IotPlcIntelligenceOutcomes.Unavailable;
        }

        if (drivers.Any(d => d.OutcomeSeverity >= OutcomeSeverity(IotPlcIntelligenceOutcomes.Degraded)))
        {
            return IotPlcIntelligenceOutcomes.Degraded;
        }

        if (drivers.Any(d => d.ReasonCode == IotPlcIntelligenceDriverReasonCodes.MissingQualityProfile || d.ReasonCode == IotPlcIntelligenceDriverReasonCodes.InsufficientObservations))
        {
            return IotPlcIntelligenceOutcomes.InsufficientData;
        }

        if (drivers.Any(d => d.OutcomeSeverity >= OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention)))
        {
            return IotPlcIntelligenceOutcomes.Attention;
        }

        return IotPlcIntelligenceOutcomes.Healthy;
    }

    private static void EvaluateSequenceGaps(List<IotPlcSignalObservation> accepted, List<DriverCandidate> drivers)
    {
        var numericSequences = accepted
            .Select(o => o.SourceSequenceReference)
            .Where(IsNumericSequence)
            .Select(s => long.Parse(s!, CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        if (numericSequences.Count < 2)
        {
            return;
        }

        for (var i = 1; i < numericSequences.Count; i++)
        {
            if (numericSequences[i] <= numericSequences[i - 1])
            {
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceConflict, OutcomeSeverity(IotPlcIntelligenceOutcomes.SourceConflict), 0, 1, "Non-monotonic numeric sequence detected."));
                return;
            }

            if (numericSequences[i] - numericSequences[i - 1] > 1)
            {
                drivers.Add(Driver(IotPlcIntelligenceDriverReasonCodes.SourceSequenceGap, OutcomeSeverity(IotPlcIntelligenceOutcomes.Attention), numericSequences[i] - numericSequences[i - 1] - 1, 1, $"Missing numeric sequence range between {numericSequences[i - 1]} and {numericSequences[i]}."));
            }
        }
    }

    private static SignalEvaluationResult BuildSignalResult(
        string outcome,
        List<DriverCandidate> drivers,
        List<IotPlcSignalObservation> accepted,
        DateTimeOffset evaluationTimeUtc,
        IotPlcQualityProfile? profile)
    {
        var denominator = accepted.Count;
        return new SignalEvaluationResult(
            outcome,
            drivers,
            accepted.Count,
            null,
            null,
            accepted.Count(o => o.QualityState == IotPlcQualityStates.Good),
            accepted.Count(o => o.QualityState == IotPlcQualityStates.Uncertain),
            accepted.Count(o => o.QualityState == IotPlcQualityStates.Bad),
            accepted.Count(o => o.QualityState == IotPlcQualityStates.Stale),
            accepted.Count(o => o.QualityState == IotPlcQualityStates.SourceConflict),
            Ratio(accepted.Count(o => o.QualityState == IotPlcQualityStates.Good), denominator),
            Ratio(accepted.Count(o => o.QualityState == IotPlcQualityStates.Uncertain), denominator),
            Ratio(accepted.Count(o => o.QualityState == IotPlcQualityStates.Bad), denominator),
            Ratio(accepted.Count(o => o.QualityState == IotPlcQualityStates.Stale), denominator),
            Ratio(accepted.Count(o => o.QualityState == IotPlcQualityStates.SourceConflict), denominator),
            accepted.MaxBy(o => o.ObservedAtUtc)?.ObservedAtUtc,
            accepted.Count == 0 ? null : (int?)Math.Max(0, (evaluationTimeUtc - accepted.Max(o => o.ObservedAtUtc)).TotalSeconds),
            null);
    }

    private static bool HasMixedEvidenceModes(IReadOnlyList<IotPlcSignalObservation> observations, string evidenceMode)
    {
        var modes = observations.Select(o => NormalizeEvidenceMode(o.EvidenceState)).Distinct().ToList();
        return modes.Count > 1;
    }

    private static bool HasMixedHealthEvidence(IEnumerable<IotPlcConnectionHealthObservation> observations, string evidenceMode)
    {
        var modes = observations.Select(o => NormalizeEvidenceMode(o.EvidenceState)).Distinct().ToList();
        return modes.Count > 1;
    }

    private static bool MatchesEvidenceMode(string evidenceState, string evidenceMode) =>
        NormalizeEvidenceMode(evidenceState) == evidenceMode;

    private static string NormalizeEvidenceMode(string evidenceState) =>
        evidenceState.Trim().ToLowerInvariant() switch
        {
            IotPlcEvidenceStates.Measured => IotPlcIntelligenceEvidenceModes.Measured,
            IotPlcEvidenceStates.Calculated => IotPlcIntelligenceEvidenceModes.Measured,
            IotPlcEvidenceStates.Estimated => IotPlcIntelligenceEvidenceModes.Measured,
            _ => IotPlcIntelligenceEvidenceModes.Simulated,
        };

    private static decimal? Ratio(int count, int denominator) =>
        denominator == 0 ? null : (decimal)count / denominator;

    private static bool IsNumericSequence(string? value) =>
        !string.IsNullOrWhiteSpace(value) && NumericSequenceRegex.IsMatch(value);

    private static DriverCandidate Driver(string code, int severity, decimal variance, int affected, string explanation) =>
        new(code, severity, variance, affected, explanation, JsonSerializer.Serialize(new { reasonCode = code, affected, variance }));

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex NumericSequencePattern();
}
