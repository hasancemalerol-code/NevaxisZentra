using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialLotTraceabilityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE material_storage_locations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    warehouse_code VARCHAR(50) NOT NULL,
                    warehouse_code_normalized VARCHAR(50) NOT NULL,
                    warehouse_name VARCHAR(200) NOT NULL,
                    location_code VARCHAR(50) NOT NULL,
                    location_code_normalized VARCHAR(50) NOT NULL,
                    location_name VARCHAR(200) NOT NULL,
                    is_active BOOLEAN NOT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_storage_locations PRIMARY KEY (id),
                    CONSTRAINT fk_msl_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_msl_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_msl_warehouse_code CHECK (warehouse_code <> ''),
                    CONSTRAINT ck_msl_warehouse_code_normalized CHECK (warehouse_code_normalized <> ''),
                    CONSTRAINT ck_msl_location_code CHECK (location_code <> ''),
                    CONSTRAINT ck_msl_location_code_normalized CHECK (location_code_normalized <> ''),
                    CONSTRAINT ck_msl_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_msl_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_storage_locations_tenant_facility_warehouse_location
                    ON material_storage_locations (tenant_id, facility_id, warehouse_code_normalized, location_code_normalized);
                CREATE INDEX ix_material_storage_locations_tenant_facility_is_active
                    ON material_storage_locations (tenant_id, facility_id, is_active);

                CREATE TABLE material_lots (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    lot_number VARCHAR(100) NOT NULL,
                    lot_number_normalized VARCHAR(100) NOT NULL,
                    supplier_lot_number VARCHAR(100) NULL,
                    manufacturer_lot_number VARCHAR(100) NULL,
                    supplier_reference VARCHAR(200) NULL,
                    purchase_order_reference VARCHAR(200) NULL,
                    goods_receipt_reference VARCHAR(200) NULL,
                    external_system VARCHAR(100) NULL,
                    external_lot_reference VARCHAR(200) NULL,
                    manufacture_date DATE NULL,
                    received_at_utc TIMESTAMPTZ NULL,
                    expiry_date DATE NULL,
                    retest_date DATE NULL,
                    initial_quantity NUMERIC(18,4) NULL,
                    current_observed_quantity NUMERIC(18,4) NULL,
                    unit_of_measure VARCHAR(20) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    quality_status VARCHAR(30) NOT NULL,
                    current_storage_location_id UUID NULL,
                    current_warehouse_reference VARCHAR(100) NULL,
                    current_location_reference VARCHAR(100) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    notes VARCHAR(2000) NULL,
                    create_idempotency_key VARCHAR(128) NULL,
                    create_idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_lots PRIMARY KEY (id),
                    CONSTRAINT fk_ml_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ml_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ml_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_ml_storage_location FOREIGN KEY (current_storage_location_id) REFERENCES material_storage_locations(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_ml_lot_number CHECK (lot_number <> ''),
                    CONSTRAINT ck_ml_lot_number_normalized CHECK (lot_number_normalized <> ''),
                    CONSTRAINT ck_ml_status CHECK (status IN ('draft','received','available','quality-hold','blocked','closed')),
                    CONSTRAINT ck_ml_quality_status CHECK (quality_status IN ('pending','accepted','conditionally-accepted','rejected','not-required','unknown')),
                    CONSTRAINT ck_ml_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_ml_initial_quantity CHECK (initial_quantity IS NULL OR initial_quantity >= 0),
                    CONSTRAINT ck_ml_current_observed_quantity CHECK (current_observed_quantity IS NULL OR current_observed_quantity >= 0),
                    CONSTRAINT ck_ml_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_lots_tenant_material_lot_number_normalized
                    ON material_lots (tenant_id, material_id, lot_number_normalized);
                CREATE UNIQUE INDEX ix_material_lots_tenant_create_idempotency_key
                    ON material_lots (tenant_id, create_idempotency_key)
                    WHERE create_idempotency_key IS NOT NULL;
                CREATE INDEX ix_material_lots_tenant_facility_status ON material_lots (tenant_id, facility_id, status);
                CREATE INDEX ix_material_lots_tenant_facility_quality_status ON material_lots (tenant_id, facility_id, quality_status);
                CREATE INDEX ix_material_lots_tenant_facility_material_id ON material_lots (tenant_id, facility_id, material_id);

                CREATE TABLE material_lot_traceability_events (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_lot_id UUID NOT NULL,
                    event_type VARCHAR(30) NOT NULL,
                    event_at_utc TIMESTAMPTZ NOT NULL,
                    quantity NUMERIC(18,4) NULL,
                    unit_of_measure VARCHAR(20) NULL,
                    from_warehouse_reference VARCHAR(100) NULL,
                    from_location_reference VARCHAR(100) NULL,
                    to_warehouse_reference VARCHAR(100) NULL,
                    to_location_reference VARCHAR(100) NULL,
                    status_before VARCHAR(20) NULL,
                    status_after VARCHAR(20) NULL,
                    quality_status_before VARCHAR(30) NULL,
                    quality_status_after VARCHAR(30) NULL,
                    reason VARCHAR(1000) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    external_event_reference VARCHAR(200) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_lot_traceability_events PRIMARY KEY (id),
                    CONSTRAINT fk_mlte_lot FOREIGN KEY (material_lot_id) REFERENCES material_lots(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mlte_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mlte_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mlte_event_type CHECK (event_type IN ('lot-created','receipt-observed','location-transfer','status-changed','quality-status-recorded','certificate-linked','note-recorded')),
                    CONSTRAINT ck_mlte_status_before CHECK (status_before IS NULL OR status_before IN ('draft','received','available','quality-hold','blocked','closed')),
                    CONSTRAINT ck_mlte_status_after CHECK (status_after IS NULL OR status_after IN ('draft','received','available','quality-hold','blocked','closed')),
                    CONSTRAINT ck_mlte_quality_status_before CHECK (quality_status_before IS NULL OR quality_status_before IN ('pending','accepted','conditionally-accepted','rejected','not-required','unknown')),
                    CONSTRAINT ck_mlte_quality_status_after CHECK (quality_status_after IS NULL OR quality_status_after IN ('pending','accepted','conditionally-accepted','rejected','not-required','unknown')),
                    CONSTRAINT ck_mlte_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_mlte_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_mlte_quantity CHECK (quantity IS NULL OR quantity >= 0),
                    CONSTRAINT ck_mlte_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_lot_traceability_events_tenant_lot_idempotency
                    ON material_lot_traceability_events (tenant_id, material_lot_id, idempotency_key);
                CREATE INDEX ix_material_lot_traceability_events_tenant_facility_lot_event_at
                    ON material_lot_traceability_events (tenant_id, facility_id, material_lot_id, event_at_utc);

                CREATE TABLE material_lot_certificate_references (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_lot_id UUID NOT NULL,
                    certificate_type VARCHAR(30) NOT NULL,
                    document_reference VARCHAR(500) NOT NULL,
                    document_number VARCHAR(100) NULL,
                    issuer VARCHAR(200) NULL,
                    issued_date DATE NULL,
                    expiry_date DATE NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_lot_certificate_references PRIMARY KEY (id),
                    CONSTRAINT fk_mlcr_lot FOREIGN KEY (material_lot_id) REFERENCES material_lots(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mlcr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mlcr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mlcr_certificate_type CHECK (certificate_type IN ('certificate-of-analysis','material-certificate','safety-data-sheet','technical-data-sheet','conformity-certificate','other')),
                    CONSTRAINT ck_mlcr_document_reference CHECK (document_reference <> ''),
                    CONSTRAINT ck_mlcr_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_mlcr_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_mlcr_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_lot_certificate_references_tenant_lot_idempotency
                    ON material_lot_certificate_references (tenant_id, material_lot_id, idempotency_key);
                CREATE INDEX ix_material_lot_certificate_references_tenant_facility_lot
                    ON material_lot_certificate_references (tenant_id, facility_id, material_lot_id);

                REVOKE ALL ON TABLE material_storage_locations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_storage_locations TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_lots FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_lots TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_lot_traceability_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE material_lot_traceability_events TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_lot_certificate_references FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE material_lot_certificate_references TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "material_storage_locations", "msl");
            ApplyTenantRls(migrationBuilder, "material_lots", "ml");
            ApplyTenantRlsAppendOnly(migrationBuilder, "material_lot_traceability_events", "mlte");
            ApplyTenantRlsAppendOnly(migrationBuilder, "material_lot_certificate_references", "mlcr");

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
                        'material.lot.status.changed','material.lot.quality.recorded','material.lot.certificate.linked'
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
                        'material.lot.status.changed','material.lot.quality.recorded','material.lot.certificate.linked'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS material_lot_certificate_references;
                DROP TABLE IF EXISTS material_lot_traceability_events;
                DROP TABLE IF EXISTS material_lots;
                DROP TABLE IF EXISTS material_storage_locations;
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
