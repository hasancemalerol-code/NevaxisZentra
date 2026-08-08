namespace CrevaFactOS.Persistence.Export;

// ─── Readiness context ────────────────────────────────────────────────────────

public sealed record ExportOperationalReadinessContext(
    ExportPackingStatus PackingStatus,
    ExportPackingReconciliationStatus ReconciliationStatus,
    bool HasOpenBlockingBrokerCorrection,
    bool HasShipment,
    string? ShipmentTransportMode,
    DateTimeOffset? PlannedDepartureAtUtc,
    bool HasVerifiedCommercialInvoice,
    bool HasVerifiedPackingList);

// ─── Failure codes (2L additions) ────────────────────────────────────────────

public static class Export2LFailureCodes
{
    public const string PackingUnitNotFound = "packing-unit-not-found";
    public const string PackingItemNotFound = "packing-item-not-found";
    public const string ShipmentNotFound = "shipment-not-found";
    public const string BrokerWorkspaceNotFound = "broker-workspace-not-found";
    public const string BrokerCorrectionNotFound = "broker-correction-not-found";
    public const string PackageVersionNotFound = "package-version-not-found";
    public const string InvalidPackingStatus = "invalid-packing-status";
    public const string InvalidShipmentStatus = "invalid-shipment-status";
    public const string InvalidBrokerWorkspaceStatus = "invalid-broker-workspace-status";
    public const string InvalidTransition = "invalid-transition";
    public const string InvalidUnitType = "invalid-unit-type";
    public const string InvalidTransportMode = "invalid-transport-mode";
    public const string DuplicateUnitCode = "duplicate-unit-code";
    public const string DuplicateShipmentReference = "duplicate-shipment-reference";
    public const string ActiveWorkspaceExists = "active-workspace-exists";
    public const string NoDraftPackageVersion = "no-draft-package-version";
    public const string PackingValidationFailed = "packing-validation-failed";
    public const string RejectedDocumentNotAllowed = "rejected-document-not-allowed";
}

// ─── Packing commands ─────────────────────────────────────────────────────────

public sealed record CreateExportPackingUnitCommand(
    Guid ExportCaseId,
    string UnitCode,
    string UnitType,
    Guid? ParentPackingUnitId,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    string? MarksAndNumbers);

public sealed record UpdateExportPackingUnitCommand(
    Guid ExportCaseId,
    Guid PackingUnitId,
    uint ExpectedVersion,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    string? MarksAndNumbers);

public sealed record AddExportPackingItemCommand(
    Guid ExportCaseId,
    Guid PackingUnitId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasure,
    Guid? SourceOrderLineId,
    string? LotBatchReference);

public sealed record CompleteExportPackingCommand(
    Guid ExportCaseId,
    uint ExpectedCaseVersion);

public sealed record VerifyExportPackingCommand(
    Guid ExportCaseId,
    uint ExpectedCaseVersion);

public sealed record ReconcileExportPackingCommand(
    Guid ExportCaseId,
    decimal? OrderedQuantityTotal);

// ─── Shipment commands ────────────────────────────────────────────────────────

public sealed record CreateExportShipmentCommand(
    Guid ExportCaseId,
    string ShipmentReference,
    string? TransportMode,
    string? IncotermCode,
    string DestinationCountryCode,
    string? DestinationCity,
    string? DeliveryAddress,
    string? ForwarderName,
    string? CarrierName,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    string? BookingReference);

public sealed record UpdateExportShipmentCommand(
    Guid ExportCaseId,
    Guid ShipmentId,
    uint ExpectedVersion,
    string? TransportMode,
    string? IncotermCode,
    string? DestinationCountryCode,
    string? DestinationCity,
    string? DeliveryAddress,
    string? ForwarderName,
    string? CarrierName,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    DateTimeOffset? ActualPickupAtUtc,
    DateTimeOffset? ActualDepartureAtUtc,
    DateTimeOffset? ActualArrivalAtUtc,
    string? BookingReference,
    string? TrackingReference,
    decimal? TotalGrossWeight,
    decimal? TotalNetWeight,
    int? TotalPackageCount);

public sealed record TransitionExportShipmentCommand(
    Guid ExportCaseId,
    Guid ShipmentId,
    uint ExpectedVersion,
    ExportShipmentStatus TargetStatus,
    string? CancellationReason);

// ─── Broker commands ──────────────────────────────────────────────────────────

public sealed record CreateExportBrokerWorkspaceCommand(
    Guid ExportCaseId,
    Guid? ShipmentId,
    string BrokerCompanyName,
    string? BrokerContactName,
    string? BrokerEmail,
    string? BrokerPhone,
    string? Notes);

