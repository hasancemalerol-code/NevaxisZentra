namespace CrevaFactOS.Persistence.Export;

/// <summary>Pure deterministic export readiness calculator (no I/O).</summary>
public static class ExportReadinessEngine
{
    public static ExportReadinessCalculationResult Calculate(
        ExportCase caseEntity,
        IReadOnlyList<ExportDocument> documents,
        ExportOperationalReadinessContext? operationalContext = null)
    {
        ArgumentNullException.ThrowIfNull(caseEntity);
        ArgumentNullException.ThrowIfNull(documents);

        var requirements = BuildRequirements(caseEntity, documents, operationalContext);
        var totalCount = requirements.Count;
        var satisfiedCount = requirements.Count(r => r.IsSatisfied);
        var blockingUnsatisfied = requirements.Count(r => r.IsBlocking && !r.IsSatisfied);
        var missingRequirementCount = requirements.Count(r => !r.IsSatisfied);

        var readinessPercent = totalCount == 0
            ? 100
            : (int)Math.Round(satisfiedCount / (double)totalCount * 100d, MidpointRounding.AwayFromZero);

        ExportReadinessStatus readinessStatus;
        if (missingRequirementCount == 0)
        {
            readinessStatus = ExportReadinessStatus.Ready;
        }
        else if (blockingUnsatisfied > 0)
        {
            readinessStatus = ExportReadinessStatus.NotReady;
        }
        else
        {
            readinessStatus = ExportReadinessStatus.Attention;
        }

        var suggestedCaseStatus = readinessStatus switch
        {
            ExportReadinessStatus.Ready => ExportCaseStatus.Ready,
            ExportReadinessStatus.NotReady => ExportCaseStatus.Blocked,
            _ => ExportCaseStatus.Preparing,
        };

        return new ExportReadinessCalculationResult(
            readinessStatus,
            readinessPercent,
            blockingUnsatisfied,
            missingRequirementCount,
            suggestedCaseStatus,
            requirements);
    }

    private static List<ExportRequirementEvaluation> BuildRequirements(
        ExportCase caseEntity,
        IReadOnlyList<ExportDocument> documents,
        ExportOperationalReadinessContext? ctx)
    {
        var list = new List<ExportRequirementEvaluation>(9)
        {
            EvaluateSourceOrderLinked(caseEntity),
            EvaluateDestinationCountry(caseEntity),
            EvaluateOriginCountry(caseEntity),
            EvaluateDocumentRequirement(
                ExportDocumentType.CommercialInvoice,
                "commercial-invoice",
                isBlocking: true,
                documents),
            EvaluateDocumentRequirement(
                ExportDocumentType.PackingList,
                "packing-list",
                isBlocking: true,
                documents),
        };

        if (ctx is not null)
        {
            list.Add(EvaluatePackingDataComplete(ctx));
            list.Add(EvaluatePackingQuantityReconciled(ctx));
            list.Add(EvaluateShipmentTransportModeKnown(ctx));
            list.Add(EvaluateBrokerCorrectionsClear(ctx));
        }

        return list;
    }

    private static ExportRequirementEvaluation EvaluatePackingDataComplete(
        ExportOperationalReadinessContext ctx)
    {
        // Satisfied when packing status is Complete or Verified.
        // If status is Draft and no signal that packing has started, allow skipping (treat as satisfied).
        // If any packing activity exists (status != Draft), require Complete or Verified.
        var packingStarted = ctx.PackingStatus != ExportPackingStatus.Draft;
        var packingDone = ctx.PackingStatus is ExportPackingStatus.Complete or ExportPackingStatus.Verified;

        var satisfied = !packingStarted || packingDone;
        return new ExportRequirementEvaluation(
            "packing-data-complete",
            "Packing data complete",
            IsBlocking: true,
            IsSatisfied: satisfied,
            Status: packingDone ? "satisfied" : (packingStarted ? "incomplete" : "not-started"));
    }

