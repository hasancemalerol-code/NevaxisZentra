namespace NevaxisZentra.Persistence.AiTasks.Delivery;

public static class AiTaskDeliveryStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions =
        new(StringComparer.Ordinal)
        {
            [AiTaskDeliveryStatuses.Queued] =
            [
                AiTaskDeliveryStatuses.Processing,
                AiTaskDeliveryStatuses.Cancelled,
            ],
            [AiTaskDeliveryStatuses.Processing] =
            [
                AiTaskDeliveryStatuses.Sent,
                AiTaskDeliveryStatuses.FailedRetryable,
                AiTaskDeliveryStatuses.FailedPermanent,
            ],
            [AiTaskDeliveryStatuses.FailedRetryable] =
            [
                AiTaskDeliveryStatuses.Queued,
                AiTaskDeliveryStatuses.DeadLetter,
                AiTaskDeliveryStatuses.Cancelled,
            ],
        };

    public static bool CanTransition(string currentStatus, string nextStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(nextStatus);

    public static void EnsureTransition(string currentStatus, string nextStatus)
    {
        if (!CanTransition(currentStatus, nextStatus))
        {
            throw new InvalidOperationException(
                $"Invalid delivery transition from '{currentStatus}' to '{nextStatus}'.");
        }
    }

    public static bool IsTerminal(string status) => AiTaskDeliveryStatuses.Terminal.Contains(status);
}
