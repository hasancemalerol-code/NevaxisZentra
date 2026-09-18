using System.Text.Json;
using NevaxisZentra.Persistence.Integration;

namespace NevaxisZentra.Persistence.EnterpriseReliability;

public static class IntegrationCircuitBreakerEngine
{
    public const int DefaultFailureThreshold = 5;
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(5);

    public static CircuitBreakerEvaluation Evaluate(
        IntegrationCircuitBreakerState state,
        bool success,
        DateTimeOffset nowUtc)
    {
        if (state.State == CircuitBreakerStates.Open)
        {
            if (state.CooldownUntilUtc is null || nowUtc < state.CooldownUntilUtc)
            {
                return new CircuitBreakerEvaluation(
                    AllowExecution: false,
                    state.State,
                    state.ConsecutiveFailures,
                    "integration.circuit-open");
            }

            state.State = CircuitBreakerStates.HalfOpen;
            state.LastProbeAtUtc = nowUtc;
        }

        if (success)
        {
            state.ConsecutiveFailures = 0;
            state.State = CircuitBreakerStates.Closed;
            state.OpenedAtUtc = null;
            state.CooldownUntilUtc = null;
            return new CircuitBreakerEvaluation(true, state.State, 0, null);
        }

        state.ConsecutiveFailures++;
        if (state.ConsecutiveFailures >= state.FailureThreshold)
        {
            state.State = CircuitBreakerStates.Open;
            state.OpenedAtUtc = nowUtc;
            state.CooldownUntilUtc = nowUtc.Add(DefaultCooldown);
            return new CircuitBreakerEvaluation(
                false,
                state.State,
                state.ConsecutiveFailures,
                "integration.circuit-open");
        }

        state.State = CircuitBreakerStates.Closed;
        return new CircuitBreakerEvaluation(true, state.State, state.ConsecutiveFailures, null);
    }

    public static IntegrationCircuitBreakerState CreateDefault(
        Guid tenantId,
        Guid facilityId,
        Guid connectorId,
        DateTimeOffset nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            ConnectorId = connectorId,
            State = CircuitBreakerStates.Closed,
            FailureThreshold = DefaultFailureThreshold,
            UpdatedAtUtc = nowUtc,
        };
}

public sealed record CircuitBreakerEvaluation(
    bool AllowExecution,
    string State,
    int ConsecutiveFailures,
    string? FailureCode);

public static class BoundedRetryPolicyEngine
{
    public const int MaxAttempts = 5;

    public static RetryDecision Evaluate(string category, int attemptCount)
    {
        if (attemptCount >= MaxAttempts)
        {
            return new RetryDecision(false, IntegrationRetryCategories.Permanent, "retry.exhausted");
        }

        return category switch
        {
            IntegrationRetryCategories.Permanent or IntegrationRetryCategories.Validation or IntegrationRetryCategories.Authorization =>
                new RetryDecision(false, category, "retry.non-retriable"),
            IntegrationRetryCategories.RateLimit =>
                new RetryDecision(true, category, null, TimeSpan.FromSeconds(Math.Min(60, attemptCount * 10))),
            IntegrationRetryCategories.Timeout or IntegrationRetryCategories.Transient =>
                new RetryDecision(true, category, null, TimeSpan.FromSeconds(Math.Min(30, attemptCount * 2))),
            _ => new RetryDecision(true, IntegrationRetryCategories.Transient, null, TimeSpan.FromSeconds(5)),
        };
    }
}

public sealed record RetryDecision(
    bool ShouldRetry,
    string Category,
    string? FailureCode,
    TimeSpan? Delay = null);

public static class DegradedModeEngine
{
    public static string ResolveOperatingMode(IReadOnlyList<SystemComponentHealthSnapshot> snapshots)
    {
        if (snapshots.Any(s =>
                s.ComponentKind == SystemComponentKinds.Database &&
                s.HealthStatus == SystemHealthStatuses.Unavailable))
        {
            return SystemOperatingModes.DegradedReadOnly;
        }

        if (snapshots.Any(s =>
                s.ComponentKind is SystemComponentKinds.IntegrationConnector or SystemComponentKinds.ErpAdapter &&
                s.HealthStatus is SystemHealthStatuses.Unavailable or SystemHealthStatuses.Degraded))
        {
            return SystemOperatingModes.DegradedReadOnly;
        }

        if (snapshots.Any(s =>
                s.ComponentKind == SystemComponentKinds.Api &&
                s.HealthStatus == SystemHealthStatuses.Recovering))
        {
            return SystemOperatingModes.Recovering;
        }

        if (snapshots.Any(s =>
                s.ComponentKind is SystemComponentKinds.EdgeAgent or SystemComponentKinds.MachineTelemetry &&
                s.HealthStatus == SystemHealthStatuses.Unavailable))
        {
            return SystemOperatingModes.DegradedBuffering;
        }

        if (snapshots.Any(s => s.HealthStatus == SystemHealthStatuses.Degraded))
        {
            return SystemOperatingModes.DegradedReadOnly;
        }

        return SystemOperatingModes.FullyOperational;
    }

    public static bool BlocksUnsafeMutation(string operatingMode) =>
        operatingMode is SystemOperatingModes.DegradedReadOnly or SystemOperatingModes.Recovering;
}

public static class RecoveryReconciliationEngine
{
    public static ReconciliationSummary BuildSummary(
        int sourceCount,
        int appliedCount,
        int bufferedCount,
        int unresolvedCount,
        IReadOnlyList<string> conflicts)
    {
        var status = conflicts.Count > 0
            ? ReconciliationRunStatuses.Conflict
            : unresolvedCount > 0
                ? ReconciliationRunStatuses.Failed
                : ReconciliationRunStatuses.Completed;

        return new ReconciliationSummary(
            status,
            sourceCount,
            appliedCount,
            bufferedCount,
            unresolvedCount,
            conflicts,
            JsonSerializer.Serialize(new
            {
                sourceCount,
                appliedCount,
                bufferedCount,
                unresolvedCount,
                conflictCount = conflicts.Count,
            }));
    }
}

public sealed record ReconciliationSummary(
    string Status,
    int SourceCount,
    int AppliedCount,
    int BufferedCount,
    int UnresolvedCount,
    IReadOnlyList<string> Conflicts,
    string SummaryJson);

public static class EdgeEventClassificationEngine
{
    public static string Classify(string eventType)
    {
        if (eventType.Contains("alarm", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("safety", StringComparison.OrdinalIgnoreCase))
        {
            return EdgeEventClassifications.Critical;
        }

        if (eventType.Contains("status", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("cycle", StringComparison.OrdinalIgnoreCase))
        {
            return EdgeEventClassifications.Important;
        }

        if (eventType.Contains("heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            return EdgeEventClassifications.Telemetry;
        }

        return EdgeEventClassifications.Disposable;
    }

    public static bool AllowsSilentDrop(string classification) =>
        classification == EdgeEventClassifications.Disposable;
}

public static class MetricCardinalityGuard
{
    private static readonly HashSet<string> AllowedLabelKeys =
    [
        "service",
        "component",
        "tenant_bucket",
        "facility_bucket",
        "status",
        "error_category",
    ];

    public static IReadOnlyDictionary<string, string> SanitizeLabels(
        IReadOnlyDictionary<string, string> labels)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in labels)
        {
            if (!AllowedLabelKeys.Contains(key))
            {
                continue;
            }

            if (Guid.TryParse(value, out _))
            {
                sanitized[key] = "id-redacted";
                continue;
            }

            sanitized[key] = value.Length > 64 ? value[..64] : value;
        }

        return sanitized;
    }
}
