using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Material;

public partial interface IMaterialAvailabilityRiskRepository
{
    Task<MaterialAvailabilityRiskWorkspaceReadModel> GetWorkspaceAsync(
        MaterialOperationContext context,
        MaterialAvailabilityRiskListFilters filters,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<MaterialAvailabilityRiskDetailReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialAvailabilityRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        Guid? requirementProfileId,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed partial class MaterialAvailabilityRiskRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaterialAvailabilityRiskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Task<MaterialAvailabilityRiskWorkspaceReadModel> GetWorkspaceAsync(
        MaterialOperationContext context,
        MaterialAvailabilityRiskListFilters filters,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.MaterialRequirementAssessments
                .AsNoTracking()
                .Include(a => a.Material)
                .Include(a => a.RequirementProfile)
                .Where(a => a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.AssessmentType == MaterialRequirementAssessmentType.AvailabilityRisk);

            if (filters.MaterialId.HasValue)
            {
                query = query.Where(a => a.MaterialId == filters.MaterialId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                query = query.Where(a => a.Status == filters.Status.Trim());
            }

            var allRows = await query.ToListAsync(ct);
            var latestByScope = allRows
                .GroupBy(a => (a.MaterialId, a.RequirementProfileId))
                .Select(g => g.OrderByDescending(a => a.EvaluatedAtUtc).ThenByDescending(a => a.Id).First())
                .OrderByDescending(a => a.EvaluatedAtUtc)
                .ThenBy(a => a.Material!.Code)
                .ToList();

            var page = Math.Max(1, filters.Page);
            var pageSize = Math.Clamp(filters.PageSize, 1, 100);
            var paged = latestByScope
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapRow)
                .ToList();

            return new MaterialAvailabilityRiskWorkspaceReadModel(
                MaterialAvailabilityRiskConstants.RuleSetVersion,
                MaterialAvailabilityRiskFollowUpConstants.AutomationDisclaimer,
                latestByScope.Count(a => a.Status == MaterialAvailabilityRiskStatuses.Shortage),
                latestByScope.Count(a => a.Status == MaterialAvailabilityRiskStatuses.Sufficient),
                latestByScope.Count(a => a.Status == MaterialAvailabilityRiskStatuses.InsufficientData),
                latestByScope.Count(a => a.Status == MaterialAvailabilityRiskStatuses.SourceConflict),
                latestByScope.Count(a => a.FollowUpStatus == MaterialAvailabilityRiskFollowUpStatuses.RequiresReview),
                latestByScope.Count(a => a.FollowUpStatus == MaterialAvailabilityRiskFollowUpStatuses.TaskCreated),
                latestByScope.Count(a =>
                    MaterialAvailabilityRiskFollowUpPresentationBuilder.IsDataReviewEligible(a)),
                latestByScope.MaxBy(a => a.EvaluatedAtUtc)?.EvaluatedAtUtc,
                true,
                canManage,
                canCreateTask,
                paged);
        }, cancellationToken);

    public Task<MaterialAvailabilityRiskDetailReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var assessment = await dbContext.MaterialRequirementAssessments
                .AsNoTracking()
                .Include(a => a.Material)
                .Include(a => a.RequirementProfile)
                .FirstOrDefaultAsync(
                    a => a.Id == assessmentId
                         && a.TenantId == context.TenantId
                         && a.FacilityId == context.FacilityId
                         && a.AssessmentType == MaterialRequirementAssessmentType.AvailabilityRisk,
                    ct);

