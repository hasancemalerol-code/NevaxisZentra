using System.Text.Json;
using NevaxisFactOS.Persistence.Teams;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ShiftHandover;

public interface IShiftHandoverRepository
{
    Task<ShiftHandoverListReadModel> ListAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        string? status,
        DateOnly? shiftDate,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverDetailReadModel?> GetByIdAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> CreateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        CreateShiftHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> UpdateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> AddItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        AddShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> UpdateItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> MarkReadyAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> RevertToDraftAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> HandOverAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> AcknowledgeAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> CompleteAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ShiftHandoverWriteResult> LinkItemTaskAsync(
        ShiftHandoverOperationContext context,
        LinkShiftHandoverItemTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftHandoverEventReadModel>> GetHistoryAsync(
        ShiftHandoverOperationContext context,
        Guid handoverId,
        CancellationToken cancellationToken = default);
}

public sealed class ShiftHandoverRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ITeamsNotificationRepository notificationRepository) : IShiftHandoverRepository
{
    public Task<ShiftHandoverListReadModel> ListAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        string? status,
        DateOnly? shiftDate,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!capabilities.CanRead)
            {
                return new ShiftHandoverListReadModel([], capabilities, ShiftHandoverConstants.AutomationDisclaimer);
            }

            var query = dbContext.ShiftHandovers.AsNoTracking()
                .Where(h => h.TenantId == context.TenantId && h.FacilityId == context.FacilityId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(h => h.Status == status);
            }

            if (shiftDate is DateOnly date)
            {
                query = query.Where(h => h.ShiftDate == date);
            }

            var handovers = await query
                .OrderByDescending(h => h.ShiftDate)
                .ThenByDescending(h => h.CreatedAtUtc)
                .Take(ShiftHandoverConstants.MaxPageSize)
                .ToListAsync(ct);

            var membershipIds = handovers
                .SelectMany(h => new[] { (Guid?)h.OutgoingMembershipId, h.IncomingMembershipId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var labels = await LoadMembershipLabelsAsync(context.TenantId, membershipIds, ct);
            var handoverIds = handovers.Select(h => h.Id).ToList();
            var itemStats = await dbContext.ShiftHandoverItems.AsNoTracking()
                .Where(i => i.TenantId == context.TenantId && handoverIds.Contains(i.HandoverId))
                .GroupBy(i => i.HandoverId)
                .Select(g => new
                {
                    HandoverId = g.Key,
                    Unresolved = g.Count(i => i.Status == ShiftHandoverItemStatuses.Open),
                    FollowUp = g.Count(i => i.RequiresFollowUp),
                })
                .ToDictionaryAsync(x => x.HandoverId, ct);

            var items = handovers.Select(h => new ShiftHandoverSummaryReadModel(
                h.Id,
                h.HandoverReference,
                h.ShiftDate,
                h.ShiftPeriod,
                h.Department,
                h.Status,
                labels.GetValueOrDefault(h.OutgoingMembershipId, h.OutgoingMembershipId.ToString("D")),
                h.IncomingMembershipId is Guid incoming
                    ? labels.GetValueOrDefault(incoming, incoming.ToString("D"))
                    : null,
                itemStats.GetValueOrDefault(h.Id)?.Unresolved ?? 0,
                itemStats.GetValueOrDefault(h.Id)?.FollowUp ?? 0,
                h.CreatedAtUtc,
                h.HandedOverAtUtc)).ToList();

            return new ShiftHandoverListReadModel(items, capabilities, ShiftHandoverConstants.AutomationDisclaimer);
        }, cancellationToken);

    public Task<ShiftHandoverDetailReadModel?> GetByIdAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadDetailAsync(context, capabilities, handoverId, ct),
            cancellationToken);

    public Task<ShiftHandoverWriteResult> CreateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        CreateShiftHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Vardiya devri olu┼şturma yetkisi yok.");
            }

            var validationError = ValidateCreate(command);
            if (validationError is not null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.ValidationFailed, validationError);
            }

            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var reference = await GenerateReferenceAsync(context.TenantId, ct);
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);

            var handover = new FactoryShiftHandover
            {
                Id = id,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                HandoverReference = reference,
                ShiftDate = command.ShiftDate,
                ShiftPeriod = command.ShiftPeriod,
                Department = command.Department.Trim(),
                Summary = command.Summary.Trim(),
                OutgoingMembershipId = context.MembershipId,
                IncomingMembershipId = command.IncomingMembershipId,
                Status = ShiftHandoverStatuses.Draft,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            dbContext.ShiftHandovers.Add(handover);
            AddEvent(context, handover, ShiftHandoverAuditEventTypes.Created, actorLabel, new { reference }, now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadDetailAsync(context, capabilities, id, ct);
            return detail is null
                ? ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.")
                : ShiftHandoverWriteResult.Success(id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> UpdateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Vardiya devri g├╝ncelleme yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (handover.Version != command.ExpectedVersion)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.ConcurrencyConflict,
                    "Kay─▒t ba┼şka bir i┼şlem taraf─▒ndan g├╝ncellendi.");
            }

            if (!ShiftHandoverLifecycleEngine.CanEdit(handover.Status))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Bu durumdaki devir g├╝ncellenemez.");
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);

            handover.ShiftDate = command.ShiftDate;
            handover.ShiftPeriod = command.ShiftPeriod;
            handover.Department = command.Department.Trim();
            handover.Summary = command.Summary.Trim();
            handover.IncomingMembershipId = command.IncomingMembershipId;
            handover.UpdatedAtUtc = now;
            handover.Version++;

            AddEvent(context, handover, ShiftHandoverAuditEventTypes.Updated, actorLabel, new { }, now);
            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(handover.Id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> AddItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        AddShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Madde ekleme yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (!ShiftHandoverLifecycleEngine.CanAddItems(handover.Status))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Bu durumda madde eklenemez.");
            }

            var validationError = ValidateItem(command.Category, command.Title, command.Description, command.Severity);
            if (validationError is not null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.ValidationFailed, validationError);
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            var sortOrder = await dbContext.ShiftHandoverItems
                .Where(i => i.TenantId == context.TenantId && i.HandoverId == handover.Id)
                .Select(i => (int?)i.SortOrder)
                .MaxAsync(ct) ?? 0;

            var item = new ShiftHandoverItem
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                HandoverId = handover.Id,
                SortOrder = sortOrder + 1,
                Category = command.Category,
                Title = command.Title.Trim(),
                Description = command.Description.Trim(),
                Severity = command.Severity,
                SourceModule = command.SourceModule,
                SourceEntityId = command.SourceEntityId,
                RequiresFollowUp = command.RequiresFollowUp,
                EvidenceMetadataJson = command.EvidenceMetadataJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            dbContext.ShiftHandoverItems.Add(item);
            handover.UpdatedAtUtc = now;
            handover.Version++;

            AddEvent(context, handover, ShiftHandoverAuditEventTypes.ItemAdded, actorLabel, new { itemId = item.Id }, now);
            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(item.Id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> UpdateItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Madde g├╝ncelleme yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (!ShiftHandoverLifecycleEngine.CanEditItems(handover.Status))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Bu durumda madde g├╝ncellenemez.");
            }

            var item = await dbContext.ShiftHandoverItems
                .SingleOrDefaultAsync(
                    i => i.TenantId == context.TenantId
                         && i.HandoverId == command.HandoverId
                         && i.Id == command.ItemId,
                    ct);
            if (item is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.ItemNotFound, "Madde bulunamad─▒.");
            }

            if (item.Version != command.ExpectedVersion)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.ConcurrencyConflict,
                    "Madde ba┼şka bir i┼şlem taraf─▒ndan g├╝ncellendi.");
            }

            var validationError = ValidateItem(command.Category, command.Title, command.Description, command.Severity);
            if (validationError is not null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.ValidationFailed, validationError);
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);

            item.Category = command.Category;
            item.Title = command.Title.Trim();
            item.Description = command.Description.Trim();
            item.Severity = command.Severity;
            item.Status = command.Status;
            item.RequiresFollowUp = command.RequiresFollowUp;
            item.EvidenceMetadataJson = command.EvidenceMetadataJson;
            item.UpdatedAtUtc = now;
            item.Version++;
            handover.UpdatedAtUtc = now;
            handover.Version++;

            AddEvent(context, handover, ShiftHandoverAuditEventTypes.ItemUpdated, actorLabel, new { itemId = item.Id }, now);
            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(item.Id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> MarkReadyAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            capabilities,
            command,
            ShiftHandoverStatuses.ReadyForHandover,
            ShiftHandoverAuditEventTypes.ReadyForHandover,
            ShiftHandoverLifecycleEngine.CanMarkReady,
            requireManage: true,
            cancellationToken: cancellationToken);

    public Task<ShiftHandoverWriteResult> RevertToDraftAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            capabilities,
            command,
            ShiftHandoverStatuses.Draft,
            ShiftHandoverAuditEventTypes.RevertedToDraft,
            ShiftHandoverLifecycleEngine.CanRevertToDraft,
            requireManage: true,
            cancellationToken: cancellationToken);

    public Task<ShiftHandoverWriteResult> HandOverAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Devir teslim yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (!ShiftHandoverLifecycleEngine.CanHandOver(handover.Status))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Devir teslim i├ğin uygun durumda de─şil.");
            }

            if (handover.Version != command.ExpectedVersion)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.ConcurrencyConflict,
                    "Kay─▒t ba┼şka bir i┼şlem taraf─▒ndan g├╝ncellendi.");
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            handover.Status = ShiftHandoverStatuses.HandedOver;
            handover.HandedOverAtUtc = now;
            handover.UpdatedAtUtc = now;
            handover.Version++;

            var unresolvedCount = await dbContext.ShiftHandoverItems
                .CountAsync(
                    i => i.TenantId == context.TenantId
                         && i.HandoverId == handover.Id
                         && i.Status == ShiftHandoverItemStatuses.Open,
                    ct);

            AddEvent(context, handover, ShiftHandoverAuditEventTypes.HandedOver, actorLabel, new { unresolvedCount }, now);

            if (handover.IncomingMembershipId is Guid incoming)
            {
                var teamsContext = new TeamsOperationContext(
                    context.TenantId,
                    context.FacilityId,
                    context.MembershipId,
                    context.IdentitySubjectId,
                    context.PermissionKey,
                    context.CorrelationId);

                await notificationRepository.TryCreateFactoryShiftHandoverAsync(
                    teamsContext,
                    incoming,
                    handover.Id,
                    handover.ShiftDate,
                    handover.ShiftPeriod,
                    handover.Summary,
                    unresolvedCount,
                    ct);
            }

            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(handover.Id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> AcknowledgeAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanAcknowledge)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "Teslim alma yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (!ShiftHandoverLifecycleEngine.CanAcknowledge(handover.Status))
            {
                if (handover.Status == ShiftHandoverStatuses.Acknowledged)
                {
                    return ShiftHandoverWriteResult.Success(handover.Id, handover.Version, idempotentReplay: true);
                }

                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Teslim alma i├ğin uygun durumda de─şil.");
            }

            if (handover.Version != command.ExpectedVersion)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.ConcurrencyConflict,
                    "Kay─▒t ba┼şka bir i┼şlem taraf─▒ndan g├╝ncellendi.");
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            handover.Status = ShiftHandoverStatuses.Acknowledged;
            handover.AcknowledgedAtUtc = now;
            handover.UpdatedAtUtc = now;
            handover.Version++;

            AddEvent(context, handover, ShiftHandoverAuditEventTypes.Acknowledged, actorLabel, new { }, now);

            if (handover.OutgoingMembershipId != context.MembershipId)
            {
                var teamsContext = new TeamsOperationContext(
                    context.TenantId,
                    context.FacilityId,
                    context.MembershipId,
                    context.IdentitySubjectId,
                    context.PermissionKey,
                    context.CorrelationId);

                await notificationRepository.TryCreateFactoryShiftHandoverAcknowledgedAsync(
                    teamsContext,
                    handover.OutgoingMembershipId,
                    handover.Id,
                    handover.ShiftDate,
                    handover.ShiftPeriod,
                    actorLabel,
                    ct);
            }

            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(handover.Id, handover.Version);
        }, cancellationToken);

    public Task<ShiftHandoverWriteResult> CompleteAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            capabilities,
            command,
            ShiftHandoverStatuses.Completed,
            ShiftHandoverAuditEventTypes.Completed,
            ShiftHandoverLifecycleEngine.CanComplete,
            requireManage: true,
            onTransition: (handover, now) => handover.CompletedAtUtc = now,
            cancellationToken);

    public Task<ShiftHandoverWriteResult> LinkItemTaskAsync(
        ShiftHandoverOperationContext context,
        LinkShiftHandoverItemTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            var item = await dbContext.ShiftHandoverItems
                .SingleOrDefaultAsync(
                    i => i.TenantId == context.TenantId
                         && i.HandoverId == command.HandoverId
                         && i.Id == command.ItemId,
                    ct);
            if (item is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.ItemNotFound, "Madde bulunamad─▒.");
            }

            if (item.LinkedAiTaskId.HasValue)
            {
                if (item.LinkedAiTaskId == command.AiTaskId)
                {
                    return ShiftHandoverWriteResult.Success(item.Id, handover.Version, idempotentReplay: true);
                }

                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.TaskAlreadyLinked,
                    "Bu maddeye zaten bir g├Ârev ba─şl─▒.");
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            item.LinkedAiTaskId = command.AiTaskId;
            item.UpdatedAtUtc = now;
            item.Version++;
            handover.UpdatedAtUtc = now;
            handover.Version++;

            AddEvent(
                context,
                handover,
                ShiftHandoverAuditEventTypes.FollowUpTaskLinked,
                actorLabel,
                new { itemId = item.Id, taskId = command.AiTaskId },
                now);

            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(item.Id, handover.Version);
        }, cancellationToken);

    public Task<IReadOnlyList<ShiftHandoverEventReadModel>> GetHistoryAsync(
        ShiftHandoverOperationContext context,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var exists = await dbContext.ShiftHandovers.AsNoTracking()
                .AnyAsync(
                    h => h.TenantId == context.TenantId
                         && h.FacilityId == context.FacilityId
                         && h.Id == handoverId,
                    ct);
            if (!exists)
            {
                return (IReadOnlyList<ShiftHandoverEventReadModel>)[];
            }

            return await dbContext.ShiftHandoverEvents.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId && e.HandoverId == handoverId)
                .OrderBy(e => e.OccurredAtUtc)
                .Select(e => new ShiftHandoverEventReadModel(
                    e.Id,
                    e.EventType,
                    e.ActorLabel,
                    e.PayloadJson,
                    e.OccurredAtUtc))
                .ToListAsync(ct);
        }, cancellationToken);

    private Task<ShiftHandoverWriteResult> TransitionAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        string targetStatus,
        string eventType,
        Func<string, bool> canTransitionFrom,
        bool requireManage,
        Action<FactoryShiftHandover, DateTimeOffset>? onTransition = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (requireManage && !capabilities.CanManage)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.AccessDenied,
                    "─░┼şlem yetkisi yok.");
            }

            var handover = await LoadMutableHandoverAsync(context, command.HandoverId, ct);
            if (handover is null)
            {
                return ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.NotFound, "Vardiya devri bulunamad─▒.");
            }

            if (!canTransitionFrom(handover.Status))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Ge├ğersiz durum ge├ği┼şi.");
            }

            if (!ShiftHandoverLifecycleEngine.CanTransition(handover.Status, targetStatus))
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.InvalidTransition,
                    "Ge├ğersiz durum ge├ği┼şi.");
            }

            if (handover.Version != command.ExpectedVersion)
            {
                return ShiftHandoverWriteResult.Fail(
                    ShiftHandoverFailureCodes.ConcurrencyConflict,
                    "Kay─▒t ba┼şka bir i┼şlem taraf─▒ndan g├╝ncellendi.");
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            handover.Status = targetStatus;
            handover.UpdatedAtUtc = now;
            handover.Version++;
            onTransition?.Invoke(handover, now);

            AddEvent(context, handover, eventType, actorLabel, new { status = targetStatus }, now);
            await dbContext.SaveChangesAsync(ct);
            return ShiftHandoverWriteResult.Success(handover.Id, handover.Version);
        }, cancellationToken);

    private async Task<ShiftHandoverDetailReadModel?> LoadDetailAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken)
    {
        if (!capabilities.CanRead)
        {
            return null;
        }

        var handover = await dbContext.ShiftHandovers.AsNoTracking()
            .SingleOrDefaultAsync(
                h => h.TenantId == context.TenantId
                     && h.FacilityId == context.FacilityId
                     && h.Id == handoverId,
                cancellationToken);
        if (handover is null)
        {
            return null;
        }

        var items = await dbContext.ShiftHandoverItems.AsNoTracking()
            .Where(i => i.TenantId == context.TenantId && i.HandoverId == handoverId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ShiftHandoverItemReadModel(
                i.Id,
                i.SortOrder,
                i.Category,
                i.Title,
                i.Description,
                i.Severity,
                i.SourceModule,
                i.SourceEntityId,
                i.Status,
                i.RequiresFollowUp,
                i.LinkedAiTaskId,
                i.EvidenceMetadataJson,
                i.Version,
                i.CreatedAtUtc,
                i.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var events = await dbContext.ShiftHandoverEvents.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.HandoverId == handoverId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(50)
            .Select(e => new ShiftHandoverEventReadModel(
                e.Id,
                e.EventType,
                e.ActorLabel,
                e.PayloadJson,
                e.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var membershipIds = new List<Guid> { handover.OutgoingMembershipId };
        if (handover.IncomingMembershipId is Guid incoming)
        {
            membershipIds.Add(incoming);
        }

        var labels = await LoadMembershipLabelsAsync(context.TenantId, membershipIds, cancellationToken);
        var facilityName = await dbContext.Facilities.AsNoTracking()
            .Where(f => f.TenantId == context.TenantId && f.Id == context.FacilityId)
            .Select(f => f.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? "Tesis";

        return new ShiftHandoverDetailReadModel(
            handover.Id,
            handover.HandoverReference,
            handover.ShiftDate,
            handover.ShiftPeriod,
            handover.Department,
            handover.Summary,
            handover.Status,
            handover.OutgoingMembershipId,
            labels.GetValueOrDefault(handover.OutgoingMembershipId, handover.OutgoingMembershipId.ToString("D")),
            handover.IncomingMembershipId,
            handover.IncomingMembershipId is Guid inc
                ? labels.GetValueOrDefault(inc, inc.ToString("D"))
                : null,
            facilityName,
            handover.CreatedAtUtc,
            handover.UpdatedAtUtc,
            handover.HandedOverAtUtc,
            handover.AcknowledgedAtUtc,
            handover.CompletedAtUtc,
            handover.Version,
            items,
            events,
            capabilities);
    }

    private async Task<FactoryShiftHandover?> LoadMutableHandoverAsync(
        ShiftHandoverOperationContext context,
        Guid handoverId,
        CancellationToken cancellationToken) =>
        await dbContext.ShiftHandovers
            .SingleOrDefaultAsync(
                h => h.TenantId == context.TenantId
                     && h.FacilityId == context.FacilityId
                     && h.Id == handoverId,
                cancellationToken);

    private void AddEvent(
        ShiftHandoverOperationContext context,
        FactoryShiftHandover handover,
        string eventType,
        string actorLabel,
        object payload,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.ShiftHandoverEvents.Add(new ShiftHandoverEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            HandoverId = handover.Id,
            EventType = eventType,
            ActorMembershipId = context.MembershipId.ToString("D"),
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = occurredAtUtc,
        });
    }

    private async Task<string> GenerateReferenceAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var count = await dbContext.ShiftHandovers.AsNoTracking()
            .CountAsync(h => h.TenantId == tenantId, cancellationToken);
        return $"VD-{DateTimeOffset.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    private async Task<Dictionary<Guid, string>> LoadMembershipLabelsAsync(
        Guid tenantId,
        IEnumerable<Guid> membershipIds,
        CancellationToken cancellationToken)
    {
        var ids = membershipIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (
            from membership in dbContext.TenantMemberships.AsNoTracking()
            join account in dbContext.SaasIdentityAccounts.AsNoTracking()
                on membership.IdentitySubjectId equals account.IdentitySubjectId into accounts
            from account in accounts.DefaultIfEmpty()
            where membership.TenantId == tenantId && ids.Contains(membership.Id)
            select new
            {
                membership.Id,
                Label = account != null ? account.DisplayName : membership.MembershipReference,
            }).ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => r.Label);
    }

    private async Task<string> ResolveActorLabelAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var labels = await LoadMembershipLabelsAsync(tenantId, [membershipId], cancellationToken);
        return labels.GetValueOrDefault(membershipId, membershipId.ToString("D"));
    }

    private static string? ValidateCreate(CreateShiftHandoverCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Department))
        {
            return "├£retim alan─▒ / departman zorunludur.";
        }

        if (command.Department.Length > ShiftHandoverConstants.MaxDepartmentLength)
        {
            return "Departman ad─▒ ├ğok uzun.";
        }

        if (command.Summary.Length > ShiftHandoverConstants.MaxSummaryLength)
        {
            return "├ûzet ├ğok uzun.";
        }

        return null;
    }

    private static string? ValidateItem(string category, string title, string description, string severity)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Madde ba┼şl─▒─ş─▒ zorunludur.";
        }

        if (title.Length > ShiftHandoverConstants.MaxItemTitleLength)
        {
            return "Madde ba┼şl─▒─ş─▒ ├ğok uzun.";
        }

        if (description.Length > ShiftHandoverConstants.MaxItemDescriptionLength)
        {
            return "Madde a├ğ─▒klamas─▒ ├ğok uzun.";
        }

        return null;
    }
}

