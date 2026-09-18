using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddEdgeGatewayMachineFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000062',
                     'edge.read',
                     'Read Edge gateways, machine registry, events, and runtime state.',
                     'Active',
                     '2026-08-10T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000063',
                     'edge.manage',
                     'Create and update Edge gateway registrations and machine assignments.',
                     'Active',
                     '2026-08-10T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000064',
                     'edge.ingest',
                     'Ingest machine telemetry events from Edge gateways and simulators.',
                     'Active',
                     '2026-08-10T12:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS edge_gateways (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    gateway_code VARCHAR(50) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(500) NULL,
                    vendor_key VARCHAR(30) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    last_seen_at_utc TIMESTAMPTZ NULL,
                    last_heartbeat_at_utc TIMESTAMPTZ NULL,
                    last_health_message VARCHAR(500) NULL,
                    credential_reference VARCHAR(200) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_edge_gateways PRIMARY KEY (id),
                    CONSTRAINT fk_eg_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_eg_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_edge_gateways_vendor_key CHECK (vendor_key IN ('simulator', 'mqtt-bridge', 'opcua-bridge')),
                    CONSTRAINT ck_edge_gateways_status CHECK (status IN ('unknown', 'online', 'offline', 'degraded', 'disabled')),
                    CONSTRAINT ck_edge_gateways_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_edge_gateways_tenant_facility_code
                    ON edge_gateways (tenant_id, facility_id, gateway_code);

                CREATE TABLE IF NOT EXISTS machine_registry (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_code VARCHAR(50) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(500) NULL,
                    asset_class VARCHAR(50) NULL,
                    asset_reference VARCHAR(200) NULL,
                    status VARCHAR(20) NOT NULL,
                    is_ready_for_production BOOLEAN NOT NULL DEFAULT FALSE,
                    last_seen_at_utc TIMESTAMPTZ NULL,
                    last_event_at_utc TIMESTAMPTZ NULL,
                    last_known_status VARCHAR(20) NULL,
                    last_known_status_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_machine_registry PRIMARY KEY (id),
                    CONSTRAINT fk_mr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_registry_status CHECK (status IN ('unknown', 'running', 'idle', 'stopped', 'fault', 'maintenance')),
                    CONSTRAINT ck_machine_registry_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_registry_tenant_facility_code
                    ON machine_registry (tenant_id, facility_id, machine_code);

                CREATE TABLE IF NOT EXISTS machine_gateway_assignments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    adapter_key VARCHAR(100) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    assigned_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_gateway_assignments PRIMARY KEY (id),
                    CONSTRAINT fk_mga_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mga_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mga_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mga_gateway FOREIGN KEY (gateway_id) REFERENCES edge_gateways(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_gateway_assignments_status CHECK (status IN ('active', 'inactive'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_gateway_assignments_tenant_facility_machine_gateway
                    ON machine_gateway_assignments (tenant_id, facility_id, machine_id, gateway_id);

                CREATE TABLE IF NOT EXISTS machine_events (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    event_kind VARCHAR(30) NOT NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NOT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    payload VARCHAR(4096) NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    duplicate_of_idempotency_key VARCHAR(200) NULL,
                    CONSTRAINT pk_machine_events PRIMARY KEY (id),
                    CONSTRAINT fk_me_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_me_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_me_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_me_gateway FOREIGN KEY (gateway_id) REFERENCES edge_gateways(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_events_event_kind CHECK (event_kind IN (
                        'heartbeat', 'status-change', 'telemetry', 'alarm', 'fault', 'cycle-start', 'cycle-end'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_events_tenant_facility_idempotency
                    ON machine_events (tenant_id, facility_id, idempotency_key);

                CREATE INDEX IF NOT EXISTS ix_machine_events_tenant_facility_machine_sequence
                    ON machine_events (tenant_id, facility_id, machine_id, source_sequence);

                CREATE TABLE IF NOT EXISTS machine_runtime (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    is_ready_for_production BOOLEAN NOT NULL DEFAULT FALSE,
                    last_seen_at_utc TIMESTAMPTZ NULL,
                    last_event_id UUID NULL,
                    last_source_sequence BIGINT NULL,
                    last_sequence_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_runtime PRIMARY KEY (id),
                    CONSTRAINT fk_mrt_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrt_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrt_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_runtime_status CHECK (status IN ('unknown', 'running', 'idle', 'stopped', 'fault', 'maintenance'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_runtime_tenant_facility_machine
                    ON machine_runtime (tenant_id, facility_id, machine_id);

                REVOKE ALL ON TABLE edge_gateways FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE edge_gateways TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_registry FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_registry TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_gateway_assignments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_gateway_assignments TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_events TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_runtime FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_runtime TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "edge_gateways", "eg");
            ApplyTenantRls(migrationBuilder, "machine_registry", "mr");
            ApplyTenantRls(migrationBuilder, "machine_gateway_assignments", "mga");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_events", "me");
            ApplyTenantRls(migrationBuilder, "machine_runtime", "mrt");

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
                        'edge.event.ingested','edge.event.duplicate.rejected','edge.gateway.health.checked'
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
                        'edge.event.ingested','edge.event.duplicate.rejected','edge.gateway.health.checked'
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
                            'erp-writeback-failed',
                            'edge-gateway-registered',
                            'edge-machine-registered',
                            'edge-event-ingested',
                            'edge-event-duplicate-rejected'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS machine_runtime;
                DROP TABLE IF EXISTS machine_events;
                DROP TABLE IF EXISTS machine_gateway_assignments;
                DROP TABLE IF EXISTS machine_registry;
                DROP TABLE IF EXISTS edge_gateways;

                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000062',
                    '10000000-0000-4000-8000-000000000063',
                    '10000000-0000-4000-8000-000000000064');
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
