using CrevaFactOS.Persistence.Integration.Webhook;

namespace CrevaFactOS.Api.Integration;

public static class IntegrationWebhookEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIntegrationWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/integration/webhooks/{ingressKey}", IngestWebhookAsync)
            .AllowAnonymous()
            .DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> IngestWebhookAsync(
        string ingressKey,
        HttpRequest request,
        IIntegrationWebhookIngestionService webhookIngestionService,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;

        var headers = request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var ingress = new IntegrationWebhookIngressContext(
            ingressKey,
            headers,
            body,
            request.Headers["X-Integration-Signature"].ToString(),
            request.Headers["X-Integration-Timestamp"].ToString(),
            request.Headers["X-Integration-Event-Id"].ToString());

        var result = await webhookIngestionService.IngestAsync(ingress, cancellationToken);
        if (!result.Success)
        {
            return TypedResults.Json(
                new { errorCode = result.ErrorCode, message = result.SafeMessage },
                statusCode: result.StatusCode);
        }

        return TypedResults.Ok(new { accepted = true, syncRunId = result.SyncRunId });
    }
}
