using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace NevaxisZentra.Desktop.Runtime;

public static class FactOsDatabaseBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static string CreateBackup(string? schemaVersion = null)
    {
        PackagedRuntimePaths.EnsureCustomerDirectories();
        var dataDirectory = Path.Combine(PackagedRuntimePaths.DataRoot, "database", "data");
        if (!Directory.Exists(dataDirectory))
        {
            throw new InvalidOperationException("Veritabanı veri dizini bulunamadı.");
        }

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var backupFolder = Path.Combine(PackagedRuntimePaths.BackupsDirectory, $"backup-{timestamp}");
        Directory.CreateDirectory(backupFolder);

        var archivePath = Path.Combine(backupFolder, "database-data.zip");
        ZipFile.CreateFromDirectory(dataDirectory, archivePath, CompressionLevel.Optimal, includeBaseDirectory: false);

        var metadata = new
        {
            createdAtUtc = DateTimeOffset.UtcNow,
            schemaVersion,
            dataDirectory,
            archive = Path.GetFileName(archivePath),
        };
        File.WriteAllText(
            Path.Combine(backupFolder, "metadata.json"),
            JsonSerializer.Serialize(metadata, JsonOptions));

        return backupFolder;
    }

    public static void RestoreBackup(string backupFolder)
    {
        var metadataPath = Path.Combine(backupFolder, "metadata.json");
        var archivePath = Path.Combine(backupFolder, "database-data.zip");
        if (!File.Exists(metadataPath) || !File.Exists(archivePath))
        {
            throw new InvalidOperationException("Geçersiz yedek paketi.");
        }

        var dataDirectory = Path.Combine(PackagedRuntimePaths.DataRoot, "database", "data");
        if (Directory.Exists(dataDirectory))
        {
            var quarantine = $"{dataDirectory}.pre-restore-{DateTimeOffset.Now:yyyyMMddHHmmss}";
            Directory.Move(dataDirectory, quarantine);
        }

        Directory.CreateDirectory(dataDirectory);
        ZipFile.ExtractToDirectory(archivePath, dataDirectory, overwriteFiles: true);
    }
}