public sealed class UnavailableShiftHandoverRepository : IShiftHandoverRepository
{
    private static ShiftHandoverAccessCapabilities EmptyCapabilities() =>
        new(false, false, false, false);

    public Task<ShiftHandoverListReadModel> ListAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        string? status,
        DateOnly? shiftDate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShiftHandoverListReadModel([], EmptyCapabilities(), ShiftHandoverConstants.AutomationDisclaimer));

    public Task<ShiftHandoverDetailReadModel?> GetByIdAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ShiftHandoverDetailReadModel?>(null);

    public Task<ShiftHandoverWriteResult> CreateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        CreateShiftHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> UpdateAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> AddItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        AddShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> UpdateItemAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        UpdateShiftHandoverItemCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> MarkReadyAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> RevertToDraftAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> HandOverAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> AcknowledgeAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> CompleteAsync(
        ShiftHandoverOperationContext context,
        ShiftHandoverAccessCapabilities capabilities,
        ShiftHandoverLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<ShiftHandoverWriteResult> LinkItemTaskAsync(
        ShiftHandoverOperationContext context,
        LinkShiftHandoverItemTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShiftHandoverWriteResult.Fail(ShiftHandoverFailureCodes.AccessDenied, "Shift handover unavailable."));

    public Task<IReadOnlyList<ShiftHandoverEventReadModel>> GetHistoryAsync(
        ShiftHandoverOperationContext context,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftHandoverEventReadModel>>([]);
}
