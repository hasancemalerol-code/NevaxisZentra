using System.Text.Json;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Teams;

public sealed record TeamsApprovalAccessCapabilities(
    bool CanCreate,
    bool CanReview,
    bool CanManage,
    bool CanReadAiTasks);

public interface ITeamsApprovalRepository
{
    Task<TeamsApprovalListReadModel> ListRequestsAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<TeamsApprovalDetailReadModel?> GetRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<TeamsApprovalWriteResult> CreateRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CreateTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsApprovalWriteResult> RecordDecisionAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        RecordTeamsApprovalDecisionCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsApprovalWriteResult> CancelRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CancelTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsApprovalWriteResult> AssignReviewerAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        AssignTeamsApprovalReviewerCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsApprovalRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ITeamsNotificationRepository notificationRepository) : ITeamsApprovalRepository
{
    public Task<TeamsApprovalListReadModel> ListRequestsAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var memberChannelIds = await LoadMemberChannelIdsAsync(context, ct);
            if (memberChannelIds.Count == 0)
            {
                return new TeamsApprovalListReadModel([], capabilities.CanCreate, capabilities.CanReview, capabilities.CanManage);
            }

            if (channelId is Guid requestedChannel && !memberChannelIds.Contains(requestedChannel))
            {
                return new TeamsApprovalListReadModel([], capabilities.CanCreate, capabilities.CanReview, capabilities.CanManage);
            }

            var assignedRequestIds = await dbContext.TeamsApprovalReviewers.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                            && r.TenantMembershipId == context.MembershipId)
                .Select(r => r.RequestId)
                .ToListAsync(ct);

            var query = dbContext.TeamsApprovalRequests.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && memberChannelIds.Contains(r.ChannelId)
                            && (capabilities.CanManage
                                || r.RequestedByTenantMembershipId == context.MembershipId
                                || assignedRequestIds.Contains(r.Id)));

            if (channelId is Guid scopedChannel)
            {
                query = query.Where(r => r.ChannelId == scopedChannel);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var items = await query
                .OrderByDescending(r => r.CreatedAtUtc)
                .Take(TeamsConstants.MaxPageSize)
                .ToListAsync(ct);

            var channelNames = await LoadChannelNamesAsync(context.TenantId, items.Select(i => i.ChannelId), ct);
            var labels = await LoadMembershipLabelsAsync(
                context.TenantId,
                items.Select(i => i.RequestedByTenantMembershipId),
                ct);

            return new TeamsApprovalListReadModel(
                items.Select(item => new TeamsApprovalSummaryReadModel(
                    item.Id,
                    item.Title,
                    item.Status,
                    channelNames.GetValueOrDefault(item.ChannelId, "Kanal"),
                    item.ChannelId,
                    labels.GetValueOrDefault(item.RequestedByTenantMembershipId, item.RequestedByTenantMembershipId.ToString("D")),
                    item.CreatedAtUtc,
                    item.SourceKind,
                    true)).ToList(),
                capabilities.CanCreate,
                capabilities.CanReview,
                capabilities.CanManage);
        }, cancellationToken);

    public Task<TeamsApprovalDetailReadModel?> GetRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadVisibleDetailAsync(context, capabilities, requestId, ct),
            cancellationToken);

    public Task<TeamsApprovalWriteResult> CreateRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CreateTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanCreate)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Onay isteği oluşturma yetkisi yok.");
            }

            var validation = TeamsApprovalPresentationBuilder.ValidateCreateInput(command);
            if (validation is not null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            var membership = await LoadChannelMembershipAsync(context, command.ChannelId, ct);
            if (membership is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.Id == command.ChannelId,
                    ct);
            if (channel is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Kanal bulunamadı.");
            }

            var sourceKind = command.MessageId.HasValue
                ? TeamsApprovalSourceKinds.Message
                : TeamsApprovalSourceKinds.AiTask;
            var sourceAccess = await ResolveSourceAccessAsync(
                context,
                capabilities,
                command.ChannelId,
                command.MessageId,
                command.AiTaskId,
                ct);
            if (!sourceAccess.CanOpen)
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.AccessDenied,
                    sourceAccess.UnavailableReason ?? "Kaynak bağlama erişimi yok.");
            }

            if (command.Reviewers.Any(r => r.TenantMembershipId == context.MembershipId))
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "İstek sahibi kendi isteğinin inceleyeni olamaz.");
            }

            foreach (var reviewer in command.Reviewers)
            {
                var reviewerValid = await IsEligibleReviewerAsync(context, command.ChannelId, reviewer.TenantMembershipId, ct);
                if (!reviewerValid)
                {
                    return TeamsApprovalWriteResult.Fail(
                        TeamsFailureCodes.ValidationFailed,
                        "İnceleyen gerçek bir kanal üyesi olmalıdır.");
                }
            }

            var existing = await dbContext.TeamsApprovalRequests
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId && r.IdempotencyKey == command.IdempotencyKey,
                    ct);
            if (existing is not null)
            {
                var expectedPurpose = TeamsConstants.BuildApprovalCreateIdempotencyPurpose(
                    command.ChannelId,
                    sourceKind,
                    command.MessageId ?? command.AiTaskId!.Value);
                if (!string.Equals(existing.IdempotencyPurpose, expectedPurpose, StringComparison.Ordinal)
                    || !string.Equals(existing.Title, command.Title.Trim(), StringComparison.Ordinal)
                    || !string.Equals(existing.Rationale, command.Rationale.Trim(), StringComparison.Ordinal))
                {
                    return TeamsApprovalWriteResult.Fail(
                        TeamsFailureCodes.IdempotencyConflict,
                        "Aynı anahtar farklı onay isteği için kullanılamaz.");
                }

                var replay = await LoadVisibleDetailAsync(context, capabilities, existing.Id, ct);
                return replay is null
                    ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                    : TeamsApprovalWriteResult.Success(existing.Id, replay, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            var requestId = Guid.NewGuid();
            var request = new TeamsApprovalRequest
            {
                Id = requestId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ChannelId = command.ChannelId,
                RequestedByTenantMembershipId = context.MembershipId,
                MessageId = command.MessageId,
                AiTaskId = command.AiTaskId,
                SourceKind = sourceKind,
                Title = command.Title.Trim(),
                Rationale = command.Rationale.Trim(),
                Status = TeamsApprovalStatuses.Pending,
                IdempotencyKey = command.IdempotencyKey,
                IdempotencyPurpose = TeamsConstants.BuildApprovalCreateIdempotencyPurpose(
                    command.ChannelId,
                    sourceKind,
                    command.MessageId ?? command.AiTaskId!.Value),
                CreatedAtUtc = now,
            };
            dbContext.TeamsApprovalRequests.Add(request);

            foreach (var reviewer in command.Reviewers)
            {
                dbContext.TeamsApprovalReviewers.Add(new TeamsApprovalReviewer
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    RequestId = requestId,
                    TenantMembershipId = reviewer.TenantMembershipId,
                    Role = reviewer.Role,
                    CreatedAtUtc = now,
                });
            }

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(
                context,
                requestId,
                command.ChannelId,
                TeamsApprovalAuditEventTypes.RequestCreated,
                actorLabel,
                new
                {
                    sourceKind,
                    messageId = command.MessageId,
                    aiTaskId = command.AiTaskId,
                    reviewerCount = command.Reviewers.Count,
                },
                now);

            foreach (var reviewer in command.Reviewers)
            {
                AddAudit(
                    context,
                    requestId,
                    command.ChannelId,
                    TeamsApprovalAuditEventTypes.ReviewerAssigned,
                    actorLabel,
                    new { tenantMembershipId = reviewer.TenantMembershipId, role = reviewer.Role },
                    now);
            }

            foreach (var reviewer in command.Reviewers)
            {
                await notificationRepository.TryCreateApprovalReviewAsync(
                    context,
                    reviewer.TenantMembershipId,
                    requestId,
                    command.Title.Trim(),
                    ct);
            }

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, requestId, ct);
            return detail is null
                ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                : TeamsApprovalWriteResult.Success(requestId, detail);
        }, cancellationToken);

    public Task<TeamsApprovalWriteResult> RecordDecisionAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        RecordTeamsApprovalDecisionCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanReview)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Onay kararı yetkisi yok.");
            }

            var validation = TeamsApprovalPresentationBuilder.ValidateDecisionInput(command.Decision, command.Note);
            if (validation is not null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            var request = await dbContext.TeamsApprovalRequests
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.FacilityId == context.FacilityId
                         && r.Id == command.RequestId,
                    ct);
            if (request is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.");
            }

            var membership = await LoadChannelMembershipAsync(context, request.ChannelId, ct);
            if (membership is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (request.RequestedByTenantMembershipId == context.MembershipId)
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.AccessDenied,
                    "İstek sahibi kendi isteğini karara bağlayamaz.");
            }

            var assigned = await dbContext.TeamsApprovalReviewers.AsNoTracking()
                .AnyAsync(
                    r => r.TenantId == context.TenantId
                         && r.RequestId == request.Id
                         && r.TenantMembershipId == context.MembershipId,
                    ct);
            if (!assigned)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Bu istek için inceleyen değilsiniz.");
            }

            if (!string.Equals(request.Status, TeamsApprovalStatuses.Pending, StringComparison.Ordinal))
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Karara bağlanmış veya iptal edilmiş istek değiştirilemez.");
            }

            var existingDecision = await dbContext.TeamsApprovalDecisions.AsNoTracking()
                .SingleOrDefaultAsync(
                    d => d.TenantId == context.TenantId && d.RequestId == request.Id,
                    ct);
            if (existingDecision is not null)
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Bu istek için karar zaten kaydedildi.");
            }

            var now = DateTimeOffset.UtcNow;
            var note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
            dbContext.TeamsApprovalDecisions.Add(new TeamsApprovalDecision
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                RequestId = request.Id,
                ReviewerTenantMembershipId = context.MembershipId,
                Decision = command.Decision,
                Note = note,
                CreatedAtUtc = now,
            });

            request.Status = TeamsApprovalPresentationBuilder.MapDecisionToStatus(command.Decision);
            request.ResolvedAtUtc = now;

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(
                context,
                request.Id,
                request.ChannelId,
                TeamsApprovalAuditEventTypes.DecisionRecorded,
                actorLabel,
                new { decision = command.Decision, hasNote = note is not null },
                now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, request.Id, ct);
            return detail is null
                ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                : TeamsApprovalWriteResult.Success(request.Id, detail);
        }, cancellationToken);

    public Task<TeamsApprovalWriteResult> CancelRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CancelTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanCreate)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Onay isteği iptal yetkisi yok.");
            }

            var request = await dbContext.TeamsApprovalRequests
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.FacilityId == context.FacilityId
                         && r.Id == command.RequestId,
                    ct);
            if (request is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.");
            }

            if (request.RequestedByTenantMembershipId != context.MembershipId)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Yalnızca istek sahibi bekleyen isteği iptal edebilir.");
            }

            var membership = await LoadChannelMembershipAsync(context, request.ChannelId, ct);
            if (membership is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (!string.Equals(request.Status, TeamsApprovalStatuses.Pending, StringComparison.Ordinal))
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Yalnızca bekleyen istek iptal edilebilir.");
            }

            var now = DateTimeOffset.UtcNow;
            request.Status = TeamsApprovalStatuses.Cancelled;
            request.CancelledAtUtc = now;

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(
                context,
                request.Id,
                request.ChannelId,
                TeamsApprovalAuditEventTypes.RequestCancelled,
                actorLabel,
                new { status = TeamsApprovalStatuses.Cancelled },
                now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, request.Id, ct);
            return detail is null
                ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                : TeamsApprovalWriteResult.Success(request.Id, detail);
        }, cancellationToken);

    public Task<TeamsApprovalWriteResult> AssignReviewerAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        AssignTeamsApprovalReviewerCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanManage)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "İnceleyen atama yetkisi yok.");
            }

            if (!TeamsApprovalPresentationBuilder.IsReviewerRole(command.Role))
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Geçersiz inceleyen rolü.");
            }

            var request = await dbContext.TeamsApprovalRequests
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.FacilityId == context.FacilityId
                         && r.Id == command.RequestId,
                    ct);
            if (request is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.");
            }

            var membership = await LoadChannelMembershipAsync(context, request.ChannelId, ct);
            if (membership is null)
            {
                return TeamsApprovalWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (!string.Equals(request.Status, TeamsApprovalStatuses.Pending, StringComparison.Ordinal))
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Karara bağlanmış isteğe inceleyen eklenemez.");
            }

            if (command.TenantMembershipId == request.RequestedByTenantMembershipId)
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "İstek sahibi inceleyen olarak atanamaz.");
            }

            if (!await IsEligibleReviewerAsync(context, request.ChannelId, command.TenantMembershipId, ct))
            {
                return TeamsApprovalWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "İnceleyen gerçek bir kanal üyesi olmalıdır.");
            }

            var existing = await dbContext.TeamsApprovalReviewers
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.RequestId == request.Id
                         && r.TenantMembershipId == command.TenantMembershipId,
                    ct);
            if (existing is not null)
            {
                var replay = await LoadVisibleDetailAsync(context, capabilities, request.Id, ct);
                return replay is null
                    ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                    : TeamsApprovalWriteResult.Success(request.Id, replay, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            dbContext.TeamsApprovalReviewers.Add(new TeamsApprovalReviewer
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                RequestId = request.Id,
                TenantMembershipId = command.TenantMembershipId,
                Role = command.Role,
                CreatedAtUtc = now,
            });

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(
                context,
                request.Id,
                request.ChannelId,
                TeamsApprovalAuditEventTypes.ReviewerAssigned,
                actorLabel,
                new { tenantMembershipId = command.TenantMembershipId, role = command.Role },
                now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, request.Id, ct);
            return detail is null
                ? TeamsApprovalWriteResult.Fail(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı.")
                : TeamsApprovalWriteResult.Success(request.Id, detail);
        }, cancellationToken);

    private async Task<TeamsApprovalDetailReadModel?> LoadVisibleDetailAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.TeamsApprovalRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.FacilityId == context.FacilityId
                     && r.Id == requestId,
                cancellationToken);
        if (request is null)
        {
            return null;
        }

        var membership = await LoadChannelMembershipAsync(context, request.ChannelId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var isRequester = request.RequestedByTenantMembershipId == context.MembershipId;
        var isAssigned = await dbContext.TeamsApprovalReviewers.AsNoTracking()
            .AnyAsync(
                r => r.TenantId == context.TenantId
                     && r.RequestId == request.Id
                     && r.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!capabilities.CanManage && !isRequester && !isAssigned)
        {
            return null;
        }

        var reviewers = await dbContext.TeamsApprovalReviewers.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId && r.RequestId == request.Id)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var decision = await dbContext.TeamsApprovalDecisions.AsNoTracking()
            .SingleOrDefaultAsync(
                d => d.TenantId == context.TenantId && d.RequestId == request.Id,
                cancellationToken);
        var audit = await dbContext.TeamsApprovalAuditEvents.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.RequestId == request.Id)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var labelIds = reviewers.Select(r => r.TenantMembershipId)
            .Append(request.RequestedByTenantMembershipId)
            .Concat(decision is null ? [] : new[] { decision.ReviewerTenantMembershipId })
            .ToList();
        var labels = await LoadMembershipLabelsAsync(context.TenantId, labelIds, cancellationToken);
        var channel = await dbContext.TeamsChannels.AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId && c.Id == request.ChannelId,
                cancellationToken);
        var channelName = channel?.Name ?? "Kanal";
        var source = await ResolveSourceAccessAsync(
            context,
            capabilities,
            request.ChannelId,
            request.MessageId,
            request.AiTaskId,
            cancellationToken);

        var canDecide = capabilities.CanReview
            && isAssigned
            && !isRequester
            && request.Status == TeamsApprovalStatuses.Pending;
        var canCancel = capabilities.CanCreate
            && isRequester
            && request.Status == TeamsApprovalStatuses.Pending;
        var canManageReviewers = capabilities.CanManage && request.Status == TeamsApprovalStatuses.Pending;

        return new TeamsApprovalDetailReadModel(
            request.Id,
            request.Title,
            request.Rationale,
            request.Status,
            request.ChannelId,
            channelName,
            request.RequestedByTenantMembershipId,
            labels.GetValueOrDefault(request.RequestedByTenantMembershipId, request.RequestedByTenantMembershipId.ToString("D")),
            new TeamsApprovalSourceReadModel(
                request.SourceKind,
                request.MessageId,
                request.AiTaskId,
                channelName,
                source.Label,
                source.CanOpen ? source.DeepLinkPath : null,
                source.CanOpen,
                source.CanOpen ? null : source.UnavailableReason),
            reviewers.Select(r => new TeamsApprovalReviewerReadModel(
                r.TenantMembershipId,
                labels.GetValueOrDefault(r.TenantMembershipId, r.TenantMembershipId.ToString("D")),
                r.Role)).ToList(),
            decision is null
                ? null
                : new TeamsApprovalDecisionReadModel(
                    decision.Id,
                    decision.ReviewerTenantMembershipId,
                    labels.GetValueOrDefault(decision.ReviewerTenantMembershipId, decision.ReviewerTenantMembershipId.ToString("D")),
                    decision.Decision,
                    decision.Note,
                    decision.CreatedAtUtc),
            audit.Select(e => new TeamsApprovalAuditEventReadModel(
                e.Id,
                e.EventType,
                e.ActorLabel,
                e.OccurredAtUtc,
                e.PayloadJson)).ToList(),
            request.CreatedAtUtc,
            request.ResolvedAtUtc,
            request.CancelledAtUtc,
            canDecide,
            canCancel,
            canManageReviewers,
            TeamsConstants.ApprovalDecisionDisclaimer);
    }

    private async Task<(bool CanOpen, string? Label, string? DeepLinkPath, string? UnavailableReason)> ResolveSourceAccessAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid channelId,
        Guid? messageId,
        Guid? aiTaskId,
        CancellationToken cancellationToken)
    {
        if (messageId is Guid mid)
        {
            var message = await dbContext.TeamsMessages.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                         && m.FacilityId == context.FacilityId
                         && m.ChannelId == channelId
                         && m.Id == mid,
                    cancellationToken);
            if (message is null)
            {
                return (false, null, null, "Teams mesajı bulunamadı veya erişim yok.");
            }

            return (
                true,
                TruncatePreview(message.Body),
                TeamsApprovalPresentationBuilder.BuildMessageDeepLink(channelId, mid),
                null);
        }

        if (aiTaskId is Guid tid)
        {
            if (!capabilities.CanReadAiTasks)
            {
                return (false, null, null, "AI görev bağlamına erişiminiz yok.");
            }

            var task = await dbContext.AiTasks.AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId
                         && t.FacilityId == context.FacilityId
                         && t.Id == tid,
                    cancellationToken);
            if (task is null)
            {
                return (false, null, null, "AI görev bulunamadı veya erişim yok.");
            }

            return (
                true,
                task.Title,
                TeamsApprovalPresentationBuilder.BuildTaskDeepLink(tid),
                null);
        }

        return (false, null, null, "Kaynak bağlamı tanımlı değil.");
    }

    private async Task<bool> IsEligibleReviewerAsync(
        TeamsOperationContext context,
        Guid channelId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken)
    {
        var membershipExists = await dbContext.TenantMemberships.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId && m.Id == tenantMembershipId,
                cancellationToken);
        if (!membershipExists)
        {
            return false;
        }

        return await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.ChannelId == channelId
                     && m.TenantMembershipId == tenantMembershipId,
                cancellationToken);
    }

    private Task<TeamsChannelMember?> LoadChannelMembershipAsync(
        TeamsOperationContext context,
        Guid channelId,
        CancellationToken cancellationToken) =>
        dbContext.TeamsChannelMembers.AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.ChannelId == channelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);

    private Task<List<Guid>> LoadMemberChannelIdsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken) =>
        dbContext.TeamsChannelMembers.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.TenantMembershipId == context.MembershipId)
            .Select(m => m.ChannelId)
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, string>> LoadChannelNamesAsync(
        Guid tenantId,
        IEnumerable<Guid> channelIds,
        CancellationToken cancellationToken)
    {
        var ids = channelIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.TeamsChannels.AsNoTracking()
            .Where(c => c.TenantId == tenantId && ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> LoadMembershipLabelsAsync(
        Guid tenantId,
        IEnumerable<Guid> membershipIds,
        CancellationToken cancellationToken)
    {
        var ids = membershipIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
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

    private void AddAudit(
        TeamsOperationContext context,
        Guid requestId,
        Guid channelId,
        string eventType,
        string actorLabel,
        object payload,
        DateTimeOffset now)
    {
        dbContext.TeamsApprovalAuditEvents.Add(new TeamsApprovalAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            RequestId = requestId,
            ChannelId = channelId,
            EventType = eventType,
            OccurredAtUtc = now,
            ActorTenantMembershipId = context.MembershipId.ToString("D"),
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(payload),
        });
    }

    private static string TruncatePreview(string body)
    {
        var trimmed = body.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
    }
}

public sealed class UnavailableTeamsApprovalRepository : ITeamsApprovalRepository
{
    public Task<TeamsApprovalListReadModel> ListRequestsAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsApprovalListReadModel([], false, false, false));

    public Task<TeamsApprovalDetailReadModel?> GetRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsApprovalDetailReadModel?>(null);

    public Task<TeamsApprovalWriteResult> CreateRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CreateTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsApprovalWriteResult> RecordDecisionAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        RecordTeamsApprovalDecisionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsApprovalWriteResult> CancelRequestAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        CancelTeamsApprovalRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsApprovalWriteResult> AssignReviewerAsync(
        TeamsOperationContext context,
        TeamsApprovalAccessCapabilities capabilities,
        AssignTeamsApprovalReviewerCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsApprovalWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));
}
