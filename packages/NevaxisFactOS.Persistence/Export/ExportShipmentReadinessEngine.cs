namespace NevaxisFactOS.Persistence.Export;

/// <summary>Pure static shipment-specific readiness calculator (no I/O).</summary>
public static class ExportShipmentReadinessEngine
{
    public static ExportShipmentReadinessReadModel Calculate(
        ExportShipment shipment,
        ExportCase exportCase,
        ExportPackingStatus packingStatus,
        IReadOnlyList<ExportDocument> documents,
        bool hasOpenBlockingBrokerCorrection)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(exportCase);
        ArgumentNullException.ThrowIfNull(documents);

        var requirements = new List<ExportRequirementEvaluation>();

        // Transport mode is required for shipment readiness
        var hasMode = !string.IsNullOrWhiteSpace(shipment.TransportMode);
        requirements.Add(new ExportRequirementEvaluation(
            "shipment-transport-mode-known",
            "Shipment transport mode known",
            IsBlocking: true,
            IsSatisfied: hasMode,
            Status: hasMode ? "satisfied" : "missing"));

        // Packing must be complete or verified
        var packingDone = packingStatus is ExportPackingStatus.Complete or ExportPackingStatus.Verified;
        requirements.Add(new ExportRequirementEvaluation(
            "packing-data-complete",
            "Packing data complete",
            IsBlocking: true,
            IsSatisfied: packingDone,
            Status: packingDone ? "satisfied" : "incomplete"));

        // Verified commercial invoice
        var hasVerifiedInvoice = documents.Any(d =>
            string.Equals(d.DocumentType, ExportDocumentType.CommercialInvoice, StringComparison.Ordinal)
            && d.DocumentStatus == ExportDocumentStatus.Verified);

        requirements.Add(new ExportRequirementEvaluation(
            "verified-commercial-invoice",
            "Verified commercial invoice",
            IsBlocking: true,
            IsSatisfied: hasVerifiedInvoice,
            Status: hasVerifiedInvoice ? "satisfied" : "missing"));

        // Verified packing list
        var hasVerifiedPackingList = documents.Any(d =>
            string.Equals(d.DocumentType, ExportDocumentType.PackingList, StringComparison.Ordinal)
            && d.DocumentStatus == ExportDocumentStatus.Verified);

        requirements.Add(new ExportRequirementEvaluation(
            "verified-packing-list",
            "Verified packing list",
            IsBlocking: true,
            IsSatisfied: hasVerifiedPackingList,
            Status: hasVerifiedPackingList ? "satisfied" : "missing"));

        // No open blocking broker corrections
        var correctionsClear = !hasOpenBlockingBrokerCorrection;
        requirements.Add(new ExportRequirementEvaluation(
            "broker-corrections-clear",
            "Broker corrections clear",
            IsBlocking: true,
            IsSatisfied: correctionsClear,
            Status: correctionsClear ? "satisfied" : "has-blocking-corrections"));

        var totalCount = requirements.Count;
        var satisfiedCount = requirements.Count(r => r.IsSatisfied);
        var blockingUnsatisfied = requirements.Count(r => r.IsBlocking && !r.IsSatisfied);
        var missingCount = requirements.Count(r => !r.IsSatisfied);

        var readinessPercent = totalCount == 0
            ? 100
            : (int)Math.Round(satisfiedCount / (double)totalCount * 100d, MidpointRounding.AwayFromZero);

        var readinessStatus = missingCount == 0
            ? "ready"
            : blockingUnsatisfied > 0
                ? "not-ready"
                : "attention";

        return new ExportShipmentReadinessReadModel(
            shipment.Id,
            readinessStatus,
            readinessPercent,
            blockingUnsatisfied,
            missingCount,
            requirements);
    }
}
