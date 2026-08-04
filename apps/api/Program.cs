using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CrevaFactOS.Configuration;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Api.Tenancy;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Application:Name"] ??= "CrevaFactOS API";
ValidateConfiguration(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddCrevaFactOSPersistence(builder.Configuration);
builder.Services.AddCrevaFactOSAuthentication(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<IdentityResolutionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = static _ => false,
    });
app.MapHealthChecks("/health/ready");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/internal/tenant-context", (
        HttpContext httpContext,
        ICurrentIdentityAccessor currentIdentity,
        ITenantContextAccessor tenantContext) => new
    {
        authenticated = httpContext.User.Identity?.IsAuthenticated == true,
        identityResolved = currentIdentity.IsResolved,
        tenantContextAvailable = tenantContext.IsAvailable,
        identitySubjectId = currentIdentity.Current?.IdentitySubjectId,
        tenantId = tenantContext.Current?.TenantId,
    });
}

app.Run();

static void ValidateConfiguration(IConfiguration configuration)
{
    if (string.IsNullOrWhiteSpace(configuration["Application:Name"]))
    {
        throw new InvalidOperationException("Application:Name must be configured.");
    }
}

public partial class Program;