            return assessment is null ? null : MapDetail(assessment);
        }, cancellationToken);

    public Task<IReadOnlyList<MaterialAvailabilityRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        Guid? requirementProfileId,
        int limit,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var normalizedLimit = Math.Clamp(limit, 1, 100);
            var query = dbContext.MaterialRequirementAssessments
                .AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.MaterialId == materialId
                            && a.AssessmentType == MaterialRequirementAssessmentType.AvailabilityRisk);

            if (requirementProfileId.HasValue)
            {
                query = query.Where(a => a.RequirementProfileId == requirementProfileId.Value);
            }

            var rows = await query
                .OrderByDescending(a => a.EvaluatedAtUtc)
                .Take(normalizedLimit)
                .ToListAsync(ct);

            return (IReadOnlyList<MaterialAvailabilityRiskAssessmentSummaryReadModel>)rows
                .Select(a => new MaterialAvailabilityRiskAssessmentSummaryReadModel(
                    a.Id,
                    a.Status,
                    a.EvaluatedAtUtc,
                    a.RuleSetVersion))
                .ToList();
        }, cancellationToken);

    private static MaterialAvailabilityRiskRowReadModel MapRow(MaterialRequirementAssessment assessment) =>
        new(
            assessment.Id,
            assessment.MaterialId,
            assessment.Material?.Code ?? string.Empty,
            assessment.Material?.Name ?? string.Empty,
            assessment.RequirementProfileId,
            assessment.RequirementProfile?.ProductionOrderReference,
            assessment.Status,
            assessment.TheoreticalRequirementGrams,
            assessment.ErpAvailableQuantity,
            assessment.ErpReservedQuantity,
            assessment.ErpUnit,
            assessment.AvailableAfterReservationQuantity,
            assessment.ShortageQuantity,
            assessment.SourceFreshness,
            assessment.EvaluatedAtUtc,
            assessment.Version,
            assessment.FollowUpStatus,
            assessment.LinkedAiTaskId,
            MaterialAvailabilityRiskFollowUpPresentationBuilder.CanCreateTaskDraft(assessment),
            MaterialAvailabilityRiskFollowUpPresentationBuilder.ResolveTaskPurpose(assessment));

    private static MaterialAvailabilityRiskDetailReadModel MapDetail(MaterialRequirementAssessment assessment)
    {
        var drivers = DeserializeDrivers(assessment.StructuredDrivers);
        var evidence = DeserializeEvidence(assessment.StructuredEvidence);

        return new MaterialAvailabilityRiskDetailReadModel(
            assessment.Id,
            assessment.FacilityId,
            assessment.MaterialId,
            assessment.Material?.Code ?? string.Empty,
            assessment.Material?.Name ?? string.Empty,
            assessment.RequirementProfileId,
            assessment.RequirementProfile?.ProductionOrderReference,
            assessment.RequirementProfile?.ProductReference,
            assessment.RequirementProfile?.MoldReference,
            assessment.Status,
            assessment.RuleSetVersion,
            assessment.InputFingerprint,
            assessment.TheoreticalRequirementGrams,
            assessment.ErpAvailableQuantity,
            assessment.ErpReservedQuantity,
            assessment.ErpUnit,
            assessment.AvailableAfterReservationQuantity,
            assessment.ShortageQuantity,
            assessment.ErpSourceUpdatedAtUtc,
            assessment.SourceFreshness,
            drivers,
            evidence,
            assessment.EvaluatedAtUtc,
            assessment.CreatedAtUtc,
            assessment.Version);
    }

    private static List<MaterialAssessmentDriverReadModel> DeserializeDrivers(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<MaterialAssessmentDriverReadModel>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<MaterialAssessmentEvidenceReadModel> DeserializeEvidence(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<MaterialAssessmentEvidenceReadModel>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public sealed partial class UnavailableMaterialAvailabilityRiskRepository : IMaterialAvailabilityRiskRepository
{
    public Task<MaterialAvailabilityRiskWorkspaceReadModel> GetWorkspaceAsync(
        MaterialOperationContext context,
        MaterialAvailabilityRiskListFilters filters,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaterialAvailabilityRiskWorkspaceReadModel(
            MaterialAvailabilityRiskConstants.RuleSetVersion,
            MaterialAvailabilityRiskFollowUpConstants.AutomationDisclaimer,
            0, 0, 0, 0, 0, 0, 0, null, true, canManage, canCreateTask, []));

    public Task<MaterialAvailabilityRiskDetailReadModel?> GetAssessmentByIdAsync(
        MaterialOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaterialAvailabilityRiskDetailReadModel?>(null);

    public Task<IReadOnlyList<MaterialAvailabilityRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        Guid? requirementProfileId,
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaterialAvailabilityRiskAssessmentSummaryReadModel>>([]);
}
