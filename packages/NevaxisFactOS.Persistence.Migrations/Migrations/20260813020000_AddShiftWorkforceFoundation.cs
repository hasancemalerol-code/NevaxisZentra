using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftWorkforceFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantEntityColumns = ["tenant_id", "entity_type", "entity_id"];
        private static readonly string[] TenantFacilityOccurredColumns = ["tenant_id", "facility_id", "occurred_at_utc"];
        private static readonly string[] TenantFacilityPersonColumns = ["tenant_id", "facility_id", "workforce_person_id"];
        private static readonly string[] TenantPersonIdempotencyColumns = ["tenant_id", "workforce_person_id", "create_idempotency_key"];
        private static readonly string[] TenantCodeColumns = ["tenant_id", "code"];
        private static readonly string[] TenantFacilityActiveColumns = ["tenant_id", "facility_id", "is_active"];
        private static readonly string[] TenantFacilityAvailabilityColumns = ["tenant_id", "facility_id", "availability_state"];
        private static readonly string[] TenantFacilityFreshnessColumns = ["tenant_id", "facility_id", "freshness_state"];
        private static readonly string[] TenantSourceEmployeeCodeColumns = ["tenant_id", "source_system", "employee_code_normalized"];
        private static readonly string[] TenantSourceExternalRefColumns = ["tenant_id", "source_system", "external_employee_ref"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_workforce_audit_events",
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
                    table.PrimaryKey("PK_shift_workforce_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_workforce_audit_events_facilities_tenant_id_facility_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_workforce_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_workforce_competency_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_label_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_tr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_workforce_competency_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_workforce_competency_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_workforce_persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_employee_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employee_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employee_code_normalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    availability_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_shift_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_calendar_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    synchronized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_limitations_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_workforce_persons", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_workforce_persons_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_workforce_persons_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_workforce_competency_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workforce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_family_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    process_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    material_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    equipment_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    competency_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    operational_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    create_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    create_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_workforce_competency_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_workforce_competency_assignments_facilities_tenant_id~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_workforce_competency_assignments_shift_workforce_comp~",
                        column: x => x.competency_definition_id,
                        principalTable: "shift_workforce_competency_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_workforce_competency_assignments_shift_workforce_pers~",
                        column: x => x.workforce_person_id,
                        principalTable: "shift_workforce_persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_workforce_competency_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_audit_events_tenant_entity",
                table: "shift_workforce_audit_events",
                columns: TenantEntityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_audit_events_tenant_facility_occurred",
                table: "shift_workforce_audit_events",
                columns: TenantFacilityOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_shift_workforce_competency_assignments_competency_definitio~",
                table: "shift_workforce_competency_assignments",
                column: "competency_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_workforce_competency_assignments_workforce_person_id",
                table: "shift_workforce_competency_assignments",
                column: "workforce_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_competency_assignments_tenant_facility_person",
                table: "shift_workforce_competency_assignments",
                columns: TenantFacilityPersonColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_competency_assignments_tenant_person_idempotency",
                table: "shift_workforce_competency_assignments",
                columns: TenantPersonIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_competency_definitions_tenant_code",
                table: "shift_workforce_competency_definitions",
                columns: TenantCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_persons_tenant_facility_active",
                table: "shift_workforce_persons",
                columns: TenantFacilityActiveColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_persons_tenant_facility_availability",
                table: "shift_workforce_persons",
                columns: TenantFacilityAvailabilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_persons_tenant_facility_freshness",
                table: "shift_workforce_persons",
                columns: TenantFacilityFreshnessColumns);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_persons_tenant_source_employee_code",
                table: "shift_workforce_persons",
                columns: TenantSourceEmployeeCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_workforce_persons_tenant_source_external_ref",
                table: "shift_workforce_persons",
                columns: TenantSourceExternalRefColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-00000000006b',
                     'shift-workforce.read',
                     'Read shift workforce persons, competencies, and source freshness.',
                     'Active',
                     '2026-08-13T02:00:00Z'),
                    ('10000000-0000-4000-8000-00000000006c',
                     'shift-workforce.manage',
                     'Synchronize workforce source data and manage competency enrichment.',
                     'Active',
                     '2026-08-13T02:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE shift_workforce_persons
                    ADD CONSTRAINT ck_swf_persons_availability_state
                        CHECK (availability_state IN ('available','leave','absent','unknown')),
                    ADD CONSTRAINT ck_swf_persons_freshness_state
                        CHECK (freshness_state IN ('fresh','stale','unknown')),
                    ADD CONSTRAINT ck_swf_persons_source_system
                        CHECK (source_system <> ''),
                    ADD CONSTRAINT ck_swf_persons_version
                        CHECK (version >= 1);

                ALTER TABLE shift_workforce_competency_assignments
                    ADD CONSTRAINT ck_swf_assignments_competency_level
                        CHECK (competency_level IN ('basic','competent','proficient','expert')),
                    ADD CONSTRAINT ck_swf_assignments_verification_status
                        CHECK (verification_status IN ('verified','pending-verification','expired','rejected')),
                    ADD CONSTRAINT ck_swf_assignments_version
                        CHECK (version >= 1);

                REVOKE ALL ON TABLE shift_workforce_persons FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_workforce_persons TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_workforce_competency_definitions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_workforce_competency_definitions TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_workforce_competency_assignments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_workforce_competency_assignments TO crevafactos_runtime;
                REVOKE ALL ON TABLE shift_workforce_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE shift_workforce_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "shift_workforce_persons", "swfp");
            ApplyTenantRls(migrationBuilder, "shift_workforce_competency_definitions", "swfcd");
            ApplyTenantRls(migrationBuilder, "shift_workforce_competency_assignments", "swfca");
            ApplyTenantRlsAppendOnly(migrationBuilder, "shift_workforce_audit_events", "swfae");

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
                        'shift-workforce.person.synchronized','shift-workforce.competency.created','shift-workforce.competency.updated'
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
                        'shift-workforce.person.synchronized','shift-workforce.competency.created','shift-workforce.competency.updated'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_workforce_audit_events");

            migrationBuilder.DropTable(
                name: "shift_workforce_competency_assignments");

            migrationBuilder.DropTable(
                name: "shift_workforce_competency_definitions");

            migrationBuilder.DropTable(
                name: "shift_workforce_persons");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
