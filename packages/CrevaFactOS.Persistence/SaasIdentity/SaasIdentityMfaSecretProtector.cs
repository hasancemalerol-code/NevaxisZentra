using Microsoft.AspNetCore.DataProtection;

namespace CrevaFactOS.Persistence.SaasIdentity;

public sealed class SaasIdentityMfaSecretProtector(IDataProtectionProvider dataProtectionProvider)
{
    private const string ProtectorPurpose = "CrevaFactOS.SaasIdentity.MfaSecret.v1";

    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Protect(string plaintextSecret) =>
        protector.Protect(plaintextSecret);

    public string Unprotect(string protectedSecret) =>
        protector.Unprotect(protectedSecret);
}
