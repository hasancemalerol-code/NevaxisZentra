using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using CrevaFactOS.Persistence.Development;

namespace CrevaFactOS.Api.Development;

public sealed class DevelopmentPilotBootstrapHealthCheck(
    IHostEnvironment environment,
    IOptions<DevelopmentPilotOptions> options,
    DevelopmentPilotBootstrapState bootstrapState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() || !options.Value.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Development pilot bootstrap is not required."));
        }

        return Task.FromResult(
            bootstrapState.IsCompleted
                ? HealthCheckResult.Healthy("Development pilot bootstrap completed.")
                : HealthCheckResult.Unhealthy("Development pilot bootstrap has not completed."));
    }
}
