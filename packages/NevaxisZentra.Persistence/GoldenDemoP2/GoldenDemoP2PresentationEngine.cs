using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.GoldenDemoP2;

public static class GoldenDemoP2PresentationEngine
{
    private static readonly IReadOnlyList<(string Code, string Label)> Steps =
    [
        ("shift-target-at-risk", "Vardiya hedefi risk altında"),
        ("machine-intelligence", "Makine zekası"),
        ("cycle-loss", "Cycle loss ayrıştırma"),
        ("recovery-opportunity", "Üretim kurtarma fırsatı"),
        ("digital-factory-team", "Dijital fabrika ekibi"),
        ("recovery-plan", "Kurtarma planı"),
        ("approval", "Onay"),
        ("intervention", "Teknik müdahale"),
        ("validation", "Doğrulama koşusu"),
        ("before-after", "Önce / sonra"),
        ("verified-outcome", "Doğrulanmış sonuç"),
        ("business-impact", "İş etkisi"),
    ];

    public static GoldenDemoPresentationStatus Build(
        GoldenDemoProductionEvent evt,
        int productionRecoveryGoodParts)
    {
        var (stepNumber, stepCode, stepLabel, isComplete) = MapEventStatus(evt.Status);
        return new GoldenDemoPresentationStatus(
            evt.Id,
            evt.MachineCode,
            evt.Status,
            stepCode,
            stepLabel,
            stepNumber,
            Steps.Count,
            isComplete,
            evt.Status != GoldenDemoP1EventStatuses.Active,
            productionRecoveryGoodParts);
    }

    private static (int StepNumber, string Code, string Label, bool IsComplete) MapEventStatus(string status) =>
        status switch
        {
            GoldenDemoP1EventStatuses.Active => (4, Steps[3].Code, Steps[3].Label, false),
            GoldenDemoP1EventStatuses.InterventionInProgress => (8, Steps[7].Code, Steps[7].Label, false),
            GoldenDemoP1EventStatuses.ValidationRun => (9, Steps[8].Code, Steps[8].Label, false),
            GoldenDemoP1EventStatuses.Verified => (12, Steps[11].Code, Steps[11].Label, true),
            _ => (1, Steps[0].Code, Steps[0].Label, false),
        };
}
