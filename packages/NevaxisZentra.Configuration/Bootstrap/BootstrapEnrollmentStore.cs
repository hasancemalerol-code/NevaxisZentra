using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NevaxisZentra.Configuration.Bootstrap;

// NOTE: this file is compiled into both NevaxisZentra.Configuration (API side) and the Desktop app
// (token issuance). Keep it dependency-free and keep explicit usings.

public enum BootstrapEnrollmentStage
{
    Pending,
    Enrolling,
    Completed,
}

public enum BootstrapEntryResult
{
    Granted,
    Unavailable,
    InvalidToken,
    Expired,
    InProgress,
}

public sealed class BootstrapEnrollmentStateDocument
{
    public int Version { get; set; } = 1;

    public string Stage { get; set; } = nameof(BootstrapEnrollmentStage.Pending);

    /// <summary>SHA-256 (base64) of the raw token. The raw token is never stored here.</summary>
    public string? TokenHash { get; set; }

    public DateTimeOffset? IssuedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? EnrollingStartedUtc { get; set; }

    public DateTimeOffset? CompletedUtc { get; set; }

    public int IssuedCount { get; set; }

    public string? CompletionReason { get; set; }

    [JsonIgnore]
    public BootstrapEnrollmentStage StageValue =>
        Enum.TryParse<BootstrapEnrollmentStage>(Stage, ignoreCase: false, out var parsed)
            ? parsed
            : BootstrapEnrollmentStage.Completed; // unknown stage fails closed
}

public sealed record BootstrapPreparation(
    BootstrapEnrollmentStage Stage,
    bool TokenIssued,
    bool TokenReplaced,
    DateTimeOffset? ExpiresAtUtc,
    string TokenFilePath,
    string? CompletionReason);

public sealed record BootstrapAvailability(bool Available, DateTimeOffset? ExpiresAtUtc);

public sealed class BootstrapStateCorruptException : InvalidOperationException
{
    public BootstrapStateCorruptException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// One-time first-account enrollment state for a PRODUCTION installation.
/// DELIVERY CHANNEL (single, defined): the raw token is written exactly once to
/// <c>first-run-token.txt</c> in the ACL-protected per-machine configuration directory, never to a
/// console, log or state file. Only a SHA-256 verifier is kept in <c>bootstrap-enrollment.json</c>.
/// </summary>
public sealed class BootstrapEnrollmentStore
{
    public const string StateFileName = "bootstrap-enrollment.json";
    public const string TokenFileName = "first-run-token.txt";
    public const string DirectoryEnvironmentVariable = "NEVAXIS_ZENTRA_BOOTSTRAP_DIR";

    public static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan EnrollingTimeout = TimeSpan.FromMinutes(5);

    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string directory;
    private readonly TimeProvider time;
    private readonly TimeSpan tokenLifetime;

    public BootstrapEnrollmentStore(string directory, TimeProvider? time = null, TimeSpan? tokenLifetime = null)
    {
        this.directory = directory;
        this.time = time ?? TimeProvider.System;
        this.tokenLifetime = tokenLifetime ?? DefaultTokenLifetime;
    }

    public string StatePath => Path.Combine(directory, StateFileName);

    public string TokenFilePath => Path.Combine(directory, TokenFileName);

    /// <summary>Null when no state exists. Throws <see cref="BootstrapStateCorruptException"/> when unreadable.</summary>
    public BootstrapEnrollmentStateDocument? Read()
    {
        lock (Gate)
        {
            return ReadUnlocked();
        }
    }

    /// <summary>
    /// Desktop first-run decision. <paramref name="existingTenantCount"/> is the authoritative count of
    /// tenants in the database: an installation that already has tenants is marked complete and never
    /// receives a token; an unused, expired token is replaced; a completed enrollment is never reopened.
    /// </summary>
    public BootstrapPreparation Prepare(int existingTenantCount)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(directory);

            BootstrapEnrollmentStateDocument? document;
            try
            {
                document = ReadUnlocked();
            }
            catch (BootstrapStateCorruptException) when (existingTenantCount > 0)
            {
                document = null; // the database proves enrollment already happened
            }

            if (document?.StageValue == BootstrapEnrollmentStage.Completed)
            {
                DeleteTokenFile();
                return Summary(document);
            }

            if (existingTenantCount > 0)
            {
                // Existing installation (or enrollment committed but the state write was lost).
                return Summary(CompleteUnlocked(document, document is null ? "existing-installation" : "reconciled-from-database"));
            }

