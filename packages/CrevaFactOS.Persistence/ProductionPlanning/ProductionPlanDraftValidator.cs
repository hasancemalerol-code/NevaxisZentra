using System.Text.RegularExpressions;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public static class ProductionPlanDraftValidator
{
    public const int MaxReferencCodeLength = 100;
    public const int MaxTitleLength = 300;
    public const int MaxNotesLength = 4000;
    public const int MaxAiExplanationLength = 4000;
    public const int MaxDemandProductIdLength = 200;
    public const int MaxDemandProductNameLength = 200;
    public const int MaxDemandOrderReferenceLength = 200;
    public const int MaxRiskTitleLength = 200;
    public const int MaxRiskDescriptionLength = 2000;
    public const int MaxSourceReferenceIdLength = 200;

    private static readonly HashSet<string> AllowedOrigins = new(StringComparer.Ordinal)
    {
        "approved-order", "erp-production-request", "manual", "ai-recommendation",
    };

    private static readonly HashSet<string> AllowedConfidences = new(StringComparer.Ordinal)
    {
        "low", "medium", "high",
    };

    private static readonly HashSet<string> AllowedUnits = new(StringComparer.Ordinal)
    {
        "pcs", "kg", "g", "t", "l", "m", "m2", "m3", "box", "pallet",
    };

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

    private static readonly HashSet<string> AllowedApprovalStatuses = new(StringComparer.Ordinal)
    {
        "not-required", "pending",
    };

    private static readonly HashSet<string> AllowedTransferStatuses = new(StringComparer.Ordinal)
    {
        "not-started",
    };

    private static readonly HashSet<string> AllowedSourceTypes = new(StringComparer.Ordinal)
    {
        "order-draft", "erp-order", "customer-order", "manual-request",
    };

    private static readonly Regex SafeReferenceCodePattern =
        new(@"^[a-zA-Z0-9\-_./]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<ProductionPlanWriteFieldError> ValidateCreate(
        CreateProductionPlanDraftCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var errors = new List<ProductionPlanWriteFieldError>();
        ValidateCommonFields(
            command.ReferenceCode,
            command.Title,
            command.Notes,
            command.Origin,
            command.AiGenerated,
            command.Confidence,
            command.PlannedStartAtUtc,
            command.PlannedEndAtUtc,
            command.Demands,
            command.Risks,
            command.Resources,
            command.Approval,
            command.Transfer,
            command.SourceReferences,
            command.AiExplanation,
            errors);
        return errors;
    }

    public static IReadOnlyList<ProductionPlanWriteFieldError> ValidateUpdate(
        UpdateProductionPlanDraftCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductionPlanId == Guid.Empty)
        {
            return [new ProductionPlanWriteFieldError(
                "productionPlanId", "required", "Production plan identifier is required.")];
        }

        var errors = new List<ProductionPlanWriteFieldError>();
        ValidateCommonFields(
            command.ReferenceCode,
            command.Title,
            command.Notes,
            command.Origin,
            command.AiGenerated,
            command.Confidence,
            command.PlannedStartAtUtc,
            command.PlannedEndAtUtc,
            command.Demands,
            command.Risks,
            command.Resources,
            command.Approval,
            command.Transfer,
            command.SourceReferences,
            command.AiExplanation,
            errors);
        return errors;
    }

    private static void ValidateCommonFields(
        string referenceCode,
        string title,
        string? notes,
        string origin,
        bool aiGenerated,
        string? confidence,
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc,
        IReadOnlyList<ProductionPlanDemandWriteModel> demands,
        IReadOnlyList<ProductionPlanRiskWriteModel> risks,
        ProductionPlanResourceAssignmentWriteModel resources,
        ProductionPlanApprovalWriteModel? approval,
        ProductionPlanTransferWriteModel? transfer,
        IReadOnlyList<ProductionPlanSourceReferenceWriteModel> sourceReferences,
        string? aiExplanation,
        List<ProductionPlanWriteFieldError> errors)
    {
        ValidateReferenceCode(referenceCode, errors);
        ValidateTitle(title, errors);
        ValidateNotes(notes, errors);
        ValidateOrigin(origin, errors);
        ValidateConfidence(aiGenerated, confidence, errors);
        ValidatePlannedWindow(plannedStartAtUtc, plannedEndAtUtc, errors);
        ValidateDemands(demands, errors);
        ValidateRisks(risks, errors);
        ValidateResources(resources, errors);
        ValidateApproval(approval, errors);
        ValidateTransfer(transfer, errors);
        ValidateSourceReferences(sourceReferences, errors);
        ValidateAiExplanation(aiGenerated, aiExplanation, errors);
    }

    private static void ValidateReferenceCode(string referenceCode, List<ProductionPlanWriteFieldError> errors)
    {
        var trimmed = referenceCode?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            errors.Add(new("referenceCode", "required", "Reference code is required."));
            return;
        }

        if (trimmed.Length > MaxReferencCodeLength)
        {
            errors.Add(new("referenceCode", "too-long",
                $"Reference code must not exceed {MaxReferencCodeLength} characters."));
            return;
        }

        if (!SafeReferenceCodePattern.IsMatch(trimmed))
        {
            errors.Add(new("referenceCode", "invalid-characters",
                "Reference code may only contain letters, digits, hyphens, underscores, dots, and slashes."));
        }
    }

    private static void ValidateTitle(string title, List<ProductionPlanWriteFieldError> errors)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            errors.Add(new("title", "required", "Title is required."));
            return;
        }

        if (trimmed.Length > MaxTitleLength)
        {
            errors.Add(new("title", "too-long",
                $"Title must not exceed {MaxTitleLength} characters."));
        }
    }

    private static void ValidateNotes(string? notes, List<ProductionPlanWriteFieldError> errors)
    {
        if (notes is not null && notes.Length > MaxNotesLength)
        {
            errors.Add(new("notes", "too-long",
                $"Notes must not exceed {MaxNotesLength} characters."));
        }
    }

    private static void ValidateOrigin(string origin, List<ProductionPlanWriteFieldError> errors)
    {
        if (!AllowedOrigins.Contains(origin ?? string.Empty))
        {
            errors.Add(new("origin", "invalid-value",
                "Origin must be one of: approved-order, erp-production-request, manual, ai-recommendation."));
        }
    }

    private static void ValidateConfidence(
        bool aiGenerated,
        string? confidence,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (confidence is null)
        {
            return;
        }

        if (!AllowedConfidences.Contains(confidence))
        {
            errors.Add(new("confidence", "invalid-value",
                "Confidence must be one of: low, medium, high."));
        }
    }

    private static void ValidatePlannedWindow(
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (plannedStartAtUtc == default || plannedStartAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add(new("plannedStartAtUtc", "invalid-utc",
                "Planned start must be a valid UTC date/time."));
        }

        if (plannedEndAtUtc == default || plannedEndAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add(new("plannedEndAtUtc", "invalid-utc",
                "Planned end must be a valid UTC date/time."));
        }

        if (plannedStartAtUtc != default
            && plannedEndAtUtc != default
            && plannedStartAtUtc.Offset == TimeSpan.Zero
            && plannedEndAtUtc.Offset == TimeSpan.Zero
            && plannedEndAtUtc < plannedStartAtUtc)
        {
            errors.Add(new("plannedEndAtUtc", "invalid-range",
                "Planned end must be on or after planned start."));
        }
    }

    private static void ValidateDemands(
        IReadOnlyList<ProductionPlanDemandWriteModel> demands,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (demands is null || demands.Count == 0)
        {
            errors.Add(new("demands", "required", "At least one demand is required."));
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < demands.Count; i++)
        {
            var demand = demands[i];
            var prefix = $"demands[{i}]";

            if (string.IsNullOrWhiteSpace(demand.Id))
            {
                errors.Add(new($"{prefix}.id", "required", "Demand id is required."));
            }
            else if (!seenIds.Add(demand.Id))
            {
                errors.Add(new($"{prefix}.id", "duplicate", "Demand id must be unique within the plan."));
            }

            if (string.IsNullOrWhiteSpace(demand.ProductId) || demand.ProductId.Length > MaxDemandProductIdLength)
            {
                errors.Add(new($"{prefix}.productId", "required", "Demand productId is required."));
            }

            if (string.IsNullOrWhiteSpace(demand.ProductName) || demand.ProductName.Length > MaxDemandProductNameLength)
            {
                errors.Add(new($"{prefix}.productName", "required", "Demand productName is required."));
            }

            if (demand.RequiredQuantity <= 0)
            {
                errors.Add(new($"{prefix}.requiredQuantity", "invalid-value",
                    "Demand quantity must be greater than zero."));
            }

            if (!AllowedUnits.Contains(demand.Unit ?? string.Empty))
            {
                errors.Add(new($"{prefix}.unit", "invalid-value",
                    $"Demand unit must be one of: {string.Join(", ", AllowedUnits)}."));
            }

            if (!AllowedPriorities.Contains(demand.Priority ?? string.Empty))
            {
                errors.Add(new($"{prefix}.priority", "invalid-value",
                    "Demand priority must be one of: low, normal, high, urgent."));
            }

            if (demand.RequestedDeliveryDateUtc.HasValue
                && demand.RequestedDeliveryDateUtc.Value.Offset != TimeSpan.Zero)
            {
                errors.Add(new($"{prefix}.requestedDeliveryDateUtc", "invalid-utc",
                    "Requested delivery date must be UTC."));
            }
        }
    }

    private static void ValidateRisks(
        IReadOnlyList<ProductionPlanRiskWriteModel> risks,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (risks is null)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < risks.Count; i++)
        {
            var risk = risks[i];
            var prefix = $"risks[{i}]";

            if (string.IsNullOrWhiteSpace(risk.Id))
            {
                errors.Add(new($"{prefix}.id", "required", "Risk id is required."));
            }
            else if (!seenIds.Add(risk.Id))
            {
                errors.Add(new($"{prefix}.id", "duplicate", "Risk id must be unique within the plan."));
            }

            if (!AllowedRiskTypes.Contains(risk.Type ?? string.Empty))
            {
                errors.Add(new($"{prefix}.type", "invalid-value",
                    $"Risk type must be one of: {string.Join(", ", AllowedRiskTypes)}."));
            }

            if (!AllowedRiskSeverities.Contains(risk.Severity ?? string.Empty))
            {
                errors.Add(new($"{prefix}.severity", "invalid-value",
                    "Risk severity must be one of: information, warning, critical."));
            }

            if (string.IsNullOrWhiteSpace(risk.Title) || risk.Title.Length > MaxRiskTitleLength)
            {
                errors.Add(new($"{prefix}.title", "required", "Risk title is required."));
            }

            if (string.IsNullOrWhiteSpace(risk.Description) || risk.Description.Length > MaxRiskDescriptionLength)
            {
                errors.Add(new($"{prefix}.description", "required", "Risk description is required."));
            }

            if (risk.Blocking && string.IsNullOrWhiteSpace(risk.Description))
            {
                errors.Add(new($"{prefix}.description", "required-for-blocking",
                    "A description is required for blocking risks."));
            }
        }
    }

    private static void ValidateResources(
        ProductionPlanResourceAssignmentWriteModel resources,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (resources is null)
        {
            return;
        }

        if (resources.MachineId == Guid.Empty.ToString() ||
            resources.MoldId == Guid.Empty.ToString() ||
            resources.MaterialId == Guid.Empty.ToString() ||
            resources.OperatorGroupId == Guid.Empty.ToString())
        {
            errors.Add(new("resources", "empty-guid",
                "Resource assignments must not use empty GUID identifiers."));
        }
    }

    private static void ValidateApproval(
        ProductionPlanApprovalWriteModel? approval,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (approval is null)
        {
            return;
        }

        if (!AllowedApprovalStatuses.Contains(approval.Status ?? string.Empty))
        {
            errors.Add(new("approval.status", "invalid-value",
                "Approval status may only be 'not-required' or 'pending' when creating or editing a draft."));
        }
    }

    private static void ValidateTransfer(
        ProductionPlanTransferWriteModel? transfer,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (transfer is null)
        {
            return;
        }

        if (!AllowedTransferStatuses.Contains(transfer.Status ?? string.Empty))
        {
            errors.Add(new("transfer.status", "invalid-value",
                "Transfer status may only be 'not-started' when creating or editing a draft."));
        }
    }

    private static void ValidateSourceReferences(
        IReadOnlyList<ProductionPlanSourceReferenceWriteModel> sourceReferences,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (sourceReferences is null)
        {
            return;
        }

        for (var i = 0; i < sourceReferences.Count; i++)
        {
            var source = sourceReferences[i];
            var prefix = $"sourceReferences[{i}]";

            if (!AllowedSourceTypes.Contains(source.SourceType ?? string.Empty))
            {
                errors.Add(new($"{prefix}.sourceType", "invalid-value",
                    "Source reference type must be one of: order-draft, erp-order, customer-order, manual-request."));
            }

            if (string.IsNullOrWhiteSpace(source.SourceId) || source.SourceId.Length > MaxSourceReferenceIdLength)
            {
                errors.Add(new($"{prefix}.sourceId", "required", "Source reference id is required."));
            }

            if (source.SourceUpdatedAtUtc.HasValue && source.SourceUpdatedAtUtc.Value.Offset != TimeSpan.Zero)
            {
                errors.Add(new($"{prefix}.sourceUpdatedAtUtc", "invalid-utc",
                    "Source updated date must be UTC."));
            }
        }
    }

    private static void ValidateAiExplanation(
        bool aiGenerated,
        string? aiExplanation,
        List<ProductionPlanWriteFieldError> errors)
    {
        if (aiExplanation is not null && aiExplanation.Length > MaxAiExplanationLength)
        {
            errors.Add(new("aiExplanation", "too-long",
                $"AI explanation must not exceed {MaxAiExplanationLength} characters."));
        }
    }

    public static bool IsEditableStatus(ProductionPlanStatus status) =>
        status is ProductionPlanStatus.Draft or ProductionPlanStatus.NeedsReview;

    public static bool IsLockedStatus(ProductionPlanStatus status) =>
        status is ProductionPlanStatus.Approved
            or ProductionPlanStatus.Scheduled
            or ProductionPlanStatus.InProgress
            or ProductionPlanStatus.Completed
            or ProductionPlanStatus.Cancelled
            or ProductionPlanStatus.ReadyForApproval
            or ProductionPlanStatus.Rejected;
}
