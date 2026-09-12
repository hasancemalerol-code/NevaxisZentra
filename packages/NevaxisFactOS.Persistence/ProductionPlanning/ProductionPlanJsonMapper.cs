using System.Globalization;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.ProductionPlanning;

public static class ProductionPlanJsonMapper
{
    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.Ordinal)
    {
        "low", "normal", "high", "urgent",
    };

    private static readonly HashSet<string> AllowedRiskTypes = new(StringComparer.Ordinal)
    {
        "capacity-shortage", "machine-conflict", "mold-conflict", "material-shortage",
        "operator-shortage", "delivery-risk", "quality-risk", "maintenance-risk",
        "data-missing", "data-conflict",
    };

    private static readonly HashSet<string> AllowedRiskSeverities = new(StringComparer.Ordinal)
    {
        "information", "warning", "critical",
    };

    private static readonly HashSet<string> AllowedSourceTypes = new(StringComparer.Ordinal)
    {
        "order-draft", "erp-order", "customer-order", "manual-request",
    };

    private static readonly HashSet<string> AllowedHistoryTypes = new(StringComparer.Ordinal)
    {
        "created", "updated", "approval", "scheduling", "transfer",
    };

    private static readonly HashSet<string> AllowedApprovalStatuses = new(StringComparer.Ordinal)
    {
        "not-required", "pending", "approved", "rejected", "expired",
    };

    private static readonly HashSet<string> AllowedTransferStatuses = new(StringComparer.Ordinal)
    {
        "not-started", "pending", "in-progress", "completed", "failed", "uncertain",
    };

    private static readonly HashSet<string> AllowedTargetSystems = new(StringComparer.Ordinal)
    {
        "erp", "mes", "crevafactos",
    };

    public static ProductionPlanJsonMapResult<ProductionPlanDetailMappedReadModel> MapDetail(
        ProductionPlanDetailReadModel plan)
    {
        var operationalSummary = MapListOperationalSummary(
            plan.DemandsJson,
            plan.RisksJson,
            plan.ResourceAssignmentJson,
            plan.TimeWindowJson);

        if (!operationalSummary.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(operationalSummary.Failure!);
        }

        var listSummary = operationalSummary.Value!;
        var summary = new ProductionPlanSummaryReadModel(
            plan.Id,
            plan.ReferenceCode,
            plan.Title,
            plan.FacilityId,
            plan.Status,
            plan.Origin,
            plan.AiGenerated,
            plan.Confidence,
            plan.PlannedStartAtUtc,
            plan.PlannedEndAtUtc,
            plan.ApprovalStatus,
            plan.TransferStatus,
            plan.TransferTargetSystem,
            plan.UpdatedAtUtc,
            listSummary.DemandCount,
            listSummary.TotalRequiredQuantity,
            listSummary.TotalRequiredQuantityUnit,
            listSummary.PrimaryRequiredQuantity,
            listSummary.PrimaryUnit,
            listSummary.PrimaryProductCode,
            listSummary.PrimaryProductName,
            listSummary.HighestDemandPriority,
            listSummary.RiskCount,
            listSummary.CriticalRiskCount,
            listSummary.BlockingRiskCount,
            listSummary.HasBlockingRisk,
            listSummary.HasMachineAssignment,
            listSummary.HasMoldAssignment,
            listSummary.HasMaterialAssignment,
            listSummary.HasOperatorGroupAssignment,
            listSummary.ShiftName,
            listSummary.EarliestRequestedDeliveryDateUtc);

        var demands = MapDemands(plan.DemandsJson);
        if (!demands.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(demands.Failure!);
        }

        var risks = MapRisks(plan.RisksJson);
        if (!risks.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(risks.Failure!);
        }

        var resources = MapResources(plan.ResourceAssignmentJson);
        if (!resources.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(resources.Failure!);
        }

        var timeWindow = MapTimeWindow(plan.TimeWindowJson);
        if (!timeWindow.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(timeWindow.Failure!);
        }

        var approval = MapApproval(plan.ApprovalJson);
        if (!approval.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(approval.Failure!);
        }

        var transfer = MapTransfer(plan.TransferJson);
        if (!transfer.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(transfer.Failure!);
        }

        var sourceReferences = MapSourceReferences(plan.SourceReferencesJson);
        if (!sourceReferences.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(sourceReferences.Failure!);
        }

        var history = MapHistory(plan.HistoryJson);
        if (!history.Succeeded)
        {
            return MapFailure<ProductionPlanDetailMappedReadModel>(history.Failure!);
        }

        return MapSuccess(
            new ProductionPlanDetailMappedReadModel(
                summary,
                plan.Notes,
                plan.CreatedBy,
                plan.AiExplanation,
                plan.CreatedAtUtc,
                plan.Version,
                demands.Value!,
                risks.Value!,
                resources.Value!,
                timeWindow.Value!,
                approval.Value!,
                transfer.Value!,
                sourceReferences.Value!,
                history.Value!));
    }

    public static ProductionPlanJsonMapResult<ProductionPlanListOperationalSummaryReadModel>
        MapListOperationalSummary(
            string demandsJson,
            string risksJson,
            string resourceAssignmentJson,
            string timeWindowJson)
    {
        var demands = MapDemands(demandsJson);
        if (!demands.Succeeded)
        {
            return MapFailure<ProductionPlanListOperationalSummaryReadModel>(demands.Failure!);
        }

        var risks = MapRisks(risksJson);
        if (!risks.Succeeded)
        {
            return MapFailure<ProductionPlanListOperationalSummaryReadModel>(risks.Failure!);
        }

        var resources = MapResources(resourceAssignmentJson);
        if (!resources.Succeeded)
        {
            return MapFailure<ProductionPlanListOperationalSummaryReadModel>(resources.Failure!);
        }

        var timeWindow = MapTimeWindow(timeWindowJson);
        if (!timeWindow.Succeeded)
        {
            return MapFailure<ProductionPlanListOperationalSummaryReadModel>(timeWindow.Failure!);
        }

        var demandItems = demands.Value!;
        var riskItems = risks.Value!;
        var primaryDemand = demandItems.Count > 0 ? demandItems[0] : null;
        var highestDemandPriority = GetHighestDemandPriority(demandItems);
        var criticalRiskCount = 0;
        var blockingRiskCount = 0;

        foreach (var risk in riskItems)
        {
            if (risk.Severity == "critical")
            {
                criticalRiskCount++;
            }

            if (risk.Blocking)
            {
                blockingRiskCount++;
            }
        }

        decimal? totalRequiredQuantity = null;
        if (demandItems.Count > 0)
        {
            var unit = demandItems[0].Unit;
            if (demandItems.All(demand => demand.Unit == unit))
            {
                totalRequiredQuantity = demandItems.Sum(demand => demand.RequiredQuantity);
            }
        }

        return MapSuccess(new ProductionPlanListOperationalSummaryReadModel(
            DemandCount: demandItems.Count,
            TotalRequiredQuantity: totalRequiredQuantity,
            TotalRequiredQuantityUnit: totalRequiredQuantity.HasValue ? primaryDemand?.Unit : null,
            PrimaryRequiredQuantity: primaryDemand?.RequiredQuantity,
            PrimaryUnit: primaryDemand?.Unit,
            PrimaryProductCode: primaryDemand?.ProductCode,
            PrimaryProductName: primaryDemand?.ProductName,
            HighestDemandPriority: highestDemandPriority,
            RiskCount: riskItems.Count,
            CriticalRiskCount: criticalRiskCount,
            BlockingRiskCount: blockingRiskCount,
            HasBlockingRisk: blockingRiskCount > 0,
            HasMachineAssignment: !string.IsNullOrWhiteSpace(resources.Value!.MachineId)
                || !string.IsNullOrWhiteSpace(resources.Value.MachineName),
            HasMoldAssignment: !string.IsNullOrWhiteSpace(resources.Value.MoldId)
                || !string.IsNullOrWhiteSpace(resources.Value.MoldName),
            HasMaterialAssignment: !string.IsNullOrWhiteSpace(resources.Value.MaterialId)
                || !string.IsNullOrWhiteSpace(resources.Value.MaterialName),
            HasOperatorGroupAssignment: !string.IsNullOrWhiteSpace(resources.Value.OperatorGroupId)
                || !string.IsNullOrWhiteSpace(resources.Value.OperatorGroupName),
            ShiftName: timeWindow.Value!.ShiftName,
            EarliestRequestedDeliveryDateUtc: demandItems
                .Where(demand => demand.RequestedDeliveryDateUtc.HasValue)
                .Select(demand => demand.RequestedDeliveryDateUtc)
                .OrderBy(value => value)
                .FirstOrDefault()));
    }

    private static string? GetHighestDemandPriority(
        IReadOnlyList<ProductionPlanDemandReadModel> demands)
    {
        if (demands.Count == 0)
        {
            return null;
        }

        var priorityWeight = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["low"] = 0,
            ["normal"] = 1,
            ["high"] = 2,
            ["urgent"] = 3,
        };

        return demands
            .Select(demand => demand.Priority)
            .OrderByDescending(priority => priorityWeight.GetValueOrDefault(priority, -1))
            .First();
    }

    private static ProductionPlanJsonMapResult<IReadOnlyList<ProductionPlanDemandReadModel>> MapDemands(
        string json)
    {
        if (!TryParseArray(json, "demands", out var elements, out var failure))
        {
            return MapFailure<IReadOnlyList<ProductionPlanDemandReadModel>>(failure!);
        }

        var items = new List<ProductionPlanDemandReadModel>(elements.Count);
        foreach (var element in elements)
        {
            var id = RequireNonEmptyString(element, "demands.id");
            if (id.Failure is not null)
            {
                return FailList<ProductionPlanDemandReadModel>(id.Failure);
            }

            var productId = RequireNonEmptyString(element, "demands.productId");
            if (productId.Failure is not null)
            {
                return FailList<ProductionPlanDemandReadModel>(productId.Failure);
            }

            var productName = RequireNonEmptyString(element, "demands.productName");
            if (productName.Failure is not null)
            {
                return FailList<ProductionPlanDemandReadModel>(productName.Failure);
            }

            if (!TryReadDecimal(element, "requiredQuantity", out var requiredQuantity, out failure))
            {
                return FailList<ProductionPlanDemandReadModel>(failure);
            }

            var unit = RequireNonEmptyString(element, "demands.unit");
            if (unit.Failure is not null)
            {
                return FailList<ProductionPlanDemandReadModel>(unit.Failure);
            }

            var priority = RequireAllowedString(element, "priority", AllowedPriorities, "demands.priority");
            if (priority.Failure is not null)
            {
                return FailList<ProductionPlanDemandReadModel>(priority.Failure);
            }

            DateTimeOffset? requestedDeliveryDateUtc = null;
            if (element.TryGetProperty("requestedDeliveryDate", out var deliveryProperty)
                && deliveryProperty.ValueKind != JsonValueKind.Null)
            {
                if (!TryParseDateTimeOffset(deliveryProperty, "demands.requestedDeliveryDate", out var parsed, out failure))
                {
                    return FailList<ProductionPlanDemandReadModel>(failure);
                }

                requestedDeliveryDateUtc = parsed;
            }

            items.Add(new ProductionPlanDemandReadModel(
                id.Value!,
                productId.Value!,
                productName.Value!,
                requiredQuantity,
                unit.Value!,
                priority.Value!,
                ReadOptionalString(element, "orderReference"),
                ReadOptionalString(element, "customerId"),
                ReadOptionalString(element, "customerName"),
                ReadOptionalString(element, "productCode"),
                requestedDeliveryDateUtc));
        }

        return MapSuccess<IReadOnlyList<ProductionPlanDemandReadModel>>(items);
    }

    private static ProductionPlanJsonMapResult<IReadOnlyList<ProductionPlanRiskReadModel>> MapRisks(string json)
    {
        if (!TryParseArray(json, "risks", out var elements, out var failure))
        {
            return MapFailure<IReadOnlyList<ProductionPlanRiskReadModel>>(failure!);
        }

        var items = new List<ProductionPlanRiskReadModel>(elements.Count);
        foreach (var element in elements)
        {
            var id = RequireNonEmptyString(element, "risks.id");
            if (id.Failure is not null)
            {
                return FailList<ProductionPlanRiskReadModel>(id.Failure);
            }

            var type = RequireAllowedString(element, "type", AllowedRiskTypes, "risks.type");
            if (type.Failure is not null)
            {
                return FailList<ProductionPlanRiskReadModel>(type.Failure);
            }

            var severity = RequireAllowedString(element, "severity", AllowedRiskSeverities, "risks.severity");
            if (severity.Failure is not null)
            {
                return FailList<ProductionPlanRiskReadModel>(severity.Failure);
            }

            var title = RequireNonEmptyString(element, "risks.title");
            if (title.Failure is not null)
            {
                return FailList<ProductionPlanRiskReadModel>(title.Failure);
            }

            var description = RequireNonEmptyString(element, "risks.description");
            if (description.Failure is not null)
            {
                return FailList<ProductionPlanRiskReadModel>(description.Failure);
            }

            if (!element.TryGetProperty("blocking", out var blockingProperty)
                || blockingProperty.ValueKind != JsonValueKind.True
                    && blockingProperty.ValueKind != JsonValueKind.False)
            {
                return FailList<ProductionPlanRiskReadModel>(InvalidRow("risks.blocking", "Blocking flag is invalid."));
            }

            items.Add(new ProductionPlanRiskReadModel(
                id.Value!,
                type.Value!,
                severity.Value!,
                title.Value!,
                description.Value!,
                blockingProperty.GetBoolean(),
                ReadOptionalString(element, "source")));
        }

        return MapSuccess<IReadOnlyList<ProductionPlanRiskReadModel>>(items);
    }

    private static ProductionPlanJsonMapResult<ProductionPlanResourceAssignmentReadModel> MapResources(
        string json)
    {
        if (!TryParseObject(json, "resources", out var element, out var failure))
        {
            return MapFailure<ProductionPlanResourceAssignmentReadModel>(failure!);
        }

        return MapSuccess(
            new ProductionPlanResourceAssignmentReadModel(
                ReadOptionalString(element, "machineId"),
                ReadOptionalString(element, "machineName"),
                ReadOptionalString(element, "moldId"),
                ReadOptionalString(element, "moldName"),
                ReadOptionalString(element, "materialId"),
                ReadOptionalString(element, "materialName"),
                ReadOptionalString(element, "operatorGroupId"),
                ReadOptionalString(element, "operatorGroupName")));
    }

    private static ProductionPlanJsonMapResult<ProductionPlanTimeWindowReadModel> MapTimeWindow(string json)
    {
        if (!TryParseObject(json, "timeWindow", out var element, out var failure))
        {
            return MapFailure<ProductionPlanTimeWindowReadModel>(failure!);
        }

        DateTimeOffset? plannedStartAtUtc = null;
        if (element.TryGetProperty("plannedStartAt", out var startProperty)
            && startProperty.ValueKind != JsonValueKind.Null)
        {
            if (!TryParseDateTimeOffset(startProperty, "timeWindow.plannedStartAt", out var parsed, out failure))
            {
                return MapFailure<ProductionPlanTimeWindowReadModel>(failure);
            }

            plannedStartAtUtc = parsed;
        }

        DateTimeOffset? plannedEndAtUtc = null;
        if (element.TryGetProperty("plannedEndAt", out var endProperty)
            && endProperty.ValueKind != JsonValueKind.Null)
        {
            if (!TryParseDateTimeOffset(endProperty, "timeWindow.plannedEndAt", out var parsed, out failure))
            {
                return MapFailure<ProductionPlanTimeWindowReadModel>(failure);
            }

            plannedEndAtUtc = parsed;
        }

        int? estimatedDurationMinutes = null;
        if (element.TryGetProperty("estimatedDurationMinutes", out var durationProperty)
            && durationProperty.ValueKind != JsonValueKind.Null)
        {
            if (!durationProperty.TryGetInt32(out var duration) || duration < 0)
            {
                return MapFailure<ProductionPlanTimeWindowReadModel>(
                    InvalidRow("timeWindow.estimatedDurationMinutes", "Estimated duration is invalid."));
            }

            estimatedDurationMinutes = duration;
        }

        return MapSuccess(
            new ProductionPlanTimeWindowReadModel(
                plannedStartAtUtc,
                plannedEndAtUtc,
                estimatedDurationMinutes,
                ReadOptionalString(element, "shiftId"),
                ReadOptionalString(element, "shiftName")));
    }

    private static ProductionPlanJsonMapResult<ProductionPlanApprovalReadModel> MapApproval(string json)
    {
        if (!TryParseObject(json, "approval", out var element, out var failure))
        {
            return MapFailure<ProductionPlanApprovalReadModel>(failure!);
        }

        var status = RequireAllowedString(element, "status", AllowedApprovalStatuses, "approval.status");
        if (status.Failure is not null)
        {
            return MapFailure<ProductionPlanApprovalReadModel>(status.Failure);
        }

        var requestedAtUtc = ReadOptionalDateTime(element, "requestedAt", "approval.requestedAt", out failure);
        if (failure is not null)
        {
            return MapFailure<ProductionPlanApprovalReadModel>(failure);
        }

        var approvedAtUtc = ReadOptionalDateTime(element, "approvedAt", "approval.approvedAt", out failure);
        if (failure is not null)
        {
            return MapFailure<ProductionPlanApprovalReadModel>(failure);
        }

        var rejectedAtUtc = ReadOptionalDateTime(element, "rejectedAt", "approval.rejectedAt", out failure);
        if (failure is not null)
        {
            return MapFailure<ProductionPlanApprovalReadModel>(failure);
        }

        return MapSuccess(
            new ProductionPlanApprovalReadModel(
                status.Value!,
                ReadOptionalString(element, "requiredRole"),
                requestedAtUtc,
                ReadOptionalString(element, "requestedBy"),
                approvedAtUtc,
                ReadOptionalString(element, "approvedBy"),
                rejectedAtUtc,
                ReadOptionalString(element, "rejectedBy"),
                ReadOptionalString(element, "rejectionReason")));
    }

    private static ProductionPlanJsonMapResult<ProductionPlanTransferReadModel> MapTransfer(string json)
    {
        if (!TryParseObject(json, "transfer", out var element, out var failure))
        {
            return MapFailure<ProductionPlanTransferReadModel>(failure!);
        }

        var status = RequireAllowedString(element, "status", AllowedTransferStatuses, "transfer.status");
        if (status.Failure is not null)
        {
            return MapFailure<ProductionPlanTransferReadModel>(status.Failure);
        }

        string? targetSystem = null;
        if (element.TryGetProperty("targetSystem", out var targetProperty)
            && targetProperty.ValueKind != JsonValueKind.Null)
        {
            var parsedTarget = RequireAllowedString(
                element,
                "targetSystem",
                AllowedTargetSystems,
                "transfer.targetSystem");
            if (parsedTarget.Failure is not null)
            {
                return MapFailure<ProductionPlanTransferReadModel>(parsedTarget.Failure);
            }

            targetSystem = parsedTarget.Value;
        }

        var startedAtUtc = ReadOptionalDateTime(element, "startedAt", "transfer.startedAt", out failure);
        if (failure is not null)
        {
            return MapFailure<ProductionPlanTransferReadModel>(failure);
        }

        var completedAtUtc = ReadOptionalDateTime(element, "completedAt", "transfer.completedAt", out failure);
        if (failure is not null)
        {
            return MapFailure<ProductionPlanTransferReadModel>(failure);
        }

        return MapSuccess(
            new ProductionPlanTransferReadModel(
                targetSystem,
                status.Value!,
                ReadOptionalString(element, "externalPlanId"),
                startedAtUtc,
                completedAtUtc,
                ReadOptionalString(element, "errorCode"),
                ReadOptionalString(element, "errorMessage")));
    }

    private static ProductionPlanJsonMapResult<IReadOnlyList<ProductionPlanSourceReferenceReadModel>> MapSourceReferences(
        string json)
    {
        if (!TryParseArray(json, "sourceReferences", out var elements, out var failure))
        {
            return MapFailure<IReadOnlyList<ProductionPlanSourceReferenceReadModel>>(failure!);
        }

        var items = new List<ProductionPlanSourceReferenceReadModel>(elements.Count);
        foreach (var element in elements)
        {
            var sourceType = RequireAllowedString(
                element,
                "sourceType",
                AllowedSourceTypes,
                "sourceReferences.sourceType");
            if (sourceType.Failure is not null)
            {
                return FailList<ProductionPlanSourceReferenceReadModel>(sourceType.Failure);
            }

            var sourceId = RequireNonEmptyString(element, "sourceReferences.sourceId");
            if (sourceId.Failure is not null)
            {
                return FailList<ProductionPlanSourceReferenceReadModel>(sourceId.Failure);
            }

            DateTimeOffset? sourceUpdatedAtUtc = null;
            if (element.TryGetProperty("sourceUpdatedAt", out var updatedProperty)
                && updatedProperty.ValueKind != JsonValueKind.Null)
            {
                if (!TryParseDateTimeOffset(
                        updatedProperty,
                        "sourceReferences.sourceUpdatedAt",
                        out var parsed,
                        out failure))
                {
                    return FailList<ProductionPlanSourceReferenceReadModel>(failure);
                }

                sourceUpdatedAtUtc = parsed;
            }

            items.Add(new ProductionPlanSourceReferenceReadModel(
                sourceType.Value!,
                sourceId.Value!,
                sourceUpdatedAtUtc));
        }

        return MapSuccess<IReadOnlyList<ProductionPlanSourceReferenceReadModel>>(items);
    }

    private static ProductionPlanJsonMapResult<IReadOnlyList<ProductionPlanHistoryEntryReadModel>> MapHistory(
        string json)
    {
        if (!TryParseArray(json, "history", out var elements, out var failure))
        {
            return MapFailure<IReadOnlyList<ProductionPlanHistoryEntryReadModel>>(failure!);
        }

        var items = new List<ProductionPlanHistoryEntryReadModel>(elements.Count);
        foreach (var element in elements)
        {
            var id = RequireNonEmptyString(element, "history.id");
            if (id.Failure is not null)
            {
                return FailList<ProductionPlanHistoryEntryReadModel>(id.Failure);
            }

            var type = RequireAllowedString(element, "type", AllowedHistoryTypes, "history.type");
            if (type.Failure is not null)
            {
                return FailList<ProductionPlanHistoryEntryReadModel>(type.Failure);
            }

            var title = RequireNonEmptyString(element, "history.title");
            if (title.Failure is not null)
            {
                return FailList<ProductionPlanHistoryEntryReadModel>(title.Failure);
            }

            var description = RequireNonEmptyString(element, "history.description");
            if (description.Failure is not null)
            {
                return FailList<ProductionPlanHistoryEntryReadModel>(description.Failure);
            }

            DateTimeOffset? occurredAtUtc = null;
            if (element.TryGetProperty("occurredAt", out var occurredProperty)
                && occurredProperty.ValueKind != JsonValueKind.Null)
            {
                if (!TryParseDateTimeOffset(
                        occurredProperty,
                        "history.occurredAt",
                        out var parsed,
                        out failure))
                {
                    return FailList<ProductionPlanHistoryEntryReadModel>(failure);
                }

                occurredAtUtc = parsed;
            }

            items.Add(new ProductionPlanHistoryEntryReadModel(
                id.Value!,
                type.Value!,
                title.Value!,
                description.Value!,
                occurredAtUtc));
        }

        return MapSuccess<IReadOnlyList<ProductionPlanHistoryEntryReadModel>>(items);
    }

    private static bool TryParseArray(
        string json,
        string field,
        out List<JsonElement> elements,
        out ProductionPlanJsonMapFailure? failure)
    {
        elements = [];
        failure = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                failure = InvalidRow(field, "Expected a JSON array.");
                return false;
            }

            elements = document.RootElement.EnumerateArray().Select(element => element.Clone()).ToList();
            return true;
        }
        catch (JsonException)
        {
            failure = InvalidRow(field, "Stored JSON is invalid.");
            return false;
        }
    }

    private static bool TryParseObject(
        string json,
        string field,
        out JsonElement element,
        out ProductionPlanJsonMapFailure? failure)
    {
        element = default;
        failure = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failure = InvalidRow(field, "Expected a JSON object.");
                return false;
            }

            element = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            failure = InvalidRow(field, "Stored JSON is invalid.");
            return false;
        }
    }

    private static ProductionPlanJsonMapResult<string> RequireNonEmptyString(
        JsonElement element,
        string field)
    {
        var propertyName = field[(field.LastIndexOf('.') + 1)..];
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            return MapFailure<string>(
                InvalidRow(field, "Required string value is missing."));
        }

        return MapSuccess(property.GetString()!);
    }

    private static ProductionPlanJsonMapResult<string> RequireAllowedString(
        JsonElement element,
        string propertyName,
        HashSet<string> allowedValues,
        string field)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString())
            || !allowedValues.Contains(property.GetString()!))
        {
            return MapFailure<string>(
                InvalidRow(field, "Value is not allowed."));
        }

        return MapSuccess(property.GetString()!);
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateTimeOffset? ReadOptionalDateTime(
        JsonElement element,
        string propertyName,
        string field,
        out ProductionPlanJsonMapFailure? failure)
    {
        failure = null;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return TryParseDateTimeOffset(property, field, out var parsed, out failure)
            ? parsed
            : null;
    }

    private static bool TryReadDecimal(
        JsonElement element,
        string propertyName,
        out decimal value,
        out ProductionPlanJsonMapFailure failure)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDecimal(out value))
        {
            failure = InvalidRow($"demands.{propertyName}", "Required quantity is invalid.");
            return false;
        }

        failure = default!;
        return true;
    }

    private static bool TryParseDateTimeOffset(
        JsonElement property,
        string field,
        out DateTimeOffset value,
        out ProductionPlanJsonMapFailure failure,
        string? displayField = null)
    {
        value = default;
        displayField ??= field;

        if (property.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(
                property.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value))
        {
            failure = InvalidRow(displayField, "Date/time value is invalid.");
            return false;
        }

        failure = default!;
        return true;
    }

    private static ProductionPlanJsonMapFailure InvalidRow(string field, string message) =>
        new(field, ProductionPlanReadFailureCodes.InvalidRow, message);

    private static ProductionPlanJsonMapResult<T> MapSuccess<T>(T value) =>
        new(true, value, null);

    private static ProductionPlanJsonMapResult<T> MapFailure<T>(ProductionPlanJsonMapFailure failure) =>
        new(false, default, failure);

    private static ProductionPlanJsonMapResult<IReadOnlyList<T>> FailList<T>(ProductionPlanJsonMapFailure failure) =>
        MapFailure<IReadOnlyList<T>>(failure);
}
