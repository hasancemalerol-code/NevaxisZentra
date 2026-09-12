using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionExecutionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Production execution permission definitions
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000016',
                     'production.execution.read',
                     'Read production orders for a facility.',
                     'Active',
                     '2026-08-07T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000017',
                     'production.execution.write',
                     'Create production orders by consuming a prepared handoff.',
                     'Active',
                     '2026-08-07T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000018',
                     'production.execution.start',
                     'Start a ready production order.',
                     'Active',
                     '2026-08-07T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000019',
                     'production.execution.pause',
                     'Pause an in-progress production order.',
                     'Active',
                     '2026-08-07T20:00:00Z'),
                    ('10000000-0000-4000-8000-000000000020',
                     'production.execution.complete',
                     'Complete an in-progress production order.',
                     'Active',
                     '2026-08-07T20:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Create production_orders table
            migrationBuilder.Sql(
                """
                CREATE TABLE production_orders (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_plan_handoff_id UUID NOT NULL,
                    source_production_plan_id UUID NOT NULL,
                    source_production_plan_version BIGINT NOT NULL,
                    reference_code VARCHAR(200) NOT NULL,
                    title VARCHAR(300) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    planned_start_at_utc TIMESTAMPTZ NOT NULL,
                    planned_end_at_utc TIMESTAMPTZ NOT NULL,
                    actual_start_at_utc TIMESTAMPTZ NULL,
                    actual_end_at_utc TIMESTAMPTZ NULL,
                    shift_name VARCHAR(100) NULL,
                    machine_reference VARCHAR(200) NULL,
                    mold_reference VARCHAR(200) NULL,
                    product_reference VARCHAR(200) NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    last_modified_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    history_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                    CONSTRAINT pk_production_orders PRIMARY KEY (id),
                    CONSTRAINT fk_orders_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_orders_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_orders_handoff FOREIGN KEY (production_plan_handoff_id) REFERENCES production_plan_handoffs(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_orders_status CHECK (status IN ('ready', 'in-progress', 'paused', 'completed', 'cancelled')),
                    CONSTRAINT ck_production_orders_reference_code CHECK (reference_code <> ''),
                    CONSTRAINT ck_production_orders_title CHECK (title <> ''),
                    CONSTRAINT ck_production_orders_version CHECK (version >= 1),
                    CONSTRAINT ck_production_orders_actual_times CHECK (
                        actual_start_at_utc IS NULL
                        OR actual_end_at_utc IS NULL
                        OR actual_end_at_utc >= actual_start_at_utc)
                );

                -- Idempotency: each handoff can produce exactly one production order
                CREATE UNIQUE INDEX ix_production_orders_handoff
                    ON production_orders (production_plan_handoff_id);

                -- Operational queries: active orders by facility
                CREATE INDEX ix_production_orders_tenant_facility_status
                    ON production_orders (tenant_id, facility_id, status);

                -- Scheduled window queries
                CREATE INDEX ix_production_orders_tenant_facility_planned_start
                    ON production_orders (tenant_id, facility_id, planned_start_at_utc);

                -- Reference code lookup
                CREATE INDEX ix_production_orders_tenant_facility_reference
                    ON production_orders (tenant_id, facility_id, reference_code);

                -- Source plan traceability
                CREATE INDEX ix_production_orders_source_plan
                    ON production_orders (source_production_plan_id);
                """);

            // 3. Runtime grants for production_orders
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_orders FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_orders TO crevafactos_runtime;
                """);

            // 4. Row-level security for production_orders
            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_orders FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_orders'
                          AND policyname = 'rls_orders_tenant_select') THEN
                        CREATE POLICY rls_orders_tenant_select
                            ON production_orders
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_orders'
                          AND policyname = 'rls_orders_tenant_insert') THEN
                        CREATE POLICY rls_orders_tenant_insert
                            ON production_orders
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_orders'
                          AND policyname = 'rls_orders_tenant_update') THEN
                        CREATE POLICY rls_orders_tenant_update
                            ON production_orders
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 5. Expand audit action taxonomy to include production execution actions
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

            // 6. Expand audit event taxonomy to include production execution events
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

            // 7. Expand outbox message type taxonomy
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                            'production-plan-handoff-prepared'
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
                            'production.plan.handoff.prepared'
                        ));
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
                            'production.plan.handoff.prepare'
                        ));
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS production_orders;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000016',
                    '10000000-0000-4000-8000-000000000017',
                    '10000000-0000-4000-8000-000000000018',
                    '10000000-0000-4000-8000-000000000019',
                    '10000000-0000-4000-8000-000000000020'
                );
                """);
        }
    }
}
