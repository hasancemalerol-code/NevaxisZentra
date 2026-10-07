using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace NevaxisZentra.Persistence.DataProtection;

/// <summary>Raised when the machine key ring cannot be used safely. Messages never contain key material.</summary>
public sealed class DataProtectionKeyRingException : InvalidOperationException
{
    public DataProtectionKeyRingException(string code, string message)
        : base($"[{code}] {message}")
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>
/// Persistent, machine-local ASP.NET Core Data Protection key ring for non-Development hosts.
/// Lives under the per-machine ACL-protected configuration directory (next to the machine secrets),
/// never under Program Files, in source, in the MSI or in logs. Keys are additionally encrypted at rest
/// with DPAPI (current Windows user) on Windows.
/// </summary>
public static class DataProtectionKeyRing
{
    public const string PathEnvironmentVariable = "NEVAXIS_ZENTRA_DATA_PROTECTION_PATH";
    public const string StateFileName = "key-ring.state.json";
    public const string ApplicationName = "crevafactos";
    private static readonly TimeSpan DefaultKeyLifetime = TimeSpan.FromDays(90);

    /// <summary>The configured directory, or the default under the per-machine config directory.</summary>
    public static string ResolveDirectory(string? configuredPath = null)
    {
        var path = configuredPath ?? Environment.GetEnvironmentVariable(PathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NevaxisZentra",
            "config",
            "data-protection");
    }

    /// <summary>
    /// Creates the directory with an owner-only ACL and verifies an existing ring is intact. Fails closed
    /// (never silently starts a new ring) when keys were deleted or a key file is corrupt.
    /// </summary>
    public static void Prepare(string directory)
    {
        EnsureNotInProgramFiles(directory);

        Directory.CreateDirectory(directory);
        ApplyOwnerOnlyAccess(directory);

        var keyFiles = Directory.GetFiles(directory, "key-*.xml");
        foreach (var file in keyFiles)
        {
            try
            {
                var document = XDocument.Load(file);
                if (!string.Equals(document.Root?.Name.LocalName, "key", StringComparison.Ordinal))
                {
                    throw new FormatException("unexpected root element");
                }
            }
            catch (Exception exception) when (exception is System.Xml.XmlException or FormatException or IOException)
            {
                throw new DataProtectionKeyRingException(
                    "corrupt-key-file",
                    $"Data Protection key file '{Path.GetFileName(file)}' in '{directory}' is unreadable. "
                    + "Protected data (for example MFA secrets) cannot be decrypted until the ring is restored from backup. "
                    + "The application will not start a new key ring over existing data.");
            }
        }

        var stateExists = File.Exists(Path.Combine(directory, StateFileName));
        if (stateExists && keyFiles.Length == 0)
        {
            throw new DataProtectionKeyRingException(
                "key-ring-missing-keys",
                $"The Data Protection key ring in '{directory}' was initialized earlier but contains no keys. "
                + "Protected data cannot be decrypted. Restore the key files from backup; the application "
                + "will not silently create a new ring.");
        }
    }

    public static void Configure(IDataProtectionBuilder builder, string directory)
    {
        builder.SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(directory))
            .SetDefaultKeyLifetime(DefaultKeyLifetime);
        if (OperatingSystem.IsWindows())
        {
            builder.ProtectKeysWithDpapi();
        }
    }

    /// <summary>
    /// Makes sure the ring has at least one key (created exactly once on a new installation) and records
    /// that it was initialized, so a later loss of keys is detected rather than silently replaced.
    /// </summary>
    public static void EnsureInitialized(IKeyManager keyManager, string directory, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(keyManager);

        // Serialize initialization across processes (two hosts starting together, or a quick API restart
        // while the previous process is still exiting) so a new installation creates its first key once.
        using var mutex = new Mutex(false, "NevaxisZentra.DataProtectionKeyRing." + DirectoryToken(directory));
        var owned = false;
        try
        {
            try
            {
                owned = mutex.WaitOne(TimeSpan.FromSeconds(30));
            }
            catch (AbandonedMutexException)
            {
                owned = true; // the previous owner died mid-initialization; the work below is idempotent
            }

            if (!owned)
            {
                throw new DataProtectionKeyRingException(
                    "key-ring-busy",
                    $"Another process is initializing the Data Protection key ring in '{directory}'. Retry in a moment.");
            }

            InitializeUnderLock(keyManager, directory, now);
        }
        finally
        {
            if (owned)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static string DirectoryToken(string directory) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(directory).ToUpperInvariant())))[..16];

    private static void InitializeUnderLock(IKeyManager keyManager, string directory, DateTimeOffset? now)
    {
        IReadOnlyCollection<IKey> keys;
        try
        {
            keys = keyManager.GetAllKeys();
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new DataProtectionKeyRingException(
                "keys-unreadable",
                $"The Data Protection keys in '{directory}' cannot be decrypted by this Windows user. "
                + "Run the application as the account that installed it, or restore the ring from backup.");
        }

        if (keys.Count == 0)
        {
            var created = now ?? DateTimeOffset.UtcNow;
            keyManager.CreateNewKey(created, created.Add(DefaultKeyLifetime));
        }

        var statePath = Path.Combine(directory, StateFileName);
        if (!File.Exists(statePath))
        {
            var temporary = Path.Combine(directory, $".{StateFileName}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(
                temporary,
                JsonSerializer.Serialize(new { version = 1, initializedUtc = (now ?? DateTimeOffset.UtcNow).ToString("o") }));
            File.Move(temporary, statePath, overwrite: true);
        }
    }
    /// <summary>True when the directory ACL is protected and grants nothing to broad groups.</summary>
    [SupportedOSPlatform("windows")]
    public static bool IsOwnerOnly(string directory)
    {
        var security = new DirectoryInfo(directory).GetAccessControl();
        if (!security.AreAccessRulesProtected)
        {
            return false;
        }

        var identities = security.GetAccessRules(true, false, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .Select(rule => rule.IdentityReference)
            .ToArray();
        foreach (var broad in new[] { WellKnownSidType.WorldSid, WellKnownSidType.BuiltinUsersSid, WellKnownSidType.AuthenticatedUserSid })
        {
            if (identities.Contains(new SecurityIdentifier(broad, null)))
            {
                return false;
            }
        }

        return identities.Contains(WindowsIdentity.GetCurrent().User!);
    }

    private static void EnsureNotInProgramFiles(string directory)
    {
        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var programFiles = Environment.GetFolderPath(folder);
            if (!string.IsNullOrEmpty(programFiles)
                && Path.GetFullPath(directory).StartsWith(programFiles, StringComparison.OrdinalIgnoreCase))
            {
                throw new DataProtectionKeyRingException(
                    "key-ring-in-program-files",
                    "The Data Protection key ring must not live under Program Files.");
            }
        }
    }

    private static void ApplyOwnerOnlyAccess(string directory)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ApplyOwnerOnlyAccessWindows(directory);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyOwnerOnlyAccessWindows(string directory)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new DataProtectionKeyRingException("acl-failed", "Current Windows user SID is unavailable.");
        foreach (var sid in new[]
                 {
                     currentUser,
                     new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                     new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                 })
        {
            security.AddAccessRule(new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
        }

        new DirectoryInfo(directory).SetAccessControl(security);
    }
}
