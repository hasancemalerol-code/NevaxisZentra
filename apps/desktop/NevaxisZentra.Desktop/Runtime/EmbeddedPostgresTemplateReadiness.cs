using System.IO;

namespace NevaxisZentra.Desktop.Runtime;

public static class EmbeddedPostgresTemplateReadiness
{
    public const int MinimumTemplateFileCount = 100;

    public static IReadOnlyList<string> Validate(PackagedRuntimeBundle bundle, RuntimeManifestDocument? manifest)
    {
        var failures = new List<string>();

        var requiresTemplate = manifest is null
            || string.Equals(
                manifest.DatabaseArchitecture,
                "EMBEDDED_LOCAL",
                StringComparison.OrdinalIgnoreCase);

        if (!requiresTemplate)
        {
            return failures;
        }

        var templateDirectory = bundle.PostgresTemplateDirectory;
        if (!Directory.Exists(templateDirectory))
        {
            failures.Add("postgres-template-directory-missing");
            return failures;
        }

        if (!File.Exists(Path.Combine(templateDirectory, "PG_VERSION")))
        {
            failures.Add("postgres-template-pg-version-missing");
        }

        var templateFiles = Directory.Exists(templateDirectory)
            ? Directory.GetFiles(templateDirectory, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();
        if (templateFiles.Length < MinimumTemplateFileCount)
        {
            failures.Add($"postgres-template-too-small files={templateFiles.Length}");
        }

        if (!File.Exists(bundle.ExpectedSchemaVersionPath))
        {
            failures.Add("expected-schema-version-missing");
        }
        else if (string.IsNullOrWhiteSpace(File.ReadAllText(bundle.ExpectedSchemaVersionPath)))
        {
            failures.Add("expected-schema-version-empty");
        }

        return failures;
    }

    public static string ToCustomerMessage() =>
        "Kurulumdaki yerel veritabanı şablonu eksik veya geçersiz. Lütfen Nevaxis Zentra kurulum paketini yeniden indirip yükleyin.";
}
