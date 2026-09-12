using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRequirementConsumptionIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE material_requirement_profiles (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    production_order_reference VARCHAR(200) NULL,
                    product_reference VARCHAR(200) NULL,
                    mold_reference VARCHAR(200) NULL,
                    requirement_type VARCHAR(30) NOT NULL,
                    planned_production_quantity NUMERIC(18,4) NULL,
                    good_part_target_quantity NUMERIC(18,4) NULL,
                    part_weight_grams NUMERIC(18,4) NULL,
                    runner_weight_grams_per_shot NUMERIC(18,4) NULL,
                    effective_cavity_count INT NULL,
                    setup_allowance_grams NUMERIC(18,4) NULL,
                    process_loss_allowance_percent NUMERIC(8,4) NULL,
                    planned_virgin_percent NUMERIC(8,4) NULL,
                    planned_regrind_percent NUMERIC(8,4) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    create_idempotency_key VARCHAR(128) NULL,
                    create_idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_requirement_profiles PRIMARY KEY (id),
                    CONSTRAINT fk_mrp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrp_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mrp_requirement_type CHECK (requirement_type IN ('production-order','trial','setup','manual-planning','other')),
                    CONSTRAINT ck_mrp_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_mrp_good_part_target_quantity CHECK (good_part_target_quantity IS NULL OR good_part_target_quantity >= 0),
                    CONSTRAINT ck_mrp_part_weight_grams CHECK (part_weight_grams IS NULL OR part_weight_grams >= 0),
                    CONSTRAINT ck_mrp_runner_weight_grams_per_shot CHECK (runner_weight_grams_per_shot IS NULL OR runner_weight_grams_per_shot >= 0),
                    CONSTRAINT ck_mrp_effective_cavity_count CHECK (effective_cavity_count IS NULL OR effective_cavity_count > 0),
                    CONSTRAINT ck_mrp_setup_allowance_grams CHECK (setup_allowance_grams IS NULL OR setup_allowance_grams >= 0),
                    CONSTRAINT ck_mrp_process_loss_allowance_percent CHECK (process_loss_allowance_percent IS NULL OR (process_loss_allowance_percent >= 0 AND process_loss_allowance_percent <= 100)),
                    CONSTRAINT ck_mrp_planned_virgin_percent CHECK (planned_virgin_percent IS NULL OR (planned_virgin_percent >= 0 AND planned_virgin_percent <= 100)),
                    CONSTRAINT ck_mrp_planned_regrind_percent CHECK (planned_regrind_percent IS NULL OR (planned_regrind_percent >= 0 AND planned_regrind_percent <= 100)),
                    CONSTRAINT ck_mrp_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_requirement_profiles_tenant_create_idempotency_key
                    ON material_requirement_profiles (tenant_id, create_idempotency_key)
                    WHERE create_idempotency_key IS NOT NULL;
                CREATE INDEX ix_material_requirement_profiles_tenant_facility_material_id
                    ON material_requirement_profiles (tenant_id, facility_id, material_id);
                CREATE INDEX ix_material_requirement_profiles_tenant_facility_requirement_type
                    ON material_requirement_profiles (tenant_id, facility_id, requirement_type);

                CREATE TABLE material_consumption_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    material_lot_id UUID NULL,
                    requirement_profile_id UUID NULL,
                    production_order_reference VARCHAR(200) NULL,
                    product_reference VARCHAR(200) NULL,
                    mold_reference VARCHAR(200) NULL,
                    observation_type VARCHAR(40) NOT NULL,
                    quantity NUMERIC(18,4) NOT NULL,
                    unit_of_measure VARCHAR(20) NOT NULL,
                    virgin_quantity NUMERIC(18,4) NULL,
                    regrind_quantity NUMERIC(18,4) NULL,
                    good_part_quantity NUMERIC(18,4) NULL,
                    scrap_quantity NUMERIC(18,4) NULL,
                    observed_at_utc TIMESTAMPTZ NOT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    notes VARCHAR(2000) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_consumption_observations PRIMARY KEY (id),
                    CONSTRAINT fk_mco_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mco_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mco_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mco_material_lot FOREIGN KEY (material_lot_id) REFERENCES material_lots(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mco_requirement_profile FOREIGN KEY (requirement_profile_id) REFERENCES material_requirement_profiles(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mco_observation_type CHECK (observation_type IN ('consumption-observed','setup-consumption-observed','trial-consumption-observed','scrap-observed','regrind-use-observed','correction-observed')),
                    CONSTRAINT ck_mco_source_type CHECK (source_type IN ('manual','imported','erp','development-pilot','unknown')),
                    CONSTRAINT ck_mco_quantity CHECK (quantity > 0),
                    CONSTRAINT ck_mco_virgin_quantity CHECK (virgin_quantity IS NULL OR virgin_quantity >= 0),
                    CONSTRAINT ck_mco_regrind_quantity CHECK (regrind_quantity IS NULL OR regrind_quantity >= 0),
                    CONSTRAINT ck_mco_good_part_quantity CHECK (good_part_quantity IS NULL OR good_part_quantity >= 0),
                    CONSTRAINT ck_mco_scrap_quantity CHECK (scrap_quantity IS NULL OR scrap_quantity >= 0),
                    CONSTRAINT ck_mco_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_mco_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_consumption_observations_tenant_material_idempotency
                    ON material_consumption_observations (tenant_id, material_id, idempotency_key);
                CREATE INDEX ix_material_consumption_observations_tenant_facility_material_observed_at
                    ON material_consumption_observations (tenant_id, facility_id, material_id, observed_at_utc);
                CREATE INDEX ix_material_consumption_observations_material_lot_id
                    ON material_consumption_observations (material_lot_id);
                CREATE INDEX ix_material_consumption_observations_requirement_profile_id
                    ON material_consumption_observations (requirement_profile_id);

                CREATE TABLE material_requirement_assessments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    material_id UUID NOT NULL,
                    requirement_profile_id UUID NULL,
                    assessment_type VARCHAR(30) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    rule_set_version VARCHAR(50) NOT NULL,
                    input_fingerprint VARCHAR(64) NOT NULL,
                    structured_drivers JSONB NOT NULL,
                    structured_evidence JSONB NOT NULL,
                    theoretical_requirement_grams NUMERIC(18,4) NULL,
                    observed_consumption_grams NUMERIC(18,4) NULL,
                    variance_grams NUMERIC(18,4) NULL,
                    variance_percent NUMERIC(8,4) NULL,
                    virgin_requirement_grams NUMERIC(18,4) NULL,
                    regrind_requirement_grams NUMERIC(18,4) NULL,
                    evaluated_at_utc TIMESTAMPTZ NOT NULL,
                    idempotency_key VARCHAR(128) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_material_requirement_assessments PRIMARY KEY (id),
                    CONSTRAINT fk_mra_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mra_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mra_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mra_requirement_profile FOREIGN KEY (requirement_profile_id) REFERENCES material_requirement_profiles(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mra_assessment_type CHECK (assessment_type IN ('requirement','consumption-variance','regrind-policy','availability-risk')),
                    CONSTRAINT ck_mra_status CHECK (status <> ''),
                    CONSTRAINT ck_mra_rule_set_version CHECK (rule_set_version <> ''),
                    CONSTRAINT ck_mra_input_fingerprint CHECK (input_fingerprint <> ''),
                    CONSTRAINT ck_mra_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_material_requirement_assessments_tenant_material_type_fingerprint
                    ON material_requirement_assessments (tenant_id, material_id, assessment_type, input_fingerprint);
                CREATE UNIQUE INDEX ix_material_requirement_assessments_tenant_material_idempotency_key
                    ON material_requirement_assessments (tenant_id, material_id, idempotency_key)
                    WHERE idempotency_key IS NOT NULL;
                CREATE INDEX ix_material_requirement_assessments_tenant_facility_material_evaluated_at
                    ON material_requirement_assessments (tenant_id, facility_id, material_id, evaluated_at_utc);
                CREATE INDEX ix_material_requirement_assessments_requirement_profile_id
                    ON material_requirement_assessments (requirement_profile_id);

                REVOKE ALL ON TABLE material_requirement_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_requirement_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_consumption_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE material_consumption_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE material_requirement_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE material_requirement_assessments TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "material_requirement_profiles", "mrp");
            ApplyTenantRlsAppendOnly(migrationBuilder, "material_consumption_observations", "mco");
            ApplyTenantRlsAppendOnly(migrationBuilder, "material_requirement_assessments", "mra");

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
                        'material.consumption.observed','material.requirement.assessed'
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
                        'material.consumption.observed','material.requirement.assessed'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS material_requirement_assessments;
                DROP TABLE IF EXISTS material_consumption_observations;
                DROP TABLE IF EXISTS material_requirement_profiles;
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
