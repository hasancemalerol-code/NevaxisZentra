using System.Globalization;
using System.Text.Json;

namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

public static class GoldenDemoCustomsFileJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string SerializeProductLines(IEnumerable<GoldenDemoCustomsFileProductLine> lines) =>
        JsonSerializer.Serialize(lines, Options);

    public static IReadOnlyList<GoldenDemoCustomsFileProductLine> DeserializeProductLines(string json)
    {
        return JsonSerializer.Deserialize<List<GoldenDemoCustomsFileProductLine>>(json, Options) ?? [];
    }

    public static GoldenDemoCustomsFileTradeRequirements DeserializeTradeRequirements(string json)
    {
        return JsonSerializer.Deserialize<GoldenDemoCustomsFileTradeRequirements>(json, Options)
            ?? new GoldenDemoCustomsFileTradeRequirements(false, false, false);
    }

    public static string SerializeTradeRequirements(GoldenDemoCustomsFileTradeRequirements requirements) =>
        JsonSerializer.Serialize(requirements, Options);

    public static GoldenDemoCustomsFileDocumentContent DeserializeDocumentContent(string json)
    {
        return JsonSerializer.Deserialize<GoldenDemoCustomsFileDocumentContent>(json, Options)
            ?? new GoldenDemoCustomsFileDocumentContent(
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                [],
                "EUR",
                "DAP",
                string.Empty,
                string.Empty);
    }

    public static string SerializeDocumentContent(GoldenDemoCustomsFileDocumentContent content) =>
        JsonSerializer.Serialize(content, Options);

    public static string SerializeGuidList(IEnumerable<Guid> ids) => JsonSerializer.Serialize(ids, Options);

    public static IReadOnlyList<Guid> DeserializeGuidList(string json) =>
        JsonSerializer.Deserialize<List<Guid>>(json, Options) ?? [];

    public static string SerializeManifest(IEnumerable<string> entries) => JsonSerializer.Serialize(entries, Options);

    public static IReadOnlyList<string> DeserializeManifest(string json) =>
        JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
}

public static class GoldenDemoCustomsFileDocumentFactory
{
    public static GoldenDemoCustomsFileDocumentContent BuildCommercialInvoiceContent(GoldenDemoCustomsFileOutcome outcome)
    {
        var lines = GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson);
        return new GoldenDemoCustomsFileDocumentContent(
            outcome.OrderNumber,
            outcome.ShipmentReference,
            outcome.PackageCount,
            outcome.ShipmentNetWeightKg,
            outcome.ShipmentGrossWeightKg,
            lines,
            outcome.Currency,
            outcome.Incoterm,
            outcome.DestinationCountry,
            outcome.CustomerName);
    }

    public static GoldenDemoCustomsFileDocumentContent BuildPackingListContent(
        GoldenDemoCustomsFileOutcome outcome,
        decimal netWeightKg)
    {
        var lines = GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson);
        return new GoldenDemoCustomsFileDocumentContent(
            outcome.OrderNumber,
            outcome.ShipmentReference,
            outcome.PackageCount,
            netWeightKg,
            outcome.ShipmentGrossWeightKg,
            lines,
            outcome.Currency,
            outcome.Incoterm,
            outcome.DestinationCountry,
            outcome.CustomerName);
    }
}

public static class GoldenDemoCustomsFileConsistencyEngine
{
    public static IReadOnlyList<GoldenDemoCustomsFileConsistencyCheck> Evaluate(
        GoldenDemoCustomsFileOutcome outcome,
        GoldenDemoCustomsFileDocument? commercialInvoice,
        GoldenDemoCustomsFileDocument? currentPackingList)
    {
        var checks = new List<GoldenDemoCustomsFileConsistencyCheck>();
        var orderLines = GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson);
        var orderQuantity = orderLines.Sum(l => l.Quantity);

