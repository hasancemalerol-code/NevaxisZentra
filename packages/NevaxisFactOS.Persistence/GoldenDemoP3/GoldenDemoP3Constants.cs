namespace NevaxisFactOS.Persistence.GoldenDemoP3;

public static class GoldenDemoP3ValidationStatuses
{
    public const string WaitingForValidation = "waiting-for-validation";
    public const string CollectingData = "collecting-data";
    public const string ReadyForVerification = "ready-for-verification";
    public const string Verified = "verified";
    public const string PartiallyVerified = "partially-verified";
    public const string NotVerified = "not-verified";
    public const string InsufficientData = "insufficient-data";
}

public static class GoldenDemoP3MetricStatuses
{
    public const string Improved = "improved";
    public const string Stable = "stable";
    public const string Regressed = "regressed";
    public const string Unavailable = "unavailable";
}
