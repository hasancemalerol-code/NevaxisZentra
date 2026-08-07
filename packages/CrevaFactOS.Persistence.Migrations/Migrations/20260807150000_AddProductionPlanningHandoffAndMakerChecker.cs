using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPlanningHandoffAndMakerChecker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Handoff permission definition
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES (
                    '10000000-0000-4000-8000-000000000015',
                    'production.planning.handoff',
                    'Prepare production handoff snapshots for scheduled plans to be consumed by the Production Execution module.',
                    'Active',
                    '2026-08-07T00:00:00Z')
                ON CONFLICT (id) DO NOTHING;
                """);

            // 2. Add last_modified_by_subject_id column to production_plans
            migrationBuilder.Sql(
                """
                ALTER TABLE production_plans
                    ADD COLUMN IF NOT EXISTS last_modified_by_subject_id UUID NULL;
                """);

            // 3. Create production_plan_handoffs table
            migrationBuilder.Sql(
                """
                CREATE TABLE production_plan_handoffs (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    production_plan_id UUID NOT NULL,
                    production_plan_version BIGINT NOT NULL,
                    reference_code VARCHAR(100) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    idempotency_key VARCHAR(500) NOT NULL,
                    prepared_at_utc TIMESTAMPTZ NOT NULL,
                    prepared_by_subject_id VARCHAR(200) NOT NULL,
                    consumed_at_utc TIMESTAMPTZ NULL,
                    production_reference VARCHAR(500) NULL,
                    failure_code VARCHAR(100) NULL,
                    correlation_id UUID NOT NULL,
                    version BIGINT NOT NULL,
                    payload_json JSONB NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_production_plan_handoffs PRIMARY KEY (id),
                    CONSTRAINT fk_handoffs_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_handoffs_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_handoffs_plan FOREIGN KEY (production_plan_id) REFERENCES production_plans(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_handoffs_status CHECK (status IN ('prepared', 'consumed', 'failed')),
                    CONSTRAINT ck_handoffs_idempotency_key CHECK (idempotency_key <> ''),
                    CONSTRAINT ck_handoffs_reference_code CHECK (reference_code <> ''),
                    CONSTRAINT ck_handoffs_correlation CHECK (correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid),
                    CONSTRAINT ck_handoffs_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX ix_handoffs_idempotency_key
                    ON production_plan_handoffs (idempotency_key);

                CREATE INDEX ix_handoffs_plan
                    ON production_plan_handoffs (tenant_id, facility_id, production_plan_id);

                CREATE INDEX ix_handoffs_tenant_status
                    ON production_plan_handoffs (tenant_id, status);

                CREATE INDEX "IX_production_plan_handoffs_production_plan_id"
                    ON production_plan_handoffs (production_plan_id);
                """);

            // 4. Runtime grants for handoffs table
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE production_plan_handoffs FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_handoffs TO crevafactos_runtime;
                """);

            // 5. Row-level security for handoffs
            migrationBuilder.Sql(
                """
                ALTER TABLE production_plan_handoffs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_plan_handoffs FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_handoffs'
                          AND policyname = 'rls_handoffs_tenant_select') THEN
                        CREATE POLICY rls_handoffs_tenant_select
                            ON production_plan_handoffs
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_handoffs'
                          AND policyname = 'rls_handoffs_tenant_insert') THEN
                        CREATE POLICY rls_handoffs_tenant_insert
                            ON production_plan_handoffs
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plan_handoffs'
                          AND policyname = 'rls_handoffs_tenant_update') THEN
                        CREATE POLICY rls_handoffs_tenant_update
                            ON production_plan_handoffs
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                            'production.plan.handoff.prepare'
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
                            'production.plan.handoff.prepared'
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
                            'production-plan-handoff-prepared'
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
                            'production-plan-transfer-uncertain'
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
                            'production.plan.transfer.uncertain'
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
                            'production.plan.transfer.mark.uncertain'
                        ));
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS production_plan_handoffs;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE production_plans
                    DROP COLUMN IF EXISTS last_modified_by_subject_id;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id = '10000000-0000-4000-8000-000000000015';
                """);
        }
    }
}
