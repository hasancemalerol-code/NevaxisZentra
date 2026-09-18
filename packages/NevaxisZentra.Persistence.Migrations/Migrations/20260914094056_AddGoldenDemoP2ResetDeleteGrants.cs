using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenDemoP2ResetDeleteGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT DELETE ON TABLE golden_demo_verification_runs TO crevafactos_runtime;
                GRANT DELETE ON TABLE golden_demo_agent_findings TO crevafactos_runtime;
                GRANT DELETE ON TABLE golden_demo_cycle_observations TO crevafactos_runtime;
                """);

            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_verification_runs", "gdvr");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_agent_findings", "gdaf");
            ApplyDeleteTenantRls(migrationBuilder, "golden_demo_cycle_observations", "gdco");
        }

        private static void ApplyDeleteTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_delete') THEN
                        CREATE POLICY rls_{prefix}_tenant_delete
                            ON {tableName}
                            FOR DELETE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE DELETE ON TABLE golden_demo_verification_runs FROM crevafactos_runtime;
                REVOKE DELETE ON TABLE golden_demo_agent_findings FROM crevafactos_runtime;
                REVOKE DELETE ON TABLE golden_demo_cycle_observations FROM crevafactos_runtime;
                """);
        }
    }
}
