using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftCoverageRiskFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantEntityColumns = ["tenant_id", "entity_type", "entity_id"];
        private static readonly string[] TenantFacilityOccurredColumns = ["tenant_id", "facility_id", "occurred_at_utc"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];
        private static readonly string[] TenantEvaluateIdempotencyColumns = ["tenant_id", "evaluate_idempotency_key"];
        private static readonly string[] TenantFacilityShiftDateColumns = ["tenant_id", "facility_id", "production_shift_id", "shift_date"];
        private static readonly string[] TenantFacilityActiveDateColumns = ["tenant_id", "facility_id", "is_active", "shift_date"];
        private static readonly string[] TenantFacilityEvaluatedColumns = ["tenant_id", "facility_id", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityFingerprintColumns = ["tenant_id", "facility_id", "input_fingerprint"];
        private static readonly string[] TenantFacilityOutcomeDateColumns = ["tenant_id", "facility_id", "outcome", "shift_date"];
        private static readonly string[] TenantAssessmentReasonColumns = ["tenant_id", "assessment_id", "reason_code"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_coverage_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    production_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_family_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    required_competency_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    minimum_approved_operator_count = table.Column<int>(type: "integer", nullable: false),
                    is_critical_role = table.Column<bool>(type: "boolean", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_until = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    create_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    create_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_coverage_requirements", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_coverage_requirements_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_coverage_requirements_production_shifts_production_sh~",
                        column: x => x.production_shift_id,
                        principalTable: "production_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_coverage_requirements_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_risk_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    actor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_risk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_risk_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_risk_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_risk_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coverage_requirement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    production_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_family_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    required_competency_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    minimum_required_count = table.Column<int>(type: "integer", nullable: true),
                    is_critical_role = table.Column<bool>(type: "boolean", nullable: true),
                    approved_eligible_count = table.Column<int>(type: "integer", nullable: false),
                    coverage_gap = table.Column<int>(type: "integer", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    leading_reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    leading_driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    structured_evidence_json = table.Column<string>(type: "text", nullable: false),
                    source_freshness_snapshot_json = table.Column<string>(type: "text", nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_risk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_risk_assessments_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_risk_assessments_production_shifts_production_shift_id",
                        column: x => x.production_shift_id,
                        principalTable: "production_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_risk_assessments_shift_coverage_requirements_coverage~",
                        column: x => x.coverage_requirement_id,
                        principalTable: "shift_coverage_requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_risk_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_risk_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observed_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    gap_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    production_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_competency_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    evidence_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    evidence_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_leading_risk = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_risk_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_risk_drivers_shift_risk_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "shift_risk_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_risk_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shift_coverage_requirements_production_shift_id",
                table: "shift_coverage_requirements",
                column: "production_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_coverage_requirements_tenant_create_idempotency",
                table: "shift_coverage_requirements",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_coverage_requirements_tenant_facility_active_date",
                table: "shift_coverage_requirements",
                columns: TenantFacilityActiveDateColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_coverage_requirements_tenant_facility_shift_date",
                table: "shift_coverage_requirements",
                columns: TenantFacilityShiftDateColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_risk_assessments_coverage_requirement_id",
                table: "shift_risk_assessments",
                column: "coverage_requirement_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_risk_assessments_production_shift_id",
                table: "shift_risk_assessments",
                column: "production_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_assessments_tenant_evaluate_idempotency",
                table: "shift_risk_assessments",
                columns: TenantEvaluateIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_assessments_tenant_facility_evaluated",
                table: "shift_risk_assessments",
                columns: TenantFacilityEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_assessments_tenant_facility_fingerprint",
                table: "shift_risk_assessments",
                columns: TenantFacilityFingerprintColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_assessments_tenant_facility_outcome_date",
                table: "shift_risk_assessments",
                columns: TenantFacilityOutcomeDateColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_audit_events_tenant_entity",
                table: "shift_risk_audit_events",
                columns: TenantEntityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_audit_events_tenant_facility_occurred",
                table: "shift_risk_audit_events",
                columns: TenantFacilityOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_risk_drivers_assessment_id",
                table: "shift_risk_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_drivers_tenant_assessment_reason",
                table: "shift_risk_drivers",
                columns: TenantAssessmentReasonColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-00000000006f',
                     'shift-risk.read',
                     'Read shift coverage requirements, risk assessments, and audit history.',
                     'Active',
                     '2026-08-13T05:00:00Z'),
                    ('10000000-0000-4000-8000-000000000070',
                     'shift-risk.manage',
                     'Create and update coverage requirements and evaluate shift coverage risk.',
                     'Active',
                     '2026-08-13T05:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE shift_coverage_requirements
                    ADD CONSTRAINT ck_scr_requirements_planned_window
                        CHECK (planned_end_utc > planned_start_utc),
                    ADD CONSTRAINT ck_scr_requirements_competency
                        CHECK (required_competency_code <> ''),
                    ADD CONSTRAINT ck_scr_requirements_minimum_count
                        CHECK (minimum_approved_operator_count >= 1 AND minimum_approved_operator_count <= 50),
                    ADD CONSTRAINT ck_scr_requirements_version
                        CHECK (version >= 1);

                ALTER TABLE shift_risk_assessments
                    ADD CONSTRAINT ck_sra_assessments_planned_window
                        CHECK (planned_end_utc > planned_start_utc),
                    ADD CONSTRAINT ck_sra_assessments_outcome
                        CHECK (outcome IN ('clear','attention','critical','insufficient-data')),
                    ADD CONSTRAINT ck_sra_assessments_counts
                        CHECK (approved_eligible_count >= 0 AND coverage_gap >= 0);

                ALTER TABLE shift_risk_drivers
                    ADD CONSTRAINT ck_srd_drivers_severity
                        CHECK (severity IN ('pass','warn','fail','info'));

                REVOKE ALL ON TABLE shift_coverage_requirements FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_coverage_requirements TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_risk_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE shift_risk_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_risk_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE shift_risk_drivers TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_risk_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE shift_risk_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "shift_coverage_requirements", "scr");
            ApplyTenantRls(migrationBuilder, "shift_risk_assessments", "sra");
            ApplyTenantRlsAppendOnly(migrationBuilder, "shift_risk_drivers", "srd");
            ApplyTenantRlsAppendOnly(migrationBuilder, "shift_risk_audit_events", "srae");
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'ck_audit_records_action_taxonomy') THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_action_taxonomy;
                    END IF;
                END $$;

                ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_action_taxonomy CHECK (
                    action IN (
                        'authenticate','access.tenant','authorize','write.audit','validate.configuration',
                        'ai.decision.append','production.plan.draft.create','production.plan.draft.update',
                        'production.plan.workflow.submit.for.review','production.plan.workflow.request.revision',
                        'production.plan.workflow.approve','production.plan.workflow.reject','production.plan.schedule',
                        'production.plan.transfer.request','production.plan.transfer.start','production.plan.transfer.succeed',
                        'production.plan.transfer.fail','production.plan.transfer.mark.uncertain','production.plan.handoff.prepare',
                        'production.order.create','production.order.start','production.order.pause','production.order.resume',
                        'production.order.complete','production.order.complete.short','production.plan.handoff.consume',
                        'production.order.quantity.good.record','production.order.quantity.scrap.record','production.order.quantity.reverse',
                        'production.order.downtime.start','production.order.downtime.end','production.order.setup.start',
                        'production.order.setup.complete','production.order.cycle.manual.record','production.order.exception.raise',
                        'production.order.exception.acknowledge','production.order.exception.resolve','production.order.downtime.recover',
                        'production.order.setup.recover','production.order.shift.assign','production.order.shift.reassign',
                        'production.order.operator.assign','production.order.operator.release','production.order.handover.create',
                        'production.order.exception.assign','production.order.exception.unassign',
                        'planning.logistics.constraints.evaluate','planning.logistics.risk.change',
                        'export.case.created','export.case.updated','export.document.registered','export.document.verified',
                        'export.document.rejected','export.readiness.changed','export.packing.unit.created','export.packing.unit.updated',
                        'export.packing.item.added','export.packing.completed','export.packing.verified','export.shipment.created',
                        'export.shipment.updated','export.shipment.status.changed','export.broker.workspace.created',
                        'export.broker.package.prepared','export.broker.package.sent','export.broker.workspace.acknowledged',
                        'export.broker.correction.created','export.broker.correction.resolved','export.compliance.evaluated',
                        'export.compliance.revalidated','export.compliance.requirement.satisfied','export.country.rule.pack.created',
                        'export.country.rule.added','export.country.rule.pack.verified','export.country.rule.pack.superseded',
                        'export.country.research.created','export.country.research.finding.added','export.country.research.reviewed',
                        'export.country.research.completed','export.country.research.linked','export.country.knowledge.created',
                        'export.country.knowledge.verified','export.country.knowledge.superseded','export.ai.analysis.started',
                        'export.ai.analysis.completed','export.ai.analysis.failed','export.ai.high-risk.found',
                        'export.ai.recommendation.reviewed','export.ai.recommendation.accepted','export.ai.recommendation.rejected',
                        'export.ai.task.requested',
                        'quality.plan.created','quality.plan.updated','quality.inspection.started','quality.result.recorded',
                        'quality.inspection.completed','quality.release.released','quality.release.rejected',
                        'quality.ncr.created','quality.ncr.updated','quality.ncr.resolved','quality.ncr.closed',
                        'quality.hold.placed','quality.hold.released','quality.disposition.decided',
                        'quality.corrective.action.created','quality.corrective.action.completed',
                        'maintenance.request.created','maintenance.request.updated','maintenance.workorder.created',
                        'maintenance.workorder.assigned','maintenance.workorder.started','maintenance.workorder.waiting',
                        'maintenance.workorder.resumed','maintenance.workorder.completed','maintenance.workorder.cancelled',
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated',
                        'maintenance.worknote.created','maintenance.preventive.due.generated',
                        'maintenance.preventive.due.workordercreated','maintenance.partusage.recorded',
                        'erp.connection.created','erp.connection.updated','erp.connection.health.checked',
                        'erp.sync.started','erp.sync.completed','erp.sync.failed',
                        'erp.sync.orders.started','erp.sync.orders.completed','erp.sync.orders.failed',
                        'erp.sync.warehouses.started','erp.sync.warehouses.completed','erp.sync.warehouses.failed',
                        'erp.sync.inventory.started','erp.sync.inventory.completed','erp.sync.inventory.failed',
                        'erp.sync.purchasing.started','erp.sync.purchasing.completed','erp.sync.purchasing.failed',
                        'erp.sync.production.started','erp.sync.production.completed','erp.sync.production.failed',
                        'erp.sync.documents.started','erp.sync.documents.completed','erp.sync.documents.failed',
                        'erp.mapping.confirmed','erp.mapping.ignored',
                        'erp.writeback.created','erp.writeback.submitted','erp.writeback.approved','erp.writeback.rejected',
                        'erp.writeback.execution.started','erp.writeback.execution.succeeded',
                        'erp.writeback.execution.failed','erp.writeback.execution.blocked',
                        'edge.gateway.registered','edge.machine.registered','edge.machine.assignment.created',
                        'edge.event.ingested','edge.event.duplicate.rejected','edge.gateway.health.checked',
                        'mold.created','mold.updated','mold.counter.recorded','mold.lifecycle.transitioned',
                        'mold.maintenance.created','mold.maintenance.updated','mold.maintenance.completed','mold.maintenance.cancelled',
                        'mold.revision.appended','mold.trial.appended',
                        'mold.compatibility.requirement.upserted','mold.compatibility.machine-profile.upserted','mold.compatibility.evaluated',
                        'material.created','material.updated','material.technical.updated','material.lifecycle.transitioned',
                        'material.lot.created','material.lot.receipt.observed','material.lot.transferred',
                        'material.lot.status.changed','material.lot.quality.recorded','material.lot.certificate.linked',
                        'material.requirement.profile.created','material.requirement.profile.updated',
                        'material.consumption.observed','material.requirement.assessed',
                        'process-oee.window.created','process-oee.window.updated','process-oee.assessment.evaluated',
                        'shift-workforce.person.synchronized','shift-workforce.competency.created','shift-workforce.competency.updated',
                        'shift-assign.assignment.created','shift-assign.assignment.approved','shift-assign.assignment.cancelled',
                        'shift-risk.requirement.created','shift-risk.requirement.updated','shift-risk.assessment.evaluated'
                    ));

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'ck_audit_records_event_taxonomy') THEN
                        ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_event_taxonomy;
                    END IF;
                END $$;

                ALTER TABLE audit_records ADD CONSTRAINT ck_audit_records_event_taxonomy CHECK (
                    event_type IN (
                        'authentication.succeeded','authentication.failed','tenant.access.granted','tenant.access.denied',
                        'authorization.granted','authorization.denied','audit.write.failed','infrastructure.configuration.rejected',
                        'ai.decision.recorded','production.plan.draft.created','production.plan.draft.updated',
                        'production.plan.submitted.for.review','production.plan.revision.requested','production.plan.approved',
                        'production.plan.rejected','production.plan.scheduled','production.plan.transfer.requested',
                        'production.plan.transfer.started','production.plan.transfer.succeeded','production.plan.transfer.failed',
                        'production.plan.transfer.uncertain','production.plan.handoff.prepared','production.order.created',
                        'production.order.started','production.order.paused','production.order.resumed','production.order.completed',
                        'production.order.completed.short','production.plan.handoff.consumed','production.order.quantity.good.recorded',
                        'production.order.quantity.scrap.recorded','production.order.quantity.reversed','production.order.downtime.started',
                        'production.order.downtime.ended','production.order.setup.started','production.order.setup.completed',
                        'production.order.cycle.manual.recorded','production.order.exception.raised','production.order.exception.acknowledged',
                        'production.order.exception.resolved','production.order.downtime.recovered','production.order.setup.recovered',
                        'production.order.shift.assigned','production.order.shift.reassigned','production.order.operator.assigned',
                        'production.order.operator.released','production.order.handover.created','production.order.exception.assigned',
                        'production.order.exception.unassigned','planning.logistics.constraints.evaluated','planning.logistics.risk.changed',
                        'export.case.created','export.case.updated','export.document.registered','export.document.verified',
                        'export.document.rejected','export.readiness.changed','export.packing.unit.created','export.packing.unit.updated',
                        'export.packing.item.added','export.packing.completed','export.packing.verified','export.shipment.created',
                        'export.shipment.updated','export.shipment.status.changed','export.broker.workspace.created',
                        'export.broker.package.prepared','export.broker.package.sent','export.broker.workspace.acknowledged',
                        'export.broker.correction.created','export.broker.correction.resolved','export.compliance.evaluated',
                        'export.compliance.revalidated','export.compliance.requirement.satisfied','export.country.rule.pack.created',
                        'export.country.rule.added','export.country.rule.pack.verified','export.country.rule.pack.superseded',
                        'export.country.research.created','export.country.research.finding.added','export.country.research.reviewed',
                        'export.country.research.completed','export.country.research.linked','export.country.knowledge.created',
                        'export.country.knowledge.verified','export.country.knowledge.superseded','export.ai.analysis.started',
                        'export.ai.analysis.completed','export.ai.analysis.failed','export.ai.high-risk.found',
                        'export.ai.recommendation.reviewed','export.ai.recommendation.accepted','export.ai.recommendation.rejected',
                        'export.ai.task.requested',
                        'quality.plan.created','quality.plan.updated','quality.inspection.started','quality.result.recorded',
                        'quality.inspection.completed','quality.release.released','quality.release.rejected',
                        'quality.ncr.created','quality.ncr.updated','quality.ncr.resolved','quality.ncr.closed',
                        'quality.hold.placed','quality.hold.released','quality.disposition.decided',
                        'quality.corrective.action.created','quality.corrective.action.completed',
                        'maintenance.request.created','maintenance.request.updated','maintenance.workorder.created',
                        'maintenance.workorder.assigned','maintenance.workorder.started','maintenance.workorder.waiting',
                        'maintenance.workorder.resumed','maintenance.workorder.completed','maintenance.workorder.cancelled',
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated',
                        'maintenance.worknote.created','maintenance.preventive.due.generated',
                        'maintenance.preventive.due.workordercreated','maintenance.partusage.recorded',
                        'erp.connection.created','erp.connection.updated','erp.connection.health.checked',
                        'erp.sync.started','erp.sync.completed','erp.sync.failed',
                        'erp.sync.orders.started','erp.sync.orders.completed','erp.sync.orders.failed',
                        'erp.sync.warehouses.started','erp.sync.warehouses.completed','erp.sync.warehouses.failed',
                        'erp.sync.inventory.started','erp.sync.inventory.completed','erp.sync.inventory.failed',
                        'erp.sync.purchasing.started','erp.sync.purchasing.completed','erp.sync.purchasing.failed',
                        'erp.sync.production.started','erp.sync.production.completed','erp.sync.production.failed',
                        'erp.sync.documents.started','erp.sync.documents.completed','erp.sync.documents.failed',
                        'erp.mapping.confirmed','erp.mapping.ignored',
                        'erp.writeback.created','erp.writeback.submitted','erp.writeback.approved','erp.writeback.rejected',
                        'erp.writeback.execution.started','erp.writeback.execution.succeeded',
                        'erp.writeback.execution.failed','erp.writeback.execution.blocked',
                        'edge.gateway.registered','edge.machine.registered','edge.machine.assignment.created',
                        'edge.event.ingested','edge.event.duplicate.rejected','edge.gateway.health.checked',
                        'mold.created','mold.updated','mold.counter.recorded','mold.lifecycle.transitioned',
                        'mold.maintenance.created','mold.maintenance.updated','mold.maintenance.completed','mold.maintenance.cancelled',
                        'mold.revision.appended','mold.trial.appended',
                        'mold.compatibility.requirement.upserted','mold.compatibility.machine-profile.upserted','mold.compatibility.evaluated',
                        'material.created','material.updated','material.technical.updated','material.lifecycle.transitioned',
                        'material.lot.created','material.lot.receipt.observed','material.lot.transferred',
                        'material.lot.status.changed','material.lot.quality.recorded','material.lot.certificate.linked',
                        'material.requirement.profile.created','material.requirement.profile.updated',
                        'material.consumption.observed','material.requirement.assessed',
                        'process-oee.window.created','process-oee.window.updated','process-oee.assessment.evaluated',
                        'shift-workforce.person.synchronized','shift-workforce.competency.created','shift-workforce.competency.updated',
                        'shift-assign.assignment.created','shift-assign.assignment.approved','shift-assign.assignment.cancelled',
                        'shift-risk.requirement.created','shift-risk.requirement.updated','shift-risk.assessment.evaluated'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "shift_risk_audit_events");
            migrationBuilder.DropTable(name: "shift_risk_drivers");
            migrationBuilder.DropTable(name: "shift_risk_assessments");
            migrationBuilder.DropTable(name: "shift_coverage_requirements");
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        private static void ApplyTenantRlsAppendOnly(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
