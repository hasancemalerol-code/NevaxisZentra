using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Mold;

public interface IMoldFollowUpRepository
{
    Task<MoldFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        MoldOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldFollowUpRowReadModel>> ListFollowUpRowsAsync(
        MoldOperationContext context,
        string? followUpStatus = null,
        string? lifecycleState = null,
        CancellationToken cancellationToken = default);

    Task<MoldFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default);

    Task<MoldFollowUpWriteResult> LinkTaskAsync(
        MoldOperationContext context,
        LinkMoldFollowUpTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldFollowUpWriteResult> TransitionFollowUpAsync(
        MoldOperationContext context,
        TransitionMoldFollowUpCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MoldFollowUpRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IMoldRepository moldRepository) : IMoldFollowUpRepository
{
    public Task<MoldFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        MoldOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var molds = await dbContext.Molds.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
                .Where(m => m.LifecycleState != MoldLifecycleState.Retired
                            && m.LifecycleState != MoldLifecycleState.Scrapped)
                .ToListAsync(ct);

            var eligible = molds.Where(MoldFollowUpPresentationBuilder.IsEligibleForFollowUp).ToList();

            return new MoldFollowUpWorkspaceReadModel(
                MoldFollowUpConstants.AutomationDisclaimer,
                eligible.Count(m => m.FollowUpStatus == MoldFollowUpStatuses.RequiresReview),
                eligible.Count(m => m.FollowUpStatus == MoldFollowUpStatuses.Acknowledged),
                eligible.Count(m => m.LinkedAiTaskId.HasValue),
                eligible.Count(m => m.LifecycleState == MoldLifecycleState.Maintenance),
                eligible.Count(MoldFollowUpPresentationBuilder.HasInsufficientEvidence),
                CanRead: true,
                CanManage: canManage,
                CanCreateTask: canCreateTask);
        }, cancellationToken);

    public Task<IReadOnlyList<MoldFollowUpRowReadModel>> ListFollowUpRowsAsync(
        MoldOperationContext context,
        string? followUpStatus = null,
        string? lifecycleState = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.Molds.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
                .Where(m => m.LifecycleState != MoldLifecycleState.Retired
                            && m.LifecycleState != MoldLifecycleState.Scrapped);

            if (!string.IsNullOrWhiteSpace(followUpStatus))
            {
                query = query.Where(m => m.FollowUpStatus == followUpStatus);
            }

            if (!string.IsNullOrWhiteSpace(lifecycleState))
            {
                var state = MoldValueConversions.LifecycleFromStorage(lifecycleState.Trim());
                query = query.Where(m => m.LifecycleState == state);
            }

            var items = await query.OrderByDescending(m => m.UpdatedAtUtc).ToListAsync(ct);

            return (IReadOnlyList<MoldFollowUpRowReadModel>)items
                .Where(MoldFollowUpPresentationBuilder.IsEligibleForFollowUp)
                .Select(MapRow)
                .ToList();
        }, cancellationToken);

    public Task<MoldFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var detail = await moldRepository.GetByIdAsync(context, moldId, ct);
            if (detail is null)
            {
                return null;
            }

            var mold = await dbContext.Molds.AsNoTracking()
                .SingleAsync(
                    m => m.TenantId == context.TenantId
                         && m.FacilityId == context.FacilityId
                         && m.Id == moldId,
                    ct);

            if (!MoldFollowUpPresentationBuilder.IsEligibleForFollowUp(mold))
            {
                return null;
            }

            var lifecycleEvents = await moldRepository.ListLifecycleHistoryAsync(context, moldId, ct);
            var auditEvents = await dbContext.MoldFollowUpAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.MoldId == moldId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(20)
                .Select(e => new MoldFollowUpAuditEventReadModel(
                    e.EventType,
                    e.PayloadJson,
                    e.Actor,
                    e.OccurredAtUtc))
                .ToListAsync(ct);

            var latestSnapshot = await dbContext.MoldCounterSnapshots.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId
                            && s.FacilityId == context.FacilityId
                            && s.MoldId == moldId)
                .OrderByDescending(s => s.RecordedAtUtc)
                .Select(s => (long?)s.ShotCount)
                .FirstOrDefaultAsync(ct);

            return new MoldFollowUpDetailReadModel(
                detail,
                mold.FollowUpStatus,
                mold.LinkedAiTaskId,
                MoldFollowUpPresentationBuilder.CanCreateTaskDraft(mold),
                MoldFollowUpPresentationBuilder.ResolveTaskPurpose(mold),
                MoldFollowUpPresentationBuilder.HasInsufficientEvidence(mold),
                MoldFollowUpPresentationBuilder.BuildEvidenceSummary(mold, latestSnapshot),
                lifecycleEvents,
                auditEvents);
        }, cancellationToken);

    public Task<MoldFollowUpWriteResult> LinkTaskAsync(
        MoldOperationContext context,
        LinkMoldFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var mold = await LoadMoldForWriteAsync(context, command.MoldId, ct);
            if (mold is null)
            {
                return MoldFollowUpWriteResult.Fail(MoldFailureCodes.NotFound, "Kalıp kaydı bulunamadı.");
            }

            if (mold.LinkedAiTaskId.HasValue)
            {
                if (mold.LinkedAiTaskId.Value == command.AiTaskId)
                {
                    var existing = await GetFollowUpDetailAsync(context, mold.Id, ct);
                    return MoldFollowUpWriteResult.Success(mold.Id, idempotentReplay: true, detail: existing);
                }

                return MoldFollowUpWriteResult.Fail(
                    MoldFailureCodes.ValidationFailed,
                    "Kalıp kaydı zaten farklı bir göreve bağlı.");
            }

            if (!MoldFollowUpPresentationBuilder.CanCreateTaskDraft(mold))
            {
                return MoldFollowUpWriteResult.Fail(
                    MoldFailureCodes.ValidationFailed,
                    "Bu kalıp kaydı için görev taslağı oluşturulamaz.");
            }

            var taskExists = await dbContext.AiTasks.AsNoTracking()
                .AnyAsync(
                    task => task.TenantId == context.TenantId
                            && task.FacilityId == context.FacilityId
                            && task.Id == command.AiTaskId,
                    ct);

            if (!taskExists)
            {
                return MoldFollowUpWriteResult.Fail(MoldFailureCodes.NotFound, "Bağlanacak AI görevi bulunamadı.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            mold.LinkedAiTaskId = command.AiTaskId;
            mold.FollowUpStatus = MoldFollowUpStatuses.TaskCreated;
            mold.FollowUpUpdatedBy = actor;
            mold.UpdatedAtUtc = now;
            mold.UpdatedBySubjectId = actor;
            mold.Version += 1;

            await AppendFollowUpAuditEventAsync(
                mold,
                MoldFollowUpAuditEventTypes.TaskLinked,
                new { aiTaskId = command.AiTaskId },
                actor,
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await GetFollowUpDetailAsync(context, mold.Id, ct);
            return MoldFollowUpWriteResult.Success(mold.Id, detail: detail);
        }, cancellationToken);

    public Task<MoldFollowUpWriteResult> TransitionFollowUpAsync(
        MoldOperationContext context,
        TransitionMoldFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var mold = await LoadMoldForWriteAsync(context, command.MoldId, ct);
            if (mold is null)
            {
                return MoldFollowUpWriteResult.Fail(MoldFailureCodes.NotFound, "Kalıp kaydı bulunamadı.");
            }

            if (mold.Version != command.ExpectedVersion)
            {
                return MoldFollowUpWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "Kayıt başka bir kullanıcı tarafından güncellendi.");
            }

            if (command.TargetFollowUpStatus == MoldFollowUpStatuses.Dismissed
                && string.IsNullOrWhiteSpace(command.Note))
            {
                return MoldFollowUpWriteResult.Fail(
                    MoldFailureCodes.ValidationFailed,
                    "Red gerekçesi zorunludur.");
            }

            if (!MoldFollowUpPresentationBuilder.IsValidFollowUpTransition(
                    mold.FollowUpStatus,
                    command.TargetFollowUpStatus))
            {
                return MoldFollowUpWriteResult.Fail(
                    MoldFailureCodes.InvalidTransition,
                    "Geçersiz takip durumu geçişi.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previous = mold.FollowUpStatus;
            mold.FollowUpStatus = command.TargetFollowUpStatus;
            mold.FollowUpUpdatedBy = actor;
            mold.UpdatedAtUtc = now;
            mold.UpdatedBySubjectId = actor;
            mold.Version += 1;

            await AppendFollowUpAuditEventAsync(
                mold,
                MoldFollowUpAuditEventTypes.FollowUpTransitioned,
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

            var detail = await GetFollowUpDetailAsync(context, mold.Id, ct);
            return MoldFollowUpWriteResult.Success(mold.Id, detail: detail);
        }, cancellationToken);

    private static MoldFollowUpRowReadModel MapRow(MoldRegistryEntry mold) =>
        new(
            mold.Id,
            mold.Code,
            mold.Name,
            MoldValueConversions.ToStorage(mold.LifecycleState),
            mold.FollowUpStatus,
            mold.CurrentShotCount,
            mold.NominalShotLife,
            mold.CounterRecordedAtUtc,
            mold.PrimaryProductReference,
            mold.UpdatedAtUtc,
            mold.LinkedAiTaskId,
            MoldFollowUpPresentationBuilder.CanCreateTaskDraft(mold),
            MoldFollowUpPresentationBuilder.ResolveTaskPurpose(mold),
            MoldFollowUpPresentationBuilder.HasInsufficientEvidence(mold));

    private async Task<MoldRegistryEntry?> LoadMoldForWriteAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Molds
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.Id == moldId,
                cancellationToken);
    }

    private async Task AppendFollowUpAuditEventAsync(
        MoldRegistryEntry mold,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.MoldFollowUpAuditEvents.Add(new MoldFollowUpAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = mold.TenantId,
            FacilityId = mold.FacilityId,
            MoldId = mold.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            Actor = actor,
            OccurredAtUtc = occurredAtUtc,
        });

        await Task.CompletedTask;
    }
}

public sealed class UnavailableMoldFollowUpRepository : IMoldFollowUpRepository
{
    public Task<MoldFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        MoldOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MoldFollowUpWorkspaceReadModel(
            MoldFollowUpConstants.AutomationDisclaimer,
            0, 0, 0, 0, 0,
            CanRead: false,
            CanManage: false,
            CanCreateTask: false));

    public Task<IReadOnlyList<MoldFollowUpRowReadModel>> ListFollowUpRowsAsync(
        MoldOperationContext context,
        string? followUpStatus = null,
        string? lifecycleState = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MoldFollowUpRowReadModel>>([]);

    public Task<MoldFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldFollowUpDetailReadModel?>(null);

    public Task<MoldFollowUpWriteResult> LinkTaskAsync(
        MoldOperationContext context,
        LinkMoldFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldFollowUpWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold follow-up service is unavailable."));

    public Task<MoldFollowUpWriteResult> TransitionFollowUpAsync(
        MoldOperationContext context,
        TransitionMoldFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldFollowUpWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold follow-up service is unavailable."));
}
