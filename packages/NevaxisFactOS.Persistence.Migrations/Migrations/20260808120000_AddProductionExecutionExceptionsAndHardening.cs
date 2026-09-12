using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionExecutionExceptionsAndHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New permission: production.execution.exception
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000024',
                     'production.execution.exception',
                     'Raise, acknowledge, and resolve execution exceptions against a production order.',
                     'Active',
                     '2026-08-08T12:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Recovery metadata columns on production_downtimes
            migrationBuilder.Sql(
                """
                ALTER TABLE production_downtimes
                    ADD COLUMN IF NOT EXISTS recovered_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS recovered_by_subject_id VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS recovery_reason VARCHAR(1000) NULL;
                """);

            // 3. Recovery metadata columns on production_setup_sessions
            migrationBuilder.Sql(
                """
                ALTER TABLE production_setup_sessions
                    ADD COLUMN IF NOT EXISTS recovered_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS recovered_by_subject_id VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS recovery_reason VARCHAR(1000) NULL;
                """);

            // 4. production_execution_exceptions table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_execution_exceptions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    exception_type VARCHAR(50) NOT NULL,
                    severity VARCHAR(20) NOT NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'open',
                    message VARCHAR(500) NOT NULL,
                    raised_at_utc TIMESTAMPTZ NOT NULL,
                    raised_by_subject_id VARCHAR(200) NOT NULL,
                    resolved_at_utc TIMESTAMPTZ NULL,
                    resolved_by_subject_id VARCHAR(200) NULL,
                    resolution_note VARCHAR(1000) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_execution_exceptions PRIMARY KEY (id),
                    CONSTRAINT fk_exceptions_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_exceptions_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_exceptions_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_exceptions_type CHECK (
                        exception_type IN (
                            'stale-downtime', 'stale-setup', 'quantity-anomaly', 'cycle-anomaly',
                            'schedule-overrun', 'handoff-inconsistency', 'operator-attention', 'other'
                        )),
                    CONSTRAINT ck_production_exceptions_severity CHECK (
                        severity IN ('info', 'warning', 'critical')),
                    CONSTRAINT ck_production_exceptions_status CHECK (
                        status IN ('open', 'acknowledged', 'resolved')),
                    CONSTRAINT ck_production_exceptions_resolved_after_raised CHECK (
                        resolved_at_utc IS NULL OR resolved_at_utc >= raised_at_utc),
                    CONSTRAINT ck_production_exceptions_resolved_note CHECK (
                        status <> 'resolved' OR (resolution_note IS NOT NULL AND length(trim(resolution_note)) > 0)),
                    CONSTRAINT ck_production_exceptions_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_exceptions_order_status
                    ON production_execution_exceptions (tenant_id, facility_id, production_order_id, status);

                CREATE INDEX IF NOT EXISTS ix_production_exceptions_order_raised_at
                    ON production_execution_exceptions (production_order_id, raised_at_utc);
                """);

            // 5. Runtime grants — exceptions (SELECT/INSERT/UPDATE, no DELETE)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_execution_exceptions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_execution_exceptions TO crevafactos_runtime;
                """);

            // 6. RLS — production_execution_exceptions
            migrationBuilder.Sql(
                """
                ALTER TABLE production_execution_exceptions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_execution_exceptions FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_execution_exceptions' AND policyname = 'rls_production_exceptions_tenant_select') THEN
                        CREATE POLICY rls_production_exceptions_tenant_select ON production_execution_exceptions FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_execution_exceptions' AND policyname = 'rls_production_exceptions_tenant_insert') THEN
                        CREATE POLICY rls_production_exceptions_tenant_insert ON production_execution_exceptions FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_execution_exceptions' AND policyname = 'rls_production_exceptions_tenant_update') THEN
                        CREATE POLICY rls_production_exceptions_tenant_update ON production_execution_exceptions FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 7. Expand audit action taxonomy (drop + recreate with 4L additions)
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
                            'production.order.setup.recover'
                        ));
                """);

            // 8. Expand audit event taxonomy (drop + recreate with 4L additions)
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
                            'production.order.setup.recovered'
                        ));
                """);

            // 9. Expand outbox message type taxonomy (drop + recreate with 4L additions)
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
                            'production-order-setup-recovered'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: restore outbox type constraint to 3L state
            migrationBuilder.Sql(
                """
                ALTER TABLE outbox_messages
                    DROP CONSTRAINT IF EXISTS ck_outbox_messages_type,
                    ADD CONSTRAINT ck_outbox_messages_type CHECK (
                        message_type IN (
                            'infrastructure.audit.recorded',
                            'infrastructure.ai-decision.recorded',
                            'infrastructure.outbox.probe',
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
                            'production-order-manual-cycle-recorded'
                        ));
                """);

            migrationBuilder.Sql("DROP TABLE IF EXISTS production_execution_exceptions;");

            migrationBuilder.Sql(
                """
                ALTER TABLE production_downtimes
                    DROP COLUMN IF EXISTS recovered_at_utc,
                    DROP COLUMN IF EXISTS recovered_by_subject_id,
                    DROP COLUMN IF EXISTS recovery_reason;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE production_setup_sessions
                    DROP COLUMN IF EXISTS recovered_at_utc,
                    DROP COLUMN IF EXISTS recovered_by_subject_id,
                    DROP COLUMN IF EXISTS recovery_reason;
                """);
        }
    }
}
