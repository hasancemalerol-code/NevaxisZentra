using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.GoldenDemoP3;

public static class GoldenDemoP3OutcomeProjectionEngine
{
    public static OutcomeSnapshot Build(GoldenDemoWorkspaceSnapshot workspace)
    {
        var verificationResult = ResolveVerificationResult(workspace);
        var baselineWindow = GoldenDemoP3MeasurementWindowEngine.BuildBaseline(workspace);
        var validationWindowObs = workspace.PostInterventionObservation;
        var validationMeasurement = validationWindowObs is null
            ? null
            : GoldenDemoP3MeasurementWindowEngine.BuildValidation(workspace, validationWindowObs);
        var validationRun = GoldenDemoP3ValidationWindowEngine.Build(
            workspace,
            verificationResult,
            baselineWindow,
            validationMeasurement);
        var hero = GoldenDemoP3OutcomeHeroEngine.Build(workspace, verificationResult);
        var metrics = GoldenDemoP3MetricComparisonEngine.Build(workspace, verificationResult);
        var checks = GoldenDemoP3VerificationCheckEngine.Build(workspace, verificationResult);
        var energy = GoldenDemoP3EnergyImpactEngine.Build(workspace);
        var business = GoldenDemoP3BusinessImpactEngine.Build(workspace, verificationResult);
        var traceability = GoldenDemoP3TraceabilityEngine.Build(workspace);

        return new OutcomeSnapshot(
            workspace.Event.Id,
            workspace.Event.MachineCode,
            ResolveInterventionStatus(workspace),
            ResolveOutcomeStatus(workspace, verificationResult),
            ResolveConfidence(verificationResult),
            validationRun,
            baselineWindow,
            validationMeasurement,
            hero,
            metrics,
            checks,
            BuildInsufficientReasons(workspace, verificationResult, baselineWindow, validationMeasurement),
            energy,
            business,
            traceability);
    }

    private static OutcomeVerificationResult ResolveVerificationResult(GoldenDemoWorkspaceSnapshot workspace)
    {
        if (workspace.Verification is not null &&
            !string.IsNullOrWhiteSpace(workspace.Verification.CriteriaJson))
        {
            var criteria = DeserializeCriteria(workspace.Verification.CriteriaJson);
            return new OutcomeVerificationResult(
                workspace.Verification.Result,
                workspace.Verification.Summary,
                criteria,
                workspace.Verification.RecoveredGoodPartsPerShift);
        }

        return OutcomeVerificationEngine.Verify(
            workspace.CurrentObservation,
            workspace.PostInterventionObservation,
            workspace.Recovery);
    }

