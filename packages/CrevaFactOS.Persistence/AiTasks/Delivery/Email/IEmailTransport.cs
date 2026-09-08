namespace CrevaFactOS.Persistence.AiTasks.Delivery.Email;

public sealed record EmailTransportRequest(
    string Host,
    int Port,
    string SecurityMode,
    string? Username,
    string? Password,
    string FromAddress,
    string? FromDisplayName,
    string? ReplyToAddress,
    string ToAddress,
    string Subject,
    string PlainTextBody,
    string HtmlBody,
    int TimeoutSeconds);

public sealed record EmailTransportResponse(
    bool Success,
    string? TransportMessageId,
    string? SmtpStatusCode,
    string? EnhancedStatusCode,
    string? ErrorCode,
    string? ErrorCategory,
    bool Retryable);

public interface IEmailTransport
{
    Task<EmailTransportResponse> SendAsync(
        EmailTransportRequest request,
        CancellationToken cancellationToken);
}
