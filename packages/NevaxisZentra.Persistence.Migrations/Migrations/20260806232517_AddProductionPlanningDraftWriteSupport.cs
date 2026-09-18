using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPlanningDraftWriteSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES (
                    '10000000-0000-4000-8000-000000000010',
                    'production.planning.write',
                    'Create and edit draft production plans within authorized facilities.',
                    'Active',
                    '2026-08-07T00:00:00Z')
                ON CONFLICT (id) DO NOTHING;
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
                            'production.plan.draft.update'
                        ));

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
                            'production.plan.draft.updated'
                        ));
                """);

            migrationBuilder.Sql(
                """
                GRANT INSERT, UPDATE ON TABLE production_plans TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plans'
                          AND policyname = 'production_plans_insert_current'
                    ) THEN
                        CREATE POLICY production_plans_insert_current
                            ON production_plans FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_tenant_id());
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_plans'
                          AND policyname = 'production_plans_update_current'
                    ) THEN
                        CREATE POLICY production_plans_update_current
                            ON production_plans FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_tenant_id())
                            WITH CHECK (tenant_id = current_tenant_id());
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS production_plans_update_current ON production_plans;
                DROP POLICY IF EXISTS production_plans_insert_current ON production_plans;

                REVOKE INSERT, UPDATE ON TABLE production_plans FROM crevafactos_runtime;
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
                            'ai.decision.append'
                        ));

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
                            'ai.decision.recorded'
                        ));
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id = '10000000-0000-4000-8000-000000000010';
                """);
        }
    }
}
