using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed class AiTaskSafeProjectionBuilder
{
    public static AiTaskSafeExternalProjection Build(AiTask task, AiTaskSecureAccessGrant grant)
    {
        var assigneeRole = TryParseAssigneeRole(task.AssigneeJson);
        return new AiTaskSafeExternalProjection(
            ProductName: "CrevaFactOS",
            TaskTitle: task.Title,
            SafeSummary: Truncate(task.Summary, 500),
            Priority: task.Priority,
            StatusLabel: task.Status,
            DueAt: task.DueAtUtc,
            FacilityDisplayName: null,
            MachineDisplayName: string.IsNullOrWhiteSpace(task.MachineId) ? null : $"Makine {task.MachineId}",
            AssignedDepartment: assigneeRole,
            PublicReference: task.TaskReference,
            IssuedAt: grant.IssuedAtUtc,
            ExpiresAt: grant.ExpiresAtUtc);
    }

    public static string BuildDeliveryProjectionJson(AiTask task) =>
        JsonSerializer.Serialize(new
        {
            version = AiTaskDeliveryConstants.PayloadVersion,
            taskReference = task.TaskReference,
            title = task.Title,
            summary = Truncate(task.Summary, 300),
            priority = task.Priority,
            status = task.Status,
        });

    private static string? TryParseAssigneeRole(string? assigneeJson)
    {
        if (string.IsNullOrWhiteSpace(assigneeJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(assigneeJson);
            return document.RootElement.TryGetProperty("role", out var role)
                ? role.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
