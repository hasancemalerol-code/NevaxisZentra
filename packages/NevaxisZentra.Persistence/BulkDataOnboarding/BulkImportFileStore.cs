using System.Text.Json;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public interface IBulkImportFileStore
{
    string GetJobDirectory(Guid tenantId, Guid facilityId, Guid jobId);
    Task<BulkImportFileManifest> SaveFilesAsync(
        Guid tenantId,
        Guid facilityId,
        Guid jobId,
        IReadOnlyList<BulkImportUploadFile> files,
        CancellationToken cancellationToken = default);
    BulkImportFileManifest ReadManifest(string? observabilityJson);
    string WriteManifest(BulkImportFileManifest manifest);
    Stream OpenRead(Guid tenantId, Guid facilityId, Guid jobId, string storedName);
}

public sealed record BulkImportUploadFile(
    string FileName,
    Stream Content,
    long SizeBytes,
    string ContentType);

public sealed class BulkImportFileStore : IBulkImportFileStore
{
    private const string ManifestKey = "bulkImportFiles";

    public string GetJobDirectory(Guid tenantId, Guid facilityId, Guid jobId)
    {
        var root = Environment.GetEnvironmentVariable("NEVAXIS_ZENTRA__BULK_IMPORT__ROOT")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Nevaxis",
                "Zentra",
                "bulk-import");
        return Path.Combine(root, tenantId.ToString("N"), facilityId.ToString("N"), jobId.ToString("N"));
    }

    public async Task<BulkImportFileManifest> SaveFilesAsync(
        Guid tenantId,
        Guid facilityId,
        Guid jobId,
        IReadOnlyList<BulkImportUploadFile> files,
        CancellationToken cancellationToken = default)
    {
        var directory = GetJobDirectory(tenantId, facilityId, jobId);
        Directory.CreateDirectory(directory);

        var stored = new List<BulkImportStoredFile>();
        foreach (var file in files)
        {
            var validationError = BulkFileSafetyValidator.ValidateFile(file.FileName, file.SizeBytes);
            if (validationError is not null)
            {
                throw new InvalidOperationException($"{BulkFailureCodes.FileRejected}:{validationError}");
            }

            var safeName = Path.GetFileName(file.FileName);
            var targetPath = Path.Combine(directory, safeName);
            await using (var output = File.Create(targetPath))
            {
                await file.Content.CopyToAsync(output, cancellationToken);
            }

            stored.Add(new BulkImportStoredFile(
                safeName,
                safeName,
                file.SizeBytes,
                file.ContentType));
        }

        return new BulkImportFileManifest(stored);
    }

    public BulkImportFileManifest ReadManifest(string? observabilityJson)
    {
        if (string.IsNullOrWhiteSpace(observabilityJson))
        {
            return BulkImportFileManifest.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(observabilityJson);
            if (!document.RootElement.TryGetProperty(ManifestKey, out var filesElement))
            {
                return BulkImportFileManifest.Empty;
            }

            var files = filesElement.EnumerateArray()
                .Select(element => new BulkImportStoredFile(
                    element.GetProperty("fileName").GetString() ?? string.Empty,
                    element.GetProperty("storedName").GetString() ?? string.Empty,
                    element.TryGetProperty("sizeBytes", out var size) ? size.GetInt64() : 0,
                    element.TryGetProperty("contentType", out var contentType)
                        ? contentType.GetString() ?? "application/octet-stream"
                        : "application/octet-stream"))
                .ToList();
            return new BulkImportFileManifest(files);
        }
        catch (JsonException)
        {
            return BulkImportFileManifest.Empty;
        }
    }

    public string WriteManifest(BulkImportFileManifest manifest)
    {
        var payload = new Dictionary<string, object>
        {
            [ManifestKey] = manifest.Files.Select(f => new
            {
                fileName = f.FileName,
                storedName = f.StoredName,
                sizeBytes = f.SizeBytes,
                contentType = f.ContentType,
            }).ToList(),
        };
        return JsonSerializer.Serialize(payload);
    }

    public Stream OpenRead(Guid tenantId, Guid facilityId, Guid jobId, string storedName)
    {
        var safeName = Path.GetFileName(storedName);
        var path = Path.Combine(GetJobDirectory(tenantId, facilityId, jobId), safeName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Bulk import file not found.", safeName);
        }

        return File.OpenRead(path);
    }
}
