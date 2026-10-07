using System.IO;

namespace NevaxisZentra.Desktop.Runtime;

public static class EmbeddedPostgresDataDirectory
{
    // PostgreSQL refuses to start ("could not open directory \"pg_notify\"") when these
    // normally-empty directories are absent. The MSI cannot carry empty directories, so a
    // template copied from the installed payload is missing them and they are recreated here.
    public static readonly IReadOnlyList<string> RequiredEmptyDirectories =
    [
        "pg_commit_ts",
        "pg_dynshmem",
        "pg_notify",
        "pg_replslot",
        "pg_serial",
        "pg_snapshots",
        "pg_stat_tmp",
        "pg_tblspc",
        "pg_twophase",
        Path.Combine("pg_logical", "mappings"),
        Path.Combine("pg_logical", "snapshots"),
        Path.Combine("pg_wal", "archive_status"),
        Path.Combine("pg_wal", "summaries"),
    ];

    public static void EnsureRequiredDirectories(string dataDirectory)
    {
        foreach (var relative in RequiredEmptyDirectories)
        {
            Directory.CreateDirectory(Path.Combine(dataDirectory, relative));
        }
    }

    public static void CopyTemplate(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }
}