    private static List<VerificationCriterionResult> DeserializeCriteria(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<VerificationCriterionResult>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string ResolveInterventionStatus(GoldenDemoWorkspaceSnapshot workspace) =>
        workspace.Plan?.Status switch
        {
            GoldenDemoPlanActionStatuses.Completed => "completed",
            GoldenDemoPlanActionStatuses.InProgress => "in-progress",
            _ when workspace.Event.Status == GoldenDemoP1EventStatuses.InterventionInProgress => "in-progress",
            _ when workspace.Plan?.ApprovedAtUtc is not null => "approved",
            _ => workspace.Plan is null ? "not-created" : "planned",
        };

    private static string ResolveConfidence(OutcomeVerificationResult verification) =>
        verification.Result switch
        {
            GoldenDemoVerificationResults.Verified => "high",
            GoldenDemoVerificationResults.PartiallyVerified => "medium",
            GoldenDemoVerificationResults.NotVerified => "low",
            _ => "insufficient",
        };

    private static string ResolveOutcomeStatus(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification) =>
        workspace.Verification?.Result
        ?? (workspace.PostInterventionObservation is null
            ? GoldenDemoVerificationResults.InsufficientData
            : verification.Result);

    private static List<string> BuildInsufficientReasons(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification,
        MeasurementWindowModel baseline,
        MeasurementWindowModel? validation)
    {
        var reasons = new List<string>();
        if (!workspace.CycleLoss.HasBaseline)
        {
            reasons.Add("INSUFFICIENT BASELINE");
        }

        if (workspace.PostInterventionObservation is null &&
            workspace.Event.Status is GoldenDemoP1EventStatuses.ValidationRun or GoldenDemoP1EventStatuses.Verified)
        {
            reasons.Add("Post-intervention validation window missing");
        }

        if (validation is not null)
        {
            var durationMinutes = (validation.WindowEndUtc - validation.WindowStartUtc).TotalMinutes;
            if (durationMinutes < 15)
            {
                reasons.Add("Validation requires additional operating time");
            }

            if (validation.GoodParts <= 0)
            {
                reasons.Add("No production recorded in validation window");
            }
        }

        if (verification.Result == GoldenDemoVerificationResults.InsufficientData)
        {
            reasons.Add(verification.Summary);
        }

        if (workspace.Metrics.KwhPerGoodPart is null)
        {
            reasons.Add("Energy telemetry unavailable for intensity comparison");
        }

        return reasons;
    }
}

public static class GoldenDemoP3MeasurementWindowEngine
{
    public static MeasurementWindowModel BuildBaseline(GoldenDemoWorkspaceSnapshot workspace)
    {
        var current = workspace.CurrentObservation;
        var baseline = workspace.Baseline;
        return new MeasurementWindowModel(
            "baseline",
            current.WindowStartUtc,
            current.WindowEndUtc,
            current.GoodParts,
            current.RejectParts,
            baseline?.ReferenceSourceLabel ?? "Current operating window",
            BuildProductionContext(workspace),
            current.TotalCycleSeconds,
            GoodPartsPerHour(current),
            current.AvailabilityPercent,
            current.MicroStopCount,
            current.KwhPerGoodPart);
    }

    public static MeasurementWindowModel BuildValidation(
        GoldenDemoWorkspaceSnapshot workspace,
        GoldenDemoCycleObservation observation) =>
        new(
            "validation",
            observation.WindowStartUtc,
            observation.WindowEndUtc,
            observation.GoodParts,
            observation.RejectParts,
            "Post-intervention validation window",
            BuildProductionContext(workspace),
            observation.TotalCycleSeconds,
            GoodPartsPerHour(observation),
            observation.AvailabilityPercent,
            observation.MicroStopCount,
            observation.KwhPerGoodPart);

    private static string BuildProductionContext(GoldenDemoWorkspaceSnapshot workspace)
    {
        var mold = string.IsNullOrWhiteSpace(workspace.Event.MoldReference)
            ? "mold unavailable"
            : workspace.Event.MoldReference;
        var product = string.IsNullOrWhiteSpace(workspace.Event.ProductReference)
            ? "product context unavailable"
            : workspace.Event.ProductReference;
        return $"{workspace.Event.MachineCode} · {mold} · {product}";
    }

    private static decimal GoodPartsPerHour(GoldenDemoCycleObservation observation)
    {
        var hours = (decimal)(observation.WindowEndUtc - observation.WindowStartUtc).TotalHours;
        return hours <= 0 ? 0 : Math.Round(observation.GoodParts / hours, 1);
    }
}

public static class GoldenDemoP3ValidationWindowEngine
{
    private const int MinimumSamplesRequired = 120;

    public static ValidationWindowModel Build(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification,
        MeasurementWindowModel baseline,
        MeasurementWindowModel? validation)
    {
        var status = ResolveStatus(workspace, verification, validation);
        var samples = validation?.GoodParts;
        var completeness = ResolveCompleteness(validation, baseline);
        var ready = status == GoldenDemoP3ValidationStatuses.ReadyForVerification ||
                      verification.Result is GoldenDemoVerificationResults.Verified
                          or GoldenDemoVerificationResults.PartiallyVerified
                          or GoldenDemoVerificationResults.NotVerified;

        return new ValidationWindowModel(
            status,
            StatusLabel(status),
            validation?.WindowStartUtc ?? workspace.Event.UpdatedAtUtc,
            samples,
            MinimumSamplesRequired,
            completeness,
            BuildSummary(workspace, status, validation),
            ready);
    }

