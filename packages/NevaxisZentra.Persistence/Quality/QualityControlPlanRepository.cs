using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Quality;

public interface IQualityControlPlanRepository
{
    Task<IReadOnlyList<QualityControlPlanSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default);

    Task<QualityControlPlanDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class QualityControlPlanRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IQualityControlPlanRepository
{
    public async Task<IReadOnlyList<QualityControlPlanSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default)
    {
        return await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityControlPlans.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                .OrderByDescending(p => p.UpdatedAtUtc)
                .Select(p => new QualityControlPlanSummaryReadModel(
                    p.Id,
                    p.Code,
                    p.Name,
                    QualityValueConversions.ToStorage(p.Status),
                    p.Revision,
                    p.ProductReference,
                    p.OperationReference,
                    p.InspectionDefinitions.Count,
                    p.Version,
                    p.UpdatedAtUtc))
                .ToListAsync(ct),
            cancellationToken);
    }

    public async Task<QualityControlPlanDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityControlPlans.AsNoTracking()
                .Include(p => p.InspectionDefinitions.OrderBy(d => d.Sequence))
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == planId,
                    ct),
            cancellationToken);

        return plan is null ? null : MapDetail(plan);
    }

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var validation = ValidatePlanInput(command.Code, command.Name, command.InspectionDefinitions);
            if (validation is not null)
            {
                return validation;
            }

            var now = DateTimeOffset.UtcNow;
            var planId = Guid.NewGuid();
            var plan = new QualityControlPlan
            {
                Id = planId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                Code = command.Code.Trim(),
                Name = command.Name.Trim(),
                Description = command.Description?.Trim(),
                ProductReference = command.ProductReference?.Trim(),
                ProductGroup = command.ProductGroup?.Trim(),
                OperationReference = command.OperationReference?.Trim(),
                Status = ParsePlanStatus(command.Status),
                Revision = command.Revision.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                Version = 1,
            };

            plan.InspectionDefinitions = command.InspectionDefinitions
                .Select((d, index) => MapDefinition(planId, context, d, index + 1))
                .ToList();

            dbContext.QualityControlPlans.Add(plan);
            AddAudit(context, QualityAuditActions.PlanCreated, QualityAuditEventTypes.PlanCreated,
                "quality.control-plan", planId, now, new { planId, plan.Code, plan.Status });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(planId, plan.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var validation = ValidatePlanInput(command.Code, command.Name, command.InspectionDefinitions);
            if (validation is not null)
            {
                return validation;
            }

            var plan = await dbContext.QualityControlPlans
                .Include(p => p.InspectionDefinitions)
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == command.PlanId,
                    ct);

            if (plan is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality control plan not found.");
            }

            if (plan.Version != command.ExpectedVersion)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ConcurrencyConflict,
                    "The quality control plan was modified. Please reload and try again.");
            }

            var now = DateTimeOffset.UtcNow;
            plan.Code = command.Code.Trim();
            plan.Name = command.Name.Trim();
            plan.Description = command.Description?.Trim();
            plan.ProductReference = command.ProductReference?.Trim();
            plan.ProductGroup = command.ProductGroup?.Trim();
            plan.OperationReference = command.OperationReference?.Trim();
            plan.Status = ParsePlanStatus(command.Status);
            plan.Revision = command.Revision.Trim();
            plan.UpdatedAtUtc = now;
            plan.Version += 1;

            dbContext.QualityInspectionDefinitions.RemoveRange(plan.InspectionDefinitions);
            plan.InspectionDefinitions = command.InspectionDefinitions
                .Select((d, index) => MapDefinition(plan.Id, context, d, index + 1))
                .ToList();

            AddAudit(context, QualityAuditActions.PlanUpdated, QualityAuditEventTypes.PlanUpdated,
                "quality.control-plan", plan.Id, now, new { planId = plan.Id, plan.Code, plan.Version });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(plan.Id, plan.Version);
        }, cancellationToken);

    private async Task<QualityWriteResult> ExecuteWriteAsync(
        QualityOperationContext context,
        Func<CancellationToken, Task<QualityWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.SourceUnavailable, "Quality service is unavailable.");
        }
    }

    private static QualityWriteResult? ValidatePlanInput(
        string code,
        string name,
        IReadOnlyList<QualityInspectionDefinitionInput> definitions)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return QualityWriteResult.Fail(QualityFailureCodes.ValidationFailed, "Code and name are required.");
        }

        if (definitions.Count == 0)
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ValidationFailed,
                "At least one inspection definition is required.");
        }

        return null;
    }

    private static QualityInspectionDefinition MapDefinition(
        Guid planId,
        QualityOperationContext context,
        QualityInspectionDefinitionInput input,
        int fallbackSequence) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            QualityControlPlanId = planId,
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            InspectionType = ParseInspectionType(input.InspectionType),
            Sequence = input.Sequence > 0 ? input.Sequence : fallbackSequence,
            Unit = input.Unit?.Trim(),
            NominalValue = input.NominalValue,
            LowerLimit = input.LowerLimit,
            UpperLimit = input.UpperLimit,
            Required = input.Required,
            Critical = input.Critical,
            Instruction = input.Instruction?.Trim(),
            Version = 1,
        };

    private static QualityControlPlanDetailReadModel MapDetail(QualityControlPlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            QualityValueConversions.ToStorage(plan.Status),
            plan.Revision,
            plan.ProductReference,
            plan.ProductGroup,
            plan.OperationReference,
            plan.EffectiveFromUtc,
            plan.EffectiveToUtc,
            plan.Version,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.InspectionDefinitions
                .OrderBy(d => d.Sequence)
                .Select(d => new QualityInspectionDefinitionReadModel(
                    d.Id,
                    d.Code,
                    d.Name,
                    d.Description,
                    QualityValueConversions.ToStorage(d.InspectionType),
                    d.Sequence,
                    d.Unit,
                    d.NominalValue,
                    d.LowerLimit,
                    d.UpperLimit,
                    d.Required,
                    d.Critical,
                    d.Instruction,
                    d.Version))
                .ToList());

    private static QualityControlPlanStatus ParsePlanStatus(string status) =>
        Enum.TryParse<QualityControlPlanStatus>(status, true, out var parsed)
            ? parsed
            : QualityValueConversions.PlanStatusFromStorage(status);

    private static QualityInspectionType ParseInspectionType(string type) =>
        Enum.TryParse<QualityInspectionType>(type, true, out var parsed)
            ? parsed
            : QualityValueConversions.InspectionTypeFromStorage(type);

    private void AddAudit(
        QualityOperationContext context,
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

public sealed class UnavailableQualityControlPlanRepository : IQualityControlPlanRepository
{
    public Task<IReadOnlyList<QualityControlPlanSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityControlPlanSummaryReadModel>>([]);

    public Task<QualityControlPlanDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityControlPlanDetailReadModel?>(null);

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality control plan service is unavailable."));

    public Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityControlPlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality control plan service is unavailable."));
}
