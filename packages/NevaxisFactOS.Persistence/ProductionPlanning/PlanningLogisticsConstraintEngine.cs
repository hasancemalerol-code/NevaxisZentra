namespace NevaxisFactOS.Persistence.ProductionPlanning;

public static class PlanningLogisticsConstraintEngine
{
    public sealed record EvaluateInput(
        ProductionPlan Plan,
        PlanningLogisticsSourceProjection Source,
        PlanningLogisticsBufferPolicy BufferPolicy,
        PlanningMaterialInboundContext MaterialContext,
        DateTimeOffset EvaluatedAtUtc);

    public static PlanningLogisticsEvaluationReadModel Evaluate(EvaluateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Plan);
        ArgumentNullException.ThrowIfNull(input.Source);
        ArgumentNullException.ThrowIfNull(input.BufferPolicy);
        ArgumentNullException.ThrowIfNull(input.MaterialContext);

        if (!input.Source.Linked)
        {
            return BuildUnlinkedEvaluation(input);
        }

        var totalBufferMinutes =
            input.BufferPolicy.PackingBufferMinutes
            + input.BufferPolicy.QualityReleaseBufferMinutes
            + input.BufferPolicy.ShipmentPreparationBufferMinutes;

        var pickupUtc = input.Source.PlannedPickupAtUtc;
        var departureUtc = input.Source.PlannedDepartureAtUtc;
        var plannedCompletionUtc = input.Plan.PlannedEndAtUtc;

        DateTimeOffset? latestProductionCompletionUtc = null;
        if (pickupUtc.HasValue)
        {
            latestProductionCompletionUtc = pickupUtc.Value.AddMinutes(-totalBufferMinutes);
        }

        var slackMinutes = latestProductionCompletionUtc.HasValue
            ? (int)Math.Floor((latestProductionCompletionUtc.Value - plannedCompletionUtc).TotalMinutes)
            : 0;

        var blockingReasons = new List<string>();
        var constraints = new List<PlanningLogisticsConstraintReadModel>();

        if (pickupUtc.HasValue && latestProductionCompletionUtc.HasValue)
        {
            var pickupBlocking = plannedCompletionUtc > pickupUtc.Value;
            if (pickupBlocking)
            {
                blockingReasons.Add("Planlanan üretim bitişi sevkiyat pickup tarihinden sonra.");
            }

            if (plannedCompletionUtc > latestProductionCompletionUtc.Value)
            {
                blockingReasons.Add("Planlanan üretim bitişi lojistik tamponu sonrası izin verilen son tamamlanma tarihini aşıyor.");
            }

            constraints.Add(BuildConstraint(
                PlanningLogisticsConstraintType.ShipmentPickup,
                pickupUtc.Value,
                latestProductionCompletionUtc.Value,
                totalBufferMinutes,
                slackMinutes,
                pickupBlocking || slackMinutes < 0,
                pickupBlocking
                    ? "Pickup, planlanan üretim bitişinden önce."
                    : "Pickup tamponu uygulandı.",
                input.Source,
                "SHP-PICKUP"));

            constraints.Add(BuildConstraint(
                PlanningLogisticsConstraintType.PackingReady,
                pickupUtc.Value.AddMinutes(-input.BufferPolicy.PackingBufferMinutes),
                latestProductionCompletionUtc.Value.AddMinutes(input.BufferPolicy.QualityReleaseBufferMinutes + input.BufferPolicy.ShipmentPreparationBufferMinutes),
                input.BufferPolicy.PackingBufferMinutes,
                slackMinutes,
                !input.Source.PackingReady && IsPickupApproaching(pickupUtc.Value, input.EvaluatedAtUtc),
                input.Source.PackingReady
                    ? "Paketleme hazır."
                    : "Paketleme henüz sevkiyata hazır değil.",
                input.Source,
                "PACK-READY"));

            constraints.Add(BuildConstraint(
                PlanningLogisticsConstraintType.QualityRelease,
                pickupUtc.Value.AddMinutes(-input.BufferPolicy.QualityReleaseBufferMinutes - input.BufferPolicy.ShipmentPreparationBufferMinutes),
                latestProductionCompletionUtc.Value.AddMinutes(input.BufferPolicy.ShipmentPreparationBufferMinutes),
                input.BufferPolicy.QualityReleaseBufferMinutes,
                slackMinutes,
                slackMinutes < 0,
                "Kalite serbest bırakma tamponu uygulandı.",
                input.Source,
                "QA-RELEASE"));
        }

        if (departureUtc.HasValue)
        {
            constraints.Add(BuildConstraint(
                PlanningLogisticsConstraintType.ShipmentDeparture,
                departureUtc.Value,
                latestProductionCompletionUtc ?? plannedCompletionUtc,
                input.BufferPolicy.ShipmentPreparationBufferMinutes,
                slackMinutes,
                departureUtc.Value < plannedCompletionUtc,
                "Fabrika açısından kalkış cutoff değerlendirildi.",
                input.Source,
                "SHP-DEPARTURE"));
        }

