using NevaxisFactOS.Persistence.Integration.Common;
using NevaxisFactOS.Persistence.Integration.FileImport;
using NevaxisFactOS.Persistence.Integration.Mapping;

namespace NevaxisFactOS.Persistence.Integration.FolderWatch;

public sealed record IntegrationFolderWatchScanResult(
    bool Success,
    IntegrationFileTable? Table,
    string? SourceFingerprint,
    string? ProcessedFilePath,
    string? CheckpointJson,
    string? SafeMessage);

public interface IIntegrationFolderWatchScanner
{
    IntegrationFolderWatchScanResult ScanStableFile(IntegrationConnector connector);

    void ArchiveProcessedFile(IntegrationConnector connector, string? processedFilePath);
}

public sealed class IntegrationFolderWatchScanner : IIntegrationFolderWatchScanner
{
    public IntegrationFolderWatchScanResult ScanStableFile(IntegrationConnector connector)
    {
        var config = IntegrationFolderWatchConnectorConfiguration.TryParse(connector.ConfigurationJson);
        if (config is null || string.IsNullOrWhiteSpace(config.RootPath))
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                null,
                null,
                "Folder watch configuration is invalid.");
        }

        var root = Path.GetFullPath(config.RootPath);
        if (!Directory.Exists(root))
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                null,
                null,
                "Folder watch root does not exist.");
        }

        var pattern = string.IsNullOrWhiteSpace(config.FilePattern) ? "*.csv" : config.FilePattern;
        var candidate = Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(file => IsStable(file, config.StableFileSeconds))
            .OrderBy(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (candidate is null)
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                null,
                connector.CheckpointJson,
                "No stable file is available.");
        }

        var normalized = Path.GetFullPath(candidate.FullName);
        if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                null,
                null,
                "Folder watch path escape blocked.");
        }

        var extension = Path.GetExtension(normalized);
        if (!IntegrationFileIngestionLimits.AllowedExtensions.Contains(extension))
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                normalized,
                null,
                "Unsupported folder watch file extension.");
        }

        using var stream = candidate.OpenRead();
        var table = IntegrationFileTableReader.Read(candidate.Name, stream);
        if (table is null)
        {
            return new IntegrationFolderWatchScanResult(
                false,
                null,
                null,
                normalized,
                null,
                "Folder watch file could not be parsed.");
        }

        using var fingerprintStream = candidate.OpenRead();
        var fingerprint = IntegrationContentFingerprint.ComputeFileFingerprint(fingerprintStream);
        return new IntegrationFolderWatchScanResult(
            true,
            table,
            fingerprint,
            normalized,
            connector.CheckpointJson,
            null);
    }

    public void ArchiveProcessedFile(IntegrationConnector connector, string? processedFilePath)
    {
        if (string.IsNullOrWhiteSpace(processedFilePath))
        {
            return;
        }

        var config = IntegrationFolderWatchConnectorConfiguration.TryParse(connector.ConfigurationJson);
        if (config is null)
        {
            return;
        }

        var root = Path.GetFullPath(config.RootPath);
        var normalized = Path.GetFullPath(processedFilePath);
        if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(normalized))
        {
            return;
        }

        var archiveRoot = Path.Combine(root, config.ArchiveSubfolder);
        Directory.CreateDirectory(archiveRoot);
        var destination = Path.Combine(archiveRoot, Path.GetFileName(normalized));
        if (File.Exists(destination))
        {
            destination = Path.Combine(
                archiveRoot,
                $"{Path.GetFileNameWithoutExtension(normalized)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(normalized)}");
        }

        File.Move(normalized, destination);
    }

    private static bool IsStable(FileInfo file, int stableFileSeconds)
    {
        var threshold = DateTimeOffset.UtcNow.AddSeconds(-Math.Clamp(stableFileSeconds, 1, 300));
        return file.LastWriteTimeUtc <= threshold.UtcDateTime;
    }
}
