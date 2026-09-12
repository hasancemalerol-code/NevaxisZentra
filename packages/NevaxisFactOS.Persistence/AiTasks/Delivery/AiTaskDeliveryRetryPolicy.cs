namespace NevaxisFactOS.Persistence.AiTasks.Delivery;

public static class AiTaskDeliveryRetryPolicy
{
    public static DateTimeOffset ComputeNextAttemptUtc(
        int attemptCount,
        DateTimeOffset nowUtc,
        int retryBaseDelaySeconds = 5)
    {
        var cappedAttempt = Math.Min(attemptCount, 6);
        var baseDelay = retryBaseDelaySeconds < 1 ? 5 : retryBaseDelaySeconds;
        var delaySeconds = Math.Min(3600, Math.Pow(2, cappedAttempt) * baseDelay);
        return nowUtc.AddSeconds(delaySeconds);
    }

    public static string MapOutcomeToErrorCategory(string outcome) => outcome switch
    {
        AiTaskDeliveryAttemptOutcomes.TransientFailure => AiTaskDeliveryErrorCategories.Transient,
        AiTaskDeliveryAttemptOutcomes.PermanentFailure => AiTaskDeliveryErrorCategories.Permanent,
        AiTaskDeliveryAttemptOutcomes.ConfigurationFailure => AiTaskDeliveryErrorCategories.Configuration,
        _ => AiTaskDeliveryErrorCategories.Permanent,
    };
}
