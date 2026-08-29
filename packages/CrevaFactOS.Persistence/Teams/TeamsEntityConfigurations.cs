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
