using System.Text.Json;

namespace NevaxisZentra.Persistence.AiTasks;

/// <summary>
/// Accepting a recommendation applies a real mutation (assign, re-prioritise, escalate, start a playbook). The acceptor
/// therefore needs the permission of the resulting mutation, not only the permission to give feedback, and the edited
/// payload may never retarget the action to a different team or playbook than the one that was recommended.
/// </summary>
public static class AiTaskRecommendationAcceptanceGuard
{
    private static readonly string[] TargetKeys = ["teamChannelId", "playbookId", "playbookKey"];

    /// <summary>Null when the action is advisory only and applies nothing.</summary>
    public static string? RequiredPermission(string actionType) =>
        actionType switch
        {
            AiTaskDecisionRecommendationActionTypes.AssignTeam => AiTaskPermissionKeys.Assign,
            AiTaskDecisionRecommendationActionTypes.SetPriority => AiTaskPermissionKeys.Manage,
            AiTaskDecisionRecommendationActionTypes.Escalate => AiTaskPermissionKeys.Escalate,
            AiTaskDecisionRecommendationActionTypes.StartPlaybook => AiTaskPermissionKeys.PlaybookRun,
            _ => null,
        };

    /// <summary>
    /// An edited payload may adjust values (such as the priority) but must not introduce or change a target reference.
    /// </summary>
    public static bool IsEditedPayloadWithinRecommendation(string storedPayloadJson, string? editedPayloadJson)
    {
        if (string.IsNullOrWhiteSpace(editedPayloadJson))
        {
            return true;
        }

        var stored = Parse(storedPayloadJson);
        var edited = Parse(editedPayloadJson);
        if (edited is null)
        {
            return false;
        }

        foreach (var key in TargetKeys)
        {
            var editedValue = ReadString(edited, key);
            if (editedValue is null)
            {
                continue;
            }

            var storedValue = stored is null ? null : ReadString(stored, key);
            if (!string.Equals(storedValue, editedValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, JsonElement>? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(Dictionary<string, JsonElement> values, string key) =>
        values.TryGetValue(key, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : null;
}