        if (commercialInvoice is null)
        {
            checks.Add(Check("invoice-presence", "Commercial Invoice presence", "fail", "Commercial Invoice not prepared.", GoldenDemoCustomsFileImpactClassifications.Measured));
        }
        else
        {
            var invoice = GoldenDemoCustomsFileJson.DeserializeDocumentContent(commercialInvoice.ContentJson);
            var invoiceQuantity = invoice.Lines.Sum(l => l.Quantity);
            checks.Add(Check(
                "order-invoice-quantity",
                "Order vs Commercial Invoice quantity",
                invoiceQuantity == orderQuantity ? "pass" : "fail",
                invoiceQuantity == orderQuantity
                    ? "Invoice quantities match order lines."
                    : $"Invoice quantity {invoiceQuantity} differs from order quantity {orderQuantity}.",
                GoldenDemoCustomsFileImpactClassifications.Calculated));
            checks.Add(Check(
                "customer-consistency",
                "Customer consistency",
                string.Equals(invoice.CustomerName, outcome.CustomerName, StringComparison.OrdinalIgnoreCase) ? "pass" : "fail",
                "Customer on invoice matches order customer.",
                GoldenDemoCustomsFileImpactClassifications.Measured));
            checks.Add(Check(
                "destination-consistency",
                "Destination consistency",
                string.Equals(invoice.DestinationCountry, outcome.DestinationCountry, StringComparison.OrdinalIgnoreCase) ? "pass" : "fail",
                "Destination on invoice matches order destination.",
                GoldenDemoCustomsFileImpactClassifications.Measured));
            checks.Add(Check(
                "currency-consistency",
                "Currency consistency",
                string.Equals(invoice.Currency, outcome.Currency, StringComparison.OrdinalIgnoreCase) ? "pass" : "fail",
                "Invoice currency matches order currency.",
                GoldenDemoCustomsFileImpactClassifications.Measured));
        }

        if (currentPackingList is null)
        {
            checks.Add(Check("packing-presence", "Packing List presence", "fail", "Packing List not prepared.", GoldenDemoCustomsFileImpactClassifications.Measured));
        }
        else
        {
            var packing = GoldenDemoCustomsFileJson.DeserializeDocumentContent(currentPackingList.ContentJson);
            var packingQuantity = packing.Lines.Sum(l => l.Quantity);
            checks.Add(Check(
                "order-packing-quantity",
                "Order vs Packing List quantity",
                packingQuantity == orderQuantity ? "pass" : "fail",
                packingQuantity == orderQuantity
                    ? "Packing List quantities match order lines."
                    : $"Packing List quantity {packingQuantity} differs from order quantity {orderQuantity}.",
                GoldenDemoCustomsFileImpactClassifications.Calculated));
            checks.Add(Check(
                "package-count",
                "Package count completeness",
                packing.PackageCount == outcome.PackageCount ? "pass" : "fail",
                packing.PackageCount == outcome.PackageCount
                    ? "Package count matches shipment plan."
                    : $"Packing List package count {packing.PackageCount} differs from shipment package count {outcome.PackageCount}.",
                GoldenDemoCustomsFileImpactClassifications.Measured));
            checks.Add(Check(
                "net-weight-consistency",
                "Net weight consistency",
                packing.NetWeightKg == outcome.ShipmentNetWeightKg ? "pass" : "fail",
                packing.NetWeightKg == outcome.ShipmentNetWeightKg
                    ? "Packing List net weight matches shipment net weight."
                    : $"Packing List net weight {FormatWeight(packing.NetWeightKg)} kg differs from shipment net weight {FormatWeight(outcome.ShipmentNetWeightKg)} kg.",
                GoldenDemoCustomsFileImpactClassifications.Calculated));
            checks.Add(Check(
                "gross-weight-consistency",
                "Gross weight consistency",
                packing.GrossWeightKg == outcome.ShipmentGrossWeightKg ? "pass" : "fail",
                packing.GrossWeightKg == outcome.ShipmentGrossWeightKg
                    ? "Packing List gross weight matches shipment gross weight."
                    : $"Packing List gross weight {FormatWeight(packing.GrossWeightKg)} kg differs from shipment gross weight {FormatWeight(outcome.ShipmentGrossWeightKg)} kg.",
                GoldenDemoCustomsFileImpactClassifications.Calculated));
        }

        return checks;
    }

    public static bool HasNetWeightMismatch(
        GoldenDemoCustomsFileOutcome outcome,
        GoldenDemoCustomsFileDocument currentPackingList) =>
        GoldenDemoCustomsFileJson.DeserializeDocumentContent(currentPackingList.ContentJson).NetWeightKg
        != outcome.ShipmentNetWeightKg;

    private static GoldenDemoCustomsFileConsistencyCheck Check(
        string code,
        string label,
        string status,
        string explanation,
        string classification) =>
        new(code, label, status, explanation, classification);

    private static string FormatWeight(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}

