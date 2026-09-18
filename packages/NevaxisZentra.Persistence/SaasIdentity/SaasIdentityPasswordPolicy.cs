namespace NevaxisZentra.Persistence.SaasIdentity;

public static class SaasIdentityPasswordPolicy
{
    public static SaasIdentityPasswordValidationResult Validate(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return SaasIdentityPasswordValidationResult.Fail("Password is required.");
        }

        if (password.Length < SaasIdentityConstants.MinimumPasswordLength)
        {
            return SaasIdentityPasswordValidationResult.Fail(
                $"Password must be at least {SaasIdentityConstants.MinimumPasswordLength} characters.");
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        foreach (var character in password)
        {
            if (char.IsUpper(character))
            {
                hasUpper = true;
            }
            else if (char.IsLower(character))
            {
                hasLower = true;
            }
            else if (char.IsDigit(character))
            {
                hasDigit = true;
            }
        }

        if (!hasUpper || !hasLower || !hasDigit)
        {
            return SaasIdentityPasswordValidationResult.Fail(
                "Password must include uppercase, lowercase, and numeric characters.");
        }

        return SaasIdentityPasswordValidationResult.Ok();
    }
}

public sealed record SaasIdentityPasswordValidationResult(bool IsValid, string? FailureMessage)
{
    public static SaasIdentityPasswordValidationResult Ok() => new(true, null);

    public static SaasIdentityPasswordValidationResult Fail(string message) => new(false, message);
}