    private static string ResolveStatus(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification,
        MeasurementWindowModel? validation)
    {
        if (workspace.Verification is not null ||
            workspace.Event.Status == GoldenDemoP1EventStatuses.Verified)
        {
            if (verification.Result is GoldenDemoVerificationResults.Verified
                or GoldenDemoVerificationResults.PartiallyVerified
                or GoldenDemoVerificationResults.NotVerified)
            {
                return verification.Result;
            }
        }

        if (workspace.Event.Status is GoldenDemoP1EventStatuses.Active
            or GoldenDemoP1EventStatuses.InterventionInProgress)
        {
            return GoldenDemoP3ValidationStatuses.WaitingForValidation;
        }

        if (validation is null)
        {
            return workspace.Event.Status == GoldenDemoP1EventStatuses.ValidationRun
                ? GoldenDemoP3ValidationStatuses.CollectingData
                : GoldenDemoP3ValidationStatuses.WaitingForValidation;
        }

        if (!workspace.CycleLoss.HasBaseline)
        {
            return GoldenDemoP3ValidationStatuses.InsufficientData;
        }

        return GoldenDemoP3ValidationStatuses.ReadyForVerification;
    }

    private static string ResolveCompleteness(MeasurementWindowModel? validation, MeasurementWindowModel baseline)
    {
        if (validation is null)
        {
            return "0%";
        }

        var sampleRatio = validation.GoodParts <= 0
            ? 0
            : Math.Min(100, (int)Math.Round(validation.GoodParts / (double)Math.Max(1, baseline.GoodParts / 2) * 100));
        return $"{sampleRatio}%";
    }

    private static string BuildSummary(
        GoldenDemoWorkspaceSnapshot workspace,
        string status,
        MeasurementWindowModel? validation) =>
        status switch
        {
            GoldenDemoP3ValidationStatuses.WaitingForValidation =>
                "FactOS is waiting for intervention completion before validating post-intervention production evidence.",
            GoldenDemoP3ValidationStatuses.CollectingData =>
                "FactOS is validating the intervention against post-intervention production evidence.",
            GoldenDemoP3ValidationStatuses.ReadyForVerification =>
                "Validation window recorded. Outcome verification can be executed against measured post-intervention data.",
            GoldenDemoVerificationResults.Verified =>
                "Production recovery verified against validation window evidence.",
            GoldenDemoVerificationResults.PartiallyVerified =>
                "Partial improvement observed; secondary goals not fully confirmed.",
            GoldenDemoVerificationResults.NotVerified =>
                "Expected production recovery not fully verified.",
            GoldenDemoP3ValidationStatuses.InsufficientData =>
                "Required validation evidence is insufficient.",
            _ => workspace.Recovery.SafetyMessage,
        };