public static class GoldenDemoCustomsFileRequirementEngine
{
    public static IReadOnlyList<GoldenDemoCustomsFileRequirementResult> Evaluate(
        GoldenDemoCustomsFileOutcome outcome,
        IReadOnlyList<GoldenDemoCustomsFileDocument> documents,
        IReadOnlyList<GoldenDemoCustomsFileBlocker> blockers,
        IReadOnlyList<GoldenDemoCustomsFileConsistencyCheck> consistencyChecks)
    {
        var trade = GoldenDemoCustomsFileJson.DeserializeTradeRequirements(outcome.TradeRequirementsJson);
        var commercialInvoice = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.CommercialInvoice);
        var packingList = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.PackingList);
        var openBlockers = blockers.Count(b => b.Status == GoldenDemoCustomsFileBlockerStatuses.Open);
        var consistencyPass = consistencyChecks.All(c => c.Status == "pass");

        var requirements = new List<GoldenDemoCustomsFileRequirementResult>
        {
            Req(GoldenDemoCustomsFileRequirementCodes.OrderDataComplete, "Order data complete", true,
                !string.IsNullOrWhiteSpace(outcome.OrderNumber), "Order number and lines loaded."),
            Req(GoldenDemoCustomsFileRequirementCodes.CustomerDataComplete, "Customer data complete", true,
                !string.IsNullOrWhiteSpace(outcome.CustomerName), "Customer identity present."),
            Req(GoldenDemoCustomsFileRequirementCodes.DestinationComplete, "Destination complete", true,
                !string.IsNullOrWhiteSpace(outcome.DestinationCountry), "Destination country present."),
            Req(GoldenDemoCustomsFileRequirementCodes.ProductDataComplete, "Product data complete", true,
                GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson).Count > 0, "Product lines loaded."),
            Req(GoldenDemoCustomsFileRequirementCodes.GtipPresentIfRequired, "GTIP present if required", false,
                true, "No GTIP requirement configured for this demo order."),
            Req(GoldenDemoCustomsFileRequirementCodes.OriginPresentIfRequired, "Origin present if required", true,
                GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson).All(l => !string.IsNullOrWhiteSpace(l.OriginCountry)),
                "Origin country present on product lines."),
            Req(GoldenDemoCustomsFileRequirementCodes.CommercialInvoiceReady, "Commercial Invoice ready", true,
                commercialInvoice?.Status == GoldenDemoCustomsFileDocumentStatuses.Ready,
                commercialInvoice is null ? "Commercial Invoice not prepared." : "Commercial Invoice current version ready."),
            Req(GoldenDemoCustomsFileRequirementCodes.PackingListReady, "Packing List ready", true,
                packingList?.Status == GoldenDemoCustomsFileDocumentStatuses.Ready,
                packingList is null ? "Packing List not prepared." : "Packing List current version ready."),
            Req(GoldenDemoCustomsFileRequirementCodes.PackingDataConsistent, "Packing data consistent", true,
                consistencyPass, "Cross-document packing checks must pass."),
            Req(GoldenDemoCustomsFileRequirementCodes.ConfiguredTradeDocumentsAddressed, "Configured trade documents addressed", true,
                EvaluateTradeDocuments(trade, documents), "Configured trade document statuses recorded."),
            Req(GoldenDemoCustomsFileRequirementCodes.NoBlockingIssues, "No blocking issues", true,
                openBlockers == 0, openBlockers == 0 ? "No open blockers." : $"{openBlockers} open blocker(s)."),
            Req(GoldenDemoCustomsFileRequirementCodes.InternalApprovalComplete, "Internal approval complete", true,
                outcome.ApprovedAtUtc is not null, outcome.ApprovedAtUtc is null ? "Approval pending." : "Approval recorded."),
        };

        return requirements;
    }

    private static bool EvaluateTradeDocuments(
        GoldenDemoCustomsFileTradeRequirements trade,
        IReadOnlyList<GoldenDemoCustomsFileDocument> documents)
    {
        if (trade.RequiresAtr && !HasStatusDocument(documents, GoldenDemoCustomsFileDocumentTypes.AtrStatus))
        {
            return false;
        }

        if (trade.RequiresEur1 && !HasStatusDocument(documents, GoldenDemoCustomsFileDocumentTypes.Eur1Status))
        {
            return false;
        }

        if (trade.RequiresCertificateOfOrigin
            && !HasStatusDocument(documents, GoldenDemoCustomsFileDocumentTypes.CertificateOfOriginStatus))
        {
            return false;
        }

        return true;
    }

    private static bool HasStatusDocument(IReadOnlyList<GoldenDemoCustomsFileDocument> documents, string documentType) =>
        documents.Any(d => d.IsCurrent && d.DocumentType == documentType && d.Status == GoldenDemoCustomsFileDocumentStatuses.Ready);

    private static GoldenDemoCustomsFileRequirementResult Req(
        string code,
        string label,
        bool required,
        bool pass,
        string explanation) =>
        new(
            code,
            label,
            "demo-export-rule",
            required,
            required ? (pass ? GoldenDemoCustomsFileRequirementStatuses.Pass : GoldenDemoCustomsFileRequirementStatuses.Fail) : GoldenDemoCustomsFileRequirementStatuses.NotRequired,
            GoldenDemoCustomsFileImpactClassifications.Calculated,
            required && !pass,
            explanation,
            null);
}

