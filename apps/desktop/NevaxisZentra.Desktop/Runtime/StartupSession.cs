using NevaxisZentra.Desktop.Services;

namespace NevaxisZentra.Desktop.Runtime;

public sealed class StartupSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    public string Stage { get; private set; } = "desktop-start";

    public string? ErrorCode { get; private set; }

    public void SetStage(string stage)
    {
        Stage = stage;
        DesktopLifecycleLogger.WriteSession(SessionId, $"stage={stage}");
    }

    public void SetFailure(string errorCode, string message)
    {
        ErrorCode = errorCode;
        DesktopLifecycleLogger.WriteSession(SessionId, $"failure code={errorCode} message={message}");
    }
}
