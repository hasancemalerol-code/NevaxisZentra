using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NevaxisZentra.Persistence.Development;

namespace NevaxisZentra.Api.Development;

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

        if (!string.IsNullOrWhiteSpace(bootstrapState.AcceptanceRoleFailure))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(bootstrapState.AcceptanceRoleFailure));
        }

        if (!bootstrapState.AcceptanceRoleVerified)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Acceptance role contract has not been verified. Required modules must not be hidden."));
        }

        return Task.FromResult(
            bootstrapState.IsCompleted
                ? HealthCheckResult.Healthy("Development pilot bootstrap completed and the acceptance role contract is satisfied.")
                : HealthCheckResult.Unhealthy("Development pilot bootstrap has not completed."));
    }
}
