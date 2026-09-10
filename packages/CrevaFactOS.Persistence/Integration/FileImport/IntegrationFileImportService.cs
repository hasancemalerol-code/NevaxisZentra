namespace CrevaFactOS.Persistence.Integration.FileImport;

public sealed record IntegrationFileImportPreviewRequest(
    string FileName,
    long FileSizeBytes,
    Stream Content,
    string? Delimiter,
    bool? HasHeaderRow,
    string? EncodingName);

public sealed record IntegrationFileImportPreviewResult(
    bool Success,
    string FileKind,
    IReadOnlyList<string> DetectedHeaders,
    IReadOnlyList<IReadOnlyDictionary<string, string>> PreviewRows,
    int TotalPreviewRowCount,
    string EvidenceClass,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationFileImportService
{
    IntegrationFileImportPreviewResult Preview(IntegrationFileImportPreviewRequest request);
}

public sealed class IntegrationFileImportService : IIntegrationFileImportService
{
    public IntegrationFileImportPreviewResult Preview(IntegrationFileImportPreviewRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return Fail("integration.file.validation-failed", validation);
        }

        request.Content.Position = 0;
        var table = IntegrationFileTableReader.Read(
            request.FileName,
            request.Content,
            request.Delimiter,
            request.HasHeaderRow,
            request.EncodingName,
            sheetName: null,
            IntegrationFileIngestionLimits.MaxPreviewRows);
        if (table is null)
        {
            return Fail("integration.file.parse-failed", "File could not be parsed.");
        }

        return new IntegrationFileImportPreviewResult(
            true,
            table.FileKind,
            table.Headers.ToList(),
            table.Rows.Take(IntegrationFileIngestionLimits.MaxPreviewRows).ToList(),
            table.Rows.Count,
            IntegrationEvidenceClasses.Imported,
            null,
            null);
    }

    private static string? ValidateRequest(IntegrationFileImportPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return "File name is required.";
        }

        if (request.FileSizeBytes <= 0)
        {
            return "File is empty.";
        }

        if (request.FileSizeBytes > IntegrationFileIngestionLimits.MaxUploadBytes)
        {
            return "File exceeds the maximum allowed upload size.";
        }

        var extension = Path.GetExtension(request.FileName);
        if (!IntegrationFileIngestionLimits.AllowedExtensions.Contains(extension))
        {
            return "File extension is not allowlisted.";
        }

        var normalizedName = Path.GetFileName(request.FileName);
        if (!string.Equals(normalizedName, request.FileName, StringComparison.Ordinal)
            || normalizedName.Contains("..", StringComparison.Ordinal))
        {
            return "Invalid file name.";
        }

        return null;
    }

    private static IntegrationFileImportPreviewResult Fail(string errorCode, string message) =>
        new(
            false,
            "unknown",
            [],
            [],
            0,
            IntegrationEvidenceClasses.Unknown,
            errorCode,
            message);
}
