using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class GrantScenarioRecoveryRuntimeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT DELETE ON TABLE scenario_recovery_runs TO crevafactos_runtime;
                GRANT DELETE ON TABLE scenario_recovery_options TO crevafactos_runtime;
                GRANT DELETE ON TABLE scenario_recovery_prediction_accuracy TO crevafactos_runtime;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE DELETE ON TABLE scenario_recovery_runs FROM crevafactos_runtime;
                REVOKE DELETE ON TABLE scenario_recovery_options FROM crevafactos_runtime;
                REVOKE DELETE ON TABLE scenario_recovery_prediction_accuracy FROM crevafactos_runtime;
                """);
        }
    }
}
