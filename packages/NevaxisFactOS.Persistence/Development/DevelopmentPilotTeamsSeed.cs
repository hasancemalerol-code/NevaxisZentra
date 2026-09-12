using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.SaasIdentity;
using NevaxisFactOS.Persistence.Teams;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotTeamsSeed
{
    internal static async Task EnsureTeamsPilotDataAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.TeamsChannels.AsNoTracking()
                .AnyAsync(c => c.Id == DevelopmentPilotIds.TeamsPilotChannelUretimId, cancellationToken))
        {
            await EnsureTenantOwnerTeamsMembershipsAsync(
                dbContext, tenantId, facilityId, actorSubjectId, DevelopmentPilotIds.TeamsPilotSeedTimeUtc, cancellationToken);
            return;
        }

        var seedTime = DevelopmentPilotIds.TeamsPilotSeedTimeUtc;
        await EnsurePilotAiTaskAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTime, cancellationToken);

        var channels = new (Guid Id, string Name, string Type, Guid? AiTaskId)[]
        {
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, "Üretim", TeamsChannelTypes.Department, null),
            (DevelopmentPilotIds.TeamsPilotChannelKaliteId, "Kalite", TeamsChannelTypes.Department, null),
            (DevelopmentPilotIds.TeamsPilotChannelBakimId, "Bakım", TeamsChannelTypes.Department, null),
            (DevelopmentPilotIds.TeamsPilotChannelStokDepoId, "Stok ve Depo", TeamsChannelTypes.Department, null),
            (DevelopmentPilotIds.TeamsPilotChannelSatinAlmaId, "Satın Alma", TeamsChannelTypes.Department, null),
            (DevelopmentPilotIds.TeamsPilotChannelYonetimId, "Yönetim", TeamsChannelTypes.Operational, null),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, "Görev Bağlamı — Pilot Operasyon İncelemesi", TeamsChannelTypes.TaskContext, DevelopmentPilotIds.TeamsPilotAiTaskId),
        };

        foreach (var (id, name, type, aiTaskId) in channels)
        {
            dbContext.TeamsChannels.Add(new TeamsChannel
            {
                Id = id,
                TenantId = tenantId,
                FacilityId = facilityId,
                Name = name,
                ChannelType = type,
                AiTaskId = aiTaskId,
                CreatedAtUtc = seedTime,
                CreatedBy = actorSubjectId,
                UpdatedAtUtc = seedTime,
                UpdatedBy = actorSubjectId,
            });
        }

        var memberSpecs = new (Guid ChannelId, Guid MembershipId, string Role)[]
        {
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, DevelopmentSaasIdentityIds.TenantOwnerMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, DevelopmentPilotIds.ReviewerTenantMembershipId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, DevelopmentPilotIds.OperatorTenantMembershipId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelKaliteId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelKaliteId, DevelopmentSaasIdentityIds.TenantOwnerMembershipId, TeamsChannelRoles.Moderator),
            (DevelopmentPilotIds.TeamsPilotChannelKaliteId, DevelopmentPilotIds.ReviewerTenantMembershipId, TeamsChannelRoles.Moderator),
            (DevelopmentPilotIds.TeamsPilotChannelBakimId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelBakimId, DevelopmentPilotIds.OperatorTenantMembershipId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelStokDepoId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelSatinAlmaId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelSatinAlmaId, DevelopmentPilotIds.ReviewerTenantMembershipId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelYonetimId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelYonetimId, DevelopmentSaasIdentityIds.TenantOwnerMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelYonetimId, DevelopmentPilotIds.ReviewerTenantMembershipId, TeamsChannelRoles.Moderator),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, DevelopmentPilotIds.TenantMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, DevelopmentSaasIdentityIds.TenantOwnerMembershipId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, DevelopmentPilotIds.ReviewerTenantMembershipId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, DevelopmentPilotIds.OperatorTenantMembershipId, TeamsChannelRoles.Member),
        };

        foreach (var (channelId, membershipId, role) in memberSpecs)
        {
            dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ChannelId = channelId,
                TenantMembershipId = membershipId,
                Role = role,
                JoinedAtUtc = seedTime,
                AddedBy = actorSubjectId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePilotAiTaskAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        if (await dbContext.AiTasks.AsNoTracking()
                .AnyAsync(t => t.Id == DevelopmentPilotIds.TeamsPilotAiTaskId, cancellationToken))
        {
            return;
        }

        dbContext.AiTasks.Add(new AiTask
        {
            Id = DevelopmentPilotIds.TeamsPilotAiTaskId,
            TenantId = tenantId,
            FacilityId = facilityId,
            TaskReference = "AIT-TEAMS-PILOT-001",
            Title = "Pilot operasyon incelemesi — Teams bağlam kanalı",
            Summary = "Teams görev bağlam kanalı entegrasyonu için geliştirme pilot görevi.",
            Category = AiTaskCategories.ProductionPlanning,
            Priority = AiTaskPriorities.Medium,
            RiskLevel = AiTaskRiskLevels.Information,
            Status = AiTaskStatuses.AwaitingReview,
            DetectedIssue = "Operasyon ekibi için insan incelemesi gerektiren bağlam.",
            Reasoning = "Teams görev bağlam kanalı ve güvenli geri bağlantı doğrulaması.",
            RecommendationJson = """{"title":"İncele","description":"Operasyon ekibi ile bağlamı gözden geçir.","actionType":"review","requiresApproval":false,"reversible":true,"targetSystem":null}""",
            ConfidenceJson = """{"score":0.72,"explanation":"Geliştirme pilot verisi.","modelVersion":"teams-foundation-pilot-1"}""",
            ImpactJson = """{"summary":"Operasyonel iletişim bağlamı."}""",
            SourcesJson = "[]",
            ApprovalState = AiTaskApprovalStates.NotRequired,
            RuleSetVersion = AiTaskConstants.RuleSetVersion,
            CreatedAtUtc = seedTime,
            UpdatedAtUtc = seedTime,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        });
    }

    internal static async Task EnsureTenantOwnerTeamsMembershipsAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        var specs = new (Guid ChannelId, string Role)[]
        {
            (DevelopmentPilotIds.TeamsPilotChannelUretimId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelKaliteId, TeamsChannelRoles.Moderator),
            (DevelopmentPilotIds.TeamsPilotChannelBakimId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelStokDepoId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelSatinAlmaId, TeamsChannelRoles.Member),
            (DevelopmentPilotIds.TeamsPilotChannelYonetimId, TeamsChannelRoles.Owner),
            (DevelopmentPilotIds.TeamsPilotChannelTaskContextId, TeamsChannelRoles.Owner),
        };

        foreach (var (channelId, role) in specs)
        {
            var exists = await dbContext.TeamsChannelMembers.AsNoTracking()
                .AnyAsync(
                    m => m.TenantId == tenantId
                         && m.ChannelId == channelId
                         && m.TenantMembershipId == DevelopmentSaasIdentityIds.TenantOwnerMembershipId,
                    cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ChannelId = channelId,
                TenantMembershipId = DevelopmentSaasIdentityIds.TenantOwnerMembershipId,
                Role = role,
                JoinedAtUtc = seedTime,
                AddedBy = actorSubjectId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var operatorTaskContextExists = await dbContext.TeamsChannelMembers.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == tenantId
                     && m.ChannelId == DevelopmentPilotIds.TeamsPilotChannelTaskContextId
                     && m.TenantMembershipId == DevelopmentPilotIds.OperatorTenantMembershipId,
                cancellationToken);
        if (!operatorTaskContextExists)
        {
            dbContext.TeamsChannelMembers.Add(new TeamsChannelMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ChannelId = DevelopmentPilotIds.TeamsPilotChannelTaskContextId,
                TenantMembershipId = DevelopmentPilotIds.OperatorTenantMembershipId,
                Role = TeamsChannelRoles.Member,
                JoinedAtUtc = seedTime,
                AddedBy = actorSubjectId,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
