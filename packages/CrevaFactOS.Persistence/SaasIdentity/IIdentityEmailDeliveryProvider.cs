namespace CrevaFactOS.Persistence.SaasIdentity;

public interface IIdentityEmailDeliveryProvider
{
    Task DeliverPasswordResetAsync(
        string recipientEmail,
        string rawResetToken,
        CancellationToken cancellationToken = default);

    Task DeliverEmailVerificationAsync(
        string recipientEmail,
        string rawVerificationToken,
        CancellationToken cancellationToken = default);

    Task DeliverInvitationAsync(
        string recipientEmail,
        string rawInvitationToken,
        CancellationToken cancellationToken = default);
}
