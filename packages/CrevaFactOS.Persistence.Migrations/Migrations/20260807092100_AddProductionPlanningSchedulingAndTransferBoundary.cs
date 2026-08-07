using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPlanningSchedulingAndTransferBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. New permission definitions
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES (
                    '10000000-0000-4000-8000-000000000013',
                    'production.planning.schedule',
                    'Schedule approved production plans by assigning time windows within authorized facilities.',
                    'Active',
                    '2026-08-07T00:00:00Z')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES (
                    '10000000-0000-4000-8000-000000000014',
                    'production.planning.transfer',
                    'Create and execute transfer requests to ERP/MES systems for scheduled production plans.',
                    'Active',
                    '2026-08-07T00:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Create production_plan_transfer_requests table
            migrationBuilder.Sql(
                """
                CREATE TABLE production_plan_transfer_requests (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_plan_id UUID NOT NULL,
                    target_system VARCHAR(50) NOT NULL,
                    operation_type VARCHAR(100) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    idempotency_key VARCHAR(500) NOT NULL,
                    attempt_number INTEGER NOT NULL,
                    requested_by_subject_id VARCHAR(200) NOT NULL,
                    requested_at_utc TIMESTAMPTZ NOT NULL,
                    started_at_utc TIMESTAMPTZ NULL,
                    completed_at_utc TIMESTAMPTZ NULL,
                    external_reference VARCHAR(500) NULL,
                    failure_code VARCHAR(100) NULL,
                    safe_failure_message VARCHAR(2000) NULL,
                    retryable_failure BOOLEAN NULL,
                    response_fingerprint VARCHAR(200) NULL,
                    correlation_id UUID NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_production_plan_transfer_requests PRIMARY KEY (id),
                    CONSTRAINT fk_transfer_requests_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_transfer_requests_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_transfer_requests_plan FOREIGN KEY (production_plan_id) REFERENCES production_plans(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_transfer_requests_status CHECK (status IN ('requested', 'processing', 'succeeded', 'failed', 'uncertain')),
                    CONSTRAINT ck_transfer_requests_target_system CHECK (target_system IN ('erp', 'mes')),
                    CONSTRAINT ck_transfer_requests_operation_type CHECK (operation_type <> ''),
                    CONSTRAINT ck_transfer_requests_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_transfer_requests_attempt_number CHECK (attempt_number >= 1)
                );

                CREATE UNIQUE INDEX ix_transfer_requests_idempotency_key
                    ON production_plan_transfer_requests (idempotency_key);

                CREATE INDEX ix_transfer_requests_plan
                    ON production_plan_transfer_requests (tenant_id, facility_id, production_plan_id);

                CREATE INDEX ix_transfer_requests_tenant_status
                    ON production_plan_transfer_requests (tenant_id, status);

                CREATE INDEX ix_transfer_requests_tenant_target_status
                    ON production_plan_transfer_requests (tenant_id, target_system, status);

                CREATE INDEX "IX_production_plan_transfer_requests_production_plan_id"
                    ON production_plan_transfer_requests (production_plan_id);
                """);

            // 3. Runtime grants for transfer requests table
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_plan_transfer_requests FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_transfer_requests TO crevafactos_runtime;
                """);

            // 4. Row-level security for transfer requests
            migrationBuilder.Sql(
                """
                ALTER TABLE production_plan_transfer_requests ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_plan_transfer_requests FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_transfer_requests'
                          AND policyname = 'rls_transfer_requests_tenant_select') THEN
                        CREATE POLICY rls_transfer_requests_tenant_select
                            ON production_plan_transfer_requests
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_transfer_requests'
                          AND policyname = 'rls_transfer_requests_tenant_insert') THEN
                        CREATE POLICY rls_transfer_requests_tenant_insert
                            ON production_plan_transfer_requests
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_transfer_requests'
                          AND policyname = 'rls_transfer_requests_tenant_update') THEN
                        CREATE POLICY rls_transfer_requests_tenant_update
                            ON production_plan_transfer_requests
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);

            // 5. Extend production_plans transfer_status constraint to include 'uncertain'
            migrationBuilder.Sql(
                """
                ALTER TABLE production_plans
                    DROP CONSTRAINT ck_production_plans_transfer_status,
                    ADD CONSTRAINT ck_production_plans_transfer_status CHECK (
                        transfer_status IN ('not-started', 'pending', 'in-progress', 'completed', 'failed', 'uncertain'));
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
                            'production.plan.transfer.mark.uncertain'
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
                            'production.plan.transfer.uncertain'
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
                            'production-plan-transfer-uncertain'
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
                            'production-plan-rejected'
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
                            'production.plan.rejected'
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
                            'production.plan.workflow.reject'
                        ));
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE production_plans
                    DROP CONSTRAINT ck_production_plans_transfer_status,
                    ADD CONSTRAINT ck_production_plans_transfer_status CHECK (
                        transfer_status IN ('not-started', 'pending', 'in-progress', 'completed', 'failed'));
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS production_plan_transfer_requests;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000013',
                    '10000000-0000-4000-8000-000000000014');
                """);
        }
    }
}
