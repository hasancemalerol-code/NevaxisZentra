using System.Globalization;
using System.IO;

namespace NevaxisZentra.Desktop.Runtime;

/// <summary>
/// Log files used while launching the bundled PostgreSQL server.
/// <para>
/// The server log (<c>pg_ctl -l</c>) is opened for writing by PostgreSQL itself. The supervisor's own capture of
/// pg_ctl's console output must therefore go to a different file: on Windows a second writer on the same file is
/// refused with "Permission denied" (a sharing violation, not an ACL problem).
/// </para>
/// </summary>
internal static class EmbeddedPostgresLogFiles
{
    public const string ServerLogName = "postgresql.log";

    public const string SupervisorCaptureLogName = "pg_ctl-supervisor.log";

    public static string ServerLog(string logDirectory) => Path.Combine(logDirectory, ServerLogName);

    public static string SupervisorCaptureLog(string logDirectory) => Path.Combine(logDirectory, SupervisorCaptureLogName);

    public static string BuildStartArguments(string dataDirectory, string logDirectory, int port) =>
        $"start -D \"{dataDirectory}\" -l \"{ServerLog(logDirectory)}\" -w -o \"-p {port.ToString(CultureInfo.InvariantCulture)}\"";

    /// <summary>Launches <c>pg_ctl start</c>; PostgreSQL owns the server log, the supervisor owns its own capture file.</summary>
    public static RuntimeProcess LaunchPgCtlStart(
        string pgCtlPath,
        string workingDirectory,
        string dataDirectory,
        string logDirectory,
        int port) =>
        RuntimeProcess.StartHidden(
            pgCtlPath,
            BuildStartArguments(dataDirectory, logDirectory, port),
            workingDirectory,
            null,
            SupervisorCaptureLog(logDirectory));
}
