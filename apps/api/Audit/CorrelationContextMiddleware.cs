using NevaxisZentra.Persistence.Audit;

namespace NevaxisZentra.Api.Audit;

public sealed class CorrelationContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-NevaxisZentra-Correlation-Id";
    public const string LegacyHeaderName = "X-CrevaFactOS-Correlation-Id";

    public async Task InvokeAsync(HttpContext httpContext, CorrelationContext correlationContext)
    {
        var correlationId = ResolveIncoming(httpContext.Request.Headers);
        correlationContext.Set(correlationId, httpContext.TraceIdentifier);
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[HeaderName] = correlationId.ToString("D");
            return Task.CompletedTask;
        });
        await next(httpContext);
    }

    private static Guid ResolveIncoming(IHeaderDictionary headers)
    {
        foreach (var headerName in new[] { HeaderName, LegacyHeaderName })
        {
            var values = headers[headerName];
            if (values.Count == 1
                && values[0] is { Length: <= 36 } value
                && Guid.TryParseExact(value, "D", out var parsed)
                && parsed != Guid.Empty)
            {
                return parsed;
            }
        }

        return Guid.NewGuid();
    }
}
