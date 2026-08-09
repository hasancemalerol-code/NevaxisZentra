using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityControlCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000046',
                     'quality.read',
                     'Read quality control plans and inspections.',
                     'Active',
                     '2026-08-09T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000047',
                     'quality.manage',
                     'Create and update quality control plans.',
                     'Active',
                     '2026-08-09T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000048',
                     'quality.inspect',
                     'Execute quality inspections and record results.',
                     'Active',
                     '2026-08-09T14:00:00Z'),
                    ('10000000-0000-4000-8000-000000000049',
                     'quality.release',
                     'Release or reject completed quality inspections.',
                     'Active',
                     '2026-08-09T14:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE quality_control_plans (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    code VARCHAR(100) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(1000) NULL,
                    product_reference VARCHAR(200) NULL,
                    product_group VARCHAR(200) NULL,
                    operation_reference VARCHAR(200) NULL,
                    status VARCHAR(20) NOT NULL,
                    revision VARCHAR(20) NOT NULL,
                    effective_from_utc TIMESTAMPTZ NULL,
                    effective_to_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_control_plans PRIMARY KEY (id),
                    CONSTRAINT fk_qcp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_qcp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_quality_control_plans_status CHECK (status IN ('draft', 'active', 'inactive', 'archived')),
                    CONSTRAINT ck_quality_control_plans_code CHECK (code <> ''),
                    CONSTRAINT ck_quality_control_plans_name CHECK (name <> ''),
                    CONSTRAINT ck_quality_control_plans_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_quality_control_plans_tenant_facility_code
                    ON quality_control_plans (tenant_id, facility_id, code);

                CREATE TABLE quality_inspection_definitions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    quality_control_plan_id UUID NOT NULL,
                    code VARCHAR(100) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(1000) NULL,
                    inspection_type VARCHAR(20) NOT NULL,
                    sequence INT NOT NULL,
                    unit VARCHAR(50) NULL,
                    nominal_value NUMERIC(18,6) NULL,
                    lower_limit NUMERIC(18,6) NULL,
                    upper_limit NUMERIC(18,6) NULL,
                    required BOOLEAN NOT NULL DEFAULT TRUE,
                    critical BOOLEAN NOT NULL DEFAULT FALSE,
                    instruction VARCHAR(2000) NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_inspection_definitions PRIMARY KEY (id),
                    CONSTRAINT fk_qid_plan FOREIGN KEY (quality_control_plan_id) REFERENCES quality_control_plans(id) ON DELETE CASCADE,
                    CONSTRAINT ck_quality_inspection_definitions_type CHECK (inspection_type IN ('measurement', 'visual', 'boolean', 'text')),
                    CONSTRAINT ck_quality_inspection_definitions_code CHECK (code <> ''),
                    CONSTRAINT ck_quality_inspection_definitions_name CHECK (name <> ''),
                    CONSTRAINT ck_quality_inspection_definitions_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_quality_inspection_definitions_plan_code
                    ON quality_inspection_definitions (quality_control_plan_id, code);

                CREATE TABLE quality_inspections (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    quality_control_plan_id UUID NOT NULL,
                    inspection_number VARCHAR(50) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    started_at_utc TIMESTAMPTZ NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    inspector_subject_id VARCHAR(200) NULL,
                    overall_result VARCHAR(20) NOT NULL,
                    release_status VARCHAR(20) NOT NULL,
                    notes VARCHAR(2000) NULL,
                    release_note VARCHAR(2000) NULL,
                    released_by_subject_id VARCHAR(200) NULL,
                    released_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_inspections PRIMARY KEY (id),
                    CONSTRAINT fk_qi_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_qi_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_qi_production_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_qi_plan FOREIGN KEY (quality_control_plan_id) REFERENCES quality_control_plans(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_quality_inspections_status CHECK (status IN ('draft', 'in-progress', 'completed', 'cancelled')),
                    CONSTRAINT ck_quality_inspections_overall CHECK (overall_result IN ('pending', 'pass', 'fail', 'conditional')),
                    CONSTRAINT ck_quality_inspections_release CHECK (release_status IN ('not-ready', 'ready-for-review', 'released', 'rejected')),
                    CONSTRAINT ck_quality_inspections_number CHECK (inspection_number <> ''),
                    CONSTRAINT ck_quality_inspections_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_quality_inspections_tenant_facility_number
                    ON quality_inspections (tenant_id, facility_id, inspection_number);

                CREATE INDEX ix_quality_inspections_tenant_facility_order
                    ON quality_inspections (tenant_id, facility_id, production_order_id);

                CREATE TABLE quality_inspection_results (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    quality_inspection_id UUID NOT NULL,
                    inspection_definition_id UUID NOT NULL,
                    measured_numeric_value NUMERIC(18,6) NULL,
                    measured_text_value VARCHAR(500) NULL,
                    boolean_result BOOLEAN NULL,
                    result VARCHAR(20) NOT NULL,
                    comment VARCHAR(1000) NULL,
                    recorded_at_utc TIMESTAMPTZ NOT NULL,
                    recorded_by_subject_id VARCHAR(200) NOT NULL,
                    source VARCHAR(50) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_quality_inspection_results PRIMARY KEY (id),
                    CONSTRAINT fk_qir_inspection FOREIGN KEY (quality_inspection_id) REFERENCES quality_inspections(id) ON DELETE CASCADE,
                    CONSTRAINT ck_quality_inspection_results_result CHECK (result IN ('pending', 'pass', 'fail', 'not-applicable')),
                    CONSTRAINT ck_quality_inspection_results_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_quality_inspection_results_inspection_definition
                    ON quality_inspection_results (quality_inspection_id, inspection_definition_id);

                REVOKE ALL ON TABLE quality_control_plans FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_control_plans TO crevafactos_runtime;
                REVOKE ALL ON TABLE quality_inspection_definitions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_inspection_definitions TO crevafactos_runtime;
                REVOKE ALL ON TABLE quality_inspections FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_inspections TO crevafactos_runtime;
                REVOKE ALL ON TABLE quality_inspection_results FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE quality_inspection_results TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "quality_control_plans", "qcp");
            ApplyTenantRls(migrationBuilder, "quality_inspection_definitions", "qid");
            ApplyTenantRls(migrationBuilder, "quality_inspections", "qi");
            ApplyTenantRls(migrationBuilder, "quality_inspection_results", "qir");

            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_action_taxonomy,
                    ADD CONSTRAINT ck_audit_records_action_taxonomy CHECK (
                        action IN (
                            'authenticate',
                            'access.tenant',
                            'authorize',
                            'write.audit',
                            'validate.configuration',
                            'ai.decision.append',
                            'production.plan.draft.create',
                            'production.plan.draft.update',
                            'production.plan.workflow.submit.for.review',
                            'production.plan.workflow.request.revision',
                            'production.plan.workflow.approve',
                            'production.plan.workflow.reject',
                            'production.plan.schedule',
                            'production.plan.transfer.request',
                            'production.plan.transfer.start',
                            'production.plan.transfer.succeed',
                            'production.plan.transfer.fail',
                            'production.plan.transfer.mark.uncertain',
                            'production.plan.handoff.prepare',
                            'production.order.create',
                            'production.order.start',
                            'production.order.pause',
                            'production.order.resume',
                            'production.order.complete',
                            'production.order.complete.short',
                            'production.plan.handoff.consume',
                            'production.order.quantity.good.record',
                            'production.order.quantity.scrap.record',
                            'production.order.quantity.reverse',
                            'production.order.downtime.start',
                            'production.order.downtime.end',
                            'production.order.setup.start',
                            'production.order.setup.complete',
                            'production.order.cycle.manual.record',
                            'production.order.exception.raise',
                            'production.order.exception.acknowledge',
                            'production.order.exception.resolve',
                            'production.order.downtime.recover',
                            'production.order.setup.recover',
                            'production.order.shift.assign',
                            'production.order.shift.reassign',
                            'production.order.operator.assign',
                            'production.order.operator.release',
                            'production.order.handover.create',
                            'production.order.exception.assign',
                            'production.order.exception.unassign',
                            'planning.logistics.constraints.evaluate',
                            'planning.logistics.risk.change',
                            'export.case.created',
                            'export.case.updated',
                            'export.document.registered',
                            'export.document.verified',
                            'export.document.rejected',
                            'export.readiness.changed',
                            'export.packing.unit.created',
                            'export.packing.unit.updated',
                            'export.packing.item.added',
                            'export.packing.completed',
                            'export.packing.verified',
                            'export.shipment.created',
                            'export.shipment.updated',
                            'export.shipment.status.changed',
                            'export.broker.workspace.created',
                            'export.broker.package.prepared',
                            'export.broker.package.sent',
                            'export.broker.workspace.acknowledged',
                            'export.broker.correction.created',
                            'export.broker.correction.resolved',
                            'export.compliance.evaluated',
                            'export.compliance.revalidated',
                            'export.compliance.requirement.satisfied',
                            'export.country.rule.pack.created',
                            'export.country.rule.added',
                            'export.country.rule.pack.verified',
                            'export.country.rule.pack.superseded',
                            'export.country.research.created',
                            'export.country.research.finding.added',
                            'export.country.research.reviewed',
                            'export.country.research.completed',
                            'export.country.research.linked',
                            'export.country.knowledge.created',
                            'export.country.knowledge.verified',
                            'export.country.knowledge.superseded',
                            'export.ai.analysis.started',
                            'export.ai.analysis.completed',
                            'export.ai.analysis.failed',
                            'export.ai.high-risk.found',
                            'export.ai.recommendation.reviewed',
                            'export.ai.recommendation.accepted',
                            'export.ai.recommendation.rejected',
                            'export.ai.task.requested',
                            'quality.plan.created',
                            'quality.plan.updated',
                            'quality.inspection.started',
                            'quality.result.recorded',
                            'quality.inspection.completed',
                            'quality.release.released',
                            'quality.release.rejected'
                        ));
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE audit_records
                    DROP CONSTRAINT ck_audit_records_event_taxonomy,
                    ADD CONSTRAINT ck_audit_records_event_taxonomy CHECK (
                        event_type IN (
                            'authentication.succeeded',
                            'authentication.failed',
                            'tenant.access.granted',
                            'tenant.access.denied',
                            'authorization.granted',
                            'authorization.denied',
                            'audit.write.failed',
                            'infrastructure.configuration.rejected',
                            'ai.decision.recorded',
                            'production.plan.draft.created',
                            'production.plan.draft.updated',
                            'production.plan.submitted.for.review',
                            'production.plan.revision.requested',
                            'production.plan.approved',
                            'production.plan.rejected',
                            'production.plan.scheduled',
                            'production.plan.transfer.requested',
                            'production.plan.transfer.started',
                            'production.plan.transfer.succeeded',
                            'production.plan.transfer.failed',
                            'production.plan.transfer.uncertain',
                            'production.plan.handoff.prepared',
                            'production.order.created',
                            'production.order.started',
                            'production.order.paused',
                            'production.order.resumed',
                            'production.order.completed',
                            'production.order.completed.short',
                            'production.plan.handoff.consumed',
                            'production.order.quantity.good.recorded',
                            'production.order.quantity.scrap.recorded',
                            'production.order.quantity.reversed',
                            'production.order.downtime.started',
                            'production.order.downtime.ended',
                            'production.order.setup.started',
                            'production.order.setup.completed',
                            'production.order.cycle.manual.recorded',
                            'production.order.exception.raised',
                            'production.order.exception.acknowledged',
                            'production.order.exception.resolved',
                            'production.order.downtime.recovered',
                            'production.order.setup.recovered',
                            'production.order.shift.assigned',
                            'production.order.shift.reassigned',
                            'production.order.operator.assigned',
                            'production.order.operator.released',
                            'production.order.handover.created',
                            'production.order.exception.assigned',
                            'production.order.exception.unassigned',
                            'planning.logistics.constraints.evaluated',
                            'planning.logistics.risk.changed',
                            'export.case.created',
                            'export.case.updated',
                            'export.document.registered',
                            'export.document.verified',
                            'export.document.rejected',
                            'export.readiness.changed',
                            'export.packing.unit.created',
                            'export.packing.unit.updated',
                            'export.packing.item.added',
                            'export.packing.completed',
                            'export.packing.verified',
                            'export.shipment.created',
                            'export.shipment.updated',
                            'export.shipment.status.changed',
                            'export.broker.workspace.created',
                            'export.broker.package.prepared',
                            'export.broker.package.sent',
                            'export.broker.workspace.acknowledged',
                            'export.broker.correction.created',
                            'export.broker.correction.resolved',
                            'export.compliance.evaluated',
                            'export.compliance.revalidated',
                            'export.compliance.requirement.satisfied',
                            'export.country.rule.pack.created',
                            'export.country.rule.added',
                            'export.country.rule.pack.verified',
                            'export.country.rule.pack.superseded',
                            'export.country.research.created',
                            'export.country.research.finding.added',
                            'export.country.research.reviewed',
                            'export.country.research.completed',
                            'export.country.research.linked',
                            'export.country.knowledge.created',
                            'export.country.knowledge.verified',
                            'export.country.knowledge.superseded',
                            'export.ai.analysis.started',
                            'export.ai.analysis.completed',
                            'export.ai.analysis.failed',
                            'export.ai.high-risk.found',
                            'export.ai.recommendation.reviewed',
                            'export.ai.recommendation.accepted',
                            'export.ai.recommendation.rejected',
                            'export.ai.task.requested',
                            'quality.plan.created',
                            'quality.plan.updated',
                            'quality.inspection.started',
                            'quality.result.recorded',
                            'quality.inspection.completed',
                            'quality.release.released',
                            'quality.release.rejected'
                        ));
                """);

            migrationBuilder.Sql(
                """
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
                            'quality-release-rejected'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS quality_inspection_results;
                DROP TABLE IF EXISTS quality_inspections;
                DROP TABLE IF EXISTS quality_inspection_definitions;
                DROP TABLE IF EXISTS quality_control_plans;
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
