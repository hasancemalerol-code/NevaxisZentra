using CrevaFactOS.Persistence.Maintenance;

namespace CrevaFactOS.Api.Maintenance;

internal static class MaintenanceApiMapper
{
    public static object MapRequestSummary(MaintenanceRequestSummaryReadModel model) => new
    {
        id = model.Id,
        requestNumber = model.RequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        title = model.Title,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        reportedBySubjectId = model.ReportedBySubjectId,
        reportedAtUtc = model.ReportedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapRequestDetail(MaintenanceRequestDetailReadModel model) => new
    {
        id = model.Id,
        requestNumber = model.RequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productionOrderStatus = model.ProductionOrderStatus,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        title = model.Title,
        description = model.Description,
        source = model.Source,
        reportedBySubjectId = model.ReportedBySubjectId,
        reportedAtUtc = model.ReportedAtUtc,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        activity = model.Activity.Select(MapActivity).ToList(),
    };

    public static object MapWorkOrderSummary(MaintenanceWorkOrderSummaryReadModel model) => new
    {
        id = model.Id,
        workOrderNumber = model.WorkOrderNumber,
        maintenanceRequestId = model.MaintenanceRequestId,
        maintenanceRequestNumber = model.MaintenanceRequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        assignedTechnicianSubjectId = model.AssignedTechnicianSubjectId,
        plannedStartUtc = model.PlannedStartUtc,
        plannedEndUtc = model.PlannedEndUtc,
        startedAtUtc = model.StartedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapWorkOrderDetail(MaintenanceWorkOrderDetailReadModel model) => new
    {
        id = model.Id,
        workOrderNumber = model.WorkOrderNumber,
        maintenanceRequestId = model.MaintenanceRequestId,
        maintenanceRequestNumber = model.MaintenanceRequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productionOrderStatus = model.ProductionOrderStatus,
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventiveDueId = model.PreventiveDueId,
        externalServiceCost = model.ExternalServiceCost,
        otherCost = model.OtherCost,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        title = model.Title,
        description = model.Description,
        assignedTechnicianSubjectId = model.AssignedTechnicianSubjectId,
        plannedStartUtc = model.PlannedStartUtc,
        plannedEndUtc = model.PlannedEndUtc,
        startedAtUtc = model.StartedAtUtc,
        completedAtUtc = model.CompletedAtUtc,
        completionNote = model.CompletionNote,
        resolutionCode = model.ResolutionCode,
        createdBySubjectId = model.CreatedBySubjectId,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        activity = model.Activity.Select(MapActivity).ToList(),
    };

