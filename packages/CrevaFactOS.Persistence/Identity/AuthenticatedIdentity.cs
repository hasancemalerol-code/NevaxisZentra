namespace CrevaFactOS.Persistence.Identity;

public sealed record AuthenticatedIdentity(
    Guid IdentitySubjectId,
    string ExternalIssuer,
    string ExternalSubject);
