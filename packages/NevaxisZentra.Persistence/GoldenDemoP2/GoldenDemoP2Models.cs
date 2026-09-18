namespace NevaxisZentra.Persistence.GoldenDemoP2;

public sealed record GoldenDemoResetResult(
    Guid ProductionEventId,
    string MachineCode,
    string PreviousStatus,
    string ResetStatus,
    DateTimeOffset ResetAtUtc);

public sealed record GoldenDemoPresentationStatus(
    Guid ProductionEventId,
    string MachineCode,
    string EventStatus,
    string PresentationStepCode,
    string PresentationStepLabel,
    int StepNumber,
    int TotalSteps,
    bool IsComplete,
    bool CanReset,
    int ProductionRecoveryGoodParts);

public static class GoldenDemoP2FailureCodes
{
    public const string EventNotFound = "golden-demo-p2.event-not-found";
    public const string Unauthorized = "golden-demo-p2.unauthorized";
}

public sealed class GoldenDemoP2Exception : Exception
{
    public string FailureCode { get; }

    public GoldenDemoP2Exception(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
