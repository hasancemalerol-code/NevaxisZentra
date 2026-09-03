using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.AiTasks;

internal sealed class AiTaskSlaPolicyEntityConfiguration : IEntityTypeConfiguration<AiTaskSlaPolicy>
{
    public void Configure(EntityTypeBuilder<AiTaskSlaPolicy> entity)
    {
        entity.ToTable("ai_task_sla_policies");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.PolicyKey).HasColumnName("policy_key").HasMaxLength(100).IsRequired();
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Priority).HasColumnName("priority").HasMaxLength(20).IsRequired();
        entity.Property(p => p.Category).HasColumnName("category").HasMaxLength(50);
        entity.Property(p => p.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(p => p.AcknowledgmentTargetMinutes).HasColumnName("acknowledgment_target_minutes");
        entity.Property(p => p.ResponseTargetMinutes).HasColumnName("response_target_minutes");
        entity.Property(p => p.ResolutionTargetMinutes).HasColumnName("resolution_target_minutes");
        entity.Property(p => p.Enabled).HasColumnName("enabled");
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => new { p.TenantId, p.PolicyKey })
            .HasDatabaseName("ux_ai_task_sla_policies_tenant_key")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Priority })
            .HasDatabaseName("ix_ai_task_sla_policies_tenant_facility_priority");
    }
}

internal sealed class AiTaskRoutingRuleEntityConfiguration : IEntityTypeConfiguration<AiTaskRoutingRule>
{
    public void Configure(EntityTypeBuilder<AiTaskRoutingRule> entity)
    {
        entity.ToTable("ai_task_routing_rules");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.RuleKey).HasColumnName("rule_key").HasMaxLength(100).IsRequired();
        entity.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(r => r.SortOrder).HasColumnName("sort_order");
        entity.Property(r => r.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(r => r.Category).HasColumnName("category").HasMaxLength(50);
        entity.Property(r => r.Priority).HasColumnName("priority").HasMaxLength(20);
        entity.Property(r => r.TargetType).HasColumnName("target_type").HasMaxLength(40).IsRequired();
        entity.Property(r => r.TargetMembershipId).HasColumnName("target_membership_id");
        entity.Property(r => r.TargetTeamChannelId).HasColumnName("target_team_channel_id");
        entity.Property(r => r.Enabled).HasColumnName("enabled");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.RuleKey })
            .HasDatabaseName("ux_ai_task_routing_rules_tenant_facility_key")
            .IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.SortOrder })
            .HasDatabaseName("ix_ai_task_routing_rules_tenant_facility_sort");
    }
}

internal sealed class AiTaskEscalationEventEntityConfiguration : IEntityTypeConfiguration<AiTaskEscalationEvent>
{
    public void Configure(EntityTypeBuilder<AiTaskEscalationEvent> entity)
    {
        entity.ToTable("ai_task_escalation_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
        entity.Property(e => e.BreachType).HasColumnName("breach_type").HasMaxLength(50).IsRequired();
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ActorId).HasColumnName("actor_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(2000);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Task).WithMany()
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.TaskId, e.IdempotencyKey })
            .HasDatabaseName("ux_ai_task_escalation_events_idempotency")
            .IsUnique();
    }
}