            var now = time.GetUtcNow();
            if (document is null)
            {
                return IssueUnlocked(document, replaced: false);
            }

            if (document.StageValue == BootstrapEnrollmentStage.Enrolling)
            {
                if (document.EnrollingStartedUtc is { } started && now - started <= EnrollingTimeout)
                {
                    return Summary(document); // enrollment genuinely in flight
                }

                // Interrupted enrollment: nothing was committed (database is authoritative, count == 0).
                document.Stage = nameof(BootstrapEnrollmentStage.Pending);
                document.EnrollingStartedUtc = null;
                WriteUnlocked(document);
            }

            var expired = document.ExpiresAtUtc is not { } expires || now >= expires;
            if (expired || !TokenFileMatches(document))
            {
                return IssueUnlocked(document, replaced: true);
            }

            return Summary(document);
        }
    }

    /// <summary>Public, secret-free availability for the onboarding UI.</summary>
    public BootstrapAvailability GetAvailability()
    {
        lock (Gate)
        {
            BootstrapEnrollmentStateDocument? document;
            try
            {
                document = ReadUnlocked();
            }
            catch (BootstrapStateCorruptException)
            {
                return new BootstrapAvailability(false, null);
            }

            if (document is null || document.StageValue == BootstrapEnrollmentStage.Completed)
            {
                return new BootstrapAvailability(false, null);
            }

            var now = time.GetUtcNow();
            if (document.ExpiresAtUtc is not { } expires || now >= expires)
            {
                return new BootstrapAvailability(false, document.ExpiresAtUtc);
            }

            return new BootstrapAvailability(true, expires);
        }
    }

    /// <summary>Validates the token and, only when valid, moves the state to Enrolling.</summary>
    public BootstrapEntryResult TryBegin(string token)
    {
        lock (Gate)
        {
            BootstrapEnrollmentStateDocument? document;
            try
            {
                document = ReadUnlocked();
            }
            catch (BootstrapStateCorruptException)
            {
                return BootstrapEntryResult.Unavailable;
            }

            if (document is null || document.StageValue == BootstrapEnrollmentStage.Completed || document.TokenHash is null)
            {
                return BootstrapEntryResult.Unavailable;
            }

            var now = time.GetUtcNow();
            if (document.StageValue == BootstrapEnrollmentStage.Enrolling)
            {
                if (document.EnrollingStartedUtc is { } started && now - started <= EnrollingTimeout)
                {
                    return BootstrapEntryResult.InProgress;
                }

                document.Stage = nameof(BootstrapEnrollmentStage.Pending);
                document.EnrollingStartedUtc = null;
            }

            if (document.ExpiresAtUtc is not { } expires || now >= expires)
            {
                return BootstrapEntryResult.Expired;
            }

            if (string.IsNullOrWhiteSpace(token) || !HashMatches(token, document.TokenHash))
            {
                return BootstrapEntryResult.InvalidToken;
            }

            document.Stage = nameof(BootstrapEnrollmentStage.Enrolling);
            document.EnrollingStartedUtc = now;
            WriteUnlocked(document);
            return BootstrapEntryResult.Granted;
        }
    }

    /// <summary>A failed enrollment attempt leaves the (still valid, unconsumed) token usable.</summary>
    public void Abort()
    {
        lock (Gate)
        {
            var document = ReadUnlockedOrNull();
            if (document?.StageValue != BootstrapEnrollmentStage.Enrolling)
            {
                return;
            }

            document.Stage = nameof(BootstrapEnrollmentStage.Pending);
            document.EnrollingStartedUtc = null;
            WriteUnlocked(document);
        }
    }

    /// <summary>Permanently closes the bootstrap path: hash removed, token file destroyed.</summary>
    public void Complete(string reason)
    {
        lock (Gate)
        {
            CompleteUnlocked(ReadUnlockedOrNull(), reason);
        }
    }

    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    private static bool HashMatches(string token, string expectedHash)
    {
        var actual = Encoding.UTF8.GetBytes(HashToken(token));
        var expected = Encoding.UTF8.GetBytes(expectedHash);
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string Reference(string tokenHash) =>
        Convert.ToHexString(Encoding.UTF8.GetBytes(tokenHash))[..8];

    private BootstrapPreparation IssueUnlocked(BootstrapEnrollmentStateDocument? previous, bool replaced)
    {
        var now = time.GetUtcNow();
        var token = GenerateToken();
        var hash = HashToken(token);
        var document = new BootstrapEnrollmentStateDocument
        {
            Stage = nameof(BootstrapEnrollmentStage.Pending),
            TokenHash = hash,
            IssuedAtUtc = now,
            ExpiresAtUtc = now.Add(tokenLifetime),
            IssuedCount = (previous?.IssuedCount ?? 0) + 1,
        };

        // State first, then delivery: a crash in between leaves a token file whose reference no longer
        // matches the state, which the next Prepare detects and replaces.
        WriteUnlocked(document);
        WriteTokenFile(token, hash, document.ExpiresAtUtc!.Value);
        return new BootstrapPreparation(
            BootstrapEnrollmentStage.Pending,
            TokenIssued: true,
            TokenReplaced: replaced,
            document.ExpiresAtUtc,
            TokenFilePath,
            CompletionReason: null);
    }

    private BootstrapEnrollmentStateDocument CompleteUnlocked(BootstrapEnrollmentStateDocument? previous, string reason)
    {
        var document = new BootstrapEnrollmentStateDocument
        {
            Stage = nameof(BootstrapEnrollmentStage.Completed),
            TokenHash = null,
            IssuedAtUtc = previous?.IssuedAtUtc,
            ExpiresAtUtc = null,
            IssuedCount = previous?.IssuedCount ?? 0,
            CompletedUtc = time.GetUtcNow(),
            CompletionReason = reason,
        };
        WriteUnlocked(document);
        DeleteTokenFile();
        return document;
    }

    private BootstrapPreparation Summary(BootstrapEnrollmentStateDocument document) =>
        new(
            document.StageValue,
            TokenIssued: false,
            TokenReplaced: false,
            document.ExpiresAtUtc,
            TokenFilePath,
            document.CompletionReason);

    private bool TokenFileMatches(BootstrapEnrollmentStateDocument document)
    {
        if (document.TokenHash is null || !File.Exists(TokenFilePath))
        {
            return false;
        }

        try
        {
            return File.ReadLines(TokenFilePath)
                .Any(line => line.Equals("Reference: " + Reference(document.TokenHash), StringComparison.Ordinal));
        }
        catch (IOException)
        {
            return false;
        }
    }

    private BootstrapEnrollmentStateDocument? ReadUnlockedOrNull()
    {
        try
        {
            return ReadUnlocked();
        }
        catch (BootstrapStateCorruptException)
        {
            return null;
        }
    }

    private BootstrapEnrollmentStateDocument? ReadUnlocked()
    {
        if (!File.Exists(StatePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BootstrapEnrollmentStateDocument>(File.ReadAllText(StatePath), JsonOptions)
                ?? throw new BootstrapStateCorruptException("Bootstrap enrollment state is empty.");
        }
        catch (JsonException)
        {
            throw new BootstrapStateCorruptException("Bootstrap enrollment state is unreadable.");
        }
    }

    private void WriteUnlocked(BootstrapEnrollmentStateDocument document) =>
        WriteProtectedAtomically(StatePath, JsonSerializer.Serialize(document, JsonOptions));

    private void WriteTokenFile(string token, string hash, DateTimeOffset expiresUtc)
    {
        var content = new StringBuilder()
            .AppendLine("NEVAXIS ZENTRA - FIRST-RUN ENROLLMENT TOKEN")
            .AppendLine("Reference: " + Reference(hash))
            .AppendLine("Expires (UTC): " + expiresUtc.UtcDateTime.ToString("u", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine("Single use. Enter this token on the first-time setup page. Do not share it.")
            .AppendLine()
            .AppendLine(token)
            .ToString();
        WriteProtectedAtomically(TokenFilePath, content);
    }

    private void DeleteTokenFile()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                var length = (int)Math.Min(new FileInfo(TokenFilePath).Length, 4096);
                File.WriteAllBytes(TokenFilePath, new byte[length]); // best-effort overwrite before delete
                File.Delete(TokenFilePath);
            }
        }
        catch (IOException)
        {
            // Completed state already forbids use of any remaining file.
        }
    }

    private void WriteProtectedAtomically(string path, string content)
    {
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            if (OperatingSystem.IsWindows())
            {
                CreateOwnerOnlyFile(temporary, bytes);
            }
            else
            {
                File.WriteAllBytes(temporary, bytes);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void CreateOwnerOnlyFile(string path, byte[] bytes)
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

        using var stream = new FileInfo(path).Create(
            FileMode.CreateNew,
            FileSystemRights.FullControl,
            FileShare.None,
            4096,
            FileOptions.WriteThrough,
            security);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(flushToDisk: true);
    }
}
