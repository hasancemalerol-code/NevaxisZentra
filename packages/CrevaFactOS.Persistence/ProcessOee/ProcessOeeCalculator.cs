using System.Globalization;

namespace CrevaFactOS.Persistence.ProcessOee;

public static class ProcessOeeCalculator
{
    private static readonly MidpointRounding Rounding = MidpointRounding.AwayFromZero;

    public static ProcessOeeCalculationResult Calculate(
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence)
    {
        var missingFields = new List<string>();
        var drivers = new List<ProcessOeeDriverReadModel>();
        var allEvidence = evidence.Evidence.ToList();

        if (!evidence.PlannedProductionSeconds.HasValue || evidence.PlannedProductionSeconds.Value <= 0)
        {
            missingFields.Add("plannedProductionSeconds");
        }

        if (evidence.HasEvidenceConflict)
        {
            drivers.Add(new ProcessOeeDriverReadModel(
                "evidence-conflict",
                "Kaynak verileri çakışıyor",
                string.Join(", ", evidence.Conflicts),
                "warn"));
        }

        if (evidence.UnknownSeconds > 0)
        {
            drivers.Add(new ProcessOeeDriverReadModel(
                "unknown-time-visible",
                "Sınıflandırılamayan süre görünür",
                Inv(evidence.UnknownSeconds),
                "info"));
        }

        var status = ProcessOeeAssessmentStatuses.Calculated;
        if (missingFields.Count > 0)
        {
            status = ProcessOeeAssessmentStatuses.InsufficientData;
        }
        else if (evidence.HasEvidenceConflict)
        {
            status = ProcessOeeAssessmentStatuses.ReviewRequired;
        }
        else if (evidence.SourceFreshnessUtc.HasValue
                 && evidence.SourceFreshnessUtc.Value < window.WindowEndUtc.AddHours(-24))
        {
            status = ProcessOeeAssessmentStatuses.StaleData;
        }

        decimal? availabilityPercent = null;
        if (evidence.PlannedProductionSeconds is > 0 && evidence.OperatingSeconds.HasValue)
        {
            if (evidence.OperatingSeconds.Value <= 0)
            {
                availabilityPercent = 0m;
            }
            else
            {
                availabilityPercent = RoundPercent(
                    (decimal)evidence.OperatingSeconds.Value / evidence.PlannedProductionSeconds.Value * 100m);
            }

            drivers.Add(new ProcessOeeDriverReadModel(
                "availability",
                "Kullanılabilirlik = Çalışma Süresi / Planlı Üretim Süresi",
                FormatPercent(availabilityPercent),
                "pass"));
        }
        else if (missingFields.Contains("plannedProductionSeconds"))
        {
            drivers.Add(new ProcessOeeDriverReadModel(
                "availability-missing",
                "Planlı üretim süresi eksik",
                null,
                "fail"));
        }

        decimal? performanceRawPercent = null;
        decimal? performancePercent = null;
        var shotCount = evidence.ObservedCycleCount ?? evidence.DerivedShotCount;
        var usingDerivedShots = !evidence.ObservedCycleCount.HasValue && evidence.DerivedShotCount.HasValue;

        if (usingDerivedShots)
        {
            drivers.Add(new ProcessOeeDriverReadModel(
                "derived-shot-count",
                "Performans türetilmiş vuruş sayısına dayanır",
                Inv(evidence.DerivedShotCount),
                "warn"));
        }

        if (shotCount.HasValue
            && evidence.IdealCycleTimeSeconds is > 0
            && evidence.OperatingSeconds is > 0)
        {
            var idealRunTime = evidence.IdealCycleTimeSeconds.Value * shotCount.Value;
            performanceRawPercent = RoundPercent(idealRunTime / evidence.OperatingSeconds.Value * 100m);
            performancePercent = performanceRawPercent > 100m ? 100m : performanceRawPercent;

            drivers.Add(new ProcessOeeDriverReadModel(
                "performance",
                usingDerivedShots
                    ? "Performans = (İdeal Çevrim × Türetilmiş Vuruş) / Çalışma Süresi"
                    : "Performans = (İdeal Çevrim × Gözlemlenen Vuruş) / Çalışma Süresi",
                FormatPercent(performancePercent),
                performanceRawPercent > 100m ? "warn" : "pass"));

            if (performanceRawPercent > 100m)
            {
                drivers.Add(new ProcessOeeDriverReadModel(
                    "performance-above-100",
                    "Ham performans %100 üzerinde",
                    FormatPercent(performanceRawPercent),
                    "warn"));
            }
        }
        else
        {
            if (!shotCount.HasValue)
            {
                missingFields.Add("observedCycleCount");
            }

            if (!evidence.IdealCycleTimeSeconds.HasValue)
            {
                missingFields.Add("idealCycleTimeSeconds");
            }

            if (!evidence.OperatingSeconds.HasValue || evidence.OperatingSeconds.Value <= 0)
            {
                missingFields.Add("operatingSeconds");
            }

            if (missingFields.Count > 0 && status == ProcessOeeAssessmentStatuses.Calculated)
            {
                status = ProcessOeeAssessmentStatuses.InsufficientData;
            }
        }

        decimal? qualityPercent = null;
        if (evidence.TotalPieceCount is > 0 && evidence.GoodPieceCount.HasValue)
        {
            qualityPercent = RoundPercent(
                (decimal)evidence.GoodPieceCount.Value / evidence.TotalPieceCount.Value * 100m);
            drivers.Add(new ProcessOeeDriverReadModel(
                "quality",
                "Kalite = İyi Parça / Toplam Parça",
                FormatPercent(qualityPercent),
                "pass"));
        }
        else
        {
            missingFields.Add("goodPieceCount");
            missingFields.Add("totalPieceCount");
            if (status == ProcessOeeAssessmentStatuses.Calculated)
            {
                status = ProcessOeeAssessmentStatuses.InsufficientData;
            }
        }

        decimal? oeePercent = null;
        if (availabilityPercent.HasValue && performancePercent.HasValue && qualityPercent.HasValue
            && status is ProcessOeeAssessmentStatuses.Calculated or ProcessOeeAssessmentStatuses.StaleData)
        {
            oeePercent = RoundPercent(availabilityPercent.Value / 100m * performancePercent.Value / 100m * qualityPercent.Value / 100m * 100m);
            drivers.Add(new ProcessOeeDriverReadModel(
                "oee",
                "OEE = Kullanılabilirlik × Performans × Kalite",
                FormatPercent(oeePercent),
                "pass"));
        }

        if (evidence.PlannedProductionSeconds is 0)
        {
            status = ProcessOeeAssessmentStatuses.InsufficientData;
            oeePercent = null;
        }

        if (evidence.OperatingSeconds is 0 && performanceRawPercent is null)
        {
            drivers.Add(new ProcessOeeDriverReadModel(
                "zero-operating-time",
                "Sıfır çalışma süresi koruması uygulandı",
                "0",
                "warn"));
        }

        var inputFingerprint = ProcessOeeIdempotencyFingerprint.ComputeEvaluationInputFingerprint(evidence, window);

        allEvidence.Add(new ProcessOeeEvidenceReadModel(
            "ruleSetVersion",
            ProcessOeeConstants.RuleSetVersion,
            "process-oee-calculator",
            DateTimeOffset.UtcNow,
            false,
            false,
            null));

        return new ProcessOeeCalculationResult(
            status,
            evidence.PlannedProductionSeconds,
            evidence.OperatingSeconds,
            evidence.DowntimeSeconds,
            evidence.UnknownSeconds,
            evidence.ObservedCycleCount,
            evidence.DerivedShotCount,
            evidence.TotalPieceCount,
            evidence.GoodPieceCount,
            evidence.ScrapPieceCount,
            availabilityPercent,
            performanceRawPercent,
            performancePercent,
            qualityPercent,
            oeePercent,
            drivers,
            allEvidence,
            missingFields.Distinct().ToList(),
            evidence.Conflicts.ToList(),
            evidence.SourceSystem,
            evidence.SourceFreshnessUtc,
            evidence.IsSimulatorDerived,
            inputFingerprint);
    }

    public static decimal RoundPercent(decimal value) =>
        Math.Round(value, ProcessOeeConstants.PercentDecimalPlaces, Rounding);

    public static string? FormatPercent(decimal? value) =>
        value.HasValue ? $"{RoundPercent(value.Value).ToString("0.00", CultureInfo.InvariantCulture)}%" : null;

    private static string? Inv(int? value) => value?.ToString(CultureInfo.InvariantCulture);
}
