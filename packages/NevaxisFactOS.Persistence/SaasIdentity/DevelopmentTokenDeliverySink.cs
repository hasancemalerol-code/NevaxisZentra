namespace NevaxisFactOS.Persistence.SaasIdentity;

public sealed class DevelopmentTokenDeliverySink : IDevelopmentTokenDeliverySink
{
    private readonly List<DevelopmentCapturedToken> capturedTokens = [];

    public void Capture(string deliveryKind, string recipientEmail, string rawToken)
    {
        capturedTokens.Add(new DevelopmentCapturedToken(
            deliveryKind,
            recipientEmail,
            rawToken,
            DateTimeOffset.UtcNow));
    }

    public IReadOnlyList<DevelopmentCapturedToken> GetCapturedTokens() => capturedTokens;
}
