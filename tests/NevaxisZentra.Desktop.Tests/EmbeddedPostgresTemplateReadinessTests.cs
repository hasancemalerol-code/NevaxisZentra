using System.IO;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class EmbeddedPostgresTemplateReadinessTests
{
    [Fact]
    public void MissingTemplateDirectoryFailsValidation()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"zentra-template-{Guid.NewGuid():N}");
        Directory.CreateDirectory(bundleRoot);
        var bundle = PackagedRuntimeBundle.Create(bundleRoot);
        var manifest = new RuntimeManifestDocument { DatabaseArchitecture = "EMBEDDED_LOCAL" };

        var failures = EmbeddedPostgresTemplateReadiness.Validate(bundle, manifest);

        Assert.Contains("postgres-template-directory-missing", failures);
    }

    [Fact]
    public void PopulatedTemplatePassesValidation()
    {
        var bundleRoot = Path.Combine(Path.GetTempPath(), $"zentra-template-{Guid.NewGuid():N}");
        var template = Path.Combine(bundleRoot, "postgres", "template");
        var migrations = Path.Combine(bundleRoot, "migrations");
        Directory.CreateDirectory(template);
        Directory.CreateDirectory(migrations);
        File.WriteAllText(Path.Combine(template, "PG_VERSION"), "17");
        File.WriteAllText(Path.Combine(migrations, "expected-schema-version.txt"), "20260921090000_AddOperationalValueFoundation");

        for (var i = 0; i < EmbeddedPostgresTemplateReadiness.MinimumTemplateFileCount; i++)
        {
            File.WriteAllText(Path.Combine(template, $"file{i}.bin"), "x");
        }

        var bundle = PackagedRuntimeBundle.Create(bundleRoot);
        var manifest = new RuntimeManifestDocument { DatabaseArchitecture = "EMBEDDED_LOCAL" };

        var failures = EmbeddedPostgresTemplateReadiness.Validate(bundle, manifest);

        Assert.Empty(failures);

        if (Directory.Exists(bundleRoot))
        {
            Directory.Delete(bundleRoot, recursive: true);
        }
    }
}
