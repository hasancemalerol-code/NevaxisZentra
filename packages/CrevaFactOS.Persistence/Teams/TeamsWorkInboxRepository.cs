using System.Globalization;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Teams;

public interface ITeamsWorkInboxRepository
{
    Task<TeamsWorkInboxReadModel> GetInboxAsync(
        TeamsOperationContext context,
        TeamsWorkInboxAccessCapabilities capabilities,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsWorkInboxRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ITeamsWorkInboxRepository
{
    public Task<TeamsWorkInboxReadModel> GetInboxAsync(
        TeamsOperationContext context,
        TeamsWorkInboxAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var items = new List<TeamsWorkInboxItemReadModel>();
            if (capabilities.CanReadAiTasks)
            {
                items.AddRange(await LoadAiTasksAsync(context, capabilities.CanReviewAiTasks, ct));
            }

            if (capabilities.CanReviewApprovals)
            {
                items.AddRange(await LoadApprovalsAsync(context, ct));
            }

            if (capabilities.CanAcknowledgeHandovers)
            {
                items.AddRange(await LoadHandoversAsync(context, ct));
            }

            if (capabilities.CanReadMentions)
            {
                items.AddRange(await LoadMentionsAsync(context, ct));
            }

            var deduped = items
                .GroupBy(item => (item.SourceKind, item.SourceId))
                .Select(group => group.First())
                .OrderByDescending(item => item.OccurredAtUtc)
                .Take(TeamsConstants.MaxInboxItems)
                .ToList();

            return new TeamsWorkInboxReadModel(
                deduped,
                new TeamsWorkInboxSourceAvailabilityReadModel(
                    capabilities.CanReadAiTasks,
                    capabilities.CanReviewApprovals,
                    capabilities.CanAcknowledgeHandovers,
                    capabilities.CanReadMentions),
                TeamsConstants.WorkInboxDisclaimer);
        }, cancellationToken);

    private async Task<IReadOnlyList<TeamsWorkInboxItemReadModel>> LoadAiTasksAsync(
        TeamsOperationContext context,
        bool canReview,
        CancellationToken cancellationToken)
    {
        var membershipKey = context.MembershipId.ToString("D");
        var subjectKey = context.IdentitySubjectId.ToString("D");
        var finalStatuses = new[]
        {
            AiTaskStatuses.Completed,
            AiTaskStatuses.Rejected,
            AiTaskStatuses.Failed,
            AiTaskStatuses.Cancelled,
        };
        var query = dbContext.AiTasks.AsNoTracking()
            .Where(t => t.TenantId == context.TenantId && t.FacilityId == context.FacilityId);

        if (canReview)
        {
            query = query.Where(t =>
                t.Status == AiTaskStatuses.AwaitingReview
                || (t.AssigneeJson != null
                    && (t.AssigneeJson.Contains(membershipKey) || t.AssigneeJson.Contains(subjectKey))
                    && !finalStatuses.Contains(t.Status)));
        }
        else
        {
            query = query.Where(t =>
                t.AssigneeJson != null
                && (t.AssigneeJson.Contains(membershipKey) || t.AssigneeJson.Contains(subjectKey))
                && !finalStatuses.Contains(t.Status));
        }

        var tasks = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(TeamsConstants.MaxPageSize)
            .ToListAsync(cancellationToken);

        return tasks.Select(task =>
        {
            var assigned = task.AssigneeJson is not null
                && (task.AssigneeJson.Contains(membershipKey, StringComparison.OrdinalIgnoreCase)
                    || task.AssigneeJson.Contains(subjectKey, StringComparison.OrdinalIgnoreCase));
            return new TeamsWorkInboxItemReadModel(
                TeamsWorkInboxSourceKinds.AiTask,
                task.Id,
                task.Title,
                assigned && task.Status != AiTaskStatuses.AwaitingReview
                    ? TeamsWorkInboxPresentationBuilder.AiTaskAssignedReason
                    : TeamsWorkInboxPresentationBuilder.AiTaskReviewReason,
                task.UpdatedAtUtc,
                TeamsWorkInboxPresentationBuilder.BuildAiTaskDeepLink(task.Id));
        }).ToList();
    }

    private async Task<IReadOnlyList<TeamsWorkInboxItemReadModel>> LoadApprovalsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken)
    {
        var memberChannelIds = await LoadMemberChannelIdsAsync(context, cancellationToken);
        if (memberChannelIds.Count == 0)
        {
            return [];
        }

        var assignedRequestIds = await dbContext.TeamsApprovalReviewers.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId && r.TenantMembershipId == context.MembershipId)
            .Select(r => r.RequestId)
            .ToListAsync(cancellationToken);
        if (assignedRequestIds.Count == 0)
        {
            return [];
        }

        var decidedRequestIds = await dbContext.TeamsApprovalDecisions.AsNoTracking()
            .Where(d => d.TenantId == context.TenantId
                        && d.ReviewerTenantMembershipId == context.MembershipId
                        && assignedRequestIds.Contains(d.RequestId))
            .Select(d => d.RequestId)
            .ToListAsync(cancellationToken);