    private static string StatusLabel(string status) => status switch
    {
        GoldenDemoP3ValidationStatuses.WaitingForValidation => "Waiting For Validation",
        GoldenDemoP3ValidationStatuses.CollectingData => "Collecting Data",
        GoldenDemoP3ValidationStatuses.ReadyForVerification => "Ready For Verification",
        GoldenDemoVerificationResults.Verified => "Verified",
        GoldenDemoVerificationResults.PartiallyVerified => "Partially Verified",
        GoldenDemoVerificationResults.NotVerified => "Not Verified",
        GoldenDemoP3ValidationStatuses.InsufficientData => "Insufficient Data",
        _ => status,
    };
}

public static class GoldenDemoP3OutcomeHeroEngine
{
    public static OutcomeHeroModel Build(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification)
    {
        var beforeAfter = workspace.BeforeAfter;
        var recovered =
            (int)(workspace.Verification?.RecoveredGoodPartsPerShift
            ?? beforeAfter?.RecoveredGoodPartsPerShift
            ?? verification.RecoveredGoodPartsPerShift);

        var showSuccess = verification.Result == GoldenDemoVerificationResults.Verified;
        var headline = verification.Result switch
        {
            GoldenDemoVerificationResults.Verified => "PRODUCTION RECOVERED",
            GoldenDemoVerificationResults.PartiallyVerified => "PARTIAL RECOVERY",
            GoldenDemoVerificationResults.NotVerified => "OUTCOME NOT VERIFIED",
            _ => "INSUFFICIENT DATA",
        };

        var subheadline = verification.Result switch
        {
            GoldenDemoVerificationResults.Verified =>
                "FactOS compared pre-intervention baseline against post-intervention validation evidence.",
            GoldenDemoVerificationResults.PartiallyVerified =>
                "Measurable improvement exists, but not all verification checks passed.",
            GoldenDemoVerificationResults.NotVerified =>
                "Intervention completed, but production recovery was not verified.",
            _ => "Validation evidence is not yet sufficient for outcome verification.",
        };

        var highlights = new List<OutcomeHeroMetricModel>();
        if (beforeAfter is not null)
        {
            highlights.Add(Metric(
                "Cycle Time",
                FormatDecimal(beforeAfter.BeforeCycleSeconds),
                FormatDecimal(beforeAfter.AfterCycleSeconds),
                DeltaPercent(beforeAfter.BeforeCycleSeconds, beforeAfter.AfterCycleSeconds, invert: true)));
            highlights.Add(Metric(
                "Throughput",
                FormatDecimal(beforeAfter.BeforeGoodPartsPerHour),
                FormatDecimal(beforeAfter.AfterGoodPartsPerHour),
                DeltaPercent(beforeAfter.BeforeGoodPartsPerHour, beforeAfter.AfterGoodPartsPerHour)));
            highlights.Add(Metric(
                "Micro-Stops",
                beforeAfter.BeforeMicroStopCount.ToString(CultureInfo.InvariantCulture),
                beforeAfter.AfterMicroStopCount.ToString(CultureInfo.InvariantCulture),
                DeltaCount(beforeAfter.BeforeMicroStopCount, beforeAfter.AfterMicroStopCount, invert: true)));
            highlights.Add(Metric(
                "Reject Rate",
                FormatDecimal(beforeAfter.BeforeRejectRatePercent, "0.0") + "%",
                FormatDecimal(beforeAfter.AfterRejectRatePercent, "0.0") + "%",
                beforeAfter.AfterRejectRatePercent <= beforeAfter.BeforeRejectRatePercent ? "Stable / Improved" : "Regressed"));
        }

        return new OutcomeHeroModel(
            headline,
            subheadline,
            Math.Max(0, recovered),
            GoldenDemoImpactClassifications.Calculated,
            verification.Result,
            showSuccess,
            highlights);
    }

    private static OutcomeHeroMetricModel Metric(
        string label,
        string before,
        string after,
        string delta) => new(label, before, after, delta, GoldenDemoP3MetricStatuses.Improved);

    private static string DeltaPercent(decimal before, decimal after, bool invert = false)
    {
        if (before == 0)
        {
            return "—";
        }

        var change = (after - before) / before * 100m;
        if (invert)
        {
            change = -change;
        }

        var sign = change >= 0 ? "+" : string.Empty;
        return $"{sign}{change.ToString("0.0", CultureInfo.InvariantCulture)}%";
    }

    private static string DeltaCount(int before, int after, bool invert = false)
    {
        var delta = after - before;
        if (invert)
        {
            delta = -delta;
        }

        var sign = delta >= 0 ? "+" : string.Empty;
        return $"{sign}{delta}";
    }

