namespace NevaxisFactOS.Persistence.GoldenDemoCustomsFile;

public static class GoldenDemoCustomsFileWorkerCodes
{
    public const string CustomsFileDigitalWorker = "DW-EXP-001";
}

public static class GoldenDemoCustomsFileOutcomeStatuses
{
    public const string Assigned = "outcome-assigned";
    public const string ContextLoaded = "context-loaded";
    public const string DocumentsPrepared = "documents-prepared";
    public const string BlockerDetected = "blocker-detected";
    public const string CorrectiveWorkCreated = "corrective-work-created";
    public const string CorrectiveWorkCompleted = "corrective-work-completed";
    public const string EvidenceRechecked = "evidence-rechecked";
    public const string ApprovalPending = "approval-pending";
    public const string Approved = "approved";
    public const string PackageAssembled = "package-assembled";
    public const string VerifiedReady = "verified-ready";
    public const string NotReady = "not-ready";
    public const string PartiallyReady = "partially-ready";
    public const string InsufficientData = "insufficient-data";
}

public static class GoldenDemoCustomsFileDocumentTypes
{
    public const string CommercialInvoice = "commercial-invoice";
    public const string PackingList = "packing-list";
    public const string AtrStatus = "atr-status";
    public const string Eur1Status = "eur1-status";
    public const string CertificateOfOriginStatus = "certificate-of-origin-status";
}

public static class GoldenDemoCustomsFileDocumentStatuses
{
    public const string Draft = "draft";
    public const string Ready = "ready";
    public const string Invalid = "invalid";
    public const string Superseded = "superseded";
}

public static class GoldenDemoCustomsFileBlockerCodes
{
    public const string NetWeightMismatch = "packing-list-net-weight-mismatch";
}

public static class GoldenDemoCustomsFileBlockerStatuses
{
    public const string Open = "open";
    public const string Resolved = "resolved";
}

public static class GoldenDemoCustomsFileCorrectiveWorkStatuses
{
    public const string Created = "created";
    public const string InProgress = "in-progress";
    public const string Completed = "completed";
}

public static class GoldenDemoCustomsFileRequirementCodes
{
    public const string OrderDataComplete = "ORDER_DATA_COMPLETE";
    public const string CustomerDataComplete = "CUSTOMER_DATA_COMPLETE";
    public const string DestinationComplete = "DESTINATION_COMPLETE";
    public const string ProductDataComplete = "PRODUCT_DATA_COMPLETE";
    public const string GtipPresentIfRequired = "GTIP_PRESENT_IF_REQUIRED";
    public const string OriginPresentIfRequired = "ORIGIN_PRESENT_IF_REQUIRED";
    public const string CommercialInvoiceReady = "COMMERCIAL_INVOICE_READY";
    public const string PackingListReady = "PACKING_LIST_READY";
    public const string PackingDataConsistent = "PACKING_DATA_CONSISTENT";
    public const string ConfiguredTradeDocumentsAddressed = "CONFIGURED_TRADE_DOCUMENTS_ADDRESSED";
    public const string NoBlockingIssues = "NO_BLOCKING_ISSUES";
    public const string InternalApprovalComplete = "INTERNAL_APPROVAL_COMPLETE";
}

public static class GoldenDemoCustomsFileRequirementStatuses
{
    public const string Pass = "pass";
    public const string Fail = "fail";
    public const string NotRequired = "not-required";
    public const string InsufficientData = "insufficient-data";
}

public static class GoldenDemoCustomsFileVerificationResults
{
    public const string VerifiedReady = "verified-ready";
    public const string NotReady = "not-ready";
    public const string PartiallyReady = "partially-ready";
    public const string InsufficientData = "insufficient-data";
}

public static class GoldenDemoCustomsFileImpactClassifications
{
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string Estimated = "estimated";
    public const string Verified = "verified";
}

public static class GoldenDemoCustomsFileFailureCodes
{
    public const string OutcomeNotFound = "outcome-not-found";
    public const string InvalidState = "invalid-state";
    public const string BlockerNotFound = "blocker-not-found";
    public const string CorrectiveWorkNotFound = "corrective-work-not-found";
    public const string Unauthorized = "unauthorized";
}

public static class GoldenDemoCustomsFileActivityStages
{
    public const string OutcomeAssigned = "outcome-assigned";
    public const string ContextLoaded = "context-loaded";
    public const string RequirementsEvaluated = "requirements-evaluated";
    public const string DocumentsPrepared = "documents-prepared";
    public const string BlockerDetected = "blocker-detected";
    public const string CorrectiveWorkCreated = "corrective-work-created";
    public const string CorrectionCompleted = "correction-completed";
    public const string EvidenceRechecked = "evidence-rechecked";
    public const string DocumentVersionUpdated = "document-version-updated";
    public const string ApprovalRequested = "approval-requested";
    public const string ApprovalCompleted = "approval-completed";
    public const string PackageAssembled = "package-assembled";
    public const string VerificationExecuted = "verification-executed";
    public const string VerifiedReady = "verified-ready";
}
