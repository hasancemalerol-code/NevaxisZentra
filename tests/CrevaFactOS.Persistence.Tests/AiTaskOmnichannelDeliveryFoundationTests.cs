using CrevaFactOS.Persistence.AiTasks.Delivery;

namespace CrevaFactOS.Persistence.Tests;

public sealed class AiTaskDeliveryStateMachineTests
{
    [Theory]
    [InlineData(AiTaskDeliveryStatuses.Queued, AiTaskDeliveryStatuses.Processing, true)]
    [InlineData(AiTaskDeliveryStatuses.Queued, AiTaskDeliveryStatuses.Cancelled, true)]
    [InlineData(AiTaskDeliveryStatuses.Processing, AiTaskDeliveryStatuses.Sent, true)]
    [InlineData(AiTaskDeliveryStatuses.Processing, AiTaskDeliveryStatuses.FailedRetryable, true)]
    [InlineData(AiTaskDeliveryStatuses.FailedRetryable, AiTaskDeliveryStatuses.Queued, true)]
    [InlineData(AiTaskDeliveryStatuses.FailedRetryable, AiTaskDeliveryStatuses.DeadLetter, true)]
    [InlineData(AiTaskDeliveryStatuses.Queued, AiTaskDeliveryStatuses.Sent, false)]
    [InlineData(AiTaskDeliveryStatuses.Sent, AiTaskDeliveryStatuses.Queued, false)]
    public void TransitionRulesAreExplicit(string current, string next, bool allowed)
    {
        Assert.Equal(allowed, AiTaskDeliveryStateMachine.CanTransition(current, next));
    }

    [Fact]
    public void RetryPolicyUsesBoundedBackoff()
    {
        var first = AiTaskDeliveryRetryPolicy.ComputeNextAttemptUtc(1, DateTimeOffset.UtcNow);
        var sixth = AiTaskDeliveryRetryPolicy.ComputeNextAttemptUtc(6, DateTimeOffset.UtcNow);
        Assert.True((sixth - DateTimeOffset.UtcNow).TotalSeconds <= 3600);
        Assert.True(first > DateTimeOffset.UtcNow);
    }
}

public sealed class AiTaskSecureAccessTokenTests
{
    [Fact]
    public void TokenHashIsStoredNotRawToken()
    {
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("test");
        var service = new AiTaskSecureAccessTokenService(provider);
        var tenantId = Guid.NewGuid();
        var grantId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var issued = service.IssueToken(tenantId, grantId, expiresAt);

        Assert.NotEqual(issued.RawToken, issued.TokenHash);
        Assert.Equal(64, issued.TokenHash.Length);
        Assert.DoesNotContain(issued.RawToken, issued.TokenHash, StringComparison.Ordinal);
    }

    [Fact]
    public void TamperedTokenIsRejected()
    {
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("test");
        var service = new AiTaskSecureAccessTokenService(provider);
        var issued = service.IssueToken(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        var tampered = issued.RawToken.Replace('a', 'b');

        Assert.False(service.TryResolve(tampered, out _, out _));
    }
}

public sealed class AiTaskOmnichannelDeliveryMigrationTests
{
    [Fact]
    public void MigrationDefinesRlsIdempotencyAndPermissions()
    {
        var migrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "packages", "CrevaFactOS.Persistence.Migrations", "Migrations",
            "20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation.cs");

        var fullPath = Path.GetFullPath(migrationPath);
        Assert.True(File.Exists(fullPath), $"Missing migration at {fullPath}");
        var source = File.ReadAllText(fullPath);

        Assert.Contains("ai_task_deliveries", source);
        Assert.Contains("ai_task_delivery_attempts", source);
        Assert.Contains("ai_task_secure_access_grants", source);
        Assert.Contains("ux_ai_task_deliveries_tenant_idempotency", source);
        Assert.Contains("token_hash", source);
        Assert.DoesNotContain("raw_token", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FORCE ROW LEVEL SECURITY", source);
        Assert.Contains("ai-task.delivery.read", source);
        Assert.Contains("ai-task.secure-access.manage", source);
    }
}
