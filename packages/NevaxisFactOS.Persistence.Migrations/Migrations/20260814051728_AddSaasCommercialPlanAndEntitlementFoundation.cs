using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasCommercialPlanAndEntitlementFoundation : Migration
    {
        private static readonly string[] PlanVersionMethodTypeColumns = ["plan_version_id", "method_type"];
        private static readonly string[] PlanVersionEntitlementColumns = ["plan_version_id", "entitlement_definition_id"];
        private static readonly string[] PlanCadenceEffectiveColumns = ["plan_id", "billing_cadence", "effective_from_utc"];
        private static readonly string[] PlanVersionNumberColumns = ["plan_id", "version_number"];
        private static readonly string[] PlanVersionComponentTypeColumns = ["plan_version_id", "component_type"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_commercial_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_saas_commercial_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_entitlement_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code_normalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_tr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_entitlement_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    plan_code_normalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_tr = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    lifecycle_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    availability_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    billing_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trial_days = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sellable = table.Column<bool>(type: "boolean", nullable: false),
                    publication_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    commercial_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    provenance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_plan_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_commercial_plan_versions_saas_commercial_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "saas_commercial_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_grace_period_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grace_period_days = table.Column<int>(type: "integer", nullable: false),
                    restricted_behavior_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    suspension_eligibility_policy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operator_explanation_tr = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_grace_period_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_commercial_grace_period_policies_saas_commercial_plan_~",
                        column: x => x.plan_version_id,
                        principalTable: "saas_commercial_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_payment_method_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    manual_verification_required = table.Column<bool>(type: "boolean", nullable: false),
                    recurring_capable = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    effective_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_payment_method_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_commercial_payment_method_capabilities_saas_commercial~",
                        column: x => x.plan_version_id,
                        principalTable: "saas_commercial_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_plan_version_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    semantics = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    integer_limit_value = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_plan_version_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_commercial_plan_version_entitlements_saas_commercial_e~",
                        column: x => x.entitlement_definition_id,
                        principalTable: "saas_commercial_entitlement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_commercial_plan_version_entitlements_saas_commercial_p~",
                        column: x => x.plan_version_id,
                        principalTable: "saas_commercial_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_commercial_price_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_commercial_price_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_commercial_price_components_saas_commercial_plan_versi~",
                        column: x => x.plan_version_id,
                        principalTable: "saas_commercial_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_audit_events_occurred_at_utc",
                table: "saas_commercial_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_entitlement_definitions_code_normalized",
                table: "saas_commercial_entitlement_definitions",
                column: "code_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_grace_period_policies_plan_version",
                table: "saas_commercial_grace_period_policies",
                column: "plan_version_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_payment_method_capabilities_version_type",
                table: "saas_commercial_payment_method_capabilities",
                columns: PlanVersionMethodTypeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_commercial_plan_version_entitlements_entitlement_defin~",
                table: "saas_commercial_plan_version_entitlements",
                column: "entitlement_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_plan_version_entitlements_version_definition",
                table: "saas_commercial_plan_version_entitlements",
                columns: PlanVersionEntitlementColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_plan_versions_plan_cadence_effective",
                table: "saas_commercial_plan_versions",
                columns: PlanCadenceEffectiveColumns);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_plan_versions_plan_version",
                table: "saas_commercial_plan_versions",
                columns: PlanVersionNumberColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_plans_plan_code_normalized",
                table: "saas_commercial_plans",
                column: "plan_code_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saas_commercial_price_components_version_type",
                table: "saas_commercial_price_components",
                columns: PlanVersionComponentTypeColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000071',
                     'saas-commercial.read',
                     'Read SaaS commercial plan catalog, entitlements, and payment-method capabilities.',
                     'Active',
                     '2026-08-14T08:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE saas_commercial_plans
                    ADD CONSTRAINT ck_sc_plans_lifecycle_state
                        CHECK (lifecycle_state IN ('draft','active','retired')),
                    ADD CONSTRAINT ck_sc_plans_availability_state
                        CHECK (availability_state IN ('draft','available','unavailable')),
                    ADD CONSTRAINT ck_sc_plans_version
                        CHECK (version >= 1);

                ALTER TABLE saas_commercial_plan_versions
                    ADD CONSTRAINT ck_sc_plan_versions_billing_cadence
                        CHECK (billing_cadence IN ('monthly','annual')),
                    ADD CONSTRAINT ck_sc_plan_versions_publication_status
                        CHECK (publication_status IN ('draft','published','retired')),
                    ADD CONSTRAINT ck_sc_plan_versions_currency_code
                        CHECK (currency_code ~ '^[A-Z]{3}$'),
                    ADD CONSTRAINT ck_sc_plan_versions_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ex_sc_plan_versions_no_overlap
                        EXCLUDE USING gist (
                            plan_id WITH =,
                            billing_cadence WITH =,
                            tstzrange(
                                effective_from_utc,
                                COALESCE(effective_until_utc, 'infinity'::timestamptz),
                                '[)') WITH &&
                        );

                ALTER TABLE saas_commercial_plan_version_entitlements
                    ADD CONSTRAINT ck_sc_plan_version_entitlements_semantics
                        CHECK (semantics IN ('unavailable','limited','unlimited','unknown'));

                ALTER TABLE saas_commercial_payment_method_capabilities
                    ADD CONSTRAINT ck_sc_payment_method_capabilities_method_type
                        CHECK (method_type IN ('card','bank-transfer','enterprise-invoice'));

                ALTER TABLE saas_commercial_price_components
                    ADD CONSTRAINT ck_sc_price_components_currency_code
                        CHECK (currency_code ~ '^[A-Z]{3}$');

                REVOKE ALL ON TABLE saas_commercial_plans FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_plans TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_plan_versions FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_plan_versions TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_price_components FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_price_components TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_entitlement_definitions FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_entitlement_definitions TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_plan_version_entitlements FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_plan_version_entitlements TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_payment_method_capabilities FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_payment_method_capabilities TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_grace_period_policies FROM crevafactos_runtime;
                GRANT SELECT ON TABLE saas_commercial_grace_period_policies TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_commercial_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_commercial_audit_events TO crevafactos_runtime;

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
                        'shift-risk.requirement.created','shift-risk.requirement.updated','shift-risk.assessment.evaluated',
                        'saas-commercial.plan.created','saas-commercial.plan.updated',
                        'saas-commercial.version.created','saas-commercial.version.published','saas-commercial.version.retired'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id = '10000000-0000-4000-8000-000000000071';
                """);

            migrationBuilder.DropTable(
                name: "saas_commercial_audit_events");

            migrationBuilder.DropTable(
                name: "saas_commercial_grace_period_policies");

            migrationBuilder.DropTable(
                name: "saas_commercial_payment_method_capabilities");

            migrationBuilder.DropTable(
                name: "saas_commercial_plan_version_entitlements");

            migrationBuilder.DropTable(
                name: "saas_commercial_price_components");

            migrationBuilder.DropTable(
                name: "saas_commercial_entitlement_definitions");

            migrationBuilder.DropTable(
                name: "saas_commercial_plan_versions");

            migrationBuilder.DropTable(
                name: "saas_commercial_plans");
        }
    }
}
