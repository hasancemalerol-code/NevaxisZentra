namespace NevaxisFactOS.Api.Development;

public sealed class DevelopmentPilotBootstrapState
{
    private volatile bool _completed;

    public bool IsCompleted => _completed;

    public void MarkCompleted() => _completed = true;
}
