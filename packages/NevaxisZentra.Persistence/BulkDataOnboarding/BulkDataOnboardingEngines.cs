using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.ContextKnowledge;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public static class BulkSchemaProfilerEngine
{
    private static readonly (Regex Pattern, string Role)[] RoleHeuristics =
    [
        (new Regex(@"(machine|resource|equipment|press|mold)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.MachinesResources),
        (new Regex(@"(order|work.?order|production.?order)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.ProductionOrders),
        (new Regex(@"(product|sku|item|part)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Products),
        (new Regex(@"(event|history|telemetry|signal)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.EventsHistory),
        (new Regex(@"(material|raw.?material|polymer)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Materials),
        (new Regex(@"(quality|inspection|ncr|defect)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Quality),
        (new Regex(@"(maintenance|work.?order.?maint)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Maintenance),
        (new Regex(@"(mold|tool|die)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.MoldsTools),
        (new Regex(@"(operator|employee|worker)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Operators),
        (new Regex(@"(shift|vardiya)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.Shifts),
        (new Regex(@"(line|station|area|hat)", RegexOptions.IgnoreCase), BulkSchemaRoleHints.LinesStations),
    ];

    public static string InferRoleHint(string tableName, IReadOnlyList<BulkColumnDescriptor> columns)
    {
        foreach (var (pattern, role) in RoleHeuristics)
        {
            if (pattern.IsMatch(tableName))
            {
                return role;
            }
        }

        if (columns.Any(c => c.Name.Contains("station", StringComparison.OrdinalIgnoreCase)))
        {
            return BulkSchemaRoleHints.LinesStations;
        }

        return BulkSchemaRoleHints.Unknown;
    }
}

public static class BulkDataQualityGateEngine
{
    public static BulkDataQualityResult Evaluate(
        BulkMappedRecord record,
        Guid expectedTenantId,
        Guid expectedFacilityId,
        IReadOnlyDictionary<string, string> requiredFieldsByEntity,
        DateTimeOffset nowUtc)
    {
        var issues = new List<BulkDataQualityIssue>();

        if (!requiredFieldsByEntity.TryGetValue(record.CanonicalEntityType, out var requiredCsv))
        {
            issues.Add(new BulkDataQualityIssue(
                BulkDataQualityReasonCodes.InvalidMapping,
                "Unknown canonical entity type.",
                BulkQuarantineSeverities.Critical));
            return new BulkDataQualityResult(false, issues);
        }

        foreach (var field in requiredCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!record.Fields.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new BulkDataQualityIssue(
                    BulkDataQualityReasonCodes.MissingRequiredKey,
                    $"Required field '{field}' is missing.",
                    BulkQuarantineSeverities.Critical));
            }
        }

        if (record.Fields.TryGetValue("tenant_id", out var tenantRaw)
            && Guid.TryParse(tenantRaw, out var tenantId)
            && tenantId != expectedTenantId)
        {
            issues.Add(new BulkDataQualityIssue(
                BulkDataQualityReasonCodes.TenantMismatch,
                "Tenant mismatch detected.",
                BulkQuarantineSeverities.Critical));
        }

        if (record.Fields.TryGetValue("facility_id", out var facilityRaw)
            && Guid.TryParse(facilityRaw, out var facilityId)
            && facilityId != expectedFacilityId)
        {
            issues.Add(new BulkDataQualityIssue(
                BulkDataQualityReasonCodes.FacilityMismatch,
                "Facility mismatch detected.",
                BulkQuarantineSeverities.Critical));
        }

        if (record.SourceTimestampUtc is { } ts)
        {
            if (ts > nowUtc.AddHours(1))
            {
                issues.Add(new BulkDataQualityIssue(
                    BulkDataQualityReasonCodes.FutureTimestamp,
                    "Source timestamp is in the future.",
                    BulkQuarantineSeverities.Warning));
            }

            if (nowUtc - ts > TimeSpan.FromDays(365 * 5))
            {
                issues.Add(new BulkDataQualityIssue(
                    BulkDataQualityReasonCodes.StaleRecord,
                    "Source timestamp is excessively stale.",
                    BulkQuarantineSeverities.Info));
            }
        }
        else if (!string.IsNullOrWhiteSpace(record.SourceTimestampRaw))
        {
            issues.Add(new BulkDataQualityIssue(
                BulkDataQualityReasonCodes.InvalidTimestamp,
                "Source timestamp could not be normalized.",
                BulkQuarantineSeverities.Warning));
        }

        foreach (var (key, value) in record.Fields)
        {
            if (key.EndsWith("_qty", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(value)
                && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                issues.Add(new BulkDataQualityIssue(
                    BulkDataQualityReasonCodes.InvalidNumber,
                    $"Field '{key}' is not a valid number.",
                    BulkQuarantineSeverities.Warning));
            }
        }

        return new BulkDataQualityResult(issues.Count == 0, issues);
    }
}

public static class BulkEntityResolutionEngine
{
    public static BulkEntityResolutionResult ResolveExact(
        string sourceCode,
        IReadOnlyDictionary<string, Guid> knownCodes)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return new BulkEntityResolutionResult(
                BulkEntityResolutionOutcomes.Unresolved,
                null,
                null,
                []);
        }

        var normalized = sourceCode.Trim();
        var matches = knownCodes
            .Where(k => string.Equals(k.Key, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
        {
            return new BulkEntityResolutionResult(
                BulkEntityResolutionOutcomes.Exact,
                matches[0].Key,
                matches[0].Value,
                []);
        }

        if (matches.Count > 1)
        {
            return new BulkEntityResolutionResult(
                BulkEntityResolutionOutcomes.Ambiguous,
                normalized,
                null,
                matches.Select(m => m.Key).ToList());
        }

        if (knownCodes.TryGetValue(normalized, out var mappedId))
        {
            return new BulkEntityResolutionResult(
                BulkEntityResolutionOutcomes.Mapped,
                normalized,
                mappedId,
                []);
        }

        return new BulkEntityResolutionResult(
            BulkEntityResolutionOutcomes.Unresolved,
            normalized,
            null,
            []);
    }
}

public static class BulkTimeNormalizationEngine
{
    private static readonly System.Text.RegularExpressions.Regex OffsetSuffix =
        new(@"[+-]\d{2}:?\d{2}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static (DateTimeOffset? NormalizedUtc, bool Ambiguous) Normalize(
        string? rawValue,
        string? declaredTimeZone = null)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return (null, false);
        }

        if (HasExplicitTimeZone(rawValue)
            && DateTimeOffset.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
                out var utcDirect))
        {
            return (utcDirect.ToUniversalTime(), false);
        }

        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            if (!string.IsNullOrWhiteSpace(declaredTimeZone)
                && TimeZoneInfo.TryFindSystemTimeZoneById(declaredTimeZone, out var tz))
            {
                var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
                return (TimeZoneInfo.ConvertTimeToUtc(unspecified, tz), false);
            }

            return (null, true);
        }

        return (null, true);
    }

    private static bool HasExplicitTimeZone(string rawValue) =>
        rawValue.Contains('Z', StringComparison.OrdinalIgnoreCase)
        || OffsetSuffix.IsMatch(rawValue.Trim());
}

public static class BulkUnitNormalizationEngine
{
    private static readonly Dictionary<string, decimal> MsToSeconds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ms"] = 0.001m,
        ["sec"] = 1m,
        ["s"] = 1m,
        ["min"] = 60m,
    };

    private static readonly Dictionary<string, decimal> MassToKg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = 0.001m,
        ["kg"] = 1m,
    };

    public static bool TryConvertDuration(string value, string fromUnit, string toUnit, out decimal converted)
    {
        converted = 0m;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric))
        {
            return false;
        }

        if (!MsToSeconds.TryGetValue(fromUnit, out var fromFactor)
            || !MsToSeconds.TryGetValue(toUnit, out var toFactor))
        {
            return false;
        }

        converted = numeric * fromFactor / toFactor;
        return true;
    }

    public static bool TryConvertMass(string value, string fromUnit, string toUnit, out decimal converted)
    {
        converted = 0m;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric))
        {
            return false;
        }

        if (!MassToKg.TryGetValue(fromUnit, out var fromFactor)
            || !MassToKg.TryGetValue(toUnit, out var toFactor))
        {
            return false;
        }

        converted = numeric * fromFactor / toFactor;
        return true;
    }
}

public static class BulkImpactPreviewEngine
{
    public static BulkImpactPreview Build(
        IReadOnlyList<BulkImportStagingRecord> staging,
        IReadOnlySet<string> existingKeys)
    {
        var valid = staging.Where(s => s.ValidationState == BulkStagingValidationStates.Valid).ToList();
        var quarantined = staging.Count(s => s.ValidationState == BulkStagingValidationStates.Quarantined);
        var duplicates = staging.Count(s => s.ValidationState == BulkStagingValidationStates.Duplicate);

        return new BulkImpactPreview(
            CountCreates(valid, BulkCanonicalEntityTypes.ManufacturingArea, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.ProductionLine, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.ProductionStation, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.ManufacturingResource, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.Material, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.ProductionOrder, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.ProductionEvent, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.QualityRecord, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.MaintenanceRecord, existingKeys),
            CountCreates(valid, BulkCanonicalEntityTypes.MoldTool, existingKeys),
            CountUpdates(valid, existingKeys),
            CountUpdates(valid, existingKeys),
            staging.Count(s => s.ValidationState == BulkStagingValidationStates.Invalid),
            quarantined,
            staging.Count(s => s.ValidationState == BulkStagingValidationStates.Invalid));
    }

    private static int CountCreates(
        IReadOnlyList<BulkImportStagingRecord> valid,
        string entityType,
        IReadOnlySet<string> existingKeys) =>
        valid.Count(s =>
            s.CanonicalEntityType == entityType
            && !existingKeys.Contains(BuildEntityKey(s)));

    private static int CountUpdates(
        IReadOnlyList<BulkImportStagingRecord> valid,
        IReadOnlySet<string> existingKeys) =>
        valid.Count(s => existingKeys.Contains(BuildEntityKey(s)));

    private static string BuildEntityKey(BulkImportStagingRecord record) =>
        $"{record.CanonicalEntityType}:{record.SourceKey}";
}

public static class BulkReconciliationEngine
{
    public static BulkReconciliationSummary Build(BulkDataImportJob job) =>
        new(
            job.TotalSourceRows,
            job.StagedRows,
            job.ValidRows,
            job.ImportedRows,
            job.UpdatedRows,
            job.DuplicateRows,
            job.QuarantinedRows,
            job.FailedRows,
            job.TotalSourceRows == job.StagedRows
                && job.StagedRows == job.ValidRows + job.InvalidRows + job.QuarantinedRows + job.DuplicateRows);
}

public static class BulkHistoricalTierEngine
{
    public static string Classify(DateTimeOffset? sourceTimestampUtc, DateTimeOffset nowUtc)
    {
        if (sourceTimestampUtc is null)
        {
            return BulkHistoricalDataTiers.Hot;
        }

        var age = nowUtc - sourceTimestampUtc.Value;
        return age <= TimeSpan.FromDays(30)
            ? BulkHistoricalDataTiers.Hot
            : age <= TimeSpan.FromDays(365)
                ? BulkHistoricalDataTiers.Historical
                : BulkHistoricalDataTiers.Archive;
    }
}

public static class BulkIdempotencyEngine
{
    public static string BuildRecordChecksum(IReadOnlyDictionary<string, string?> fields)
    {
        var canonical = JsonSerializer.Serialize(fields.OrderBy(k => k.Key));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    public static string BuildImportIdempotencyKey(
        string sourceSystem,
        string sourceTable,
        string sourceKey,
        string recordChecksum) =>
        $"bulk:{sourceSystem}:{sourceTable}:{sourceKey}:{recordChecksum}";
}

public static class BulkFileSafetyValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv", ".xlsx", ".json",
    };

    public static string? ValidateFile(string fileName, long fileSizeBytes, long maxBytes = 50 * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..", StringComparison.Ordinal))
        {
            return "Path traversal rejected.";
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return "Unsupported file extension.";
        }

        if (fileSizeBytes <= 0 || fileSizeBytes > maxBytes)
        {
            return "File size is out of allowed bounds.";
        }

        return null;
    }
}

public static class BulkKnowledgeSafetyGuard
{
    public static bool BlocksAutoPromotion(bool hasVerifiedOutcome) =>
        KnowledgePromotionGate.Evaluate(new KnowledgePromotionRequest(
            Guid.NewGuid(),
            "imported-historical",
            string.Empty,
            string.Empty,
            [],
            [],
            "{}",
            "{}",
            string.Empty,
            HasVerifiedOutcome: hasVerifiedOutcome,
            HasSufficientEvidence: false,
            HasUnresolvedConflict: false,
            ObservationCompleted: false,
            Confidence: 0m,
            ApplicabilityConstraintsJson: "{}")).PromotionStatus != ContextKnowledgePromotionStatuses.Verified;
}
