using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using NevaxisZentra.Configuration.Bootstrap;

namespace NevaxisZentra.Persistence.Tests;

internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now = now.Add(by);
}

public sealed class BootstrapEnrollmentStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"zentra-bootstrap-{Guid.NewGuid():N}");
    private readonly ManualTimeProvider time = new(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private BootstrapEnrollmentStore NewStore() => new(directory, time);

    private static string ReadRawToken(BootstrapEnrollmentStore store) =>
        File.ReadAllLines(store.TokenFilePath).Last(line => !string.IsNullOrWhiteSpace(line)).Trim();

    private int TokenOccurrencesInDirectory(string token) =>
        Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Sum(file => CountOccurrences(File.ReadAllText(file), token));

    private static int CountOccurrences(string text, string needle)
    {
        var count = 0;
        for (var index = text.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = text.IndexOf(needle, index + 1, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    [Fact]
    public void FreshInstallationMakesBootstrapAvailableWithATimeBoundedToken()
    {
        var store = NewStore();

        var preparation = store.Prepare(existingTenantCount: 0);

        Assert.True(preparation.TokenIssued);
        Assert.False(preparation.TokenReplaced);
        Assert.Equal(BootstrapEnrollmentStage.Pending, preparation.Stage);
        var availability = store.GetAvailability();
        Assert.True(availability.Available);
        Assert.Equal(time.GetUtcNow().Add(BootstrapEnrollmentStore.DefaultTokenLifetime), availability.ExpiresAtUtc);
        Assert.True(File.Exists(store.TokenFilePath));
    }

    [Fact]
    public void StateFileCarriesOnlyDurableFieldsNotDerivedValues()
    {
        var store = NewStore();
        store.Prepare(0);

        var text = File.ReadAllText(store.StatePath);

        Assert.DoesNotContain("StageValue", text, StringComparison.Ordinal);
        Assert.Contains("TokenHash", text, StringComparison.Ordinal);
        Assert.Equal(BootstrapEnrollmentStage.Pending, store.Read()!.StageValue);
    }

    [Fact]
    public void TokenIsHighEntropyAndUniquePerIssue()
    {
        var a = BootstrapEnrollmentStore.GenerateToken();
        var b = BootstrapEnrollmentStore.GenerateToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 43, "256-bit token expected");
        Assert.Matches("^[A-Za-z0-9_-]+$", a);
    }

    [Fact]
    public void RawTokenIsDeliveredExactlyOnceToTheTokenFileAndNowhereElse()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);

        // Exactly one occurrence in the whole directory, and it is the delivery file.
        Assert.Equal(1, TokenOccurrencesInDirectory(token));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(store.TokenFilePath), token));
        Assert.DoesNotContain(token, File.ReadAllText(store.StatePath), StringComparison.Ordinal);

        // Only a verifier is persisted, and it is what the token hashes to.
        var state = store.Read()!;
        Assert.Equal(BootstrapEnrollmentStore.HashToken(token), state.TokenHash);
        Assert.NotEqual(token, state.TokenHash);

        // Re-running the first-run decision while pending does not re-deliver or duplicate the token.
        store.Prepare(0);
        Assert.Equal(token, ReadRawToken(store));
        Assert.Equal(1, TokenOccurrencesInDirectory(token));
    }

    [Fact]
    public void ValidTokenIsGrantedOnceAndCannotBeReused()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);

        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(token));
        Assert.Equal(BootstrapEntryResult.InProgress, store.TryBegin(token));

        store.Complete("enrolled");

        Assert.Equal(BootstrapEntryResult.Unavailable, store.TryBegin(token));
        Assert.False(store.GetAvailability().Available);
    }

    [Fact]
    public void WrongTokenIsRejectedWithoutConsumingTheRealOne()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);

        Assert.Equal(BootstrapEntryResult.InvalidToken, store.TryBegin("not-the-token"));
        Assert.Equal(BootstrapEntryResult.InvalidToken, store.TryBegin(string.Empty));

        Assert.Equal(BootstrapEnrollmentStage.Pending, store.Read()!.StageValue);
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(token));
    }

    [Fact]
    public void TokenExpires()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);

        time.Advance(BootstrapEnrollmentStore.DefaultTokenLifetime + TimeSpan.FromSeconds(1));

        Assert.Equal(BootstrapEntryResult.Expired, store.TryBegin(token));
        Assert.False(store.GetAvailability().Available);
    }

    [Fact]
    public void BootstrapIsUnavailableAfterCompletionAndRestartsDoNotReopenIt()
    {
        var store = NewStore();
        store.Prepare(0);
        store.TryBegin(ReadRawToken(store));
        store.Complete("enrolled");
        Assert.False(File.Exists(store.TokenFilePath), "token file must be destroyed on completion");
        Assert.Null(store.Read()!.TokenHash);

        for (var restart = 0; restart < 3; restart++)
        {
            time.Advance(TimeSpan.FromDays(40));
            var afterRestart = NewStore().Prepare(existingTenantCount: restart == 0 ? 1 : 0);

            Assert.Equal(BootstrapEnrollmentStage.Completed, afterRestart.Stage);
            Assert.False(afterRestart.TokenIssued);
            Assert.False(File.Exists(store.TokenFilePath));
            Assert.False(NewStore().GetAvailability().Available);
        }
    }

    [Fact]
    public void ExpiredAndUnusedTokenIsReplacedOnNextStartup()
    {
        var store = NewStore();
        store.Prepare(0);
        var firstToken = ReadRawToken(store);
        time.Advance(BootstrapEnrollmentStore.DefaultTokenLifetime + TimeSpan.FromMinutes(1));
        Assert.Equal(BootstrapEntryResult.Expired, store.TryBegin(firstToken));

        var restart = NewStore().Prepare(existingTenantCount: 0);

        Assert.True(restart.TokenIssued);
        Assert.True(restart.TokenReplaced);
        var secondToken = ReadRawToken(store);
        Assert.NotEqual(firstToken, secondToken);
        Assert.Equal(2, store.Read()!.IssuedCount);
        Assert.True(store.GetAvailability().Available);

        // The old token is dead; the new one works; the old one still appears nowhere.
        Assert.Equal(BootstrapEntryResult.InvalidToken, store.TryBegin(firstToken));
        Assert.Equal(0, TokenOccurrencesInDirectory(firstToken));
        Assert.Equal(1, TokenOccurrencesInDirectory(secondToken));
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(secondToken));
    }

    [Fact]
    public void LostTokenFileIsReplacedBecauseTheRawTokenCannotBeRecovered()
    {
        var store = NewStore();
        store.Prepare(0);
        var original = ReadRawToken(store);
        File.Delete(store.TokenFilePath);

        var restart = store.Prepare(0);

        Assert.True(restart.TokenReplaced);
        Assert.NotEqual(original, ReadRawToken(store));
    }

    [Fact]
    public void InterruptedEnrollmentRecoversSafelyAndKeepsTheUnusedToken()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(token));

        // Application killed mid-enrollment; nothing was committed (tenant count still 0).
        time.Advance(BootstrapEnrollmentStore.EnrollingTimeout + TimeSpan.FromSeconds(5));
        var restart = NewStore().Prepare(existingTenantCount: 0);

        Assert.False(restart.TokenIssued, "the token was never used, so it must not be silently replaced");
        Assert.Equal(BootstrapEnrollmentStage.Pending, store.Read()!.StageValue);
        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(token));
    }

    [Fact]
    public void AbortAfterAFailedAttemptLeavesTheTokenUsable()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);
        store.TryBegin(token);

        store.Abort();

        Assert.Equal(BootstrapEntryResult.Granted, store.TryBegin(token));
    }

    [Fact]
    public void EnrollmentCommittedButNotRecordedIsClosedFromTheDatabase()
    {
        var store = NewStore();
        store.Prepare(0);
        store.TryBegin(ReadRawToken(store));

        // Crash after the database commit, before Complete(): the tenant exists.
        var restart = NewStore().Prepare(existingTenantCount: 1);

        Assert.Equal(BootstrapEnrollmentStage.Completed, restart.Stage);
        Assert.Equal("reconciled-from-database", restart.CompletionReason);
        Assert.False(File.Exists(store.TokenFilePath));
    }

    [Fact]
    public void ExistingInstallationIsClosedWithoutIssuingAnyToken()
    {
        var store = NewStore();

        var preparation = store.Prepare(existingTenantCount: 4);

        Assert.Equal(BootstrapEnrollmentStage.Completed, preparation.Stage);
        Assert.Equal("existing-installation", preparation.CompletionReason);
        Assert.False(preparation.TokenIssued);
        Assert.False(File.Exists(store.TokenFilePath));
        Assert.False(store.GetAvailability().Available);
    }

    [Fact]
    public void CorruptStateFailsClosedUnlessTheDatabaseProvesEnrollmentHappened()
    {
        var store = NewStore();
        Directory.CreateDirectory(directory);
        File.WriteAllText(store.StatePath, "{ not json");

        Assert.False(store.GetAvailability().Available);
        Assert.Equal(BootstrapEntryResult.Unavailable, store.TryBegin("anything"));
        Assert.Throws<BootstrapStateCorruptException>(() => store.Prepare(existingTenantCount: 0));

        var recovered = store.Prepare(existingTenantCount: 2);
        Assert.Equal(BootstrapEnrollmentStage.Completed, recovered.Stage);
    }

    [Fact]
    public async Task ConcurrentAttemptsWithTheSameTokenGrantExactlyOne()
    {
        var store = NewStore();
        store.Prepare(0);
        var token = ReadRawToken(store);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => Task.Run(() => NewStore().TryBegin(token))));

        Assert.Equal(1, results.Count(result => result == BootstrapEntryResult.Granted));
        Assert.All(results.Where(result => result != BootstrapEntryResult.Granted), result =>
            Assert.Equal(BootstrapEntryResult.InProgress, result));
    }

    [Fact]
    public void StateAndTokenFilesAreOwnerOnly()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = NewStore();
        store.Prepare(0);

        AssertOwnerOnly(store.StatePath);
        AssertOwnerOnly(store.TokenFilePath);
    }

    [SupportedOSPlatform("windows")]
    private static void AssertOwnerOnly(string path)
    {
        var security = new FileInfo(path).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected);
        var identities = security.GetAccessRules(true, false, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .Select(rule => rule.IdentityReference)
            .ToArray();
        Assert.Contains(WindowsIdentity.GetCurrent().User!, identities);
        foreach (var broad in new[] { WellKnownSidType.WorldSid, WellKnownSidType.BuiltinUsersSid, WellKnownSidType.AuthenticatedUserSid })
        {
            Assert.DoesNotContain(new SecurityIdentifier(broad, null), identities);
        }
    }
}
