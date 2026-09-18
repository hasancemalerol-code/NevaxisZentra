using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRegistryTechnicalSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000067',
                     'material.read',
                     'Read material registry and technical specifications.',
                     'Active',
                     '2026-08-12T10:00:00Z'),
                    ('10000000-0000-4000-8000-000000000068',
                     'material.manage',
                     'Create and update materials, technical specifications, and lifecycle transitions.',
                     'Active',
                     '2026-08-12T10:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE materials (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    code VARCHAR(100) NOT NULL,
                    code_normalized VARCHAR(100) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(1000) NULL,
                    material_category VARCHAR(30) NOT NULL,
                    polymer_family VARCHAR(20) NOT NULL,
                    manufacturer VARCHAR(200) NULL,
                    trade_name_or_grade VARCHAR(200) NULL,
                    color_name VARCHAR(100) NULL,
                    color_code VARCHAR(50) NULL,
                    physical_form VARCHAR(20) NOT NULL,
                    lifecycle_status VARCHAR(20) NOT NULL,
                    unit_of_measure VARCHAR(20) NOT NULL,
                    is_active BOOLEAN NOT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_system VARCHAR(100) NULL,
                    source_reference VARCHAR(200) NULL,
                    external_material_code VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    create_idempotency_key VARCHAR(128) NULL,
                    create_idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_materials PRIMARY KEY (id),
                    CONSTRAINT fk_materials_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_materials_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_materials_code CHECK (code <> ''),
                    CONSTRAINT ck_materials_code_normalized CHECK (code_normalized <> ''),
                    CONSTRAINT ck_materials_name CHECK (name <> ''),
                    CONSTRAINT ck_materials_category CHECK (material_category IN ('virgin-polymer','compound','masterbatch','additive','filler','regrind','other')),
                    CONSTRAINT ck_materials_polymer_family CHECK (polymer_family IN ('PP','PE','HDPE','LDPE','ABS','PA','POM','PC','PMMA','PET','PBT','PS','TPE','TPU','PVC','other','unknown')),
                    CONSTRAINT ck_materials_physical_form CHECK (physical_form IN ('granule','powder','liquid','flake','pellet','other')),
                    CONSTRAINT ck_materials_lifecycle_status CHECK (lifecycle_status IN ('draft','active','blocked','obsolete')),
                    CONSTRAINT ck_materials_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_materials_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_materials_tenant_code_normalized ON materials (tenant_id, code_normalized);
                CREATE UNIQUE INDEX ix_materials_tenant_create_idempotency_key ON materials (tenant_id, create_idempotency_key)
                    WHERE create_idempotency_key IS NOT NULL;
                CREATE INDEX ix_materials_tenant_facility_lifecycle ON materials (tenant_id, facility_id, lifecycle_status);
                CREATE INDEX ix_materials_tenant_facility_category ON materials (tenant_id, facility_id, material_category);
                CREATE INDEX ix_materials_tenant_facility_polymer ON materials (tenant_id, facility_id, polymer_family);

                CREATE TABLE material_technical_specifications (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    density_g_per_cm3 NUMERIC(18,4) NULL,
                    melt_flow_rate NUMERIC(18,4) NULL,
                    melt_flow_unit VARCHAR(20) NULL,
                    melt_flow_test_temperature_c NUMERIC(18,2) NULL,
                    melt_flow_test_load_kg NUMERIC(18,3) NULL,
                    moisture_limit_percent NUMERIC(18,3) NULL,
                    drying_required BOOLEAN NULL,
                    drying_temperature_min_c NUMERIC(18,2) NULL,
                    drying_temperature_max_c NUMERIC(18,2) NULL,
                    drying_time_min_hours NUMERIC(18,2) NULL,
                    drying_time_max_hours NUMERIC(18,2) NULL,
                    melt_temperature_min_c NUMERIC(18,2) NULL,
                    melt_temperature_max_c NUMERIC(18,2) NULL,
                    mold_temperature_min_c NUMERIC(18,2) NULL,
                    mold_temperature_max_c NUMERIC(18,2) NULL,
                    storage_temperature_min_c NUMERIC(18,2) NULL,
                    storage_temperature_max_c NUMERIC(18,2) NULL,
                    maximum_regrind_percent NUMERIC(18,3) NULL,
                    technical_notes VARCHAR(2000) NULL,
                    safety_data_sheet_reference VARCHAR(500) NULL,
                    technical_data_sheet_reference VARCHAR(500) NULL,
                    specification_source VARCHAR(20) NOT NULL,
                    specification_source_reference VARCHAR(500) NULL,
                    specification_recorded_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_technical_specifications PRIMARY KEY (id),
                    CONSTRAINT fk_mts_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mts_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mts_specification_source CHECK (specification_source IN ('manual','imported','development-pilot','unknown')),
                    CONSTRAINT ck_mts_density CHECK (density_g_per_cm3 IS NULL OR density_g_per_cm3 > 0),
                    CONSTRAINT ck_mts_melt_flow_rate CHECK (melt_flow_rate IS NULL OR melt_flow_rate > 0),
                    CONSTRAINT ck_mts_moisture_limit CHECK (moisture_limit_percent IS NULL OR (moisture_limit_percent >= 0 AND moisture_limit_percent <= 100)),
                    CONSTRAINT ck_mts_max_regrind CHECK (maximum_regrind_percent IS NULL OR (maximum_regrind_percent >= 0 AND maximum_regrind_percent <= 100)),
                    CONSTRAINT ck_mts_drying_temp CHECK (drying_temperature_min_c IS NULL OR drying_temperature_max_c IS NULL OR drying_temperature_min_c <= drying_temperature_max_c),
                    CONSTRAINT ck_mts_drying_time CHECK (drying_time_min_hours IS NULL OR drying_time_max_hours IS NULL OR drying_time_min_hours <= drying_time_max_hours),
                    CONSTRAINT ck_mts_melt_temp CHECK (melt_temperature_min_c IS NULL OR melt_temperature_max_c IS NULL OR melt_temperature_min_c <= melt_temperature_max_c),
                    CONSTRAINT ck_mts_mold_temp CHECK (mold_temperature_min_c IS NULL OR mold_temperature_max_c IS NULL OR mold_temperature_min_c <= mold_temperature_max_c),
                    CONSTRAINT ck_mts_storage_temp CHECK (storage_temperature_min_c IS NULL OR storage_temperature_max_c IS NULL OR storage_temperature_min_c <= storage_temperature_max_c),
                    CONSTRAINT ck_mts_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_technical_specifications_material_id ON material_technical_specifications (material_id);

                CREATE TABLE material_lifecycle_events (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    previous_status VARCHAR(20) NOT NULL,
                    new_status VARCHAR(20) NOT NULL,
                    reason VARCHAR(1000) NOT NULL,
                    actor_subject_id VARCHAR(200) NOT NULL,
                    occurred_at_utc TIMESTAMPTZ NOT NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_lifecycle_events PRIMARY KEY (id),
                    CONSTRAINT fk_mle_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mle_previous_status CHECK (previous_status IN ('draft','active','blocked','obsolete')),
                    CONSTRAINT ck_mle_new_status CHECK (new_status IN ('draft','active','blocked','obsolete')),
                    CONSTRAINT ck_mle_reason CHECK (reason <> ''),
                    CONSTRAINT ck_mle_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_lifecycle_events_tenant_material_idempotency
                    ON material_lifecycle_events (tenant_id, material_id, idempotency_key);
                CREATE INDEX ix_material_lifecycle_events_tenant_facility_material_occurred
                    ON material_lifecycle_events (tenant_id, facility_id, material_id, occurred_at_utc DESC);

                REVOKE ALL ON TABLE materials FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE materials TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_technical_specifications FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_technical_specifications TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_lifecycle_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_lifecycle_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "materials", "mat");
            ApplyTenantRls(migrationBuilder, "material_technical_specifications", "mts");
            ApplyTenantRls(migrationBuilder, "material_lifecycle_events", "mle");

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
                        'material.created','material.updated','material.technical.updated','material.lifecycle.transitioned'
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
                        'material.created','material.updated','material.technical.updated','material.lifecycle.transitioned'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS material_lifecycle_events;
                DROP TABLE IF EXISTS material_technical_specifications;
                DROP TABLE IF EXISTS materials;
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
