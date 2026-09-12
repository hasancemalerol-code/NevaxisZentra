using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldRegistryLifecycleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000065',
                     'mold.read',
                     'Read mold registry and lifecycle history.',
                     'Active',
                     '2026-08-11T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000066',
                     'mold.manage',
                     'Create and update molds, counters, and lifecycle transitions.',
                     'Active',
                     '2026-08-11T20:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE molds (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    code VARCHAR(100) NOT NULL,
                    code_normalized VARCHAR(100) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(1000) NULL,
                    serial_number VARCHAR(100) NULL,
                    manufacturer VARCHAR(200) NULL,
                    manufacture_year INT NULL,
                    manufacture_date DATE NULL,
                    commissioning_date DATE NULL,
                    source_type VARCHAR(20) NOT NULL,
                    external_source_reference VARCHAR(200) NULL,
                    ownership_type VARCHAR(30) NOT NULL,
                    owner_reference VARCHAR(200) NULL,
                    location_type VARCHAR(30) NOT NULL,
                    location_label VARCHAR(200) NULL,
                    location_changed_at_utc TIMESTAMPTZ NULL,
                    location_changed_by_subject_id VARCHAR(200) NULL,
                    lifecycle_state VARCHAR(20) NOT NULL,
                    mold_type VARCHAR(100) NULL,
                    cavity_count INT NULL,
                    nominal_shot_life BIGINT NULL,
                    current_shot_count BIGINT NULL,
                    counter_recorded_at_utc TIMESTAMPTZ NULL,
                    counter_source VARCHAR(20) NULL,
                    counter_source_reference VARCHAR(200) NULL,
                    counter_recorded_by_subject_id VARCHAR(200) NULL,
                    counter_note VARCHAR(1000) NULL,
                    primary_product_reference VARCHAR(200) NULL,
                    material_family_reference VARCHAR(200) NULL,
                    runner_type VARCHAR(100) NULL,
                    hot_runner_present BOOLEAN NULL,
                    mold_weight_kg NUMERIC(18,3) NULL,
                    dimension_length_mm NUMERIC(18,3) NULL,
                    dimension_width_mm NUMERIC(18,3) NULL,
                    dimension_height_mm NUMERIC(18,3) NULL,
                    notes VARCHAR(2000) NULL,
                    create_idempotency_key VARCHAR(128) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_molds PRIMARY KEY (id),
                    CONSTRAINT fk_molds_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_molds_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_molds_code CHECK (code <> ''),
                    CONSTRAINT ck_molds_code_normalized CHECK (code_normalized <> ''),
                    CONSTRAINT ck_molds_name CHECK (name <> ''),
                    CONSTRAINT ck_molds_source_type CHECK (source_type IN ('manual', 'imported', 'erp', 'unknown')),
                    CONSTRAINT ck_molds_ownership_type CHECK (ownership_type IN ('company-owned', 'customer-owned', 'supplier-owned', 'leased', 'unknown')),
                    CONSTRAINT ck_molds_location_type CHECK (location_type IN ('warehouse', 'production-area', 'maintenance-area', 'trial-area', 'external-customer', 'external-supplier', 'transit', 'unknown')),
                    CONSTRAINT ck_molds_lifecycle_state CHECK (lifecycle_state IN ('draft', 'active', 'maintenance', 'trial', 'quarantined', 'retired', 'scrapped')),
                    CONSTRAINT ck_molds_counter_source CHECK (counter_source IS NULL OR counter_source IN ('manual', 'production', 'edge', 'erp', 'imported', 'unknown')),
                    CONSTRAINT ck_molds_cavity_count CHECK (cavity_count IS NULL OR cavity_count > 0),
                    CONSTRAINT ck_molds_nominal_shot_life CHECK (nominal_shot_life IS NULL OR nominal_shot_life >= 0),
                    CONSTRAINT ck_molds_current_shot_count CHECK (current_shot_count IS NULL OR current_shot_count >= 0),
                    CONSTRAINT ck_molds_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_molds_tenant_code_normalized ON molds (tenant_id, code_normalized);
                CREATE UNIQUE INDEX ix_molds_tenant_create_idempotency_key ON molds (tenant_id, create_idempotency_key)
                    WHERE create_idempotency_key IS NOT NULL;
                CREATE INDEX ix_molds_tenant_facility_lifecycle ON molds (tenant_id, facility_id, lifecycle_state);

                CREATE TABLE mold_lifecycle_events (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    previous_state VARCHAR(20) NOT NULL,
                    new_state VARCHAR(20) NOT NULL,
                    reason VARCHAR(1000) NOT NULL,
                    actor_subject_id VARCHAR(200) NOT NULL,
                    occurred_at_utc TIMESTAMPTZ NOT NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_mold_lifecycle_events PRIMARY KEY (id),
                    CONSTRAINT fk_mle_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mle_previous_state CHECK (previous_state IN ('draft', 'active', 'maintenance', 'trial', 'quarantined', 'retired', 'scrapped')),
                    CONSTRAINT ck_mle_new_state CHECK (new_state IN ('draft', 'active', 'maintenance', 'trial', 'quarantined', 'retired', 'scrapped')),
                    CONSTRAINT ck_mle_reason CHECK (reason <> ''),
                    CONSTRAINT ck_mle_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_mold_lifecycle_events_tenant_mold_idempotency
                    ON mold_lifecycle_events (tenant_id, mold_id, idempotency_key);
                CREATE INDEX ix_mold_lifecycle_events_tenant_facility_mold_occurred
                    ON mold_lifecycle_events (tenant_id, facility_id, mold_id, occurred_at_utc DESC);

                CREATE TABLE mold_counter_snapshots (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    shot_count BIGINT NOT NULL,
                    recorded_at_utc TIMESTAMPTZ NOT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    recorded_by_subject_id VARCHAR(200) NULL,
                    note VARCHAR(1000) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_mold_counter_snapshots PRIMARY KEY (id),
                    CONSTRAINT fk_mcs_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mcs_source_type CHECK (source_type IN ('manual', 'production', 'edge', 'erp', 'imported', 'unknown')),
                    CONSTRAINT ck_mcs_shot_count CHECK (shot_count >= 0),
                    CONSTRAINT ck_mcs_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_mold_counter_snapshots_tenant_mold_idempotency
                    ON mold_counter_snapshots (tenant_id, mold_id, idempotency_key);
                CREATE INDEX ix_mold_counter_snapshots_tenant_facility_mold_recorded
                    ON mold_counter_snapshots (tenant_id, facility_id, mold_id, recorded_at_utc DESC);

                REVOKE ALL ON TABLE molds FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE molds TO crevafactos_runtime;
                REVOKE ALL ON TABLE mold_lifecycle_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE mold_lifecycle_events TO crevafactos_runtime;
                REVOKE ALL ON TABLE mold_counter_snapshots FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE mold_counter_snapshots TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "molds", "mold");
            ApplyTenantRls(migrationBuilder, "mold_lifecycle_events", "mle");
            ApplyTenantRls(migrationBuilder, "mold_counter_snapshots", "mcs");

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
                        'mold.created','mold.updated','mold.counter.recorded','mold.lifecycle.transitioned'
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
                        'mold.created','mold.updated','mold.counter.recorded','mold.lifecycle.transitioned'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS mold_counter_snapshots;
                DROP TABLE IF EXISTS mold_lifecycle_events;
                DROP TABLE IF EXISTS molds;
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
    }
}
