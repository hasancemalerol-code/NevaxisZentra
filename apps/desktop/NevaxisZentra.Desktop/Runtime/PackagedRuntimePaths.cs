using System.IO;

namespace NevaxisZentra.Desktop.Runtime;

public static class PackagedRuntimePaths
{
    public static string DataRoot => PackagedRuntimeBundle.GetPersistentDataRoot();

    public static string LogsDirectory => Path.Combine(DataRoot, "Logs");

    public static string DiagnosticsDirectory => Path.Combine(DataRoot, "Diagnostics");

    public static string BackupsDirectory => Path.Combine(DataRoot, "Backups");

    public static string ConfigDirectory => Path.Combine(DataRoot, "Config");

    public static string RuntimeDirectory => Path.Combine(DataRoot, "Runtime");

    public static void EnsureCustomerDirectories()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(DiagnosticsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(RuntimeDirectory);
        Directory.CreateDirectory(Path.Combine(DataRoot, "database", "data"));
        Directory.CreateDirectory(Path.Combine(DataRoot, "database", "logs"));
    }
}
