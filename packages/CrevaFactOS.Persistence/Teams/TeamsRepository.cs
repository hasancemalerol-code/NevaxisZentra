using System.Text.Json;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Teams;

public sealed record TeamsAccessCapabilities(
    bool CanReadAiTasks,
    bool CanReadMaintenance,
    bool CanReadQuality,
    bool CanReadMaterial,
    bool CanReadProcurement,
    bool CanReadProduction,
    bool CanReadMold,
    bool CanReadOperationalRisk);

public interface ITeamsRepository
{
    Task<TeamsChannelListReadModel> ListAccessibleChannelsAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        CancellationToken cancellationToken = default);

    Task<TeamsChannelDetailReadModel?> GetChannelDetailAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid channelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TeamsTaskContextChannelReadModel?> GetTaskContextChannelAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid aiTaskId,
        CancellationToken cancellationToken = default);

    Task<TeamsWriteResult> CreateChannelAsync(
        TeamsOperationContext context,
        CreateTeamsChannelCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsWriteResult> AddMemberAsync(
        TeamsOperationContext context,
        AddTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsWriteResult> RemoveMemberAsync(
        TeamsOperationContext context,
        RemoveTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        PostTeamsMessageCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsWriteResult> MarkChannelReadAsync(
        TeamsOperationContext context,
        MarkTeamsChannelReadCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsMessageTaskDraftWriteResult> CreateMessageTaskDraftAsync(
        TeamsOperationContext teamsContext,
        AiTaskOperationContext aiTaskContext,
        CreateMessageTaskDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<TeamsMessageSourceContextReadModel?> GetMessageSourceContextAsync(
        TeamsOperationContext context,
        Guid messageId,
        CancellationToken cancellationToken = default);
}

public sealed class TeamsRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskRepository aiTaskRepository,
    ITeamsNotificationRepository notificationRepository) : ITeamsRepository
{
    public Task<TeamsChannelListReadModel> ListAccessibleChannelsAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var memberChannelIds = await dbContext.TeamsChannelMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.TenantMembershipId == context.MembershipId)
                .Select(m => m.ChannelId)
                .ToListAsync(ct);

            if (memberChannelIds.Count == 0)
            {
                return new TeamsChannelListReadModel([], false, false);
            }

            var channels = await dbContext.TeamsChannels.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && memberChannelIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            var readStates = await dbContext.TeamsChannelReadStates.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                            && r.TenantMembershipId == context.MembershipId
                            && memberChannelIds.Contains(r.ChannelId))
                .ToDictionaryAsync(r => r.ChannelId, ct);

            var memberCounts = await dbContext.TeamsChannelMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && memberChannelIds.Contains(m.ChannelId))
                .GroupBy(m => m.ChannelId)
                .Select(g => new { ChannelId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ChannelId, x => x.Count, ct);

            var roles = await dbContext.TeamsChannelMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                            && m.TenantMembershipId == context.MembershipId
                            && memberChannelIds.Contains(m.ChannelId))
                .ToDictionaryAsync(m => m.ChannelId, m => m.Role, ct);

            var latestMessages = await LoadLatestMessagePreviewsAsync(context, memberChannelIds, ct);
            var unreadCounts = await ComputeUnreadCountsAsync(context, memberChannelIds, readStates, ct);

            var items = channels.Select(channel =>
            {
                readStates.TryGetValue(channel.Id, out var readState);
                latestMessages.TryGetValue(channel.Id, out var latest);
                unreadCounts.TryGetValue(channel.Id, out var unread);
                roles.TryGetValue(channel.Id, out var role);
                memberCounts.TryGetValue(channel.Id, out var memberCount);

                return new TeamsChannelSummaryReadModel(
                    channel.Id,
                    channel.Name,
                    channel.ChannelType,
                    channel.AiTaskId,
                    memberCount,
                    unread,
                    latest,
                    readState?.LastReadAtUtc,
                    role ?? TeamsChannelRoles.Member);
            }).ToList();

            var canManage = string.Equals(context.PermissionKey, TeamsPermissionKeys.Manage, StringComparison.Ordinal);
            var canWrite = canManage || string.Equals(context.PermissionKey, TeamsPermissionKeys.Write, StringComparison.Ordinal);

            return new TeamsChannelListReadModel(items, canManage, canWrite);
        }, cancellationToken);

    public Task<TeamsChannelDetailReadModel?> GetChannelDetailAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid channelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var membership = await LoadMembershipAsync(context, channelId, ct);
            if (membership is null)
            {
                return null;
            }

            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.Id == channelId,
                    ct);
            if (channel is null)
            {
                return null;
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, TeamsConstants.MaxPageSize);

            var totalCount = await dbContext.TeamsMessages.AsNoTracking()
                .CountAsync(
                    m => m.TenantId == context.TenantId
                         && m.ChannelId == channelId,
                    ct);

            var messages = await dbContext.TeamsMessages.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.ChannelId == channelId)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            messages.Reverse();

            var members = await LoadChannelMembersAsync(context.TenantId, channelId, ct);
            var labelMap = await LoadMembershipLabelsAsync(context.TenantId, members.Select(m => m.TenantMembershipId), ct);
            var mentionMap = await LoadMentionsForMessagesAsync(context.TenantId, messages.Select(m => m.Id), ct);
            var linkedTaskMap = await LoadLinkedTasksForMessagesAsync(
                context,
                capabilities,
                messages.Select(m => m.Id).ToList(),
                ct);
            var mappedMessages = messages
                .Select(m => MapMessageWithMentions(m, labelMap, mentionMap, capabilities, linkedTaskMap))
                .ToList();

            var readState = await dbContext.TeamsChannelReadStates.AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.ChannelId == channelId
                         && r.TenantMembershipId == context.MembershipId,
                    ct);

            TeamsSourceContextReadModel? taskContext = null;
            if (channel.AiTaskId.HasValue)
            {
                var task = await dbContext.AiTasks.AsNoTracking()
                    .SingleOrDefaultAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == channel.AiTaskId.Value,
                        ct);
                if (task is not null)
                {
                    taskContext = TeamsPresentationBuilder.BuildAiTaskSourceContext(
                        task.Id,
                        task.Title,
                        capabilities.CanReadAiTasks);
                }
            }

            return new TeamsChannelDetailReadModel(
                channel.Id,
                channel.Name,
                channel.ChannelType,
                channel.AiTaskId,
                taskContext,
                members.Select(m => new TeamsMemberReadModel(
                    m.TenantMembershipId,
                    labelMap.GetValueOrDefault(m.TenantMembershipId, m.TenantMembershipId.ToString("D")),
                    m.Role)).ToList(),
                mappedMessages,
                totalCount,
                page,
                pageSize,
                readState?.LastReadAtUtc,
                readState?.LastReadMessageId,
                membership.Role);
        }, cancellationToken);

    public Task<TeamsTaskContextChannelReadModel?> GetTaskContextChannelAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid aiTaskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!capabilities.CanReadAiTasks)
            {
                return null;
            }

            var task = await dbContext.AiTasks.AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId
                         && t.FacilityId == context.FacilityId
                         && t.Id == aiTaskId,
                    ct);
            if (task is null)
            {
                return null;
            }

            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.AiTaskId == aiTaskId
                         && c.ChannelType == TeamsChannelTypes.TaskContext,
                    ct);
            if (channel is null)
            {
                return null;
            }

            var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
                .AnyAsync(
                    m => m.TenantId == context.TenantId
                         && m.ChannelId == channel.Id
                         && m.TenantMembershipId == context.MembershipId,
                    ct);
            if (!isMember)
            {
                return null;
            }

            return new TeamsTaskContextChannelReadModel(
                channel.Id,
                channel.Name,
                TeamsPresentationBuilder.BuildTeamsChannelDeepLink(channel.Id),
                true,
                TeamsPresentationBuilder.BuildAiTaskDeepLink(task.Id),
                task.Title);
        }, cancellationToken);

    public Task<TeamsWriteResult> CreateChannelAsync(
        TeamsOperationContext context,
        CreateTeamsChannelCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validation = ValidateCreateChannel(command);
            if (validation is not null)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            if (command.ChannelType == TeamsChannelTypes.TaskContext)
            {
                if (!command.AiTaskId.HasValue)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Görev bağlam kanalı için AI görevi gerekli.");
                }

                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == command.AiTaskId.Value,
                        ct);
                if (!taskExists)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.NotFound, "Bağlanacak AI görevi bulunamadı.");
                }

                var existingTaskChannel = await dbContext.TeamsChannels.AsNoTracking()
                    .AnyAsync(
                        c => c.TenantId == context.TenantId
                             && c.FacilityId == context.FacilityId
                             && c.AiTaskId == command.AiTaskId.Value
                             && c.ChannelType == TeamsChannelTypes.TaskContext,
                        ct);
                if (existingTaskChannel)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Bu görev için zaten bir bağlam kanalı var.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.MembershipId.ToString();
            var channelId = Guid.NewGuid();
            var channel = new TeamsChannel
            {
                Id = channelId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                Name = command.Name.Trim(),
                ChannelType = command.ChannelType,
                AiTaskId = command.AiTaskId,
                CreatedAtUtc = now,
                CreatedBy = actor,
                UpdatedAtUtc = now,
                UpdatedBy = actor,
            };

            dbContext.TeamsChannels.Add(channel);
            dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ChannelId = channelId,
                TenantMembershipId = context.MembershipId,
                Role = TeamsChannelRoles.Owner,
                JoinedAtUtc = now,
                AddedBy = actor,
            });

            var memberIds = new HashSet<Guid>(command.InitialMemberMembershipIds ?? [])
            {
                context.MembershipId,
            };

            foreach (var membershipId in memberIds.Where(id => id != context.MembershipId))
            {
                var validMembership = await dbContext.TenantMemberships.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId && m.Id == membershipId,
                        ct);
                if (!validMembership)
                {
                    continue;
                }

                dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ChannelId = channelId,
                    TenantMembershipId = membershipId,
                    Role = TeamsChannelRoles.Member,
                    JoinedAtUtc = now,
                    AddedBy = actor,
                });
            }

            await AppendAuditEventAsync(
                context,
                channelId,
                null,
                TeamsAuditEventTypes.ChannelCreated,
                actor,
                await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct),
                new { name = channel.Name, channelType = channel.ChannelType, aiTaskId = channel.AiTaskId },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);
            return TeamsWriteResult.Success(channelId);
        }, cancellationToken);

    public Task<TeamsWriteResult> AddMemberAsync(
        TeamsOperationContext context,
        AddTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var actorMembership = await LoadMembershipAsync(context, command.ChannelId, ct);
            if (actorMembership is null
                || !IsManagerRole(actorMembership.Role))
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanal üyeliği yönetim yetkisi gerekli.");
            }

            if (!TeamsChannelRoles.Member.Equals(command.Role, StringComparison.Ordinal)
                && !TeamsChannelRoles.Moderator.Equals(command.Role, StringComparison.Ordinal))
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Geçersiz üye rolü.");
            }

            var membershipExists = await dbContext.TenantMemberships.AsNoTracking()
                .AnyAsync(
                    m => m.TenantId == context.TenantId && m.Id == command.TenantMembershipId,
                    ct);
            if (!membershipExists)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.NotFound, "Üye bulunamadı.");
            }

            var existing = await dbContext.TeamsChannelMembers
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                         && m.ChannelId == command.ChannelId
                         && m.TenantMembershipId == command.TenantMembershipId,
                    ct);
            if (existing is not null)
            {
                return TeamsWriteResult.Success(existing.Id, idempotentReplay: true);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.MembershipId.ToString();
            var memberId = Guid.NewGuid();
            dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
            {
                Id = memberId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ChannelId = command.ChannelId,
                TenantMembershipId = command.TenantMembershipId,
                Role = command.Role,
                JoinedAtUtc = now,
                AddedBy = actor,
            });

            await AppendAuditEventAsync(
                context,
                command.ChannelId,
                null,
                TeamsAuditEventTypes.MemberAdded,
                actor,
                await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct),
                new { tenantMembershipId = command.TenantMembershipId, role = command.Role },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);
            return TeamsWriteResult.Success(memberId);
        }, cancellationToken);

    public Task<TeamsWriteResult> RemoveMemberAsync(
        TeamsOperationContext context,
        RemoveTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var actorMembership = await LoadMembershipAsync(context, command.ChannelId, ct);
            if (actorMembership is null
                || !IsManagerRole(actorMembership.Role))
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanal üyeliği yönetim yetkisi gerekli.");
            }

            var member = await dbContext.TeamsChannelMembers
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                         && m.ChannelId == command.ChannelId
                         && m.TenantMembershipId == command.TenantMembershipId,
                    ct);
            if (member is null)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.NotFound, "Kanal üyesi bulunamadı.");
            }

            if (member.Role == TeamsChannelRoles.Owner)
            {
                var ownerCount = await dbContext.TeamsChannelMembers.AsNoTracking()
                    .CountAsync(
                        m => m.TenantId == context.TenantId
                             && m.ChannelId == command.ChannelId
                             && m.Role == TeamsChannelRoles.Owner,
                        ct);
                if (ownerCount <= 1)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Kanalın son sahibi kaldırılamaz.");
                }
            }

            dbContext.TeamsChannelMembers.Remove(member);

            var now = DateTimeOffset.UtcNow;
            var actor = context.MembershipId.ToString();
            await AppendAuditEventAsync(
                context,
                command.ChannelId,
                null,
                TeamsAuditEventTypes.MemberRemoved,
                actor,
                await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct),
                new { tenantMembershipId = command.TenantMembershipId },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);
            return TeamsWriteResult.Success(member.Id);
        }, cancellationToken);

    public Task<TeamsWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        PostTeamsMessageCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validation = ValidatePostMessage(command);
            if (validation is not null)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, validation);
            }

            var membership = await LoadMembershipAsync(context, command.ChannelId, ct);
            if (membership is null)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            var fingerprint = TeamsIdempotencyFingerprint.ComputeMessageFingerprint(context, command);
            var normalizedKey = command.IdempotencyKey.Trim();
            var existing = await dbContext.TeamsMessages.AsNoTracking()
                .Include(m => m.Mentions)
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId && m.IdempotencyKey == normalizedKey,
                    ct);

            if (existing is not null)
            {
                return await ResolveIdempotentReplayAsync(context, capabilities, existing, fingerprint, ct);
            }

            if (command.AiTaskId.HasValue)
            {
                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == command.AiTaskId.Value,
                        ct);
                if (!taskExists)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.NotFound, "Bağlanacak AI görevi bulunamadı.");
                }
            }

            var channelMemberIds = await dbContext.TeamsChannelMembers.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.ChannelId == command.ChannelId)
                .Select(m => m.TenantMembershipId)
                .ToListAsync(ct);

            var mentionIds = (command.MentionedMembershipIds ?? [])
                .Distinct()
                .Where(id => channelMemberIds.Contains(id))
                .ToList();

            if ((command.MentionedMembershipIds ?? []).Distinct().Count() != mentionIds.Count)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Yalnızca kanal üyeleri etiketlenebilir.");
            }

            var now = DateTimeOffset.UtcNow;
            var messageId = Guid.NewGuid();
            var message = new TeamsMessage
            {
                Id = messageId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ChannelId = command.ChannelId,
                AuthorTenantMembershipId = context.MembershipId,
                Body = command.Body.Trim(),
                AiTaskId = command.AiTaskId,
                SourceModule = string.IsNullOrWhiteSpace(command.SourceModule) ? null : command.SourceModule.Trim(),
                SourceEntityId = string.IsNullOrWhiteSpace(command.SourceEntityId) ? null : command.SourceEntityId.Trim(),
                IdempotencyKey = normalizedKey,
                IdempotencyPayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
            };

            foreach (var mentionId in mentionIds)
            {
                message.Mentions.Add(new TeamsMessageMention
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    MessageId = messageId,
                    MentionedTenantMembershipId = mentionId,
                });
            }

            dbContext.TeamsMessages.Add(message);

            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleAsync(c => c.TenantId == context.TenantId && c.Id == command.ChannelId, ct);
            foreach (var mentionId in mentionIds)
            {
                await notificationRepository.TryCreateMentionAsync(
                    context,
                    mentionId,
                    command.ChannelId,
                    channel.Name,
                    messageId,
                    message.Body,
                    ct);
            }

            var actor = context.MembershipId.ToString();
            await AppendAuditEventAsync(
                context,
                command.ChannelId,
                messageId,
                TeamsAuditEventTypes.MessageCreated,
                actor,
                await ResolveActorLabelAsync(context.TenantId, context.MembershipId, ct),
                new { bodyLength = message.Body.Length, mentionCount = mentionIds.Count },
                now,
                ct);

            await dbContext.SaveChangesAsync(ct);

            var labelMap = await LoadMembershipLabelsAsync(
                context.TenantId,
                [context.MembershipId, .. mentionIds],
                ct);
            var mapped = MapMessageSync(message, labelMap, capabilities);
            return TeamsWriteResult.Success(messageId, message: mapped);
        }, cancellationToken);

    public Task<TeamsWriteResult> MarkChannelReadAsync(
        TeamsOperationContext context,
        MarkTeamsChannelReadCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var membership = await LoadMembershipAsync(context, command.ChannelId, ct);
            if (membership is null)
            {
                return TeamsWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
            }

            if (command.LastReadMessageId.HasValue)
            {
                var messageValid = await dbContext.TeamsMessages.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId
                             && m.ChannelId == command.ChannelId
                             && m.Id == command.LastReadMessageId.Value,
                        ct);
                if (!messageValid)
                {
                    return TeamsWriteResult.Fail(TeamsFailureCodes.ValidationFailed, "Okundu işareti için geçersiz mesaj.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var readState = await dbContext.TeamsChannelReadStates
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                         && r.ChannelId == command.ChannelId
                         && r.TenantMembershipId == context.MembershipId,
                    ct);

            if (readState is null)
            {
                readState = new TeamsChannelReadState
                {
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ChannelId = command.ChannelId,
                    TenantMembershipId = context.MembershipId,
                    LastReadMessageId = command.LastReadMessageId,
                    LastReadAtUtc = now,
                };
                dbContext.TeamsChannelReadStates.Add(readState);
            }
            else
            {
                readState.LastReadMessageId = command.LastReadMessageId ?? readState.LastReadMessageId;
                readState.LastReadAtUtc = now;
            }

            await dbContext.SaveChangesAsync(ct);
            return TeamsWriteResult.Success(command.ChannelId);
        }, cancellationToken);

    private async Task<TeamsWriteResult> ResolveIdempotentReplayAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        TeamsMessage existing,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (existing.FacilityId != context.FacilityId)
        {
            return TeamsWriteResult.Fail(TeamsFailureCodes.FacilityScopeDenied, "Tesis kapsamı reddedildi.");
        }

        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return TeamsWriteResult.Fail(
                TeamsFailureCodes.IdempotencyLegacyUnverifiable,
                "Mevcut mesaj idempotent tekrar için doğrulanamıyor.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return TeamsWriteResult.Fail(
                TeamsFailureCodes.IdempotencyConflict,
                "Idempotency anahtarı farklı bir içerikle zaten kullanıldı.");
        }

        var isMember = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == context.TenantId
                     && m.ChannelId == existing.ChannelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);
        if (!isMember)
        {
            return TeamsWriteResult.Fail(TeamsFailureCodes.ChannelAccessDenied, "Kanala erişim yok.");
        }

        var mentionIds = existing.Mentions.Select(m => m.MentionedTenantMembershipId).ToList();
        var labelMap = await LoadMembershipLabelsAsync(
            context.TenantId,
            [existing.AuthorTenantMembershipId, .. mentionIds],
            cancellationToken);
        var mapped = MapMessageSync(existing, labelMap, capabilities);
        return TeamsWriteResult.Success(existing.Id, idempotentReplay: true, message: mapped);
    }

    private static TeamsMessageReadModel MapMessageSync(
        TeamsMessage message,
        IReadOnlyDictionary<Guid, string> labelMap,
        TeamsAccessCapabilities capabilities,
        IReadOnlyDictionary<Guid, TeamsMessageLinkedTaskReadModel>? linkedTasks = null)
    {
        TeamsSourceContextReadModel? sourceContext = null;
        if (message.AiTaskId.HasValue)
        {
            sourceContext = TeamsPresentationBuilder.BuildAiTaskSourceContext(
                message.AiTaskId.Value,
                null,
                capabilities.CanReadAiTasks);
        }
        else if (!string.IsNullOrWhiteSpace(message.SourceModule)
                 && !string.IsNullOrWhiteSpace(message.SourceEntityId))
        {
            sourceContext = TeamsPresentationBuilder.BuildOperationalSourceContext(
                message.SourceModule,
                message.SourceEntityId,
                CanOpenOperationalSource(message.SourceModule, capabilities));
        }

        TeamsMessageLinkedTaskReadModel? linkedTask = null;
        if (linkedTasks is not null && linkedTasks.TryGetValue(message.Id, out var resolvedLinkedTask))
        {
            linkedTask = resolvedLinkedTask;
        }

        return new TeamsMessageReadModel(
            message.Id,
            message.ChannelId,
            message.AuthorTenantMembershipId,
            labelMap.GetValueOrDefault(message.AuthorTenantMembershipId, message.AuthorTenantMembershipId.ToString("D")),
            message.Body,
            sourceContext,
            message.Mentions.Select(m => new TeamsMessageMentionReadModel(
                m.MentionedTenantMembershipId,
                labelMap.GetValueOrDefault(m.MentionedTenantMembershipId, m.MentionedTenantMembershipId.ToString("D")))).ToList(),
            message.CreatedAtUtc,
            linkedTask);
    }

    private static TeamsMessageReadModel MapMessageWithMentions(
        TeamsMessage message,
        IReadOnlyDictionary<Guid, string> labelMap,
        Dictionary<Guid, List<TeamsMessageMention>> mentionMap,
        TeamsAccessCapabilities capabilities,
        IReadOnlyDictionary<Guid, TeamsMessageLinkedTaskReadModel>? linkedTasks = null)
    {
        if (mentionMap.TryGetValue(message.Id, out var mentions))
        {
            message.Mentions = mentions;
        }

        return MapMessageSync(message, labelMap, capabilities, linkedTasks);
    }

    private static bool CanOpenOperationalSource(string sourceModule, TeamsAccessCapabilities capabilities) =>
        sourceModule switch
        {
            "maintenance" => capabilities.CanReadMaintenance,
            "quality" => capabilities.CanReadQuality,
            "material" => capabilities.CanReadMaterial,
            "procurement" => capabilities.CanReadProcurement,
            "production" or "production-planning" => capabilities.CanReadProduction,
            "mold" => capabilities.CanReadMold,
            "operational-risk" => capabilities.CanReadOperationalRisk,
            _ => false,
        };

    private async Task<TeamsChannelMember?> LoadMembershipAsync(
        TeamsOperationContext context,
        Guid channelId,
        CancellationToken cancellationToken) =>
        await dbContext.TeamsChannelMembers.AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                     && m.FacilityId == context.FacilityId
                     && m.ChannelId == channelId
                     && m.TenantMembershipId == context.MembershipId,
                cancellationToken);

    private async Task<List<TeamsChannelMember>> LoadChannelMembersAsync(
        Guid tenantId,
        Guid channelId,
        CancellationToken cancellationToken) =>
        await dbContext.TeamsChannelMembers.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.ChannelId == channelId)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAtUtc)
            .ToListAsync(cancellationToken);

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

    private async Task<Dictionary<Guid, List<TeamsMessageMention>>> LoadMentionsForMessagesAsync(
        Guid tenantId,
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken)
    {
        var ids = messageIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, List<TeamsMessageMention>>();
        }

        var mentions = await dbContext.TeamsMessageMentions.AsNoTracking()
            .Where(m => m.TenantId == tenantId && ids.Contains(m.MessageId))
            .ToListAsync(cancellationToken);

        return mentions.GroupBy(m => m.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, TeamsLatestMessagePreviewReadModel>> LoadLatestMessagePreviewsAsync(
        TeamsOperationContext context,
        List<Guid> channelIds,
        CancellationToken cancellationToken)
    {
        if (channelIds.Count == 0)
        {
            return new Dictionary<Guid, TeamsLatestMessagePreviewReadModel>();
        }

        var messages = await dbContext.TeamsMessages.AsNoTracking()
            .Where(m => m.TenantId == context.TenantId && channelIds.Contains(m.ChannelId))
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var latestByChannel = messages
            .GroupBy(m => m.ChannelId)
            .Select(g => g.First())
            .ToList();

        var authorIds = latestByChannel.Select(m => m.AuthorTenantMembershipId).Distinct();
        var labelMap = await LoadMembershipLabelsAsync(context.TenantId, authorIds, cancellationToken);

        return latestByChannel.ToDictionary(
            m => m.ChannelId,
            m => new TeamsLatestMessagePreviewReadModel(
                m.Id,
                labelMap.GetValueOrDefault(m.AuthorTenantMembershipId, "—"),
                TruncatePreview(m.Body),
                m.CreatedAtUtc));
    }

    private async Task<Dictionary<Guid, int>> ComputeUnreadCountsAsync(
        TeamsOperationContext context,
        List<Guid> channelIds,
        Dictionary<Guid, TeamsChannelReadState> readStates,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var channelId in channelIds)
        {
            readStates.TryGetValue(channelId, out var readState);
            var query = dbContext.TeamsMessages.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.ChannelId == channelId);

            if (readState?.LastReadAtUtc is not null)
            {
                query = query.Where(m => m.CreatedAtUtc > readState.LastReadAtUtc);
            }

            result[channelId] = await query.CountAsync(cancellationToken);
        }

        return result;
    }

    private async Task AppendAuditEventAsync(
        TeamsOperationContext context,
        Guid? channelId,
        Guid? messageId,
        string eventType,
        string actorMembershipId,
        string actorLabel,
        object payload,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.TeamsMessageAuditEvents.Add(new TeamsMessageAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ChannelId = channelId,
            MessageId = messageId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorTenantMembershipId = actorMembershipId,
            ActorLabel = actorLabel,
            PayloadJson = JsonSerializer.Serialize(payload),
        });

        await Task.CompletedTask;
    }

    private async Task<string> ResolveActorLabelAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var labels = await LoadMembershipLabelsAsync(tenantId, [membershipId], cancellationToken);
        return labels.GetValueOrDefault(membershipId, membershipId.ToString("D"));
    }

    private static bool IsManagerRole(string role) =>
        role is TeamsChannelRoles.Owner or TeamsChannelRoles.Moderator;

    private static string? ValidateCreateChannel(CreateTeamsChannelCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Kanal adı gerekli.";
        }

        if (command.Name.Trim().Length > TeamsConstants.MaxChannelNameLength)
        {
            return "Kanal adı çok uzun.";
        }

        if (command.ChannelType is not (TeamsChannelTypes.Department
            or TeamsChannelTypes.Operational
            or TeamsChannelTypes.TaskContext))
        {
            return "Geçersiz kanal türü.";
        }

        return null;
    }

    private static string? ValidatePostMessage(PostTeamsMessageCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency anahtarı gerekli.";
        }

        if (string.IsNullOrWhiteSpace(command.Body))
        {
            return "Mesaj gövdesi gerekli.";
        }

        if (command.Body.Trim().Length > TeamsConstants.MaxMessageBodyLength)
        {
            return "Mesaj gövdesi çok uzun.";
        }

        return null;
    }

    private static string TruncatePreview(string body)
    {
        var trimmed = body.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
    }

    public Task<TeamsMessageTaskDraftWriteResult> CreateMessageTaskDraftAsync(
        TeamsOperationContext teamsContext,
        AiTaskOperationContext aiTaskContext,
        CreateMessageTaskDraftCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validationError = TeamsMessageTaskPresentationBuilder.ValidateDraftInput(
                command.Title,
                command.Category,
                command.Priority);
            if (validationError is not null)
            {
                return TeamsMessageTaskDraftWriteResult.Fail(
                    TeamsFailureCodes.ValidationFailed,
                    validationError);
            }

            var message = await dbContext.TeamsMessages.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == teamsContext.TenantId
                         && m.FacilityId == teamsContext.FacilityId
                         && m.Id == command.MessageId,
                    ct);
            if (message is null)
            {
                return TeamsMessageTaskDraftWriteResult.Fail(
                    TeamsFailureCodes.NotFound,
                    "Mesaj bulunamadı.");
            }

            var membership = await LoadMembershipAsync(teamsContext, message.ChannelId, ct);
            if (membership is null)
            {
                return TeamsMessageTaskDraftWriteResult.Fail(
                    TeamsFailureCodes.ChannelAccessDenied,
                    "Kanala erişim yok.");
            }

            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == teamsContext.TenantId
                         && c.FacilityId == teamsContext.FacilityId
                         && c.Id == message.ChannelId,
                    ct);
            if (channel is null)
            {
                return TeamsMessageTaskDraftWriteResult.Fail(
                    TeamsFailureCodes.NotFound,
                    "Kanal bulunamadı.");
            }

            var existingLink = await dbContext.TeamsMessageTaskLinks.AsNoTracking()
                .SingleOrDefaultAsync(
                    l => l.TenantId == teamsContext.TenantId
                         && l.MessageId == message.Id
                         && l.LinkPurpose == TeamsMessageTaskLinkPurposes.HumanReviewDraft,
                    ct);
            if (existingLink is not null)
            {
                var linkedTask = await dbContext.AiTasks.AsNoTracking()
                    .SingleOrDefaultAsync(
                        t => t.TenantId == teamsContext.TenantId
                             && t.FacilityId == teamsContext.FacilityId
                             && t.Id == existingLink.AiTaskId,
                        ct);
                if (linkedTask is null)
                {
                    return TeamsMessageTaskDraftWriteResult.Fail(
                        TeamsFailureCodes.NotFound,
                        "Bağlı görev bulunamadı.");
                }

                return TeamsMessageTaskDraftWriteResult.Success(
                    linkedTask.Id,
                    linkedTask.TaskReference,
                    linkedTask.Status,
                    idempotentReplay: true);
            }

            var sourceLinkedCommand = TeamsMessageTaskPresentationBuilder.BuildSourceLinkedTaskCommand(
                message,
                channel.Name,
                command);
            var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
                aiTaskContext,
                sourceLinkedCommand,
                ct);
            if (!taskResult.Succeeded)
            {
                var failureCode = taskResult.FailureCode switch
                {
                    AiTaskFailureCodes.IdempotencyConflict => TeamsFailureCodes.IdempotencyConflict,
                    AiTaskFailureCodes.ValidationFailed => TeamsFailureCodes.ValidationFailed,
                    AiTaskFailureCodes.FacilityScopeDenied => TeamsFailureCodes.FacilityScopeDenied,
                    _ => TeamsFailureCodes.AccessDenied,
                };

                var failureReason = taskResult.FailureReason switch
                {
                    "Title is required." => "Görev başlığı zorunludur.",
                    "A valid category is required." => "Geçerli bir görev kategorisi seçin.",
                    "A valid priority is required." => "Geçerli bir öncelik seçin.",
                    _ => taskResult.FailureReason ?? "Görev taslağı oluşturulamadı.",
                };

                return TeamsMessageTaskDraftWriteResult.Fail(failureCode, failureReason);
            }

            var createdTask = await dbContext.AiTasks.AsNoTracking()
                .SingleAsync(
                    t => t.TenantId == teamsContext.TenantId
                         && t.FacilityId == teamsContext.FacilityId
                         && t.Id == taskResult.EntityId!.Value,
                    ct);

            if (taskResult.IdempotentReplay)
            {
                var replayLink = await dbContext.TeamsMessageTaskLinks.AsNoTracking()
                    .SingleOrDefaultAsync(
                        l => l.TenantId == teamsContext.TenantId
                             && l.MessageId == message.Id
                             && l.LinkPurpose == TeamsMessageTaskLinkPurposes.HumanReviewDraft,
                        ct);
                if (replayLink is not null)
                {
                    return TeamsMessageTaskDraftWriteResult.Success(
                        createdTask.Id,
                        createdTask.TaskReference,
                        createdTask.Status,
                        idempotentReplay: true);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var actorLabel = teamsContext.MembershipId.ToString();
            var link = new TeamsMessageTaskLink
            {
                Id = Guid.NewGuid(),
                TenantId = teamsContext.TenantId,
                FacilityId = teamsContext.FacilityId,
                ChannelId = message.ChannelId,
                MessageId = message.Id,
                AiTaskId = createdTask.Id,
                LinkPurpose = TeamsMessageTaskLinkPurposes.HumanReviewDraft,
                IdempotencyKey = sourceLinkedCommand.IdempotencyKey,
                SourceContextReference = TeamsMessageTaskPresentationBuilder.BuildSourceContextReference(
                    message.ChannelId,
                    message.Id),
                CreatedByTenantMembershipId = teamsContext.MembershipId,
                CreatedAtUtc = now,
            };

            dbContext.TeamsMessageTaskLinks.Add(link);
            dbContext.TeamsMessageAuditEvents.Add(new TeamsMessageAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = teamsContext.TenantId,
                FacilityId = teamsContext.FacilityId,
                ChannelId = message.ChannelId,
                MessageId = message.Id,
                EventType = TeamsAuditEventTypes.MessageTaskDraftLinked,
                OccurredAtUtc = now,
                ActorTenantMembershipId = teamsContext.MembershipId.ToString(),
                ActorLabel = actorLabel,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    linkPurpose = TeamsMessageTaskLinkPurposes.HumanReviewDraft,
                    aiTaskId = createdTask.Id,
                    idempotencyKey = sourceLinkedCommand.IdempotencyKey,
                    sourceContextReference = link.SourceContextReference,
                }),
            });

            if (!string.IsNullOrWhiteSpace(createdTask.AssigneeJson))
            {
                var channelMemberIds = await dbContext.TeamsChannelMembers.AsNoTracking()
                    .Where(m => m.TenantId == teamsContext.TenantId && m.ChannelId == message.ChannelId)
                    .Select(m => m.TenantMembershipId)
                    .ToListAsync(ct);
                foreach (var memberId in channelMemberIds.Where(id => id != teamsContext.MembershipId))
                {
                    if (createdTask.AssigneeJson.Contains(memberId.ToString("D"), StringComparison.OrdinalIgnoreCase))
                    {
                        await notificationRepository.TryCreateTaskLinkAsync(
                            teamsContext,
                            memberId,
                            link.Id,
                            createdTask.Id,
                            createdTask.TaskReference,
                            ct);
                    }
                }
            }

            await dbContext.SaveChangesAsync(ct);

            return TeamsMessageTaskDraftWriteResult.Success(
                createdTask.Id,
                createdTask.TaskReference,
                createdTask.Status,
                idempotentReplay: taskResult.IdempotentReplay);
        }, cancellationToken);

    public Task<TeamsMessageSourceContextReadModel?> GetMessageSourceContextAsync(
        TeamsOperationContext context,
        Guid messageId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var message = await dbContext.TeamsMessages.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                         && m.FacilityId == context.FacilityId
                         && m.Id == messageId,
                    ct);
            if (message is null)
            {
                return null;
            }

            var membership = await LoadMembershipAsync(context, message.ChannelId, ct);
            var channel = await dbContext.TeamsChannels.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.Id == message.ChannelId,
                    ct);
            if (channel is null)
            {
                return null;
            }

            var deepLink = TeamsMessageTaskPresentationBuilder.BuildMessageDeepLink(
                message.ChannelId,
                message.Id);

            if (membership is null)
            {
                return new TeamsMessageSourceContextReadModel(
                    message.Id,
                    message.ChannelId,
                    channel.Name,
                    TruncatePreview(message.Body),
                    deepLink,
                    CanOpen: false,
                    UnavailableReason: "Teams mesaj bağlamına erişiminiz yok.");
            }

            return new TeamsMessageSourceContextReadModel(
                message.Id,
                message.ChannelId,
                channel.Name,
                TruncatePreview(message.Body),
                deepLink,
                CanOpen: true,
                UnavailableReason: null);
        }, cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, TeamsMessageLinkedTaskReadModel>> LoadLinkedTasksForMessagesAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        List<Guid> messageIds,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return new Dictionary<Guid, TeamsMessageLinkedTaskReadModel>();
        }

        var links = await dbContext.TeamsMessageTaskLinks.AsNoTracking()
            .Where(l => l.TenantId == context.TenantId && messageIds.Contains(l.MessageId))
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return new Dictionary<Guid, TeamsMessageLinkedTaskReadModel>();
        }

        var taskIds = links.Select(l => l.AiTaskId).Distinct().ToList();
        var tasks = await dbContext.AiTasks.AsNoTracking()
            .Where(t => t.TenantId == context.TenantId
                        && t.FacilityId == context.FacilityId
                        && taskIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var result = new Dictionary<Guid, TeamsMessageLinkedTaskReadModel>();
        foreach (var link in links)
        {
            if (!tasks.TryGetValue(link.AiTaskId, out var task))
            {
                continue;
            }

            result[link.MessageId] = new TeamsMessageLinkedTaskReadModel(
                task.Id,
                task.TaskReference,
                task.Status,
                TeamsPresentationBuilder.BuildAiTaskDeepLink(task.Id),
                capabilities.CanReadAiTasks);
        }

        return result;
    }
}

