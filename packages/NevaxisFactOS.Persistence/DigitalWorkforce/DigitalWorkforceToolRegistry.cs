namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforceToolDefinition(
    string Identifier,
    string RequiredPermission,
    string RiskClass,
    bool RequiresIdempotency,
    TimeSpan Timeout,
    bool IsExternal);

public static class DigitalWorkforceToolRegistry
{
    private static readonly Dictionary<string, DigitalWorkforceToolDefinition> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DigitalWorkforceToolCapabilities.ProductionRead] = new(
                DigitalWorkforceToolCapabilities.ProductionRead, "production.read",
                DigitalWorkActionClassifications.Read, false, TimeSpan.FromSeconds(30), false),
            [DigitalWorkforceToolCapabilities.ProductionPlan] = new(
                DigitalWorkforceToolCapabilities.ProductionPlan, "production.manage",
                DigitalWorkActionClassifications.ChangeOperation, true, TimeSpan.FromSeconds(60), true),
            [DigitalWorkforceToolCapabilities.MaintenanceCreate] = new(
                DigitalWorkforceToolCapabilities.MaintenanceCreate, "maintenance.manage",
                DigitalWorkActionClassifications.WriteInternal, true, TimeSpan.FromSeconds(60), false),
            [DigitalWorkforceToolCapabilities.ProcurementDraft] = new(
                DigitalWorkforceToolCapabilities.ProcurementDraft, "procurement.draft",
                DigitalWorkActionClassifications.Prepare, true, TimeSpan.FromSeconds(45), false),
            [DigitalWorkforceToolCapabilities.MaterialRead] = new(
                DigitalWorkforceToolCapabilities.MaterialRead, "material.read",
                DigitalWorkActionClassifications.Read, false, TimeSpan.FromSeconds(30), false),
            [DigitalWorkforceToolCapabilities.TaskCenterCreate] = new(
                DigitalWorkforceToolCapabilities.TaskCenterCreate, "taskcenter.create",
                DigitalWorkActionClassifications.WriteInternal, true, TimeSpan.FromSeconds(30), false),
            [DigitalWorkforceToolCapabilities.TeamsSend] = new(
                DigitalWorkforceToolCapabilities.TeamsSend, "teams.send",
                DigitalWorkActionClassifications.WriteExternal, true, TimeSpan.FromSeconds(45), true),
            [DigitalWorkforceToolCapabilities.IntegrationRead] = new(
                DigitalWorkforceToolCapabilities.IntegrationRead, "integration.read",
                DigitalWorkActionClassifications.Read, false, TimeSpan.FromSeconds(30), false),
        };

    public static bool TryGet(string tool, out DigitalWorkforceToolDefinition definition) =>
        Registry.TryGetValue(tool, out definition!);

    public static DigitalWorkforceToolDefinition Require(string tool) =>
        TryGet(tool, out var definition)
            ? definition
            : throw new DigitalWorkforceException(
                DigitalWorkforceFailureCodes.ValidationFailed,
                $"Unknown tool: {tool}");
}

public static class DigitalWorkforceToolExecutor
{
    public static DigitalWorkforceToolExecutionResult Execute(
        DigitalWorkExecution execution,
        DigitalWorkforceToolDefinition toolDefinition)
    {
        return toolDefinition.Identifier switch
        {
            DigitalWorkforceToolCapabilities.ProductionPlan => new(
                DigitalWorkExternalActionStatuses.Acknowledged,
                $"plan-{execution.Id:N}",
                """{"priorityAdjusted":true,"restoredPrevious":false}""",
                "production-plan-applied",
                false,
                null),
            DigitalWorkforceToolCapabilities.ProcurementDraft => new(
                DigitalWorkExternalActionStatuses.Acknowledged,
                $"proc-{execution.Id:N}",
                """{"reservationDrafted":true}""",
                "material-reservation-draft",
                false,
                $$"""{"action":"restore-reservation","executionId":"{{execution.Id:N}}"}"""),
            DigitalWorkforceToolCapabilities.MaintenanceCreate => new(
                DigitalWorkExternalActionStatuses.Acknowledged,
                $"wo-{execution.Id:N}",
                """{"workOrderCreated":true}""",
                "maintenance-work-order",
                false,
                null),
            DigitalWorkforceToolCapabilities.TeamsSend => new(
                DigitalWorkExternalActionStatuses.Sent,
                $"msg-{execution.Id:N}",
                """{"notificationSent":true}""",
                "teams-notification",
                true,
                null),
            _ => new(
                DigitalWorkExternalActionStatuses.Acknowledged,
                $"int-{execution.Id:N}",
                """{"completed":true}""",
                "internal-action",
                false,
                null),
        };
    }
}

public sealed record DigitalWorkforceToolExecutionResult(
    string ExternalActionStatus,
    string ExternalReferenceId,
    string ResultJson,
    string EvidenceReference,
    bool IsIrreversible,
    string? CompensationActionJson);