        var requests = await dbContext.TeamsApprovalRequests.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && memberChannelIds.Contains(r.ChannelId)
                        && assignedRequestIds.Contains(r.Id)
                        && r.RequestedByTenantMembershipId != context.MembershipId
                        && r.Status == TeamsApprovalStatuses.Pending
                        && !decidedRequestIds.Contains(r.Id))
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(TeamsConstants.MaxPageSize)
            .ToListAsync(cancellationToken);

        return requests.Select(request => new TeamsWorkInboxItemReadModel(
            TeamsWorkInboxSourceKinds.Approval,
            request.Id,
            request.Title,
            TeamsWorkInboxPresentationBuilder.ApprovalReason,
            request.CreatedAtUtc,
            TeamsWorkInboxPresentationBuilder.BuildApprovalDeepLink(request.Id))).ToList();
    }

    private async Task<IReadOnlyList<TeamsWorkInboxItemReadModel>> LoadHandoversAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken)
    {
        var memberChannelIds = await LoadMemberChannelIdsAsync(context, cancellationToken);
        if (memberChannelIds.Count == 0)
        {
            return [];
        }

        var acknowledgedIds = await dbContext.TeamsShiftHandoverAcknowledgements.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId && a.AcknowledgedByTenantMembershipId == context.MembershipId)
            .Select(a => a.HandoverId)
            .ToListAsync(cancellationToken);

        var handovers = await dbContext.TeamsShiftHandovers.AsNoTracking()
            .Where(h => h.TenantId == context.TenantId
                        && h.FacilityId == context.FacilityId
                        && memberChannelIds.Contains(h.ChannelId)
                        && h.Status == TeamsHandoverStatuses.Published
                        && h.AuthorTenantMembershipId != context.MembershipId
                        && !acknowledgedIds.Contains(h.Id))
            .OrderByDescending(h => h.PublishedAtUtc ?? h.CreatedAtUtc)
            .Take(TeamsConstants.MaxPageSize)
            .ToListAsync(cancellationToken);

        return handovers.Select(handover => new TeamsWorkInboxItemReadModel(
            TeamsWorkInboxSourceKinds.Handover,
            handover.Id,
            $"{handover.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} vardiya devri",
            TeamsWorkInboxPresentationBuilder.HandoverReason,
            handover.PublishedAtUtc ?? handover.CreatedAtUtc,
            TeamsWorkInboxPresentationBuilder.BuildHandoverDeepLink(handover.Id))).ToList();
    }

    private async Task<IReadOnlyList<TeamsWorkInboxItemReadModel>> LoadMentionsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken)
    {
        var memberChannelIds = await LoadMemberChannelIdsAsync(context, cancellationToken);
        if (memberChannelIds.Count == 0)
        {
            return [];
        }

        var readStates = await dbContext.TeamsChannelReadStates.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.TenantMembershipId == context.MembershipId
                        && memberChannelIds.Contains(r.ChannelId))
            .ToDictionaryAsync(r => r.ChannelId, r => r.LastReadAtUtc, cancellationToken);

        var mentionRows = await (
            from mention in dbContext.TeamsMessageMentions.AsNoTracking()
            join message in dbContext.TeamsMessages.AsNoTracking()
                on new { mention.TenantId, mention.MessageId } equals new { message.TenantId, MessageId = message.Id }
            where mention.TenantId == context.TenantId
                  && mention.MentionedTenantMembershipId == context.MembershipId
                  && message.FacilityId == context.FacilityId
                  && memberChannelIds.Contains(message.ChannelId)
                  && message.AuthorTenantMembershipId != context.MembershipId
            orderby message.CreatedAtUtc descending
            select new { message.Id, message.ChannelId, message.Body, message.CreatedAtUtc }
        ).Take(TeamsConstants.MaxPageSize).ToListAsync(cancellationToken);

        return mentionRows
            .Where(row =>
                !readStates.TryGetValue(row.ChannelId, out var lastRead)
                || row.CreatedAtUtc > lastRead)
            .Select(row => new TeamsWorkInboxItemReadModel(
                TeamsWorkInboxSourceKinds.Mention,
                row.Id,
                TrimTitle(row.Body),
                TeamsWorkInboxPresentationBuilder.MentionReason,
                row.CreatedAtUtc,
                TeamsWorkInboxPresentationBuilder.BuildMentionDeepLink(row.ChannelId, row.Id)))
            .ToList();
    }

    private Task<List<Guid>> LoadMemberChannelIdsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken) =>
        dbContext.TeamsChannelMembers.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.TenantMembershipId == context.MembershipId)
            .Select(m => m.ChannelId)
            .ToListAsync(cancellationToken);

    private static string TrimTitle(string body)
    {
        var trimmed = body.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..120];
    }
}

public sealed class UnavailableTeamsWorkInboxRepository : ITeamsWorkInboxRepository
{
    public Task<TeamsWorkInboxReadModel> GetInboxAsync(
        TeamsOperationContext context,
        TeamsWorkInboxAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsWorkInboxReadModel(
            [],
            new TeamsWorkInboxSourceAvailabilityReadModel(false, false, false, false),
            TeamsConstants.WorkInboxDisclaimer));
}
