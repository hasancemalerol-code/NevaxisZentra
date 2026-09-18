using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddErpOrderInboundAndReconciliation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE erp_entity_mappings
                    ADD COLUMN IF NOT EXISTS internal_entity_id UUID NULL,
                    ADD COLUMN IF NOT EXISTS suggested_internal_entity_id UUID NULL,
                    ADD COLUMN IF NOT EXISTS suggested_reason VARCHAR(100) NULL,
                    ADD COLUMN IF NOT EXISTS version BIGINT NOT NULL DEFAULT 1;

                ALTER TABLE erp_entity_mappings DROP CONSTRAINT IF EXISTS ck_erp_entity_mappings_status;
                ALTER TABLE erp_entity_mappings ADD CONSTRAINT ck_erp_entity_mappings_status
                    CHECK (status IN ('unmapped', 'suggested', 'mapped', 'ignored', 'conflict'));

                ALTER TABLE erp_entity_mappings DROP CONSTRAINT IF EXISTS ck_erp_entity_mappings_version;
                ALTER TABLE erp_entity_mappings ADD CONSTRAINT ck_erp_entity_mappings_version CHECK (version >= 1);

                UPDATE erp_entity_mappings
                SET status = 'unmapped', updated_at_utc = TIMESTAMPTZ '2026-08-09T21:00:00Z'
                WHERE status = 'mapped' AND internal_entity_id IS NULL;

                ALTER TABLE erp_sync_runs DROP CONSTRAINT IF EXISTS ck_erp_sync_runs_entity_type;
                ALTER TABLE erp_sync_runs ADD CONSTRAINT ck_erp_sync_runs_entity_type
                    CHECK (entity_type IN ('customers', 'products', 'orders'));

                CREATE TABLE IF NOT EXISTS erp_orders (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    external_code VARCHAR(100) NULL,
                    order_number VARCHAR(100) NOT NULL,
                    external_customer_id VARCHAR(100) NOT NULL,
                    external_customer_code VARCHAR(100) NULL,
                    order_date_utc TIMESTAMPTZ NOT NULL,
                    requested_delivery_date_utc TIMESTAMPTZ NULL,
                    currency_code VARCHAR(10) NULL,
                    status VARCHAR(40) NOT NULL,
                    reference VARCHAR(200) NULL,
                    notes VARCHAR(1000) NULL,
                    source_version VARCHAR(100) NULL,
                    source_updated_at_utc TIMESTAMPTZ NULL,
                    imported_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_orders PRIMARY KEY (id),
                    CONSTRAINT fk_eor_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eor_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eor_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_orders_tenant_facility_profile_external
                    ON erp_orders (tenant_id, facility_id, connection_profile_id, external_id);

                CREATE TABLE IF NOT EXISTS erp_order_lines (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    order_id UUID NOT NULL,
                    external_line_id VARCHAR(100) NOT NULL,
                    line_number INT NOT NULL,
                    external_product_id VARCHAR(100) NOT NULL,
                    external_product_code VARCHAR(100) NULL,
                    description VARCHAR(500) NULL,
                    quantity NUMERIC(18,4) NOT NULL,
                    unit VARCHAR(30) NOT NULL,
                    requested_delivery_date_utc TIMESTAMPTZ NULL,
                    source_version VARCHAR(100) NULL,
                    CONSTRAINT pk_erp_order_lines PRIMARY KEY (id),
                    CONSTRAINT fk_eol_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eol_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eol_order FOREIGN KEY (order_id) REFERENCES erp_orders(id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_order_lines_tenant_facility_profile_order_external_line
                    ON erp_order_lines (tenant_id, facility_id, connection_profile_id, order_id, external_line_id);

                REVOKE ALL ON TABLE erp_orders FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_orders TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_order_lines FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_order_lines TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "erp_orders", "eor");
            ApplyTenantRls(migrationBuilder, "erp_order_lines", "eol");

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
                        'erp.mapping.confirmed','erp.mapping.ignored'
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
                        'erp.mapping.confirmed','erp.mapping.ignored'
                    ));

                ALTER TABLE outbox_messages
                    DROP CONSTRAINT IF EXISTS ck_outbox_messages_type,
                    ADD CONSTRAINT ck_outbox_messages_type CHECK (
                        message_type IN (
                            'infrastructure.audit.recorded',
                            'infrastructure.ai-decision.recorded',
                            'infrastructure.outbox.probe',
                            'production-plan-submitted-for-review',
                            'production-plan-revision-requested',
                            'production-plan-approved',
                            'production-plan-rejected',
                            'production-plan-scheduled',
                            'production-plan-transfer-requested',
                            'production-plan-transfer-started',
                            'production-plan-transfer-succeeded',
                            'production-plan-transfer-failed',
                            'production-plan-transfer-uncertain',
                            'production-plan-handoff-prepared',
                            'production-order-created',
                            'production-order-started',
                            'production-order-paused',
                            'production-order-resumed',
                            'production-order-completed',
                            'production-order-completed-short',
                            'production-plan-handoff-consumed',
                            'production-order-good-quantity-recorded',
                            'production-order-scrap-recorded',
                            'production-order-quantity-reversed',
                            'production-order-downtime-started',
                            'production-order-downtime-ended',
                            'production-order-setup-started',
                            'production-order-setup-completed',
                            'production-order-manual-cycle-recorded',
                            'production-order-exception-raised',
                            'production-order-exception-acknowledged',
                            'production-order-exception-resolved',
                            'production-order-downtime-recovered',
                            'production-order-setup-recovered',
                            'production-order-shift-assigned',
                            'production-order-shift-reassigned',
                            'production-order-operator-assigned',
                            'production-order-operator-released',
                            'production-order-shift-handover-created',
                            'production-order-exception-assigned',
                            'production-order-exception-unassigned',
                            'planning-logistics-risk-high',
                            'planning-logistics-deadline-breached',
                            'export-case-created',
                            'export-document-registered',
                            'export-document-verified',
                            'export-readiness-changed',
                            'export-ready',
                            'export-blocked',
                            'export-packing-completed',
                            'export-packing-verified',
                            'export-shipment-created',
                            'export-shipment-status-changed',
                            'export-broker-package-sent',
                            'export-broker-workspace-acknowledged',
                            'export-compliance-blocked',
                            'export-compliance-ready',
                            'export-compliance-revalidation-required',
                            'export-country-research-completed',
                            'export-country-rule-pack-verified',
                            'export-country-knowledge-updated',
                            'export-ai-analysis-completed',
                            'export-ai-high-risk-found',
                            'export-ai-recommendation-accepted',
                            'export-ai-task-requested',
                            'quality-inspection-completed',
                            'quality-release-released',
                            'quality-release-rejected',
                            'quality-ncr-created',
                            'quality-hold-placed',
                            'quality-hold-released',
                            'quality-ncr-resolved',
                            'quality-ncr-closed',
                            'quality-corrective-action-completed',
                            'maintenance-request-created',
                            'maintenance-work-order-created',
                            'maintenance-work-order-started',
                            'maintenance-work-order-completed',
                            'maintenance-preventive-due-changed',
                            'maintenance-worknote-created',
                            'maintenance-preventive-due-generated',
                            'maintenance-preventive-workorder-created',
                            'maintenance-partusage-recorded',
                            'erp-connection-created',
                            'erp-sync-completed',
                            'erp-sync-failed',
                            'erp-mapping-confirmed',
                            'erp-sync-orders-completed',
                            'erp-sync-orders-failed'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS erp_order_lines;
                DROP TABLE IF EXISTS erp_orders;
                """);
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
    }
}
