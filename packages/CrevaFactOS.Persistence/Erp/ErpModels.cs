namespace CrevaFactOS.Persistence.Erp;

public sealed record ErpOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class ErpFailureCodes
{
    public const string InvalidScope = "erp.invalid-scope";
    public const string NotFound = "erp.not-found";
    public const string ConcurrencyConflict = "erp.concurrency-conflict";
    public const string ValidationFailed = "erp.validation-failed";
    public const string SyncAlreadyRunning = "erp.sync-already-running";
    public const string CapabilityUnsupported = "erp.capability-unsupported";
    public const string ProviderUnsupported = "erp.provider-unsupported";
    public const string SourceUnavailable = "erp.source-unavailable";
}

public sealed class ErpWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ErpWriteResult Success(Guid entityId, uint version) =>
        new() { Succeeded = true, EntityId = entityId, Version = version };

    public static ErpWriteResult Fail(string code, string? reason = null) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed class ErpSyncResult
{
    public bool Succeeded { get; init; }
    public Guid? SyncRunId { get; init; }
    public string? Status { get; init; }
    public int RecordsRead { get; init; }
    public int RecordsCreated { get; init; }
    public int RecordsUpdated { get; init; }
    public int RecordsSkipped { get; init; }
    public int RecordsFailed { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ErpSyncResult Ok(
        Guid syncRunId,
        string status,
        int read,
        int created,
        int updated,
        int skipped,
        int failed) =>
        new()
        {
            Succeeded = true,
            SyncRunId = syncRunId,
            Status = status,
            RecordsRead = read,
            RecordsCreated = created,
            RecordsUpdated = updated,
            RecordsSkipped = skipped,
            RecordsFailed = failed,
        };

    public static ErpSyncResult Fail(string code, string? reason = null, Guid? syncRunId = null) =>
        new()
        {
            Succeeded = false,
            SyncRunId = syncRunId,
            FailureCode = code,
            FailureReason = reason,
        };
}

public sealed record ErpConnectionSummaryReadModel(
    Guid Id,
    Guid FacilityId,
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string ConnectionMode,
    string Status,
    DateTimeOffset? LastSuccessfulConnectionAtUtc,
    DateTimeOffset? LastFailedConnectionAtUtc,
    DateTimeOffset? LastHealthCheckAtUtc,
    int? LastHealthLatencyMs,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    string? CredentialReference);

public sealed record ErpHealthCheckReadModel(
    Guid ConnectionProfileId,
    string Provider,
    string Status,
    bool IsHealthy,
    DateTimeOffset CheckedAtUtc,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    uint Version);

public sealed record ErpSyncRunSummaryReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string EntityType,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    int RecordsRead,
    int RecordsCreated,
    int RecordsUpdated,
    int RecordsSkipped,
    int RecordsFailed,
    string? ErrorCode,
    string? ErrorMessage,
    string TriggeredBySubjectId,
    IReadOnlyList<ErpIntegrationErrorReadModel> Errors);

public sealed record ErpIntegrationErrorReadModel(
    string Code,
    string EntityType,
    string? ExternalId,
    string Message,
    DateTimeOffset OccurredAtUtc);

public sealed record CanonicalErpCustomerReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? TaxNumber,
    string? TaxOffice,
    string? Email,
    string? Phone,
    string? CountryCode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CanonicalErpProductReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ErpWorkspaceSummaryReadModel(
    int ConnectionCount,
    int HealthyCount,
    int ProblematicCount,
    DateTimeOffset? LastSyncAtUtc,
    int CustomerCount,
    int ProductCount);

public sealed record CreateErpConnectionCommand(
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);

public sealed record UpdateErpConnectionCommand(
    Guid ConnectionId,
    uint ExpectedVersion,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);
