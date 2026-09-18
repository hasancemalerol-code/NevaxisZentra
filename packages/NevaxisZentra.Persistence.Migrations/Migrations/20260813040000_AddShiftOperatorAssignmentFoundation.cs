using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftOperatorAssignmentFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantEntityColumns = ["tenant_id", "entity_type", "entity_id"];
        private static readonly string[] TenantFacilityOccurredColumns = ["tenant_id", "facility_id", "occurred_at_utc"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];
        private static readonly string[] TenantFacilityOrderRefColumns = ["tenant_id", "facility_id", "production_order_reference"];
        private static readonly string[] TenantFacilityStatusDateColumns = ["tenant_id", "facility_id", "status", "shift_date"];
        private static readonly string[] TenantPersonWindowColumns = ["tenant_id", "workforce_person_id", "planned_start_utc", "planned_end_utc"];
        private static readonly string[] TenantFacilityEvaluatedColumns = ["tenant_id", "facility_id", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityFingerprintColumns = ["tenant_id", "facility_id", "input_fingerprint"];
        private static readonly string[] TenantResultColumns = ["tenant_id", "result_id"];
        private static readonly string[] TenantAssessmentPersonColumns = ["tenant_id", "assessment_id", "workforce_person_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_operator_assignment_audit_events",
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
                    table.PrimaryKey("PK_shift_operator_assignment_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignment_audit_events_facilities_tenant_id~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignment_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_operator_eligibility_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    required_competency_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    machine_family_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    process_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_operator_eligibility_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_assessments_facilities_tenant_id~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_assessments_production_shifts_pr~",
                        column: x => x.production_shift_id,
                        principalTable: "production_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_operator_eligibility_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workforce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    primary_reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employee_code_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_operator_eligibility_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_results_shift_operator_eligibili~",
                        column: x => x.assessment_id,
                        principalTable: "shift_operator_eligibility_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_results_shift_workforce_persons_~",
                        column: x => x.workforce_person_id,
                        principalTable: "shift_workforce_persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_results_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_operator_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    workforce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    eligibility_assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eligibility_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_competency_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    machine_family_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    process_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    create_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    create_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    approve_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    approve_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_operator_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_production_shifts_production_shi~",
                        column: x => x.production_shift_id,
                        principalTable: "production_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_shift_operator_eligibility_asses~",
                        column: x => x.eligibility_assessment_id,
                        principalTable: "shift_operator_eligibility_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_shift_operator_eligibility_resul~",
                        column: x => x.eligibility_result_id,
                        principalTable: "shift_operator_eligibility_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_shift_workforce_persons_workforc~",
                        column: x => x.workforce_person_id,
                        principalTable: "shift_workforce_persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_operator_eligibility_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observed_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    evidence_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_operator_eligibility_reasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_reasons_shift_operator_eligibili~",
                        column: x => x.result_id,
                        principalTable: "shift_operator_eligibility_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_operator_eligibility_reasons_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignment_audit_events_tenant_entity",
                table: "shift_operator_assignment_audit_events",
                columns: TenantEntityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignment_audit_events_tenant_facility_occurred",
                table: "shift_operator_assignment_audit_events",
                columns: TenantFacilityOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_assignments_eligibility_assessment_id",
                table: "shift_operator_assignments",
                column: "eligibility_assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_assignments_eligibility_result_id",
                table: "shift_operator_assignments",
                column: "eligibility_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_assignments_production_shift_id",
                table: "shift_operator_assignments",
                column: "production_shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_assignments_workforce_person_id",
                table: "shift_operator_assignments",
                column: "workforce_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignments_tenant_create_idempotency",
                table: "shift_operator_assignments",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignments_tenant_facility_order_ref",
                table: "shift_operator_assignments",
                columns: TenantFacilityOrderRefColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignments_tenant_facility_status_date",
                table: "shift_operator_assignments",
                columns: TenantFacilityStatusDateColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_assignments_tenant_person_window",
                table: "shift_operator_assignments",
                columns: TenantPersonWindowColumns,
                unique: true,
                filter: "status = 'approved'");

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_eligibility_assessments_production_shift_id",
                table: "shift_operator_eligibility_assessments",
                column: "production_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_eligibility_assessments_tenant_facility_evaluated",
                table: "shift_operator_eligibility_assessments",
                columns: TenantFacilityEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_eligibility_assessments_tenant_facility_fingerprint",
                table: "shift_operator_eligibility_assessments",
                columns: TenantFacilityFingerprintColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_eligibility_reasons_result_id",
                table: "shift_operator_eligibility_reasons",
                column: "result_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_eligibility_reasons_tenant_result",
                table: "shift_operator_eligibility_reasons",
                columns: TenantResultColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_eligibility_results_assessment_id",
                table: "shift_operator_eligibility_results",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_operator_eligibility_results_workforce_person_id",
                table: "shift_operator_eligibility_results",
                column: "workforce_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_operator_eligibility_results_tenant_assessment_person",
                table: "shift_operator_eligibility_results",
                columns: TenantAssessmentPersonColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-00000000006d',
                     'shift-assign.read',
                     'Read shift operator assignment planning, eligibility, and audit history.',
                     'Active',
                     '2026-08-13T04:00:00Z'),
                    ('10000000-0000-4000-8000-00000000006e',
                     'shift-assign.manage',
                     'Create, approve, and cancel shift operator assignment plans.',
                     'Active',
                     '2026-08-13T04:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE shift_operator_eligibility_assessments
                    ADD CONSTRAINT ck_soa_assessments_planned_window
                        CHECK (planned_end_utc > planned_start_utc),
                    ADD CONSTRAINT ck_soa_assessments_required_competency
                        CHECK (required_competency_code <> '');

                ALTER TABLE shift_operator_eligibility_results
                    ADD CONSTRAINT ck_soa_results_outcome
                        CHECK (outcome IN ('eligible','conditional','ineligible','insufficient-data'));

                ALTER TABLE shift_operator_eligibility_reasons
                    ADD CONSTRAINT ck_soa_reasons_severity
                        CHECK (severity IN ('pass','warn','fail','info'));

                ALTER TABLE shift_operator_assignments
                    ADD CONSTRAINT ck_soa_assignments_status
                        CHECK (status IN ('draft','approved','cancelled')),
                    ADD CONSTRAINT ck_soa_assignments_planned_window
                        CHECK (planned_end_utc > planned_start_utc),
                    ADD CONSTRAINT ck_soa_assignments_version
                        CHECK (version >= 1);

                REVOKE ALL ON TABLE shift_operator_eligibility_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_operator_eligibility_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_operator_eligibility_results FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_operator_eligibility_results TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_operator_eligibility_reasons FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_operator_eligibility_reasons TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_operator_assignments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_operator_assignments TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_operator_assignment_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE shift_operator_assignment_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "shift_operator_eligibility_assessments", "soea");
            ApplyTenantRls(migrationBuilder, "shift_operator_eligibility_results", "soer");
            ApplyTenantRls(migrationBuilder, "shift_operator_eligibility_reasons", "soern");
            ApplyTenantRls(migrationBuilder, "shift_operator_assignments", "soa");
            ApplyTenantRlsAppendOnly(migrationBuilder, "shift_operator_assignment_audit_events", "soaae");

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
                        'shift-assign.assignment.created','shift-assign.assignment.approved','shift-assign.assignment.cancelled'
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
                        'shift-assign.assignment.created','shift-assign.assignment.approved','shift-assign.assignment.cancelled'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_operator_assignment_audit_events");

            migrationBuilder.DropTable(
                name: "shift_operator_assignments");

            migrationBuilder.DropTable(
                name: "shift_operator_eligibility_reasons");

            migrationBuilder.DropTable(
                name: "shift_operator_eligibility_results");

            migrationBuilder.DropTable(
                name: "shift_operator_eligibility_assessments");
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
