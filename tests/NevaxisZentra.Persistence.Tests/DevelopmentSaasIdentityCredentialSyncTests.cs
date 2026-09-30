using NevaxisZentra.Persistence.SaasIdentity;
using Microsoft.AspNetCore.Identity;

namespace NevaxisZentra.Persistence.Tests;

public sealed class DevelopmentSaasIdentityCredentialSyncTests
{
    private const string OriginalPassword = "Original-Dev-Password-1!";
    private const string RotatedPassword = "Rotated-Dev-Password-2!";

    private static readonly DateTimeOffset SeedTime = new(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

    private readonly PasswordHasher<SaasIdentityAccount> hasher = new();

    [Fact]
    public void MatchingPasswordLeavesCredentialAndStampUnchanged()
    {
        var (account, credential) = CreateSeeded(OriginalPassword);
        var hash = credential.PasswordHash;
        var stamp = account.SecurityStamp;

        var changed = DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, OriginalPassword, SeedTime.AddDays(1));

        Assert.False(changed);
        Assert.Equal(hash, credential.PasswordHash);
        Assert.Equal(stamp, account.SecurityStamp);
        Assert.Equal(SeedTime, credential.UpdatedAtUtc);
    }

    [Fact]
    public void DifferentConfiguredPasswordIsRehashedAndStampRotated()
    {
        var (account, credential) = CreateSeeded(OriginalPassword);
        var hash = credential.PasswordHash;
        var stamp = account.SecurityStamp;
        var later = SeedTime.AddDays(1);

        var changed = DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, RotatedPassword, later);

        Assert.True(changed);
        Assert.NotEqual(hash, credential.PasswordHash);
        Assert.NotEqual(stamp, account.SecurityStamp);
        Assert.Equal(later, credential.UpdatedAtUtc);
        Assert.Equal(later, account.UpdatedAtUtc);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(account, credential.PasswordHash, RotatedPassword));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(account, credential.PasswordHash, OriginalPassword));
    }

    [Fact]
    public void RepeatBootstrapAfterSyncIsIdempotent()
    {
        var (account, credential) = CreateSeeded(OriginalPassword);

        Assert.False(DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, OriginalPassword, SeedTime.AddDays(1)));
        Assert.True(DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, RotatedPassword, SeedTime.AddDays(2)));

        var hash = credential.PasswordHash;
        var stamp = account.SecurityStamp;
        var updatedAt = credential.UpdatedAtUtc;

        Assert.False(DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, RotatedPassword, SeedTime.AddDays(3)));
        Assert.Equal(hash, credential.PasswordHash);
        Assert.Equal(stamp, account.SecurityStamp);
        Assert.Equal(updatedAt, credential.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyConfiguredPasswordNeverMutates(string configuredPassword)
    {
        var (account, credential) = CreateSeeded(OriginalPassword);
        var hash = credential.PasswordHash;
        var stamp = account.SecurityStamp;

        var changed = DevelopmentSaasIdentitySeed.TrySyncExistingCredential(
            hasher, account, credential, configuredPassword, SeedTime.AddDays(1));

        Assert.False(changed);
        Assert.Equal(hash, credential.PasswordHash);
        Assert.Equal(stamp, account.SecurityStamp);
    }

    [Fact]
    public async Task SeedIsNotReachableWhenDevelopmentIdentityIsDisabled()
    {
        // The seed's only entry is EnsureDevelopmentSaasIdentityFixturesAsync, which returns before any
        // account or credential work when Enabled is false (the non-development default).
        var options = Microsoft.Extensions.Options.Options.Create(new DevelopmentSaasIdentityOptions
        {
            Enabled = false,
            TenantOwnerPassword = RotatedPassword,
        });

        await DevelopmentSaasIdentitySeed.EnsureDevelopmentSaasIdentityFixturesAsync(
            dbContext: null!,
            mfaSecretProtector: null!,
            options,
            SeedTime,
            CancellationToken.None);
    }

    private (SaasIdentityAccount Account, SaasIdentityPasswordCredential Credential) CreateSeeded(string password)
    {
        var account = new SaasIdentityAccount
        {
            Id = Guid.NewGuid(),
            SecurityStamp = Guid.NewGuid(),
            CreatedAtUtc = SeedTime,
            UpdatedAtUtc = SeedTime,
        };
        var credential = new SaasIdentityPasswordCredential
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PasswordHash = hasher.HashPassword(account, password),
            PasswordHashVersion = 1,
            CreatedAtUtc = SeedTime,
            UpdatedAtUtc = SeedTime,
        };
        return (account, credential);
    }
}
