namespace NevaxisZentra.Persistence.Integration.FileImport;

public static class IntegrationFileIngestionLimits
{
    public const long MaxUploadBytes = 10 * 1024 * 1024;
    public const int MaxPreviewRows = 100;
    public const int MaxImportRows = 50_000;
    public const int MaxColumns = 100;

    public static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".csv", ".txt", ".json", ".xlsx" };
}
