using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningLogisticsConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE planning_logistics_constraint_evaluations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_plan_id UUID NOT NULL,
                    source_export_case_id UUID NULL,
                    source_export_case_number VARCHAR(100) NULL,
                    source_shipment_id UUID NULL,
                    source_shipment_reference VARCHAR(100) NULL,
                    source_order_reference VARCHAR(100) NULL,
                    source_version BIGINT NOT NULL,
                    pickup_deadline_utc TIMESTAMPTZ NULL,
                    departure_deadline_utc TIMESTAMPTZ NULL,
                    latest_production_completion_utc TIMESTAMPTZ NOT NULL,
                    planned_production_completion_utc TIMESTAMPTZ NOT NULL,
                    slack_minutes INTEGER NOT NULL,
                    risk_level VARCHAR(20) NOT NULL,
                    blocking BOOLEAN NOT NULL,
                    blocking_reason TEXT NULL,
                    linkage_status VARCHAR(50) NOT NULL,
                    constraints_json JSONB NOT NULL,
                    evaluated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_planning_logistics_constraint_evaluations PRIMARY KEY (id),
                    CONSTRAINT fk_planning_logistics_evaluations_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_planning_logistics_evaluations_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_planning_logistics_evaluations_plan FOREIGN KEY (production_plan_id) REFERENCES production_plans(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_planning_logistics_evaluations_version CHECK (version >= 1),
                    CONSTRAINT ck_planning_logistics_evaluations_risk_level CHECK (risk_level IN ('none', 'low', 'medium', 'high', 'critical'))
                );

                CREATE INDEX ix_planning_logistics_evaluations_plan_evaluated
                    ON planning_logistics_constraint_evaluations (tenant_id, facility_id, production_plan_id, evaluated_at_utc DESC);

                CREATE INDEX ix_planning_logistics_evaluations_plan
                    ON planning_logistics_constraint_evaluations (tenant_id, production_plan_id);
                """);

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE planning_logistics_constraint_evaluations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE planning_logistics_constraint_evaluations TO crevafactos_runtime;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE planning_logistics_constraint_evaluations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE planning_logistics_constraint_evaluations FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'planning_logistics_constraint_evaluations'
                          AND policyname = 'rls_planning_logistics_evaluations_tenant_select') THEN
                        CREATE POLICY rls_planning_logistics_evaluations_tenant_select
                            ON planning_logistics_constraint_evaluations
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'planning_logistics_constraint_evaluations'
                          AND policyname = 'rls_planning_logistics_evaluations_tenant_insert') THEN
                        CREATE POLICY rls_planning_logistics_evaluations_tenant_insert
                            ON planning_logistics_constraint_evaluations
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'planning_logistics_constraint_evaluations'
                          AND policyname = 'rls_planning_logistics_evaluations_tenant_update') THEN
                        CREATE POLICY rls_planning_logistics_evaluations_tenant_update
                            ON planning_logistics_constraint_evaluations
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

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
                            'export.ai.task.requested'
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
                            'export.ai.task.requested'
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
                            'export-ai-task-requested'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS planning_logistics_constraint_evaluations;
                """);
        }
    }
}
