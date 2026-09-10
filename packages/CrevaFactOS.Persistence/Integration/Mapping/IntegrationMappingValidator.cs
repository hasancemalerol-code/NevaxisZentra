namespace CrevaFactOS.Persistence.Integration.Mapping;

public static class IntegrationMappingValidator
{
    private static readonly HashSet<string> SupportedMaterialTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "code", "name", "description", "materialcategory", "polymerfamily",
        "manufacturer", "tradenameorgrade", "colorname", "colorcode",
        "physicalform", "unitofmeasure", "isactive", "sourcetype",
        "sourcesystem", "sourcereference", "externalmaterialcode",
    };

    public static IntegrationMappingValidationResult ValidateDefinition(
        IntegrationMappingDefinition definition,
        IReadOnlyList<string>? sourceHeaders = null)
    {
        var issues = new List<IntegrationMappingValidationIssue>();

        if (string.IsNullOrWhiteSpace(definition.TargetCanonicalEntityType))
        {
            issues.Add(new("target-missing", "Target canonical entity type is required."));
        }

        if (string.IsNullOrWhiteSpace(definition.SourceEntityType))
        {
            issues.Add(new("source-entity-missing", "Source entity type is required."));
        }

        if (string.IsNullOrWhiteSpace(definition.SourceKeyField))
        {
            issues.Add(new("source-key-missing", "Source key field is required."));
        }

        var sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in definition.FieldMappings)
        {
            if (string.IsNullOrWhiteSpace(rule.SourceField))
            {
                issues.Add(new("source-field-missing", "A field mapping is missing source field.", rule.TargetField));
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.TargetField))
            {
                issues.Add(new("target-field-missing", "A field mapping is missing target field.", rule.SourceField));
                continue;
            }

            if (!sourceFields.Add(rule.SourceField))
            {
                issues.Add(new("duplicate-source-field", $"Source field '{rule.SourceField}' is mapped more than once."));
            }

            if (!targetFields.Add(rule.TargetField))
            {
                issues.Add(new("duplicate-target-field", $"Target field '{rule.TargetField}' is mapped more than once."));
            }

            if (sourceHeaders is not null
                && sourceHeaders.Count > 0
                && !sourceHeaders.Contains(rule.SourceField, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(new(
                    "missing-source-column",
                    $"Source column '{rule.SourceField}' is not present in the uploaded file.",
                    rule.SourceField));
            }

            if (!IsSupportedTargetType(rule.TargetType))
            {
                issues.Add(new(
                    "unsupported-target-type",
                    $"Target type '{rule.TargetType}' is not supported.",
                    rule.TargetField));
            }

            if (rule.TargetType == IntegrationMappingTargetTypes.Enum && rule.EnumMappings.Count == 0)
            {
                issues.Add(new(
                    "enum-mapping-missing",
                    $"Enum mapping for '{rule.TargetField}' must define enum mappings.",
                    rule.TargetField));
            }
        }

        ValidateRequiredTargets(definition, targetFields, issues);

        if (sourceHeaders is not null
            && sourceHeaders.Count > 0
            && !sourceHeaders.Contains(definition.SourceKeyField, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new(
                "missing-key-column",
                $"Source key column '{definition.SourceKeyField}' is not present in the uploaded file.",
                definition.SourceKeyField));
        }

        return issues.Count == 0
            ? IntegrationMappingValidationResult.Valid()
            : IntegrationMappingValidationResult.Invalid(issues);
    }

    public static IntegrationMappedRowResult MapRow(
        int rowNumber,
        IReadOnlyDictionary<string, string> sourceRow,
        IntegrationMappingDefinition definition)
    {
        var issues = new List<IntegrationMappingValidationIssue>();
        var mapped = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in definition.FieldMappings)
        {
            sourceRow.TryGetValue(rule.SourceField, out var rawValue);
            rawValue = rawValue?.Trim();

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (rule.Required)
                {
                    issues.Add(new(
                        IntegrationQuarantineReasonCodes.MissingRequiredField,
                        $"Required field '{rule.SourceField}' is missing.",
                        rule.SourceField));
                    continue;
                }

                if (rule.NullBehavior == IntegrationMappingNullBehaviors.Reject && rule.Required)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rule.DefaultValue))
                {
                    mapped[rule.TargetField] = rule.DefaultValue;
                }
                else if (definition.Defaults.TryGetValue(rule.TargetField, out var defaultValue))
                {
                    mapped[rule.TargetField] = defaultValue;
                }

                continue;
            }

            var converted = ConvertValue(rawValue, rule, issues);
            if (converted is not null)
            {
                mapped[rule.TargetField] = converted;
            }
        }

        foreach (var (targetField, defaultValue) in definition.Defaults)
        {
            if (!mapped.ContainsKey(targetField))
            {
                mapped[targetField] = defaultValue;
            }
        }

        if (!mapped.ContainsKey("SourceType"))
        {
            mapped["SourceType"] = "imported";
        }

        var sourceRecordId = sourceRow.TryGetValue(definition.SourceKeyField, out var keyValue)
            ? keyValue?.Trim() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(sourceRecordId))
        {
            issues.Add(new(
                IntegrationQuarantineReasonCodes.MissingRequiredField,
                $"Source key '{definition.SourceKeyField}' is missing.",
                definition.SourceKeyField));
        }

        var fingerprint = IntegrationContentFingerprint.Compute(mapped);
        return new IntegrationMappedRowResult(
            rowNumber,
            sourceRecordId,
            issues.Count == 0,
            mapped,
            issues,
            fingerprint);
    }

    private static void ValidateRequiredTargets(
        IntegrationMappingDefinition definition,
        HashSet<string> targetFields,
        List<IntegrationMappingValidationIssue> issues)
    {
        if (!string.Equals(
                definition.TargetCanonicalEntityType,
                IntegrationCanonicalProjectionTypes.Material,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!targetFields.Contains("code"))
        {
            issues.Add(new("required-target-missing", "Material mapping must include target field 'code'."));
        }

        if (!targetFields.Contains("name"))
        {
            issues.Add(new("required-target-missing", "Material mapping must include target field 'name'."));
        }

        foreach (var rule in definition.FieldMappings)
        {
            if (!SupportedMaterialTargets.Contains(rule.TargetField.Replace("_", string.Empty, StringComparison.Ordinal)))
            {
                issues.Add(new(
                    "unsupported-target-field",
                    $"Target field '{rule.TargetField}' is not supported for material projection.",
                    rule.TargetField));
            }
        }
    }

    private static string? ConvertValue(
        string rawValue,
        IntegrationFieldMappingRule rule,
        List<IntegrationMappingValidationIssue> issues)
    {
        if (rule.Transform is "trim")
        {
            rawValue = rawValue.Trim();
        }
        else if (rule.Transform is "uppercase")
        {
            rawValue = rawValue.ToUpperInvariant();
        }

        switch (rule.TargetType)
        {
            case IntegrationMappingTargetTypes.Text:
                return rawValue;
            case IntegrationMappingTargetTypes.Number:
                if (!decimal.TryParse(rawValue, out _))
                {
                    issues.Add(new(
                        IntegrationQuarantineReasonCodes.MalformedDecimal,
                        $"Value '{rawValue}' is not a valid decimal for '{rule.TargetField}'.",
                        rule.SourceField));
                    return null;
                }

                return rawValue;
            case IntegrationMappingTargetTypes.Date:
                if (!DateTimeOffset.TryParse(rawValue, out _))
                {
                    issues.Add(new(
                        IntegrationQuarantineReasonCodes.MalformedDate,
                        $"Value '{rawValue}' is not a valid date for '{rule.TargetField}'.",
                        rule.SourceField));
                    return null;
                }

                return rawValue;
            case IntegrationMappingTargetTypes.Bool:
                if (!bool.TryParse(rawValue, out _) && rawValue is not ("0" or "1" or "yes" or "no"))
                {
                    issues.Add(new(
                        IntegrationQuarantineReasonCodes.InvalidType,
                        $"Value '{rawValue}' is not a valid boolean for '{rule.TargetField}'.",
                        rule.SourceField));
                    return null;
                }

                return rawValue;
            case IntegrationMappingTargetTypes.Enum:
                if (rule.EnumMappings.TryGetValue(rawValue, out var mapped))
                {
                    return mapped;
                }

                issues.Add(new(
                    IntegrationQuarantineReasonCodes.InvalidEnum,
                    $"Value '{rawValue}' is not mapped for '{rule.TargetField}'.",
                    rule.SourceField));
                return null;
            default:
                issues.Add(new(
                    IntegrationQuarantineReasonCodes.UnsupportedData,
                    $"Unsupported target type '{rule.TargetType}'.",
                    rule.TargetField));
                return null;
        }
    }

    private static bool IsSupportedTargetType(string targetType) =>
        targetType is IntegrationMappingTargetTypes.Text
            or IntegrationMappingTargetTypes.Number
            or IntegrationMappingTargetTypes.Date
            or IntegrationMappingTargetTypes.Bool
            or IntegrationMappingTargetTypes.Enum;
}
