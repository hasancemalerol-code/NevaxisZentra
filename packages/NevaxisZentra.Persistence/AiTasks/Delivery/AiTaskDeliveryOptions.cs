namespace NevaxisZentra.Persistence.AiTasks.Delivery;

public sealed class AiTaskDeliveryOptions
{
    public const string SectionName = "AiTaskDelivery";

    public bool WorkerEnabled { get; set; } = true;

    public int LeaseSeconds { get; set; } = AiTaskDeliveryConstants.DefaultLeaseSeconds;

    public int RetryBaseDelaySeconds { get; set; } = 5;

    public bool EnableClosureTtlPolicies { get; set; }
}
