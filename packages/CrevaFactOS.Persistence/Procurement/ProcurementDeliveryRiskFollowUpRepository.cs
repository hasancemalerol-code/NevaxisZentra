using System.Text.Json;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Procurement;

public sealed partial class ProcurementDeliveryRiskRepository
{
    public Task<ProcurementDeliveryRiskFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var assessment = await LoadAssessmentForReadAsync(context, assessmentId, ct);
            if (assessment is null)
            {
                return null;
            }

            var detail = MapDetail(assessment);
            var auditEvents = await dbContext.ProcurementDeliveryRiskFollowUpAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.AssessmentId == assessmentId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(20)
                .Select(e => new ProcurementDeliveryRiskFollowUpAuditEventReadModel(
                    e.EventType,
                    e.PayloadJson,
                    e.Actor,
                    e.OccurredAtUtc))
                .ToListAsync(ct);

            var insufficient = ProcurementDeliveryRiskFollowUpPresentationBuilder.IsDataReviewEligible(assessment);
            return new ProcurementDeliveryRiskFollowUpDetailReadModel(
                detail,
                assessment.FollowUpStatus,
                assessment.LinkedAiTaskId,
                ProcurementDeliveryRiskFollowUpPresentationBuilder.CanCreateTaskDraft(assessment),
                ProcurementDeliveryRiskFollowUpPresentationBuilder.ResolveTaskPurpose(assessment),
                insufficient,
                ProcurementDeliveryRiskFollowUpPresentationBuilder.BuildEvidenceSummary(assessment),
                auditEvents);
        }, cancellationToken);

    public Task<ProcurementDeliveryRiskFollowUpWriteResult> LinkTaskAsync(
        ErpOperationContext context,
        LinkProcurementDeliveryRiskFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var assessment = await LoadAssessmentForWriteAsync(context, command.AssessmentId, ct);
                if (assessment is null)
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.NotFound,
                        "Tedarik risk değerlendirmesi bulunamadı.");
                }

                if (assessment.LinkedAiTaskId.HasValue)
                {
                    if (assessment.LinkedAiTaskId.Value == command.AiTaskId)
                    {
                        var existing = await GetFollowUpDetailAsync(context, assessment.Id, ct);
                        return ProcurementDeliveryRiskFollowUpWriteResult.Success(
                            assessment.Id,
                            idempotentReplay: true,
                            detail: existing);
                    }

                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.ValidationFailed,
                        "Değerlendirme zaten farklı bir göreve bağlı.");
                }

                if (!ProcurementDeliveryRiskFollowUpPresentationBuilder.CanCreateTaskDraft(assessment))
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.ValidationFailed,
                        "Bu değerlendirme için görev taslağı oluşturulamaz.");
                }

                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        task => task.TenantId == context.TenantId
                                && task.FacilityId == context.FacilityId
                                && task.Id == command.AiTaskId,
                        ct);

                if (!taskExists)
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.NotFound,
                        "Bağlanacak AI görevi bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                assessment.LinkedAiTaskId = command.AiTaskId;
                assessment.FollowUpStatus = ProcurementDeliveryRiskFollowUpStatuses.TaskCreated;
                assessment.FollowUpUpdatedBy = actor;
                assessment.Version += 1;

                await AppendFollowUpAuditEventAsync(
                    assessment,
                    ProcurementDeliveryRiskFollowUpAuditEventTypes.TaskLinked,
                    new { aiTaskId = command.AiTaskId },
                    actor,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetFollowUpDetailAsync(context, assessment.Id, ct);
                return ProcurementDeliveryRiskFollowUpWriteResult.Success(assessment.Id, detail: detail);
            },
            cancellationToken);

    public Task<ProcurementDeliveryRiskFollowUpWriteResult> TransitionFollowUpAsync(
        ErpOperationContext context,
        TransitionProcurementDeliveryRiskFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var assessment = await LoadAssessmentForWriteAsync(context, command.AssessmentId, ct);
                if (assessment is null)
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.NotFound,
                        "Tedarik risk değerlendirmesi bulunamadı.");
                }

                if (assessment.Version != command.ExpectedVersion)
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict,
                        "Kayıt başka bir kullanıcı tarafından güncellendi.");
                }

                if (command.TargetFollowUpStatus == ProcurementDeliveryRiskFollowUpStatuses.Dismissed
                    && string.IsNullOrWhiteSpace(command.Note))
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.ValidationFailed,
                        "Red gerekçesi zorunludur.");
                }

                if (!ProcurementDeliveryRiskFollowUpPresentationBuilder.IsValidFollowUpTransition(
                        assessment.FollowUpStatus,
                        command.TargetFollowUpStatus))
                {
                    return ProcurementDeliveryRiskFollowUpWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.InvalidTransition,
                        "Geçersiz takip durumu geçişi.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previous = assessment.FollowUpStatus;
                assessment.FollowUpStatus = command.TargetFollowUpStatus;
                assessment.FollowUpUpdatedBy = actor;
                assessment.Version += 1;

                await AppendFollowUpAuditEventAsync(
                    assessment,
                    ProcurementDeliveryRiskFollowUpAuditEventTypes.FollowUpTransitioned,
                    new
                    {
                        previousStatus = previous,
                        targetStatus = command.TargetFollowUpStatus,
                        note = command.Note,
                    },
                    actor,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetFollowUpDetailAsync(context, assessment.Id, ct);
                return ProcurementDeliveryRiskFollowUpWriteResult.Success(assessment.Id, detail: detail);
            },
            cancellationToken);

    private async Task<ProcurementDeliveryRiskAssessment?> LoadAssessmentForReadAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken) =>
        await dbContext.ProcurementDeliveryRiskAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == assessmentId
                     && a.TenantId == context.TenantId
                     && a.FacilityId == context.FacilityId
                     && a.IsCurrent,
                cancellationToken);

    private async Task<ProcurementDeliveryRiskAssessment?> LoadAssessmentForWriteAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken) =>
        await dbContext.ProcurementDeliveryRiskAssessments
            .FirstOrDefaultAsync(
                a => a.Id == assessmentId
                     && a.TenantId == context.TenantId
                     && a.FacilityId == context.FacilityId
                     && a.IsCurrent,
                cancellationToken);

    private async Task AppendFollowUpAuditEventAsync(
        ProcurementDeliveryRiskAssessment assessment,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.ProcurementDeliveryRiskFollowUpAuditEvents.Add(new ProcurementDeliveryRiskFollowUpAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = assessment.TenantId,
            FacilityId = assessment.FacilityId,
            AssessmentId = assessment.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            Actor = actor,
            OccurredAtUtc = occurredAtUtc,
        });

        await Task.CompletedTask;
    }
}
