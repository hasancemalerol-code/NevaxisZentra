using System.IO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.Email;

public sealed class SmtpEmailTransport(IOptions<EmailDeliveryOptions> options) : IEmailTransport
{
    public async Task<EmailTransportResponse> SendAsync(
        EmailTransportRequest request,
        CancellationToken cancellationToken)
    {
        var host = ResolveHost(request);
        var port = ResolvePort(request);

        var message = BuildMessage(request);
        using var client = new SmtpClient
        {
            Timeout = request.TimeoutSeconds * 1000,
            CheckCertificateRevocation = true,
        };

        try
        {
            var secureSocketOptions = MapSecurityMode(request.SecurityMode);
            await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                await client.AuthenticateAsync(request.Username, request.Password ?? string.Empty, cancellationToken);
            }

            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            var messageId = message.MessageId ?? GenerateMessageId();
            return new EmailTransportResponse(
                true,
                messageId,
                SanitizeStatusResponse(response),
                null,
                null,
                null,
                false);
        }
        catch (AuthenticationException)
        {
            return ClassifyFailure("smtp-auth-failed", EmailProviderErrorCategories.Authentication, false);
        }
        catch (SmtpCommandException ex) when (IsPermanentSmtpFailure(ex))
        {
            var category = ex.StatusCode is >= SmtpStatusCode.MailboxUnavailable and <= SmtpStatusCode.MailboxNameNotAllowed
                ? EmailProviderErrorCategories.Recipient
                : EmailProviderErrorCategories.Permanent;
            return ClassifyFailure(
                $"smtp-{SanitizeStatusCode(ex.StatusCode)}",
                category,
                false,
                SanitizeStatusCode(ex.StatusCode),
                SanitizeEnhancedStatusCode(ex.ErrorCode));
        }
        catch (SmtpCommandException ex)
        {
            return ClassifyFailure(
                $"smtp-{SanitizeStatusCode(ex.StatusCode)}",
                EmailProviderErrorCategories.Retryable,
                true,
                SanitizeStatusCode(ex.StatusCode),
                SanitizeEnhancedStatusCode(ex.ErrorCode));
        }
        catch (SmtpProtocolException)
        {
            return ClassifyFailure("smtp-protocol", EmailProviderErrorCategories.Configuration, false);
        }
        catch (SslHandshakeException)
        {
            return ClassifyFailure("smtp-tls-handshake", EmailProviderErrorCategories.Configuration, false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ClassifyFailure("smtp-timeout", EmailProviderErrorCategories.Retryable, true);
        }
        catch (IOException)
        {
            return ClassifyFailure("smtp-network", EmailProviderErrorCategories.Network, true);
        }
        catch (HttpRequestException)
        {
            return ClassifyFailure("smtp-network", EmailProviderErrorCategories.Network, true);
        }
    }

    internal static EmailTransportResponse ClassifyFailure(
        string errorCode,
        string category,
        bool retryable,
        string? smtpStatusCode = null,
        string? enhancedStatusCode = null) =>
        new(false, null, smtpStatusCode, enhancedStatusCode, errorCode, category, retryable);

    private string ResolveHost(EmailTransportRequest request) =>
        string.IsNullOrWhiteSpace(options.Value.TestSmtpHost) ? request.Host : options.Value.TestSmtpHost!;

    private int ResolvePort(EmailTransportRequest request) =>
        options.Value.TestSmtpPort ?? request.Port;

    private static MimeMessage BuildMessage(EmailTransportRequest request)
    {
        var fromDisplayName = EmailRecipientNormalizer.SanitizeHeaderValue(request.FromDisplayName);
        var from = string.IsNullOrWhiteSpace(fromDisplayName)
            ? MailboxAddress.Parse(request.FromAddress)
            : new MailboxAddress(fromDisplayName, request.FromAddress);

        var message = new MimeMessage
        {
            From = { from },
            Subject = EmailRecipientNormalizer.SanitizeHeaderValue(request.Subject),
            MessageId = GenerateMessageId(),
        };
        message.To.Add(MailboxAddress.Parse(request.ToAddress));

        if (!string.IsNullOrWhiteSpace(request.ReplyToAddress))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(request.ReplyToAddress));
        }

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.PlainTextBody,
            HtmlBody = request.HtmlBody,
        };
        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private static SecureSocketOptions MapSecurityMode(string securityMode) => securityMode switch
    {
        EmailSecurityModes.Tls => SecureSocketOptions.SslOnConnect,
        EmailSecurityModes.StartTls => SecureSocketOptions.StartTls,
        EmailSecurityModes.None => SecureSocketOptions.None,
        _ => SecureSocketOptions.StartTls,
    };

    private static bool IsPermanentSmtpFailure(SmtpCommandException ex) =>
        (int)ex.StatusCode >= 500 && (int)ex.StatusCode < 600;

    private static string GenerateMessageId() =>
        $"<{Guid.NewGuid():N}.NevaxisZentra@localhost>";

    private static string? SanitizeStatusResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var trimmed = response.Trim();
        return trimmed.Length > 20 ? trimmed[..20] : trimmed;
    }

    private static string? SanitizeStatusCode(SmtpResponse? response) =>
        response is null ? null : SanitizeStatusCode(response.StatusCode);

    private static string SanitizeStatusCode(SmtpStatusCode statusCode) =>
        ((int)statusCode).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string? SanitizeEnhancedStatusCode(SmtpErrorCode? errorCode) =>
        errorCode?.ToString();
}
