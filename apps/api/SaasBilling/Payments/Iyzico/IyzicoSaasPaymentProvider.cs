using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.SaasBilling.Payments;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Api.SaasBilling.Payments.Iyzico;

public sealed class IyzicoSaasPaymentProvider(
    IOptions<SaasPaymentProviderOptions> optionsAccessor,
    IHttpClientFactory httpClientFactory) : ISaasPaymentProvider
{
    private const string InitializePath = "/payment/iyzipos/checkoutform/initialize/auth/ecom";
    private const string RetrievePath = "/payment/iyzipos/checkoutform/auth/ecom/detail";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly int[] DefaultInstallments = [1];

    private readonly SaasPaymentProviderOptions _options = optionsAccessor.Value;

    public string ProviderKey => SaasPaymentProviderKeys.Iyzico;

    public SaasPaymentProviderStatus GetStatus()
    {
        if (!IsSandboxConfigured())
        {
            return new SaasPaymentProviderStatus(
                ProviderKey,
                SaasPaymentProviderModes.NotConfigured,
                false,
                false,
                false,
                "provider-not-configured");
        }

        return new SaasPaymentProviderStatus(
            ProviderKey,
            SaasPaymentProviderModes.Sandbox,
            true,
            false,
            true,
            "sandbox-ready");
    }

    public async Task<SaasPaymentCheckoutInitializationResult> InitializeHostedCheckoutAsync(
        SaasPaymentCheckoutInitializationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSandboxConfigured())
        {
            return new SaasPaymentCheckoutInitializationResult(
                false,
                SaasPaymentFailureCodes.ProviderNotConfigured,
                null,
                null,
                null);
        }

        var callbackUrl = BuildCallbackUrl(request.PaymentAttemptId);
        var price = request.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        var body = JsonSerializer.Serialize(new
        {
            locale = "tr",
            conversationId = request.ConversationId,
            price,
            paidPrice = price,
            currency = request.CurrencyCode.ToUpperInvariant(),
            basketId = request.InvoiceId.ToString("D"),
            paymentGroup = "PRODUCT",
            callbackUrl,
            enabledInstallments = DefaultInstallments,
            buyer = new
            {
                id = request.TenantId.ToString("D"),
                name = request.BuyerReferenceMasked,
                surname = "Tenant",
                identityNumber = "11111111111",
                email = "billing@tenant.local",
                gsmNumber = "+905555555555",
                registrationAddress = "Tenant",
                city = "Istanbul",
                country = "Turkey",
                ip = "127.0.0.1",
            },
            shippingAddress = new
            {
                contactName = request.BuyerReferenceMasked,
                city = "Istanbul",
                country = "Turkey",
                address = "Tenant",
            },
            billingAddress = new
            {
                contactName = request.BuyerReferenceMasked,
                city = "Istanbul",
                country = "Turkey",
                address = "Tenant",
            },
            basketItems = new[]
            {
                new
                {
                    id = request.InvoiceId.ToString("D"),
                    name = "SaaS Invoice",
                    category1 = "Subscription",
                    itemType = "VIRTUAL",
                    price,
                },
            },
        }, JsonOptions);

        using var response = await SendAuthorizedPostAsync(InitializePath, body, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new SaasPaymentCheckoutInitializationResult(
                false,
                SaasPaymentFailureCodes.ProviderUnavailable,
                null,
                null,
                null);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (!string.Equals(root.GetProperty("status").GetString(), "success", StringComparison.OrdinalIgnoreCase))
        {
            return new SaasPaymentCheckoutInitializationResult(
                false,
                SaasPaymentFailureCodes.ProviderUnavailable,
                null,
                null,
                null);
        }

        var token = root.GetProperty("token").GetString();
        var paymentPageUrl = root.GetProperty("paymentPageUrl").GetString();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(paymentPageUrl))
        {
            return new SaasPaymentCheckoutInitializationResult(
                false,
                SaasPaymentFailureCodes.ValidationFailed,
                null,
                null,
                null);
        }

        return new SaasPaymentCheckoutInitializationResult(
            true,
            null,
            IyzicoPaymentMasking.MaskToken(token),
            paymentPageUrl.Trim(),
            DateTimeOffset.UtcNow.AddMinutes(30));
    }

    public async Task<SaasPaymentCheckoutVerificationResult> RetrieveCheckoutResultAsync(
        SaasPaymentCheckoutVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSandboxConfigured())
        {
            return new SaasPaymentCheckoutVerificationResult(
                false,
                SaasPaymentNormalizedStatuses.Failed,
                SaasPaymentFailureCodes.ProviderNotConfigured,
                null,
                null,
                null,
                null);
        }

        var token = request.CheckoutSessionToken.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new SaasPaymentCheckoutVerificationResult(
                false,
                SaasPaymentNormalizedStatuses.Failed,
                SaasPaymentFailureCodes.CheckoutVerificationFailed,
                null,
                null,
                null,
                null);
        }

        var body = JsonSerializer.Serialize(new
        {
            locale = "tr",
            conversationId = request.ConversationId,
            token,
        }, JsonOptions);

        using var response = await SendAuthorizedPostAsync(RetrievePath, body, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new SaasPaymentCheckoutVerificationResult(
                false,
                SaasPaymentNormalizedStatuses.Failed,
                SaasPaymentFailureCodes.CheckoutVerificationFailed,
                null,
                null,
                null,
                null);
        }

        return ParseCheckoutDetail(responseBody, IyzicoPaymentMasking.MaskToken(request.CheckoutSessionToken));
    }

    public SaasPaymentWebhookValidationResult ValidateWebhookSignature(
        SaasPaymentWebhookValidationRequest request)
    {
        if (!IsSandboxConfigured())
        {
            return new SaasPaymentWebhookValidationResult(false, SaasPaymentFailureCodes.ProviderNotConfigured);
        }

        if (!request.Headers.TryGetValue("X-IYZ-SIGNATURE-V3", out var signature)
            || string.IsNullOrWhiteSpace(signature))
        {
            return new SaasPaymentWebhookValidationResult(false, SaasPaymentFailureCodes.WebhookSignatureInvalid);
        }

        try
        {
            using var document = JsonDocument.Parse(request.RawBody);
            var root = document.RootElement;
            var iyziEventType = root.GetProperty("iyziEventType").GetString() ?? string.Empty;
            var status = root.GetProperty("status").GetString() ?? string.Empty;
            var paymentConversationId = root.TryGetProperty("paymentConversationId", out var conversationElement)
                ? conversationElement.GetString() ?? string.Empty
                : string.Empty;
            var paymentId = root.TryGetProperty("paymentId", out var paymentIdElement)
                ? paymentIdElement.GetString()
                : null;
            var iyziPaymentId = root.TryGetProperty("iyziPaymentId", out var iyziPaymentIdElement)
                ? iyziPaymentIdElement.GetString()
                : paymentId;
            var token = root.TryGetProperty("token", out var tokenElement)
                ? tokenElement.GetString()
                : null;

            var expected = IyzicoAuthorizationHelper.ComputeWebhookSignatureV3(
                _options.IyzicoSecretKey,
                iyziEventType,
                iyziPaymentId,
                token,
                paymentConversationId,
                status);

            return IyzicoAuthorizationHelper.FixedTimeEquals(expected, signature.Trim())
                ? new SaasPaymentWebhookValidationResult(true, null)
                : new SaasPaymentWebhookValidationResult(false, SaasPaymentFailureCodes.WebhookSignatureInvalid);
        }
        catch (JsonException)
        {
            return new SaasPaymentWebhookValidationResult(false, SaasPaymentFailureCodes.WebhookRejected);
        }
    }

    public SaasPaymentNormalizedEvent? NormalizeProviderEvent(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var iyziEventType = root.GetProperty("iyziEventType").GetString() ?? "unknown";
            var status = root.GetProperty("status").GetString() ?? string.Empty;
            var paymentConversationId = root.TryGetProperty("paymentConversationId", out var conversationElement)
                ? conversationElement.GetString() ?? string.Empty
                : string.Empty;
            var paymentId = root.TryGetProperty("paymentId", out var paymentIdElement)
                ? paymentIdElement.GetString()
                : null;
            var iyziPaymentId = root.TryGetProperty("iyziPaymentId", out var iyziPaymentIdElement)
                ? iyziPaymentIdElement.GetString()
                : paymentId;
            var token = root.TryGetProperty("token", out var tokenElement)
                ? tokenElement.GetString()
                : null;
            var referenceCode = root.TryGetProperty("iyziReferenceCode", out var referenceElement)
                ? referenceElement.GetString() ?? string.Empty
                : string.Empty;

            decimal? amount = null;
            if (root.TryGetProperty("price", out var priceElement)
                && decimal.TryParse(priceElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                amount = parsedPrice;
            }

            string? currencyCode = root.TryGetProperty("currency", out var currencyElement)
                ? currencyElement.GetString()
                : null;

            var identity = string.IsNullOrWhiteSpace(referenceCode)
                ? $"{iyziEventType}:{iyziPaymentId}:{paymentConversationId}:{status}"
                : referenceCode;

            var sanitizedEvidence = JsonSerializer.Serialize(new
            {
                iyziEventType,
                status,
                paymentConversationId,
                paymentReferenceMasked = IyzicoPaymentMasking.MaskPaymentReference(iyziPaymentId),
                checkoutReferenceMasked = string.IsNullOrWhiteSpace(token)
                    ? null
                    : IyzicoPaymentMasking.MaskToken(token),
            }, JsonOptions);

            return new SaasPaymentNormalizedEvent(
                identity,
                MapProviderStatus(status),
                IyzicoPaymentMasking.MaskPaymentReference(iyziPaymentId),
                string.IsNullOrWhiteSpace(token) ? null : IyzicoPaymentMasking.MaskToken(token),
                amount,
                currencyCode,
                sanitizedEvidence);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task<SaasPaymentStateQueryResult> QueryPaymentStateAsync(
        SaasPaymentStateQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var verification = RetrieveCheckoutResultAsync(
            new SaasPaymentCheckoutVerificationRequest(
                request.ProviderPaymentReferenceMasked,
                request.ConversationId),
            cancellationToken);

        return verification.ContinueWith(
            task => new SaasPaymentStateQueryResult(
                task.Result.Succeeded,
                task.Result.NormalizedStatus,
                task.Result.FailureCode,
                task.Result.PaidAmount,
                task.Result.CurrencyCode,
                task.Result.SanitizedEvidenceJson),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private bool IsSandboxConfigured() =>
        string.Equals(_options.ProviderMode, SaasPaymentProviderModes.Sandbox, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_options.IyzicoBaseUrl)
        && !string.IsNullOrWhiteSpace(_options.IyzicoApiKey)
        && !string.IsNullOrWhiteSpace(_options.IyzicoSecretKey)
        && !string.IsNullOrWhiteSpace(_options.CallbackBaseUrl);

    private string BuildCallbackUrl(Guid paymentAttemptId)
    {
        var baseUrl = _options.CallbackBaseUrl.TrimEnd('/');
        return $"{baseUrl}/ayarlar/faturalandirma/odeme-donusu?paymentAttemptId={paymentAttemptId:D}";
    }

    private async Task<HttpResponseMessage> SendAuthorizedPostAsync(
        string uriPath,
        string body,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(IyzicoSaasPaymentProvider));
        var baseUrl = _options.IyzicoBaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + uriPath)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(
            IyzicoAuthorizationHelper.BuildAuthorizationHeader(
                _options.IyzicoApiKey,
                _options.IyzicoSecretKey,
                uriPath,
                body));

        return await client.SendAsync(request, cancellationToken);
    }

    private static SaasPaymentCheckoutVerificationResult ParseCheckoutDetail(
        string responseBody,
        string checkoutSessionReferenceMasked)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (!string.Equals(root.GetProperty("status").GetString(), "success", StringComparison.OrdinalIgnoreCase))
        {
            return new SaasPaymentCheckoutVerificationResult(
                false,
                SaasPaymentNormalizedStatuses.Failed,
                SaasPaymentFailureCodes.CheckoutVerificationFailed,
                null,
                null,
                null,
                null);
        }

        var paymentStatus = root.GetProperty("paymentStatus").GetString() ?? string.Empty;
        var normalizedStatus = MapProviderStatus(paymentStatus);
        decimal? paidAmount = null;
        if (root.TryGetProperty("paidPrice", out var paidPriceElement)
            && decimal.TryParse(paidPriceElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPaid))
        {
            paidAmount = parsedPaid;
        }

        var currencyCode = root.TryGetProperty("currency", out var currencyElement)
            ? currencyElement.GetString()
            : null;
        var paymentId = root.TryGetProperty("paymentId", out var paymentIdElement)
            ? paymentIdElement.GetString()
            : null;

        var sanitizedEvidence = JsonSerializer.Serialize(new
        {
            paymentStatus,
            paymentReferenceMasked = IyzicoPaymentMasking.MaskPaymentReference(paymentId),
            checkoutReferenceMasked = checkoutSessionReferenceMasked,
        }, JsonOptions);

        return new SaasPaymentCheckoutVerificationResult(
            normalizedStatus is SaasPaymentNormalizedStatuses.Succeeded
                or SaasPaymentNormalizedStatuses.Pending,
            normalizedStatus,
            normalizedStatus == SaasPaymentNormalizedStatuses.Failed
                ? SaasPaymentFailureCodes.CheckoutVerificationFailed
                : null,
            paidAmount,
            currencyCode,
            IyzicoPaymentMasking.MaskPaymentReference(paymentId),
            sanitizedEvidence);
    }

    private static string MapProviderStatus(string status) =>
        status.ToUpperInvariant() switch
        {
            "SUCCESS" => SaasPaymentNormalizedStatuses.Succeeded,
            "FAILURE" => SaasPaymentNormalizedStatuses.Failed,
            "CANCEL" or "CANCELLED" => SaasPaymentNormalizedStatuses.Cancelled,
            "REFUND" or "REFUNDED" => SaasPaymentNormalizedStatuses.Refunded,
            "INIT_THREEDS" or "CALLBACK_THREEDS" or "PENDING" => SaasPaymentNormalizedStatuses.Pending,
            _ => SaasPaymentNormalizedStatuses.Failed,
        };
}
