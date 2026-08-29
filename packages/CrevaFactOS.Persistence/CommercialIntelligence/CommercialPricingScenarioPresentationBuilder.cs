namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialPricingScenarioPresentationBuilder
{
    public static string BuildTaskDraftIdempotencyKey(Guid scenarioId) =>
        $"pricing-scenario-task:{scenarioId:D}:human-review-draft";

    public static string BuildScenarioTitle(CommercialPricingScenarioDetailReadModel scenario) =>
        $"Fiyat senaryosu {scenario.ScenarioReference}";

    public static string BuildScenarioSummary(CommercialPricingScenarioDetailReadModel scenario)
    {
        var guardrail = scenario.GuardrailResult ?? "değerlendirilmedi";
        return $"Önerilen birim fiyat {scenario.ProposedUnitPrice:0.00} TRY, koruma sonucu {guardrail}.";
    }

    public static string BuildScenarioDeepLink(Guid scenarioId) =>
        $"/ticari-zeka/fiyat-koruma-ve-onay?scenario={scenarioId:D}";
}
