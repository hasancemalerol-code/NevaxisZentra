using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    public partial class AddMaintenanceReliabilityFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE maintenance_requests
                    ADD COLUMN IF NOT EXISTS failure_category VARCHAR(30) NOT NULL DEFAULT 'other';

                ALTER TABLE maintenance_requests
                    DROP CONSTRAINT IF EXISTS ck_maintenance_requests_failure_category;

                ALTER TABLE maintenance_requests
                    ADD CONSTRAINT ck_maintenance_requests_failure_category
                    CHECK (failure_category IN (
                        'mechanical', 'electrical', 'hydraulic', 'pneumatic', 'cooling',
                        'heating', 'control', 'mold', 'robot', 'auxiliary', 'other'));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE maintenance_requests
                    DROP CONSTRAINT IF EXISTS ck_maintenance_requests_failure_category;

                ALTER TABLE maintenance_requests
                    DROP COLUMN IF EXISTS failure_category;
                """);
        }
    }
}
