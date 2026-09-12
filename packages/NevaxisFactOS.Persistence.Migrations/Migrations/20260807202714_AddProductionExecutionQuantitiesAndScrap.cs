using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionExecutionQuantitiesAndScrap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. production.execution.record permission definition
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000021',
                     'production.execution.record',
                     'Record good production and scrap quantities against an active production order.',
                     'Active',
                     '2026-08-07T21:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Add quantity columns to production_orders
            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    ADD COLUMN IF NOT EXISTS planned_quantity NUMERIC NULL,
                    ADD COLUMN IF NOT EXISTS planned_quantity_unit VARCHAR(50) NULL,
                    ADD COLUMN IF NOT EXISTS good_quantity NUMERIC NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS scrap_quantity NUMERIC NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS last_production_entry_at_utc TIMESTAMPTZ NULL;

                ALTER TABLE production_orders
                    ADD CONSTRAINT ck_production_orders_planned_quantity
                        CHECK (planned_quantity IS NULL OR planned_quantity > 0),
                    ADD CONSTRAINT ck_production_orders_good_quantity
                        CHECK (good_quantity >= 0),
                    ADD CONSTRAINT ck_production_orders_scrap_quantity
                        CHECK (scrap_quantity >= 0);
                """);

            // 3. Create production_execution_entries table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_execution_entries (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    entry_type VARCHAR(30) NOT NULL,
                    quantity NUMERIC NOT NULL,
                    unit VARCHAR(50) NOT NULL DEFAULT 'piece',
                    reason_code VARCHAR(100) NULL,
                    comment VARCHAR(1000) NULL,
                    recorded_by_subject_id VARCHAR(200) NOT NULL,
                    recorded_at_utc TIMESTAMPTZ NOT NULL,
                    reverses_entry_id UUID NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_execution_entries PRIMARY KEY (id),
                    CONSTRAINT fk_entries_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_entries_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_entries_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_execution_entries_entry_type CHECK (entry_type IN ('good-production', 'scrap', 'reversal')),
                    CONSTRAINT ck_execution_entries_unit CHECK (unit IN ('piece')),
                    CONSTRAINT ck_execution_entries_quantity_positive CHECK (quantity > 0),
                    CONSTRAINT ck_execution_entries_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_execution_entries_order_time
                    ON production_execution_entries (tenant_id, facility_id, production_order_id, recorded_at_utc);

                CREATE INDEX IF NOT EXISTS ix_execution_entries_order
                    ON production_execution_entries (production_order_id);

                CREATE INDEX IF NOT EXISTS ix_execution_entries_reverses
                    ON production_execution_entries (reverses_entry_id)
                    WHERE reverses_entry_id IS NOT NULL;
                """);

            // 4. Runtime grants for production_execution_entries (immutable: SELECT + INSERT only)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_execution_entries FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE production_execution_entries TO crevafactos_runtime;
                """);

            // 5. Row-level security for production_execution_entries
            migrationBuilder.Sql(
                """
                ALTER TABLE production_execution_entries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_execution_entries FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_execution_entries'
                          AND policyname = 'rls_execution_entries_tenant_select') THEN
                        CREATE POLICY rls_execution_entries_tenant_select
                            ON production_execution_entries
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_execution_entries'
                          AND policyname = 'rls_execution_entries_tenant_insert') THEN
                        CREATE POLICY rls_execution_entries_tenant_insert
                            ON production_execution_entries
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 6. Expand audit action taxonomy
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
                            'production.order.quantity.reverse'
                        ));
                """);

            // 7. Expand audit event taxonomy
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
                            'production.order.quantity.reversed'
                        ));
                """);

            // 8. Expand outbox message type taxonomy
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
                            'production-order-quantity-reversed'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert outbox taxonomy
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
                            'production-plan-handoff-consumed'
                        ));
                """);

            // Revert audit event taxonomy
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
                            'production.plan.handoff.consumed'
                        ));
                """);

            // Revert audit action taxonomy
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
                            'production.plan.handoff.consume'
                        ));
                """);

            migrationBuilder.Sql("DROP TABLE IF EXISTS production_execution_entries;");

            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    DROP CONSTRAINT IF EXISTS ck_production_orders_planned_quantity,
                    DROP CONSTRAINT IF EXISTS ck_production_orders_good_quantity,
                    DROP CONSTRAINT IF EXISTS ck_production_orders_scrap_quantity,
                    DROP COLUMN IF EXISTS planned_quantity,
                    DROP COLUMN IF EXISTS planned_quantity_unit,
                    DROP COLUMN IF EXISTS good_quantity,
                    DROP COLUMN IF EXISTS scrap_quantity,
                    DROP COLUMN IF EXISTS last_production_entry_at_utc;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id = '10000000-0000-4000-8000-000000000021';
                """);
        }
    }
}
