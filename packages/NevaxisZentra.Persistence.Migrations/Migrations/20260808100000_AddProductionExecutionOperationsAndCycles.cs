using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionExecutionOperationsAndCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New permissions: downtime + setup
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000022',
                     'production.execution.downtime',
                     'Start and end downtime sessions against an active production order.',
                     'Active',
                     '2026-08-08T10:00:00Z'),
                    ('10000000-0000-4000-8000-000000000023',
                     'production.execution.setup',
                     'Start and complete setup or changeover sessions against a production order.',
                     'Active',
                     '2026-08-08T10:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Operational summary columns on production_orders
            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    ADD COLUMN IF NOT EXISTS total_downtime_seconds BIGINT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS total_setup_seconds BIGINT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS manual_cycle_count INTEGER NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS total_manual_cycle_duration_ms BIGINT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS last_cycle_at_utc TIMESTAMPTZ NULL;

                ALTER TABLE production_orders
                    ADD CONSTRAINT ck_production_orders_total_downtime
                        CHECK (total_downtime_seconds >= 0),
                    ADD CONSTRAINT ck_production_orders_total_setup
                        CHECK (total_setup_seconds >= 0),
                    ADD CONSTRAINT ck_production_orders_manual_cycle_count
                        CHECK (manual_cycle_count >= 0),
                    ADD CONSTRAINT ck_production_orders_total_cycle_duration
                        CHECK (total_manual_cycle_duration_ms >= 0);
                """);

            // 3. production_downtimes table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_downtimes (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    category VARCHAR(20) NOT NULL,
                    reason_code VARCHAR(100) NOT NULL,
                    started_at_utc TIMESTAMPTZ NOT NULL,
                    ended_at_utc TIMESTAMPTZ NULL,
                    duration_seconds BIGINT NULL,
                    comment VARCHAR(1000) NULL,
                    started_by_subject_id VARCHAR(200) NOT NULL,
                    ended_by_subject_id VARCHAR(200) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_downtimes PRIMARY KEY (id),
                    CONSTRAINT fk_downtimes_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_downtimes_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_downtimes_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_downtimes_category CHECK (category IN ('planned', 'unplanned')),
                    CONSTRAINT ck_production_downtimes_ended_after_started CHECK (ended_at_utc IS NULL OR ended_at_utc >= started_at_utc),
                    CONSTRAINT ck_production_downtimes_duration CHECK ((duration_seconds IS NULL) = (ended_at_utc IS NULL)),
                    CONSTRAINT ck_production_downtimes_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_downtimes_order_time
                    ON production_downtimes (tenant_id, facility_id, production_order_id, started_at_utc);

                CREATE INDEX IF NOT EXISTS ix_production_downtimes_order
                    ON production_downtimes (production_order_id);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_downtimes_active
                    ON production_downtimes (production_order_id)
                    WHERE ended_at_utc IS NULL;
                """);

            // 4. production_setup_sessions table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_setup_sessions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    setup_type VARCHAR(50) NOT NULL,
                    started_at_utc TIMESTAMPTZ NOT NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    duration_seconds BIGINT NULL,
                    previous_machine_reference VARCHAR(200) NULL,
                    previous_mold_reference VARCHAR(200) NULL,
                    target_machine_reference VARCHAR(200) NULL,
                    target_mold_reference VARCHAR(200) NULL,
                    comment VARCHAR(1000) NULL,
                    started_by_subject_id VARCHAR(200) NOT NULL,
                    completed_by_subject_id VARCHAR(200) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_setup_sessions PRIMARY KEY (id),
                    CONSTRAINT fk_setup_sessions_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_setup_sessions_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_setup_sessions_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_setup_sessions_type CHECK (setup_type IN ('initial-setup', 'mold-change', 'material-change', 'product-change', 'parameter-adjustment', 'other')),
                    CONSTRAINT ck_production_setup_sessions_completed_after_started CHECK (completed_at_utc IS NULL OR completed_at_utc >= started_at_utc),
                    CONSTRAINT ck_production_setup_sessions_duration CHECK ((duration_seconds IS NULL) = (completed_at_utc IS NULL)),
                    CONSTRAINT ck_production_setup_sessions_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_setup_sessions_order_time
                    ON production_setup_sessions (tenant_id, facility_id, production_order_id, started_at_utc);

                CREATE INDEX IF NOT EXISTS ix_production_setup_sessions_order
                    ON production_setup_sessions (production_order_id);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_setup_sessions_active
                    ON production_setup_sessions (production_order_id)
                    WHERE completed_at_utc IS NULL;
                """);

            // 5. production_cycle_entries table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_cycle_entries (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    cycle_number INTEGER NOT NULL,
                    cycle_started_at_utc TIMESTAMPTZ NOT NULL,
                    cycle_ended_at_utc TIMESTAMPTZ NOT NULL,
                    cycle_duration_ms BIGINT NOT NULL,
                    quantity_produced NUMERIC NULL,
                    recorded_by_subject_id VARCHAR(200) NOT NULL,
                    recorded_at_utc TIMESTAMPTZ NOT NULL,
                    source VARCHAR(20) NOT NULL DEFAULT 'manual',
                    comment VARCHAR(1000) NULL,
                    correlation_id UUID NOT NULL,
                    CONSTRAINT pk_production_cycle_entries PRIMARY KEY (id),
                    CONSTRAINT fk_cycle_entries_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_cycle_entries_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_cycle_entries_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_cycle_entries_duration CHECK (cycle_duration_ms > 0),
                    CONSTRAINT ck_production_cycle_entries_source CHECK (source IN ('manual')),
                    CONSTRAINT ck_production_cycle_entries_cycle_number CHECK (cycle_number > 0),
                    CONSTRAINT ck_production_cycle_entries_quantity CHECK (quantity_produced IS NULL OR quantity_produced >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_cycle_entries_order_cycle_number
                    ON production_cycle_entries (tenant_id, facility_id, production_order_id, cycle_number);

                CREATE INDEX IF NOT EXISTS ix_production_cycle_entries_order_time
                    ON production_cycle_entries (production_order_id, recorded_at_utc);
                """);

            // 6. Runtime grants — downtimes (SELECT/INSERT/UPDATE, no DELETE)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_downtimes FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_downtimes TO crevafactos_runtime;
                """);

            // 7. Runtime grants — setup sessions (SELECT/INSERT/UPDATE, no DELETE)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_setup_sessions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_setup_sessions TO crevafactos_runtime;
                """);

            // 8. Runtime grants — cycle entries (SELECT/INSERT only, immutable)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_cycle_entries FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE production_cycle_entries TO crevafactos_runtime;
                """);

            // 9. RLS — production_downtimes
            migrationBuilder.Sql(
                """
                ALTER TABLE production_downtimes ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_downtimes FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_downtimes' AND policyname = 'rls_production_downtimes_tenant_select') THEN
                        CREATE POLICY rls_production_downtimes_tenant_select ON production_downtimes FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_downtimes' AND policyname = 'rls_production_downtimes_tenant_insert') THEN
                        CREATE POLICY rls_production_downtimes_tenant_insert ON production_downtimes FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_downtimes' AND policyname = 'rls_production_downtimes_tenant_update') THEN
                        CREATE POLICY rls_production_downtimes_tenant_update ON production_downtimes FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 10. RLS — production_setup_sessions
            migrationBuilder.Sql(
                """
                ALTER TABLE production_setup_sessions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_setup_sessions FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_setup_sessions' AND policyname = 'rls_production_setup_sessions_tenant_select') THEN
                        CREATE POLICY rls_production_setup_sessions_tenant_select ON production_setup_sessions FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_setup_sessions' AND policyname = 'rls_production_setup_sessions_tenant_insert') THEN
                        CREATE POLICY rls_production_setup_sessions_tenant_insert ON production_setup_sessions FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_setup_sessions' AND policyname = 'rls_production_setup_sessions_tenant_update') THEN
                        CREATE POLICY rls_production_setup_sessions_tenant_update ON production_setup_sessions FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 11. RLS — production_cycle_entries
            migrationBuilder.Sql(
                """
                ALTER TABLE production_cycle_entries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_cycle_entries FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_cycle_entries' AND policyname = 'rls_production_cycle_entries_tenant_select') THEN
                        CREATE POLICY rls_production_cycle_entries_tenant_select ON production_cycle_entries FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_cycle_entries' AND policyname = 'rls_production_cycle_entries_tenant_insert') THEN
                        CREATE POLICY rls_production_cycle_entries_tenant_insert ON production_cycle_entries FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 12. Expand audit action taxonomy
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
                            'production.order.cycle.manual.record'
                        ));
                """);

            // 13. Expand audit event taxonomy
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
                            'production.order.cycle.manual.recorded'
                        ));
                """);

            // 14. Expand outbox message type taxonomy
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
                            'production-order-manual-cycle-recorded'
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
                            'production-order-completed-short',
                            'production-plan-handoff-consumed',
                            'production-order-good-quantity-recorded',
                            'production-order-scrap-recorded',
                            'production-order-quantity-reversed'
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
                            'production.order.completed.short',
                            'production.plan.handoff.consumed',
                            'production.order.quantity.good.recorded',
                            'production.order.quantity.scrap.recorded',
                            'production.order.quantity.reversed'
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
                            'production.order.complete.short',
                            'production.plan.handoff.consume',
                            'production.order.quantity.good.record',
                            'production.order.quantity.scrap.record',
                            'production.order.quantity.reverse'
                        ));
                """);

            // Drop new tables
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_cycle_entries;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_setup_sessions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_downtimes;");

            // Drop operational summary columns from production_orders
            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    DROP CONSTRAINT IF EXISTS ck_production_orders_total_downtime,
                    DROP CONSTRAINT IF EXISTS ck_production_orders_total_setup,
                    DROP CONSTRAINT IF EXISTS ck_production_orders_manual_cycle_count,
                    DROP CONSTRAINT IF EXISTS ck_production_orders_total_cycle_duration,
                    DROP COLUMN IF EXISTS total_downtime_seconds,
                    DROP COLUMN IF EXISTS total_setup_seconds,
                    DROP COLUMN IF EXISTS manual_cycle_count,
                    DROP COLUMN IF EXISTS total_manual_cycle_duration_ms,
                    DROP COLUMN IF EXISTS last_cycle_at_utc;
                """);

            // Remove permissions
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000022',
                    '10000000-0000-4000-8000-000000000023'
                );
                """);
        }
    }
}
