namespace NevaxisZentra.Api.Development;

public sealed class DevelopmentPilotBootstrapState
{
    private volatile bool _completed;
    private volatile bool _acceptanceRoleVerified;
    private string? _acceptanceRoleFailure;

    public bool IsCompleted => _completed;

    public bool AcceptanceRoleVerified => _acceptanceRoleVerified;

    public string? AcceptanceRoleFailure => _acceptanceRoleFailure;

    public void MarkCompleted() => _completed = true;

    public void MarkAcceptanceRoleVerified()
    {
        _acceptanceRoleVerified = true;
        _acceptanceRoleFailure = null;
    }

    public void MarkAcceptanceRoleFailure(string message)
    {
        _acceptanceRoleVerified = false;
        _acceptanceRoleFailure = message;
    }
}