    private static string FormatDecimal(decimal value, string format = "0.0") =>
        value.ToString(format, CultureInfo.InvariantCulture);
}

public static class GoldenDemoP3MetricComparisonEngine
{
    public static IReadOnlyList<MetricComparisonModel> Build(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification)
    {
        if (workspace.BeforeAfter is null)
        {
            return [];
        }

        var beforeAfter = workspace.BeforeAfter;
        return
        [
            Compare("cycle-time", "Cycle Time", Format(beforeAfter.BeforeCycleSeconds), Format(beforeAfter.AfterCycleSeconds),
                beforeAfter.AfterCycleSeconds <= beforeAfter.BeforeCycleSeconds, GoldenDemoImpactClassifications.Measured),
            Compare("throughput", "Good Parts / Hour", Format(beforeAfter.BeforeGoodPartsPerHour), Format(beforeAfter.AfterGoodPartsPerHour),
                beforeAfter.AfterGoodPartsPerHour >= beforeAfter.BeforeGoodPartsPerHour, GoldenDemoImpactClassifications.Measured),
            Compare("projected-shift", "Projected Good Parts / Shift",
                workspace.Recovery.CurrentExpectedShiftGoodParts.ToString(CultureInfo.InvariantCulture),
                workspace.Recovery.RecoveredExpectedShiftGoodParts.ToString(CultureInfo.InvariantCulture),
                workspace.Recovery.RecoveredExpectedShiftGoodParts >= workspace.Recovery.CurrentExpectedShiftGoodParts,
                GoldenDemoImpactClassifications.Calculated),
            Compare("availability", "Availability",
                Format(workspace.Metrics.AvailabilityPercent) + "%",
                workspace.PostInterventionObservation is null ? null : Format(workspace.PostInterventionObservation.AvailabilityPercent) + "%",
                workspace.PostInterventionObservation?.AvailabilityPercent >= workspace.Metrics.AvailabilityPercent - 1m,
                GoldenDemoImpactClassifications.Measured),
            Compare("micro-stops", "Micro-Stops",
                beforeAfter.BeforeMicroStopCount.ToString(CultureInfo.InvariantCulture),
                beforeAfter.AfterMicroStopCount.ToString(CultureInfo.InvariantCulture),
                beforeAfter.AfterMicroStopCount <= beforeAfter.BeforeMicroStopCount,
                GoldenDemoImpactClassifications.Measured),
            Compare("reject-rate", "Reject Rate",
                Format(beforeAfter.BeforeRejectRatePercent, "0.0") + "%",
                Format(beforeAfter.AfterRejectRatePercent, "0.0") + "%",
                beforeAfter.AfterRejectRatePercent <= beforeAfter.BeforeRejectRatePercent + 0.5m,
                GoldenDemoImpactClassifications.Measured),
            Compare("kwh-good-part", "kWh / Good Part",
                beforeAfter.BeforeKwhPerGoodPart?.ToString("0.000", CultureInfo.InvariantCulture),
                beforeAfter.AfterKwhPerGoodPart?.ToString("0.000", CultureInfo.InvariantCulture),
                beforeAfter.BeforeKwhPerGoodPart is not null &&
                beforeAfter.AfterKwhPerGoodPart is not null &&
                beforeAfter.AfterKwhPerGoodPart <= beforeAfter.BeforeKwhPerGoodPart,
                beforeAfter.BeforeKwhPerGoodPart is null ? GoldenDemoImpactClassifications.Estimated : GoldenDemoImpactClassifications.Measured),
            Compare("recovered-production", "+X Good Parts / Shift",
                "0",
                Math.Max(0, (int)verification.RecoveredGoodPartsPerShift).ToString(CultureInfo.InvariantCulture),
                verification.RecoveredGoodPartsPerShift > 0,
                GoldenDemoImpactClassifications.Calculated),
        ];
    }

