using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenDemoCustomsFileRuntimeAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_outcomes TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_documents TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_blockers TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_corrective_works TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_packages TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_verification_runs TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_activities TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_outcomes", "gdcfo");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_documents", "gdcfd");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_blockers", "gdcfbk");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_corrective_works", "gdcfcw");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_packages", "gdcfpk");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_verification_runs", "gdcfvr");
            ApplyTenantRls(migrationBuilder, "golden_demo_customs_file_activities", "gdcfac");
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_outcomes FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_documents FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_blockers FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_corrective_works FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_packages FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_verification_runs FROM crevafactos_runtime;
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE golden_demo_customs_file_activities FROM crevafactos_runtime;
                """);
        }
    }
}
