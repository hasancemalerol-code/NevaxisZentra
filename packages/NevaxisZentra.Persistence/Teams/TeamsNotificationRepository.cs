using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Teams;

public interface ITeamsNotificationRepository
{
    Task<TeamsNotificationListReadModel> ListAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeamsNotificationWriteResult> MarkReadAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        MarkTeamsNotificationReadCommand command,
        CancellationToken cancellationToken = default);

    Task TryCreateDirectMessageReceivedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid conversationId,
        Guid messageId,
        string authorLabel,
        string messageBody,
        CancellationToken cancellationToken = default);

    Task TryCreateMentionAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid channelId,
        string channelName,
        Guid messageId,
        string messageBody,
        CancellationToken cancellationToken = default);

    Task TryCreateApprovalReviewAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid requestId,
        string requestTitle,
        CancellationToken cancellationToken = default);

    Task TryCreateHandoverAcknowledgementAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftType,
        CancellationToken cancellationToken = default);

    Task TryCreateFactoryShiftHandoverAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string summary,
        int unresolvedCount,
        CancellationToken cancellationToken = default);

    Task TryCreateFactoryShiftHandoverAcknowledgedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string acknowledgedByLabel,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskLinkAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid linkId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskAssignedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskEscalatedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        int escalationLevel,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskClosureApprovalRequestedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskClosureApprovedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskClosureRejectedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    Task TryCreateTaskReopenedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsNotificationRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ITeamsNotificationRepository
{
    public Task<TeamsNotificationListReadModel> ListAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!capabilities.CanRead)
            {
                return new TeamsNotificationListReadModel([], 0, TeamsConstants.NotificationDisclaimer);
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, TeamsConstants.MaxPageSize);

            var query = dbContext.TeamsNotifications.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId
                            && n.FacilityId == context.FacilityId
                            && n.RecipientTenantMembershipId == context.MembershipId);

            if (unreadOnly)
            {
                query = query.Where(n => n.ReadAtUtc == null);
            }

            var rows = await query
                .OrderByDescending(n => n.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var unreadCount = await dbContext.TeamsNotifications.AsNoTracking()
                .CountAsync(
                    n => n.TenantId == context.TenantId
                         && n.FacilityId == context.FacilityId
                         && n.RecipientTenantMembershipId == context.MembershipId
                         && n.ReadAtUtc == null,
                    ct);

            var items = new List<TeamsNotificationReadModel>();
            foreach (var row in rows)
            {
                items.Add(await MapNotificationAsync(context, row, ct));
            }

            return new TeamsNotificationListReadModel(items, unreadCount, TeamsConstants.NotificationDisclaimer);
        }, cancellationToken);

    public Task<TeamsNotificationWriteResult> MarkReadAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        MarkTeamsNotificationReadCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanRead)
            {
                return TeamsNotificationWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Bildirim okuma yetkisi yok.");
            }

            var notification = await dbContext.TeamsNotifications
                .SingleOrDefaultAsync(
                    n => n.TenantId == context.TenantId
                         && n.FacilityId == context.FacilityId
                         && n.Id == command.NotificationId
                         && n.RecipientTenantMembershipId == context.MembershipId,
                    ct);
            if (notification is null)
            {
                return TeamsNotificationWriteResult.Fail(TeamsFailureCodes.NotFound, "Bildirim bulunamadı.");
            }

            if (notification.ReadAtUtc.HasValue)
            {
                return TeamsNotificationWriteResult.Success(notification.Id, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            notification.ReadAtUtc = now;
            var actorLabel = context.MembershipId.ToString("D");
            dbContext.TeamsNotificationAuditEvents.Add(new TeamsNotificationAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                NotificationId = notification.Id,
                EventType = TeamsNotificationAuditEventTypes.Read,
                OccurredAtUtc = now,
                ActorTenantMembershipId = actorLabel,
                ActorLabel = actorLabel,
                PayloadJson = JsonSerializer.Serialize(new { notificationId = notification.Id }),
            });

            await dbContext.SaveChangesAsync(ct);
            return TeamsNotificationWriteResult.Success(notification.Id);
        }, cancellationToken);

    public Task TryCreateDirectMessageReceivedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid conversationId,
        Guid messageId,
        string authorLabel,
        string messageBody,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.DirectMessage,
            TeamsNotificationSourceModules.TeamsDirect,
            messageId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildDirectMessageRoute(conversationId),
            TeamsNotificationPresentationBuilder.BuildDirectMessageTitle(authorLabel),
            TeamsNotificationPresentationBuilder.DirectMessageReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(messageBody),
            TeamsNotificationPresentationBuilder.BuildDirectMessageFingerprint(messageId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateMentionAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid channelId,
        string channelName,
        Guid messageId,
        string messageBody,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.Mention,
            TeamsNotificationSourceModules.Teams,
            messageId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildMentionRoute(channelId, messageId),
            TeamsNotificationPresentationBuilder.BuildMentionTitle(channelName),
            TeamsNotificationPresentationBuilder.MentionReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(messageBody),
            TeamsNotificationPresentationBuilder.BuildMentionFingerprint(messageId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateApprovalReviewAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid requestId,
        string requestTitle,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.ApprovalReview,
            TeamsNotificationSourceModules.Teams,
            requestId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildApprovalRoute(requestId),
            TeamsNotificationPresentationBuilder.BuildApprovalReviewTitle(requestTitle),
            TeamsNotificationPresentationBuilder.ApprovalReviewReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(requestTitle),
            TeamsNotificationPresentationBuilder.BuildApprovalReviewFingerprint(requestId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateHandoverAcknowledgementAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftType,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.HandoverAcknowledgement,
            TeamsNotificationSourceModules.Teams,
            handoverId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildHandoverRoute(handoverId),
            TeamsNotificationPresentationBuilder.BuildHandoverTitle(shiftDate, shiftType),
            TeamsNotificationPresentationBuilder.HandoverAcknowledgementReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(
                TeamsNotificationPresentationBuilder.BuildHandoverTitle(shiftDate, shiftType)),
            TeamsNotificationPresentationBuilder.BuildHandoverAcknowledgementFingerprint(handoverId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateFactoryShiftHandoverAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string summary,
        int unresolvedCount,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.FactoryShiftHandover,
            TeamsNotificationSourceModules.ShiftHandover,
            handoverId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildFactoryShiftHandoverRoute(handoverId),
            TeamsNotificationPresentationBuilder.BuildFactoryShiftHandoverTitle(shiftDate, shiftPeriod, unresolvedCount),
            TeamsNotificationPresentationBuilder.FactoryShiftHandoverReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(summary),
            TeamsNotificationPresentationBuilder.BuildFactoryShiftHandoverFingerprint(handoverId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateFactoryShiftHandoverAcknowledgedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string acknowledgedByLabel,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.FactoryShiftHandoverAcknowledged,
            TeamsNotificationSourceModules.ShiftHandover,
            handoverId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildFactoryShiftHandoverRoute(handoverId),
            TeamsNotificationPresentationBuilder.BuildHandoverTitle(shiftDate, shiftPeriod),
            TeamsNotificationPresentationBuilder.FactoryShiftHandoverAcknowledgedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview($"{acknowledgedByLabel} tarafından teslim alındı."),
            TeamsNotificationPresentationBuilder.BuildFactoryShiftHandoverAcknowledgedFingerprint(handoverId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskLinkAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid linkId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskLink,
            TeamsNotificationSourceModules.AiTask,
            linkId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskLinkTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskLinkReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskLinkFingerprint(linkId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskAssignedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskAssigned,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskAssignedTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskAssignedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskAssignedFingerprint(taskId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskEscalatedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        int escalationLevel,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskEscalated,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskEscalatedTitle(taskReference, escalationLevel),
            TeamsNotificationPresentationBuilder.TaskEscalatedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskEscalatedFingerprint(taskId, escalationLevel, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskClosureApprovalRequestedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskClosureApprovalRequested,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskClosureApprovalRequestedTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskClosureApprovalRequestedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskClosureApprovalRequestedFingerprint(taskId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskClosureApprovedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskClosureApproved,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskClosureApprovedTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskClosureApprovedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskClosureApprovedFingerprint(taskId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskClosureRejectedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        string? rejectionReason,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskClosureRejected,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskClosureRejectedTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskClosureRejectedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(rejectionReason ?? taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskClosureRejectedFingerprint(taskId, recipientMembershipId),
            cancellationToken);

    public Task TryCreateTaskReopenedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) =>
        TryCreateAsync(
            context,
            recipientMembershipId,
            TeamsNotificationTypes.TaskReopened,
            TeamsNotificationSourceModules.AiTask,
            taskId.ToString("D"),
            TeamsNotificationPresentationBuilder.BuildTaskLinkRoute(taskId),
            TeamsNotificationPresentationBuilder.BuildTaskReopenedTitle(taskReference),
            TeamsNotificationPresentationBuilder.TaskReopenedReason,
            TeamsNotificationPresentationBuilder.TruncatePreview(taskReference),
            TeamsNotificationPresentationBuilder.BuildTaskReopenedFingerprint(taskId, recipientMembershipId),
            cancellationToken);

    private async Task TryCreateAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        string notificationType,
        string sourceModule,
        string sourceEntityId,
        string routeKey,
        string title,
        string reason,
        string preview,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (recipientMembershipId == context.MembershipId)
        {
            return;
        }

        var exists = await dbContext.TeamsNotifications.AsNoTracking()
            .AnyAsync(
                n => n.TenantId == context.TenantId
                     && n.RecipientTenantMembershipId == recipientMembershipId
                     && n.SourceEventFingerprint == fingerprint,
                cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var notificationId = Guid.NewGuid();
        var actorLabel = context.MembershipId.ToString("D");
        dbContext.TeamsNotifications.Add(new TeamsNotification
        {
            Id = notificationId,
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            RecipientTenantMembershipId = recipientMembershipId,
            NotificationType = notificationType,
            SourceModule = sourceModule,
            SourceEntityId = sourceEntityId,
            SourceRouteKey = routeKey,
            SourceTitle = TeamsNotificationPresentationBuilder.TruncatePreview(title),
            SourcePreview = preview,
            SourceEventFingerprint = fingerprint,
            CreatedAtUtc = now,
        });
        dbContext.TeamsNotificationAuditEvents.Add(new TeamsNotificationAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            NotificationId = notificationId,
            EventType = TeamsNotificationAuditEventTypes.Created,
            OccurredAtUtc = now,
            ActorTenantMembershipId = actorLabel,
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(new
            {
                notificationType,
                sourceModule,
                sourceEntityId,
                recipientMembershipId,
                fingerprint,
            }),
        });
    }

    private async Task<TeamsNotificationReadModel> MapNotificationAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        var access = await ResolveSourceAccessAsync(context, row, cancellationToken);
        if (!access.CanOpen)
        {
            return new TeamsNotificationReadModel(
                row.Id,
                row.NotificationType,
                row.SourceModule,
                row.SourceEntityId,
                row.SourceRouteKey,
                ResolveGenericTitle(row.NotificationType),
                ResolveGenericReason(row.NotificationType),
                null,
                row.CreatedAtUtc,
                row.ReadAtUtc,
                false,
                access.UnavailableReason ?? TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        return new TeamsNotificationReadModel(
            row.Id,
            row.NotificationType,
            row.SourceModule,
            row.SourceEntityId,
            row.SourceRouteKey,
            row.SourceTitle,
            ResolveGenericReason(row.NotificationType),
            row.SourcePreview,
            row.CreatedAtUtc,
            row.ReadAtUtc,
            true,
            null);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> ResolveSourceAccessAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken) =>
        row.NotificationType switch
        {
            TeamsNotificationTypes.DirectMessage => await CanOpenDirectMessageAsync(context, row, cancellationToken),
            TeamsNotificationTypes.Mention => await CanOpenMentionAsync(context, row, cancellationToken),
            TeamsNotificationTypes.ApprovalReview => await CanOpenApprovalAsync(context, row, cancellationToken),
            TeamsNotificationTypes.HandoverAcknowledgement => await CanOpenHandoverAsync(context, row, cancellationToken),
            TeamsNotificationTypes.TaskLink => await CanOpenTaskLinkAsync(context, row, cancellationToken),
            TeamsNotificationTypes.TaskAssigned
                or TeamsNotificationTypes.TaskEscalated
                or TeamsNotificationTypes.TaskSlaWarning
                or TeamsNotificationTypes.TaskClosureApprovalRequested
                or TeamsNotificationTypes.TaskClosureApproved
                or TeamsNotificationTypes.TaskClosureRejected
                or TeamsNotificationTypes.TaskReopened => await CanOpenAssignedTaskAsync(context, row, cancellationToken),
            _ => (false, TeamsNotificationUnavailableReasons.SourceInaccessible),
        };

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenAssignedTaskAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (row.RecipientTenantMembershipId != context.MembershipId)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        if (!Guid.TryParse(row.SourceEntityId, out var taskId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var taskExists = await dbContext.AiTasks.AsNoTracking()
            .AnyAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == taskId,
                cancellationToken);

        return taskExists
            ? (true, null)
            : (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenDirectMessageAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(row.SourceEntityId, out var messageId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var message = await dbContext.TeamsDirectMessages.AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.Id == messageId,
                cancellationToken);
        if (message is null)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var isParticipant = await dbContext.TeamsDirectConversationMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ConversationId == message.ConversationId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        return isParticipant
            ? (true, null)
            : (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenMentionAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(row.SourceEntityId, out var messageId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var message = await dbContext.TeamsMessages.AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.Id == messageId,
                cancellationToken);
        if (message is null)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ChannelId == message.ChannelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!isMember)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var mentioned = await dbContext.TeamsMessageMentions.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.MessageId == messageId
                     && m.MentionedTenantMembershipId == context.MembershipId,
                cancellationToken);
        return mentioned
            ? (true, null)
            : (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenApprovalAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(row.SourceEntityId, out var requestId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var request = await dbContext.TeamsApprovalRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.FacilityId == context.FacilityId
                     && r.Id == requestId,
                cancellationToken);
        if (request is null
            || !string.Equals(request.Status, TeamsApprovalStatuses.Pending, StringComparison.Ordinal)
            || request.RequestedByTenantMembershipId == context.MembershipId)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var assigned = await dbContext.TeamsApprovalReviewers.AsNoTracking()
            .AnyAsync(
                r => r.TenantId == context.TenantId
                     && r.RequestId == requestId
                     && r.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!assigned)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ChannelId == request.ChannelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        return isMember
            ? (true, null)
            : (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenHandoverAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(row.SourceEntityId, out var handoverId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var handover = await dbContext.TeamsShiftHandovers.AsNoTracking()
            .SingleOrDefaultAsync(
                h => h.TenantId == context.TenantId
                     && h.FacilityId == context.FacilityId
                     && h.Id == handoverId,
                cancellationToken);
        if (handover is null
            || !string.Equals(handover.Status, TeamsHandoverStatuses.Published, StringComparison.Ordinal)
            || handover.AuthorTenantMembershipId == context.MembershipId)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ChannelId == handover.ChannelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!isMember)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var acked = await dbContext.TeamsShiftHandoverAcknowledgements.AsNoTracking()
            .AnyAsync(
                a => a.TenantId == context.TenantId
                     && a.HandoverId == handoverId
                     && a.AcknowledgedByTenantMembershipId == context.MembershipId,
                cancellationToken);
        return acked
            ? (false, TeamsNotificationUnavailableReasons.SourceInaccessible)
            : (true, null);
    }

    private async Task<(bool CanOpen, string? UnavailableReason)> CanOpenTaskLinkAsync(
        TeamsOperationContext context,
        TeamsNotification row,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(row.SourceEntityId, out var linkId))
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var link = await dbContext.TeamsMessageTaskLinks.AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.TenantId == context.TenantId
                     && l.FacilityId == context.FacilityId
                     && l.Id == linkId,
                cancellationToken);
        if (link is null)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ChannelId == link.ChannelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!isMember)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var task = await dbContext.AiTasks.AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == link.AiTaskId,
                cancellationToken);
        if (task is null)
        {
            return (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
        }

        var membershipKey = context.MembershipId.ToString("D");
        var hasTaskAccess = task.AssigneeJson is not null
            && task.AssigneeJson.Contains(membershipKey, StringComparison.OrdinalIgnoreCase);
        return hasTaskAccess
            ? (true, null)
            : (false, TeamsNotificationUnavailableReasons.SourceInaccessible);
    }

    private static string ResolveGenericTitle(string notificationType) =>
        notificationType switch
        {
            TeamsNotificationTypes.DirectMessage => "Özel mesaj bildirimi",
            TeamsNotificationTypes.Mention => "Etiket bildirimi",
            TeamsNotificationTypes.ApprovalReview => "Onay bildirimi",
            TeamsNotificationTypes.HandoverAcknowledgement => "Vardiya devri bildirimi",
            TeamsNotificationTypes.TaskLink => "Görev bağlantı bildirimi",
            TeamsNotificationTypes.TaskAssigned => "Görev atama bildirimi",
            TeamsNotificationTypes.TaskEscalated => "Görev eskalasyon bildirimi",
            TeamsNotificationTypes.TaskSlaWarning => "Görev SLA uyarı bildirimi",
            TeamsNotificationTypes.TaskClosureApprovalRequested => "Görev kapanış onay bildirimi",
            TeamsNotificationTypes.TaskClosureApproved => "Görev kapanış onaylandı bildirimi",
            TeamsNotificationTypes.TaskClosureRejected => "Görev kapanış ret bildirimi",
            TeamsNotificationTypes.TaskReopened => "Görev yeniden açıldı bildirimi",
            _ => "Teams bildirimi",
        };

    private static string ResolveGenericReason(string notificationType) =>
        notificationType switch
        {
            TeamsNotificationTypes.DirectMessage => TeamsNotificationPresentationBuilder.DirectMessageReason,
            TeamsNotificationTypes.Mention => TeamsNotificationPresentationBuilder.MentionReason,
            TeamsNotificationTypes.ApprovalReview => TeamsNotificationPresentationBuilder.ApprovalReviewReason,
            TeamsNotificationTypes.HandoverAcknowledgement => TeamsNotificationPresentationBuilder.HandoverAcknowledgementReason,
            TeamsNotificationTypes.TaskLink => TeamsNotificationPresentationBuilder.TaskLinkReason,
            TeamsNotificationTypes.TaskAssigned => TeamsNotificationPresentationBuilder.TaskAssignedReason,
            TeamsNotificationTypes.TaskEscalated => TeamsNotificationPresentationBuilder.TaskEscalatedReason,
            TeamsNotificationTypes.TaskSlaWarning => TeamsNotificationPresentationBuilder.TaskSlaWarningReason,
            TeamsNotificationTypes.TaskClosureApprovalRequested => TeamsNotificationPresentationBuilder.TaskClosureApprovalRequestedReason,
            TeamsNotificationTypes.TaskClosureApproved => TeamsNotificationPresentationBuilder.TaskClosureApprovedReason,
            TeamsNotificationTypes.TaskClosureRejected => TeamsNotificationPresentationBuilder.TaskClosureRejectedReason,
            TeamsNotificationTypes.TaskReopened => TeamsNotificationPresentationBuilder.TaskReopenedReason,
            _ => "Teams bildirimi",
        };
}

public sealed class UnavailableTeamsNotificationRepository : ITeamsNotificationRepository
{
    public Task<TeamsNotificationListReadModel> ListAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsNotificationListReadModel([], 0, TeamsConstants.NotificationDisclaimer));

    public Task<TeamsNotificationWriteResult> MarkReadAsync(
        TeamsOperationContext context,
        TeamsNotificationAccessCapabilities capabilities,
        MarkTeamsNotificationReadCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsNotificationWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Bildirimler kullanılamıyor."));

    public Task TryCreateDirectMessageReceivedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid conversationId,
        Guid messageId,
        string authorLabel,
        string messageBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateMentionAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid channelId,
        string channelName,
        Guid messageId,
        string messageBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateApprovalReviewAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid requestId,
        string requestTitle,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateHandoverAcknowledgementAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftType,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateFactoryShiftHandoverAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string summary,
        int unresolvedCount,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateFactoryShiftHandoverAcknowledgedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid handoverId,
        DateOnly shiftDate,
        string shiftPeriod,
        string acknowledgedByLabel,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskLinkAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid linkId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskAssignedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskEscalatedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        int escalationLevel,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskClosureApprovalRequestedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskClosureApprovedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskClosureRejectedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        string? rejectionReason,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TryCreateTaskReopenedAsync(
        TeamsOperationContext context,
        Guid recipientMembershipId,
        Guid taskId,
        string taskReference,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
