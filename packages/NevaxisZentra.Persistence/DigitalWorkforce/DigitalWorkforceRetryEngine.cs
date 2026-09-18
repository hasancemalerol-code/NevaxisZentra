namespace NevaxisZentra.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforceRetryDecision(
    bool ShouldRetry,
    string ErrorClassification,
    DateTimeOffset? NextRetryAtUtc,
    string Status);

public static class DigitalWorkforceRetryEngine
{
    public static DigitalWorkforceRetryDecision Evaluate(
        DigitalWorkExecution execution,
        string errorMessage,
        string? failureCode,
        DateTimeOffset now)
    {
        var classification = Classify(errorMessage, failureCode);
        if (classification is DigitalWorkExecutionErrorClassifications.Permanent
            or DigitalWorkExecutionErrorClassifications.Policy
            or DigitalWorkExecutionErrorClassifications.Approval
            or DigitalWorkExecutionErrorClassifications.Configuration)
        {
            return new(false, classification, null, DigitalWorkExecutionStatuses.Failed);
        }

        if (classification == DigitalWorkExecutionErrorClassifications.ExternalUnknown)
        {
            return execution.AttemptCount >= execution.MaxAttempts
                ? new(false, classification, null, DigitalWorkExecutionStatuses.Failed)
                : new(true, classification, now.AddMinutes(Math.Min(60, execution.AttemptCount * 5)),
                    DigitalWorkExecutionStatuses.RetryWait);
        }

        if (execution.AttemptCount >= execution.MaxAttempts)
        {
            return new(false, classification, null, DigitalWorkExecutionStatuses.Failed);
        }

        var delayMinutes = Math.Min(30, Math.Pow(2, execution.AttemptCount));
        return new(true, classification, now.AddMinutes(delayMinutes), DigitalWorkExecutionStatuses.RetryWait);
    }

    public static string Classify(string errorMessage, string? failureCode)
    {
        if (string.Equals(failureCode, DigitalWorkforceFailureCodes.PolicyDenied, StringComparison.Ordinal))
        {
            return DigitalWorkExecutionErrorClassifications.Policy;
        }

        if (string.Equals(failureCode, DigitalWorkforceFailureCodes.ApprovalRequired, StringComparison.Ordinal))
        {
            return DigitalWorkExecutionErrorClassifications.Approval;
        }

        if (string.Equals(failureCode, DigitalWorkforceFailureCodes.ValidationFailed, StringComparison.Ordinal))
        {
            return DigitalWorkExecutionErrorClassifications.Configuration;
        }

        if (errorMessage.Contains("unknown", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) &&
            errorMessage.Contains("external", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkExecutionErrorClassifications.ExternalUnknown;
        }

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("503", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("502", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("transient", StringComparison.OrdinalIgnoreCase))
        {
            return DigitalWorkExecutionErrorClassifications.Transient;
        }

        return DigitalWorkExecutionErrorClassifications.Permanent;
    }
}
