using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldMaintenanceRevisionTrialHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE mold_maintenance_records (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    maintenance_type VARCHAR(30) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    opened_at_utc TIMESTAMPTZ NOT NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    summary VARCHAR(1000) NOT NULL,
                    findings VARCHAR(2000) NULL,
                    actions_taken VARCHAR(2000) NULL,
                    performed_by_display_name VARCHAR(200) NULL,
                    external_maintenance_request_reference VARCHAR(200) NULL,
                    external_work_order_reference VARCHAR(200) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_mold_maintenance_records PRIMARY KEY (id),
                    CONSTRAINT fk_mmr_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mmr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mmr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mmr_summary CHECK (summary <> ''),
                    CONSTRAINT ck_mmr_maintenance_type CHECK (maintenance_type IN ('preventive','corrective','cleaning','inspection','repair','overhaul','other')),
                    CONSTRAINT ck_mmr_status CHECK (status IN ('planned','in-progress','completed','cancelled')),
                    CONSTRAINT ck_mmr_source_type CHECK (source_type IN ('manual','imported','erp','unknown')),
                    CONSTRAINT ck_mmr_version CHECK (version >= 1),
                    CONSTRAINT ck_mmr_completed_after_open CHECK (completed_at_utc IS NULL OR completed_at_utc >= opened_at_utc),
                    CONSTRAINT ck_mmr_completed_required CHECK (status <> 'completed' OR completed_at_utc IS NOT NULL)
                );

                CREATE UNIQUE INDEX ix_mold_maintenance_records_tenant_mold_idempotency
                    ON mold_maintenance_records (tenant_id, mold_id, idempotency_key);
                CREATE INDEX ix_mold_maintenance_records_tenant_facility_mold_opened
                    ON mold_maintenance_records (tenant_id, facility_id, mold_id, opened_at_utc DESC);

                CREATE TABLE mold_revision_records (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    revision_number VARCHAR(50) NOT NULL,
                    revision_number_normalized VARCHAR(50) NOT NULL,
                    revision_type VARCHAR(30) NOT NULL,
                    revision_date DATE NOT NULL,
                    summary VARCHAR(1000) NOT NULL,
                    reason VARCHAR(1000) NULL,
                    technical_change VARCHAR(2000) NULL,
                    previous_cavity_count INT NULL,
                    new_cavity_count INT NULL,
                    performed_by_display_name VARCHAR(200) NULL,
                    external_reference VARCHAR(200) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_mold_revision_records PRIMARY KEY (id),
                    CONSTRAINT fk_mrr_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mrr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mrr_summary CHECK (summary <> ''),
                    CONSTRAINT ck_mrr_revision_type CHECK (revision_type IN ('design-change','cavity-change','material-change','cooling-change','runner-change','repair-improvement','customer-change','other')),
                    CONSTRAINT ck_mrr_source_type CHECK (source_type IN ('manual','imported','erp','unknown')),
                    CONSTRAINT ck_mrr_previous_cavity CHECK (previous_cavity_count IS NULL OR previous_cavity_count > 0),
                    CONSTRAINT ck_mrr_new_cavity CHECK (new_cavity_count IS NULL OR new_cavity_count > 0)
                );

                CREATE UNIQUE INDEX ix_mold_revision_records_tenant_mold_idempotency
                    ON mold_revision_records (tenant_id, mold_id, idempotency_key);
                CREATE UNIQUE INDEX ix_mold_revision_records_tenant_mold_revision_number
                    ON mold_revision_records (tenant_id, mold_id, revision_number_normalized);
                CREATE INDEX ix_mold_revision_records_tenant_facility_mold_date
                    ON mold_revision_records (tenant_id, facility_id, mold_id, revision_date DESC);

                CREATE TABLE mold_trial_records (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    trial_number VARCHAR(50) NOT NULL,
                    trial_number_normalized VARCHAR(50) NOT NULL,
                    trial_date DATE NOT NULL,
                    result VARCHAR(30) NOT NULL,
                    machine_reference VARCHAR(200) NULL,
                    production_order_reference VARCHAR(200) NULL,
                    product_reference VARCHAR(200) NULL,
                    material_reference VARCHAR(200) NULL,
                    cycle_time_seconds NUMERIC(18,3) NULL,
                    shot_count INT NULL,
                    good_part_count INT NULL,
                    scrap_part_count INT NULL,
                    findings VARCHAR(2000) NULL,
                    decision VARCHAR(1000) NULL,
                    performed_by_display_name VARCHAR(200) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    idempotency_key VARCHAR(128) NOT NULL,
                    idempotency_payload_fingerprint VARCHAR(64) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_mold_trial_records PRIMARY KEY (id),
                    CONSTRAINT fk_mtr_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mtr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mtr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mtr_result CHECK (result IN ('pending','successful','conditionally-approved','failed','cancelled')),
                    CONSTRAINT ck_mtr_source_type CHECK (source_type IN ('manual','imported','erp','unknown')),
                    CONSTRAINT ck_mtr_shot_count CHECK (shot_count IS NULL OR shot_count >= 0),
                    CONSTRAINT ck_mtr_good_part_count CHECK (good_part_count IS NULL OR good_part_count >= 0),
                    CONSTRAINT ck_mtr_scrap_part_count CHECK (scrap_part_count IS NULL OR scrap_part_count >= 0),
                    CONSTRAINT ck_mtr_cycle_time CHECK (cycle_time_seconds IS NULL OR cycle_time_seconds > 0),
                    CONSTRAINT ck_mtr_part_counts CHECK (
                        shot_count IS NULL
                        OR good_part_count IS NULL
                        OR scrap_part_count IS NULL
                        OR (good_part_count + scrap_part_count <= shot_count))
                );

                CREATE UNIQUE INDEX ix_mold_trial_records_tenant_mold_idempotency
                    ON mold_trial_records (tenant_id, mold_id, idempotency_key);
                CREATE UNIQUE INDEX ix_mold_trial_records_tenant_mold_trial_number
                    ON mold_trial_records (tenant_id, mold_id, trial_number_normalized);
                CREATE INDEX ix_mold_trial_records_tenant_facility_mold_date
                    ON mold_trial_records (tenant_id, facility_id, mold_id, trial_date DESC);

                REVOKE ALL ON TABLE mold_maintenance_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE mold_maintenance_records TO crevafactos_runtime;
                REVOKE ALL ON TABLE mold_revision_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE mold_revision_records TO crevafactos_runtime;
                REVOKE ALL ON TABLE mold_trial_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE mold_trial_records TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "mold_maintenance_records", "mmr");
            ApplyTenantRlsAppendOnly(migrationBuilder, "mold_revision_records", "mrr");
            ApplyTenantRlsAppendOnly(migrationBuilder, "mold_trial_records", "mtr");

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
                        'mold.revision.appended','mold.trial.appended'
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
                        'mold.revision.appended','mold.trial.appended'
                    ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS mold_trial_records;
                DROP TABLE IF EXISTS mold_revision_records;
                DROP TABLE IF EXISTS mold_maintenance_records;
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
