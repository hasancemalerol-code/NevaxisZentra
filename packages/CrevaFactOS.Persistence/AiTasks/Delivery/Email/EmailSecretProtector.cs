using Microsoft.AspNetCore.DataProtection;

namespace CrevaFactOS.Persistence.AiTasks.Delivery.Email;

public sealed class EmailSecretProtector(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("CrevaFactOS.AiTask.Email.Secrets");

    public string Protect(string plaintext) => protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => protector.Unprotect(protectedValue);
}
