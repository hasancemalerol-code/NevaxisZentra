using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ShiftWorkforce;

public sealed class ShiftWorkforceSourceSynchronizer(
    CrevaFactOSDbContext dbContext,
    IEnumerable<IWorkforceSourceProvider> sourceProviders)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ShiftWorkforceSynchronizeResult> SynchronizeAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var createdCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();

        foreach (var provider in sourceProviders)
        {
            var records = await provider.FetchPersonsAsync(
                context.TenantId, context.FacilityId, cancellationToken);

            foreach (var record in records)
            {
                if (!ShiftWorkforceEmployeeCodeNormalizer.TryNormalize(
                        record.EmployeeCode, out var displayCode, out var normalizedCode, out _))
                {
                    continue;
                }

                var existing = await dbContext.ShiftWorkforcePersons
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.SourceSystem == provider.SourceSystemIdentifier
                             && p.ExternalEmployeeRef == record.ExternalEmployeeRef,
                        cancellationToken);

                if (existing is null)
                {
                    var person = new ShiftWorkforcePerson
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        SourceSystem = provider.SourceSystemIdentifier,
                        ExternalEmployeeRef = record.ExternalEmployeeRef,
                        EmployeeCode = displayCode,
                        EmployeeCodeNormalized = normalizedCode,
                        DisplayName = record.DisplayName,
                        Department = record.Department,
                        JobTitle = record.JobTitle,
                        IsActive = record.IsActive,
                        AvailabilityState = record.AvailabilityState,
                        SourceShiftReference = record.SourceShiftReference,
                        SourceCalendarReference = record.SourceCalendarReference,
                        SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                        SynchronizedAtUtc = now,
                        SourceFingerprint = record.SourceFingerprint,
                        FreshnessState = record.FreshnessState,
                        SourceLimitationsJson = JsonSerializer.Serialize(record.SourceLimitations, JsonOptions),
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        Version = 1,
                    };

                    dbContext.ShiftWorkforcePersons.Add(person);
                    dbContext.ShiftWorkforceAuditEvents.Add(BuildAuditEvent(
                        context, person.Id, ShiftWorkforceAuditEventTypes.PersonSynchronized,
                        ShiftWorkforceEntityTypes.Person,
                        new { action = "created", sourceSystem = provider.SourceSystemIdentifier, externalRef = record.ExternalEmployeeRef },
                        actor, now));
                    createdCount++;
                    continue;
                }

                if (existing.SourceFingerprint == record.SourceFingerprint)
                {
                    unchangedCount++;
                    continue;
                }

                ApplyErpOwnedFields(existing, record, displayCode, normalizedCode, now);
                existing.SynchronizedAtUtc = now;
                existing.UpdatedAtUtc = now;
                existing.Version++;

                dbContext.ShiftWorkforceAuditEvents.Add(BuildAuditEvent(
                    context, existing.Id, ShiftWorkforceAuditEventTypes.PersonSynchronized,
                    ShiftWorkforceEntityTypes.Person,
                    new { action = "updated", sourceSystem = provider.SourceSystemIdentifier, externalRef = record.ExternalEmployeeRef },
                    actor, now));
                updatedCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShiftWorkforceSynchronizeResult(createdCount, updatedCount, unchangedCount);
    }

    internal static void ApplyErpOwnedFields(
        ShiftWorkforcePerson person,
        WorkforceSourceRecord record,
        string displayCode,
        string normalizedCode,
        DateTimeOffset now)
    {
        person.EmployeeCode = displayCode;
        person.EmployeeCodeNormalized = normalizedCode;
        person.DisplayName = record.DisplayName;
        person.Department = record.Department;
        person.JobTitle = record.JobTitle;
        person.IsActive = record.IsActive;
        person.AvailabilityState = record.AvailabilityState;
        person.SourceShiftReference = record.SourceShiftReference;
        person.SourceCalendarReference = record.SourceCalendarReference;
        person.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        person.SourceFingerprint = record.SourceFingerprint;
        person.FreshnessState = record.FreshnessState;
        person.SourceLimitationsJson = JsonSerializer.Serialize(record.SourceLimitations, JsonOptions);
        _ = now;
    }

    private static ShiftWorkforceAuditEvent BuildAuditEvent(
        ShiftWorkforceOperationContext context,
        Guid entityId,
        string eventType,
        string entityType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actor,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = context.CorrelationId,
        };
}
