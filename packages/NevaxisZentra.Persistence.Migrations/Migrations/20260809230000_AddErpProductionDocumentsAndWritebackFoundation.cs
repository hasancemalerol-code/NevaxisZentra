using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddErpProductionDocumentsAndWritebackFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000059',
                     'erp.writeback.request',
                     'Create and submit ERP writeback requests.',
                     'Active',
                     '2026-08-09T23:00:00Z'),
                    ('10000000-0000-4000-8000-000000000060',
                     'erp.writeback.approve',
                     'Approve or reject pending ERP writeback requests.',
                     'Active',
                     '2026-08-09T23:00:00Z'),
                    ('10000000-0000-4000-8000-000000000061',
                     'erp.writeback.execute',
                     'Execute approved ERP writeback requests.',
                     'Active',
                     '2026-08-09T23:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE erp_sync_runs DROP CONSTRAINT IF EXISTS ck_erp_sync_runs_entity_type;
                ALTER TABLE erp_sync_runs ADD CONSTRAINT ck_erp_sync_runs_entity_type
                    CHECK (entity_type IN (
                        'customers', 'products', 'orders', 'warehouses', 'inventory', 'purchasing',
                        'production', 'documents'));

                CREATE TABLE IF NOT EXISTS erp_production_orders (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    external_code VARCHAR(100) NULL,
                    production_order_number VARCHAR(100) NOT NULL,
                    external_product_id VARCHAR(100) NOT NULL,
                    external_product_code VARCHAR(100) NULL,
                    product_description VARCHAR(500) NULL,
                    planned_quantity NUMERIC(18,4) NOT NULL,
                    completed_quantity NUMERIC(18,4) NULL,
                    scrap_quantity NUMERIC(18,4) NULL,
                    unit VARCHAR(30) NOT NULL,
                    status VARCHAR(40) NOT NULL,
                    planned_start_utc TIMESTAMPTZ NULL,
                    planned_end_utc TIMESTAMPTZ NULL,
                    actual_start_utc TIMESTAMPTZ NULL,
                    actual_end_utc TIMESTAMPTZ NULL,
                    external_sales_order_id VARCHAR(100) NULL,
                    external_sales_order_code VARCHAR(100) NULL,
                    source_version VARCHAR(100) NULL,
                    source_updated_at_utc TIMESTAMPTZ NULL,
                    imported_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_production_orders PRIMARY KEY (id),
                    CONSTRAINT fk_epr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_epr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_production_orders_tenant_facility_profile_external
                    ON erp_production_orders (tenant_id, facility_id, connection_profile_id, external_id);

                CREATE TABLE IF NOT EXISTS erp_documents (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    external_code VARCHAR(100) NULL,
                    document_type VARCHAR(40) NOT NULL,
                    document_number VARCHAR(100) NULL,
                    document_date_utc TIMESTAMPTZ NULL,
                    status VARCHAR(40) NULL,
                    related_entity_type VARCHAR(30) NULL,
                    related_external_entity_id VARCHAR(100) NULL,
                    related_external_entity_code VARCHAR(100) NULL,
                    file_name VARCHAR(300) NULL,
                    mime_type VARCHAR(100) NULL,
                    content_available BOOLEAN NOT NULL DEFAULT FALSE,
                    external_reference VARCHAR(200) NULL,
                    source_version VARCHAR(100) NULL,
                    source_updated_at_utc TIMESTAMPTZ NULL,
                    imported_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_documents PRIMARY KEY (id),
                    CONSTRAINT fk_edo_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_edo_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_documents_tenant_facility_profile_external
                    ON erp_documents (tenant_id, facility_id, connection_profile_id, external_id);

                CREATE TABLE IF NOT EXISTS erp_writeback_requests (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    command_type VARCHAR(100) NOT NULL,
                    target_entity_type VARCHAR(30) NULL,
                    target_external_id VARCHAR(100) NULL,
                    payload_json JSONB NOT NULL,
                    payload_hash VARCHAR(64) NOT NULL,
                    idempotency_key VARCHAR(500) NOT NULL,
                    status VARCHAR(40) NOT NULL,
                    requested_by_subject_id VARCHAR(200) NOT NULL,
                    requested_at_utc TIMESTAMPTZ NOT NULL,
                    submitted_at_utc TIMESTAMPTZ NULL,
                    approved_payload_hash VARCHAR(64) NULL,
                    approved_version BIGINT NULL,
                    executed_at_utc TIMESTAMPTZ NULL,
                    external_execution_reference VARCHAR(200) NULL,
                    last_error_code VARCHAR(100) NULL,
                    last_error_message VARCHAR(500) NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_erp_writeback_requests PRIMARY KEY (id),
                    CONSTRAINT fk_ewr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ewr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ewr_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_erp_writeback_requests_status CHECK (status IN (
                        'draft', 'pending-approval', 'approved', 'rejected', 'executing',
                        'succeeded', 'failed', 'blocked', 'superseded')),
                    CONSTRAINT ck_erp_writeback_requests_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_erp_writeback_requests_payload CHECK (
                        jsonb_typeof(payload_json) = 'object' AND octet_length(payload_json::text) <= 16384),
                    CONSTRAINT ck_erp_writeback_requests_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_writeback_requests_tenant_facility_profile_command_idempotency
                    ON erp_writeback_requests (tenant_id, facility_id, connection_profile_id, command_type, idempotency_key);

                CREATE TABLE IF NOT EXISTS erp_writeback_approvals (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    writeback_request_id UUID NOT NULL,
                    decision VARCHAR(30) NOT NULL,
                    decided_by_subject_id VARCHAR(200) NOT NULL,
                    decided_at_utc TIMESTAMPTZ NOT NULL,
                    request_version BIGINT NOT NULL,
                    payload_hash VARCHAR(64) NOT NULL,
                    comment VARCHAR(1000) NULL,
                    CONSTRAINT pk_erp_writeback_approvals PRIMARY KEY (id),
                    CONSTRAINT fk_ewa_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ewa_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ewa_writeback_request FOREIGN KEY (writeback_request_id) REFERENCES erp_writeback_requests(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_erp_writeback_approvals_decision CHECK (decision IN ('approved', 'rejected')),
                    CONSTRAINT ck_erp_writeback_approvals_request_version CHECK (request_version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_erp_writeback_approvals_tenant_facility_request_decided
                    ON erp_writeback_approvals (tenant_id, facility_id, writeback_request_id, decided_at_utc);

                REVOKE ALL ON TABLE erp_production_orders FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_production_orders TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_documents FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_documents TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_writeback_requests FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_writeback_requests TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_writeback_approvals FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE erp_writeback_approvals TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "erp_production_orders", "epr");
            ApplyTenantRls(migrationBuilder, "erp_documents", "edo");
            ApplyTenantRls(migrationBuilder, "erp_writeback_requests", "ewr");
            ApplyTenantRlsAppendOnly(migrationBuilder, "erp_writeback_approvals", "ewa");

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
                        'erp.writeback.execution.failed','erp.writeback.execution.blocked'
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
                        'erp.writeback.execution.failed','erp.writeback.execution.blocked'
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
                            'erp-sync-orders-failed',
                            'erp-sync-warehouses-completed',
                            'erp-sync-warehouses-failed',
                            'erp-sync-inventory-completed',
                            'erp-sync-inventory-failed',
                            'erp-sync-purchasing-completed',
                            'erp-sync-purchasing-failed',
                            'erp-sync-production-completed',
                            'erp-sync-production-failed',
                            'erp-sync-documents-completed',
                            'erp-sync-documents-failed',
                            'erp-writeback-submitted',
                            'erp-writeback-approved',
                            'erp-writeback-rejected',
                            'erp-writeback-succeeded',
                            'erp-writeback-failed'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS erp_writeback_approvals;
                DROP TABLE IF EXISTS erp_writeback_requests;
                DROP TABLE IF EXISTS erp_documents;
                DROP TABLE IF EXISTS erp_production_orders;

                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000059',
                    '10000000-0000-4000-8000-000000000060',
                    '10000000-0000-4000-8000-000000000061');
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
