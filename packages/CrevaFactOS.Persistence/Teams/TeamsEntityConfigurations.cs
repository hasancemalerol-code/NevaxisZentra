using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Teams;

internal sealed class TeamsChannelEntityConfiguration : IEntityTypeConfiguration<TeamsChannel>
{
    public void Configure(EntityTypeBuilder<TeamsChannel> entity)
    {
        entity.ToTable("teams_channels");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(TeamsConstants.MaxChannelNameLength).IsRequired();
        entity.Property(c => c.ChannelType).HasColumnName("channel_type").HasMaxLength(30).IsRequired();
        entity.Property(c => c.AiTaskId).HasColumnName("ai_task_id");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(c => c.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.TenantId, c.FacilityId })
            .HasDatabaseName("ix_teams_channels_tenant_facility");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.ChannelType })
            .HasDatabaseName("ix_teams_channels_tenant_facility_type");
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.AiTaskId })
            .HasDatabaseName("ix_teams_channels_tenant_facility_task")
            .HasFilter("ai_task_id IS NOT NULL");
    }
}

internal sealed class TeamsChannelMemberEntityConfiguration : IEntityTypeConfiguration<TeamsChannelMember>
{
    public void Configure(EntityTypeBuilder<TeamsChannelMember> entity)
    {
        entity.ToTable("teams_channel_members");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.ChannelId).HasColumnName("channel_id");
        entity.Property(m => m.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(m => m.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        entity.Property(m => m.JoinedAtUtc).HasColumnName("joined_at_utc");
        entity.Property(m => m.AddedBy).HasColumnName("added_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(m => m.Channel).WithMany(c => c.Members)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => new { m.TenantId, m.ChannelId, m.TenantMembershipId })
            .HasDatabaseName("ux_teams_channel_members_channel_membership")
            .IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.TenantMembershipId })
            .HasDatabaseName("ix_teams_channel_members_tenant_membership");
    }
}

internal sealed class TeamsMessageEntityConfiguration : IEntityTypeConfiguration<TeamsMessage>
{
    public void Configure(EntityTypeBuilder<TeamsMessage> entity)
    {
        entity.ToTable("teams_messages");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.ChannelId).HasColumnName("channel_id");
        entity.Property(m => m.AuthorTenantMembershipId).HasColumnName("author_tenant_membership_id");
        entity.Property(m => m.Body).HasColumnName("body").HasMaxLength(TeamsConstants.MaxMessageBodyLength).IsRequired();
        entity.Property(m => m.AiTaskId).HasColumnName("ai_task_id");
        entity.Property(m => m.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(m => m.SourceEntityId).HasColumnName("source_entity_id").HasMaxLength(200);
        entity.Property(m => m.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(m => m.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(m => m.Channel).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => new { m.TenantId, m.IdempotencyKey })
            .HasDatabaseName("ux_teams_messages_tenant_idempotency")
            .IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.ChannelId, m.CreatedAtUtc })
            .HasDatabaseName("ix_teams_messages_tenant_facility_channel_created");
    }
}

internal sealed class TeamsMessageMentionEntityConfiguration : IEntityTypeConfiguration<TeamsMessageMention>
{
    public void Configure(EntityTypeBuilder<TeamsMessageMention> entity)
    {
        entity.ToTable("teams_message_mentions");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.MessageId).HasColumnName("message_id");
        entity.Property(m => m.MentionedTenantMembershipId).HasColumnName("mentioned_tenant_membership_id");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(m => m.Message).WithMany(msg => msg.Mentions)
            .HasForeignKey(m => m.MessageId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => new { m.TenantId, m.MessageId, m.MentionedTenantMembershipId })
            .HasDatabaseName("ux_teams_message_mentions_message_membership")
            .IsUnique();
    }
}

