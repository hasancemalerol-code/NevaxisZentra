namespace NevaxisFactOS.Persistence.Integration;

public sealed record IntegrationOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId = null)
{
    public string SubjectId => IdentitySubjectId.ToString();
}

public sealed record IntegrationConnectorSummaryReadModel(
    Guid Id,
    Guid FacilityId,
    string Name,
    string ConnectorKind,
    string SourceSystemType,
    string SourceSystemName,
    string Environment,
    string Status,
    bool Enabled,
    string HealthStatus,
    string FreshnessState,
    string QualityState,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastAttemptedSyncAtUtc,
    string DomainBindingKind,
    Guid? DomainBindingId,
    uint Version);

public sealed record CreateIntegrationConnectorCommand(
    string Name,
    string? Description,
    string ConnectorKind,
    string SourceSystemType,
    string SourceSystemName,
    string Environment,
    string AuthenticationMode,
    string? SecretReference,
    string? EndpointOrLocation,
    string SynchronizationMode,
    string? ScheduleExpression,
    string DomainBindingKind,
    Guid? DomainBindingId,
    string ConfigurationJson);

public sealed record UpdateIntegrationConnectorCommand(
    Guid ConnectorId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string Environment,
    bool Enabled,
    string AuthenticationMode,
    string? SecretReference,
    string? EndpointOrLocation,
    string SynchronizationMode,
    string? ScheduleExpression,
    string ConfigurationJson);

public sealed record IntegrationWriteResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? ConnectorId = null)
{
    public static IntegrationWriteResult Ok(Guid connectorId) =>
        new(true, null, null, connectorId);

    public static IntegrationWriteResult Fail(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}

public static class IntegrationFailureCodes
{
    public const string ValidationFailed = "integration.validation-failed";
    public const string NotFound = "integration.not-found";
    public const string ConcurrencyConflict = "integration.concurrency-conflict";
    public const string SecretExposureBlocked = "integration.secret-exposure-blocked";
}
