using System.Globalization;
using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.Persistence.FactoryCopilot;

/// <summary>Customer-facing AI states of a Copilot answer. Each one is a different thing for the user to do.</summary>
public static class FactoryCopilotAiStates
{
    /// <summary>The OpenAI key is missing, disabled or invalid in the server configuration (administrator action).</summary>
    public const string NotConfigured = "AI_NOT_CONFIGURED";

    /// <summary>The provider rejected the credential (administrator action).</summary>
    public const string AuthenticationFailed = "AI_AUTHENTICATION_FAILED";

    public const string RateLimited = "AI_RATE_LIMITED";
    public const string Timeout = "AI_TIMEOUT";
    public const string ProviderUnavailable = "AI_PROVIDER_UNAVAILABLE";
    public const string InvalidOutput = "AI_INVALID_OUTPUT";

    /// <summary>The model answered with claims that are not in the retrieved facts; the answer was dropped.</summary>
    public const string UngroundedResponse = "AI_UNGROUNDED_RESPONSE";

    public const string BudgetLimited = "AI_BUDGET_LIMITED";
    public const string PolicyRejected = "AI_POLICY_REJECTED";

    /// <summary>No relevant factory data was available; nothing was sent to the model.</summary>
    public const string InsufficientData = "INSUFFICIENT_DATA";

    public const string InjectionBlocked = "prompt-injection-detected";
}

/// <summary>
/// Builds the bounded, tool-derived facts the model may answer from, and verifies the model's answer against them.
/// The facts never contain tenant or user identifiers, credentials or paths.
/// </summary>
internal static partial class FactoryCopilotGrounding
{
    public const int MaxQueryCharacters = 1000;
    private const int MaxFacts = 40;
    private const int MaxFactCharacters = 480;

    public static IReadOnlyList<ModelGroundedFact> BuildFacts(
        FactoryCopilotToolBundle bundle,
        FactoryCopilotResolvedContext resolved,
        string contextQuality)
    {
        var texts = new List<string> { $"Scope: single line {resolved.LineCode} ({resolved.LineName}); no other lines or facilities are included" };
        if (contextQuality != FactoryCopilotContextQualityStates.Sufficient)
        {
            texts.Add("Live (recent) operational context is not available; figures below cover only the recorded period");
        }

        if (!string.IsNullOrWhiteSpace(resolved.ResourceCode))
        {
            texts.Add($"Selected resource: {resolved.ResourceCode}");
        }

        if (!string.IsNullOrWhiteSpace(resolved.OrderReference))
        {
            texts.Add($"Selected order: {resolved.OrderReference}");
        }

        if (bundle.ThroughputOverview is { } overview)
        {
            var measurement = overview.Measurement;
            texts.Add($"Good units per hour (current period): {Number(measurement.GoodUnitsPerHour)}");
            texts.Add($"Target good units per hour: {Number(measurement.TargetGoodUnitsPerHour)}");
            texts.Add($"Production gap per hour: {Number(overview.ProductionGap.GapPerHour)}");
            texts.Add($"Data quality: {overview.DataQuality}");
            foreach (var constraint in overview.Constraints.Take(3))
            {
                texts.Add($"Constraint ({constraint.Role}): station {constraint.StationCode}, type {constraint.ConstraintType}");
            }

            foreach (var node in overview.LossTree.Nodes.OrderByDescending(n => n.LostGoodUnitsPerHour).Take(6))
            {
                texts.Add($"Loss driver: {node.Label}, lost good units per hour {Number(node.LostGoodUnitsPerHour)}");
            }
        }

        if (bundle.MachinePerformance is { } machines)
        {
            texts.Add($"Machine data period (all available events): {machines.PeriodFromUtc:yyyy-MM-dd} to {machines.PeriodToUtc:yyyy-MM-dd}, {machines.EventCount} events");
            if (machines.CalendarClassified)
            {
                AddCalendarMachineFacts(texts, machines);
            }
            else
            {
                texts.Add("Machine events are not classified by the factory calendar; the figures below are raw totals, not scheduled-time KPIs");
                foreach (var machine in machines.Machines)
                {
                    var reasons = machine.TopUnplannedReasons.Count == 0
                        ? "none"
                        : string.Join("; ", machine.TopUnplannedReasons.Select(r => $"{r.Reason} {Number(r.Minutes)} min x{r.Count}"));
                    texts.Add($"Machine {machine.ResourceCode}: produced {Number(machine.UnclassifiedUnits)} units, {machine.UnclassifiedStopCount} stops totalling {Number(machine.UnclassifiedStopMinutes)} min; top stop reasons: {reasons}");
                }
            }
        }

        if (bundle.PriorShiftOverview is { } prior)
        {
            texts.Add($"Prior shift good units per hour: {Number(prior.Measurement.GoodUnitsPerHour)}");
        }

        texts.AddRange(bundle.ShiftSignals.Take(4).Select(s => $"Shift signal: {s}"));
        texts.AddRange(bundle.ExecutiveSignals.Take(4).Select(s => $"Operational signal: {s}"));
        texts.AddRange(bundle.RecoveryOptions.Take(3).Select(s => $"Recovery option: {s}"));
        if (bundle.ScenarioWorkspace is { } workspace)
        {
            texts.Add($"Scenario run phase: {workspace.CurrentPhase}");
        }

        if (bundle.WorkDetail is { } work)
        {
            texts.Add($"Work case: {work.Summary.Title}, state {work.Summary.DisplayState}");
        }

        return texts
            .Select(Clean)
            .Where(t => t.Length > 0)
            .Take(MaxFacts)
            .Select((text, index) => new ModelGroundedFact($"F{index + 1}", text))
            .ToList();
    }

