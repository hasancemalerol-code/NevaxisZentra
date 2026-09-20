using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class FixScenarioRecoveryRuntimeAccess : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            FixTable(migrationBuilder, "scenario_recovery_runs", "srr");
            FixTable(migrationBuilder, "scenario_recovery_options", "sro");
            FixTable(migrationBuilder, "scenario_recovery_prediction_accuracy", "srpa");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        private static void FixTable(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                DROP POLICY IF EXISTS {prefix}_tenant_isolation ON {tableName};
                DROP POLICY IF EXISTS rls_{prefix}_tenant_select ON {tableName};
                DROP POLICY IF EXISTS rls_{prefix}_tenant_insert ON {tableName};
                DROP POLICY IF EXISTS rls_{prefix}_tenant_update ON {tableName};

                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{prefix}_tenant_select ON {tableName}
                    FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);

                CREATE POLICY rls_{prefix}_tenant_insert ON {tableName}
                    FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);

                CREATE POLICY rls_{prefix}_tenant_update ON {tableName}
                    FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);

                REVOKE ALL ON TABLE {tableName} FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE {tableName} TO crevafactos_runtime;
                """);
        }
    }
}
