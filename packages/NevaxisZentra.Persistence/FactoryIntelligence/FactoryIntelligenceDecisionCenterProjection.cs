using NevaxisZentra.Persistence.FactoryDecisionCenter;

namespace NevaxisZentra.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceDecisionCenterProjection
{
    public static FactoryDecisionEarlyWarningQueues MapQueues(IReadOnlyList<FactoryIntelligenceRiskCard> cards)
    {
        var mapped = cards.Select(MapCard).ToList();
        return new FactoryDecisionEarlyWarningQueues(
            mapped.Where(c => c.RiskState == FactoryIntelligenceRiskStates.Critical).ToList(),
            mapped.Where(c =>
                c.RiskType is FactoryIntelligenceRiskTypes.CriticalOrderDelivery
                    or FactoryIntelligenceRiskTypes.CompoundDelivery).ToList(),
            mapped.Where(c => c.RiskType == FactoryIntelligenceRiskTypes.MachineDegradation).ToList(),
            mapped.Where(c => c.RiskType == FactoryIntelligenceRiskTypes.MaterialShortage).ToList(),
            mapped.Where(c => c.RiskType == FactoryIntelligenceRiskTypes.QualityDeterioration).ToList());
    }

    private static FactoryDecisionEarlyWarningCard MapCard(FactoryIntelligenceRiskCard card) =>
        new(
            card.RiskId,
            card.LinkedCaseId,
            card.RiskType,
            card.RiskState,
            card.MachineCode,
            card.OrderReference,
            card.ObservedSummary,
            card.PredictedSummary,
            card.TimeToImpactLabel,
            card.ConfidenceBand,
            card.Horizon,
            card.Evidence,
            card.RecommendedActions,
            card.DataQuality,
            card.GeneratedAtUtc);
}
