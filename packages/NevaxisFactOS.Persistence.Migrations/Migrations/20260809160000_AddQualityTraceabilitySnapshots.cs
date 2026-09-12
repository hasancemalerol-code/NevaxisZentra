using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    public partial class AddQualityTraceabilitySnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quality_inspections
                    ADD COLUMN IF NOT EXISTS plan_code_snapshot VARCHAR(100) NULL,
                    ADD COLUMN IF NOT EXISTS plan_name_snapshot VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS plan_revision_snapshot VARCHAR(50) NULL;

                ALTER TABLE quality_inspection_results
                    ADD COLUMN IF NOT EXISTS definition_code_snapshot VARCHAR(100) NULL,
                    ADD COLUMN IF NOT EXISTS definition_name_snapshot VARCHAR(200) NULL,
                    ADD COLUMN IF NOT EXISTS inspection_type_snapshot VARCHAR(20) NULL,
                    ADD COLUMN IF NOT EXISTS unit_snapshot VARCHAR(50) NULL,
                    ADD COLUMN IF NOT EXISTS nominal_value_snapshot NUMERIC NULL,
                    ADD COLUMN IF NOT EXISTS lower_limit_snapshot NUMERIC NULL,
                    ADD COLUMN IF NOT EXISTS upper_limit_snapshot NUMERIC NULL,
                    ADD COLUMN IF NOT EXISTS required_snapshot BOOLEAN NULL,
                    ADD COLUMN IF NOT EXISTS critical_snapshot BOOLEAN NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE quality_inspection_results
                    DROP COLUMN IF EXISTS critical_snapshot,
                    DROP COLUMN IF EXISTS required_snapshot,
                    DROP COLUMN IF EXISTS upper_limit_snapshot,
                    DROP COLUMN IF EXISTS lower_limit_snapshot,
                    DROP COLUMN IF EXISTS nominal_value_snapshot,
                    DROP COLUMN IF EXISTS unit_snapshot,
                    DROP COLUMN IF EXISTS inspection_type_snapshot,
                    DROP COLUMN IF EXISTS definition_name_snapshot,
                    DROP COLUMN IF EXISTS definition_code_snapshot;

                ALTER TABLE quality_inspections
                    DROP COLUMN IF EXISTS plan_revision_snapshot,
                    DROP COLUMN IF EXISTS plan_name_snapshot,
                    DROP COLUMN IF EXISTS plan_code_snapshot;
                """);
        }
    }
}
