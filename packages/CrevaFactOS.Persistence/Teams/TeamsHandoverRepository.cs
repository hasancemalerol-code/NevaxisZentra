using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Teams;

public interface ITeamsHandoverRepository
{
    Task<TeamsHandoverListReadModel> ListHandoversAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<TeamsHandoverDetailReadModel?> GetHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default);

    Task<TeamsHandoverWriteResult> CreateHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        CreateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsHandoverWriteResult> UpdateDraftAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        UpdateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsHandoverWriteResult> PublishHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        PublishTeamsHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsHandoverWriteResult> AcknowledgeHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        AcknowledgeTeamsHandoverCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsHandoverRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ITeamsNotificationRepository notificationRepository) : ITeamsHandoverRepository
{
    public Task<TeamsHandoverListReadModel> ListHandoversAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!capabilities.CanRead)
            {
                return new TeamsHandoverListReadModel([], false, false, false);
            }

            var memberChannelIds = await LoadMemberChannelIdsAsync(context, ct);
            if (memberChannelIds.Count == 0)
            {
                return new TeamsHandoverListReadModel([], capabilities.CanCreate, capabilities.CanAcknowledge, capabilities.CanManage);
            }

            if (channelId is Guid requested && !memberChannelIds.Contains(requested))
            {
                return new TeamsHandoverListReadModel([], capabilities.CanCreate, capabilities.CanAcknowledge, capabilities.CanManage);
            }

            var query = dbContext.TeamsShiftHandovers.AsNoTracking()
                .Where(h => h.TenantId == context.TenantId
                            && h.FacilityId == context.FacilityId
                            && memberChannelIds.Contains(h.ChannelId)
                            && (capabilities.CanManage
                                || h.Status == TeamsHandoverStatuses.Published
                                || h.AuthorTenantMembershipId == context.MembershipId));

            if (channelId is Guid scoped)
            {
                query = query.Where(h => h.ChannelId == scoped);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(h => h.Status == status);
            }

            var items = await query
                .OrderByDescending(h => h.CreatedAtUtc)
                .Take(TeamsConstants.MaxPageSize)
                .ToListAsync(ct);

            var channelNames = await LoadChannelNamesAsync(context.TenantId, items.Select(i => i.ChannelId), ct);
            var labels = await LoadMembershipLabelsAsync(context.TenantId, items.Select(i => i.AuthorTenantMembershipId), ct);
            var itemCounts = await dbContext.TeamsShiftHandoverItems.AsNoTracking()
                .Where(i => i.TenantId == context.TenantId && items.Select(h => h.Id).Contains(i.HandoverId))
                .GroupBy(i => i.HandoverId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
            var ackCounts = await dbContext.TeamsShiftHandoverAcknowledgements.AsNoTracking()
                .Where(a => a.TenantId == context.TenantId && items.Select(h => h.Id).Contains(a.HandoverId))
                .GroupBy(a => a.HandoverId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            return new TeamsHandoverListReadModel(
                items.Select(item => new TeamsHandoverSummaryReadModel(
                    item.Id,
                    item.ChannelId,
                    channelNames.GetValueOrDefault(item.ChannelId, "Kanal"),
                    item.ShiftDate,
                    item.ShiftType,
                    item.Status,
                    labels.GetValueOrDefault(item.AuthorTenantMembershipId, item.AuthorTenantMembershipId.ToString("D")),
                    item.CreatedAtUtc,
                    item.PublishedAtUtc,
                    itemCounts.GetValueOrDefault(item.Id),
                    ackCounts.GetValueOrDefault(item.Id))).ToList(),
                capabilities.CanCreate,
                capabilities.CanAcknowledge,
                capabilities.CanManage);
        }, cancellationToken);

    public Task<TeamsHandoverDetailReadModel?> GetHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadVisibleDetailAsync(context, capabilities, handoverId, ct),
            cancellationToken);

    public Task<TeamsHandoverWriteResult> CreateHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        CreateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanCreate)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Vardiya devri oluşturma yetkisi yok.");
            }

            var validation = TeamsHandoverPresentationBuilder.ValidateCreateInput(command);
            if (validation is not null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            var channel = await LoadHandoverChannelAsync(context, command.ChannelId, ct);
            if (channel is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (!TeamsHandoverPresentationBuilder.IsHandoverChannelType(channel.ChannelType))
            {
                return TeamsHandoverWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Vardiya devri yalnızca operasyon veya departman kanalında oluşturulabilir.");
            }

            var itemError = await ValidateItemSourcesAsync(context, capabilities, command.ChannelId, command.Items, ct);
            if (itemError is not null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, itemError);
            }

            var existing = await dbContext.TeamsShiftHandovers
                .SingleOrDefaultAsync(
                    h => h.TenantId == context.TenantId && h.IdempotencyKey == command.IdempotencyKey,
                    ct);
            if (existing is not null)
            {
                var expectedPurpose = TeamsConstants.BuildHandoverCreateIdempotencyPurpose(
                    command.ChannelId, command.ShiftDate, command.ShiftType);
                if (!string.Equals(existing.IdempotencyPurpose, expectedPurpose, StringComparison.Ordinal)
                    || !string.Equals(existing.Summary, command.Summary.Trim(), StringComparison.Ordinal)
                    || !string.Equals(existing.SafetyNote, command.SafetyNote.Trim(), StringComparison.Ordinal)
                    || existing.ShiftDate != command.ShiftDate
                    || !string.Equals(existing.ShiftType, command.ShiftType, StringComparison.Ordinal)
                    || existing.ChannelId != command.ChannelId)
                {
                    return TeamsHandoverWriteResult.Fail(
                        TeamsFailureCodes.IdempotencyConflict,
                        "Aynı anahtar farklı vardiya devri için kullanılamaz.");
                }

                var replay = await LoadVisibleDetailAsync(context, capabilities, existing.Id, ct);
                return replay is null
                    ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                    : TeamsHandoverWriteResult.Success(existing.Id, replay, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            var handoverId = Guid.NewGuid();
            dbContext.TeamsShiftHandovers.Add(new TeamsShiftHandover
            {
                Id = handoverId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ChannelId = command.ChannelId,
                AuthorTenantMembershipId = context.MembershipId,
                ShiftDate = command.ShiftDate,
                ShiftType = command.ShiftType,
                Status = TeamsHandoverStatuses.Draft,
                Summary = command.Summary.Trim(),
                SafetyNote = command.SafetyNote.Trim(),
                IdempotencyKey = command.IdempotencyKey,
                IdempotencyPurpose = TeamsConstants.BuildHandoverCreateIdempotencyPurpose(
                    command.ChannelId, command.ShiftDate, command.ShiftType),
                CreatedAtUtc = now,
            });

            AddItems(context, handoverId, command.Items, now);

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(context, handoverId, command.ChannelId, TeamsHandoverAuditEventTypes.Created, actorLabel, new
            {
                itemCount = command.Items.Count,
                shiftType = command.ShiftType,
            }, now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, handoverId, ct);
            return detail is null
                ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                : TeamsHandoverWriteResult.Success(handoverId, detail);
        }, cancellationToken);

    public Task<TeamsHandoverWriteResult> UpdateDraftAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        UpdateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanCreate)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Vardiya devri güncelleme yetkisi yok.");
            }

            var validation = TeamsHandoverPresentationBuilder.ValidateDraftFields(
                command.ShiftDate, command.ShiftType, command.Summary, command.SafetyNote, command.Items);
            if (validation is not null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            var handover = await dbContext.TeamsShiftHandovers
                .Include(h => h.Items)
                .SingleOrDefaultAsync(
                    h => h.TenantId == context.TenantId
                         && h.FacilityId == context.FacilityId
                         && h.Id == command.HandoverId,
                    ct);
            if (handover is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.");
            }

            if (handover.AuthorTenantMembershipId != context.MembershipId)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Yalnızca yazar taslağı güncelleyebilir.");
            }

            if (await LoadChannelMembershipAsync(context, handover.ChannelId, ct) is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (!string.Equals(handover.Status, TeamsHandoverStatuses.Draft, StringComparison.Ordinal))
            {
                return TeamsHandoverWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Yayımlanmış vardiya devri değiştirilemez.");
            }

            var itemError = await ValidateItemSourcesAsync(context, capabilities, handover.ChannelId, command.Items, ct);
            if (itemError is not null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, itemError);
            }

            handover.ShiftDate = command.ShiftDate;
            handover.ShiftType = command.ShiftType;
            handover.Summary = command.Summary.Trim();
            handover.SafetyNote = command.SafetyNote.Trim();
            dbContext.TeamsShiftHandoverItems.RemoveRange(handover.Items);
            AddItems(context, handover.Id, command.Items, DateTimeOffset.UtcNow);

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(context, handover.Id, handover.ChannelId, TeamsHandoverAuditEventTypes.Updated, actorLabel, new
            {
                itemCount = command.Items.Count,
            }, now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, handover.Id, ct);
            return detail is null
                ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                : TeamsHandoverWriteResult.Success(handover.Id, detail);
        }, cancellationToken);

    public Task<TeamsHandoverWriteResult> PublishHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        PublishTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanCreate)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Vardiya devri yayımlama yetkisi yok.");
            }

            var handover = await dbContext.TeamsShiftHandovers
                .SingleOrDefaultAsync(
                    h => h.TenantId == context.TenantId
                         && h.FacilityId == context.FacilityId
                         && h.Id == command.HandoverId,
                    ct);
            if (handover is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.");
            }

            if (handover.AuthorTenantMembershipId != context.MembershipId)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Yalnızca yazar taslağı yayımlayabilir.");
            }

            if (await LoadChannelMembershipAsync(context, handover.ChannelId, ct) is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (string.Equals(handover.Status, TeamsHandoverStatuses.Published, StringComparison.Ordinal))
            {
                var replay = await LoadVisibleDetailAsync(context, capabilities, handover.Id, ct);
                return replay is null
                    ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                    : TeamsHandoverWriteResult.Success(handover.Id, replay, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            handover.Status = TeamsHandoverStatuses.Published;
            handover.PublishedAtUtc = now;

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(context, handover.Id, handover.ChannelId, TeamsHandoverAuditEventTypes.Published, actorLabel, new
            {
                status = TeamsHandoverStatuses.Published,
            }, now);

            var memberIds = await dbContext.TeamsChannelMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.ChannelId == handover.ChannelId)
                .Select(m => m.TenantMembershipId)
                .ToListAsync(ct);
            foreach (var memberId in memberIds.Where(id => id != context.MembershipId))
            {
                await notificationRepository.TryCreateHandoverAcknowledgementAsync(
                    context,
                    memberId,
                    handover.Id,
                    handover.ShiftDate,
                    handover.ShiftType,
                    ct);
            }

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, handover.Id, ct);
            return detail is null
                ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                : TeamsHandoverWriteResult.Success(handover.Id, detail);
        }, cancellationToken);

    public Task<TeamsHandoverWriteResult> AcknowledgeHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        AcknowledgeTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanAcknowledge)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Vardiya devri teslim alma yetkisi yok.");
            }

            var handover = await dbContext.TeamsShiftHandovers
                .SingleOrDefaultAsync(
                    h => h.TenantId == context.TenantId
                         && h.FacilityId == context.FacilityId
                         && h.Id == command.HandoverId,
                    ct);
            if (handover is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.");
            }

            if (await LoadChannelMembershipAsync(context, handover.ChannelId, ct) is null)
            {
                return TeamsHandoverWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (handover.AuthorTenantMembershipId == context.MembershipId)
            {
                return TeamsHandoverWriteResult.Fail(
                    TeamsFailureCodes.AccessDenied,
                    "Yazar kendi vardiya devrini teslim alamaz.");
            }

            if (!string.Equals(handover.Status, TeamsHandoverStatuses.Published, StringComparison.Ordinal))
            {
                return TeamsHandoverWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    "Yalnızca yayımlanmış vardiya devri teslim alınabilir.");
            }

            var existing = await dbContext.TeamsShiftHandoverAcknowledgements
                .SingleOrDefaultAsync(
                    a => a.TenantId == context.TenantId
                         && a.HandoverId == handover.Id
                         && a.AcknowledgedByTenantMembershipId == context.MembershipId,
                    ct);
            if (existing is not null)
            {
                var replay = await LoadVisibleDetailAsync(context, capabilities, handover.Id, ct);
                return replay is null
                    ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                    : TeamsHandoverWriteResult.Success(handover.Id, replay, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            dbContext.TeamsShiftHandoverAcknowledgements.Add(new TeamsShiftHandoverAcknowledgement
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                HandoverId = handover.Id,
                AcknowledgedByTenantMembershipId = context.MembershipId,
                AcknowledgedAtUtc = now,
            });

            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            AddAudit(context, handover.Id, handover.ChannelId, TeamsHandoverAuditEventTypes.Acknowledged, actorLabel, new
            {
                acknowledgedBy = context.MembershipId,
            }, now);

            await dbContext.SaveChangesAsync(ct);
            var detail = await LoadVisibleDetailAsync(context, capabilities, handover.Id, ct);
            return detail is null
                ? TeamsHandoverWriteResult.Fail(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı.")
                : TeamsHandoverWriteResult.Success(handover.Id, detail);
        }, cancellationToken);

    private async Task<TeamsHandoverDetailReadModel?> LoadVisibleDetailAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken)
    {
        if (!capabilities.CanRead)
        {
            return null;
        }

        var handover = await dbContext.TeamsShiftHandovers.AsNoTracking()
            .SingleOrDefaultAsync(
                h => h.TenantId == context.TenantId
                     && h.FacilityId == context.FacilityId
                     && h.Id == handoverId,
                cancellationToken);
        if (handover is null)
        {
            return null;
        }

        var membership = await LoadChannelMembershipAsync(context, handover.ChannelId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var isAuthor = handover.AuthorTenantMembershipId == context.MembershipId;
        if (!capabilities.CanManage
            && !isAuthor
            && !string.Equals(handover.Status, TeamsHandoverStatuses.Published, StringComparison.Ordinal))
        {
            return null;
        }

        var items = await dbContext.TeamsShiftHandoverItems.AsNoTracking()
            .Where(i => i.TenantId == context.TenantId && i.HandoverId == handover.Id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);
        var acknowledgements = await dbContext.TeamsShiftHandoverAcknowledgements.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId && a.HandoverId == handover.Id)
            .OrderBy(a => a.AcknowledgedAtUtc)
            .ToListAsync(cancellationToken);
        var audit = await dbContext.TeamsShiftHandoverAuditEvents.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.HandoverId == handover.Id)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var labelIds = acknowledgements.Select(a => a.AcknowledgedByTenantMembershipId)
            .Append(handover.AuthorTenantMembershipId)
            .ToList();
        var labels = await LoadMembershipLabelsAsync(context.TenantId, labelIds, cancellationToken);
        var channel = await dbContext.TeamsChannels.AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == context.TenantId && c.Id == handover.ChannelId, cancellationToken);
        var channelName = channel?.Name ?? "Kanal";

        var mappedItems = new List<TeamsHandoverItemReadModel>();
        foreach (var item in items)
        {
            var source = await ResolveItemSourceAsync(
                context, capabilities, handover.ChannelId, item.MessageId, item.AiTaskId, cancellationToken);
            mappedItems.Add(new TeamsHandoverItemReadModel(
                item.Id,
                item.ItemKind,
                item.Title,
                item.Body,
                item.MessageId,
                item.AiTaskId,
                source.SourceKind,
                source.CanOpen ? source.Label : null,
                source.CanOpen ? source.DeepLinkPath : null,
                source.CanOpen,
                source.CanOpen ? null : source.UnavailableReason));
        }

        var alreadyAcked = acknowledgements.Any(a => a.AcknowledgedByTenantMembershipId == context.MembershipId);
        var canEdit = capabilities.CanCreate && isAuthor && handover.Status == TeamsHandoverStatuses.Draft;
        var canPublish = canEdit;
        var canAcknowledge = capabilities.CanAcknowledge
            && !isAuthor
            && handover.Status == TeamsHandoverStatuses.Published
            && !alreadyAcked;

        return new TeamsHandoverDetailReadModel(
            handover.Id,
            handover.ChannelId,
            channelName,
            handover.AuthorTenantMembershipId,
            labels.GetValueOrDefault(handover.AuthorTenantMembershipId, handover.AuthorTenantMembershipId.ToString("D")),
            handover.ShiftDate,
            handover.ShiftType,
            handover.Status,
            handover.Summary,
            handover.SafetyNote,
            mappedItems,
            acknowledgements.Select(a => new TeamsHandoverAcknowledgementReadModel(
                a.AcknowledgedByTenantMembershipId,
                labels.GetValueOrDefault(a.AcknowledgedByTenantMembershipId, a.AcknowledgedByTenantMembershipId.ToString("D")),
                a.AcknowledgedAtUtc)).ToList(),
            audit.Select(e => new TeamsHandoverAuditEventReadModel(
                e.Id,
                e.EventType,
                e.ActorLabel,
                e.OccurredAtUtc)).ToList(),
            handover.CreatedAtUtc,
            handover.PublishedAtUtc,
            canEdit,
            canPublish,
            canAcknowledge,
            TeamsConstants.HandoverDisclaimer);
    }

    private async Task<string?> ValidateItemSourcesAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid channelId,
        IReadOnlyList<TeamsHandoverItemInput> items,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var source = await ResolveItemSourceAsync(
                context, capabilities, channelId, item.MessageId, item.AiTaskId, cancellationToken);
            if ((item.MessageId.HasValue || item.AiTaskId.HasValue) && !source.CanOpen)
            {
                return source.UnavailableReason ?? "Bağlı kaynağa erişim yok.";
            }
        }

        return null;
    }

    private async Task<(bool CanOpen, string? SourceKind, string? Label, string? DeepLinkPath, string? UnavailableReason)> ResolveItemSourceAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid channelId,
        Guid? messageId,
        Guid? aiTaskId,
        CancellationToken cancellationToken)
    {
        if (messageId is Guid mid && mid != Guid.Empty)
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
                return (false, "message", null, null, "Teams mesajı bulunamadı veya erişim yok.");
            }

            var preview = message.Body.Trim();
            return (
                true,
                "message",
                preview.Length <= 120 ? preview : preview[..117] + "...",
                TeamsHandoverPresentationBuilder.BuildMessageDeepLink(channelId, mid),
                null);
        }

        if (aiTaskId is Guid tid && tid != Guid.Empty)
        {
            if (!capabilities.CanReadAiTasks)
            {
                return (false, "ai-task", null, null, "AI görev bağlamına erişiminiz yok.");
            }

            var task = await dbContext.AiTasks.AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId
                         && t.FacilityId == context.FacilityId
                         && t.Id == tid,
                    cancellationToken);
            if (task is null)
            {
                return (false, "ai-task", null, null, "AI görev bulunamadı veya erişim yok.");
            }

            return (true, "ai-task", task.Title, TeamsHandoverPresentationBuilder.BuildTaskDeepLink(tid), null);
        }

        return (true, null, null, null, null);
    }

    private void AddItems(
        TeamsOperationContext context,
        Guid handoverId,
        IReadOnlyList<TeamsHandoverItemInput> items,
        DateTimeOffset now)
    {
        var order = 0;
        foreach (var item in items)
        {
            dbContext.TeamsShiftHandoverItems.Add(new TeamsShiftHandoverItem
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                HandoverId = handoverId,
                SortOrder = order++,
                ItemKind = item.ItemKind,
                Title = item.Title.Trim(),
                Body = item.Body.Trim(),
                MessageId = item.MessageId is Guid mid && mid != Guid.Empty ? mid : null,
                AiTaskId = item.AiTaskId is Guid tid && tid != Guid.Empty ? tid : null,
                CreatedAtUtc = now,
            });
        }
    }

    private async Task<TeamsChannel?> LoadHandoverChannelAsync(
        TeamsOperationContext context,
        Guid channelId,
        CancellationToken cancellationToken)
    {
        if (await LoadChannelMembershipAsync(context, channelId, cancellationToken) is null)
        {
            return null;
        }

        return await dbContext.TeamsChannels.AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                     && c.FacilityId == context.FacilityId
                     && c.Id == channelId,
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
        Guid handoverId,
        Guid channelId,
        string eventType,
        string actorLabel,
        object payload,
        DateTimeOffset now)
    {
        dbContext.TeamsShiftHandoverAuditEvents.Add(new TeamsShiftHandoverAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            HandoverId = handoverId,
            ChannelId = channelId,
            EventType = eventType,
            OccurredAtUtc = now,
            ActorTenantMembershipId = context.MembershipId.ToString("D"),
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(payload),
        });
    }
}

public sealed class UnavailableTeamsHandoverRepository : ITeamsHandoverRepository
{
    public Task<TeamsHandoverListReadModel> ListHandoversAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid? channelId,
        string? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsHandoverListReadModel([], false, false, false));

    public Task<TeamsHandoverDetailReadModel?> GetHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        Guid handoverId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsHandoverDetailReadModel?>(null);

    public Task<TeamsHandoverWriteResult> CreateHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        CreateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsHandoverWriteResult> UpdateDraftAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        UpdateTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsHandoverWriteResult> PublishHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        PublishTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsHandoverWriteResult> AcknowledgeHandoverAsync(
        TeamsOperationContext context,
        TeamsHandoverAccessCapabilities capabilities,
        AcknowledgeTeamsHandoverCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsHandoverWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));
}
