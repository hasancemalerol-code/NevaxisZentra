namespace CrevaFactOS.Persistence.SaasIdentity;

public interface IDevelopmentTokenDeliverySink
{
    void Capture(string deliveryKind, string recipientEmail, string rawToken);

    IReadOnlyList<DevelopmentCapturedToken> GetCapturedTokens();
}

public sealed record DevelopmentCapturedToken(
    string DeliveryKind,
    string RecipientEmail,
    string RawToken,
    DateTimeOffset CapturedAtUtc);
