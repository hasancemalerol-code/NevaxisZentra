namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeExecutionFailureCodes
{
    public const string NotFound = "factory-outcome.not-found";
    public const string ConcurrencyConflict = "factory-outcome.concurrency-conflict";
    public const string ApprovalRequired = "factory-outcome.approval-required";
    public const string ExecutionBlocked = "factory-outcome.execution-blocked";
    public const string VerificationFailed = "factory-outcome.verification-failed";
    public const string DuplicateExecution = "factory-outcome.duplicate-execution";
}

public sealed class FactoryOutcomeExecutionException : Exception
{
    public string FailureCode { get; }

    public FactoryOutcomeExecutionException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }
}
