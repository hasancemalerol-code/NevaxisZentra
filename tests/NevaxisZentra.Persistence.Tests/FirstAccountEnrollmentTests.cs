using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NevaxisZentra.Configuration.Bootstrap;
using NevaxisZentra.Persistence.Bootstrap;
using NevaxisZentra.Persistence.SaasIdentity;

namespace NevaxisZentra.Persistence.Tests;

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception) + (exception is null ? string.Empty : " | " + exception));
}

internal sealed class FakeEnrollmentDatabase : IFirstAccountEnrollmentDatabase
{
    public int TenantCount { get; set; }

    public Exception? FailWith { get; set; }

    public List<FirstAccountEnrollmentInput> Created { get; } = [];

    public Task<int> CountTenantsAsync(CancellationToken cancellationToken) => Task.FromResult(TenantCount);

    public Task<FirstAccountEnrollmentResult> CreateAsync(FirstAccountEnrollmentInput input, CancellationToken cancellationToken)
    {
        if (FailWith is { } failure)
        {
            throw failure;
        }

        Created.Add(input);
        TenantCount++;
        return Task.FromResult(new FirstAccountEnrollmentResult(
            FirstAccountEnrollmentStatus.Enrolled,
            [],
            Guid.NewGuid(),
            input.OrganizationCode,
            "acc_test"));
    }
}

