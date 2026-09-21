using System.Globalization;
using System.Text.Json;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public static class BulkMappingProfileEngine
{
    private static readonly Dictionary<string, string> TableAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["areas"] = "areas",
        ["area"] = "areas",
        ["factory_areas"] = "areas",
        ["alanlar"] = "areas",
        ["lines"] = "lines",
        ["line"] = "lines",
        ["production_lines"] = "lines",
        ["hatlar"] = "lines",
        ["stations"] = "stations",
        ["station"] = "stations",
        ["production_stations"] = "stations",
        ["istasyonlar"] = "stations",
        ["resources"] = "resources",
        ["resource"] = "resources",
        ["production_resources"] = "resources",
        ["kaynaklar"] = "resources",
        ["materials"] = "materials",
        ["material"] = "materials",
        ["malzemeler"] = "materials",
        ["orders"] = "orders",
        ["production_orders"] = "orders",
        ["events"] = "events",
        ["production_events"] = "events",
        ["quality"] = "quality",
        ["maintenance"] = "maintenance",
        ["molds"] = "molds",
        ["tools"] = "molds",
    };

    private static readonly Dictionary<string, string> TableToEntity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["areas"] = BulkCanonicalEntityTypes.ManufacturingArea,
        ["lines"] = BulkCanonicalEntityTypes.ProductionLine,
        ["stations"] = BulkCanonicalEntityTypes.ProductionStation,
        ["resources"] = BulkCanonicalEntityTypes.ManufacturingResource,
        ["materials"] = BulkCanonicalEntityTypes.Material,
        ["orders"] = BulkCanonicalEntityTypes.ProductionOrder,
        ["events"] = BulkCanonicalEntityTypes.ProductionEvent,
        ["quality"] = BulkCanonicalEntityTypes.QualityRecord,
        ["maintenance"] = BulkCanonicalEntityTypes.MaintenanceRecord,
        ["molds"] = BulkCanonicalEntityTypes.MoldTool,
    };

    private static readonly Dictionary<string, Dictionary<string, string>> DefaultColumnAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["areas"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["area_code"] = "code",
                ["area_name"] = "name",
                ["code"] = "code",
                ["name"] = "name",
            },
            ["lines"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["line_code"] = "code",
                ["line_name"] = "name",
                ["area_code"] = "area_code",
                ["code"] = "code",
                ["name"] = "name",
            },
            ["stations"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["station_code"] = "code",
                ["station_name"] = "name",
                ["line_code"] = "line_code",
                ["sequence_order"] = "sequence_order",
                ["station_kind"] = "station_kind",
                ["code"] = "code",
                ["name"] = "name",
            },
            ["resources"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["resource_code"] = "code",
                ["resource_name"] = "name",
                ["station_code"] = "station_code",
                ["resource_type"] = "resource_type",
                ["machine_registry_code"] = "machine_registry_code",
                ["machine_code"] = "machine_registry_code",
                ["manufacturer"] = "manufacturer",
                ["model"] = "model",
                ["status"] = "default_state",
                ["external_id"] = "external_id",
                ["integration_id"] = "external_id",
                ["code"] = "code",
                ["name"] = "name",
            },
            ["materials"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["material_code"] = "code",
                ["material_name"] = "name",
                ["unit_of_measure"] = "unit_of_measure",
                ["code"] = "code",
                ["name"] = "name",
            },
            ["orders"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["order_code"] = "order_code",
                ["product_code"] = "product_code",
                ["line_code"] = "line_code",
                ["quantity"] = "quantity",
            },
            ["events"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["event_id"] = "event_id",
                ["resource_code"] = "resource_code",
                ["event_type"] = "event_type",
                ["occurred_at"] = "occurred_at",
                ["good_qty"] = "good_qty",
            },
        };

    public static string NormalizeTableName(string rawTableName)
    {
        var normalized = Path.GetFileNameWithoutExtension(rawTableName).Trim();
        return TableAliases.GetValueOrDefault(normalized, normalized.ToLowerInvariant());
    }

    public static string InferEntityType(string tableName) =>
        TableToEntity.GetValueOrDefault(NormalizeTableName(tableName), BulkCanonicalEntityTypes.ManufacturingResource);

    public static IReadOnlyList<BulkMappingRule> BuildAutoMappingRules(
        string tableName,
        IReadOnlyList<string> columns,
        string? entityType = null)
    {
        var normalizedTable = NormalizeTableName(tableName);
        var canonicalEntity = entityType ?? InferEntityType(normalizedTable);
        var aliases = DefaultColumnAliases.GetValueOrDefault(normalizedTable) ?? [];
        var rules = new List<BulkMappingRule>();

        foreach (var column in columns)
        {
            if (aliases.TryGetValue(column, out var canonicalField))
            {
                rules.Add(new BulkMappingRule(
                    normalizedTable,
                    column,
                    canonicalEntity,
                    canonicalField,
                    BulkMappingRuleTypes.Direct,
                    null,
                    IsRequiredField(canonicalEntity, canonicalField)));
                continue;
            }

            if (string.Equals(column, "code", StringComparison.OrdinalIgnoreCase)
                || column.EndsWith("_code", StringComparison.OrdinalIgnoreCase))
            {
                var target = column.Equals("code", StringComparison.OrdinalIgnoreCase)
                    ? "code"
                    : column;
                rules.Add(new BulkMappingRule(
                    normalizedTable,
                    column,
                    canonicalEntity,
                    target,
                    BulkMappingRuleTypes.Direct,
                    null,
                    IsRequiredField(canonicalEntity, target)));
            }
        }

        return rules;
    }

    public static Dictionary<string, string?> MapRow(
        string tableName,
        IReadOnlyDictionary<string, string> sourceRow,
        BulkMappingProfileDefinition mapping)
    {
        var normalizedTable = BulkMappingProfileEngine.NormalizeTableName(tableName);
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rules = mapping.Rules.Where(r =>
            string.Equals(BulkMappingProfileEngine.NormalizeTableName(r.SourceTable), normalizedTable, StringComparison.OrdinalIgnoreCase)).ToList();

        if (rules.Count == 0)
        {
            rules = BuildAutoMappingRules(normalizedTable, sourceRow.Keys.ToList()).ToList();
        }

        foreach (var rule in rules)
        {
            if (string.Equals(rule.MappingType, BulkMappingRuleTypes.Ignore, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!sourceRow.TryGetValue(rule.SourceColumn, out var rawValue))
            {
                if (rule.Required)
                {
                    fields[rule.CanonicalField] = null;
                }

                continue;
            }

            fields[rule.CanonicalField] = ApplyRule(rule, rawValue);
        }

        return fields;
    }

    public static string ResolveSourceKey(string tableName, IReadOnlyDictionary<string, string?> fields, int rowIndex)
    {
        foreach (var candidate in new[] { "code", "order_code", "event_id", "material_code", "resource_code", "machine_registry_code" })
        {
            if (fields.TryGetValue(candidate, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return $"{NormalizeTableName(tableName)}-{rowIndex + 1:D6}";
    }

    public static BulkMappingProfileDefinition DeserializeProfile(string mappingJson, string sourceSystem, string sourceVersion) =>
        new(
            sourceSystem,
            sourceVersion,
            string.IsNullOrWhiteSpace(mappingJson)
                ? []
                : JsonSerializer.Deserialize<List<BulkMappingRule>>(mappingJson) ?? []);

    public static string SerializeRules(IReadOnlyList<BulkMappingRule> rules) =>
        JsonSerializer.Serialize(rules);

    private static string? ApplyRule(BulkMappingRule rule, string rawValue)
    {
        if (string.Equals(rule.MappingType, BulkMappingRuleTypes.Default, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(rule.ConversionJson))
        {
            return rule.ConversionJson;
        }

        if (string.Equals(rule.MappingType, BulkMappingRuleTypes.EnumMap, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(rule.ConversionJson))
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(rule.ConversionJson);
            if (map is not null && map.TryGetValue(rawValue, out var mapped))
            {
                return mapped;
            }
        }

        return rawValue?.Trim();
    }

    private static bool IsRequiredField(string entityType, string field)
    {
        return entityType switch
        {
            BulkCanonicalEntityTypes.ManufacturingArea => field is "code" or "name",
            BulkCanonicalEntityTypes.ProductionLine => field is "code" or "name" or "area_code",
            BulkCanonicalEntityTypes.ProductionStation => field is "code" or "name" or "line_code",
            BulkCanonicalEntityTypes.ManufacturingResource => field is "code" or "name" or "station_code" or "resource_type",
            BulkCanonicalEntityTypes.Material => field is "code" or "name",
            BulkCanonicalEntityTypes.ProductionEvent => field is "resource_code" or "event_type" or "occurred_at",
            _ => false,
        };
    }
}
