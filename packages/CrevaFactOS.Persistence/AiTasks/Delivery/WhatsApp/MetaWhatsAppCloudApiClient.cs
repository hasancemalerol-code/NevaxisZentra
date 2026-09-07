using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed record MetaWhatsAppSendRequest(
    string PhoneNumberId,
    string AccessToken,
    string RecipientE164,
    WhatsAppTaskNotificationTemplate Template,
    WhatsAppTaskNotificationContent Content);

public sealed record MetaWhatsAppSendResponse(
    bool Success,
    int? HttpStatusCode,
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorCategory,
    bool Retryable,
    int? RetryAfterSeconds);

public interface IMetaWhatsAppCloudApiClient
{
    Task<MetaWhatsAppSendResponse> SendTemplateMessageAsync(
        MetaWhatsAppSendRequest request,
        CancellationToken cancellationToken);
}

public sealed class MetaWhatsAppCloudApiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppBusinessDeliveryOptions> options) : IMetaWhatsAppCloudApiClient
{
    public async Task<MetaWhatsAppSendResponse> SendTemplateMessageAsync(
        MetaWhatsAppSendRequest request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var baseUrl = string.IsNullOrWhiteSpace(settings.TestApiBaseUrl)
            ? settings.ApiBaseUrl.TrimEnd('/')
            : settings.TestApiBaseUrl.TrimEnd('/');

        var payload = new
        {
            messaging_product = "whatsapp",
            to = request.RecipientE164.TrimStart('+'),
            type = "template",
            template = new
            {
                name = request.Template.TemplateName,
                language = new { code = request.Template.LanguageCode },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = request.Content.TaskTitle },
                            new { type = "text", text = request.Content.PriorityLabel },
                            new { type = "text", text = request.Content.DueDateLabel },
                            new { type = "text", text = request.Content.SecureTaskUrl },
                        },
                    },
                },
            },
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/{request.PhoneNumberId}/messages");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient(nameof(MetaWhatsAppCloudApiClient));
        client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);

        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var messageId = TryExtractMessageId(body);
                return new MetaWhatsAppSendResponse(true, (int)response.StatusCode, messageId, null, null, false, null);
            }

            return ClassifyFailure((int)response.StatusCode, body, response.Headers.RetryAfter?.Delta);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new MetaWhatsAppSendResponse(false, null, null, "provider-timeout", WhatsAppProviderErrorCategories.Retryable, true, null);
        }
        catch (HttpRequestException)
        {
            return new MetaWhatsAppSendResponse(false, null, null, "provider-network", WhatsAppProviderErrorCategories.Retryable, true, null);
        }
    }

    internal static MetaWhatsAppSendResponse ClassifyFailure(int statusCode, string body, TimeSpan? retryAfter)
    {
        var errorCode = TryExtractErrorCode(body) ?? $"http-{statusCode}";
        var retryAfterSeconds = retryAfter is null ? null : (int?)Math.Max(1, (int)retryAfter.Value.TotalSeconds);

        if (statusCode == 401 || statusCode == 403)
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.Authentication, false, retryAfterSeconds);
        }

        if (statusCode == 429)
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.RateLimit, true, retryAfterSeconds);
        }

        if (statusCode >= 500)
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.Retryable, true, retryAfterSeconds);
        }

        if (statusCode == 400 && body.Contains("template", StringComparison.OrdinalIgnoreCase))
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.PolicyTemplate, false, retryAfterSeconds);
        }

        if (statusCode == 400 && body.Contains("recipient", StringComparison.OrdinalIgnoreCase))
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.Recipient, false, retryAfterSeconds);
        }

        if (statusCode >= 400 && statusCode < 500)
        {
            return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.Permanent, false, retryAfterSeconds);
        }

        return new MetaWhatsAppSendResponse(false, statusCode, null, errorCode, WhatsAppProviderErrorCategories.Unknown, false, retryAfterSeconds);
    }

    private static string? TryExtractMessageId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array
                && messages.GetArrayLength() > 0
                && messages[0].TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? TryExtractErrorCode(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("code", out var code))
            {
                return code.ValueKind == JsonValueKind.Number ? code.GetRawText() : code.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