public static class GoldenDemoCustomsFileVerificationEngine
{
    public static GoldenDemoCustomsFileVerificationModel Evaluate(
        GoldenDemoCustomsFileOutcome outcome,
        IReadOnlyList<GoldenDemoCustomsFileRequirementResult> requirements,
        GoldenDemoCustomsFilePackage? package,
        IReadOnlyList<GoldenDemoCustomsFileBlocker> blockers,
        GoldenDemoCustomsFileVerificationRun? latestRun)
    {
        var mandatoryFails = requirements.Where(r => r.Required && r.Status == GoldenDemoCustomsFileRequirementStatuses.Fail).ToList();
        var openBlockers = blockers.Count(b => b.Status == GoldenDemoCustomsFileBlockerStatuses.Open);

        string result;
        string summary;
        if (outcome.Status == GoldenDemoCustomsFileOutcomeStatuses.Assigned)
        {
            result = GoldenDemoCustomsFileVerificationResults.InsufficientData;
            summary = "Outcome assigned but worker execution not started.";
        }
        else if (package is null)
        {
            result = GoldenDemoCustomsFileVerificationResults.NotReady;
            summary = "Customs clearance package has not been assembled.";
        }
        else if (openBlockers > 0 || mandatoryFails.Count > 0)
        {
            result = mandatoryFails.Count > 0 && openBlockers == 0
                ? GoldenDemoCustomsFileVerificationResults.PartiallyReady
                : GoldenDemoCustomsFileVerificationResults.NotReady;
            summary = openBlockers > 0
                ? "Blocking issues remain unresolved."
                : "Mandatory requirements are not satisfied.";
        }
        else if (outcome.ApprovedAtUtc is null)
        {
            result = GoldenDemoCustomsFileVerificationResults.NotReady;
            summary = "Human approval is required before broker handoff.";
        }
        else
        {
            result = GoldenDemoCustomsFileVerificationResults.VerifiedReady;
            summary = "Customs clearance package verified ready for customs broker handoff.";
        }

        return new GoldenDemoCustomsFileVerificationModel(
            result,
            Label(result),
            requirements,
            summary,
            latestRun?.CreatedAtUtc);
    }

    private static string Label(string result) => result switch
    {
        GoldenDemoCustomsFileVerificationResults.VerifiedReady => "VERIFIED READY",
        GoldenDemoCustomsFileVerificationResults.PartiallyReady => "PARTIALLY READY",
        GoldenDemoCustomsFileVerificationResults.NotReady => "NOT READY",
        _ => "INSUFFICIENT DATA",
    };
}

public static class GoldenDemoCustomsFileHeroEngine
{
    public static GoldenDemoCustomsFileHeroModel Build(
        GoldenDemoCustomsFileVerificationModel verification,
        IReadOnlyList<GoldenDemoCustomsFileDocument> documents,
        IReadOnlyList<GoldenDemoCustomsFileBlocker> blockers,
        GoldenDemoCustomsFilePackage? package,
        GoldenDemoCustomsFileOutcome outcome,
        IReadOnlyList<GoldenDemoCustomsFileConsistencyCheck> consistencyChecks)
    {
        var currentDocs = documents.Where(d => d.IsCurrent).ToList();
        var readyDocs = currentDocs.Count(d => d.Status == GoldenDemoCustomsFileDocumentStatuses.Ready);
        var openBlockers = blockers.Count(b => b.Status == GoldenDemoCustomsFileBlockerStatuses.Open);
        var consistencyPass = consistencyChecks.Count == 0
            ? "PENDING"
            : consistencyChecks.All(c => c.Status == "pass") ? "PASS" : "FAIL";
        var showSuccess = verification.Result == GoldenDemoCustomsFileVerificationResults.VerifiedReady;

        return new GoldenDemoCustomsFileHeroModel(
            showSuccess,
            showSuccess ? "GÜMRÜK DOSYASI HAZIR" : verification.Result switch
            {
                GoldenDemoCustomsFileVerificationResults.PartiallyReady => "GÜMRÜK DOSYASI KISMEN HAZIR",
                GoldenDemoCustomsFileVerificationResults.NotReady => "GÜMRÜK DOSYASI HAZIR DEĞİL",
                _ => "YETERSİZ VERİ",
            },
            showSuccess
                ? "Customs Clearance Package verified ready for customs broker handoff."
                : verification.Summary,
            verification.ResultLabel,
            readyDocs,
            currentDocs.Count,
            openBlockers,
            consistencyPass,
            outcome.ApprovedAtUtc is null ? "PENDING" : "COMPLETE",
            package is not null && verification.Result == GoldenDemoCustomsFileVerificationResults.VerifiedReady ? "READY" : "NOT READY",
            package?.Id,
            package?.PackageVersion);
    }
}

