namespace CrevaFactOS.Persistence.Identity;

public interface ICurrentIdentityAccessor
{
    bool IsResolved { get; }

    AuthenticatedIdentity? Current { get; }

    Guid RequireIdentitySubjectId();
}