    private static MetricComparisonModel Compare(
        string code,
        string label,
        string? before,
        string? after,
        bool? improved,
        string classification)
    {
        var status = after is null || before is null
            ? GoldenDemoP3MetricStatuses.Unavailable
            : improved == true
                ? GoldenDemoP3MetricStatuses.Improved
                : improved == false
                    ? GoldenDemoP3MetricStatuses.Regressed
                    : GoldenDemoP3MetricStatuses.Stable;

        var delta = before is null || after is null ? null : $"{before} → {after}";
        return new MetricComparisonModel(code, label, before, after, delta, status, classification);
    }

    private static string Format(decimal value, string format = "0.0") =>
        value.ToString(format, CultureInfo.InvariantCulture);
}

public static class GoldenDemoP3VerificationCheckEngine
{
    public static IReadOnlyList<VerificationCheckModel> Build(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification)
    {
        if (verification.Criteria.Count == 0)
        {
            return
            [
                new VerificationCheckModel(
                    "validation-window",
                    "Validation Window",
                    workspace.PostInterventionObservation is not null,
                    workspace.PostInterventionObservation?.WindowEndUtc.ToString("O"),
                    workspace.CurrentObservation.WindowEndUtc.ToString("O"),
                    workspace.PostInterventionObservation is null ? "FAIL" : "PASS",
                    verification.Summary,
                    GoldenDemoImpactClassifications.Measured),
            ];
        }

        return verification.Criteria.Select(criterion =>
        {
            var parts = criterion.Detail?.Split("→", StringSplitOptions.TrimEntries) ?? [];
            return new VerificationCheckModel(
                criterion.Code,
                criterion.Label,
                criterion.Passed,
                parts.Length > 1 ? parts[1] : criterion.Detail,
                parts.Length > 0 ? parts[0].Replace("Before", string.Empty, StringComparison.OrdinalIgnoreCase).Trim() : null,
                criterion.Passed ? "PASS" : "FAIL",
                criterion.Detail ?? criterion.Label,
                GoldenDemoImpactClassifications.Measured);
        }).ToList();
    }
}

public static class GoldenDemoP3EnergyImpactEngine
{
    public static EnergyImpactModel Build(GoldenDemoWorkspaceSnapshot workspace)
    {
        var before = workspace.BeforeAfter?.BeforeKwhPerGoodPart ?? workspace.CurrentObservation.KwhPerGoodPart;
        var after = workspace.BeforeAfter?.AfterKwhPerGoodPart ?? workspace.PostInterventionObservation?.KwhPerGoodPart;

        if (before is null || after is null)
        {
            return new EnergyImpactModel(
                false,
                GoldenDemoImpactClassifications.Estimated,
                before,
                after,
                null,
                "ENERGY IMPACT — Insufficient data",
                "Energy intensity comparison unavailable without kWh / good part telemetry.");
        }

        var change = before.Value == 0
            ? 0
            : Math.Round((after.Value - before.Value) / before.Value * 100m, 1);

        return new EnergyImpactModel(
            true,
            GoldenDemoImpactClassifications.Measured,
            before,
            after,
            change,
            $"Before {before.Value.ToString("0.000", CultureInfo.InvariantCulture)} kWh/good part · After {after.Value.ToString("0.000", CultureInfo.InvariantCulture)} kWh/good part · Change {change.ToString("0.0", CultureInfo.InvariantCulture)}%",
            change <= 0
                ? "Observed relationship: post-intervention energy intensity improved alongside production recovery."
                : "Associated pattern: energy intensity moved with post-intervention operating conditions.");
    }
}

public static class GoldenDemoP3BusinessImpactEngine
{
    public static BusinessImpactClosureModel Build(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification)
    {
        var impact = workspace.BusinessImpact;
        var operationalParts = verification.RecoveredGoodPartsPerShift > 0
            ? (int)verification.RecoveredGoodPartsPerShift
            : impact.OperationalGoodPartsPerShift;

        return new BusinessImpactClosureModel(
            operationalParts,
            GoldenDemoImpactClassifications.Calculated,
            impact.CapacityImpactPercent,
            impact.EstimatedBusinessImpactTry,
            GoldenDemoImpactClassifications.Estimated,
            verification.Result == GoldenDemoVerificationResults.Verified ? impact.VerifiedBusinessImpactTry : null,
            GoldenDemoImpactClassifications.Estimated,
            BuildCategories(workspace, verification, operationalParts));
    }