        if (input.MaterialContext.Available && input.MaterialContext.MaterialEtaUtc.HasValue && latestProductionCompletionUtc.HasValue)
        {
            var materialBlocking = input.MaterialContext.MaterialEtaUtc.Value > latestProductionCompletionUtc.Value;
            if (materialBlocking)
            {
                blockingReasons.Add("Malzeme ETA izin verilen en geç üretim başlangıcından sonra.");
            }

            constraints.Add(BuildConstraint(
                PlanningLogisticsConstraintType.MaterialInbound,
                input.MaterialContext.MaterialEtaUtc.Value,
                latestProductionCompletionUtc.Value,
                null,
                slackMinutes,
                materialBlocking,
                "Malzeme ETA canonical kaynaktan okundu.",
                input.Source,
                "MAT-INBOUND"));
        }

        if (input.Source.ExportHandoffBlocked)
        {
            blockingReasons.Add(
                input.Source.NormalizedBlockingReason ?? "export-readiness-blocked");
        }

        var blocking = blockingReasons.Count > 0;
        var riskLevel = PlanningLogisticsRiskPolicy.MapSlackToRisk(slackMinutes, blocking);

        return new PlanningLogisticsEvaluationReadModel(
            EvaluationId: Guid.NewGuid(),
            ProductionPlanId: input.Plan.Id,
            ExportCaseId: input.Source.ExportCaseId,
            ExportCaseNumber: input.Source.ExportCaseNumber,
            ShipmentId: input.Source.ShipmentId,
            ShipmentReference: input.Source.ShipmentReference,
            SourceOrderReference: input.Source.SourceOrderReference,
            PickupDeadlineUtc: pickupUtc,
            DepartureDeadlineUtc: departureUtc,
            LatestProductionCompletionUtc: latestProductionCompletionUtc ?? plannedCompletionUtc,
            PlannedProductionCompletionUtc: plannedCompletionUtc,
            SlackMinutes: slackMinutes,
            RiskLevel: riskLevel,
            Blocking: blocking,
            BlockingReason: blockingReasons.Count > 0 ? string.Join(" ", blockingReasons) : null,
            SourceVersion: input.Source.SourceVersion,
            EvaluatedAtUtc: input.EvaluatedAtUtc,
            Version: 1,
            Constraints: constraints,
            LinkageStatus: "linked");
    }

    private static PlanningLogisticsEvaluationReadModel BuildUnlinkedEvaluation(EvaluateInput input)
    {
        return new PlanningLogisticsEvaluationReadModel(
            EvaluationId: Guid.NewGuid(),
            ProductionPlanId: input.Plan.Id,
            ExportCaseId: null,
            ExportCaseNumber: null,
            ShipmentId: null,
            ShipmentReference: null,
            SourceOrderReference: null,
            PickupDeadlineUtc: null,
            DepartureDeadlineUtc: null,
            LatestProductionCompletionUtc: input.Plan.PlannedEndAtUtc,
            PlannedProductionCompletionUtc: input.Plan.PlannedEndAtUtc,
            SlackMinutes: 0,
            RiskLevel: PlanningLogisticsRiskLevel.None,
            Blocking: false,
            BlockingReason: null,
            SourceVersion: 0,
            EvaluatedAtUtc: input.EvaluatedAtUtc,
            Version: 1,
            Constraints: Array.Empty<PlanningLogisticsConstraintReadModel>(),
            LinkageStatus: input.Source.UnavailableReason ?? "not-linked");
    }

    private static bool IsPickupApproaching(
        DateTimeOffset pickupUtc,
        DateTimeOffset evaluatedAtUtc) =>
        (pickupUtc - evaluatedAtUtc).TotalHours <= 24;

    private static PlanningLogisticsConstraintReadModel BuildConstraint(
        PlanningLogisticsConstraintType constraintType,
        DateTimeOffset requiredByUtc,
        DateTimeOffset calculatedLatestCompletionUtc,
        int? bufferMinutes,
        int slackMinutes,
        bool blocking,
        string reason,
        PlanningLogisticsSourceProjection source,
        string sourceReference) =>
        new(
            Id: Guid.NewGuid(),
            ConstraintType: constraintType,
            Status: ResolveConstraintStatus(slackMinutes, blocking),
            Severity: PlanningLogisticsRiskPolicy.MapSlackToRisk(slackMinutes, blocking),
            RequiredByUtc: requiredByUtc,
            CalculatedLatestStartUtc: null,
            CalculatedLatestCompletionUtc: calculatedLatestCompletionUtc,
            BufferMinutes: bufferMinutes,
            SlackMinutes: slackMinutes,
            Blocking: blocking,
            RiskLevel: PlanningLogisticsRiskPolicy.MapSlackToRisk(slackMinutes, blocking),
            Reason: reason,
            SourceReference: sourceReference,
            SourceType: PlanningLogisticsSourceType.ExportShipment,
            SourceRecordId: source.ShipmentId,
            SourceExportCaseId: source.ExportCaseId,
            SourceShipmentId: source.ShipmentId);

    private static PlanningLogisticsConstraintStatus ResolveConstraintStatus(int slackMinutes, bool blocking)
    {
        if (blocking && slackMinutes < 0)
        {
            return PlanningLogisticsConstraintStatus.Breached;
        }

        if (blocking || slackMinutes <= PlanningLogisticsRiskPolicy.HighSlackThresholdMinutes)
        {
            return PlanningLogisticsConstraintStatus.AtRisk;
        }

        return PlanningLogisticsConstraintStatus.Active;
    }
}
