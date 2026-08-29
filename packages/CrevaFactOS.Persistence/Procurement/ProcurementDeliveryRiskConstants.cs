namespace CrevaFactOS.Persistence.Procurement;

/// <summary>
/// Authoritative supplier delivery risk boundary derived from ERP purchase order projections.
/// Does not create purchase orders, supplier messages, stock actions or production changes.
/// </summary>
public static class ProcurementDeliveryRiskConstants
{
    public const string RuleSetVersion = "PROCUREMENT-DELIVERY-RISK-V1";

    public const string WorkspaceDisclaimer =
        "Bu ekran yalnızca tedarik teslimat riskini gösterir; otomatik satın alma, tedarikçi iletişimi, stok veya üretim işlemi yapmaz.";
}

public static class ProcurementDeliveryRiskStatuses
{
    public const string OnTrack = "on-track";
    public const string DeliveryRisk = "delivery-risk";
    public const string Overdue = "overdue";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        OnTrack,
        DeliveryRisk,
        Overdue,
        InsufficientData,
        SourceConflict,
    };
}

public static class ProcurementDeliveryRiskDriverCodes
{
    public const string PurchaseOrderMissing = "purchase-order-missing";
    public const string PurchaseOrderLineMissing = "purchase-order-line-missing";
    public const string CommitmentDateMissing = "commitment-date-missing";
    public const string OpenQuantityUnknown = "open-quantity-unknown";
    public const string OrderClosedOrCancelled = "order-closed-or-cancelled";
    public const string ErpSourceStale = "erp-source-stale";
    public const string ErpSourceUnknown = "erp-source-unknown";
    public const string CommitmentDateConflict = "commitment-date-conflict";
    public const string PartialReceiptOpenQuantity = "partial-receipt-open-quantity";
    public const string OverdueOpenQuantity = "overdue-open-quantity";
    public const string OnTrackOpenCommitment = "on-track-open-commitment";
}

public static class ProcurementDeliveryRiskFailureCodes
{
    public const string NotFound = "not-found";
    public const string ValidationFailed = "validation-failed";
    public const string AssessmentConcurrencyConflict = "assessment-concurrency-conflict";
    public const string InvalidTransition = "invalid-transition";
    public const string SourceUnavailable = "source-unavailable";
}
