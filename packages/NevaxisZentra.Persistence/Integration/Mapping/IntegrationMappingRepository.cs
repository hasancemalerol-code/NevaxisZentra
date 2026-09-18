using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Integration.Mapping;

public sealed record IntegrationMappingSummaryReadModel(
    Guid Id,
    Guid ConnectorId,
    int VersionNumber,
    string Status,
    string TargetCanonicalEntityType,
    string SourceSchemaFingerprint,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ValidatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    uint Version);

public sealed record CreateIntegrationMappingDraftCommand(
    Guid ConnectorId,
    string TargetCanonicalEntityType,
    string SourceSchemaFingerprint,
    IReadOnlyList<string> SourceHeaders,
    IntegrationMappingDefinition Definition);

public sealed record ActivateIntegrationMappingCommand(
    Guid MappingVersionId,
    uint ExpectedVersion);

public interface IIntegrationMappingRepository
{
    Task<IReadOnlyList<IntegrationMappingSummaryReadModel>> ListByConnectorAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default);

    Task<IntegrationMappingVersion?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        CancellationToken cancellationToken = default);

    Task<IntegrationWriteResult> CreateDraftAsync(
        IntegrationOperationContext context,
        CreateIntegrationMappingDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<IntegrationMappingValidationResult> ValidateDraftAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        IReadOnlyList<string>? sourceHeaders,
        CancellationToken cancellationToken = default);

    Task<IntegrationWriteResult> MarkValidatedAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        uint expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IntegrationWriteResult> ActivateAsync(
        IntegrationOperationContext context,
        ActivateIntegrationMappingCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationMappingRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IIntegrationMappingRepository
{
    public Task<IReadOnlyList<IntegrationMappingSummaryReadModel>> ListByConnectorAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await dbContext.IntegrationMappingVersions.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.ConnectorId == connectorId)
                .OrderByDescending(m => m.VersionNumber)
                .ToListAsync(ct);

            return (IReadOnlyList<IntegrationMappingSummaryReadModel>)rows.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<IntegrationMappingVersion?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.IntegrationMappingVersions
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == mappingVersionId,
                    ct), cancellationToken);

    public Task<IntegrationWriteResult> CreateDraftAsync(
        IntegrationOperationContext context,
        CreateIntegrationMappingDraftCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var connector = await dbContext.IntegrationConnectors
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == command.ConnectorId,
                    ct);
            if (connector is null)
            {
                return IntegrationWriteResult.Fail(IntegrationFailureCodes.NotFound, "Connector not found.");
            }

            command.Definition.TargetCanonicalEntityType = command.TargetCanonicalEntityType;
            command.Definition.SourceEntityType = command.Definition.SourceEntityType.Trim();
            command.Definition.SourceKeyField = command.Definition.SourceKeyField.Trim();

            var validation = IntegrationMappingValidator.ValidateDefinition(
                command.Definition,
                command.SourceHeaders);
            if (!validation.IsValid)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    validation.Issues[0].Message);
            }

            var nextVersion = await dbContext.IntegrationMappingVersions
                .Where(m => m.ConnectorId == command.ConnectorId)
                .Select(m => (int?)m.VersionNumber)
                .MaxAsync(ct) ?? 0;

            var now = DateTimeOffset.UtcNow;
            var mapping = new IntegrationMappingVersion
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectorId = command.ConnectorId,
                VersionNumber = nextVersion + 1,
                Status = IntegrationMappingVersionStatuses.Draft,
                TargetCanonicalEntityType = command.TargetCanonicalEntityType,
                SourceSchemaFingerprint = command.SourceSchemaFingerprint,
                MappingJson = command.Definition.ToJson(),
                CreatedBySubjectId = context.SubjectId,
                CreatedAtUtc = now,
                Version = 1,
            };

            dbContext.IntegrationMappingVersions.Add(mapping);
            IntegrationRepositorySupport.AddAudit(
                dbContext,
                context,
                "integration.mapping.create-draft",
                "integration.mapping.created",
                "integration_mapping_version",
                mapping.Id,
                now,
                new { mapping.ConnectorId, mapping.VersionNumber, mapping.TargetCanonicalEntityType });

            await dbContext.SaveChangesAsync(ct);
            return IntegrationWriteResult.Ok(mapping.Id);
        }, cancellationToken);

    public Task<IntegrationMappingValidationResult> ValidateDraftAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        IReadOnlyList<string>? sourceHeaders,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var mapping = await dbContext.IntegrationMappingVersions.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == mappingVersionId,
                    ct);
            if (mapping is null)
            {
                return IntegrationMappingValidationResult.Invalid([
                    new IntegrationMappingValidationIssue("not-found", "Mapping version not found."),
                ]);
            }

            var definition = IntegrationMappingDefinition.TryParse(mapping.MappingJson);
            if (definition is null)
            {
                return IntegrationMappingValidationResult.Invalid([
                    new IntegrationMappingValidationIssue("invalid-json", "Mapping JSON is invalid."),
                ]);
            }

            return IntegrationMappingValidator.ValidateDefinition(definition, sourceHeaders);
        }, cancellationToken);

    public Task<IntegrationWriteResult> MarkValidatedAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        uint expectedVersion,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var mapping = await dbContext.IntegrationMappingVersions
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == mappingVersionId,
                    ct);
            if (mapping is null)
            {
                return IntegrationWriteResult.Fail(IntegrationFailureCodes.NotFound, "Mapping version not found.");
            }

            if (mapping.Version != expectedVersion)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ConcurrencyConflict,
                    "Mapping version was updated by another session.");
            }

            if (mapping.Status is not IntegrationMappingVersionStatuses.Draft)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    "Only draft mappings can be validated.");
            }

            var definition = IntegrationMappingDefinition.TryParse(mapping.MappingJson);
            if (definition is null)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    "Mapping JSON is invalid.");
            }

            var validation = IntegrationMappingValidator.ValidateDefinition(definition);
            if (!validation.IsValid)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    validation.Issues[0].Message);
            }

            var now = DateTimeOffset.UtcNow;
            mapping.Status = IntegrationMappingVersionStatuses.Validated;
            mapping.ValidatedAtUtc = now;
            mapping.Version += 1;

            IntegrationRepositorySupport.AddAudit(
                dbContext,
                context,
                "integration.mapping.validate",
                "integration.mapping.validated",
                "integration_mapping_version",
                mapping.Id,
                now,
                new { mapping.ConnectorId, mapping.VersionNumber });

            await dbContext.SaveChangesAsync(ct);
            return IntegrationWriteResult.Ok(mapping.Id);
        }, cancellationToken);

    public Task<IntegrationWriteResult> ActivateAsync(
        IntegrationOperationContext context,
        ActivateIntegrationMappingCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var mapping = await dbContext.IntegrationMappingVersions
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MappingVersionId,
                    ct);
            if (mapping is null)
            {
                return IntegrationWriteResult.Fail(IntegrationFailureCodes.NotFound, "Mapping version not found.");
            }

            if (mapping.Version != command.ExpectedVersion)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ConcurrencyConflict,
                    "Mapping version was updated by another session.");
            }

            if (mapping.Status is not IntegrationMappingVersionStatuses.Validated
                and not IntegrationMappingVersionStatuses.Draft)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    "Only validated draft mappings can be activated.");
            }

            var definition = IntegrationMappingDefinition.TryParse(mapping.MappingJson);
            if (definition is null)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    "Mapping JSON is invalid.");
            }

            var validation = IntegrationMappingValidator.ValidateDefinition(definition);
            if (!validation.IsValid)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ValidationFailed,
                    validation.Issues[0].Message);
            }

            var connector = await dbContext.IntegrationConnectors
                .SingleAsync(c => c.Id == mapping.ConnectorId, ct);

            var now = DateTimeOffset.UtcNow;
            var previousActive = await dbContext.IntegrationMappingVersions
                .Where(m => m.ConnectorId == mapping.ConnectorId
                    && m.TargetCanonicalEntityType == mapping.TargetCanonicalEntityType
                    && m.Status == IntegrationMappingVersionStatuses.Active)
                .ToListAsync(ct);

            foreach (var previous in previousActive)
            {
                previous.Status = IntegrationMappingVersionStatuses.Superseded;
                previous.SupersededByMappingVersionId = mapping.Id;
            }

            mapping.Status = IntegrationMappingVersionStatuses.Active;
            mapping.ValidatedAtUtc ??= now;
            mapping.ActivatedAtUtc = now;
            mapping.ActivatedBySubjectId = context.SubjectId;
            mapping.Version += 1;
            connector.ActiveMappingVersionId = mapping.Id;
            connector.UpdatedAtUtc = now;
            connector.Version += 1;

            IntegrationRepositorySupport.AddAudit(
                dbContext,
                context,
                "integration.mapping.activate",
                "integration.mapping.activated",
                "integration_mapping_version",
                mapping.Id,
                now,
                new { mapping.ConnectorId, mapping.VersionNumber, mapping.TargetCanonicalEntityType });

            await dbContext.SaveChangesAsync(ct);
            return IntegrationWriteResult.Ok(mapping.Id);
        }, cancellationToken);

    private static IntegrationMappingSummaryReadModel MapSummary(IntegrationMappingVersion row) =>
        new(
            row.Id,
            row.ConnectorId,
            row.VersionNumber,
            row.Status,
            row.TargetCanonicalEntityType,
            row.SourceSchemaFingerprint,
            row.CreatedAtUtc,
            row.ValidatedAtUtc,
            row.ActivatedAtUtc,
            row.Version);
}

