namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

public sealed class GoldenDemoCustomsFileOutcome
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string WorkerCode { get; set; } = GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker;
    public string AssignedOutcomeText { get; set; } = string.Empty;
    public string Status { get; set; } = GoldenDemoCustomsFileOutcomeStatuses.Assigned;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public string Incoterm { get; set; } = "DAP";
    public string ProductLinesJson { get; set; } = "[]";
    public int PackageCount { get; set; }
    public decimal OrderGrossWeightKg { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public decimal ShipmentNetWeightKg { get; set; }
    public decimal ShipmentGrossWeightKg { get; set; }
    public string TradeRequirementsJson { get; set; } = "{}";
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? ApprovedBySubjectId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? CurrentPackageId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class GoldenDemoCustomsFileDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = GoldenDemoCustomsFileDocumentStatuses.Draft;
    public string ContentJson { get; set; } = "{}";
    public string CreatedByWorkerCode { get; set; } = GoldenDemoCustomsFileWorkerCodes.CustomsFileDigitalWorker;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class GoldenDemoCustomsFileBlocker
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public string BlockerCode { get; set; } = string.Empty;
    public string Status { get; set; } = GoldenDemoCustomsFileBlockerStatuses.Open;
    public string Reason { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "{}";
    public Guid? RelatedDocumentId { get; set; }
    public DateTimeOffset DetectedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public sealed class GoldenDemoCustomsFileCorrectiveWork
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public Guid BlockerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = GoldenDemoCustomsFileCorrectiveWorkStatuses.Created;
    public string ResponsibleRole { get; set; } = string.Empty;
    public string CompletionEvidenceJson { get; set; } = "{}";
    public Guid? RelatedDocumentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class GoldenDemoCustomsFilePackage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public int PackageVersion { get; set; }
    public string ManifestJson { get; set; } = "{}";
    public string DocumentReferenceIdsJson { get; set; } = "[]";
    public DateTimeOffset AssembledAtUtc { get; set; }
}

public sealed class GoldenDemoCustomsFileVerificationRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public string Result { get; set; } = GoldenDemoCustomsFileVerificationResults.InsufficientData;
    public string ChecksJson { get; set; } = "[]";
    public string Summary { get; set; } = string.Empty;
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class GoldenDemoCustomsFileActivity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OutcomeId { get; set; }
    public string StageCode { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
