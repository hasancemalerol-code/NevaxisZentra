using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrevaFactOS.Persistence.Integration.Mapping;

public sealed class IntegrationMappingDefinition
{
    public string TargetCanonicalEntityType { get; set; } = IntegrationCanonicalProjectionTypes.Material;
    public string SourceEntityType { get; set; } = "materials";
    public string SourceKeyField { get; set; } = "code";
    public List<IntegrationFieldMappingRule> FieldMappings { get; set; } = [];
    public Dictionary<string, string> Defaults { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static IntegrationMappingDefinition? TryParse(string mappingJson)
    {
        if (string.IsNullOrWhiteSpace(mappingJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IntegrationMappingDefinition>(
                mappingJson,
                IntegrationMappingJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, IntegrationMappingJson.Options);
}

public sealed class IntegrationFieldMappingRule
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string TargetType { get; set; } = IntegrationMappingTargetTypes.Text;
    public string? Transform { get; set; }
    public string? DefaultValue { get; set; }
    public string NullBehavior { get; set; } = IntegrationMappingNullBehaviors.Reject;
    public Dictionary<string, string> EnumMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class IntegrationMappingTargetTypes
{
    public const string Text = "string";
    public const string Number = "decimal";
    public const string Date = "date";
    public const string Bool = "bool";
    public const string Enum = "enum";
}

public static class IntegrationMappingNullBehaviors
{
    public const string Reject = "reject";
    public const string Default = "default";
    public const string Skip = "skip";
}

public sealed record IntegrationMappingValidationIssue(
    string Code,
    string Message,
    string? Field = null,
    string Severity = "error");

public sealed record IntegrationMappingValidationResult(
    bool IsValid,
    IReadOnlyList<IntegrationMappingValidationIssue> Issues)
{
    public static IntegrationMappingValidationResult Valid() =>
        new(true, []);

    public static IntegrationMappingValidationResult Invalid(IReadOnlyList<IntegrationMappingValidationIssue> issues) =>
        new(false, issues);
}

public sealed record IntegrationMappedRowResult(
    int RowNumber,
    string SourceRecordId,
    bool IsValid,
    IReadOnlyDictionary<string, string?> MappedValues,
    IReadOnlyList<IntegrationMappingValidationIssue> Issues,
    string ContentFingerprint);

internal static class IntegrationMappingJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}