    private static ExportRequirementEvaluation EvaluatePackingQuantityReconciled(
        ExportOperationalReadinessContext ctx)
    {
        var packingAttempted = ctx.PackingStatus != ExportPackingStatus.Draft;

        var satisfied = ctx.ReconciliationStatus is
            ExportPackingReconciliationStatus.Matched or
            ExportPackingReconciliationStatus.UnableToValidate or
            ExportPackingReconciliationStatus.NotStarted;

        var isBlocking = packingAttempted
            && ctx.ReconciliationStatus is
                ExportPackingReconciliationStatus.Incomplete or
                ExportPackingReconciliationStatus.Overpacked or
                ExportPackingReconciliationStatus.Mismatch;

        var requirementSatisfied = !isBlocking && satisfied;

        return new ExportRequirementEvaluation(
            "packing-quantity-reconciled",
            "Packing quantity reconciled",
            IsBlocking: isBlocking,
            IsSatisfied: requirementSatisfied,
            Status: ctx.ReconciliationStatus switch
            {
                ExportPackingReconciliationStatus.Matched => "matched",
                ExportPackingReconciliationStatus.UnableToValidate => "unable-to-validate",
                ExportPackingReconciliationStatus.Incomplete => "incomplete",
                ExportPackingReconciliationStatus.Overpacked => "overpacked",
                ExportPackingReconciliationStatus.Mismatch => "mismatch",
                _ => "not-started",
            });
    }

    private static ExportRequirementEvaluation EvaluateShipmentTransportModeKnown(
        ExportOperationalReadinessContext ctx)
    {
        if (!ctx.HasShipment)
        {
            return new ExportRequirementEvaluation(
                "shipment-transport-mode-known",
                "Shipment transport mode known",
                IsBlocking: false,
                IsSatisfied: true,
                Status: "not-applicable");
        }

        var hasModeString = !string.IsNullOrWhiteSpace(ctx.ShipmentTransportMode);
        return new ExportRequirementEvaluation(
            "shipment-transport-mode-known",
            "Shipment transport mode known",
            IsBlocking: false,
            IsSatisfied: hasModeString,
            Status: hasModeString ? "satisfied" : "missing");
    }

    private static ExportRequirementEvaluation EvaluateBrokerCorrectionsClear(
        ExportOperationalReadinessContext ctx)
    {
        var satisfied = !ctx.HasOpenBlockingBrokerCorrection;
        return new ExportRequirementEvaluation(
            "broker-corrections-clear",
            "Broker corrections clear",
            IsBlocking: true,
            IsSatisfied: satisfied,
            Status: satisfied ? "satisfied" : "has-blocking-corrections");
    }

    private static ExportRequirementEvaluation EvaluateSourceOrderLinked(ExportCase caseEntity)
    {
        var satisfied = caseEntity.SourceOrderId != Guid.Empty;
        return new ExportRequirementEvaluation(
            "source-order-linked",
            "Source order linked",
            IsBlocking: true,
            IsSatisfied: satisfied,
            Status: satisfied ? "satisfied" : "missing");
    }

    private static ExportRequirementEvaluation EvaluateDestinationCountry(ExportCase caseEntity)
    {
        var satisfied = !string.IsNullOrWhiteSpace(caseEntity.DestinationCountryCode);
        return new ExportRequirementEvaluation(
            "destination-country-known",
            "Destination country known",
            IsBlocking: true,
            IsSatisfied: satisfied,
            Status: satisfied ? "satisfied" : "missing");
    }

    private static ExportRequirementEvaluation EvaluateOriginCountry(ExportCase caseEntity)
    {
        // V1: origin is attention/non-blocking when missing.
        var satisfied = !string.IsNullOrWhiteSpace(caseEntity.OriginCountryCode);
        return new ExportRequirementEvaluation(
            "origin-country-known",
            "Origin country known",
            IsBlocking: false,
            IsSatisfied: satisfied,
            Status: satisfied ? "satisfied" : "missing");
    }

    private static ExportRequirementEvaluation EvaluateDocumentRequirement(
        string documentType,
        string requirementKey,
        bool isBlocking,
        IReadOnlyList<ExportDocument> documents)
    {
        var match = documents
            .Where(d => string.Equals(d.DocumentType, documentType, StringComparison.Ordinal))
            .OrderByDescending(d => d.UpdatedAtUtc)
            .FirstOrDefault();

        if (match is null)
        {
            return new ExportRequirementEvaluation(
                requirementKey,
                documentType,
                isBlocking,
                IsSatisfied: false,
                Status: ExportValueConversions.ToStorage(ExportDocumentStatus.Missing));
        }

        // V1: Received or Verified counts as present/satisfied; missing/draft/rejected are not.
        var satisfied = match.DocumentStatus is ExportDocumentStatus.Received or ExportDocumentStatus.Verified;
        return new ExportRequirementEvaluation(
            requirementKey,
            documentType,
            isBlocking,
            IsSatisfied: satisfied,
            Status: ExportValueConversions.ToStorage(match.DocumentStatus));
    }
}
