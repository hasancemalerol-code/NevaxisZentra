namespace CrevaFactOS.Api.Authentication;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public AuthenticationMode Mode { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;
}
