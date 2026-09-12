namespace NevaxisFactOS.Persistence.Identity;

public sealed class IdentitySubject
{
    public Guid Id { get; set; }

    public string ExternalIssuer { get; set; } = string.Empty;

    public string ExternalSubject { get; set; } = string.Empty;

    public IdentitySubjectStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
