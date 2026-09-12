namespace NevaxisFactOS.Persistence.ShiftWorkforce;

public sealed record WorkforceSourceRecord(
    string ExternalEmployeeRef,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    bool IsActive,
    string AvailabilityState,
    string? SourceShiftReference,
    string? SourceCalendarReference,
    DateTimeOffset? SourceUpdatedAtUtc,
    string SourceFingerprint,
    string FreshnessState,
    IReadOnlyList<string> SourceLimitations);

public interface IWorkforceSourceProvider
{
    string SourceSystemIdentifier { get; }

    Task<IReadOnlyList<WorkforceSourceRecord>> FetchPersonsAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