    public static object MapPreventivePlanSummary(PreventiveMaintenancePlanSummaryReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        intervalType = model.IntervalType,
        intervalValue = model.IntervalValue,
        nextDueAtUtc = model.NextDueAtUtc,
        status = model.Status,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapPreventivePlanDetail(PreventiveMaintenancePlanDetailReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        intervalType = model.IntervalType,
        intervalValue = model.IntervalValue,
        nextDueAtUtc = model.NextDueAtUtc,
        status = model.Status,
        description = model.Description,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapWorkspaceSummary(MaintenanceWorkspaceSummaryReadModel model) => new
    {
        openRequestCount = model.OpenRequestCount,
        openWorkOrderCount = model.OpenWorkOrderCount,
        criticalWorkOrderCount = model.CriticalWorkOrderCount,
        inProgressCount = model.InProgressCount,
        waitingCount = model.WaitingCount,
        completedTodayCount = model.CompletedTodayCount,
        overduePreventiveCount = model.OverduePreventiveCount,
    };

    private static object MapActivity(MaintenanceActivityReadModel model) => new
    {
        action = model.Action,
        eventType = model.EventType,
        occurredAtUtc = model.OccurredAtUtc,
        actorSubjectId = model.ActorSubjectId,
        summary = model.Summary,
    };

    public static object MapWorkNote(MaintenanceWorkNoteReadModel model) => new
    {
        id = model.Id,
        workOrderId = model.WorkOrderId,
        noteType = model.NoteType,
        text = model.Text,
        isImportant = model.IsImportant,
        createdAtUtc = model.CreatedAtUtc,
        createdBySubjectId = model.CreatedBySubjectId,
    };

    public static object MapTimelineEntry(MaintenanceTimelineEntryReadModel model) => new
    {
        entryType = model.EntryType,
        title = model.Title,
        detail = model.Detail,
        occurredAtUtc = model.OccurredAtUtc,
        actorSubjectId = model.ActorSubjectId,
    };

    public static object MapPartUsage(MaintenancePartUsageReadModel model) => new
    {
        id = model.Id,
        workOrderId = model.WorkOrderId,
        partReference = model.PartReference,
        partName = model.PartName,
        quantity = model.Quantity,
        unit = model.Unit,
        sourceSystem = model.SourceSystem,
        sourceReference = model.SourceReference,
        estimatedUnitCost = model.EstimatedUnitCost,
        actualUnitCost = model.ActualUnitCost,
        estimatedLineCost = model.EstimatedLineCost,
        actualLineCost = model.ActualLineCost,
        recordedAtUtc = model.RecordedAtUtc,
        recordedBySubjectId = model.RecordedBySubjectId,
    };

    public static object MapCostSummary(MaintenanceCostSummaryReadModel model) => new
    {
        workOrderId = model.WorkOrderId,
        estimatedPartsCost = model.EstimatedPartsCost,
        actualPartsCost = model.ActualPartsCost,
        externalServiceCost = model.ExternalServiceCost,
        otherCost = model.OtherCost,
        totalMaintenanceCost = model.TotalMaintenanceCost,
    };

    public static object MapPreventiveDueSummary(PreventiveMaintenanceDueSummaryReadModel model) => new
    {
        id = model.Id,
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventivePlanName = model.PreventivePlanName,
        assetType = model.AssetType,
        assetReference = model.AssetReference,
        dueAtUtc = model.DueAtUtc,
        status = model.Status,
        generatedAtUtc = model.GeneratedAtUtc,
        workOrderId = model.WorkOrderId,
        workOrderNumber = model.WorkOrderNumber,
        completedAtUtc = model.CompletedAtUtc,
        version = model.Version,
    };

    public static object MapPreventiveDueDetail(PreventiveMaintenanceDueDetailReadModel model) => new
    {
        id = model.Id,
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventivePlanName = model.PreventivePlanName,
        assetType = model.AssetType,
        assetReference = model.AssetReference,
        dueAtUtc = model.DueAtUtc,
        status = model.Status,
        generatedAtUtc = model.GeneratedAtUtc,
        workOrderId = model.WorkOrderId,
        workOrderNumber = model.WorkOrderNumber,
        completedAtUtc = model.CompletedAtUtc,
        version = model.Version,
    };

    public static object MapAssetHistory(MaintenanceAssetHistoryReadModel model) => new
    {
        assetReference = model.AssetReference,
        requests = model.Requests.Select(MapRequestSummary).ToList(),
        workOrders = model.WorkOrders.Select(MapWorkOrderSummary).ToList(),
        preventivePlans = model.PreventivePlans.Select(MapPreventivePlanSummary).ToList(),
        preventiveDues = model.PreventiveDues.Select(MapPreventiveDueSummary).ToList(),
        workNotes = model.WorkNotes.Select(MapWorkNote).ToList(),
        partUsages = model.PartUsages.Select(MapPartUsage).ToList(),
        costSummaries = model.CostSummaries.Select(MapCostSummary).ToList(),
    };

    public static object MapReliabilityOverview(MaintenanceReliabilityOverviewReadModel model) => new
    {
        totalAssetsWithMaintenanceHistory = model.TotalAssetsWithMaintenanceHistory,
        openRequests = model.OpenRequests,
        openWorkOrders = model.OpenWorkOrders,
        criticalOpenWorkOrders = model.CriticalOpenWorkOrders,
        overduePreventiveCount = model.OverduePreventiveCount,
        completedWorkOrdersPeriod = model.CompletedWorkOrdersPeriod,
        faultCountPeriod = model.FaultCountPeriod,
        repeatFaultCountPeriod = model.RepeatFaultCountPeriod,
        averageRepairDurationMinutes = model.AverageRepairDurationMinutes,
        medianRepairDurationMinutes = model.MedianRepairDurationMinutes,
        preventiveCompletionRate = model.PreventiveCompletionRate,
        assetsAtRiskCount = model.AssetsAtRiskCount,
        period = new { dateFromUtc = model.Period.DateFromUtc, dateToUtc = model.Period.DateToUtc },
        mttr = MapMttr(model.Mttr),
        mtbf = MapMtbf(model.Mtbf),
        costAnalytics = model.CostAnalytics is null ? null : MapCostAnalytics(model.CostAnalytics),
    };

    public static object MapReliabilityReport(MaintenanceReliabilityReportReadModel model) => new
    {
        period = new { dateFromUtc = model.Period.DateFromUtc, dateToUtc = model.Period.DateToUtc },
        mttr = MapMttr(model.Mttr),
        mtbf = MapMtbf(model.Mtbf),
        repeatFailures = model.RepeatFailures.Select(MapRepeatFailure).ToList(),
        preventiveCompliance = MapPreventiveCompliance(model.PreventiveCompliance),
        backlogAging = MapBacklogAging(model.BacklogAging),
        costAnalytics = model.CostAnalytics is null ? null : MapCostAnalytics(model.CostAnalytics),
    };

    public static object MapFailureBreakdownRow(MaintenanceFailureBreakdownRowReadModel model) => new
    {
        groupKey = model.GroupKey,
        groupLabel = model.GroupLabel,
        count = model.Count,
        percentage = model.Percentage,
        repeatCount = model.RepeatCount,
        averageRepairMinutes = model.AverageRepairMinutes,
    };

    public static object MapAssetReliability(MaintenanceAssetReliabilityReadModel model) => new
    {
        assetReference = model.AssetReference,
        assetType = model.AssetType,
        assetName = model.AssetName,
        totalFaults = model.TotalFaults,
        faultsLast30Days = model.FaultsLast30Days,
        faultsLast90Days = model.FaultsLast90Days,
        repeatFaultCount = model.RepeatFaultCount,
        completedCorrectiveWorkOrders = model.CompletedCorrectiveWorkOrders,
        meanRepairMinutes = model.MeanRepairMinutes,
        openWorkOrders = model.OpenWorkOrders,
        criticalOpenWorkOrders = model.CriticalOpenWorkOrders,
        overduePreventiveCount = model.OverduePreventiveCount,
        preventiveComplianceRate = model.PreventiveComplianceRate,
        lastFailureAtUtc = model.LastFailureAtUtc,
        lastMaintenanceCompletedAtUtc = model.LastMaintenanceCompletedAtUtc,
        conditionStatus = model.ConditionStatus,
        conditionReasons = model.ConditionReasons,
        mtbf = MapMtbf(model.Mtbf),
        costAnalytics = model.CostAnalytics is null ? null : MapCostAnalytics(model.CostAnalytics),
    };

    public static object MapConditionSignal(MaintenanceConditionSignalReadModel model) => new
    {
        signalId = model.SignalId,
        tenantId = model.TenantId,
        facilityId = model.FacilityId,
        assetType = model.AssetType,
        assetReference = model.AssetReference,
        signalType = model.SignalType,
        severity = model.Severity,
        observedAtUtc = model.ObservedAtUtc,
        sourceType = model.SourceType,
        sourceReference = model.SourceReference,
        value = model.Value,
        unit = model.Unit,
        summary = model.Summary,
        isVerified = model.IsVerified,
        expiresAtUtc = model.ExpiresAtUtc,
    };

    public static object MapAssetCondition(MaintenanceAssetConditionReadModel model) => new
    {
        assetReference = model.AssetReference,
        conditionStatus = model.ConditionStatus,
        reasons = model.Reasons,
        signals = model.Signals.Select(MapConditionSignal).ToList(),
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapRiskProjection(MaintenanceAssetRiskProjectionReadModel model) => new
    {
        assetReference = model.AssetReference,
        conditionStatus = model.ConditionStatus,
        blocking = model.Blocking,
        primaryReason = model.PrimaryReason,
        repeatFailureCount = model.RepeatFailureCount,
        overduePreventiveCount = model.OverduePreventiveCount,
        criticalOpenWorkOrderCount = model.CriticalOpenWorkOrderCount,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    private static object MapMttr(MaintenanceMttrReadModel model) => new
    {
        label = model.Label,
        eligibleRepairCount = model.EligibleRepairCount,
        excludedRepairCount = model.ExcludedRepairCount,
        totalRepairMinutes = model.TotalRepairMinutes,
        meanRepairMinutes = model.MeanRepairMinutes,
        medianRepairMinutes = model.MedianRepairMinutes,
        dataCompletenessStatus = model.DataCompletenessStatus,
    };

    private static object MapMtbf(MaintenanceMtbfReadModel model) => new
    {
        mtbfAvailable = model.MtbfAvailable,
        mtbfHours = model.MtbfHours,
        reason = model.Reason,
    };

    private static object MapPreventiveCompliance(MaintenancePreventiveComplianceReadModel model) => new
    {
        dueCount = model.DueCount,
        completedOnTimeCount = model.CompletedOnTimeCount,
        completedLateCount = model.CompletedLateCount,
        overdueOpenCount = model.OverdueOpenCount,
        skippedCount = model.SkippedCount,
        preventiveCompletionRate = model.PreventiveCompletionRate,
        onTimeCompletionRate = model.OnTimeCompletionRate,
        ruleDescription = model.RuleDescription,
    };

    private static object MapBacklogAging(MaintenanceBacklogAgingReadModel model) => new
    {
        openRequests = MapAgingBuckets(model.OpenRequests),
        openWorkOrders = MapAgingBuckets(model.OpenWorkOrders),
        criticalOpenWorkOrders = MapAgingBuckets(model.CriticalOpenWorkOrders),
        waitingWorkOrders = MapAgingBuckets(model.WaitingWorkOrders),
    };

    private static object MapAgingBuckets(MaintenanceAgingBucketsReadModel model) => new
    {
        day0To1 = model.Day0To1,
        day2To3 = model.Day2To3,
        day4To7 = model.Day4To7,
        day8To14 = model.Day8To14,
        day15Plus = model.Day15Plus,
    };

    private static object MapRepeatFailure(MaintenanceRepeatFailureReadModel model) => new
    {
        assetReference = model.AssetReference,
        assetType = model.AssetType,
        assetName = model.AssetName,
        failureCategory = model.FailureCategory,
        failureSignature = model.FailureSignature,
        failureCount = model.FailureCount,
        repeatCount = model.RepeatCount,
        failuresLast30Days = model.FailuresLast30Days,
        failuresLast90Days = model.FailuresLast90Days,
        lastOccurrenceUtc = model.LastOccurrenceUtc,
        daysSinceLastFailure = model.DaysSinceLastFailure,
        openRelatedWorkOrderCount = model.OpenRelatedWorkOrderCount,
        conditionStatus = model.ConditionStatus,
    };

    private static object MapCostAnalytics(MaintenanceCostAnalyticsReadModel model) => new
    {
        totalMaintenanceCost = model.TotalMaintenanceCost,
        averageCostPerCompletedWorkOrder = model.AverageCostPerCompletedWorkOrder,
        topCostAssets = model.TopCostAssets.Select(a => new
        {
            assetReference = a.AssetReference,
            assetType = a.AssetType,
            totalCost = a.TotalCost,
        }).ToList(),
        recordsWithCost = model.RecordsWithCost,
        recordsWithoutCost = model.RecordsWithoutCost,
        dataCompletenessStatus = model.DataCompletenessStatus,
    };

    public static object MapConditionOverview(MaintenanceConditionOverviewReadModel model) => new
    {
        monitoredAssetCount = model.MonitoredAssetCount,
        normalCount = model.NormalCount,
        watchCount = model.WatchCount,
        degradedCount = model.DegradedCount,
        criticalCount = model.CriticalCount,
        insufficientDataCount = model.InsufficientDataCount,
        lastEvaluatedAtUtc = model.LastEvaluatedAtUtc,
    };

    public static object MapConditionAssetRow(MaintenanceConditionAssetRowReadModel model) => new
    {
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        machineCode = model.MachineCode,
        machineName = model.MachineName,
        machineId = model.MachineId,
        conditionStatus = model.ConditionStatus,
        activeAnomalyCount = model.ActiveAnomalyCount,
        highestAnomalySeverity = model.HighestAnomalySeverity,
        dataFreshness = model.DataFreshness,
        dataSufficiency = model.DataSufficiency,
        lastEvaluatedAtUtc = model.LastEvaluatedAtUtc,
    };

    public static object MapConditionProfile(MaintenanceConditionProfileReadModel model) => new
    {
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        machineId = model.MachineId,
        machineCode = model.MachineCode,
        machineName = model.MachineName,
        conditionStatus = model.ConditionStatus,
        overallConditionScore = model.OverallConditionScore,
        lastEvaluatedAtUtc = model.LastEvaluatedAtUtc,
        dataFreshness = model.DataFreshness,
        dataSufficiency = model.DataSufficiency,
        activeAnomalyCount = model.ActiveAnomalyCount,
        highestAnomalySeverity = model.HighestAnomalySeverity,
        trendState = model.TrendState,
        signals = model.Signals.Select(MapConditionIntelligenceSignal).ToList(),
        activeAnomalies = model.ActiveAnomalies.Select(MapConditionAnomaly).ToList(),
        context = new
        {
            alarmContext = new
            {
                recentAlarmCount24h = model.Context.AlarmContext.RecentAlarmCount24h,
                highestSeverity24h = model.Context.AlarmContext.HighestSeverity24h,
            },
            downtimeContext = new
            {
                episodeCount24h = model.Context.DowntimeContext.EpisodeCount24h,
                totalDurationMinutes24h = model.Context.DowntimeContext.TotalDurationMinutes24h,
            },
            maintenanceHistory = new
            {
                recentFailureCount90d = model.Context.MaintenanceHistory.RecentFailureCount90d,
                lastFailureCategory = model.Context.MaintenanceHistory.LastFailureCategory,
                lastMaintenanceCompletedAtUtc = model.Context.MaintenanceHistory.LastMaintenanceCompletedAtUtc,
            },
            metricSnapshots = model.Context.MetricSnapshots.Select(m => new
            {
                metricKey = m.MetricKey,
                displayName = m.DisplayName,
                expectedUnit = m.ExpectedUnit,
                dataSufficiency = m.DataSufficiency,
                dataFreshness = m.DataFreshness,
                trendState = m.TrendState,
                baselineValue = m.BaselineValue,
                observedValue = m.ObservedValue,
                deviationPercent = m.DeviationPercent,
            }).ToList(),
        },
    };

    public static object MapConditionAnomaly(MaintenanceConditionAnomalyReadModel model) => new
    {
        id = model.Id,
        metricKey = model.MetricKey,
        metricDisplayName = model.MetricDisplayName,
        detectedAtUtc = model.DetectedAtUtc,
        observedValue = model.ObservedValue,
        baselineValue = model.BaselineValue,
        deviationPercent = model.DeviationPercent,
        severity = model.Severity,
        direction = model.Direction,
        evidenceSummary = model.EvidenceSummary,
        sourceFreshness = model.SourceFreshness,
        dataQuality = model.DataQuality,
        status = model.Status,
    };

    public static object MapConditionIntelligenceSignal(MaintenancePredictiveConditionSignalReadModel model) => new
    {
        signalType = model.SignalType,
        metricKey = model.MetricKey,
        severity = model.Severity,
        summary = model.Summary,
    };

    public static object MapPredictiveRiskOverview(MaintenancePredictiveRiskOverviewReadModel model) => new
    {
        monitoredAssetCount = model.MonitoredAssetCount,
        insufficientDataCount = model.InsufficientDataCount,
        lowRiskCount = model.LowRiskCount,
        watchCount = model.WatchCount,
        highRiskCount = model.HighRiskCount,
        criticalRiskCount = model.CriticalRiskCount,
        lastAssessedAtUtc = model.LastAssessedAtUtc,
    };

    public static object MapPredictiveRiskAssetRow(MaintenancePredictiveRiskAssetRowReadModel model) => new
    {
        assessmentId = model.AssessmentId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        machineCode = model.MachineCode,
        machineName = model.MachineName,
        riskBand = model.RiskBand,
        reviewPriority = model.ReviewPriority,
        dataFreshness = model.DataFreshness,
        dataSufficiency = model.DataSufficiency,
        assessedAtUtc = model.AssessedAtUtc,
        followUpStatus = model.FollowUpStatus,
        linkedAiTaskId = model.LinkedAiTaskId,
    };

    public static object MapPredictiveRiskAssessment(MaintenancePredictiveRiskAssessmentReadModel model) => new
    {
        id = model.Id,
        machineId = model.MachineId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        machineCode = model.MachineCode,
        machineName = model.MachineName,
        riskBand = model.RiskBand,
        reviewPriority = model.ReviewPriority,
        ruleSetVersion = model.RuleSetVersion,
        conditionEvaluationIdentity = model.ConditionEvaluationIdentity,
        dataFreshness = model.DataFreshness,
        dataSufficiency = model.DataSufficiency,
        activeAnomalyCount = model.ActiveAnomalyCount,
        maintenanceHistorySummary = model.MaintenanceHistorySummary,
        explanation = model.Explanation,
        assessedAtUtc = model.AssessedAtUtc,
        followUpStatus = model.FollowUpStatus,
        linkedAiTaskId = model.LinkedAiTaskId,
        canCreateTaskDraft = MaintenancePredictiveRiskPresentationBuilder.CanCreateTaskDraft(model),
        riskDrivers = model.Drivers.Select(d => new
        {
            driverCode = d.DriverCode,
            summary = d.Summary,
        }).ToList(),
        evidence = model.Evidence.Select(e => new
        {
            evidenceType = e.EvidenceType,
            evidenceReference = e.EvidenceReference,
            summary = e.Summary,
        }).ToList(),
        recommendation = model.Recommendation is null
            ? null
            : MapPredictiveRecommendation(model.Recommendation),
    };

    public static object MapPredictiveRiskAssessmentSummary(MaintenancePredictiveRiskAssessmentSummaryReadModel model) => new
    {
        riskBand = model.RiskBand,
        reviewPriority = model.ReviewPriority,
        assessedAtUtc = model.AssessedAtUtc,
        ruleSetVersion = model.RuleSetVersion,
    };

    public static object MapPredictiveRecommendation(MaintenancePredictiveRecommendationReadModel model) => new
    {
        category = model.Category,
        reviewPriority = model.ReviewPriority,
        explanation = model.Explanation,
        ruleSetVersion = model.RuleSetVersion,
        generatedAtUtc = model.GeneratedAtUtc,
        status = model.Status,
    };

    public static object MapMaintenanceAiDraftSummary(MaintenanceAiDraftSummaryReadModel model) => new
    {
        id = model.Id,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        status = model.Status,
        qualitativeConfidence = model.QualitativeConfidence,
        suggestedTitle = model.SuggestedTitle,
        version = model.Version,
        generatedAtUtc = model.GeneratedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        createdMaintenanceRequestId = model.CreatedMaintenanceRequestId,
        isDevelopmentProvider = model.IsDevelopmentProvider,
    };

    public static object MapMaintenanceAiDraftDetail(MaintenanceAiDraftDetailReadModel model) => new
    {
        id = model.Id,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        status = model.Status,
        qualitativeConfidence = model.QualitativeConfidence,
        conditionEvaluationReference = model.ConditionEvaluationReference,
        riskAssessmentId = model.RiskAssessmentId,
        inputFingerprint = model.InputFingerprint,
        ruleSetVersion = model.RuleSetVersion,
        providerIdentifier = model.ProviderIdentifier,
        modelIdentifier = model.ModelIdentifier,
        promptVersion = model.PromptVersion,
        isDevelopmentProvider = model.IsDevelopmentProvider,
        dataFreshness = model.DataFreshness,
        dataSufficiency = model.DataSufficiency,
        riskBand = model.RiskBand,
        deterministicRecommendationCategory = model.DeterministicRecommendationCategory,
        validatedOutput = MapMaintenanceAiStructuredOutput(model.ValidatedOutput),
        aiOriginalOutput = MapMaintenanceAiStructuredOutput(model.AiOriginalOutput),
        suggestedTitle = model.SuggestedTitle,
        suggestedDescription = model.SuggestedDescription,
        suggestedPriority = model.SuggestedPriority,
        suggestedRequestType = model.SuggestedRequestType,
        humanEditedTitle = model.HumanEditedTitle,
        humanEditedDescription = model.HumanEditedDescription,
        humanEditedPriority = model.HumanEditedPriority,
        humanEditedRequestType = model.HumanEditedRequestType,
        humanEditorSubjectId = model.HumanEditorSubjectId,
        humanEditedAtUtc = model.HumanEditedAtUtc,
        generatedBySubjectId = model.GeneratedBySubjectId,
        generatedAtUtc = model.GeneratedAtUtc,
        approvedBySubjectId = model.ApprovedBySubjectId,
        approvedAtUtc = model.ApprovedAtUtc,
        rejectedBySubjectId = model.RejectedBySubjectId,
        rejectedAtUtc = model.RejectedAtUtc,
        rejectionReason = model.RejectionReason,
        createdMaintenanceRequestId = model.CreatedMaintenanceRequestId,
        createdMaintenanceRequestNumber = model.CreatedMaintenanceRequestNumber,
        failureCode = model.FailureCode,
        failureMessage = model.FailureMessage,
        version = model.Version,
        auditTrail = model.AuditTrail.Select(a => new
        {
            action = a.Action,
            actorSubjectId = a.ActorSubjectId,
            draftVersion = a.DraftVersion,
            resultCode = a.ResultCode,
            occurredAtUtc = a.OccurredAtUtc,
        }).ToList(),
    };

    private static object MapMaintenanceAiStructuredOutput(MaintenanceAiStructuredOutput output) => new
    {
        summary = output.Summary,
        observedCondition = output.ObservedCondition,
        suggestedTitle = output.SuggestedTitle,
        suggestedDescription = output.SuggestedDescription,
        suggestedPriority = output.SuggestedPriority,
        suggestedRequestType = output.SuggestedRequestType,
        recommendedReviewType = output.RecommendedReviewType,
        evidenceReferences = output.EvidenceReferences.Select(e => new
        {
            evidenceType = e.EvidenceType,
            evidenceReference = e.EvidenceReference,
            summary = e.Summary,
        }).ToList(),
        missingInformation = output.MissingInformation,
        uncertaintyExplanation = output.UncertaintyExplanation,
        qualitativeConfidence = output.QualitativeConfidence,
        safetyNotes = output.SafetyNotes,
        prohibitedActionConfirmation = output.ProhibitedActionConfirmation,
    };
}
