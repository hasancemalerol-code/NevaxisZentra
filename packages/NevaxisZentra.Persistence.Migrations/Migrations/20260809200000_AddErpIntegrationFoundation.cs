using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddErpIntegrationFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000056',
                     'erp.read',
                     'Read ERP connection profiles, sync runs, and imported master data.',
                     'Active',
                     '2026-08-09T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000057',
                     'erp.manage',
                     'Create and update ERP connection profiles and entity mappings.',
                     'Active',
                     '2026-08-09T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000058',
                     'erp.sync',
                     'Trigger and retry ERP synchronization runs.',
                     'Active',
                     '2026-08-09T20:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS erp_connection_profiles (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    external_company_id VARCHAR(100) NULL,
                    external_company_code VARCHAR(100) NULL,
                    base_endpoint VARCHAR(500) NULL,
                    connection_mode VARCHAR(30) NOT NULL,
                    credential_reference VARCHAR(200) NULL,
                    status VARCHAR(30) NOT NULL,
                    last_successful_connection_at_utc TIMESTAMPTZ NULL,
                    last_failed_connection_at_utc TIMESTAMPTZ NULL,
                    last_health_check_at_utc TIMESTAMPTZ NULL,
                    last_health_latency_ms INT NULL,
                    last_error_code VARCHAR(100) NULL,
                    last_error_message VARCHAR(500) NULL,
                    last_successful_sync_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_erp_connection_profiles PRIMARY KEY (id),
                    CONSTRAINT fk_ecp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ecp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_erp_connection_profiles_provider CHECK (provider IN ('logo', 'mikro', 'netsis', 'simulator')),
                    CONSTRAINT ck_erp_connection_profiles_connection_mode CHECK (connection_mode IN ('api', 'database', 'file', 'simulator')),
                    CONSTRAINT ck_erp_connection_profiles_status CHECK (status IN ('draft', 'configured', 'healthy', 'degraded', 'unavailable', 'disabled')),
                    CONSTRAINT ck_erp_connection_profiles_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_connection_profiles_tenant_facility_name
                    ON erp_connection_profiles (tenant_id, facility_id, name);

                CREATE TABLE IF NOT EXISTS erp_sync_runs (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    entity_type VARCHAR(30) NOT NULL,
                    started_at_utc TIMESTAMPTZ NOT NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    status VARCHAR(40) NOT NULL,
                    records_read INT NOT NULL,
                    records_created INT NOT NULL,
                    records_updated INT NOT NULL,
                    records_skipped INT NOT NULL,
                    records_failed INT NOT NULL,
                    cursor VARCHAR(200) NULL,
                    error_code VARCHAR(100) NULL,
                    error_message VARCHAR(500) NULL,
                    triggered_by_subject_id VARCHAR(200) NOT NULL,
                    retried_from_sync_run_id UUID NULL,
                    CONSTRAINT pk_erp_sync_runs PRIMARY KEY (id),
                    CONSTRAINT fk_esr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_esr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_esr_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_erp_sync_runs_entity_type CHECK (entity_type IN ('customers', 'products')),
                    CONSTRAINT ck_erp_sync_runs_status CHECK (status IN ('running', 'completed', 'completed-with-errors', 'failed'))
                );

                CREATE INDEX IF NOT EXISTS ix_erp_sync_runs_tenant_facility_started
                    ON erp_sync_runs (tenant_id, facility_id, started_at_utc);

                CREATE TABLE IF NOT EXISTS erp_integration_errors (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    sync_run_id UUID NOT NULL,
                    code VARCHAR(100) NOT NULL,
                    entity_type VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NULL,
                    message VARCHAR(500) NOT NULL,
                    occurred_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_integration_errors PRIMARY KEY (id),
                    CONSTRAINT fk_eie_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eie_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eie_sync_run FOREIGN KEY (sync_run_id) REFERENCES erp_sync_runs(id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS erp_customers (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    external_code VARCHAR(100) NULL,
                    name VARCHAR(300) NOT NULL,
                    tax_number VARCHAR(50) NULL,
                    tax_office VARCHAR(100) NULL,
                    email VARCHAR(200) NULL,
                    phone VARCHAR(50) NULL,
                    country_code VARCHAR(10) NULL,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    source_version VARCHAR(100) NULL,
                    source_updated_at_utc TIMESTAMPTZ NULL,
                    imported_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_customers PRIMARY KEY (id),
                    CONSTRAINT fk_ecu_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ecu_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ecu_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_customers_tenant_facility_profile_external
                    ON erp_customers (tenant_id, facility_id, connection_profile_id, external_id);

                CREATE TABLE IF NOT EXISTS erp_products (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    provider VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    external_code VARCHAR(100) NULL,
                    name VARCHAR(300) NOT NULL,
                    unit VARCHAR(30) NULL,
                    barcode VARCHAR(100) NULL,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    source_version VARCHAR(100) NULL,
                    source_updated_at_utc TIMESTAMPTZ NULL,
                    imported_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_products PRIMARY KEY (id),
                    CONSTRAINT fk_epr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_epr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_epr_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_products_tenant_facility_profile_external
                    ON erp_products (tenant_id, facility_id, connection_profile_id, external_id);

                CREATE TABLE IF NOT EXISTS erp_entity_mappings (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    connection_profile_id UUID NOT NULL,
                    entity_type VARCHAR(30) NOT NULL,
                    external_id VARCHAR(100) NOT NULL,
                    canonical_entity_id UUID NULL,
                    status VARCHAR(30) NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_erp_entity_mappings PRIMARY KEY (id),
                    CONSTRAINT fk_eem_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eem_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eem_connection_profile FOREIGN KEY (connection_profile_id) REFERENCES erp_connection_profiles(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_erp_entity_mappings_entity_type CHECK (entity_type IN ('customers', 'products')),
                    CONSTRAINT ck_erp_entity_mappings_status CHECK (status IN ('unmapped', 'mapped', 'ignored'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_erp_entity_mappings_tenant_facility_profile_type_external
                    ON erp_entity_mappings (tenant_id, facility_id, connection_profile_id, entity_type, external_id);

                REVOKE ALL ON TABLE erp_connection_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_connection_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_sync_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_sync_runs TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_integration_errors FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_integration_errors TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_customers FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_customers TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_products FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_products TO crevafactos_runtime;
                REVOKE ALL ON TABLE erp_entity_mappings FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE erp_entity_mappings TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "erp_connection_profiles", "ecp");
            ApplyTenantRls(migrationBuilder, "erp_sync_runs", "esr");
            ApplyTenantRls(migrationBuilder, "erp_integration_errors", "eie");
            ApplyTenantRls(migrationBuilder, "erp_customers", "ecu");
            ApplyTenantRls(migrationBuilder, "erp_products", "epr");
            ApplyTenantRls(migrationBuilder, "erp_entity_mappings", "eem");

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
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated',
                        'maintenance.worknote.created','maintenance.preventive.due.generated',
                        'maintenance.preventive.due.workordercreated','maintenance.partusage.recorded',
                        'erp.connection.created','erp.connection.updated','erp.connection.health.checked',
                        'erp.sync.started','erp.sync.completed','erp.sync.failed'
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
                        'maintenance.preventiveplan.created','maintenance.preventiveplan.updated',
                        'maintenance.worknote.created','maintenance.preventive.due.generated',
                        'maintenance.preventive.due.workordercreated','maintenance.partusage.recorded',
                        'erp.connection.created','erp.connection.updated','erp.connection.health.checked',
                        'erp.sync.started','erp.sync.completed','erp.sync.failed'
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
                            'maintenance-preventive-due-changed',
                            'maintenance-worknote-created',
                            'maintenance-preventive-due-generated',
                            'maintenance-preventive-workorder-created',
                            'maintenance-partusage-recorded',
                            'erp-connection-created',
                            'erp-sync-completed',
                            'erp-sync-failed'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS erp_entity_mappings;
                DROP TABLE IF EXISTS erp_products;
                DROP TABLE IF EXISTS erp_customers;
                DROP TABLE IF EXISTS erp_integration_errors;
                DROP TABLE IF EXISTS erp_sync_runs;
                DROP TABLE IF EXISTS erp_connection_profiles;
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
