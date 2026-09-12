namespace NevaxisFactOS.Persistence.ProductionPlanning;

public static class ProductionPlanReadFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string InvalidRequest = "invalid-request";
    public const string NotFound = "not-found";
    public const string SourceUnavailable = "source-unavailable";
    public const string InvalidRow = "invalid-row";
    public const string DataIntegrity = "data-integrity";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ProductionPlanReadFailure(
    string Code,
    string InternalReason)
{
    public static ProductionPlanReadFailure InvalidScope(string reason) =>
        new(ProductionPlanReadFailureCodes.InvalidScope, reason);

    public static ProductionPlanReadFailure InvalidRequest(string reason) =>
        new(ProductionPlanReadFailureCodes.InvalidRequest, reason);

    public static ProductionPlanReadFailure NotFound(string reason) =>
        new(ProductionPlanReadFailureCodes.NotFound, reason);

    public static ProductionPlanReadFailure SourceUnavailable(string reason) =>
        new(ProductionPlanReadFailureCodes.SourceUnavailable, reason);

    public static ProductionPlanReadFailure InvalidRow(string reason) =>
        new(ProductionPlanReadFailureCodes.InvalidRow, reason);

    public static ProductionPlanReadFailure DataIntegrity(string reason) =>
        new(ProductionPlanReadFailureCodes.DataIntegrity, reason);

    public static ProductionPlanReadFailure UnexpectedError(string reason) =>
        new(ProductionPlanReadFailureCodes.UnexpectedError, reason);
}
