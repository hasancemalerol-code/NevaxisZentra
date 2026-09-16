namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforceOperationalLimitEvaluation(
    bool Allowed,
    bool RequiresEscalation,
    string? Reason);

public static class DigitalWorkforceOperationalLimitEngine
{
    public const int DefaultMaxActionsPerHour = 120;
    public const int DefaultMaxExternalWritesPerHour = 30;
    public const int DefaultMaxConcurrentExecutions = 5;

    public static DigitalWorkforceOperationalLimitEvaluation Evaluate(
        int actionsStartedLastHour,
        int externalWritesLastHour,
        int runningExecutions,
        int maxActionsPerHour = DefaultMaxActionsPerHour,
        int maxExternalWritesPerHour = DefaultMaxExternalWritesPerHour,
        int maxConcurrentExecutions = DefaultMaxConcurrentExecutions,
        string? actionClassification = null)
    {
        if (runningExecutions >= maxConcurrentExecutions)
        {
            return new(false, true, "Eşzamanlı aksiyon limiti aşıldı.");
        }

        if (actionsStartedLastHour >= maxActionsPerHour)
        {
            return new(false, true, "Saatlik aksiyon limiti aşıldı.");
        }

        if (actionClassification is DigitalWorkActionClassifications.WriteExternal &&
            externalWritesLastHour >= maxExternalWritesPerHour)
        {
            return new(false, true, "Saatlik dış yazma limiti aşıldı.");
        }

        return new(true, false, null);
    }
}