public static class GoldenDemoCustomsFileActivityProjector
{
    public static IReadOnlyList<GoldenDemoCustomsFileActivityModel> Project(
        IReadOnlyList<GoldenDemoCustomsFileActivity> activities) =>
        activities
            .OrderBy(a => a.OccurredAtUtc)
            .Select(a => new GoldenDemoCustomsFileActivityModel(
                a.StageCode,
                Label(a.StageCode),
                a.Actor,
                a.Action,
                a.Status,
                a.EvidenceReference,
                a.OccurredAtUtc))
            .ToList();

    private static string Label(string stageCode) => stageCode switch
    {
        GoldenDemoCustomsFileActivityStages.OutcomeAssigned => "Outcome Assigned",
        GoldenDemoCustomsFileActivityStages.ContextLoaded => "Context Loaded",
        GoldenDemoCustomsFileActivityStages.RequirementsEvaluated => "Requirements Evaluated",
        GoldenDemoCustomsFileActivityStages.DocumentsPrepared => "Documents Prepared",
        GoldenDemoCustomsFileActivityStages.BlockerDetected => "Blocker Detected",
        GoldenDemoCustomsFileActivityStages.CorrectiveWorkCreated => "Corrective Work Created",
        GoldenDemoCustomsFileActivityStages.CorrectionCompleted => "Correction Completed",
        GoldenDemoCustomsFileActivityStages.EvidenceRechecked => "Evidence Re-checked",
        GoldenDemoCustomsFileActivityStages.DocumentVersionUpdated => "Packing List New Version",
        GoldenDemoCustomsFileActivityStages.ApprovalRequested => "Approval Requested",
        GoldenDemoCustomsFileActivityStages.ApprovalCompleted => "Approval Completed",
        GoldenDemoCustomsFileActivityStages.PackageAssembled => "Customs Package Assembled",
        GoldenDemoCustomsFileActivityStages.VerificationExecuted => "Verification Executed",
        GoldenDemoCustomsFileActivityStages.VerifiedReady => "Verified Ready",
        _ => stageCode,
    };
}

public static class GoldenDemoCustomsFileSnapshotProjector
{
    public static GoldenDemoCustomsFileWorkspaceSnapshot Project(
        GoldenDemoCustomsFileOutcome outcome,
        IReadOnlyList<GoldenDemoCustomsFileDocument> documents,
        IReadOnlyList<GoldenDemoCustomsFileBlocker> blockers,
        IReadOnlyList<GoldenDemoCustomsFileCorrectiveWork> correctiveWorks,
        IReadOnlyList<GoldenDemoCustomsFileActivity> activities,
        GoldenDemoCustomsFilePackage? package,
        GoldenDemoCustomsFileVerificationRun? latestVerification)
    {
        var commercialInvoice = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.CommercialInvoice);
        var packingList = documents.FirstOrDefault(d =>
            d.IsCurrent && d.DocumentType == GoldenDemoCustomsFileDocumentTypes.PackingList);
        var consistency = GoldenDemoCustomsFileConsistencyEngine.Evaluate(outcome, commercialInvoice, packingList);
        var requirements = GoldenDemoCustomsFileRequirementEngine.Evaluate(outcome, documents, blockers, consistency);
        var verification = GoldenDemoCustomsFileVerificationEngine.Evaluate(
            outcome,
            requirements,
            package,
            blockers,
            latestVerification);
        var hero = GoldenDemoCustomsFileHeroEngine.Build(
            verification,
            documents,
            blockers,
            package,
            outcome,
            consistency);

