namespace CrevaFactOS.Persistence.Export;

/// <summary>Pure deterministic export readiness calculator (no I/O).</summary>
public static class ExportReadinessEngine
{
    public static ExportReadinessCalculationResult Calculate(
        ExportCase caseEntity,
        IReadOnlyList<ExportDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(caseEntity);
        ArgumentNullException.ThrowIfNull(documents);

        var requirements = BuildRequirements(caseEntity, documents);
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
        IReadOnlyList<ExportDocument> documents)
    {
        var list = new List<ExportRequirementEvaluation>(5)
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

        return list;
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
