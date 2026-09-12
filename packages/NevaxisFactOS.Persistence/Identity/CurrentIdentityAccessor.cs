namespace NevaxisFactOS.Persistence.Identity;

public sealed class CurrentIdentityAccessor : ICurrentIdentityAccessor
{
    public bool IsResolved => Current is not null;

    public AuthenticatedIdentity? Current { get; private set; }

    public Guid RequireIdentitySubjectId() => Current?.IdentitySubjectId
        ?? throw new InvalidOperationException("An authenticated identity subject is required for this operation.");

    public void Set(AuthenticatedIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (Current is not null)
        {
            throw new InvalidOperationException("Authenticated identity has already been established for this scope.");
        }

        Current = identity;
    }
}
