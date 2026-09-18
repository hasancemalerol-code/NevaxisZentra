using System.Globalization;

namespace NevaxisZentra.Persistence.SaasIdentity;

public static class SaasIdentityEmailNormalizer
{
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var trimmed = email.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        return trimmed.ToLowerInvariant();
    }

    public static string NormalizeOrganizationContext(string organizationContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationContext);

        return organizationContext.Trim().ToLower(CultureInfo.InvariantCulture);
    }

    public static bool IsValidEmailFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');
        return atIndex > 0
            && atIndex < trimmed.Length - 1
            && trimmed.IndexOf('@', atIndex + 1) < 0;
    }
}
