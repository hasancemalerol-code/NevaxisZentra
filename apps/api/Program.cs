using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CrevaFactOS.Configuration;
using CrevaFactOS.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Application:Name"] ??= "CrevaFactOS API";
ValidateConfiguration(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddCrevaFactOSPersistence(builder.Configuration);

var app = builder.Build();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = static _ => false,
    });
app.MapHealthChecks("/health/ready");

app.Run();

static void ValidateConfiguration(IConfiguration configuration)
{
    if (string.IsNullOrWhiteSpace(configuration["Application:Name"]))
    {
        throw new InvalidOperationException("Application:Name must be configured.");
    }
}

public partial class Program;
