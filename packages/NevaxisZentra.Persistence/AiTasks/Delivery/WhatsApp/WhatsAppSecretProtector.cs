using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.WhatsApp;

public sealed class WhatsAppSecretProtector(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("NevaxisZentra.AiTask.WhatsApp.Secrets");

    public string Protect(string plaintext) => protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => protector.Unprotect(protectedValue);

    public static string ComputeEventHash(string provider, string providerEventId, string providerMessageId, string status) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{provider}|{providerEventId}|{providerMessageId}|{status}")));
}
