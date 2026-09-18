namespace NevaxisZentra.Persistence.Authorization;

public interface IAuthorizationContext
{
    bool IsAvailable { get; }

    AuthorizationContext? Current { get; }
}
