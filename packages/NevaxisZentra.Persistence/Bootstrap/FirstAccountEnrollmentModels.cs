using System.Globalization;
using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.SaasIdentity;

namespace NevaxisZentra.Persistence.Bootstrap;

public sealed record FirstAccountEnrollmentRequest(
    string? Token,
    string? OrganizationCode,
    string? OrganizationName,
    string? OwnerEmail,
    string? OwnerDisplayName,
    string? Password);

public enum FirstAccountEnrollmentStatus
{
    Enrolled,
    Unavailable,
    InvalidToken,
    Expired,
    InProgress,
    InvalidRequest,
    AlreadyEnrolled,
    Failed,
}

public sealed record FirstAccountEnrollmentResult(
    FirstAccountEnrollmentStatus Status,
    IReadOnlyList<string> Errors,
    Guid? TenantId = null,
    string? TenantKey = null,
    string? AccountReference = null)
{
    public static FirstAccountEnrollmentResult Of(FirstAccountEnrollmentStatus status) =>
        new(status, []);
}

/// <summary>Validated, normalized enrollment input (never contains the token).</summary>
public sealed record FirstAccountEnrollmentInput(
    string OrganizationCode,
    string OrganizationName,
    string OwnerEmailNormalized,
    string OwnerDisplayName,
    string Password,
    int TokenIssuedCount,
    Guid FacilityId);

public static partial class FirstAccountEnrollmentValidator
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,38}[a-z0-9])$", RegexOptions.CultureInvariant)]
    private static partial Regex OrganizationCodePattern();

    /// <summary>Returns stable error codes (no free text, no echo of submitted values).</summary>
    public static IReadOnlyList<string> Validate(FirstAccountEnrollmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        var code = request.OrganizationCode?.Trim().ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
        if (!OrganizationCodePattern().IsMatch(code))
        {
            errors.Add("organization-code-invalid");
        }

        if (string.IsNullOrWhiteSpace(request.OrganizationName) || request.OrganizationName.Trim().Length > 120)
        {
            errors.Add("organization-name-invalid");
        }

        if (string.IsNullOrWhiteSpace(request.OwnerDisplayName) || request.OwnerDisplayName.Trim().Length > 120)
        {
            errors.Add("owner-name-invalid");
        }

        var email = request.OwnerEmail?.Trim() ?? string.Empty;
        if (email.Length > 254 || !SaasIdentityEmailNormalizer.IsValidEmailFormat(email))
        {
            errors.Add("owner-email-invalid");
        }

        errors.AddRange(ValidatePassword(request.Password, email));
        return errors;
    }

    public static IReadOnlyList<string> ValidatePassword(string? password, string email)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinimumPasswordLength)
        {
            errors.Add("password-too-short");
            return errors;
        }

        if (password.Length > MaximumPasswordLength)
        {
            errors.Add("password-too-long");
        }

        var classes = 0;
        classes += password.Any(char.IsLower) ? 1 : 0;
        classes += password.Any(char.IsUpper) ? 1 : 0;
        classes += password.Any(char.IsDigit) ? 1 : 0;
        classes += password.Any(c => !char.IsLetterOrDigit(c)) ? 1 : 0;
        if (classes < 3)
        {
            errors.Add("password-too-simple");
        }

        var localPart = email.Split('@')[0];
        if (localPart.Length >= 4 && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("password-contains-email");
        }

        return errors;
    }

    public static FirstAccountEnrollmentInput Normalize(
        FirstAccountEnrollmentRequest request,
        int tokenIssuedCount,
        Guid facilityId) =>
        new(
            request.OrganizationCode!.Trim().ToLower(CultureInfo.InvariantCulture),
            request.OrganizationName!.Trim(),
            SaasIdentityEmailNormalizer.Normalize(request.OwnerEmail!),
            request.OwnerDisplayName!.Trim(),
            request.Password!,
            tokenIssuedCount,
            facilityId);
}
