using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAvailabilityRiskFoundation : Migration
    {
        private static readonly string[] AvailabilityRiskStatusIndexColumns =
            ["tenant_id", "facility_id", "assessment_type", "status", "evaluated_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "available_after_reservation_quantity",
                table: "material_requirement_assessments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "erp_available_quantity",
                table: "material_requirement_assessments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "erp_reserved_quantity",
                table: "material_requirement_assessments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "erp_source_updated_at_utc",
                table: "material_requirement_assessments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "erp_unit",
                table: "material_requirement_assessments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shortage_quantity",
                table: "material_requirement_assessments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_freshness",
                table: "material_requirement_assessments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_requirement_assessments_tenant_id_facility_id_asse~",
                table: "material_requirement_assessments",
                columns: AvailabilityRiskStatusIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_material_requirement_assessments_tenant_id_facility_id_asse~",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "available_after_reservation_quantity",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "erp_available_quantity",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "erp_reserved_quantity",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "erp_source_updated_at_utc",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "erp_unit",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "shortage_quantity",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "source_freshness",
                table: "material_requirement_assessments");
        }
    }
}
