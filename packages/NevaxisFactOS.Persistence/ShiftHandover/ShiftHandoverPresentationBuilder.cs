using NevaxisFactOS.Persistence.AiTasks;

namespace NevaxisFactOS.Persistence.ShiftHandover;

public static class ShiftHandoverPresentationBuilder
{
    public static string BuildDeepLink(Guid handoverId) =>
        $"/vardiya-devir/{handoverId:D}";

    public static string BuildTaskDraftIdempotencyKey(Guid handoverId, Guid itemId) =>
        $"shift-handover-follow-up:{handoverId:D}:{itemId:D}";

    public static bool CanCreateFollowUpTask(FactoryShiftHandover handover, ShiftHandoverItem item) =>
        item.RequiresFollowUp
        && !item.LinkedAiTaskId.HasValue
        && item.Status == ShiftHandoverItemStatuses.Open
        && handover.Status is ShiftHandoverStatuses.HandedOver
            or ShiftHandoverStatuses.Acknowledged
            or ShiftHandoverStatuses.Completed;

    public static string BuildTaskTitle(FactoryShiftHandover handover, ShiftHandoverItem item) =>
        $"Vardiya devri takibi ÔÇö {item.Title}";

    public static string BuildTaskSummary(FactoryShiftHandover handover, ShiftHandoverItem item) =>
        $"Devir: {handover.HandoverReference}. Vardiya: {handover.ShiftDate:yyyy-MM-dd} {handover.ShiftPeriod}. " +
        $"Kategori: {item.Category}. {item.Description}";

    public static string ResolveTaskPriority(string severity) =>
        severity switch
        {
            ShiftHandoverItemSeverities.Critical => AiTaskPriorities.Critical,
            ShiftHandoverItemSeverities.High => AiTaskPriorities.High,
            ShiftHandoverItemSeverities.Low => AiTaskPriorities.Low,
            _ => AiTaskPriorities.Medium,
        };

    public static CreateSourceLinkedAiTaskCommand BuildFollowUpTaskCommand(
        FactoryShiftHandover handover,
        ShiftHandoverItem item)
    {
        var priority = ResolveTaskPriority(item.Severity);
        var deepLink = BuildDeepLink(handover.Id);

        return new CreateSourceLinkedAiTaskCommand(
            Title: BuildTaskTitle(handover, item),
            Summary: BuildTaskSummary(handover, item),
            Category: MapCategoryToAiTask(item.Category),
            Priority: priority,
            RiskLevel: priority,
            DetectedIssue: item.Description,
            Reasoning: $"Vardiya devir teslimi {handover.Id:D} maddesi i├ğin insan incelemeli takip g├Ârevi.",
            Recommendation: new AiTaskRecommendationModel(
                "Vardiya devri takibi",
                "Yetkili ekip vardiya devri maddesini incelemeli ve gerekli operasyonel aksiyonu planlamal─▒d─▒r.",
                AiTaskActionTypes.Review,
                RequiresApproval: true,
                Reversible: true,
                TargetSystem: "crevafactos"),
            Confidence: new AiTaskConfidenceModel(
                0.65m,
                $"Madde ├Ânceli─şi: {item.Severity}. Kategori: {item.Category}.",
                null),
                Impact: new AiTaskImpactModel(
                null, null, null, null, null, null, null,
                $"Vardiya devri maddesi: {item.Title}. ├ûncelik: {item.Severity}. Kategori: {item.Category}."),
            Sources:
            [
                new AiTaskSourceReferenceModel(
                    ShiftHandoverConstants.SourceModule,
                    handover.Id.ToString(),
                    $"Devir {handover.HandoverReference}",
                    handover.HandedOverAtUtc ?? handover.UpdatedAtUtc,
                    deepLink),
                new AiTaskSourceReferenceModel(
                    ShiftHandoverConstants.SourceModule,
                    item.Id.ToString(),
                    item.Title,
                    item.UpdatedAtUtc,
                    deepLink),
            ],
            SourceModule: ShiftHandoverConstants.SourceModule,
            SourceEntityId: $"{handover.Id:D}:{item.Id:D}",
            IdempotencyKey: BuildTaskDraftIdempotencyKey(handover.Id, item.Id),
            SiteId: null,
            ProductionLineId: null,
            MachineId: null,
            MoldId: null,
            ProductionOrderId: null,
            SalesOrderId: null,
            MaterialId: null,
            SupplierId: null,
            CustomerId: null,
            DueAtUtc: null);
    }

    private static string MapCategoryToAiTask(string category) =>
        category switch
        {
            ShiftHandoverItemCategories.Quality => AiTaskCategories.Quality,
            ShiftHandoverItemCategories.Maintenance => AiTaskCategories.Maintenance,
            ShiftHandoverItemCategories.Production => AiTaskCategories.LiveProduction,
            ShiftHandoverItemCategories.Material => AiTaskCategories.MaterialRisk,
            ShiftHandoverItemCategories.Order => AiTaskCategories.DeliveryRisk,
            ShiftHandoverItemCategories.Machine => AiTaskCategories.Maintenance,
            ShiftHandoverItemCategories.Safety => AiTaskCategories.LiveProduction,
            _ => AiTaskCategories.LiveProduction,
        };
}