    private static void AddCalendarMachineFacts(List<string> texts, FactoryCopilotMachinePerformance machines)
    {
        texts.Add(FactoryWorkingCalendar.Description);
        texts.Add($"Factory working days with scheduled production in the period: {machines.WorkingDays} (weekends and days without scheduled production are excluded); scheduled production time per machine: {machines.ScheduledMinutesPerMachine} min ({Number(machines.ScheduledMinutesPerMachine / 60m)} h)");
        foreach (var machine in machines.Machines)
        {
            texts.Add($"Machine {machine.ResourceCode} production: {Number(machine.ScheduledUnits)} units in scheduled time = {Number(machine.UnitsPerScheduledHour)} units per scheduled hour; {Number(machine.PlannedBreakUnits)} units during planned breaks (not counted); {Number(machine.OutsideShiftUnits)} outside-shift (overtime) units");
            var reasons = machine.TopUnplannedReasons.Count == 0
                ? "none"
                : string.Join("; ", machine.TopUnplannedReasons.Select(r => $"{r.Reason} {Number(r.Minutes)} min x{r.Count}"));
            texts.Add($"Machine {machine.ResourceCode} downtime: unplanned downtime {Number(machine.UnplannedStopMinutes)} min in {machine.UnplannedStops} stops within scheduled time, availability {Number(machine.AvailabilityPercent)}% of scheduled time; planned-break stop time {Number(machine.PlannedBreakStopMinutes)} min (planned, not a loss); outside-shift stop time {Number(machine.OutsideShiftStopMinutes)} min (not counted); top unplanned reasons: {reasons}");
        }

        if (machines.Machines.Count > 0)
        {
            var mostDowntime = machines.Machines.MaxBy(m => m.UnplannedStopMinutes)!;
            var lowestAvailability = machines.Machines.MinBy(m => m.AvailabilityPercent ?? 100m)!;
            var bestRate = machines.Machines.MaxBy(m => m.UnitsPerScheduledHour ?? 0m)!;
            var worstRate = machines.Machines.MinBy(m => m.UnitsPerScheduledHour ?? 0m)!;
            texts.Add($"Machine with the most unplanned downtime: {mostDowntime.ResourceCode} ({Number(mostDowntime.UnplannedStopMinutes)} min)");
            texts.Add($"Machine with the lowest availability: {lowestAvailability.ResourceCode} ({Number(lowestAvailability.AvailabilityPercent)}%)");
            texts.Add($"Machine with the highest units per scheduled hour: {bestRate.ResourceCode} ({Number(bestRate.UnitsPerScheduledHour)})");
            texts.Add($"Machine with the lowest units per scheduled hour: {worstRate.ResourceCode} ({Number(worstRate.UnitsPerScheduledHour)})");
            var overtime = machines.Machines.Sum(m => m.OutsideShiftUnits);
            texts.Add(overtime > 0
                ? $"Outside-shift (overtime) production: {Number(overtime)} units in total"
                : "No outside-shift production is recorded; no overtime is classified");
        }
    }

    /// <summary>Strips control characters and bounds the length of the question sent to the model.</summary>
    public static string SanitizeQuestion(string question)
    {
        var sanitized = new string(question.Where(ch => !char.IsControl(ch) || ch is '\n' or '\t').ToArray()).Trim();
        return sanitized.Length <= MaxQueryCharacters ? sanitized : sanitized[..MaxQueryCharacters];
    }