public sealed class FirstAccountEnrollmentServiceTests : IDisposable
{
    private const string StrongPassword = "Str0ng-Passphrase-For-Tests!";
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"zentra-enroll-{Guid.NewGuid():N}");
    private readonly ManualTimeProvider time = new(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeEnrollmentDatabase database = new();
    private readonly CapturingLogger<FirstAccountEnrollmentService> logger = new();
    private readonly BootstrapEnrollmentStore store;
    private int completedCallbacks;

    public FirstAccountEnrollmentServiceTests()
    {
        store = new BootstrapEnrollmentStore(directory, time);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private FirstAccountEnrollmentService NewService() =>
        new(store, database, logger, () => completedCallbacks++, () => Guid.Parse("bb000001-0005-4000-8000-000000000005"));

    private string IssueToken()
    {
        store.Prepare(0);
        return File.ReadAllLines(store.TokenFilePath).Last(line => !string.IsNullOrWhiteSpace(line)).Trim();
    }

    private static FirstAccountEnrollmentRequest Request(string token, string password = StrongPassword) =>
        new(token, "acme-fab", "Acme Fabrika", "owner@acme.example", "Ada Owner", password);

    [Fact]
    public async Task FreshInstallBootstrapIsAvailableAndEnrollmentCreatesTheFirstOwner()
    {
        var token = IssueToken();
        var service = NewService();
        Assert.True(service.GetAvailability().Available);

        var result = await service.EnrollAsync(Request(token), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Enrolled, result.Status);
        var created = Assert.Single(database.Created);
        Assert.Equal("acme-fab", created.OrganizationCode);
        Assert.Equal("owner@acme.example", created.OwnerEmailNormalized);
        Assert.Equal(1, created.TokenIssuedCount);
        Assert.Equal(Guid.Parse("bb000001-0005-4000-8000-000000000005"), created.FacilityId);
        Assert.Equal(1, completedCallbacks);
        Assert.False(service.GetAvailability().Available);
        Assert.Equal(BootstrapEnrollmentStage.Completed, store.Read()!.StageValue);
        Assert.False(File.Exists(store.TokenFilePath));
    }

    [Fact]
    public async Task TokenCannotBeReusedAfterSuccessfulEnrollment()
    {
        var token = IssueToken();
        var service = NewService();
        await service.EnrollAsync(Request(token), CancellationToken.None);

        var second = await service.EnrollAsync(Request(token), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Unavailable, second.Status);
        Assert.Single(database.Created);
    }

    [Fact]
    public async Task ExpiredTokenIsRejectedAndNothingIsCreated()
    {
        var token = IssueToken();
        time.Advance(BootstrapEnrollmentStore.DefaultTokenLifetime + TimeSpan.FromMinutes(1));

        var result = await NewService().EnrollAsync(Request(token), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Expired, result.Status);
        Assert.Empty(database.Created);
    }

    [Fact]
    public async Task WrongTokenDoesNotTouchTheDatabase()
    {
        IssueToken();

        var result = await NewService().EnrollAsync(Request("wrong-token"), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.InvalidToken, result.Status);
        Assert.Empty(database.Created);
        Assert.Equal(0, completedCallbacks);
    }

    [Fact]
    public async Task InvalidDetailsAreRejectedWithoutConsumingTheToken()
    {
        var token = IssueToken();
        var service = NewService();

        var weak = await service.EnrollAsync(Request(token, password: "short"), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.InvalidRequest, weak.Status);
        Assert.Contains("password-too-short", weak.Errors);
        Assert.Equal(BootstrapEnrollmentStage.Pending, store.Read()!.StageValue);

        var retry = await service.EnrollAsync(Request(token), CancellationToken.None);
        Assert.Equal(FirstAccountEnrollmentStatus.Enrolled, retry.Status);
    }

    [Fact]
    public async Task DatabaseFailureRollsBackAndLeavesTheTokenUsable()
    {
        var token = IssueToken();
        database.FailWith = new InvalidOperationException("boom containing password " + StrongPassword);
        var service = NewService();

        var failed = await service.EnrollAsync(Request(token), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Failed, failed.Status);
        Assert.Equal(BootstrapEnrollmentStage.Pending, store.Read()!.StageValue);
        Assert.Equal(0, completedCallbacks);

        database.FailWith = null;
        var retry = await service.EnrollAsync(Request(token), CancellationToken.None);
        Assert.Equal(FirstAccountEnrollmentStatus.Enrolled, retry.Status);
    }

    [Fact]
    public async Task ExistingTenantClosesBootstrapInsteadOfCreatingASecondFirstAccount()
    {
        var token = IssueToken();
        database.TenantCount = 1;

        var result = await NewService().EnrollAsync(Request(token), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.AlreadyEnrolled, result.Status);
        Assert.Empty(database.Created);
        Assert.Equal(BootstrapEnrollmentStage.Completed, store.Read()!.StageValue);
    }

    [Fact]
    public async Task NeitherTokenNorPasswordNorEmailEverReachTheLogs()
    {
        var token = IssueToken();
        database.FailWith = new InvalidOperationException("detail " + token + " " + StrongPassword);
        var service = NewService();
        await service.EnrollAsync(Request(token), CancellationToken.None);
        database.FailWith = null;
        await service.EnrollAsync(Request(token), CancellationToken.None);

        var logs = string.Join("\n", logger.Messages);
        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(token, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(StrongPassword, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("owner@acme.example", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplacementTokenAfterExpiryWorksEndToEnd()
    {
        var first = IssueToken();
        time.Advance(BootstrapEnrollmentStore.DefaultTokenLifetime + TimeSpan.FromHours(1));
        store.Prepare(0); // next startup replaces the expired, unused token
        var second = File.ReadAllLines(store.TokenFilePath).Last(line => !string.IsNullOrWhiteSpace(line)).Trim();
        var service = NewService();

        Assert.Equal(FirstAccountEnrollmentStatus.InvalidToken, (await service.EnrollAsync(Request(first), CancellationToken.None)).Status);
        var result = await service.EnrollAsync(Request(second), CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Enrolled, result.Status);
        Assert.Equal(2, database.Created.Single().TokenIssuedCount);
    }

    [Fact]
    public void ThrottleBlocksTokenGuessingAndRecoversAfterTheWindow()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero));
        var throttle = new BootstrapAttemptThrottle(maxFailures: 3, TimeSpan.FromMinutes(10), clock);

        throttle.RecordFailure();
        throttle.RecordFailure();
        Assert.False(throttle.IsBlocked());
        throttle.RecordFailure();
        Assert.True(throttle.IsBlocked());

        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.False(throttle.IsBlocked());
        throttle.RecordFailure();
        throttle.Reset();
        Assert.False(throttle.IsBlocked());
    }
}

public sealed class FirstAccountEnrollmentPlanTests
{
    private static readonly Guid FacilityId = Guid.Parse("bb000001-0005-4000-8000-000000000005");

    private static FirstAccountEnrollmentPlan BuildPlan(out Guid[] permissions, string password = "Str0ng-Passphrase-For-Tests!")
    {
        permissions = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        var input = new FirstAccountEnrollmentInput("acme-fab", "Acme Fabrika", "owner@acme.example", "Ada Owner", password, 2, FacilityId);
        return FirstAccountEnrollmentPlan.Build(input, permissions, DateTimeOffset.UtcNow, new PasswordHasher<SaasIdentityAccount>());
    }

    [Fact]
    public void PlanCreatesTenantOwnerIdentityMembershipRoleFacilityAndAudit()
    {
        var plan = BuildPlan(out var permissions);

        Assert.Equal("acme-fab", plan.Tenant.TenantKey);
        Assert.Equal(plan.Tenant.Id, plan.Membership.TenantId);
        Assert.Equal(plan.Tenant.Id, plan.Role.TenantId);
        Assert.Equal(plan.Tenant.Id, plan.Facility.TenantId);
        Assert.Equal(FacilityId, plan.Facility.Id);
        Assert.Equal(plan.Membership.Id, plan.MembershipRole.TenantMembershipId);
        Assert.Equal(plan.Role.Id, plan.MembershipRole.TenantRoleId);
        Assert.Equal(plan.Membership.Id, plan.FacilityMembership.TenantMembershipId);
        Assert.Equal(plan.IdentitySubject.Id, plan.Membership.IdentitySubjectId);
        Assert.Equal(plan.IdentitySubject.Id, plan.Account.IdentitySubjectId);
        Assert.Equal(SaasIdentityAccountLifecycleStatuses.Active, plan.Account.LifecycleStatus);
        Assert.NotNull(plan.Account.EmailVerifiedAtUtc);
        Assert.Equal(SaasIdentityConstants.Issuer, plan.IdentitySubject.ExternalIssuer);
        Assert.Equal(permissions.Length, plan.PermissionAssignments.Count);
        Assert.All(plan.PermissionAssignments, assignment =>
        {
            Assert.Equal(plan.Tenant.Id, assignment.TenantId);
            Assert.Equal(plan.Role.Id, assignment.TenantRoleId);
            Assert.Contains(assignment.PermissionDefinitionId, permissions);
        });
        Assert.Equal(FirstAccountEnrollmentEventTypes.Completed, plan.AuditEvent.EventType);
    }

    [Fact]
    public void PasswordIsStoredOnlyAsAVerifiableHash()
    {
        const string password = "Str0ng-Passphrase-For-Tests!";
        var plan = BuildPlan(out _, password);

        Assert.NotEqual(password, plan.Credential.PasswordHash);
        Assert.DoesNotContain(password, plan.Credential.PasswordHash, StringComparison.Ordinal);
        var verification = new PasswordHasher<SaasIdentityAccount>()
            .VerifyHashedPassword(plan.Account, plan.Credential.PasswordHash, password);
        Assert.NotEqual(PasswordVerificationResult.Failed, verification);
    }

    [Fact]
    public void AuditEvidenceHoldsIdentifiersOnlyNeverSecretsOrPersonalData()
    {
        const string password = "Str0ng-Passphrase-For-Tests!";
        var plan = BuildPlan(out _, password);
        var payload = plan.AuditEvent.PayloadJson;

        Assert.Contains(plan.Account.AccountReference, payload, StringComparison.Ordinal);
        Assert.Contains("\"tokenIssuedCount\":2", payload, StringComparison.Ordinal);
        Assert.DoesNotContain(password, payload, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Credential.PasswordHash, payload, StringComparison.Ordinal);
        Assert.DoesNotContain("owner@acme.example", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("token\":\"", payload, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class FirstAccountEnrollmentValidatorTests
{
    [Theory]
    [InlineData("ab")]
    [InlineData("-bad-")]
    [InlineData("Has Space")]
    [InlineData("UPPER_case")]
    [InlineData("")]
    public void InvalidOrganizationCodesAreRejected(string code)
    {
        var errors = FirstAccountEnrollmentValidator.Validate(
            new FirstAccountEnrollmentRequest("t", code, "Org", "a@b.example", "Name", "Str0ng-Passphrase!"));

        Assert.Contains("organization-code-invalid", errors);
    }

    [Theory]
    [InlineData("short1!", "password-too-short")]
    [InlineData("alllowercaseletters", "password-too-simple")]
    [InlineData("ada.owner-Passw0rd!", "password-contains-email")]
    public void WeakPasswordsAreRejectedWithStableCodes(string password, string expectedCode)
    {
        var errors = FirstAccountEnrollmentValidator.ValidatePassword(password, "ada.owner@acme.example");

        Assert.Contains(expectedCode, errors);
    }

    [Fact]
    public void ValidRequestPassesAndNormalizes()
    {
        var request = new FirstAccountEnrollmentRequest(
            "token", "Acme-Fab", " Acme Fabrika ", " Owner@Acme.Example ", " Ada ", "Str0ng-Passphrase-For-Tests!");

        Assert.Empty(FirstAccountEnrollmentValidator.Validate(request));
        var input = FirstAccountEnrollmentValidator.Normalize(request, 1, Guid.Empty);
        Assert.Equal("acme-fab", input.OrganizationCode);
        Assert.Equal("owner@acme.example", input.OwnerEmailNormalized);
        Assert.Equal("Acme Fabrika", input.OrganizationName);
    }
}
