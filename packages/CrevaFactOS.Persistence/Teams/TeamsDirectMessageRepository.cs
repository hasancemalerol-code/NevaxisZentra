using System.Text.Json;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Teams;

public interface ITeamsDirectMessageRepository
{
    Task<IReadOnlyList<TeamsDirectRecipientReadModel>> ListRecipientsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken = default);

    Task<TeamsDirectConversationListReadModel> ListConversationsAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CancellationToken cancellationToken = default);

    Task<TeamsDirectConversationDetailReadModel?> GetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeamsDirectConversationWriteResult> CreateOrGetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CreateTeamsDirectConversationCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsDirectMessageWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        PostTeamsDirectMessageCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsDirectMessageWriteResult> MarkConversationReadAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        MarkTeamsDirectConversationReadCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsDirectMessageRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ITeamsDirectMessageRepository
{
    public Task<IReadOnlyList<TeamsDirectRecipientReadModel>> ListRecipientsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await (
                from facilityMembership in dbContext.FacilityMemberships.AsNoTracking()
                join tenantMembership in dbContext.TenantMemberships.AsNoTracking()
                    on facilityMembership.TenantMembershipId equals tenantMembership.Id
                join account in dbContext.SaasIdentityAccounts.AsNoTracking()
                    on tenantMembership.IdentitySubjectId equals account.IdentitySubjectId into accounts
                from account in accounts.DefaultIfEmpty()
                where facilityMembership.TenantId == context.TenantId
                      && facilityMembership.FacilityId == context.FacilityId
                      && facilityMembership.Status == FacilityMembershipStatus.Active
                      && tenantMembership.TenantId == context.TenantId
                      && tenantMembership.Status == TenantMembershipStatus.Active
                      && tenantMembership.Id != context.MembershipId
                orderby account != null ? account.DisplayName : tenantMembership.MembershipReference
                select new TeamsDirectRecipientReadModel(
                    tenantMembership.Id,
                    account != null ? account.DisplayName : tenantMembership.MembershipReference)
            ).ToListAsync(ct);

            return (IReadOnlyList<TeamsDirectRecipientReadModel>)rows;
        }, cancellationToken);

    public Task<TeamsDirectConversationListReadModel> ListConversationsAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!capabilities.CanRead)
            {
                return new TeamsDirectConversationListReadModel([], false, TeamsConstants.DirectMessageDisclaimer);
            }

            var conversationIds = await dbContext.TeamsDirectConversationMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.TenantMembershipId == context.MembershipId)
                .Select(m => m.ConversationId)
                .ToListAsync(ct);

            if (conversationIds.Count == 0)
            {
                return new TeamsDirectConversationListReadModel([], capabilities.CanWrite, TeamsConstants.DirectMessageDisclaimer);
            }

            var conversations = await dbContext.TeamsDirectConversations.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && conversationIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAtUtc)
                .Take(TeamsConstants.MaxPageSize)
                .ToListAsync(ct);

            var members = await dbContext.TeamsDirectConversationMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && conversationIds.Contains(m.ConversationId))
                .ToListAsync(ct);

            var otherParticipantIds = conversations
                .Select(conversation => ResolveOtherParticipantMembershipId(conversation, context.MembershipId))
                .Distinct()
                .ToList();

            var labels = await LoadMembershipLabelsAsync(context.TenantId, otherParticipantIds, ct);
            var readStates = await dbContext.TeamsDirectReadStates.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                            && r.TenantMembershipId == context.MembershipId
                            && conversationIds.Contains(r.ConversationId))
                .ToDictionaryAsync(r => r.ConversationId, r => r.LastReadAtUtc, ct);

            var latestMessages = await LoadLatestMessagePreviewsAsync(context.TenantId, conversationIds, ct);
            var unreadCounts = await ComputeUnreadCountsAsync(context, conversationIds, readStates, ct);

            var items = conversations.Select(conversation =>
            {
                var otherParticipantId = ResolveOtherParticipantMembershipId(conversation, context.MembershipId);
                latestMessages.TryGetValue(conversation.Id, out var latest);
                unreadCounts.TryGetValue(conversation.Id, out var unread);
                return new TeamsDirectConversationSummaryReadModel(
                    conversation.Id,
                    otherParticipantId,
                    labels.GetValueOrDefault(otherParticipantId, otherParticipantId.ToString("D")),
                    latest.Preview,
                    latest.CreatedAtUtc == default ? null : latest.CreatedAtUtc,
                    unread,
                    conversation.CreatedAtUtc);
            }).OrderByDescending(item => item.LatestMessageAtUtc ?? item.CreatedAtUtc).ToList();

            return new TeamsDirectConversationListReadModel(items, capabilities.CanWrite, TeamsConstants.DirectMessageDisclaimer);
        }, cancellationToken);

    public Task<TeamsDirectConversationDetailReadModel?> GetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadParticipantDetailAsync(context, capabilities, conversationId, page, pageSize, ct),
            cancellationToken);

    public Task<TeamsDirectConversationWriteResult> CreateOrGetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CreateTeamsDirectConversationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanWrite)
            {
                return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesaj yazma yetkisi yok.");
            }

            var validation = TeamsDirectMessagePresentationBuilder.ValidateCreateConversation(command);
            if (validation is not null)
            {
                return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            if (command.RecipientTenantMembershipId == context.MembershipId)
            {
                return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Kendinize özel mesaj başlatılamaz.");
            }

            if (!await IsEligibleFacilityMemberAsync(context.TenantId, context.FacilityId, command.RecipientTenantMembershipId, ct))
            {
                return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.NotFound, "Alıcı bulunamadı veya tesis kapsamında uygun değil.");
            }

            var (low, high) = OrderPair(context.MembershipId, command.RecipientTenantMembershipId);
            var normalizedKey = command.IdempotencyKey.Trim();
            var purpose = TeamsDirectMessagePresentationBuilder.BuildConversationIdempotencyPurpose(low, high);
            var fingerprint = TeamsIdempotencyFingerprint.ComputeDirectConversationFingerprint(context, command);

            var existingByKey = await dbContext.TeamsDirectConversations.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId && c.IdempotencyKey == normalizedKey,
                    ct);
            if (existingByKey is not null)
            {
                if (!string.Equals(existingByKey.IdempotencyPurpose, purpose, StringComparison.Ordinal))
                {
                    return TeamsDirectConversationWriteResult.Fail(
                        TeamsFailureCodes.IdempotencyConflict,
                        "Idempotency anahtarı farklı bir içerikle zaten kullanıldı.");
                }

                var replayDetail = await LoadParticipantDetailAsync(
                    context, capabilities, existingByKey.Id, 1, TeamsConstants.DefaultPageSize, ct);
                if (replayDetail is null)
                {
                    return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.");
                }

                return TeamsDirectConversationWriteResult.Success(existingByKey.Id, replayDetail, idempotentReplay: true);
            }

            var existingByPair = await dbContext.TeamsDirectConversations.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.ParticipantLowMembershipId == low
                         && c.ParticipantHighMembershipId == high,
                    ct);
            if (existingByPair is not null)
            {
                var replayDetail = await LoadParticipantDetailAsync(
                    context, capabilities, existingByPair.Id, 1, TeamsConstants.DefaultPageSize, ct);
                if (replayDetail is null)
                {
                    return TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.");
                }

                return TeamsDirectConversationWriteResult.Success(existingByPair.Id, replayDetail, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            var conversationId = Guid.NewGuid();
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            var conversation = new TeamsDirectConversation
            {
                Id = conversationId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ParticipantLowMembershipId = low,
                ParticipantHighMembershipId = high,
                InitiatedByTenantMembershipId = context.MembershipId,
                IdempotencyKey = normalizedKey,
                IdempotencyPurpose = purpose,
                CreatedAtUtc = now,
            };

            dbContext.TeamsDirectConversations.Add(conversation);
            dbContext.TeamsDirectConversationMembers.AddRange(
                new TeamsDirectConversationMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConversationId = conversationId,
                    TenantMembershipId = context.MembershipId,
                    JoinedAtUtc = now,
                },
                new TeamsDirectConversationMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConversationId = conversationId,
                    TenantMembershipId = command.RecipientTenantMembershipId,
                    JoinedAtUtc = now,
                });

            await AppendAuditEventAsync(
                context,
                conversationId,
                null,
                TeamsDirectMessageAuditEventTypes.ConversationCreated,
                context.MembershipId,
                actorLabel,
                new
                {
                    recipientTenantMembershipId = command.RecipientTenantMembershipId,
                    idempotencyFingerprint = fingerprint,
                },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await LoadParticipantDetailAsync(
                context, capabilities, conversationId, 1, TeamsConstants.DefaultPageSize, ct);
            return detail is null
                ? TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.")
                : TeamsDirectConversationWriteResult.Success(conversationId, detail);
        }, cancellationToken);

    public Task<TeamsDirectMessageWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        PostTeamsDirectMessageCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanWrite)
            {
                return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesaj yazma yetkisi yok.");
            }

            var validation = TeamsDirectMessagePresentationBuilder.ValidatePostMessage(command);
            if (validation is not null)
            {
                return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            if (!await IsParticipantAsync(context, command.ConversationId, ct))
            {
                return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.");
            }

            var fingerprint = TeamsIdempotencyFingerprint.ComputeDirectMessageFingerprint(context, command);
            var normalizedKey = command.IdempotencyKey.Trim();
            var existing = await dbContext.TeamsDirectMessages.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId && m.IdempotencyKey == normalizedKey,
                    ct);
            if (existing is not null)
            {
                return await ResolveMessageIdempotentReplayAsync(context, capabilities, existing, fingerprint, ct);
            }

            var now = DateTimeOffset.UtcNow;
            var messageId = Guid.NewGuid();
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            dbContext.TeamsDirectMessages.Add(new TeamsDirectMessage
            {
                Id = messageId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConversationId = command.ConversationId,
                AuthorTenantMembershipId = context.MembershipId,
                Body = command.Body.Trim(),
                IdempotencyKey = normalizedKey,
                IdempotencyPayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
            });

            await AppendAuditEventAsync(
                context,
                command.ConversationId,
                messageId,
                TeamsDirectMessageAuditEventTypes.MessageCreated,
                context.MembershipId,
                actorLabel,
                new { bodyLength = command.Body.Trim().Length },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await LoadParticipantDetailAsync(
                context, capabilities, command.ConversationId, 1, TeamsConstants.DefaultPageSize, ct);
            return detail is null
                ? TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.")
                : TeamsDirectMessageWriteResult.Success(messageId, detail);
        }, cancellationToken);

    public Task<TeamsDirectMessageWriteResult> MarkConversationReadAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        MarkTeamsDirectConversationReadCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (!capabilities.CanWrite)
            {
                return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesaj okundu işaretleme yetkisi yok.");
            }

            if (!await IsParticipantAsync(context, command.ConversationId, ct))
            {
                return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.");
            }

            if (command.LastReadMessageId.HasValue)
            {
                var messageValid = await dbContext.TeamsDirectMessages.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId
                             && m.ConversationId == command.ConversationId
                             && m.Id == command.LastReadMessageId.Value,
                        ct);
                if (!messageValid)
                {
                    return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Okundu işareti için geçersiz mesaj.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct);
            var readState = await dbContext.TeamsDirectReadStates
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.ConversationId == command.ConversationId
                         && r.TenantMembershipId == context.MembershipId,
                    ct);

            if (readState is null)
            {
                readState = new TeamsDirectReadState
                {
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConversationId = command.ConversationId,
                    TenantMembershipId = context.MembershipId,
                    LastReadMessageId = command.LastReadMessageId,
                    LastReadAtUtc = now,
                };
                dbContext.TeamsDirectReadStates.Add(readState);
            }
            else
            {
                readState.LastReadMessageId = command.LastReadMessageId ?? readState.LastReadMessageId;
                readState.LastReadAtUtc = now;
            }

            await AppendAuditEventAsync(
                context,
                command.ConversationId,
                command.LastReadMessageId,
                TeamsDirectMessageAuditEventTypes.ReadStateAdvanced,
                context.MembershipId,
                actorLabel,
                new { lastReadMessageId = command.LastReadMessageId },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var detail = await LoadParticipantDetailAsync(
                context, capabilities, command.ConversationId, 1, TeamsConstants.DefaultPageSize, ct);
            return detail is null
                ? TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.")
                : TeamsDirectMessageWriteResult.Success(command.ConversationId, detail, idempotentReplay: true);
        }, cancellationToken);

    private async Task<TeamsDirectConversationDetailReadModel?> LoadParticipantDetailAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!capabilities.CanRead)
        {
            return null;
        }

        if (!await IsParticipantAsync(context, conversationId, cancellationToken))
        {
            return null;
        }

        var conversation = await dbContext.TeamsDirectConversations.AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                     && c.FacilityId == context.FacilityId
                     && c.Id == conversationId,
                cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, TeamsConstants.MaxPageSize);

        var totalCount = await dbContext.TeamsDirectMessages.AsNoTracking()
            .CountAsync(
                m => m.TenantId == context.TenantId && m.ConversationId == conversationId,
                cancellationToken);

        var messages = await dbContext.TeamsDirectMessages.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        messages.Reverse();

        var otherParticipantId = ResolveOtherParticipantMembershipId(conversation, context.MembershipId);
        var authorIds = messages.Select(m => m.AuthorTenantMembershipId).Append(otherParticipantId).Distinct().ToList();
        var labels = await LoadMembershipLabelsAsync(context.TenantId, authorIds, cancellationToken);

        var readState = await dbContext.TeamsDirectReadStates.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                         && r.ConversationId == conversationId
                         && r.TenantMembershipId == context.MembershipId,
                cancellationToken);

        return new TeamsDirectConversationDetailReadModel(
            conversation.Id,
            otherParticipantId,
            labels.GetValueOrDefault(otherParticipantId, otherParticipantId.ToString("D")),
            messages.Select(message => new TeamsDirectMessageReadModel(
                message.Id,
                message.AuthorTenantMembershipId,
                labels.GetValueOrDefault(message.AuthorTenantMembershipId, message.AuthorTenantMembershipId.ToString("D")),
                message.Body,
                message.CreatedAtUtc,
                message.AuthorTenantMembershipId == context.MembershipId)).ToList(),
            totalCount,
            page,
            pageSize,
            readState?.LastReadMessageId,
            readState?.LastReadAtUtc,
            capabilities.CanWrite,
            TeamsConstants.DirectMessageDisclaimer);
    }

    private async Task<TeamsDirectMessageWriteResult> ResolveMessageIdempotentReplayAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        TeamsDirectMessage existing,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (existing.FacilityId != context.FacilityId)
        {
            return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.FacilityScopeDenied, "Tesis kapsamı reddedildi.");
        }

        if (!await IsParticipantAsync(context, existing.ConversationId, cancellationToken))
        {
            return TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.");
        }

        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return TeamsDirectMessageWriteResult.Fail(
                TeamsFailureCodes.IdempotencyLegacyUnverifiable,
                "Mevcut mesaj idempotent tekrar için doğrulanamıyor.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return TeamsDirectMessageWriteResult.Fail(
                TeamsFailureCodes.IdempotencyConflict,
                "Idempotency anahtarı farklı bir içerikle zaten kullanıldı.");
        }

        var detail = await LoadParticipantDetailAsync(
            context, capabilities, existing.ConversationId, 1, TeamsConstants.DefaultPageSize, cancellationToken);
        return detail is null
            ? TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.")
            : TeamsDirectMessageWriteResult.Success(existing.Id, detail, idempotentReplay: true);
    }

    private Task<bool> IsParticipantAsync(
        TeamsOperationContext context,
        Guid conversationId,
        CancellationToken cancellationToken) =>
        dbContext.TeamsDirectConversationMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.ConversationId == conversationId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);

    private Task<bool> IsEligibleFacilityMemberAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken) =>
        dbContext.FacilityMemberships.AsNoTracking()
            .Join(
                dbContext.TenantMemberships.AsNoTracking(),
                fm => fm.TenantMembershipId,
                tm => tm.Id,
                (fm, tm) => new { fm, tm })
            .AnyAsync(
                x => x.fm.TenantId == tenantId
                     && x.fm.FacilityId == facilityId
                     && x.fm.TenantMembershipId == tenantMembershipId
                     && x.fm.Status == FacilityMembershipStatus.Active
                     && x.tm.TenantId == tenantId
                     && x.tm.Id == tenantMembershipId
                     && x.tm.Status == TenantMembershipStatus.Active,
                cancellationToken);

    private async Task<Dictionary<Guid, (string Preview, DateTimeOffset CreatedAtUtc)>> LoadLatestMessagePreviewsAsync(
        Guid tenantId,
        List<Guid> conversationIds,
        CancellationToken cancellationToken)
    {
        if (conversationIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.TeamsDirectMessages.AsNoTracking()
            .Where(m => m.TenantId == tenantId && conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAtUtc).First())
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.ConversationId,
            row => (TeamsDirectMessagePresentationBuilder.TruncatePreview(row.Body), row.CreatedAtUtc));
    }

    private async Task<Dictionary<Guid, int>> ComputeUnreadCountsAsync(
        TeamsOperationContext context,
        List<Guid> conversationIds,
        Dictionary<Guid, DateTimeOffset> readStates,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var conversationId in conversationIds)
        {
            readStates.TryGetValue(conversationId, out var lastRead);
            var query = dbContext.TeamsDirectMessages.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                            && m.ConversationId == conversationId
                            && m.AuthorTenantMembershipId != context.MembershipId);
            if (lastRead != default)
            {
                query = query.Where(m => m.CreatedAtUtc > lastRead);
            }

            result[conversationId] = await query.CountAsync(cancellationToken);
        }

        return result;
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

        return rows.ToDictionary(row => row.Id, row => row.Label);
    }

    private async Task<string> ResolveActorLabelAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var labels = await LoadMembershipLabelsAsync(tenantId, [membershipId], cancellationToken);
        return labels.GetValueOrDefault(membershipId, membershipId.ToString("D"));
    }

    private Task AppendAuditEventAsync(
        TeamsOperationContext context,
        Guid conversationId,
        Guid? messageId,
        string eventType,
        Guid actorMembershipId,
        string actorLabel,
        object payload,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.TeamsDirectMessageAuditEvents.Add(new TeamsDirectMessageAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConversationId = conversationId,
            MessageId = messageId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorTenantMembershipId = actorMembershipId.ToString("D"),
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(payload),
        });

        return Task.CompletedTask;
    }

    private static (Guid Low, Guid High) OrderPair(Guid first, Guid second) =>
        first.CompareTo(second) <= 0 ? (first, second) : (second, first);

    private static Guid ResolveOtherParticipantMembershipId(
        TeamsDirectConversation conversation,
        Guid currentMembershipId) =>
        conversation.ParticipantLowMembershipId == currentMembershipId
            ? conversation.ParticipantHighMembershipId
            : conversation.ParticipantLowMembershipId;
}

public sealed class UnavailableTeamsDirectMessageRepository : ITeamsDirectMessageRepository
{
    public Task<IReadOnlyList<TeamsDirectRecipientReadModel>> ListRecipientsAsync(
        TeamsOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TeamsDirectRecipientReadModel>>([]);

    public Task<TeamsDirectConversationListReadModel> ListConversationsAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsDirectConversationListReadModel([], false, TeamsConstants.DirectMessageDisclaimer));

    public Task<TeamsDirectConversationDetailReadModel?> GetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsDirectConversationDetailReadModel?>(null);

    public Task<TeamsDirectConversationWriteResult> CreateOrGetConversationAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        CreateTeamsDirectConversationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsDirectConversationWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesajlar kullanılamıyor."));

    public Task<TeamsDirectMessageWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        PostTeamsDirectMessageCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesajlar kullanılamıyor."));

    public Task<TeamsDirectMessageWriteResult> MarkConversationReadAsync(
        TeamsOperationContext context,
        TeamsDirectMessageAccessCapabilities capabilities,
        MarkTeamsDirectConversationReadCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsDirectMessageWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Özel mesajlar kullanılamıyor."));
}
