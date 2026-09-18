using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityAndProductionPlanningFoundation : Migration
    {
        private static readonly string[] FacilityTenantCodeColumns = ["tenant_id", "code"];
        private static readonly string[] FacilityTenantIdColumns = ["tenant_id", "id"];
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] ProductionPlanReferenceColumns = ["tenant_id", "facility_id", "reference_code"];
        private static readonly string[] ProductionPlanStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] ProductionPlanUpdatedColumns = ["tenant_id", "facility_id", "updated_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facilities", x => x.id);
                    table.UniqueConstraint("AK_facilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_facilities_code", "code <> ''");
                    table.CheckConstraint("ck_facilities_name", "name <> ''");
                    table.CheckConstraint("ck_facilities_status", "status IN ('Active', 'Inactive')");
                    table.CheckConstraint("ck_facilities_time_zone_id", "time_zone_id <> ''");
                    table.ForeignKey(
                        name: "FK_facilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    origin = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ai_generated = table.Column<bool>(type: "boolean", nullable: false),
                    confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    planned_start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    approval_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    transfer_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    transfer_target_system = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ai_explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    demands_json = table.Column<string>(type: "jsonb", nullable: false),
                    risks_json = table.Column<string>(type: "jsonb", nullable: false),
                    resource_assignment_json = table.Column<string>(type: "jsonb", nullable: false),
                    time_window_json = table.Column<string>(type: "jsonb", nullable: false),
                    approval_json = table.Column<string>(type: "jsonb", nullable: false),
                    transfer_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_references_json = table.Column<string>(type: "jsonb", nullable: false),
                    history_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_plans", x => x.id);
                    table.CheckConstraint("ck_production_plans_approval_json", "jsonb_typeof(approval_json) = 'object'");
                    table.CheckConstraint("ck_production_plans_approval_status", "approval_status IN ('not-required', 'pending', 'approved', 'rejected', 'expired')");
                    table.CheckConstraint("ck_production_plans_confidence", "confidence IN ('low', 'medium', 'high')");
                    table.CheckConstraint("ck_production_plans_created_by", "created_by <> ''");
                    table.CheckConstraint("ck_production_plans_demands_json", "jsonb_typeof(demands_json) = 'array'");
                    table.CheckConstraint("ck_production_plans_history_json", "jsonb_typeof(history_json) = 'array'");
                    table.CheckConstraint("ck_production_plans_origin", "origin IN ('approved-order', 'erp-production-request', 'manual', 'ai-recommendation')");
                    table.CheckConstraint("ck_production_plans_planned_window", "planned_end_at_utc >= planned_start_at_utc");
                    table.CheckConstraint("ck_production_plans_reference_code", "reference_code <> ''");
                    table.CheckConstraint("ck_production_plans_resource_assignment_json", "jsonb_typeof(resource_assignment_json) = 'object'");
                    table.CheckConstraint("ck_production_plans_risks_json", "jsonb_typeof(risks_json) = 'array'");
                    table.CheckConstraint("ck_production_plans_source_references_json", "jsonb_typeof(source_references_json) = 'array'");
                    table.CheckConstraint("ck_production_plans_status", "status IN ('draft', 'needs-review', 'ready-for-approval', 'approved', 'rejected', 'scheduled', 'in-progress', 'completed', 'cancelled')");
                    table.CheckConstraint("ck_production_plans_time_window_json", "jsonb_typeof(time_window_json) = 'object'");
                    table.CheckConstraint("ck_production_plans_title", "title <> ''");
                    table.CheckConstraint("ck_production_plans_transfer_json", "jsonb_typeof(transfer_json) = 'object'");
                    table.CheckConstraint("ck_production_plans_transfer_status", "transfer_status IN ('not-started', 'pending', 'in-progress', 'completed', 'failed')");
                    table.CheckConstraint("ck_production_plans_transfer_target_system", "transfer_target_system IS NULL OR transfer_target_system IN ('erp', 'mes', 'crevafactos')");
                    table.ForeignKey(
                        name: "FK_production_plans_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_plans_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_facilities_tenant_id_code",
                table: "facilities",
                columns: FacilityTenantCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facilities_tenant_id_id",
                table: "facilities",
                columns: FacilityTenantIdColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_plans_tenant_id_facility_id_reference_code",
                table: "production_plans",
                columns: ProductionPlanReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_plans_tenant_id_facility_id_status",
                table: "production_plans",
                columns: ProductionPlanStatusColumns);

            migrationBuilder.CreateIndex(
                name: "IX_production_plans_tenant_id_facility_id_updated_at_utc",
                table: "production_plans",
                columns: ProductionPlanUpdatedColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_plans");

            migrationBuilder.DropTable(
                name: "facilities");
        }
    }
}
