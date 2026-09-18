using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

internal sealed class CommercialPricingScenarioEntityConfiguration
    : IEntityTypeConfiguration<CommercialPricingScenario>
{
    public void Configure(EntityTypeBuilder<CommercialPricingScenario> entity)
    {
        entity.ToTable("commercial_pricing_scenarios");
        entity.HasKey(scenario => scenario.Id);
        entity.Property(scenario => scenario.Id).HasColumnName("id");
        entity.Property(scenario => scenario.TenantId).HasColumnName("tenant_id");
        entity.Property(scenario => scenario.FacilityId).HasColumnName("facility_id");
        entity.Property(scenario => scenario.ScenarioReference).HasColumnName("scenario_reference").HasMaxLength(100).IsRequired();
        entity.Property(scenario => scenario.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(40).IsRequired();
        entity.Property(scenario => scenario.GuardrailResult).HasColumnName("guardrail_result").HasMaxLength(30);
        entity.Property(scenario => scenario.ProfileId).HasColumnName("profile_id");
        entity.Property(scenario => scenario.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(scenario => scenario.MaterialId).HasColumnName("material_id");
        entity.Property(scenario => scenario.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
        entity.Property(scenario => scenario.SalesEvidenceObservationId).HasColumnName("sales_evidence_observation_id");
        entity.Property(scenario => scenario.Quantity).HasColumnName("quantity").HasPrecision(18, 6);
        entity.Property(scenario => scenario.BaselineUnitPrice).HasColumnName("baseline_unit_price").HasPrecision(18, 6);
        entity.Property(scenario => scenario.ProposedUnitPrice).HasColumnName("proposed_unit_price").HasPrecision(18, 6);
        entity.Property(scenario => scenario.ProposedDiscountRatio).HasColumnName("proposed_discount_ratio").HasPrecision(18, 6);
        entity.Property(scenario => scenario.ValidityWindowStartUtc).HasColumnName("validity_window_start_utc");
        entity.Property(scenario => scenario.ValidityWindowEndUtc).HasColumnName("validity_window_end_utc");
        entity.Property(scenario => scenario.CommercialRationale).HasColumnName("commercial_rationale").HasMaxLength(2000).IsRequired();
        entity.Property(scenario => scenario.LatestEvaluationSnapshotId).HasColumnName("latest_evaluation_snapshot_id");
        entity.Property(scenario => scenario.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(scenario => scenario.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(scenario => scenario.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(scenario => scenario.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(scenario => scenario.EvaluationCount).HasColumnName("evaluation_count");
        entity.Property(scenario => scenario.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(scenario => scenario.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(scenario => scenario.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(scenario => scenario.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();

        entity.HasIndex(scenario => new { scenario.TenantId, scenario.FacilityId, scenario.LifecycleStatus })
            .HasDatabaseName("ix_commprofit7_scenarios_tenant_facility_lifecycle");

        entity.HasIndex(scenario => new { scenario.TenantId, scenario.FacilityId, scenario.GuardrailResult })
            .HasDatabaseName("ix_commprofit7_scenarios_tenant_facility_guardrail");

        entity.HasIndex(scenario => new { scenario.TenantId, scenario.FacilityId, scenario.ProfileId })
            .HasDatabaseName("ix_commprofit7_scenarios_tenant_facility_profile");

        entity.HasOne(scenario => scenario.Profile)
            .WithMany()
            .HasForeignKey(scenario => scenario.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(scenario => scenario.LatestEvaluationSnapshot)
            .WithMany()
            .HasForeignKey(scenario => scenario.LatestEvaluationSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(scenario => scenario.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(scenario => new { scenario.TenantId, scenario.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CommercialPricingScenarioEvaluationSnapshotEntityConfiguration
    : IEntityTypeConfiguration<CommercialPricingScenarioEvaluationSnapshot>
{
    public void Configure(EntityTypeBuilder<CommercialPricingScenarioEvaluationSnapshot> entity)
    {
        entity.ToTable("commercial_pricing_scenario_evaluation_snapshots");
        entity.HasKey(snapshot => snapshot.Id);
        entity.Property(snapshot => snapshot.Id).HasColumnName("id");
        entity.Property(snapshot => snapshot.TenantId).HasColumnName("tenant_id");
        entity.Property(snapshot => snapshot.FacilityId).HasColumnName("facility_id");
        entity.Property(snapshot => snapshot.ScenarioId).HasColumnName("scenario_id");
        entity.Property(snapshot => snapshot.ScenarioVersionAtEvaluation).HasColumnName("scenario_version_at_evaluation");
        entity.Property(snapshot => snapshot.EvaluationSequence).HasColumnName("evaluation_sequence");
        entity.Property(snapshot => snapshot.GuardrailResult).HasColumnName("guardrail_result").HasMaxLength(30).IsRequired();
        entity.Property(snapshot => snapshot.AssessmentOutcome).HasColumnName("assessment_outcome").HasMaxLength(30).IsRequired();
        entity.Property(snapshot => snapshot.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(snapshot => snapshot.CostCoverageStatus).HasColumnName("cost_coverage_status").HasMaxLength(40).IsRequired();
        entity.Property(snapshot => snapshot.LeadingDriverCode).HasColumnName("leading_driver_code").HasMaxLength(50);
        entity.Property(snapshot => snapshot.InsufficientDataReason).HasColumnName("insufficient_data_reason").HasMaxLength(500);
        entity.Property(snapshot => snapshot.BaselineUnitPrice).HasColumnName("baseline_unit_price").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ProposedUnitPrice).HasColumnName("proposed_unit_price").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.TotalIncludedUnitCost).HasColumnName("total_included_unit_cost").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ExpectedUnitContribution).HasColumnName("expected_unit_contribution").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ExpectedContributionMarginRatio).HasColumnName("expected_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.BaselineContributionMarginRatio).HasColumnName("baseline_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ContributionMarginDelta).HasColumnName("contribution_margin_delta").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ProfileMinimumContributionMarginRatio).HasColumnName("profile_minimum_contribution_margin_ratio").HasPrecision(18, 6);
        entity.Property(snapshot => snapshot.ProfileId).HasColumnName("profile_id");
        entity.Property(snapshot => snapshot.ProfileVersion).HasColumnName("profile_version").HasMaxLength(20).IsRequired();
        entity.Property(snapshot => snapshot.InputSnapshotJson).HasColumnName("input_snapshot_json").HasColumnType("jsonb").IsRequired();
        entity.Property(snapshot => snapshot.OutputSnapshotJson).HasColumnName("output_snapshot_json").HasColumnType("jsonb").IsRequired();
        entity.Property(snapshot => snapshot.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(snapshot => snapshot.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(snapshot => snapshot.EvaluatedBy).HasColumnName("evaluated_by").HasMaxLength(200).IsRequired();

        entity.HasIndex(snapshot => new { snapshot.TenantId, snapshot.FacilityId, snapshot.ScenarioId, snapshot.EvaluationSequence })
            .IsUnique()
            .HasDatabaseName("ux_commprofit7_snapshots_scenario_sequence");

        entity.HasOne(snapshot => snapshot.Scenario)
            .WithMany(scenario => scenario.EvaluationSnapshots)
            .HasForeignKey(snapshot => snapshot.ScenarioId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(snapshot => new { snapshot.TenantId, snapshot.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CommercialPricingScenarioAuditEventEntityConfiguration
    : IEntityTypeConfiguration<CommercialPricingScenarioAuditEvent>
{
    public void Configure(EntityTypeBuilder<CommercialPricingScenarioAuditEvent> entity)
    {
        entity.ToTable("commercial_pricing_scenario_audit_events");
        entity.HasKey(auditEvent => auditEvent.Id);
        entity.Property(auditEvent => auditEvent.Id).HasColumnName("id");
        entity.Property(auditEvent => auditEvent.TenantId).HasColumnName("tenant_id");
        entity.Property(auditEvent => auditEvent.FacilityId).HasColumnName("facility_id");
        entity.Property(auditEvent => auditEvent.ScenarioId).HasColumnName("scenario_id");
        entity.Property(auditEvent => auditEvent.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        entity.Property(auditEvent => auditEvent.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        entity.Property(auditEvent => auditEvent.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(auditEvent => auditEvent.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(auditEvent => auditEvent.CorrelationId).HasColumnName("correlation_id");

        entity.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.FacilityId, auditEvent.ScenarioId, auditEvent.OccurredAtUtc })
            .HasDatabaseName("ix_commprofit7_audit_tenant_facility_scenario_occurred");

        entity.HasOne(auditEvent => auditEvent.Scenario)
            .WithMany(scenario => scenario.AuditEvents)
            .HasForeignKey(auditEvent => auditEvent.ScenarioId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(auditEvent => new { auditEvent.TenantId, auditEvent.FacilityId })
            .HasPrincipalKey(facility => new { facility.TenantId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
