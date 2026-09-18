namespace NevaxisZentra.Persistence.SaasIdentity;

public sealed class UnavailableIdentityEmailDeliveryProvider : IIdentityEmailDeliveryProvider
{
    private static InvalidOperationException Unavailable() =>
        new("Identity email delivery is not configured.");

    public Task DeliverPasswordResetAsync(
        string recipientEmail,
        string rawResetToken,
        CancellationToken cancellationToken = default) =>
        Task.FromException(Unavailable());

    public Task DeliverEmailVerificationAsync(
        string recipientEmail,
        string rawVerificationToken,
        CancellationToken cancellationToken = default) =>
        Task.FromException(Unavailable());

    public Task DeliverInvitationAsync(
        string recipientEmail,
        string rawInvitationToken,
        CancellationToken cancellationToken = default) =>
        Task.FromException(Unavailable());
}