public sealed class UnavailableIntegrationMappingRepository : IIntegrationMappingRepository
{
    public Task<IReadOnlyList<IntegrationMappingSummaryReadModel>> ListByConnectorAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IntegrationMappingSummaryReadModel>>([]);

    public Task<IntegrationMappingVersion?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IntegrationMappingVersion?>(null);

    public Task<IntegrationWriteResult> CreateDraftAsync(
        IntegrationOperationContext context,
        CreateIntegrationMappingDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationWriteResult.Fail("integration.unavailable", "Persistence unavailable."));

    public Task<IntegrationMappingValidationResult> ValidateDraftAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        IReadOnlyList<string>? sourceHeaders,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationMappingValidationResult.Invalid([
            new IntegrationMappingValidationIssue("unavailable", "Persistence unavailable."),
        ]));

    public Task<IntegrationWriteResult> MarkValidatedAsync(
        IntegrationOperationContext context,
        Guid mappingVersionId,
        uint expectedVersion,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationWriteResult.Fail("integration.unavailable", "Persistence unavailable."));

    public Task<IntegrationWriteResult> ActivateAsync(
        IntegrationOperationContext context,
        ActivateIntegrationMappingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationWriteResult.Fail("integration.unavailable", "Persistence unavailable."));
}
