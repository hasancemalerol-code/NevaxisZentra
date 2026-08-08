using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionShiftResourceExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New permissions: assign, handover, exception.assign
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000025',
                     'production.execution.assign',
                     'Assign shifts and operators to production orders.',
                     'Active',
                     '2026-08-08T13:00:00Z'),
                    ('10000000-0000-4000-8000-000000000026',
                     'production.execution.handover',
                     'Create shift handover notes for production orders.',
                     'Active',
                     '2026-08-08T13:00:00Z'),
                    ('10000000-0000-4000-8000-000000000027',
                     'production.execution.exception.assign',
                     'Assign and unassign exception ownership on production orders.',
                     'Active',
                     '2026-08-08T13:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. New resource summary columns on production_orders
            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    ADD COLUMN IF NOT EXISTS current_shift_id UUID NULL,
                    ADD COLUMN IF NOT EXISTS current_shift_code VARCHAR(20) NULL,
                    ADD COLUMN IF NOT EXISTS current_shift_start_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS current_shift_end_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS primary_operator_subject_id VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS active_operator_count INTEGER NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS last_assignment_at_utc TIMESTAMPTZ NULL;
                """);

            // 3. Exception ownership columns
            migrationBuilder.Sql(
                """
                ALTER TABLE production_execution_exceptions
                    ADD COLUMN IF NOT EXISTS assigned_to_subject_id VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS assigned_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS assigned_by_subject_id VARCHAR(200) NULL;
                """);

            // 4. production_shifts table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_shifts (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    code VARCHAR(20) NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    start_local_time INTERVAL NOT NULL,
                    end_local_time INTERVAL NOT NULL,
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    version BIGINT NOT NULL DEFAULT 1,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_production_shifts PRIMARY KEY (id),
                    CONSTRAINT fk_shifts_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_shifts_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_production_shifts_code CHECK (length(trim(code)) > 0),
                    CONSTRAINT ck_production_shifts_name CHECK (length(trim(name)) > 0),
                    CONSTRAINT ck_production_shifts_sort_order CHECK (sort_order >= 0),
                    CONSTRAINT ck_production_shifts_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_shifts_facility_active
                    ON production_shifts (tenant_id, facility_id, is_active);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_shifts_facility_code_active
                    ON production_shifts (tenant_id, facility_id, code)
                    WHERE is_active = true;
                """);

            // 5. production_order_shift_assignments table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_order_shift_assignments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    production_shift_id UUID NOT NULL,
                    shift_code VARCHAR(20) NOT NULL,
                    shift_date DATE NOT NULL,
                    shift_start_at_utc TIMESTAMPTZ NOT NULL,
                    shift_end_at_utc TIMESTAMPTZ NOT NULL,
                    assigned_at_utc TIMESTAMPTZ NOT NULL,
                    assigned_by_subject_id VARCHAR(200) NOT NULL,
                    released_at_utc TIMESTAMPTZ NULL,
                    released_by_subject_id VARCHAR(200) NULL,
                    release_reason VARCHAR(500) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_order_shift_assignments PRIMARY KEY (id),
                    CONSTRAINT fk_shift_assignments_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_shift_assignments_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_shift_assignments_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_shift_assignments_shift FOREIGN KEY (production_shift_id) REFERENCES production_shifts(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_shift_assignments_end_after_start CHECK (shift_end_at_utc > shift_start_at_utc),
                    CONSTRAINT ck_shift_assignments_released_after_assigned CHECK (released_at_utc IS NULL OR released_at_utc >= assigned_at_utc),
                    CONSTRAINT ck_shift_assignments_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_shift_assignments_order_released
                    ON production_order_shift_assignments (production_order_id, released_at_utc);

                CREATE INDEX IF NOT EXISTS ix_production_shift_assignments_facility_start
                    ON production_order_shift_assignments (tenant_id, facility_id, shift_start_at_utc);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_shift_assignments_active_per_order
                    ON production_order_shift_assignments (production_order_id)
                    WHERE released_at_utc IS NULL;
                """);

            // 6. production_operator_assignments table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_operator_assignments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    subject_id VARCHAR(200) NOT NULL,
                    assignment_role VARCHAR(30) NOT NULL,
                    assigned_at_utc TIMESTAMPTZ NOT NULL,
                    assigned_by_subject_id VARCHAR(200) NOT NULL,
                    released_at_utc TIMESTAMPTZ NULL,
                    released_by_subject_id VARCHAR(200) NULL,
                    release_reason VARCHAR(500) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL DEFAULT 1,
                    CONSTRAINT pk_production_operator_assignments PRIMARY KEY (id),
                    CONSTRAINT fk_operator_assignments_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_operator_assignments_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_operator_assignments_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_operator_assignments_role CHECK (
                        assignment_role IN ('primary-operator', 'operator', 'supervisor')),
                    CONSTRAINT ck_operator_assignments_subject CHECK (length(trim(subject_id)) > 0),
                    CONSTRAINT ck_operator_assignments_released_after_assigned CHECK (released_at_utc IS NULL OR released_at_utc >= assigned_at_utc),
                    CONSTRAINT ck_operator_assignments_version CHECK (version >= 1)
                );

                CREATE INDEX IF NOT EXISTS ix_production_operator_assignments_order_released
                    ON production_operator_assignments (production_order_id, released_at_utc);

                CREATE INDEX IF NOT EXISTS ix_production_operator_assignments_subject_released
                    ON production_operator_assignments (subject_id, released_at_utc);

                CREATE UNIQUE INDEX IF NOT EXISTS uix_production_primary_operator_active_per_order
                    ON production_operator_assignments (production_order_id)
                    WHERE assignment_role = 'primary-operator' AND released_at_utc IS NULL;
                """);

            // 7. production_shift_handovers table
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS production_shift_handovers (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_order_id UUID NOT NULL,
                    from_shift_assignment_id UUID NOT NULL,
                    to_shift_assignment_id UUID NULL,
                    note VARCHAR(2000) NOT NULL,
                    open_issues_summary VARCHAR(2000) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    correlation_id UUID NOT NULL,
                    CONSTRAINT pk_production_shift_handovers PRIMARY KEY (id),
                    CONSTRAINT fk_handovers_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_handovers_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_handovers_order FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_handovers_from_assignment FOREIGN KEY (from_shift_assignment_id) REFERENCES production_order_shift_assignments(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_handovers_note CHECK (length(trim(note)) > 0)
                );

                CREATE INDEX IF NOT EXISTS ix_production_shift_handovers_order_created
                    ON production_shift_handovers (production_order_id, created_at_utc);
                """);

            // 8. RLS — production_shifts
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_shifts FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_shifts TO crevafactos_runtime;

                ALTER TABLE production_shifts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_shifts FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_shifts' AND policyname = 'rls_production_shifts_tenant_select') THEN
                        CREATE POLICY rls_production_shifts_tenant_select ON production_shifts FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_shifts' AND policyname = 'rls_production_shifts_tenant_insert') THEN
                        CREATE POLICY rls_production_shifts_tenant_insert ON production_shifts FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_shifts' AND policyname = 'rls_production_shifts_tenant_update') THEN
                        CREATE POLICY rls_production_shifts_tenant_update ON production_shifts FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 9. RLS — production_order_shift_assignments
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_order_shift_assignments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_order_shift_assignments TO crevafactos_runtime;

                ALTER TABLE production_order_shift_assignments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_order_shift_assignments FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_order_shift_assignments' AND policyname = 'rls_shift_assignments_tenant_select') THEN
                        CREATE POLICY rls_shift_assignments_tenant_select ON production_order_shift_assignments FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_order_shift_assignments' AND policyname = 'rls_shift_assignments_tenant_insert') THEN
                        CREATE POLICY rls_shift_assignments_tenant_insert ON production_order_shift_assignments FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_order_shift_assignments' AND policyname = 'rls_shift_assignments_tenant_update') THEN
                        CREATE POLICY rls_shift_assignments_tenant_update ON production_order_shift_assignments FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 10. RLS — production_operator_assignments
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_operator_assignments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_operator_assignments TO crevafactos_runtime;

                ALTER TABLE production_operator_assignments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_operator_assignments FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_operator_assignments' AND policyname = 'rls_operator_assignments_tenant_select') THEN
                        CREATE POLICY rls_operator_assignments_tenant_select ON production_operator_assignments FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_operator_assignments' AND policyname = 'rls_operator_assignments_tenant_insert') THEN
                        CREATE POLICY rls_operator_assignments_tenant_insert ON production_operator_assignments FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_operator_assignments' AND policyname = 'rls_operator_assignments_tenant_update') THEN
                        CREATE POLICY rls_operator_assignments_tenant_update ON production_operator_assignments FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 11. RLS — production_shift_handovers (SELECT/INSERT only — immutable)
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_shift_handovers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE production_shift_handovers TO crevafactos_runtime;

                ALTER TABLE production_shift_handovers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_shift_handovers FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_shift_handovers' AND policyname = 'rls_handovers_tenant_select') THEN
                        CREATE POLICY rls_handovers_tenant_select ON production_shift_handovers FOR SELECT TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = current_schema() AND tablename = 'production_shift_handovers' AND policyname = 'rls_handovers_tenant_insert') THEN
                        CREATE POLICY rls_handovers_tenant_insert ON production_shift_handovers FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 11b. Column-level grant on identity_subjects to allow facility membership validation via ExternalSubject
            migrationBuilder.Sql(
                """
                GRANT SELECT (id, external_issuer, external_subject, status) ON public.identity_subjects TO crevafactos_runtime;
                """);

            // 12. Expand audit action taxonomy (drop + recreate with 5L additions)
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
                            'production.order.exception.unassign'
                        ));
                """);

            // 13. Expand audit event taxonomy (drop + recreate with 5L additions)
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
                            'production.order.exception.unassigned'
                        ));
                """);

            // 14. Expand outbox message type taxonomy (drop + recreate with 5L additions)
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
                            'production-order-exception-unassigned'
                        ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_shift_handovers;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_operator_assignments;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_order_shift_assignments;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_shifts;");

            migrationBuilder.Sql(
                """
                ALTER TABLE production_execution_exceptions
                    DROP COLUMN IF EXISTS assigned_to_subject_id,
                    DROP COLUMN IF EXISTS assigned_at_utc,
                    DROP COLUMN IF EXISTS assigned_by_subject_id;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE production_orders
                    DROP COLUMN IF EXISTS current_shift_id,
                    DROP COLUMN IF EXISTS current_shift_code,
                    DROP COLUMN IF EXISTS current_shift_start_at_utc,
                    DROP COLUMN IF EXISTS current_shift_end_at_utc,
                    DROP COLUMN IF EXISTS primary_operator_subject_id,
                    DROP COLUMN IF EXISTS active_operator_count,
                    DROP COLUMN IF EXISTS last_assignment_at_utc;
                """);
        }
    }
}
