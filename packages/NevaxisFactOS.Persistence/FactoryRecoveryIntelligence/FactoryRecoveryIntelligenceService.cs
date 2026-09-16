using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryOutcomeExecution;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public interface IFactoryRecoveryIntelligenceService
{
    Task<FactoryRecoveryIntelligenceSnapshot> BuildForCaseAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        CancellationToken cancellationToken);

    Task<FactoryRecoveryHistoricalCaseDetail> GetHistoricalCaseAsync(
        Guid learningRecordId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FactoryRecoveryHistoricalCaseDetail>> SearchHistoricalCasesAsync(
        Guid facilityId,
        FactoryRecoveryLearningSearchFilters filters,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FactoryRecoveryWorkerEffectiveness>> GetWorkerEffectivenessAsync(
        Guid facilityId,
        CancellationToken cancellationToken);
}

public sealed class FactoryRecoveryIntelligenceService : IFactoryRecoveryIntelligenceService
{
    private readonly IFactoryRecoveryLearningRepository repository;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public FactoryRecoveryIntelligenceService(
        IFactoryRecoveryLearningRepository repository,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.repository = repository;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<FactoryRecoveryIntelligenceSnapshot> BuildForCaseAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var facilityId = rootCase.FacilityId ?? Guid.Empty;
        var impact = FactoryOutcomeImpactEngine.FromJson(rootCase.ImpactGraphJson);
        var pattern = FactoryRecoveryPatternEngine.Extract(rootCase, childCases, impact);

        var candidates = await repository.FindCandidatesAsync(
            tenantId,
            facilityId,
            pattern.PatternKey,
            pattern.OutcomeType,
            pattern.EventCategory,
            pattern.MachineCode,
            10,
            cancellationToken);

        var successRecords = candidates
            .Where(r => r.LearningDisposition == FactoryRecoveryLearningDisposition.VerifiedSuccess)
            .ToList();
        var failureRecords = await repository.ListFailureModesAsync(
            tenantId,
            facilityId,
            pattern.PatternKey.Split('|')[0],
            10,
            cancellationToken);

        var similar = successRecords
            .Select(r => FactoryRecoverySimilarityEngine.Score(pattern, r))
            .Where(m => m.SimilarityPercent >= 40)
            .OrderByDescending(m => m.SimilarityPercent)
            .ThenByDescending(m => m.VerifiedAtUtc)
            .Take(7)
            .ToList();

        var strategyPerformance = FactoryRecoveryStrategyIntelligenceEngine.BuildPerformance(
            successRecords,
            failureRecords.ToList());

        var failureModes = failureRecords
            .Select(r => r.FailureSummary ?? r.CausePattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var recoveryValues = similar
            .Select(s => s.RecoveryMinutes)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToList();
        var medianRecovery = Median(recoveryValues);
        var evidenceStrength = FactoryRecoverySimilarityEngine.ResolveEvidenceStrength(
            successRecords.Count,
            similar.Count);

        var planEvidence = new[]
        {
            FactoryRecoveryStrategyIntelligenceEngine.BuildPlanEvidence(
                FactoryOutcomeExecutionPlanKeys.Primary,
                "primary",
                successRecords,
                failureRecords.ToList()),
            FactoryRecoveryStrategyIntelligenceEngine.BuildPlanEvidence(
                FactoryOutcomeExecutionPlanKeys.Alternative,
                "alternative",
                successRecords,
                failureRecords.ToList()),
        };

        return new FactoryRecoveryIntelligenceSnapshot(
            similar.Count,
            similar,
            strategyPerformance,
            failureModes,
            medianRecovery,
            evidenceStrength,
            planEvidence);
    }

    public async Task<FactoryRecoveryHistoricalCaseDetail> GetHistoricalCaseAsync(
        Guid learningRecordId,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var record = await repository.GetByIdAsync(tenantId, learningRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Öğrenme kaydı bulunamadı.");

        return MapHistoricalDetail(record);
    }

    public async Task<IReadOnlyList<FactoryRecoveryHistoricalCaseDetail>> SearchHistoricalCasesAsync(
        Guid facilityId,
        FactoryRecoveryLearningSearchFilters filters,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var records = await repository.SearchAsync(tenantId, facilityId, filters, cancellationToken);
        return records.Select(MapHistoricalDetail).ToList();
    }

    public async Task<IReadOnlyList<FactoryRecoveryWorkerEffectiveness>> GetWorkerEffectivenessAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();
        var records = await repository.SearchAsync(
            tenantId,
            facilityId,
            new FactoryRecoveryLearningSearchFilters(null, null, null, null, null, null, null, null, 200),
            cancellationToken);

        var successRecords = records
            .Where(r => r.LearningDisposition == FactoryRecoveryLearningDisposition.VerifiedSuccess)
            .ToList();

        var grouped = successRecords
            .SelectMany(r => DeserializeList(r.WorkersJson).Select(w => (Worker: w, Record: r)))
            .GroupBy(x => x.Worker, StringComparer.OrdinalIgnoreCase);

        return grouped.Select(group =>
        {
            var recoveryTimes = group
                .Select(x => x.Record.TimeToRecoveryMinutes)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .OrderBy(v => v)
                .ToList();
            var commonWork = group
                .Select(x => x.Record.EventCategory)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            return new FactoryRecoveryWorkerEffectiveness(
                group.Key,
                ResolveWorkerName(group.Key),
                group.Count(),
                Median(recoveryTimes),
                commonWork,
                FactoryRecoverySimilarityEngine.ResolveSampleConfidence(group.Count()));
        }).OrderByDescending(w => w.VerifiedRecoveries).ToList();
    }

    private static FactoryRecoveryHistoricalCaseDetail MapHistoricalDetail(
        FactoryOutcomeLearningRecord record) =>
        new(
            record.Id,
            record.SourceCaseId,
            record.OutcomeType,
            record.EventCategory,
            record.OrderReference ?? "—",
            record.MachineCode ?? "—",
            record.MaterialCode ?? "—",
            record.MoldCode ?? "—",
            record.Severity,
            record.CausePattern,
            record.CausePattern,
            record.SelectedStrategyKey,
            record.AlternativeStrategyKey,
            DeserializeList(record.WorkersJson),
            DeserializeList(record.ActionsJson),
            DeserializeList(record.BlockersJson),
            DeserializeList(record.FailureModesJson),
            record.DeliveryProtected == true,
            record.ActualRecoveredQuantity,
            record.TimeToRecoveryMinutes,
            record.TimeToFirstActionMinutes,
            record.EvidenceStrength,
            record.LearningDisposition,
            record.VerifiedAtUtc);

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int? Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return values.Count % 2 == 1
            ? values[values.Count / 2]
            : (values[(values.Count - 1) / 2] + values[values.Count / 2]) / 2;
    }

    private static string ResolveWorkerName(string workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.ProductionRecovery => "Üretim Kurtarma",
        DigitalWorkforceWorkerCodes.MaintenanceResolution => "Bakım Çözüm",
        DigitalWorkforceWorkerCodes.MaterialShortage => "Malzeme Açığı",
        DigitalWorkforceWorkerCodes.QualityNcr => "Kalite NCR",
        DigitalWorkforceWorkerCodes.MoldLifecycle => "Kalıp Yaşam Döngüsü",
        DigitalWorkforceWorkerCodes.SupplierRecovery => "Tedarik & Teslimat",
        _ => workerCode,
    };
}
