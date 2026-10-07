using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace NevaxisZentra.Desktop.Runtime;

/// <summary>
/// Per-machine runtime secrets for a packaged installation. Values are generated on first run,
/// stored in a file that is protected by an owner-only ACL, and are never shipped in the MSI.
/// </summary>
public static class PackagedMachineSecrets
{
    public const string RuntimePasswordKey = "NEVAXIS_FACTOS__PERSISTENCE__PASSWORD";
    public const string MigrationPasswordKey = "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD";
    public const string DemoIdentityKeyPrefix = "DevelopmentSaasIdentity__";

#if !ZENTRA_PRODUCTION_BUILD
    // Demo channels only: compiled out of customer PRODUCTION builds (Directory.Build.props).
    private const string DemoOwnerPassword = "NevaxisDemo2026!";
#endif

    // URL-safe alphabet: no quote, '=' or whitespace, so values are safe in .env files and SQL literals.
    public static string GenerateSecretValue(int byteLength = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>RFC 4648 base32 (no padding): the only form the TOTP verifier can decode.</summary>
    public static string GenerateBase32Secret(int byteLength = 20)
    {
        var data = RandomNumberGenerator.GetBytes(byteLength);
        var output = new StringBuilder((byteLength * 8 + 4) / 5);
        int buffer = 0, bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 0x1F]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return output.ToString();
    }

    public static bool IsValidBase32Secret(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 16 && value.All(c => Base32Alphabet.Contains(c, StringComparison.Ordinal));

    /// <summary>Creates the secrets file if absent. Returns true only when a new file was created.</summary>
    public static bool EnsureCreated(
        string secretsPath,
        string? demoTemplatePath,
        bool isDemoChannel,
        IList<string> diagnostics)
    {
        if (File.Exists(secretsPath))
        {
            diagnostics.Add("secrets=existing");
            return false;
        }

        var lines = new List<string>
        {
            "# Machine-local packaged runtime secrets. DO NOT COMMIT.",
            $"{RuntimePasswordKey}={GenerateSecretValue()}",
            $"{MigrationPasswordKey}={GenerateSecretValue()}",
        };

        if (isDemoChannel)
        {
            var identityLines = ReadDemoIdentityLines(demoTemplatePath);
            if (identityLines.Count > 0)
            {
                lines.AddRange(identityLines);
                diagnostics.Add("secrets=demo-identity-from-template");
            }
            else
            {
#if ZENTRA_PRODUCTION_BUILD
                // No demo credential exists in a production binary: demo identity stays disabled.
                lines.Add($"{DemoIdentityKeyPrefix}Enabled=false");
#else
                lines.Add($"{DemoIdentityKeyPrefix}TenantOwnerPassword={DemoOwnerPassword}");
                lines.Add($"{DemoIdentityKeyPrefix}ActiveUserPassword={DemoOwnerPassword}");
                lines.Add($"{DemoIdentityKeyPrefix}DeactivatedUserPassword={DemoOwnerPassword}");
                lines.Add($"{DemoIdentityKeyPrefix}NoMembershipUserPassword={DemoOwnerPassword}");
                lines.Add($"{DemoIdentityKeyPrefix}MfaSharedSecret={GenerateBase32Secret()}");
                lines.Add($"{DemoIdentityKeyPrefix}Enabled=true");
#endif
            }
        }
        else
        {
            lines.Add($"{DemoIdentityKeyPrefix}Enabled=false");
        }

        lines.Add(string.Empty);
        WriteProtectedAtomically(secretsPath, string.Join(Environment.NewLine, lines));
        diagnostics.Add(isDemoChannel ? "secrets=generated-demo" : "secrets=generated-production");
        return true;
    }

    public static IReadOnlyDictionary<string, string> Load(string secretsPath) =>
        RuntimePaths.ReadEnvironmentFilePairs(secretsPath)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Machine secrets always win over inherited process values.</summary>
    public static void ImportIntoProcess(IReadOnlyDictionary<string, string> secrets)
    {
        foreach (var pair in secrets)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.Process);
        }
    }

    public static void ApplyOwnerOnlyAccess(string path)
    {
        var info = new FileInfo(path);
        info.SetAccessControl(BuildOwnerOnlySecurity());
    }

    internal static FileSecurity BuildOwnerOnlySecurity()
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Current Windows user SID is unavailable.");
        foreach (var sid in new[]
                 {
                     currentUser,
                     new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                     new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                 })
        {
            security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        }

        return security;
    }

    private static List<string> ReadDemoIdentityLines(string? demoTemplatePath)
    {
        // Only the application-level demo identity is taken from the template; any database
        // credential that may appear there is deliberately ignored.
        if (string.IsNullOrWhiteSpace(demoTemplatePath) || !File.Exists(demoTemplatePath))
        {
            return [];
        }

        var mfaKey = DemoIdentityKeyPrefix + "MfaSharedSecret";
        return RuntimePaths.ReadEnvironmentFilePairs(demoTemplatePath)
            .Where(pair => pair.Key.StartsWith(DemoIdentityKeyPrefix, StringComparison.Ordinal))
            .Select(pair => string.Equals(pair.Key, mfaKey, StringComparison.Ordinal) && !IsValidBase32Secret(pair.Value)
                // A template secret that is not valid base32 makes every MFA verification fail with 503.
                ? $"{pair.Key}={GenerateBase32Secret()}"
                : $"{pair.Key}={pair.Value}")
            .ToList();
    }

    private static void WriteProtectedAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            // The ACL is applied at creation time so the secret is never readable through inherited rights.
            using (var stream = new FileInfo(temp).Create(
                       FileMode.CreateNew,
                       FileSystemRights.FullControl,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough,
                       BuildOwnerOnlySecurity()))
            {
                var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temp, path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
}
