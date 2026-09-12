using NevaxisFactOS.Persistence.Integration.Mapping;

namespace NevaxisFactOS.Persistence.Integration.FileImport;

public sealed record IntegrationFileRowPreview(
    int RowNumber,
    string SourceRecordId,
    string Status,
    IReadOnlyList<IntegrationMappingValidationIssue> Issues);

public sealed record IntegrationFileDryRunResult(
    bool Success,
    string FileKind,
    string SchemaFingerprint,
    bool SchemaCompatible,
    IReadOnlyList<string> SchemaWarnings,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int DuplicateRows,
    int NewProjections,
    int Updates,
    int Unchanged,
    int QuarantineCandidates,
    IReadOnlyList<IntegrationFileRowPreview> RowPreviews,
    string? ErrorCode,
    string? SafeMessage);

public static class IntegrationFileDryRunEngine
{
    public static IntegrationFileDryRunResult Execute(
        IntegrationFileTable table,
        IntegrationMappingDefinition definition,
        string activeSchemaFingerprint,
        IReadOnlyDictionary<string, IntegrationSourceIdentity> existingIdentities,
        bool allowSupersetSchema = false)
    {
        var fingerprint = IntegrationSchemaFingerprint.Compute(table.Headers);
        var expectedHeaders = CollectExpectedSourceHeaders(definition);
        var headerComparison = IntegrationSchemaFingerprint.CompareHeaders(expectedHeaders, table.Headers);

        var schemaWarnings = new List<string>();
        var schemaCompatible = string.IsNullOrWhiteSpace(activeSchemaFingerprint)
            || string.Equals(activeSchemaFingerprint, fingerprint, StringComparison.Ordinal)
            || (allowSupersetSchema && expectedHeaders.Count > 0 && headerComparison.IsCompatible);
        if (!schemaCompatible)
        {
            schemaWarnings.Add("Source schema fingerprint differs from the active mapping.");
        }
        else if (!string.Equals(activeSchemaFingerprint, fingerprint, StringComparison.Ordinal)
            && headerComparison.AddedFields.Count > 0)
        {
            schemaWarnings.Add("Source includes additional columns beyond the mapped schema.");
        }

        if (!schemaCompatible)
        {
            return new IntegrationFileDryRunResult(
                false,
                table.FileKind,
                fingerprint,
                false,
                schemaWarnings,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                [],
                "integration.schema-drift",
                "Active mapping is incompatible with the uploaded file schema.");
        }

        var mappingValidation = IntegrationMappingValidator.ValidateDefinition(definition, table.Headers);
        if (!mappingValidation.IsValid)
        {
            return new IntegrationFileDryRunResult(
                false,
                table.FileKind,
                fingerprint,
                false,
                schemaWarnings,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                [],
                "integration.mapping.validation-failed",
                mappingValidation.Issues[0].Message);
        }

        var previews = new List<IntegrationFileRowPreview>();
        var validRows = 0;
        var invalidRows = 0;
        var duplicateRows = 0;
        var newProjections = 0;
        var updates = 0;
        var unchanged = 0;
        var quarantineCandidates = 0;
        var seenSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < table.Rows.Count; index++)
        {
            var rowNumber = index + 1;
            var mapped = IntegrationMappingValidator.MapRow(rowNumber, table.Rows[index], definition);
            if (!mapped.IsValid)
            {
                invalidRows++;
                quarantineCandidates++;
                previews.Add(new IntegrationFileRowPreview(
                    rowNumber,
                    mapped.SourceRecordId,
                    "invalid",
                    mapped.Issues));
                continue;
            }

            if (!seenSourceIds.Add(mapped.SourceRecordId))
            {
                duplicateRows++;
                quarantineCandidates++;
                previews.Add(new IntegrationFileRowPreview(
                    rowNumber,
                    mapped.SourceRecordId,
                    "duplicate-in-file",
                    [new IntegrationMappingValidationIssue(
                        IntegrationQuarantineReasonCodes.DuplicateConflict,
                        "Duplicate source identity within file.",
                        definition.SourceKeyField)]));
                continue;
            }

            validRows++;
            if (existingIdentities.TryGetValue(mapped.SourceRecordId, out var existing))
            {
                if (string.Equals(existing.ContentFingerprint, mapped.ContentFingerprint, StringComparison.Ordinal))
                {
                    unchanged++;
                    previews.Add(new IntegrationFileRowPreview(rowNumber, mapped.SourceRecordId, "unchanged", []));
                }
                else
                {
                    updates++;
                    previews.Add(new IntegrationFileRowPreview(rowNumber, mapped.SourceRecordId, "update", []));
                }
            }
            else
            {
                newProjections++;
                previews.Add(new IntegrationFileRowPreview(rowNumber, mapped.SourceRecordId, "new", []));
            }
        }

        return new IntegrationFileDryRunResult(
            true,
            table.FileKind,
            fingerprint,
            schemaCompatible,
            schemaWarnings,
            table.Rows.Count,
            validRows,
            invalidRows,
            duplicateRows,
            newProjections,
            updates,
            unchanged,
            quarantineCandidates,
            previews.Take(IntegrationFileIngestionLimits.MaxPreviewRows).ToList(),
            null,
            null);
    }

    private static List<string> CollectExpectedSourceHeaders(IntegrationMappingDefinition definition)
    {
        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(definition.SourceKeyField))
        {
            headers.Add(definition.SourceKeyField);
        }

        foreach (var rule in definition.FieldMappings)
        {
            if (!string.IsNullOrWhiteSpace(rule.SourceField))
            {
                headers.Add(rule.SourceField);
            }
        }

        return headers.ToList();
    }
}
