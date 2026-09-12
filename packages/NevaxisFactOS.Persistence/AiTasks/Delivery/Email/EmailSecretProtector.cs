using Microsoft.AspNetCore.DataProtection;

namespace NevaxisFactOS.Persistence.AiTasks.Delivery.Email;

public sealed class EmailSecretProtector(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("NevaxisFactOS.AiTask.Email.Secrets");

    public string Protect(string plaintext) => protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => protector.Unprotect(protectedValue);
}
