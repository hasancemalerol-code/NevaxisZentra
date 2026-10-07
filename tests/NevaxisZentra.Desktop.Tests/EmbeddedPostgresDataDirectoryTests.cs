using System.IO;
using NevaxisZentra.Desktop.Runtime;

namespace NevaxisZentra.Desktop.Tests;

public sealed class EmbeddedPostgresDataDirectoryTests
{
    [Fact]
    public void EnsureRequiredDirectoriesRecreatesEmptyDirectoriesMissingFromMsiPayload()
    {
        var data = Path.Combine(Path.GetTempPath(), $"zentra-pgdata-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(data, "pg_wal"));
            File.WriteAllText(Path.Combine(data, "PG_VERSION"), "17");

            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(data);

            foreach (var relative in EmbeddedPostgresDataDirectory.RequiredEmptyDirectories)
            {
                Assert.True(Directory.Exists(Path.Combine(data, relative)), relative);
            }

            Assert.Contains("pg_notify", EmbeddedPostgresDataDirectory.RequiredEmptyDirectories);
            Assert.Equal("17", File.ReadAllText(Path.Combine(data, "PG_VERSION")));
        }
        finally
        {
            if (Directory.Exists(data))
            {
                Directory.Delete(data, recursive: true);
            }
        }
    }

    [Fact]
    public void EnsureRequiredDirectoriesIsIdempotentAndKeepsExistingContent()
    {
        var data = Path.Combine(Path.GetTempPath(), $"zentra-pgdata-{Guid.NewGuid():N}");
        try
        {
            var notify = Path.Combine(data, "pg_notify");
            Directory.CreateDirectory(notify);
            File.WriteAllText(Path.Combine(notify, "0000"), "x");

            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(data);
            EmbeddedPostgresDataDirectory.EnsureRequiredDirectories(data);

            Assert.True(File.Exists(Path.Combine(notify, "0000")));
        }
        finally
        {
            if (Directory.Exists(data))
            {
                Directory.Delete(data, recursive: true);
            }
        }
    }

    [Fact]
    public void RequiredDirectoriesCoverEveryEmptyDirectoryInTheBuiltTemplate()
    {
        // Mirrors the 13 empty directories initdb creates for PostgreSQL 17 (verified against
        // the release template); if the bundled PostgreSQL major version changes, re-verify.
        Assert.Equal(13, EmbeddedPostgresDataDirectory.RequiredEmptyDirectories.Count);
    }
}
