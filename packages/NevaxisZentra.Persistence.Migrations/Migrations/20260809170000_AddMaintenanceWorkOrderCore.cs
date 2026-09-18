using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddMaintenanceWorkOrderCore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000053',
                     'maintenance.read',
                     'Read maintenance requests, work orders, and preventive plans.',
                     'Active',
                     '2026-08-09T17:00:00Z'),
                    ('10000000-0000-4000-8000-000000000054',
                     'maintenance.manage',
                     'Create and update maintenance requests, work orders, and preventive plans.',
                     'Active',
                     '2026-08-09T17:00:00Z'),
                    ('10000000-0000-4000-8000-000000000055',
                     'maintenance.execute',
                     'Execute maintenance work order lifecycle and technician actions.',
                     'Active',
                     '2026-08-09T17:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS maintenance_requests (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    request_number VARCHAR(50) NOT NULL,
                    asset_type VARCHAR(50) NOT NULL,
                    asset_id VARCHAR(200) NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    asset_name VARCHAR(200) NULL,
                    production_order_id UUID NULL,
                    type VARCHAR(30) NOT NULL,
                    priority VARCHAR(20) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    title VARCHAR(200) NOT NULL,
                    description VARCHAR(4000) NOT NULL,
                    reported_at_utc TIMESTAMPTZ NOT NULL,
                    reported_by_subject_id VARCHAR(200) NOT NULL,
                    source VARCHAR(50) NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_maintenance_requests PRIMARY KEY (id),
                    CONSTRAINT fk_mr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_maintenance_requests_type CHECK (type IN ('fault', 'corrective', 'preventive', 'inspection', 'other')),
                    CONSTRAINT ck_maintenance_requests_priority CHECK (priority IN ('low', 'normal', 'high', 'critical')),
                    CONSTRAINT ck_maintenance_requests_status CHECK (status IN ('open', 'triaged', 'converted-to-work-order', 'closed', 'cancelled')),
                    CONSTRAINT ck_maintenance_requests_asset_type CHECK (asset_type IN ('machine', 'mold', 'auxiliary-equipment')),
                    CONSTRAINT ck_maintenance_requests_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_maintenance_requests_tenant_facility_number
                    ON maintenance_requests (tenant_id, facility_id, request_number);

                CREATE TABLE IF NOT EXISTS maintenance_work_orders (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    work_order_number VARCHAR(50) NOT NULL,
                    maintenance_request_id UUID NULL,
                    asset_type VARCHAR(50) NOT NULL,
                    asset_id VARCHAR(200) NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    asset_name VARCHAR(200) NULL,
                    production_order_id UUID NULL,
                    type VARCHAR(30) NOT NULL,
                    priority VARCHAR(20) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    title VARCHAR(200) NOT NULL,
                    description VARCHAR(4000) NOT NULL,
                    assigned_technician_subject_id VARCHAR(200) NULL,
                    planned_start_utc TIMESTAMPTZ NULL,
                    planned_end_utc TIMESTAMPTZ NULL,
                    started_at_utc TIMESTAMPTZ NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    completion_note VARCHAR(2000) NULL,
                    resolution_code VARCHAR(30) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_maintenance_work_orders PRIMARY KEY (id),
                    CONSTRAINT fk_mwo_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mwo_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mwo_request FOREIGN KEY (maintenance_request_id) REFERENCES maintenance_requests(id) ON DELETE SET NULL,
                    CONSTRAINT ck_maintenance_work_orders_type CHECK (type IN ('fault', 'corrective', 'preventive', 'inspection', 'other')),
                    CONSTRAINT ck_maintenance_work_orders_priority CHECK (priority IN ('low', 'normal', 'high', 'critical')),
                    CONSTRAINT ck_maintenance_work_orders_status CHECK (status IN ('draft', 'planned', 'assigned', 'in-progress', 'waiting', 'completed', 'cancelled')),
                    CONSTRAINT ck_maintenance_work_orders_asset_type CHECK (asset_type IN ('machine', 'mold', 'auxiliary-equipment')),
                    CONSTRAINT ck_maintenance_work_orders_resolution CHECK (resolution_code IS NULL OR resolution_code IN ('repaired', 'adjusted', 'cleaned', 'replaced-part', 'temporary-fix', 'no-fault-found', 'other')),
                    CONSTRAINT ck_maintenance_work_orders_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_maintenance_work_orders_tenant_facility_number
                    ON maintenance_work_orders (tenant_id, facility_id, work_order_number);

                CREATE TABLE IF NOT EXISTS preventive_maintenance_plans (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    code VARCHAR(50) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    asset_type VARCHAR(50) NOT NULL,
                    asset_id VARCHAR(200) NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    interval_type VARCHAR(20) NOT NULL,
                    interval_value INT NOT NULL,
                    next_due_at_utc TIMESTAMPTZ NULL,
                    status VARCHAR(20) NOT NULL,
                    description VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_preventive_maintenance_plans PRIMARY KEY (id),
                    CONSTRAINT fk_pmp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_pmp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_preventive_maintenance_plans_asset_type CHECK (asset_type IN ('machine', 'mold', 'auxiliary-equipment')),
                    CONSTRAINT ck_preventive_maintenance_plans_interval_type CHECK (interval_type IN ('days', 'weeks', 'months')),
                    CONSTRAINT ck_preventive_maintenance_plans_status CHECK (status IN ('active', 'inactive')),
                    CONSTRAINT ck_preventive_maintenance_plans_interval_value CHECK (interval_value >= 1),
                    CONSTRAINT ck_preventive_maintenance_plans_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_preventive_maintenance_plans_tenant_facility_code
                    ON preventive_maintenance_plans (tenant_id, facility_id, code);

                REVOKE ALL ON TABLE maintenance_requests FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_requests TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_work_orders FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_work_orders TO crevafactos_runtime;
                REVOKE ALL ON TABLE preventive_maintenance_plans FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE preventive_maintenance_plans TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "maintenance_requests", "mr");
            ApplyTenantRls(migrationBuilder, "maintenance_work_orders", "mwo");
            ApplyTenantRls(migrationBuilder, "preventive_maintenance_plans", "pmp");

            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_action_taxonomy;
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
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated'
                    ));

                ALTER TABLE audit_records DROP CONSTRAINT ck_audit_records_event_taxonomy;
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
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated'
                    ));

                ALTER TABLE outbox_messages
                    DROP CONSTRAINT ck_outbox_messages_type,
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
                            'maintenance-preventive-due-changed'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS preventive_maintenance_plans;
                DROP TABLE IF EXISTS maintenance_work_orders;
                DROP TABLE IF EXISTS maintenance_requests;
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