internal sealed class TeamsChannelReadStateEntityConfiguration : IEntityTypeConfiguration<TeamsChannelReadState>
{
    public void Configure(EntityTypeBuilder<TeamsChannelReadState> entity)
    {
        entity.ToTable("teams_channel_read_states");
        entity.HasKey(r => new { r.TenantId, r.ChannelId, r.TenantMembershipId });
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ChannelId).HasColumnName("channel_id");
        entity.Property(r => r.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(r => r.LastReadMessageId).HasColumnName("last_read_message_id");
        entity.Property(r => r.LastReadAtUtc).HasColumnName("last_read_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TeamsMessageAuditEventEntityConfiguration : IEntityTypeConfiguration<TeamsMessageAuditEvent>
{
    public void Configure(EntityTypeBuilder<TeamsMessageAuditEvent> entity)
    {
        entity.ToTable("teams_message_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ChannelId).HasColumnName("channel_id");
        entity.Property(e => e.MessageId).HasColumnName("message_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ActorTenantMembershipId).HasColumnName("actor_tenant_membership_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.ActorLabel).HasColumnName("actor_label").HasMaxLength(200).IsRequired();
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ChannelId, e.OccurredAtUtc })
            .HasDatabaseName("ix_teams_message_audit_events_tenant_facility_channel");
    }
}

internal sealed class TeamsMessageTaskLinkEntityConfiguration : IEntityTypeConfiguration<TeamsMessageTaskLink>
{
    public void Configure(EntityTypeBuilder<TeamsMessageTaskLink> entity)
    {
        entity.ToTable("teams_message_task_links");
        entity.HasKey(l => l.Id);
        entity.Property(l => l.Id).HasColumnName("id");
        entity.Property(l => l.TenantId).HasColumnName("tenant_id");
        entity.Property(l => l.FacilityId).HasColumnName("facility_id");
        entity.Property(l => l.ChannelId).HasColumnName("channel_id");
        entity.Property(l => l.MessageId).HasColumnName("message_id");
        entity.Property(l => l.AiTaskId).HasColumnName("ai_task_id");
        entity.Property(l => l.LinkPurpose).HasColumnName("link_purpose").HasMaxLength(60).IsRequired();
        entity.Property(l => l.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(l => l.SourceContextReference).HasColumnName("source_context_reference").HasMaxLength(200).IsRequired();
        entity.Property(l => l.CreatedByTenantMembershipId).HasColumnName("created_by_tenant_membership_id");
        entity.Property(l => l.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(l => new { l.TenantId, l.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(l => new { l.TenantId, l.MessageId, l.LinkPurpose })
            .HasDatabaseName("ux_teams_message_task_links_tenant_message_purpose")
            .IsUnique();
        entity.HasIndex(l => new { l.TenantId, l.AiTaskId })
            .HasDatabaseName("ix_teams_message_task_links_tenant_task");
        entity.HasIndex(l => new { l.TenantId, l.FacilityId, l.MessageId })
            .HasDatabaseName("ix_teams_message_task_links_tenant_facility_message");
    }
}

internal sealed class TeamsApprovalRequestEntityConfiguration : IEntityTypeConfiguration<TeamsApprovalRequest>
{
    public void Configure(EntityTypeBuilder<TeamsApprovalRequest> entity)
    {
        entity.ToTable("teams_approval_requests");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ChannelId).HasColumnName("channel_id");
        entity.Property(r => r.RequestedByTenantMembershipId).HasColumnName("requested_by_tenant_membership_id");
        entity.Property(r => r.MessageId).HasColumnName("message_id");
        entity.Property(r => r.AiTaskId).HasColumnName("ai_task_id");
        entity.Property(r => r.SourceKind).HasColumnName("source_kind").HasMaxLength(20).IsRequired();
        entity.Property(r => r.Title).HasColumnName("title").HasMaxLength(TeamsConstants.MaxApprovalTitleLength).IsRequired();
        entity.Property(r => r.Rationale).HasColumnName("rationale").HasMaxLength(TeamsConstants.MaxApprovalRationaleLength).IsRequired();
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(r => r.IdempotencyPurpose).HasColumnName("idempotency_purpose").HasMaxLength(160).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(r => r.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .HasDatabaseName("ux_teams_approval_requests_tenant_idempotency")
            .IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.ChannelId, r.CreatedAtUtc })
            .HasDatabaseName("ix_teams_approval_requests_tenant_facility_channel");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.Status })
            .HasDatabaseName("ix_teams_approval_requests_tenant_facility_status");
    }
}

internal sealed class TeamsApprovalReviewerEntityConfiguration : IEntityTypeConfiguration<TeamsApprovalReviewer>
{
    public void Configure(EntityTypeBuilder<TeamsApprovalReviewer> entity)
    {
        entity.ToTable("teams_approval_reviewers");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.RequestId).HasColumnName("request_id");
        entity.Property(r => r.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(r => r.Role).HasColumnName("role").HasMaxLength(30).IsRequired();
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.Request).WithMany(req => req.Reviewers)
            .HasForeignKey(r => r.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.RequestId, r.TenantMembershipId })
            .HasDatabaseName("ux_teams_approval_reviewers_request_membership")
            .IsUnique();
    }
}

internal sealed class TeamsApprovalDecisionEntityConfiguration : IEntityTypeConfiguration<TeamsApprovalDecision>
{
    public void Configure(EntityTypeBuilder<TeamsApprovalDecision> entity)
    {
        entity.ToTable("teams_approval_decisions");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.RequestId).HasColumnName("request_id");
        entity.Property(d => d.ReviewerTenantMembershipId).HasColumnName("reviewer_tenant_membership_id");
        entity.Property(d => d.Decision).HasColumnName("decision").HasMaxLength(30).IsRequired();
        entity.Property(d => d.Note).HasColumnName("note").HasMaxLength(TeamsConstants.MaxApprovalDecisionNoteLength);
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(d => d.Request).WithMany(req => req.Decisions)
            .HasForeignKey(d => d.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.RequestId })
            .HasDatabaseName("ux_teams_approval_decisions_tenant_request")
            .IsUnique();
    }
}

internal sealed class TeamsApprovalAuditEventEntityConfiguration : IEntityTypeConfiguration<TeamsApprovalAuditEvent>
{
    public void Configure(EntityTypeBuilder<TeamsApprovalAuditEvent> entity)
    {
        entity.ToTable("teams_approval_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RequestId).HasColumnName("request_id");
        entity.Property(e => e.ChannelId).HasColumnName("channel_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(60).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ActorTenantMembershipId).HasColumnName("actor_tenant_membership_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.ActorLabel).HasColumnName("actor_label").HasMaxLength(200).IsRequired();
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.RequestId, e.OccurredAtUtc })
            .HasDatabaseName("ix_teams_approval_audit_events_tenant_request");
    }
}
