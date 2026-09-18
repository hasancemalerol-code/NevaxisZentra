using NevaxisZentra.Persistence.Outbox;

namespace NevaxisZentra.Persistence.Tests;

public sealed class OutboxFoundationTests
{
    [Fact]
    public void InfrastructureCatalogAndExactSchemaAreEnforced()
    {
        Assert.True(TrySerialize(CreateRequest(), out _));
        Assert.False(TrySerialize(CreateRequest() with { MessageType = "production.order.created" }, out var typeFailure));
        Assert.False(TrySerialize(CreateRequest() with { SchemaVersion = 2 }, out var schemaFailure));
        Assert.Equal("outbox.contract.unsupported", typeFailure);
        Assert.Equal("outbox.contract.unsupported", schemaFailure);
    }

    [Fact]
    public void IdentityCorrelationAndUtcAreRequired()
    {
        Assert.False(TrySerialize(CreateRequest() with { IdempotencyKey = "" }, out var identityFailure));
        Assert.Equal("outbox.identity.invalid", identityFailure);
        Assert.False(TrySerialize(CreateRequest(), out var correlationFailure, Guid.Empty));
        Assert.Equal("outbox.identity.invalid", correlationFailure);
        Assert.False(TrySerialize(CreateRequest() with
        {
            OccurredAtUtc = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.FromHours(3)),
        }, out var timestampFailure));
        Assert.Equal("outbox.timestamp.invalid", timestampFailure);
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("access.token")]
    [InlineData("connection.string")]
    [InlineData("model.prompt")]
    [InlineData("model.completion")]
    [InlineData("hidden.reasoning")]
    public void SensitiveMetadataKeysAreRejected(string key)
    {
        var request = CreateRequest() with
        {
            Headers = new Dictionary<string, string?> { [key] = "prohibited" },
        };

        Assert.False(TrySerialize(request, out var failure));
        Assert.Equal("outbox.metadata.invalid", failure);
    }

    [Fact]
    public void MetadataCountValueAndSerializedSizeAreBounded()
    {
        var tooMany = Enumerable.Range(0, 21).ToDictionary(index => $"key{index}", _ => (string?)"value");
        var longValue = new Dictionary<string, string?> { ["summary"] = new string('x', 301) };
        var oversized = Enumerable.Range(0, 20).ToDictionary(index => $"field{index}", _ => (string?)new string('x', 300));

        Assert.False(TrySerialize(CreateRequest() with { Payload = tooMany }, out var countFailure));
        Assert.Equal("outbox.metadata.invalid", countFailure);
        Assert.False(TrySerialize(CreateRequest() with { Payload = longValue }, out var valueFailure));
        Assert.Equal("outbox.metadata.invalid", valueFailure);
        Assert.False(TrySerialize(CreateRequest() with { Payload = oversized }, out var sizeFailure));
        Assert.Equal("outbox.payload.too_large", sizeFailure);
    }

    private static bool TrySerialize(OutboxWriteRequest request, out string? failureCode, Guid? correlationId = null) =>
        OutboxPayloadValidator.TrySerialize(
            request,
            correlationId ?? Guid.NewGuid(),
            out _, out _, out failureCode);

    private static OutboxWriteRequest CreateRequest()
    {
        var occurred = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        return new OutboxWriteRequest(
            OutboxMessageTypes.Probe,
            OutboxMessageTypes.SchemaVersion,
            "probe:001",
            new Dictionary<string, string?> { ["probe.id"] = "probe-001" },
            new Dictionary<string, string?> { ["source"] = "test.fixture" },
            occurred,
            occurred);
    }
}