public sealed record PrepareExportBrokerPackageCommand(
    Guid ExportCaseId,
    Guid BrokerWorkspaceId,
    uint ExpectedWorkspaceVersion,
    IReadOnlyList<Guid> DocumentIds,
    string? Notes);

public sealed record SendExportBrokerPackageCommand(
    Guid ExportCaseId,
    Guid BrokerWorkspaceId,
    uint ExpectedWorkspaceVersion);

public sealed record AcknowledgeExportBrokerCommand(
    Guid ExportCaseId,
    Guid BrokerWorkspaceId,
    uint ExpectedWorkspaceVersion,
    string? BrokerReference);

public sealed record CreateExportBrokerCorrectionCommand(
    Guid ExportCaseId,
    Guid BrokerWorkspaceId,
    string Category,
    string Title,
    string Description,
    ExportBrokerCorrectionSeverity Severity,
    Guid? RelatedDocumentId);

public sealed record ResolveExportBrokerCorrectionCommand(
    Guid ExportCaseId,
    Guid BrokerWorkspaceId,
    Guid CorrectionId,
    uint ExpectedWorkspaceVersion,
    ExportBrokerCorrectionStatus Resolution,
    string? ResolutionNote);

// ─── Write results ────────────────────────────────────────────────────────────

public sealed record ExportPackingUnitWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? PackingUnitId { get; init; }
    public uint? UnitVersion { get; init; }
    public uint? CaseVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportPackingUnitWriteResult Success(Guid packingUnitId, uint unitVersion, uint caseVersion) =>
        new() { Succeeded = true, PackingUnitId = packingUnitId, UnitVersion = unitVersion, CaseVersion = caseVersion };

    public static ExportPackingUnitWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportPackingItemWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ItemId { get; init; }
    public uint? CaseVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportPackingItemWriteResult Success(Guid itemId, uint caseVersion) =>
        new() { Succeeded = true, ItemId = itemId, CaseVersion = caseVersion };

    public static ExportPackingItemWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportPackingStatusWriteResult
{
    public required bool Succeeded { get; init; }
    public uint? CaseVersion { get; init; }
    public string? PackingStatus { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportPackingStatusWriteResult Success(uint caseVersion, string packingStatus) =>
        new() { Succeeded = true, CaseVersion = caseVersion, PackingStatus = packingStatus };

    public static ExportPackingStatusWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportShipmentWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ShipmentId { get; init; }
    public uint? ShipmentVersion { get; init; }
    public string? ShipmentStatus { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportShipmentWriteResult Success(Guid shipmentId, uint shipmentVersion, string shipmentStatus) =>
        new() { Succeeded = true, ShipmentId = shipmentId, ShipmentVersion = shipmentVersion, ShipmentStatus = shipmentStatus };

    public static ExportShipmentWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportBrokerWorkspaceWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? WorkspaceId { get; init; }
    public uint? WorkspaceVersion { get; init; }
    public string? WorkspaceStatus { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportBrokerWorkspaceWriteResult Success(Guid workspaceId, uint workspaceVersion, string workspaceStatus) =>
        new() { Succeeded = true, WorkspaceId = workspaceId, WorkspaceVersion = workspaceVersion, WorkspaceStatus = workspaceStatus };

    public static ExportBrokerWorkspaceWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportBrokerPackageWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? PackageVersionId { get; init; }
    public int? VersionNumber { get; init; }
    public uint? WorkspaceVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportBrokerPackageWriteResult Success(Guid packageVersionId, int versionNumber, uint workspaceVersion) =>
        new() { Succeeded = true, PackageVersionId = packageVersionId, VersionNumber = versionNumber, WorkspaceVersion = workspaceVersion };

    public static ExportBrokerPackageWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportBrokerCorrectionWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? CorrectionId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportBrokerCorrectionWriteResult Success(Guid correctionId) =>
        new() { Succeeded = true, CorrectionId = correctionId };

    public static ExportBrokerCorrectionWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

// ─── Read models ──────────────────────────────────────────────────────────────

public sealed record ExportPackingUnitReadModel(
    Guid Id,
    Guid ExportCaseId,
    string UnitCode,
    string UnitType,
    Guid? ParentPackingUnitId,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    decimal? Volume,
    string? MarksAndNumbers,
    string Status,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ExportPackingItemReadModel> Items);

public sealed record ExportPackingItemReadModel(
    Guid Id,
    Guid PackingUnitId,
    Guid? SourceOrderLineId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasure,
    string? LotBatchReference,
    DateTimeOffset CreatedAtUtc);

public sealed record ExportPackingReadModel(
    Guid ExportCaseId,
    string PackingStatus,
    DateTimeOffset? PackingCompletedAtUtc,
    string? PackingCompletedBySubjectId,
    DateTimeOffset? PackingVerifiedAtUtc,
    string? PackingVerifiedBySubjectId,
    IReadOnlyList<ExportPackingUnitReadModel> Units);

public sealed record ExportPackingReconciliationReadModel(
    Guid ExportCaseId,
    string ReconciliationStatus,
    string? Explanation,
    int ItemCount,
    decimal TotalQuantity);

public sealed record ExportShipmentReadModel(
    Guid Id,
    Guid ExportCaseId,
    string ShipmentReference,
    string Status,
    string? TransportMode,
    string? IncotermCode,
    string DestinationCountryCode,
    string? DestinationCity,
    string? DeliveryAddress,
    string? ForwarderName,
    string? CarrierName,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    DateTimeOffset? ActualPickupAtUtc,
    DateTimeOffset? ActualDepartureAtUtc,
    DateTimeOffset? ActualArrivalAtUtc,
    string? BookingReference,
    string? TrackingReference,
    decimal? TotalGrossWeight,
    decimal? TotalNetWeight,
    int? TotalPackageCount,
    string? CancellationReason,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ExportBrokerWorkspaceReadModel(
    Guid Id,
    Guid ExportCaseId,
    Guid? ShipmentId,
    string BrokerCompanyName,
    string? BrokerContactName,
    string? BrokerEmail,
    string? BrokerPhone,
    string Status,
    DateTimeOffset? PreparedAtUtc,
    string? PreparedBySubjectId,
    DateTimeOffset? SentAtUtc,
    string? SentBySubjectId,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? BrokerReference,
    string? Notes,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ExportBrokerPackageVersionReadModel> PackageVersions,
    IReadOnlyList<ExportBrokerCorrectionReadModel> Corrections);

public sealed record ExportBrokerPackageVersionReadModel(
    Guid Id,
    int VersionNumber,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc,
    string? SentBySubjectId,
    IReadOnlyList<ExportBrokerPackageDocumentReadModel> Documents);

public sealed record ExportBrokerPackageDocumentReadModel(
    Guid Id,
    Guid DocumentId,
    string DocumentType,
    string DocumentStatusSnapshot,
    string DisplayNameSnapshot,
    bool Included);

public sealed record ExportBrokerCorrectionReadModel(
    Guid Id,
    Guid BrokerWorkspaceId,
    string Category,
    string Title,
    string Description,
    string Severity,
    string Status,
    Guid? RelatedDocumentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolvedBySubjectId,
    string? ResolutionNote);

public sealed record ExportShipmentReadinessReadModel(
    Guid ShipmentId,
    string ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    IReadOnlyList<ExportRequirementEvaluation> Requirements);

// ─── Query results ────────────────────────────────────────────────────────────

public sealed record ExportPackingResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportPackingReadModel? Packing { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportPackingResult Success(ExportPackingReadModel model) =>
        new() { Succeeded = true, Packing = model };

    public static ExportPackingResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportPackingResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportPackingReconciliationResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportPackingReconciliationReadModel? Reconciliation { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportPackingReconciliationResult Success(ExportPackingReconciliationReadModel model) =>
        new() { Succeeded = true, Reconciliation = model };

    public static ExportPackingReconciliationResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportPackingReconciliationResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportShipmentListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ExportShipmentReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportShipmentListResult Success(IReadOnlyList<ExportShipmentReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportShipmentListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportShipmentDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportShipmentReadModel? Shipment { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportShipmentDetailResult Success(ExportShipmentReadModel model) =>
        new() { Succeeded = true, Shipment = model };

    public static ExportShipmentDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = Export2LFailureCodes.ShipmentNotFound };

    public static ExportShipmentDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportShipmentReadinessResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportShipmentReadinessReadModel? Readiness { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportShipmentReadinessResult Success(ExportShipmentReadinessReadModel model) =>
        new() { Succeeded = true, Readiness = model };

    public static ExportShipmentReadinessResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = Export2LFailureCodes.ShipmentNotFound };

    public static ExportShipmentReadinessResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportBrokerWorkspaceResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportBrokerWorkspaceReadModel? Workspace { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportBrokerWorkspaceResult Success(ExportBrokerWorkspaceReadModel model) =>
        new() { Succeeded = true, Workspace = model };

    public static ExportBrokerWorkspaceResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = Export2LFailureCodes.BrokerWorkspaceNotFound };

    public static ExportBrokerWorkspaceResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
