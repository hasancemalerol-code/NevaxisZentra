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

internal sealed class TeamsShiftHandoverEntityConfiguration : IEntityTypeConfiguration<TeamsShiftHandover>
{
    public void Configure(EntityTypeBuilder<TeamsShiftHandover> entity)
    {
        entity.ToTable("teams_shift_handovers");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.TenantId).HasColumnName("tenant_id");
        entity.Property(h => h.FacilityId).HasColumnName("facility_id");
        entity.Property(h => h.ChannelId).HasColumnName("channel_id");
        entity.Property(h => h.AuthorTenantMembershipId).HasColumnName("author_tenant_membership_id");
        entity.Property(h => h.ShiftDate).HasColumnName("shift_date");
        entity.Property(h => h.ShiftType).HasColumnName("shift_type").HasMaxLength(20).IsRequired();
        entity.Property(h => h.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(h => h.Summary).HasColumnName("summary").HasMaxLength(TeamsConstants.MaxHandoverSummaryLength).IsRequired();
        entity.Property(h => h.SafetyNote).HasColumnName("safety_note").HasMaxLength(TeamsConstants.MaxHandoverSafetyNoteLength).IsRequired();
        entity.Property(h => h.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(h => h.IdempotencyPurpose).HasColumnName("idempotency_purpose").HasMaxLength(160).IsRequired();
        entity.Property(h => h.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(h => h.PublishedAtUtc).HasColumnName("published_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(h => h.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(h => new { h.TenantId, h.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(h => new { h.TenantId, h.IdempotencyKey })
            .HasDatabaseName("ux_teams_shift_handovers_tenant_idempotency")
            .IsUnique();
        entity.HasIndex(h => new { h.TenantId, h.FacilityId, h.ChannelId, h.ShiftDate })
            .HasDatabaseName("ix_teams_shift_handovers_tenant_facility_channel_date");
        entity.HasIndex(h => new { h.TenantId, h.FacilityId, h.Status })
            .HasDatabaseName("ix_teams_shift_handovers_tenant_facility_status");
    }
}

internal sealed class TeamsShiftHandoverItemEntityConfiguration : IEntityTypeConfiguration<TeamsShiftHandoverItem>
{
    public void Configure(EntityTypeBuilder<TeamsShiftHandoverItem> entity)
    {
        entity.ToTable("teams_shift_handover_items");
        entity.HasKey(i => i.Id);
        entity.Property(i => i.Id).HasColumnName("id");
        entity.Property(i => i.TenantId).HasColumnName("tenant_id");
        entity.Property(i => i.FacilityId).HasColumnName("facility_id");
        entity.Property(i => i.HandoverId).HasColumnName("handover_id");
        entity.Property(i => i.SortOrder).HasColumnName("sort_order");
        entity.Property(i => i.ItemKind).HasColumnName("item_kind").HasMaxLength(30).IsRequired();
        entity.Property(i => i.Title).HasColumnName("title").HasMaxLength(TeamsConstants.MaxHandoverItemTitleLength).IsRequired();
        entity.Property(i => i.Body).HasColumnName("body").HasMaxLength(TeamsConstants.MaxHandoverItemBodyLength).IsRequired();
        entity.Property(i => i.MessageId).HasColumnName("message_id");
        entity.Property(i => i.AiTaskId).HasColumnName("ai_task_id");
        entity.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(i => i.Handover).WithMany(h => h.Items)
            .HasForeignKey(i => i.HandoverId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(i => new { i.TenantId, i.HandoverId, i.SortOrder })
            .HasDatabaseName("ix_teams_shift_handover_items_tenant_handover");
    }
}

internal sealed class TeamsShiftHandoverAcknowledgementEntityConfiguration : IEntityTypeConfiguration<TeamsShiftHandoverAcknowledgement>
{
    public void Configure(EntityTypeBuilder<TeamsShiftHandoverAcknowledgement> entity)
    {
        entity.ToTable("teams_shift_handover_acknowledgements");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.HandoverId).HasColumnName("handover_id");
        entity.Property(a => a.AcknowledgedByTenantMembershipId).HasColumnName("acknowledged_by_tenant_membership_id");
        entity.Property(a => a.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Handover).WithMany(h => h.Acknowledgements)
            .HasForeignKey(a => a.HandoverId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.HandoverId, a.AcknowledgedByTenantMembershipId })
            .HasDatabaseName("ux_teams_shift_handover_acks_tenant_handover_member")
            .IsUnique();
    }
}

internal sealed class TeamsShiftHandoverAuditEventEntityConfiguration : IEntityTypeConfiguration<TeamsShiftHandoverAuditEvent>
{
    public void Configure(EntityTypeBuilder<TeamsShiftHandoverAuditEvent> entity)
    {
        entity.ToTable("teams_shift_handover_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.HandoverId).HasColumnName("handover_id");
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
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.HandoverId, e.OccurredAtUtc })
            .HasDatabaseName("ix_teams_shift_handover_audit_tenant_handover");
    }
}

internal sealed class TeamsDirectConversationEntityConfiguration : IEntityTypeConfiguration<TeamsDirectConversation>
{
    public void Configure(EntityTypeBuilder<TeamsDirectConversation> entity)
    {
        entity.ToTable("teams_direct_conversations");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.ParticipantLowMembershipId).HasColumnName("participant_low_membership_id");
        entity.Property(c => c.ParticipantHighMembershipId).HasColumnName("participant_high_membership_id");
        entity.Property(c => c.InitiatedByTenantMembershipId).HasColumnName("initiated_by_tenant_membership_id");
        entity.Property(c => c.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(c => c.IdempotencyPurpose).HasColumnName("idempotency_purpose").HasMaxLength(160).IsRequired();
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.ParticipantLowMembershipId, c.ParticipantHighMembershipId })
            .HasDatabaseName("ux_teams_direct_conversations_tenant_facility_pair")
            .IsUnique();
        entity.HasIndex(c => new { c.TenantId, c.IdempotencyKey })
            .HasDatabaseName("ux_teams_direct_conversations_tenant_idempotency")
            .IsUnique();
    }
}

