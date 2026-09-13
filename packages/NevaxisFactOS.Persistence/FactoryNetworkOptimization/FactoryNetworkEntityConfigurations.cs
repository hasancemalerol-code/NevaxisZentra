using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

internal sealed class FactoryNetworkEntityConfiguration : IEntityTypeConfiguration<FactoryNetwork>
{
    public void Configure(EntityTypeBuilder<FactoryNetwork> entity)
    {
        entity.ToTable("factory_networks");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(e => e.PlanningHorizon).HasColumnName("planning_horizon").HasMaxLength(30);
        entity.Property(e => e.TimezonePolicyJson).HasColumnName("timezone_policy_json");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.Name }).HasDatabaseName("ix_factory_networks_tenant_name");
    }
}

internal sealed class FactoryNetworkFacilityEntityConfiguration : IEntityTypeConfiguration<FactoryNetworkFacility>
{
    public void Configure(EntityTypeBuilder<FactoryNetworkFacility> entity)
    {
        entity.ToTable("factory_network_facilities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(30);
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.HasIndex(e => new { e.TenantId, e.NetworkId, e.FacilityId })
            .IsUnique()
            .HasDatabaseName("ix_factory_network_facilities_network_facility");
    }
}

internal sealed class FacilityCapabilityProfileEntityConfiguration : IEntityTypeConfiguration<FacilityCapabilityProfile>
{
    public void Configure(EntityTypeBuilder<FacilityCapabilityProfile> entity)
    {
        entity.ToTable("facility_capability_profiles");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TechnologiesJson).HasColumnName("technologies_json");
        entity.Property(e => e.LinesJson).HasColumnName("lines_json");
        entity.Property(e => e.MachineFamiliesJson).HasColumnName("machine_families_json");
        entity.Property(e => e.MoldCompatibilityJson).HasColumnName("mold_compatibility_json");
        entity.Property(e => e.MaterialsJson).HasColumnName("materials_json");
        entity.Property(e => e.QualityCertificationsJson).HasColumnName("quality_certifications_json");
        entity.Property(e => e.ShiftsJson).HasColumnName("shifts_json");
        entity.Property(e => e.CapacityJson).HasColumnName("capacity_json");
        entity.Property(e => e.RiskJson).HasColumnName("risk_json");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId })
            .IsUnique()
            .HasDatabaseName("ix_facility_capability_profiles_tenant_facility");
    }
}

internal sealed class NetworkCapacitySnapshotEntityConfiguration : IEntityTypeConfiguration<NetworkCapacitySnapshot>
{
    public void Configure(EntityTypeBuilder<NetworkCapacitySnapshot> entity)
    {
        entity.ToTable("network_capacity_snapshots");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.CapturedAtUtc).HasColumnName("captured_at_utc");
        entity.Property(e => e.FacilityCapacityJson).HasColumnName("facility_capacity_json");
        entity.Property(e => e.DemandJson).HasColumnName("demand_json");
        entity.Property(e => e.FreshnessJson).HasColumnName("freshness_json");
        entity.HasIndex(e => new { e.TenantId, e.NetworkId, e.CapturedAtUtc })
            .HasDatabaseName("ix_network_capacity_snapshots_network_captured");
    }
}

internal sealed class NetworkScenarioEntityConfiguration : IEntityTypeConfiguration<NetworkScenario>
{
    public void Configure(EntityTypeBuilder<NetworkScenario> entity)
    {
        entity.ToTable("network_scenarios");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.AllocationId).HasColumnName("allocation_id");
        entity.Property(e => e.ScenarioCode).HasColumnName("scenario_code").HasMaxLength(10);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(60);
        entity.Property(e => e.SourceFacilityId).HasColumnName("source_facility_id");
        entity.Property(e => e.DestinationFacilityId).HasColumnName("destination_facility_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(60);
        entity.Property(e => e.Score).HasColumnName("score");
        entity.Property(e => e.ComparisonJson).HasColumnName("comparison_json");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

internal sealed class NetworkAllocationEntityConfiguration : IEntityTypeConfiguration<NetworkAllocation>
{
    public void Configure(EntityTypeBuilder<NetworkAllocation> entity)
    {
        entity.ToTable("network_allocations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.FactoryDecisionId).HasColumnName("factory_decision_id");
        entity.Property(e => e.SelectedScenarioId).HasColumnName("selected_scenario_id");
        entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(60);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.SourceFacilityId).HasColumnName("source_facility_id");
        entity.Property(e => e.DestinationFacilityId).HasColumnName("destination_facility_id");
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(60);
        entity.Property(e => e.ReasonJson).HasColumnName("reason_json");
        entity.Property(e => e.ExpectedImpactJson).HasColumnName("expected_impact_json");
        entity.Property(e => e.ActualImpactJson).HasColumnName("actual_impact_json");
        entity.Property(e => e.InitiatedBySubjectId).HasColumnName("initiated_by_subject_id").HasMaxLength(120);
        entity.Property(e => e.PublishedAtUtc).HasColumnName("published_at_utc");
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.NetworkId, e.Status })
            .HasDatabaseName("ix_network_allocations_tenant_network_status");
    }
}

internal sealed class NetworkAllocationItemEntityConfiguration : IEntityTypeConfiguration<NetworkAllocationItem>
{
    public void Configure(EntityTypeBuilder<NetworkAllocationItem> entity)
    {
        entity.ToTable("network_allocation_items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.AllocationId).HasColumnName("allocation_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(60);
        entity.Property(e => e.SourceFacilityId).HasColumnName("source_facility_id");
        entity.Property(e => e.DestinationFacilityId).HasColumnName("destination_facility_id");
        entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
        entity.Property(e => e.SourceQuantity).HasColumnName("source_quantity");
        entity.Property(e => e.DestinationQuantity).HasColumnName("destination_quantity");
        entity.Property(e => e.SourcePlanVersionId).HasColumnName("source_plan_version_id");
        entity.Property(e => e.DestinationPlanVersionId).HasColumnName("destination_plan_version_id");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.HasIndex(e => new { e.TenantId, e.AllocationId, e.OrderId })
            .HasDatabaseName("ix_network_allocation_items_allocation_order");
    }
}

internal sealed class FactoryNetworkPolicyEntityConfiguration : IEntityTypeConfiguration<FactoryNetworkPolicy>
{
    public void Configure(EntityTypeBuilder<FactoryNetworkPolicy> entity)
    {
        entity.ToTable("factory_network_policies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.AutonomyMode).HasColumnName("autonomy_mode").HasMaxLength(20);
        entity.Property(e => e.PolicyEnvelopeJson).HasColumnName("policy_envelope_json");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.NetworkId })
            .IsUnique()
            .HasDatabaseName("ix_factory_network_policies_tenant_network");
    }
}

internal sealed class NetworkActivityEventEntityConfiguration : IEntityTypeConfiguration<NetworkActivityEvent>
{
    public void Configure(EntityTypeBuilder<NetworkActivityEvent> entity)
    {
        entity.ToTable("network_activity_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.NetworkId).HasColumnName("network_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(60);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(500);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.NetworkId, e.OccurredAtUtc })
            .HasDatabaseName("ix_network_activity_events_network_occurred");
    }
}
