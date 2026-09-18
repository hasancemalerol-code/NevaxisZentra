namespace NevaxisZentra.Persistence.Identity;

public interface IIdentitySubjectResolver
{
    Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken = default);
}
