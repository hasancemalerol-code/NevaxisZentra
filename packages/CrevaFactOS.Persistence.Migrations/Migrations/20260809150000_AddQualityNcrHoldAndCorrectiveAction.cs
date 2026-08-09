using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    public partial class AddQualityNcrHoldAndCorrectiveAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000050',
                     'quality.ncr.manage',
                     'Create and update nonconformance reports.',
                     'Active',
                     '2026-08-09T15:00:00Z'),
                    ('10000000-0000-4000-8000-000000000051',
                     'quality.hold',
                     'Place and release quality holds.',
                     'Active',
                     '2026-08-09T15:00:00Z'),
                    ('10000000-0000-4000-8000-000000000052',
                     'quality.disposition',
                     'Decide nonconformance disposition.',
                     'Active',
                     '2026-08-09T15:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS quality_nonconformances (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    ncr_number VARCHAR(50) NOT NULL,
                    production_order_id UUID NULL,
                    quality_inspection_id UUID NULL,
                    quality_inspection_result_id UUID NULL,
                    product_reference VARCHAR(200) NULL,
                    operation_reference VARCHAR(200) NULL,
                    title VARCHAR(200) NOT NULL,
                    description VARCHAR(4000) NOT NULL,
                    category VARCHAR(100) NULL,
                    severity VARCHAR(20) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    detected_at_utc TIMESTAMPTZ NOT NULL,
                    detected_by_subject_id VARCHAR(200) NOT NULL,
                    owner_subject_id VARCHAR(200) NULL,
                    disposition_status VARCHAR(20) NOT NULL,
                    disposition_type VARCHAR(30) NULL,
                    disposition_note VARCHAR(2000) NULL,
                    disposition_decided_by_subject_id VARCHAR(200) NULL,
                    disposition_decided_at_utc TIMESTAMPTZ NULL,
                    hold_status VARCHAR(20) NOT NULL,
                    closed_at_utc TIMESTAMPTZ NULL,
                    closed_by_subject_id VARCHAR(200) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_nonconformances PRIMARY KEY (id),
                    CONSTRAINT fk_qn_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_qn_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_quality_nonconformances_severity CHECK (severity IN ('minor', 'major', 'critical')),
                    CONSTRAINT ck_quality_nonconformances_status CHECK (status IN ('open', 'under-review', 'action-required', 'resolved', 'closed', 'cancelled')),
                    CONSTRAINT ck_quality_nonconformances_disposition_status CHECK (disposition_status IN ('pending', 'decided')),
                    CONSTRAINT ck_quality_nonconformances_disposition_type CHECK (disposition_type IS NULL OR disposition_type IN ('use-as-is', 'rework', 'scrap', 'return-to-supplier', 'sort-or-screen', 'pending')),
                    CONSTRAINT ck_quality_nonconformances_hold_status CHECK (hold_status IN ('none', 'active', 'released')),
                    CONSTRAINT ck_quality_nonconformances_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_quality_nonconformances_tenant_facility_number
                    ON quality_nonconformances (tenant_id, facility_id, ncr_number);

                CREATE UNIQUE INDEX ix_quality_nonconformances_inspection_result
                    ON quality_nonconformances (tenant_id, facility_id, quality_inspection_result_id)
                    WHERE quality_inspection_result_id IS NOT NULL;

                CREATE TABLE IF NOT EXISTS quality_holds (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    ncr_id UUID NOT NULL,
                    production_order_id UUID NULL,
                    scope VARCHAR(50) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    reason VARCHAR(2000) NOT NULL,
                    placed_at_utc TIMESTAMPTZ NOT NULL,
                    placed_by_subject_id VARCHAR(200) NOT NULL,
                    released_at_utc TIMESTAMPTZ NULL,
                    released_by_subject_id VARCHAR(200) NULL,
                    release_reason VARCHAR(2000) NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_holds PRIMARY KEY (id),
                    CONSTRAINT fk_qh_ncr FOREIGN KEY (ncr_id) REFERENCES quality_nonconformances(id) ON DELETE CASCADE,
                    CONSTRAINT ck_quality_holds_status CHECK (status IN ('active', 'released', 'cancelled')),
                    CONSTRAINT ck_quality_holds_version CHECK (version >= 1)
                );

                CREATE INDEX ix_quality_holds_tenant_facility_order
                    ON quality_holds (tenant_id, facility_id, production_order_id);

                CREATE TABLE IF NOT EXISTS quality_corrective_actions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    ncr_id UUID NOT NULL,
                    title VARCHAR(200) NOT NULL,
                    description VARCHAR(2000) NOT NULL,
                    owner_subject_id VARCHAR(200) NULL,
                    status VARCHAR(20) NOT NULL,
                    due_at_utc TIMESTAMPTZ NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    completed_by_subject_id VARCHAR(200) NULL,
                    completion_note VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_corrective_actions PRIMARY KEY (id),
                    CONSTRAINT fk_qca_ncr FOREIGN KEY (ncr_id) REFERENCES quality_nonconformances(id) ON DELETE CASCADE,
                    CONSTRAINT ck_quality_corrective_actions_status CHECK (status IN ('open', 'in-progress', 'completed', 'cancelled')),
                    CONSTRAINT ck_quality_corrective_actions_version CHECK (version >= 1)
                );

                REVOKE ALL ON TABLE quality_nonconformances FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_nonconformances TO crevafactos_runtime;
                REVOKE ALL ON TABLE quality_holds FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_holds TO crevafactos_runtime;
                REVOKE ALL ON TABLE quality_corrective_actions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_corrective_actions TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "quality_nonconformances", "qn");
            ApplyTenantRls(migrationBuilder, "quality_holds", "qh");
            ApplyTenantRls(migrationBuilder, "quality_corrective_actions", "qca");

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
                        'quality.corrective.action.created','quality.corrective.action.completed'
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
                        'quality.corrective.action.created','quality.corrective.action.completed'
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
                            'quality-corrective-action-completed'
                        ));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS quality_corrective_actions;
                DROP TABLE IF EXISTS quality_holds;
                DROP TABLE IF EXISTS quality_nonconformances;
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
    }
}
