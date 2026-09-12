namespace NevaxisFactOS.Persistence.ProductionPlanning;

public static class ProductionPlanValueConversions
{
    private static readonly Dictionary<ProductionPlanStatus, string> StatusValues =
        new()
        {
            [ProductionPlanStatus.Draft] = "draft",
            [ProductionPlanStatus.NeedsReview] = "needs-review",
            [ProductionPlanStatus.ReadyForApproval] = "ready-for-approval",
            [ProductionPlanStatus.Approved] = "approved",
            [ProductionPlanStatus.Rejected] = "rejected",
            [ProductionPlanStatus.Scheduled] = "scheduled",
            [ProductionPlanStatus.InProgress] = "in-progress",
            [ProductionPlanStatus.Completed] = "completed",
            [ProductionPlanStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<ProductionPlanOrigin, string> OriginValues =
        new()
        {
            [ProductionPlanOrigin.ApprovedOrder] = "approved-order",
            [ProductionPlanOrigin.ErpProductionRequest] = "erp-production-request",
            [ProductionPlanOrigin.Manual] = "manual",
            [ProductionPlanOrigin.AiRecommendation] = "ai-recommendation",
        };

    private static readonly Dictionary<ProductionPlanConfidence, string> ConfidenceValues =
        new()
        {
            [ProductionPlanConfidence.Low] = "low",
            [ProductionPlanConfidence.Medium] = "medium",
            [ProductionPlanConfidence.High] = "high",
        };

    private static readonly Dictionary<ProductionPlanApprovalStatus, string> ApprovalStatusValues =
        new()
        {
            [ProductionPlanApprovalStatus.NotRequired] = "not-required",
            [ProductionPlanApprovalStatus.Pending] = "pending",
            [ProductionPlanApprovalStatus.Approved] = "approved",
            [ProductionPlanApprovalStatus.Rejected] = "rejected",
            [ProductionPlanApprovalStatus.Expired] = "expired",
        };

    private static readonly Dictionary<ProductionPlanTransferStatus, string> TransferStatusValues =
        new()
        {
            [ProductionPlanTransferStatus.NotStarted] = "not-started",
            [ProductionPlanTransferStatus.Pending] = "pending",
            [ProductionPlanTransferStatus.InProgress] = "in-progress",
            [ProductionPlanTransferStatus.Completed] = "completed",
            [ProductionPlanTransferStatus.Failed] = "failed",
            [ProductionPlanTransferStatus.Uncertain] = "uncertain",
        };

    private static readonly Dictionary<ProductionPlanTargetSystem, string> TargetSystemValues =
        new()
        {
            [ProductionPlanTargetSystem.Erp] = "erp",
            [ProductionPlanTargetSystem.Mes] = "mes",
            [ProductionPlanTargetSystem.NevaxisFactOS] = "crevafactos",
        };

    public static string ToStorage(ProductionPlanStatus value) =>
        StatusValues[value];

    public static ProductionPlanStatus StatusFromStorage(string value) =>
        FromStorage(value, StatusValues, nameof(ProductionPlanStatus));

    public static string ToStorage(ProductionPlanOrigin value) =>
        OriginValues[value];

    public static ProductionPlanOrigin OriginFromStorage(string value) =>
        FromStorage(value, OriginValues, nameof(ProductionPlanOrigin));

    public static string ToStorage(ProductionPlanConfidence value) =>
        ConfidenceValues[value];

    public static ProductionPlanConfidence ConfidenceFromStorage(string value) =>
        FromStorage(value, ConfidenceValues, nameof(ProductionPlanConfidence));

    public static string ToStorage(ProductionPlanApprovalStatus value) =>
        ApprovalStatusValues[value];

    public static ProductionPlanApprovalStatus ApprovalStatusFromStorage(string value) =>
        FromStorage(value, ApprovalStatusValues, nameof(ProductionPlanApprovalStatus));

    public static string ToStorage(ProductionPlanTransferStatus value) =>
        TransferStatusValues[value];

    public static ProductionPlanTransferStatus TransferStatusFromStorage(string value) =>
        FromStorage(value, TransferStatusValues, nameof(ProductionPlanTransferStatus));

    public static string ToStorage(ProductionPlanTargetSystem value) =>
        TargetSystemValues[value];

    public static ProductionPlanTargetSystem TargetSystemFromStorage(string value) =>
        FromStorage(value, TargetSystemValues, nameof(ProductionPlanTargetSystem));

    private static TEnum FromStorage<TEnum>(
        string value,
        Dictionary<TEnum, string> map,
        string enumName)
        where TEnum : struct, Enum
    {
        foreach (var pair in map)
        {
            if (pair.Value == value)
            {
                return pair.Key;
            }
        }

        throw new InvalidOperationException(
            $"Unsupported {enumName} storage value '{value}'.");
    }
}
