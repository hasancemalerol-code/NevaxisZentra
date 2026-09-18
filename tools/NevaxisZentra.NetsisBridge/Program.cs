using System.Security.Claims;
using NevaxisZentra.Persistence.Erp;
using NevaxisZentra.Persistence.Erp.Netsis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<NetsisReadTransportTestFixture>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
});

app.Use(async (context, next) =>
{
    var configuredToken = builder.Configuration["NetsisBridge:AuthToken"];
    if (string.IsNullOrWhiteSpace(configuredToken))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { errorCode = "netsis.bridge.unconfigured" });
        return;
    }

    if (!TryReadBearerToken(context.Request, out var token) || !string.Equals(token, configuredToken, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { errorCode = NetsisTransportErrorCodes.AuthenticationFailed });
        return;
    }

    context.User = new ClaimsPrincipal(new ClaimsIdentity("NetsisBridge"));
    await next();
});

app.MapGet("/health", (NetsisReadTransportTestFixture fixture) =>
{
    var descriptor = BuildDescriptor();
    var health = fixture.CheckHealthAsync(descriptor).GetAwaiter().GetResult();
    return Results.Ok(new
    {
        contextValid = health.IsContextValid,
        readinessState = health.ReadinessState,
        companyId = NetsisReadTransportTestFixture.FixtureCompanyId,
        companyCode = NetsisReadTransportTestFixture.FixtureCompanyCode,
        readOnlyMode = true,
    });
});

app.MapGet("/context", (NetsisReadTransportTestFixture fixture) =>
{
    var descriptor = BuildDescriptor();
    var context = fixture.DiscoverContextAsync(descriptor).GetAwaiter().GetResult();
    return Results.Ok(new
    {
        productFamily = context.ProductFamily,
        productVersion = context.ProductVersion,
        companyId = context.CompanyId,
        companyCode = context.CompanyCode,
        companyName = context.CompanyName,
        period = context.Period,
        transportKind = NetsisTransportKinds.BridgeHttp,
        readOnlyMode = context.ReadOnlyMode,
        supportedCapabilities = context.SupportedCapabilities,
    });
});

app.MapGet("/customers", async (
    NetsisReadTransportTestFixture fixture,
    int? pageSize,
    string? cursor,
    CancellationToken cancellationToken) =>
{
    var descriptor = BuildDescriptor();
    var page = await fixture.ReadCustomersAsync(
        descriptor,
        new NetsisReadPageRequest(pageSize ?? NetsisReadTransportDefaults.DefaultPageSize, cursor),
        cancellationToken);

    return Results.Ok(new
    {
        items = page.Items.Select(item => new
        {
            recordId = item.RecordId,
            code = item.Code,
            name = item.Name,
            taxNumber = item.TaxNumber,
            email = item.Email,
            phone = item.Phone,
            country = item.Country,
            isActive = item.IsActive,
            sourceVersion = item.SourceVersion,
            sourceUpdatedAtUtc = item.SourceUpdatedAtUtc,
        }),
        continuationToken = page.ContinuationToken,
    });
});

app.MapGet("/products", async (
    NetsisReadTransportTestFixture fixture,
    int? pageSize,
    string? cursor,
    CancellationToken cancellationToken) =>
{
    var descriptor = BuildDescriptor();
    var page = await fixture.ReadProductsAsync(
        descriptor,
        new NetsisReadPageRequest(pageSize ?? NetsisReadTransportDefaults.DefaultPageSize, cursor),
        cancellationToken);

    return Results.Ok(new
    {
        items = page.Items.Select(item => new
        {
            recordId = item.RecordId,
            code = item.Code,
            name = item.Name,
            unit = item.Unit,
            barcode = item.Barcode,
            isActive = item.IsActive,
            sourceVersion = item.SourceVersion,
            sourceUpdatedAtUtc = item.SourceUpdatedAtUtc,
        }),
        continuationToken = page.ContinuationToken,
    });
});

app.Run();

static NetsisConnectionDescriptor BuildDescriptor() =>
    new(
        NetsisTransportKinds.TestFixture,
        NetsisReadTransportTestFixture.FixtureProductFamily,
        NetsisReadTransportTestFixture.FixtureProductVersion,
        NetsisReadTransportTestFixture.FixtureCompanyId,
        NetsisReadTransportTestFixture.FixtureCompanyCode,
        1,
        null,
        NetsisCredentialReferences.TestFixture);

static bool TryReadBearerToken(HttpRequest request, out string? token)
{
    token = null;
    if (!request.Headers.TryGetValue("Authorization", out var header))
    {
        return false;
    }

    var value = header.ToString();
    const string prefix = "Bearer ";
    if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    token = value[prefix.Length..].Trim();
    return !string.IsNullOrWhiteSpace(token);
}