public sealed class UnavailableTeamsRepository : ITeamsRepository
{
    public Task<TeamsChannelListReadModel> ListAccessibleChannelsAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TeamsChannelListReadModel([], false, false));

    public Task<TeamsChannelDetailReadModel?> GetChannelDetailAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid channelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsChannelDetailReadModel?>(null);

    public Task<TeamsTaskContextChannelReadModel?> GetTaskContextChannelAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        Guid aiTaskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsTaskContextChannelReadModel?>(null);

    public Task<TeamsWriteResult> CreateChannelAsync(
        TeamsOperationContext context,
        CreateTeamsChannelCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsWriteResult> AddMemberAsync(
        TeamsOperationContext context,
        AddTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsWriteResult> RemoveMemberAsync(
        TeamsOperationContext context,
        RemoveTeamsChannelMemberCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsWriteResult> PostMessageAsync(
        TeamsOperationContext context,
        TeamsAccessCapabilities capabilities,
        PostTeamsMessageCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsWriteResult> MarkChannelReadAsync(
        TeamsOperationContext context,
        MarkTeamsChannelReadCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsWriteResult.Fail(TeamsFailureCodes.AccessDenied, "Teams unavailable."));

    public Task<TeamsMessageTaskDraftWriteResult> CreateMessageTaskDraftAsync(
        TeamsOperationContext teamsContext,
        AiTaskOperationContext aiTaskContext,
        CreateMessageTaskDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamsMessageTaskDraftWriteResult.Fail(
            TeamsFailureCodes.AccessDenied,
            "Teams unavailable."));

    public Task<TeamsMessageSourceContextReadModel?> GetMessageSourceContextAsync(
        TeamsOperationContext context,
        Guid messageId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamsMessageSourceContextReadModel?>(null);
}
