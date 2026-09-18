using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.ShiftWorkforce;

public interface IShiftWorkforceRepository
{
    Task<ShiftWorkforceOverviewReadModel> GetOverviewAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ShiftWorkforcePersonListReadModel> ListPersonsAsync(
        ShiftWorkforceOperationContext context,
        ShiftWorkforcePersonListFilters filters,
        CancellationToken cancellationToken = default);

    Task<ShiftWorkforcePersonDetailReadModel?> GetPersonByIdAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftWorkforceCompetencyDefinitionReadModel>> ListCompetencyDefinitionsAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel>> ListPersonCompetenciesAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<ShiftWorkforceWriteResult> CreateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CreateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftWorkforceWriteResult> UpdateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        UpdateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftWorkforceSynchronizeResult> SynchronizeAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default);
}

public sealed class ShiftWorkforceRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ShiftWorkforceSourceSynchronizer sourceSynchronizer) : IShiftWorkforceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<ShiftWorkforceOverviewReadModel> GetOverviewAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var persons = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                var assignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
                    .Include(a => a.CompetencyDefinition)
                    .Where(a => a.TenantId == context.TenantId && a.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                var now = DateTimeOffset.UtcNow;
                var competencyAttentionCount = assignments.Count(a => ShiftWorkforceValueConversions.RequiresCompetencyAttention(a, now));

                return new ShiftWorkforceOverviewReadModel(
                    persons.Count,
                    persons.Count(p => p.IsActive),
                    persons.Count(ShiftWorkforceValueConversions.IsOperationallyAvailable),
                    persons.Count(p => p.IsActive && p.AvailabilityState != ShiftWorkforceAvailabilityStates.Available),
                    persons.Count(p => p.FreshnessState == ShiftWorkforceFreshnessStates.Stale),
                    competencyAttentionCount);
            },
            cancellationToken);

    public Task<ShiftWorkforcePersonListReadModel> ListPersonsAsync(
        ShiftWorkforceOperationContext context,
        ShiftWorkforcePersonListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Search))
                {
                    var search = filters.Search.Trim();
                    query = query.Where(p =>
                        p.DisplayName.Contains(search)
                        || p.EmployeeCode.Contains(search)
                        || (p.Department != null && p.Department.Contains(search))
                        || (p.JobTitle != null && p.JobTitle.Contains(search)));
                }

                if (filters.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == filters.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Availability))
                {
                    var availability = filters.Availability.Trim();
                    query = query.Where(p => p.AvailabilityState == availability);
                }

                if (!string.IsNullOrWhiteSpace(filters.Freshness))
                {
                    var freshness = filters.Freshness.Trim();
                    query = query.Where(p => p.FreshnessState == freshness);
                }

                if (!string.IsNullOrWhiteSpace(filters.CompetencyCode))
                {
                    var competencyCode = filters.CompetencyCode.Trim();
                    query = query.Where(p => p.CompetencyAssignments.Any(a =>
                        a.CompetencyDefinition != null && a.CompetencyDefinition.Code == competencyCode));
                }

                var persons = await query
                    .Include(p => p.CompetencyAssignments)
                    .ThenInclude(a => a.CompetencyDefinition)
                    .ToListAsync(ct);

                var totalCount = persons.Count;
                var now = DateTimeOffset.UtcNow;

                var items = persons
                    .OrderBy(p => p.DisplayName)
                    .ThenBy(p => p.EmployeeCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p =>
                    {
                        var competencies = p.CompetencyAssignments
                            .Select(a => MapAssignment(a, now))
                            .ToList();

                        return new ShiftWorkforcePersonSummaryReadModel(
                            p.Id,
                            p.EmployeeCode,
                            p.DisplayName,
                            p.Department,
                            p.JobTitle,
                            p.IsActive,
                            p.AvailabilityState,
                            p.SourceShiftReference,
                            p.SourceSystem,
                            p.FreshnessState,
                            ShiftWorkforceValueConversions.BuildCompetencySummary(competencies),
                            competencies.Any(c => c.RequiresAttention));
                    })
                    .ToList();

                return new ShiftWorkforcePersonListReadModel(items, totalCount, page, pageSize);
            },
            cancellationToken);
    }

    public Task<ShiftWorkforcePersonDetailReadModel?> GetPersonByIdAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .Include(p => p.CompetencyAssignments)
                    .ThenInclude(a => a.CompetencyDefinition)
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == personId,
                        ct);

                return person is null ? null : MapPersonDetail(person);
            },
            cancellationToken);

    public Task<IReadOnlyList<ShiftWorkforceCompetencyDefinitionReadModel>> ListCompetencyDefinitionsAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var definitions = await dbContext.ShiftWorkforceCompetencyDefinitions.AsNoTracking()
                    .Where(d => d.TenantId == context.TenantId && d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .ThenBy(d => d.Code)
                    .ToListAsync(ct);

                return (IReadOnlyList<ShiftWorkforceCompetencyDefinitionReadModel>)definitions
                    .Select(d => new ShiftWorkforceCompetencyDefinitionReadModel(
                        d.Code, d.DisplayLabelTr, d.DescriptionTr, d.IsActive, d.SortOrder))
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel>> ListPersonCompetenciesAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var assignments = await dbContext.ShiftWorkforceCompetencyAssignments.AsNoTracking()
                    .Include(a => a.CompetencyDefinition)
                    .Where(a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.WorkforcePersonId == personId)
                    .OrderBy(a => a.CompetencyDefinition!.SortOrder)
                    .ThenBy(a => a.CompetencyDefinition!.Code)
                    .ToListAsync(ct);

                var now = DateTimeOffset.UtcNow;
                return (IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel>)assignments
                    .Select(a => MapAssignment(a, now))
                    .ToList();
            },
            cancellationToken);

    public Task<ShiftWorkforceWriteResult> CreateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CreateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            context,
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.ValidationFailed,
                        "Idempotency key is required.");
                }

                var person = await dbContext.ShiftWorkforcePersons
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == personId,
                        ct);

                if (person is null)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.NotFound,
                        "Workforce person not found.");
                }

                if (!person.IsActive)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.PersonInactive,
                        "Competency enrichment is not allowed for inactive persons.");
                }

                var definition = await dbContext.ShiftWorkforceCompetencyDefinitions
                    .FirstOrDefaultAsync(
                        d => d.TenantId == context.TenantId
                             && d.Code == command.CompetencyDefinitionCode.Trim()
                             && d.IsActive,
                        ct);

                if (definition is null)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.CompetencyDefinitionNotFound,
                        "Competency definition not found.");
                }

                var idempotencyKey = command.IdempotencyKey.Trim();
                var fingerprint = ShiftWorkforceIdempotencyFingerprint.ComputeCreateCompetencyFingerprint(
                    context, personId, command, definition.Id);

                var existing = await dbContext.ShiftWorkforceCompetencyAssignments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.WorkforcePersonId == personId
                             && a.CreateIdempotencyKey == idempotencyKey,
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, fingerprint);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var assignment = new ShiftWorkforceCompetencyAssignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkforcePersonId = personId,
                    CompetencyDefinitionId = definition.Id,
                    MachineFamilyScope = TrimOptional(command.MachineFamilyScope),
                    ProcessScope = TrimOptional(command.ProcessScope),
                    MaterialScope = TrimOptional(command.MaterialScope),
                    EquipmentScope = TrimOptional(command.EquipmentScope),
                    CompetencyLevel = command.CompetencyLevel.Trim(),
                    VerificationStatus = command.VerificationStatus.Trim(),
                    ValidFromUtc = command.ValidFromUtc,
                    ValidUntilUtc = command.ValidUntilUtc,
                    VerifiedBySubjectId = command.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified
                        ? actor
                        : null,
                    VerifiedAtUtc = command.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified
                        ? now
                        : null,
                    OperationalNote = TrimOptional(command.OperationalNote),
                    CreateIdempotencyKey = idempotencyKey,
                    CreateIdempotencyPayloadFingerprint = fingerprint,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                    Version = 1,
                };

                dbContext.ShiftWorkforceCompetencyAssignments.Add(assignment);
                dbContext.ShiftWorkforceAuditEvents.Add(BuildAuditEvent(
                    context, assignment.Id, ShiftWorkforceAuditEventTypes.CompetencyCreated,
                    ShiftWorkforceEntityTypes.CompetencyAssignment,
                    new { personId, competencyCode = definition.Code },
                    actor, now));

                try
                {
                    await dbContext.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    var raced = await dbContext.ShiftWorkforceCompetencyAssignments
                        .FirstOrDefaultAsync(
                            a => a.TenantId == context.TenantId
                                 && a.WorkforcePersonId == personId
                                 && a.CreateIdempotencyKey == idempotencyKey,
                            ct);

                    if (raced is not null)
                    {
                        return ResolveCreateIdempotency(raced, fingerprint);
                    }

                    throw;
                }

                return ShiftWorkforceWriteResult.Success(assignment.Id, assignment.Version);
            },
            cancellationToken);

    public Task<ShiftWorkforceWriteResult> UpdateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        UpdateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(
            context,
            async ct =>
            {
                var person = await dbContext.ShiftWorkforcePersons.AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == personId,
                        ct);

                if (person is null)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.NotFound,
                        "Workforce person not found.");
                }

                if (!person.IsActive)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.PersonInactive,
                        "Competency enrichment is not allowed for inactive persons.");
                }

                var assignment = await dbContext.ShiftWorkforceCompetencyAssignments
                    .FirstOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.WorkforcePersonId == personId
                             && a.Id == command.AssignmentId,
                        ct);

                if (assignment is null)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.NotFound,
                        "Competency assignment not found.");
                }

                if (assignment.Version != command.ExpectedVersion)
                {
                    return ShiftWorkforceWriteResult.Fail(
                        ShiftWorkforceFailureCodes.ConcurrencyConflict,
                        "The competency assignment was modified by another user.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();

                if (!string.IsNullOrWhiteSpace(command.CompetencyLevel))
                {
                    assignment.CompetencyLevel = command.CompetencyLevel.Trim();
                }

                if (!string.IsNullOrWhiteSpace(command.VerificationStatus))
                {
                    assignment.VerificationStatus = command.VerificationStatus.Trim();
                    if (assignment.VerificationStatus == ShiftWorkforceVerificationStatuses.Verified)
                    {
                        assignment.VerifiedBySubjectId = actor;
                        assignment.VerifiedAtUtc = now;
                    }
                }

                if (command.ValidFromUtc.HasValue)
                {
                    assignment.ValidFromUtc = command.ValidFromUtc;
                }

                if (command.ValidUntilUtc.HasValue)
                {
                    assignment.ValidUntilUtc = command.ValidUntilUtc;
                }

                if (command.OperationalNote is not null)
                {
                    assignment.OperationalNote = TrimOptional(command.OperationalNote);
                }

                assignment.UpdatedAtUtc = now;
                assignment.UpdatedBy = actor;
                assignment.Version++;

                dbContext.ShiftWorkforceAuditEvents.Add(BuildAuditEvent(
                    context, assignment.Id, ShiftWorkforceAuditEventTypes.CompetencyUpdated,
                    ShiftWorkforceEntityTypes.CompetencyAssignment,
                    new { personId, assignmentId = assignment.Id },
                    actor, now));

                await dbContext.SaveChangesAsync(ct);
                return ShiftWorkforceWriteResult.Success(assignment.Id, assignment.Version);
            },
            cancellationToken);

    public Task<ShiftWorkforceSynchronizeResult> SynchronizeAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => sourceSynchronizer.SynchronizeAsync(context, ct),
            cancellationToken);

    private async Task<ShiftWorkforceWriteResult> ExecuteWriteAsync(
        ShiftWorkforceOperationContext context,
        Func<CancellationToken, Task<ShiftWorkforceWriteResult>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ShiftWorkforceWriteResult.Fail(
                ShiftWorkforceFailureCodes.ConcurrencyConflict,
                "The record was modified by another user.");
        }
    }

    private static ShiftWorkforceWriteResult ResolveCreateIdempotency(
        ShiftWorkforceCompetencyAssignment existing,
        string fingerprint)
    {
        if (string.IsNullOrEmpty(existing.CreateIdempotencyPayloadFingerprint))
        {
            return ShiftWorkforceWriteResult.Fail(
                ShiftWorkforceFailureCodes.IdempotencyLegacyUnverifiable,
                "The idempotency key belongs to a legacy create that cannot be verified safely.");
        }

        if (existing.CreateIdempotencyPayloadFingerprint == fingerprint)
        {
            return ShiftWorkforceWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
        }

        return ShiftWorkforceWriteResult.Fail(
            ShiftWorkforceFailureCodes.IdempotencyConflict,
            "The idempotency key was already used with a different command payload.");
    }

    private static ShiftWorkforcePersonDetailReadModel MapPersonDetail(ShiftWorkforcePerson person)
    {
        var now = DateTimeOffset.UtcNow;
        var competencies = person.CompetencyAssignments
            .Select(a => MapAssignment(a, now))
            .ToList();

        var limitations = DeserializeLimitations(person.SourceLimitationsJson);

        return new ShiftWorkforcePersonDetailReadModel(
            person.Id,
            person.EmployeeCode,
            person.DisplayName,
            person.Department,
            person.JobTitle,
            person.IsActive,
            person.AvailabilityState,
            person.SourceShiftReference,
            person.SourceSystem,
            person.FreshnessState,
            person.SourceShiftReference,
            person.SourceCalendarReference,
            person.SourceUpdatedAtUtc,
            person.SynchronizedAtUtc,
            person.SourceFingerprint,
            limitations,
            person.SourceSystem == ShiftWorkforceSourceSystems.LocalDevelopmentPilot,
            competencies,
            person.Version);
    }

    private static ShiftWorkforceCompetencyAssignmentReadModel MapAssignment(
        ShiftWorkforceCompetencyAssignment assignment,
        DateTimeOffset nowUtc) =>
        new(
            assignment.Id,
            assignment.CompetencyDefinition?.Code ?? string.Empty,
            assignment.CompetencyDefinition?.DisplayLabelTr ?? string.Empty,
            assignment.MachineFamilyScope,
            assignment.ProcessScope,
            assignment.MaterialScope,
            assignment.EquipmentScope,
            assignment.CompetencyLevel,
            assignment.VerificationStatus,
            assignment.ValidFromUtc,
            assignment.ValidUntilUtc,
            assignment.VerifiedAtUtc,
            assignment.OperationalNote,
            assignment.Version,
            ShiftWorkforceValueConversions.RequiresCompetencyAttention(assignment, nowUtc));

    private static List<string> DeserializeLimitations(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

public sealed class UnavailableShiftWorkforceRepository : IShiftWorkforceRepository
{
    private static ShiftWorkforceWriteResult Unavailable() =>
        ShiftWorkforceWriteResult.Fail("shift-workforce.unavailable", "Shift workforce persistence is unavailable.");

    public Task<ShiftWorkforceOverviewReadModel> GetOverviewAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftWorkforceOverviewReadModel(0, 0, 0, 0, 0, 0));

    public Task<ShiftWorkforcePersonListReadModel> ListPersonsAsync(
        ShiftWorkforceOperationContext context,
        ShiftWorkforcePersonListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftWorkforcePersonListReadModel([], 0, filters.Page, filters.PageSize));

    public Task<ShiftWorkforcePersonDetailReadModel?> GetPersonByIdAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ShiftWorkforcePersonDetailReadModel?>(null);

    public Task<IReadOnlyList<ShiftWorkforceCompetencyDefinitionReadModel>> ListCompetencyDefinitionsAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftWorkforceCompetencyDefinitionReadModel>>([]);

    public Task<IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel>> ListPersonCompetenciesAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel>>([]);

    public Task<ShiftWorkforceWriteResult> CreateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CreateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ShiftWorkforceWriteResult> UpdateCompetencyAssignmentAsync(
        ShiftWorkforceOperationContext context,
        Guid personId,
        UpdateShiftWorkforceCompetencyCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ShiftWorkforceSynchronizeResult> SynchronizeAsync(
        ShiftWorkforceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftWorkforceSynchronizeResult(0, 0, 0));
}
