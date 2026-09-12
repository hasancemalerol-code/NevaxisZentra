using NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Api.AiTasks;

public static class WhatsAppWebhookEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWhatsAppWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/integrations/whatsapp");
        group.MapGet("/webhook", VerifyWebhookAsync).AllowAnonymous();
        group.MapPost("/webhook", ReceiveWebhookAsync).AllowAnonymous();
        return endpoints;
    }

    private static IResult VerifyWebhookAsync(
        HttpRequest request,
        IWhatsAppWebhookProcessor processor)
    {
        var mode = request.Query["hub.mode"].ToString();
        var token = request.Query["hub.verify_token"].ToString();
        var challenge = request.Query["hub.challenge"].ToString();

        if (!processor.VerifySubscription(mode, token, challenge, out var responseBody))
        {
            return Results.Unauthorized();
        }

        return Results.Text(responseBody ?? string.Empty);
    }

    private static async Task<IResult> ReceiveWebhookAsync(
        HttpRequest request,
        IWhatsAppWebhookProcessor processor,
        IOptions<WhatsAppBusinessDeliveryOptions> options,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var memory = new MemoryStream();
        await request.Body.CopyToAsync(memory, cancellationToken);
        var rawBody = memory.ToArray();

        var signature = request.Headers["X-Hub-Signature-256"].ToString();
        var appSecret = options.Value.AppSecret;
        if (string.IsNullOrWhiteSpace(appSecret)
            || !processor.VerifySignature(appSecret, signature, rawBody))
        {
            return Results.Unauthorized();
        }

        var result = await processor.ProcessAsync(rawBody, cancellationToken);
        return result.Accepted ? Results.Ok() : Results.BadRequest(new { code = result.FailureCode });
    }
}
