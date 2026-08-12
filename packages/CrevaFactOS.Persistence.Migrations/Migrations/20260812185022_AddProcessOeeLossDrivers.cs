using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessOeeLossDrivers : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantAssessmentReasonUniqueColumns = ["tenant_id", "assessment_id", "reason_code"];
        private static readonly string[] TenantFacilityAssessmentIndexColumns = ["tenant_id", "facility_id", "assessment_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_oee_loss_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operational_priority = table.Column<int>(type: "integer", nullable: false),
                    observed_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    expected_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    absolute_loss_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    affected_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    affected_quantity = table.Column<int>(type: "integer", nullable: true),
                    source_references_json = table.Column<string>(type: "text", nullable: false),
                    evidence_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    explanation_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_limitations_json = table.Column<string>(type: "text", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_leading_loss = table.Column<bool>(type: "boolean", nullable: false),
                    normalized_impact_points = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_loss_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_loss_drivers_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_loss_drivers_process_oee_assessments_assessment~",
                        column: x => x.assessment_id,
                        principalTable: "process_oee_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_loss_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_loss_drivers_assessment_id",
                table: "process_oee_loss_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_loss_drivers_tenant_assessment_reason",
                table: "process_oee_loss_drivers",
                columns: TenantAssessmentReasonUniqueColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_loss_drivers_tenant_facility_assessment",
                table: "process_oee_loss_drivers",
                columns: TenantFacilityAssessmentIndexColumns);

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE process_oee_loss_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_loss_drivers TO crevafactos_runtime;

                ALTER TABLE process_oee_loss_drivers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_loss_drivers FORCE ROW LEVEL SECURITY;

                CREATE POLICY pold_tenant_select ON process_oee_loss_drivers
                    FOR SELECT
                    USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);

                CREATE POLICY pold_tenant_insert ON process_oee_loss_drivers
                    FOR INSERT
                    WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_oee_loss_drivers");
        }
    }
}