        return new GoldenDemoCustomsFileWorkspaceSnapshot(
            outcome.Id,
            outcome.WorkerCode,
            "Customs File Digital Worker",
            "Gümrük Dosyası Dijital Çalışanı",
            outcome.AssignedOutcomeText,
            outcome.Status,
            StatusLabel(outcome.Status),
            outcome.OrderNumber,
            outcome.CustomerName,
            outcome.DestinationCountry,
            outcome.DeliveryAddress,
            outcome.Currency,
            outcome.Incoterm,
            outcome.PackageCount,
            outcome.ShipmentNetWeightKg,
            outcome.ShipmentGrossWeightKg,
            outcome.ShipmentReference,
            GoldenDemoCustomsFileJson.DeserializeProductLines(outcome.ProductLinesJson),
            GoldenDemoCustomsFileJson.DeserializeTradeRequirements(outcome.TradeRequirementsJson),
            documents
                .OrderBy(d => d.DocumentType)
                .ThenBy(d => d.VersionNumber)
                .Select(MapDocument)
                .ToList(),
            requirements,
            consistency,
            blockers.Select(MapBlocker).ToList(),
            correctiveWorks.Select(MapWork).ToList(),
            GoldenDemoCustomsFileActivityProjector.Project(activities),
            new GoldenDemoCustomsFileApprovalModel(
                true,
                outcome.ApprovedAtUtc is not null,
                "export-operations-reviewer",
                outcome.ApprovedBySubjectId,
                outcome.ApprovedAtUtc,
                outcome.Id.ToString()),
            package is null
                ? null
                : new GoldenDemoCustomsFilePackageModel(
                    package.Id,
                    package.PackageVersion,
                    GoldenDemoCustomsFileJson.DeserializeManifest(package.ManifestJson),
                    GoldenDemoCustomsFileJson.DeserializeGuidList(package.DocumentReferenceIdsJson),
                    package.AssembledAtUtc,
                    $"FACTOS-CUSTOMS-PACKAGE-{outcome.OrderNumber}-v{package.PackageVersion}"),
            verification,
            hero);
    }

    private static string StatusLabel(string status) => status switch
    {
        GoldenDemoCustomsFileOutcomeStatuses.Assigned => "Outcome Assigned",
        GoldenDemoCustomsFileOutcomeStatuses.ContextLoaded => "Context Loaded",
        GoldenDemoCustomsFileOutcomeStatuses.DocumentsPrepared => "Documents Prepared",
        GoldenDemoCustomsFileOutcomeStatuses.BlockerDetected => "Blocker Detected",
        GoldenDemoCustomsFileOutcomeStatuses.CorrectiveWorkCreated => "Corrective Work Created",
        GoldenDemoCustomsFileOutcomeStatuses.CorrectiveWorkCompleted => "Corrective Work Completed",
        GoldenDemoCustomsFileOutcomeStatuses.EvidenceRechecked => "Evidence Re-checked",
        GoldenDemoCustomsFileOutcomeStatuses.ApprovalPending => "Approval Pending",
        GoldenDemoCustomsFileOutcomeStatuses.Approved => "Approved",
        GoldenDemoCustomsFileOutcomeStatuses.PackageAssembled => "Package Assembled",
        GoldenDemoCustomsFileOutcomeStatuses.VerifiedReady => "Verified Ready",
        _ => status,
    };

    private static GoldenDemoCustomsFileDocumentModel MapDocument(GoldenDemoCustomsFileDocument document) =>
        new(
            document.Id,
            document.DocumentType,
            document.VersionNumber,
            document.IsCurrent,
            document.Status,
            GoldenDemoCustomsFileJson.DeserializeDocumentContent(document.ContentJson),
            document.CreatedAtUtc);

    private static GoldenDemoCustomsFileBlockerModel MapBlocker(GoldenDemoCustomsFileBlocker blocker) =>
        new(
            blocker.Id,
            blocker.BlockerCode,
            blocker.Status,
            blocker.Reason,
            blocker.EvidenceJson,
            blocker.RelatedDocumentId,
            blocker.DetectedAtUtc,
            blocker.ResolvedAtUtc);

    private static GoldenDemoCustomsFileCorrectiveWorkModel MapWork(GoldenDemoCustomsFileCorrectiveWork work) =>
        new(
            work.Id,
            work.Title,
            work.Status,
            work.ResponsibleRole,
            work.CompletionEvidenceJson,
            work.RelatedDocumentId,
            work.CreatedAtUtc,
            work.CompletedAtUtc);
}
