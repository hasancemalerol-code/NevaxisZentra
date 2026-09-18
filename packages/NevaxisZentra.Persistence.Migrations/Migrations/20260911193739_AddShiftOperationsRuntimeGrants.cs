using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftOperationsRuntimeGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE operational_shift_instances TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_open_items TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_handover_acceptances TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
