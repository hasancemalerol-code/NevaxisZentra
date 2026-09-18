using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Mold;

public interface IMoldCompatibilityRepository
{
    Task<MoldCompatibilityRequirementReadModel?> GetRequirementAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> UpsertRequirementAsync(
        MoldOperationContext context,
        UpsertMoldCompatibilityRequirementCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MachineCompatibilityCapabilityReadModel>> ListMachineProfilesAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default);

    Task<MachineCompatibilityCapabilityReadModel?> GetMachineProfileAsync(
        MoldOperationContext context,
        Guid machineAssetId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> UpsertMachineProfileAsync(
        MoldOperationContext context,
        UpsertMachineCompatibilityCapabilityCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldCompatibilityEvaluationResult> EvaluateAsync(
        MoldOperationContext context,
        EvaluateMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldCompatibilityAssessmentReadModel>> EvaluateAllAsync(
        MoldOperationContext context,
        EvaluateAllMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldCompatibilityOverviewReadModel?> GetOverviewAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default);

    Task<MoldCompatibilityAssessmentReadModel?> GetAssessmentAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}

public sealed class MoldCompatibilityRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMoldCompatibilityRepository
{
    public Task<MoldCompatibilityRequirementReadModel?> GetRequirementAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return null;
            }

            var entity = await dbContext.MoldCompatibilityRequirements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == moldId,
                    ct);

            return entity is null ? null : MapRequirement(entity);
        }, cancellationToken);

    public Task<MoldWriteResult> UpsertRequirementAsync(
        MoldOperationContext context,
        UpsertMoldCompatibilityRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validationError = MoldCompatibilityValidator.ValidateRequirementCommand(command);
            if (validationError is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, validationError);
            }

            if (!await MoldExistsAsync(context, command.MoldId, ct))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var subjectId = context.IdentitySubjectId.ToString();
            var entity = await dbContext.MoldCompatibilityRequirements
                .FirstOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == command.MoldId,
                    ct);

            if (entity is null)
            {
                entity = new MoldCompatibilityRequirement
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MoldId = command.MoldId,
                    CreatedAtUtc = now,
                    CreatedBySubjectId = subjectId,
                    Version = 1,
                };
                dbContext.MoldCompatibilityRequirements.Add(entity);
            }
            else if (command.ExpectedVersion is not null && entity.Version != command.ExpectedVersion.Value)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "Compatibility requirement was updated by another request.");
            }

            ApplyRequirement(entity, command, now, subjectId);
            await dbContext.SaveChangesAsync(ct);
            AddAudit(
                context,
                MoldAuditActions.CompatibilityRequirementUpserted,
                MoldAuditEventTypes.CompatibilityRequirementUpserted,
                "mold.compatibility.requirement",
                entity.Id,
                now,
                new { moldId = command.MoldId });

            return MoldWriteResult.Success(entity.Id, entity.Version);
        }, cancellationToken);

    public Task<IReadOnlyList<MachineCompatibilityCapabilityReadModel>> ListMachineProfilesAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var profiles = await dbContext.MachineCompatibilityCapabilityProfiles
                .AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                .OrderBy(p => p.MachineDisplayName)
                .ThenBy(p => p.MachineCode)
                .ThenBy(p => p.MachineAssetId)
                .ToListAsync(ct);

            return (IReadOnlyList<MachineCompatibilityCapabilityReadModel>)profiles.Select(MapMachineProfile).ToList();
        }, cancellationToken);

    public Task<MachineCompatibilityCapabilityReadModel?> GetMachineProfileAsync(
        MoldOperationContext context,
        Guid machineAssetId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var entity = await dbContext.MachineCompatibilityCapabilityProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.MachineAssetId == machineAssetId,
                    ct);

            return entity is null ? null : MapMachineProfile(entity);
        }, cancellationToken);

    public Task<MoldWriteResult> UpsertMachineProfileAsync(
        MoldOperationContext context,
        UpsertMachineCompatibilityCapabilityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validationError = MoldCompatibilityValidator.ValidateMachineProfileCommand(command);
            if (validationError is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, validationError);
            }

            var machineVisible = await dbContext.MachineRegistries
                .AsNoTracking()
                .AnyAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MachineAssetId,
                    ct);

            if (!machineVisible)
            {
                return MoldWriteResult.Fail(
                    MoldCompatibilityFailureCodes.MachineNotVisible,
                    "Machine is not visible in this facility.");
            }

            var now = DateTimeOffset.UtcNow;
            var subjectId = context.IdentitySubjectId.ToString();
            var entity = await dbContext.MachineCompatibilityCapabilityProfiles
                .FirstOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.MachineAssetId == command.MachineAssetId,
                    ct);

            if (entity is null)
            {
                entity = new MachineCompatibilityCapabilityProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    MachineAssetId = command.MachineAssetId,
                    CreatedAtUtc = now,
                    CreatedBySubjectId = subjectId,
                    Version = 1,
                };
                dbContext.MachineCompatibilityCapabilityProfiles.Add(entity);
            }
            else if (command.ExpectedVersion is not null && entity.Version != command.ExpectedVersion.Value)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "Machine compatibility profile was updated by another request.");
            }

            ApplyMachineProfile(entity, command, now, subjectId);
            await dbContext.SaveChangesAsync(ct);
            AddAudit(
                context,
                MoldAuditActions.CompatibilityMachineProfileUpserted,
                MoldAuditEventTypes.CompatibilityMachineProfileUpserted,
                "mold.compatibility.machine-profile",
                entity.Id,
                now,
                new { machineAssetId = command.MachineAssetId });

            return MoldWriteResult.Success(entity.Id, entity.Version);
        }, cancellationToken);

    public Task<MoldCompatibilityEvaluationResult> EvaluateAsync(
        MoldOperationContext context,
        EvaluateMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => EvaluateCoreAsync(context, command.MoldId, command.MachineAssetId, ct),
            cancellationToken);

    private async Task<MoldCompatibilityEvaluationResult> EvaluateCoreAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid machineAssetId,
        CancellationToken cancellationToken)
    {
        if (!await MoldExistsAsync(context, moldId, cancellationToken))
        {
            return FailEvaluation(MoldFailureCodes.NotFound, "Mold not found.");
        }

        var requirementEntity = await dbContext.MoldCompatibilityRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId,
                cancellationToken);

        var machineProfileEntity = await dbContext.MachineCompatibilityCapabilityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TenantId == context.TenantId
                    && p.FacilityId == context.FacilityId
                    && p.MachineAssetId == machineAssetId,
                cancellationToken);

        var moldRegistry = await dbContext.Molds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.Id == moldId,
                cancellationToken);

        var requirement = requirementEntity is null ? null : MapRequirement(requirementEntity);
        var machineProfile = machineProfileEntity is null ? null : MapMachineProfile(machineProfileEntity);

        if (requirement is null || machineProfile is null)
        {
            var output = MoldCompatibilityEvaluator.Evaluate(new MoldCompatibilityEvaluator.EvaluationInput(
                requirement,
                machineProfile,
                moldRegistry));
            var assessment = await PersistAssessmentAsync(
                context,
                moldId,
                machineAssetId,
                machineProfile,
                requirement,
                output,
                $"{moldId}:{machineAssetId}:missing-profile",
                cancellationToken);
            return SuccessEvaluation(assessment, false);
        }

        var fingerprint = MoldCompatibilityFingerprint.ComputeEvaluationFingerprint(requirement, machineProfile);
        var existing = await dbContext.MoldMachineCompatibilityAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                    && a.FacilityId == context.FacilityId
                    && a.MoldId == moldId
                    && a.MachineAssetId == machineAssetId
                    && a.InputFingerprint == fingerprint,
                cancellationToken);

        if (existing is not null)
        {
            return SuccessEvaluation(MapAssessment(existing, machineProfile, idempotentReplay: true), true);
        }

        var evaluation = MoldCompatibilityEvaluator.Evaluate(new MoldCompatibilityEvaluator.EvaluationInput(
            requirement,
            machineProfile,
            moldRegistry));

        var created = await PersistAssessmentAsync(
            context,
            moldId,
            machineAssetId,
            machineProfile,
            requirement,
            evaluation,
            fingerprint,
            cancellationToken);

        return SuccessEvaluation(created, created.IdempotentReplay);
    }

    public Task<IReadOnlyList<MoldCompatibilityAssessmentReadModel>> EvaluateAllAsync(
        MoldOperationContext context,
        EvaluateAllMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, command.MoldId, ct))
            {
                return (IReadOnlyList<MoldCompatibilityAssessmentReadModel>)Array.Empty<MoldCompatibilityAssessmentReadModel>();
            }

            var machineProfiles = await dbContext.MachineCompatibilityCapabilityProfiles
                .AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                .OrderBy(p => p.MachineDisplayName)
                .ThenBy(p => p.MachineCode)
                .ThenBy(p => p.MachineAssetId)
                .ToListAsync(ct);

            var results = new List<MoldCompatibilityAssessmentReadModel>();
            foreach (var profileEntity in machineProfiles)
            {
                var result = await EvaluateCoreAsync(
                    context,
                    command.MoldId,
                    profileEntity.MachineAssetId,
                    ct);
                if (result.Assessment is not null)
                {
                    results.Add(result.Assessment);
                }
            }

            return (IReadOnlyList<MoldCompatibilityAssessmentReadModel>)SortAssessments(results);
        }, cancellationToken);

    public Task<MoldCompatibilityOverviewReadModel?> GetOverviewAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return null;
            }

            var requirement = await dbContext.MoldCompatibilityRequirements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == moldId,
                    ct);

            var latestAssessments = await LoadLatestAssessmentsAsync(context, moldId, ct);
            var summaries = latestAssessments
                .Select(a => new MoldCompatibilityAssessmentSummaryReadModel(
                    a.Id,
                    a.MachineAssetId,
                    a.MachineCode,
                    a.MachineDisplayName,
                    a.Status,
                    a.Reasons.FirstOrDefault(r => r.Status == MoldCompatibilityRuleStatuses.Fail)?.LabelTr
                        ?? a.Reasons.FirstOrDefault(r => r.Status == MoldCompatibilityRuleStatuses.Unknown)?.LabelTr,
                    a.RequirementStale,
                    a.MachineProfileStale,
                    a.TheoreticalPartsPerHour,
                    a.EvaluatedAtUtc))
                .ToList();

            return new MoldCompatibilityOverviewReadModel(
                moldId,
                requirement is null ? null : MapRequirement(requirement),
                summaries.Count(s => s.Status == MoldCompatibilityStatuses.Compatible),
                summaries.Count(s => s.Status == MoldCompatibilityStatuses.Conditional),
                summaries.Count(s => s.Status == MoldCompatibilityStatuses.Incompatible),
                summaries.Count(s => s.Status == MoldCompatibilityStatuses.InsufficientData),
                SortSummaries(summaries));
        }, cancellationToken);

    public Task<MoldCompatibilityAssessmentReadModel?> GetAssessmentAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var entity = await dbContext.MoldMachineCompatibilityAssessments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.MoldId == moldId
                        && a.Id == assessmentId,
                    ct);

            if (entity is null)
            {
                return null;
            }

            var profile = await dbContext.MachineCompatibilityCapabilityProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.MachineAssetId == entity.MachineAssetId,
                    ct);

            if (profile is null)
            {
                profile = await dbContext.MachineRegistries
                    .AsNoTracking()
                    .Where(m => m.Id == entity.MachineAssetId)
                    .Select(m => new MachineCompatibilityCapabilityProfile
                    {
                        MachineAssetId = m.Id,
                        MachineCode = m.MachineCode,
                        MachineDisplayName = m.Name,
                    })
                    .FirstOrDefaultAsync(ct);
            }

            return profile is null
                ? null
                : MapAssessment(entity, MapMachineProfile(profile));
        }, cancellationToken);

    private async Task<List<MoldCompatibilityAssessmentReadModel>> LoadLatestAssessmentsAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken)
    {
        var assessments = await dbContext.MoldMachineCompatibilityAssessments
            .AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                && a.FacilityId == context.FacilityId
                && a.MoldId == moldId)
            .OrderByDescending(a => a.EvaluatedAtUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var profiles = await dbContext.MachineCompatibilityCapabilityProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
            .ToDictionaryAsync(p => p.MachineAssetId, MapMachineProfile, cancellationToken);

        var latestByMachine = new Dictionary<Guid, MoldCompatibilityAssessmentReadModel>();
        foreach (var assessment in assessments)
        {
            if (latestByMachine.ContainsKey(assessment.MachineAssetId))
            {
                continue;
            }

            if (!profiles.TryGetValue(assessment.MachineAssetId, out var profile))
            {
                continue;
            }

            latestByMachine[assessment.MachineAssetId] = MapAssessment(assessment, profile);
        }

        return SortAssessments(latestByMachine.Values).ToList();
    }

    private async Task<MoldCompatibilityAssessmentReadModel> PersistAssessmentAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid machineAssetId,
        MachineCompatibilityCapabilityReadModel? machineProfile,
        MoldCompatibilityRequirementReadModel? requirement,
        MoldCompatibilityEvaluator.EvaluationOutput output,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.MoldMachineCompatibilityAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                    && a.FacilityId == context.FacilityId
                    && a.MoldId == moldId
                    && a.MachineAssetId == machineAssetId
                    && a.InputFingerprint == fingerprint,
                cancellationToken);

        if (existing is not null)
        {
            return MapAssessment(
                existing,
                machineProfile ?? EmptyMachineProfile(machineAssetId),
                idempotentReplay: true);
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new MoldMachineCompatibilityAssessment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MoldId = moldId,
            MachineAssetId = machineAssetId,
            Status = output.Status,
            RuleSetVersion = MoldCompatibilityConstants.RuleSetVersion,
            RequirementVersion = requirement?.Version ?? 0,
            MachineProfileVersion = machineProfile?.Version ?? 0,
            EvaluatedAtUtc = now,
            StructuredReasonsJson = MoldCompatibilityEvaluator.SerializeReasons(output.Reasons),
            StructuredEvidenceJson = MoldCompatibilityEvaluator.SerializeEvidence(output.Evidence),
            TheoreticalPartsPerHour = output.TheoreticalPartsPerHour,
            InputFingerprint = fingerprint,
            CreatedAtUtc = now,
            CreatedBySubjectId = context.IdentitySubjectId.ToString(),
        };

        dbContext.MoldMachineCompatibilityAssessments.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var replay = await dbContext.MoldMachineCompatibilityAssessments
                .AsNoTracking()
                .FirstAsync(
                    a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.MoldId == moldId
                        && a.MachineAssetId == machineAssetId
                        && a.InputFingerprint == fingerprint,
                    cancellationToken);

            return MapAssessment(
                replay,
                machineProfile ?? EmptyMachineProfile(machineAssetId),
                idempotentReplay: true);
        }

        AddAudit(
            context,
            MoldAuditActions.CompatibilityEvaluated,
            MoldAuditEventTypes.CompatibilityEvaluated,
            "mold.compatibility.assessment",
            entity.Id,
            now,
            new { moldId, machineAssetId });

        return MapAssessment(entity, machineProfile ?? EmptyMachineProfile(machineAssetId));
    }

    private static MoldCompatibilityEvaluationResult SuccessEvaluation(
        MoldCompatibilityAssessmentReadModel assessment,
        bool idempotentReplay) =>
        new(true, null, null, assessment, idempotentReplay);

    private static MoldCompatibilityEvaluationResult FailEvaluation(string code, string reason) =>
        new(false, code, reason, null, false);

    private static List<MoldCompatibilityAssessmentReadModel> SortAssessments(
        IEnumerable<MoldCompatibilityAssessmentReadModel> assessments) =>
        assessments
            .OrderBy(a => StatusSortKey(a.Status))
            .ThenBy(a => a.MachineDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.MachineCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.MachineAssetId)
            .ToList();

    private static List<MoldCompatibilityAssessmentSummaryReadModel> SortSummaries(
        IEnumerable<MoldCompatibilityAssessmentSummaryReadModel> summaries) =>
        summaries
            .OrderBy(a => StatusSortKey(a.Status))
            .ThenBy(a => a.MachineDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.MachineCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.MachineAssetId)
            .ToList();

    private static int StatusSortKey(string status) => status switch
    {
        MoldCompatibilityStatuses.Compatible => 0,
        MoldCompatibilityStatuses.Conditional => 1,
        MoldCompatibilityStatuses.InsufficientData => 2,
        MoldCompatibilityStatuses.Incompatible => 3,
        _ => 4,
    };

    private static MoldCompatibilityRequirementReadModel MapRequirement(MoldCompatibilityRequirement entity) =>
        new(
            entity.Id,
            entity.MoldId,
            entity.RequiredClampForceTon,
            entity.MoldWidthMm,
            entity.MoldHeightMm,
            entity.MoldDepthMm,
            entity.RequiredTieBarWidthMm,
            entity.RequiredTieBarHeightMm,
            entity.MinimumShotCapacityGrams,
            entity.RequiredInjectionUnit,
            entity.SupportedMaterialGroup,
            entity.NominalCycleTimeSeconds,
            entity.EffectiveCavityCount,
            entity.SourceType,
            entity.SourceReference,
            entity.DataRecordedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);

    private static MachineCompatibilityCapabilityReadModel MapMachineProfile(
        MachineCompatibilityCapabilityProfile entity) =>
        new(
            entity.Id,
            entity.MachineAssetId,
            entity.MachineCode,
            entity.MachineDisplayName,
            entity.ClampForceTon,
            entity.MaximumMoldWidthMm,
            entity.MaximumMoldHeightMm,
            entity.MinimumMoldDepthMm,
            entity.MaximumMoldDepthMm,
            entity.TieBarWidthMm,
            entity.TieBarHeightMm,
            entity.MaximumShotCapacityGrams,
            entity.InjectionUnit,
            MoldCompatibilityMaterialGroups.Deserialize(entity.SupportedMaterialGroups),
            entity.SourceType,
            entity.SourceReference,
            entity.DataRecordedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);

    private static MoldCompatibilityAssessmentReadModel MapAssessment(
        MoldMachineCompatibilityAssessment entity,
        MachineCompatibilityCapabilityReadModel machineProfile,
        bool idempotentReplay = false)
    {
        var reasons = MoldCompatibilityEvaluator.DeserializeReasons(entity.StructuredReasonsJson);
        var evidence = MoldCompatibilityEvaluator.DeserializeEvidence(entity.StructuredEvidenceJson);
        var missingFields = reasons
            .Where(r => r.Status == MoldCompatibilityRuleStatuses.Unknown)
            .Select(r => r.RuleCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new MoldCompatibilityAssessmentReadModel(
            entity.Id,
            entity.MoldId,
            entity.MachineAssetId,
            machineProfile.MachineCode,
            machineProfile.MachineDisplayName,
            entity.Status,
            entity.RuleSetVersion,
            entity.RequirementVersion,
            entity.MachineProfileVersion,
            entity.EvaluatedAtUtc,
            reasons,
            evidence,
            entity.TheoreticalPartsPerHour,
            MoldCompatibilityEvaluator.IsStale(
                evidence.TryGetValue("requirementSourceType", out var reqSource) ? reqSource : MoldSourceTypes.Manual,
                ParseTimestamp(evidence, "requirementDataRecordedAtUtc")),
            MoldCompatibilityEvaluator.IsStale(
                evidence.TryGetValue("machineSourceType", out var machineSource) ? machineSource : machineProfile.SourceType,
                machineProfile.DataRecordedAtUtc),
            missingFields,
            idempotentReplay);
    }

    private static DateTimeOffset? ParseTimestamp(IReadOnlyDictionary<string, string> evidence, string key) =>
        evidence.TryGetValue(key, out var value) && DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : null;

    private static void ApplyRequirement(
        MoldCompatibilityRequirement entity,
        UpsertMoldCompatibilityRequirementCommand command,
        DateTimeOffset now,
        string subjectId)
    {
        entity.RequiredClampForceTon = command.RequiredClampForceTon;
        entity.MoldWidthMm = command.MoldWidthMm;
        entity.MoldHeightMm = command.MoldHeightMm;
        entity.MoldDepthMm = command.MoldDepthMm;
        entity.RequiredTieBarWidthMm = command.RequiredTieBarWidthMm;
        entity.RequiredTieBarHeightMm = command.RequiredTieBarHeightMm;
        entity.MinimumShotCapacityGrams = command.MinimumShotCapacityGrams;
        entity.RequiredInjectionUnit = command.RequiredInjectionUnit?.Trim();
        entity.SupportedMaterialGroup = command.SupportedMaterialGroup?.Trim();
        entity.NominalCycleTimeSeconds = command.NominalCycleTimeSeconds;
        entity.EffectiveCavityCount = command.EffectiveCavityCount;
        entity.SourceType = command.SourceType.Trim().ToLowerInvariant();
        entity.SourceReference = command.SourceReference?.Trim();
        entity.DataRecordedAtUtc = command.DataRecordedAtUtc ?? now;
        entity.UpdatedAtUtc = now;
        entity.UpdatedBySubjectId = subjectId;
        entity.Version += 1;
    }

    private static void ApplyMachineProfile(
        MachineCompatibilityCapabilityProfile entity,
        UpsertMachineCompatibilityCapabilityCommand command,
        DateTimeOffset now,
        string subjectId)
    {
        entity.MachineCode = command.MachineCode.Trim();
        entity.MachineDisplayName = command.MachineDisplayName.Trim();
        entity.ClampForceTon = command.ClampForceTon;
        entity.MaximumMoldWidthMm = command.MaximumMoldWidthMm;
        entity.MaximumMoldHeightMm = command.MaximumMoldHeightMm;
        entity.MinimumMoldDepthMm = command.MinimumMoldDepthMm;
        entity.MaximumMoldDepthMm = command.MaximumMoldDepthMm;
        entity.TieBarWidthMm = command.TieBarWidthMm;
        entity.TieBarHeightMm = command.TieBarHeightMm;
        entity.MaximumShotCapacityGrams = command.MaximumShotCapacityGrams;
        entity.InjectionUnit = command.InjectionUnit?.Trim();
        entity.SupportedMaterialGroups = MoldCompatibilityMaterialGroups.Serialize(command.SupportedMaterialGroups);
        entity.SourceType = command.SourceType.Trim().ToLowerInvariant();
        entity.SourceReference = command.SourceReference?.Trim();
        entity.DataRecordedAtUtc = command.DataRecordedAtUtc ?? now;
        entity.UpdatedAtUtc = now;
        entity.UpdatedBySubjectId = subjectId;
        entity.Version += 1;
    }

    private Task<bool> MoldExistsAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken) =>
        dbContext.Molds.AsNoTracking().AnyAsync(
            m => m.TenantId == context.TenantId
                && m.FacilityId == context.FacilityId
                && m.Id == moldId,
            cancellationToken);

    private static MachineCompatibilityCapabilityReadModel EmptyMachineProfile(Guid machineAssetId) =>
        new(
            Guid.Empty,
            machineAssetId,
            "—",
            "—",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<string>(),
            MoldSourceTypes.Unknown,
            null,
            null,
            DateTimeOffset.MinValue,
            0);

    private void AddAudit(
        MoldOperationContext context,
        string action,
        string eventType,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now,
        object metadata)
    {
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = eventType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class UnavailableMoldCompatibilityRepository : IMoldCompatibilityRepository
{
    public Task<MoldCompatibilityRequirementReadModel?> GetRequirementAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldCompatibilityRequirementReadModel?>(null);

    public Task<MoldWriteResult> UpsertRequirementAsync(
        MoldOperationContext context,
        UpsertMoldCompatibilityRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold compatibility is unavailable."));

    public Task<IReadOnlyList<MachineCompatibilityCapabilityReadModel>> ListMachineProfilesAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MachineCompatibilityCapabilityReadModel>>(Array.Empty<MachineCompatibilityCapabilityReadModel>());

    public Task<MachineCompatibilityCapabilityReadModel?> GetMachineProfileAsync(
        MoldOperationContext context,
        Guid machineAssetId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MachineCompatibilityCapabilityReadModel?>(null);

    public Task<MoldWriteResult> UpsertMachineProfileAsync(
        MoldOperationContext context,
        UpsertMachineCompatibilityCapabilityCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold compatibility is unavailable."));

    public Task<MoldCompatibilityEvaluationResult> EvaluateAsync(
        MoldOperationContext context,
        EvaluateMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Fail(MoldFailureCodes.SourceUnavailable, "Mold compatibility is unavailable."));

    public Task<IReadOnlyList<MoldCompatibilityAssessmentReadModel>> EvaluateAllAsync(
        MoldOperationContext context,
        EvaluateAllMoldMachineCompatibilityCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MoldCompatibilityAssessmentReadModel>>(Array.Empty<MoldCompatibilityAssessmentReadModel>());

    public Task<MoldCompatibilityOverviewReadModel?> GetOverviewAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldCompatibilityOverviewReadModel?>(null);

    public Task<MoldCompatibilityAssessmentReadModel?> GetAssessmentAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldCompatibilityAssessmentReadModel?>(null);

    private static MoldCompatibilityEvaluationResult Fail(string code, string reason) =>
        new(false, code, reason, null, false);
}