internal sealed class TeamsDirectConversationMemberEntityConfiguration : IEntityTypeConfiguration<TeamsDirectConversationMember>
{
    public void Configure(EntityTypeBuilder<TeamsDirectConversationMember> entity)
    {
        entity.ToTable("teams_direct_conversation_members");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.ConversationId).HasColumnName("conversation_id");
        entity.Property(m => m.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(m => m.JoinedAtUtc).HasColumnName("joined_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(m => m.Conversation).WithMany(c => c.Members)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => new { m.TenantId, m.ConversationId, m.TenantMembershipId })
            .HasDatabaseName("ux_teams_direct_conversation_members_conversation_membership")
            .IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.TenantMembershipId })
            .HasDatabaseName("ix_teams_direct_conversation_members_tenant_facility_membership");
    }
}

internal sealed class TeamsDirectMessageEntityConfiguration : IEntityTypeConfiguration<TeamsDirectMessage>
{
    public void Configure(EntityTypeBuilder<TeamsDirectMessage> entity)
    {
        entity.ToTable("teams_direct_messages");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.ConversationId).HasColumnName("conversation_id");
        entity.Property(m => m.AuthorTenantMembershipId).HasColumnName("author_tenant_membership_id");
        entity.Property(m => m.Body).HasColumnName("body").HasMaxLength(TeamsConstants.MaxMessageBodyLength).IsRequired();
        entity.Property(m => m.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        entity.Property(m => m.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => new { m.TenantId, m.IdempotencyKey })
            .HasDatabaseName("ux_teams_direct_messages_tenant_idempotency")
            .IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.ConversationId, m.CreatedAtUtc })
            .HasDatabaseName("ix_teams_direct_messages_tenant_facility_conversation_created");
    }
}

internal sealed class TeamsDirectReadStateEntityConfiguration : IEntityTypeConfiguration<TeamsDirectReadState>
{
    public void Configure(EntityTypeBuilder<TeamsDirectReadState> entity)
    {
        entity.ToTable("teams_direct_read_states");
        entity.HasKey(r => new { r.TenantId, r.ConversationId, r.TenantMembershipId });
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.ConversationId).HasColumnName("conversation_id");
        entity.Property(r => r.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(r => r.LastReadMessageId).HasColumnName("last_read_message_id");
        entity.Property(r => r.LastReadAtUtc).HasColumnName("last_read_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.TenantMembershipId })
            .HasDatabaseName("ix_teams_direct_read_states_tenant_facility_membership");
    }
}

internal sealed class TeamsDirectMessageAuditEventEntityConfiguration : IEntityTypeConfiguration<TeamsDirectMessageAuditEvent>
{
    public void Configure(EntityTypeBuilder<TeamsDirectMessageAuditEvent> entity)
    {
        entity.ToTable("teams_direct_message_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
        entity.Property(e => e.MessageId).HasColumnName("message_id");
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
        entity.HasOne(e => e.Conversation).WithMany()
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConversationId, e.OccurredAtUtc })
            .HasDatabaseName("ix_teams_direct_message_audit_tenant_conversation");
    }
}