    /// <summary>
    /// A numeric claim is accepted only when the same number appears in the retrieved facts or in the question, and an
    /// identifier-like token (digits or hyphens) only when it appears in the facts. Returns the first offending token.
    /// </summary>
    public static string? FindUngroundedClaim(string answer, IReadOnlyList<ModelGroundedFact> facts, string question)
    {
        answer = ThousandsSeparatorPattern().Replace(FactIdPattern().Replace(answer, string.Empty), string.Empty);
        var source = string.Join('\n', facts.Select(f => f.Text)) + "\n" + question;
        var allowedNumbers = NumberPattern().Matches(source)
            .Select(m => NormalizeNumber(m.Value))
            .ToHashSet(StringComparer.Ordinal);

        var sourceValues = NumberPattern().Matches(source)
            .Select(m => decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToList();

        foreach (Match match in NumberPattern().Matches(answer))
        {
            if (allowedNumbers.Contains(NormalizeNumber(match.Value)) || IsRoundedSourceValue(match.Value, sourceValues))
            {
                continue;
            }

            return match.Value;
        }

        foreach (var token in answer.Split([' ', ',', ';', ':', '(', ')', '[', ']', '\n', '\r', '"'], StringSplitOptions.RemoveEmptyEntries))
        {
            // Turkish suffixes attach with an apostrophe ("M-2'dir"): the identifier is the part before it.
            var trimmed = token.Split('\'', '’')[0].Trim('.', '!', '?');
            if (trimmed.Length >= 3
                && trimmed.Contains('-', StringComparison.Ordinal)
                && trimmed.Any(char.IsLetterOrDigit)
                && trimmed.Any(char.IsDigit)
                && !source.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }
        }

        return null;
    }

    /// <summary>Maps a gateway outcome to the customer-facing AI state. Null when a real model answer was produced.</summary>
    public static string? MapAiState(ModelGatewayResponse response)
    {
        var realModelAnswered = response.Succeeded
            && !response.Routing.UsedFallback
            && response.Routing.IntelligencePath == ModelGatewayIntelligencePaths.FrontierModel
            && response.Routing.ProviderId == "openai";
        if (realModelAnswered)
        {
            return null;
        }

        var reason = response.Routing.RoutingReason ?? string.Empty;
        if (reason.StartsWith("cost-budget", StringComparison.OrdinalIgnoreCase))
        {
            return FactoryCopilotAiStates.BudgetLimited;
        }

        var state = response.FailureState;
        if (string.IsNullOrEmpty(state) && response.Routing.UsedFallback)
        {
            state = reason;
        }

        var outcome = response.ProviderOutcome ?? string.Empty;
        return state switch
        {
            ModelGatewayFailureStates.AuthenticationFailed => FactoryCopilotAiStates.AuthenticationFailed,
            ModelGatewayFailureStates.RateLimit => FactoryCopilotAiStates.RateLimited,
            ModelGatewayFailureStates.Timeout => FactoryCopilotAiStates.Timeout,
            ModelGatewayFailureStates.InvalidOutput or ModelGatewayFailureStates.SchemaFailure =>
                FactoryCopilotAiStates.InvalidOutput,
            ModelGatewayFailureStates.PolicyRejected => FactoryCopilotAiStates.PolicyRejected,
            ModelGatewayFailureStates.InsufficientContext => FactoryCopilotAiStates.InsufficientData,
            _ when outcome is "openai-not-configured" or "openai-invalid-configuration" =>
                FactoryCopilotAiStates.NotConfigured,
            _ => FactoryCopilotAiStates.ProviderUnavailable,
        };
    }

    private static string Number(decimal? value) => (value ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);

    private static string Clean(string text)
    {
        var collapsed = new string(text.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        return collapsed.Length <= MaxFactCharacters ? collapsed : collapsed[..MaxFactCharacters];
    }

    /// <summary>
    /// A large number may be a rounding or truncation of a retrieved value ("approximately 813" or "814" for 813.54) to the number of decimals
    /// the answer shows. Small numbers are never accepted as rounded values, so an invented small count is still caught.
    /// </summary>
    private static bool IsRoundedSourceValue(string token, IReadOnlyList<decimal> sourceValues)
    {
        var text = token.Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || Math.Abs(value) < 100m)
        {
            return false;
        }

        var decimals = text.Contains('.', StringComparison.Ordinal) ? text.Length - text.IndexOf('.', StringComparison.Ordinal) - 1 : 0;
        return decimals <= 2
            && sourceValues.Any(v => Math.Round(v, decimals, MidpointRounding.AwayFromZero) == value
                                     || Math.Round(v, decimals, MidpointRounding.ToZero) == value);
    }

    private static string NormalizeNumber(string value)
    {
        var text = value.Replace(',', '.');
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString("0.####", CultureInfo.InvariantCulture)
            : text;
    }

    // "12,345" is one number: a comma or no-break space followed by exactly three digits is a thousands separator.
    [GeneratedRegex(@"(?<=\d)[,  ](?=\d{3}(?!\d))")]
    private static partial Regex ThousandsSeparatorPattern();

    [GeneratedRegex(@"\bF\d+\b")]
    private static partial Regex FactIdPattern();

    [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
    private static partial Regex NumberPattern();
}
