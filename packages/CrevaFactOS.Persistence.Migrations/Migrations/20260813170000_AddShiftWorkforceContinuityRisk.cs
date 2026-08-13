using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftWorkforceContinuityRisk : Migration
    {
        private static readonly string[] TenantFacilityTypeEvaluatedColumns = ["tenant_id", "facility_id", "assessment_type", "evaluated_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assessment_type",
                table: "shift_risk_assessments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "coverage");

            migrationBuilder.AddColumn<int>(
                name: "approved_assignment_count",
                table: "shift_risk_assessments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "planned_assignment_minutes",
                table: "shift_risk_assessments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "workforce_person_id",
                table: "shift_risk_assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workforce_person_id",
                table: "shift_risk_drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_assessments_tenant_facility_type_evaluated",
                table: "shift_risk_assessments",
                columns: TenantFacilityTypeEvaluatedColumns);

            migrationBuilder.DropIndex(
                name: "ix_shift_risk_drivers_tenant_assessment_reason",
                table: "shift_risk_drivers");

            migrationBuilder.Sql(
                """
                UPDATE shift_risk_assessments
                SET assessment_type = 'coverage'
                WHERE assessment_type IS NULL OR assessment_type = '';

                ALTER TABLE shift_risk_assessments
                    ADD CONSTRAINT ck_sra_assessments_assignment_counts
                        CHECK (approved_assignment_count >= 0 AND planned_assignment_minutes >= 0);

                CREATE UNIQUE INDEX ix_shift_risk_drivers_tenant_assessment_reason_person
                    ON shift_risk_drivers (
                        tenant_id,
                        assessment_id,
                        reason_code,
                        COALESCE(workforce_person_id, '00000000-0000-0000-0000-000000000000'::uuid));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ix_shift_risk_drivers_tenant_assessment_reason_person;

                ALTER TABLE shift_risk_assessments
                    DROP CONSTRAINT IF EXISTS ck_sra_assessments_assignment_counts;
                """);

            migrationBuilder.DropIndex(
                name: "ix_shift_risk_assessments_tenant_facility_type_evaluated",
                table: "shift_risk_assessments");

            migrationBuilder.DropColumn(
                name: "assessment_type",
                table: "shift_risk_assessments");

            migrationBuilder.DropColumn(
                name: "approved_assignment_count",
                table: "shift_risk_assessments");

            migrationBuilder.DropColumn(
                name: "planned_assignment_minutes",
                table: "shift_risk_assessments");

            migrationBuilder.DropColumn(
                name: "workforce_person_id",
                table: "shift_risk_assessments");

            migrationBuilder.DropColumn(
                name: "workforce_person_id",
                table: "shift_risk_drivers");

            migrationBuilder.CreateIndex(
                name: "ix_shift_risk_drivers_tenant_assessment_reason",
                table: "shift_risk_drivers",
                columns: ["tenant_id", "assessment_id", "reason_code"],
                unique: true);
        }
    }
}
