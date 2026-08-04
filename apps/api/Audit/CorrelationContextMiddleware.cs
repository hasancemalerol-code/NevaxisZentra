using CrevaFactOS.Persistence.Audit;

namespace CrevaFactOS.Api.Audit;

public sealed class CorrelationContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-CrevaFactOS-Correlation-Id";

    public async Task InvokeAsync(HttpContext httpContext, CorrelationContext correlationContext)
    {
        var correlationId = ResolveIncoming(httpContext.Request.Headers[HeaderName]);
        correlationContext.Set(correlationId, httpContext.TraceIdentifier);
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[HeaderName] = correlationId.ToString("D");
            return Task.CompletedTask;
        });
        await next(httpContext);
    }

    private static Guid ResolveIncoming(Microsoft.Extensions.Primitives.StringValues values) =>
        values.Count == 1
        && values[0] is { Length: <= 36 } value
        && Guid.TryParseExact(value, "D", out var parsed)
        && parsed != Guid.Empty
            ? parsed
            : Guid.NewGuid();
}