    private static IReadOnlyList<BusinessImpactCategoryModel> BuildCategories(
        GoldenDemoWorkspaceSnapshot workspace,
        OutcomeVerificationResult verification,
        int operationalParts)
    {
        var beforeCycle = workspace.CurrentObservation.TotalCycleSeconds.ToString("0.0", CultureInfo.InvariantCulture);
        var afterCycle = workspace.PostInterventionObservation?.TotalCycleSeconds.ToString("0.0", CultureInfo.InvariantCulture) ?? "—";
        var beforeThroughput = workspace.BeforeAfter?.BeforeGoodPartsPerHour.ToString("0.0", CultureInfo.InvariantCulture) ?? "—";
        var afterThroughput = workspace.BeforeAfter?.AfterGoodPartsPerHour.ToString("0.0", CultureInfo.InvariantCulture) ?? "—";

        return
        [
            new("production-performance", "Production Performance Loss", $"{beforeCycle}s cycle", $"{afterCycle}s cycle",
                $"+{operationalParts} good parts / shift", GoldenDemoImpactClassifications.Calculated),
            new("downtime-exposure", "Downtime / Micro-stop Exposure",
                workspace.CurrentObservation.MicroStopCount.ToString(CultureInfo.InvariantCulture),
                workspace.PostInterventionObservation?.MicroStopCount.ToString(CultureInfo.InvariantCulture) ?? "—",
                "Reduced micro-stop exposure", GoldenDemoImpactClassifications.Measured),
            new("quality-scrap", "Quality / Scrap Loss",
                workspace.BeforeAfter?.BeforeRejectRatePercent.ToString("0.0", CultureInfo.InvariantCulture) + "%" ?? "—",
                workspace.BeforeAfter?.AfterRejectRatePercent.ToString("0.0", CultureInfo.InvariantCulture) + "%" ?? "—",
                "Stable / improved reject guardrail", GoldenDemoImpactClassifications.Measured),
            new("energy-inefficiency", "Energy Inefficiency",
                workspace.BeforeAfter?.BeforeKwhPerGoodPart?.ToString("0.000", CultureInfo.InvariantCulture) ?? "Unavailable",
                workspace.BeforeAfter?.AfterKwhPerGoodPart?.ToString("0.000", CultureInfo.InvariantCulture) ?? "Unavailable",
                workspace.Metrics.EnergyIntensityDeviationPercent is null ? "Insufficient data" : "Associated improvement",
                workspace.Metrics.KwhPerGoodPart is null ? GoldenDemoImpactClassifications.Estimated : GoldenDemoImpactClassifications.Measured),
            new("capacity-loss", "Capacity Loss",
                beforeThroughput,
                afterThroughput,
                $"+{operationalParts} protected throughput / shift", GoldenDemoImpactClassifications.Calculated),
        ];
    }
}

public static class GoldenDemoP3TraceabilityEngine
{
    public static OutcomeTraceabilityModel Build(GoldenDemoWorkspaceSnapshot workspace) =>
        new(
            workspace.Plan?.Id.ToString(),
            workspace.Verification?.Id.ToString(),
            workspace.Plan is null ? null : $"event:{workspace.Event.Id}",
            workspace.AgentFindings
                .Where(f => f.AgentCode != GoldenDemoAgentCodes.Orchestrator)
                .Select(f => $"{f.AgentCode}:{f.Id}")
                .ToList(),
            "Agent finding → orchestrator decision → approved action → intervention → validation → outcome");
}
