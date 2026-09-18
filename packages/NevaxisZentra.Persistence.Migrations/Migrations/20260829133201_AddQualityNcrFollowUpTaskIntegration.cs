using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityNcrFollowUpTaskIntegration : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] AuditEventIndexColumns = ["tenant_id", "facility_id", "ncr_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "follow_up_status",
                table: "quality_nonconformances",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "follow_up_updated_by",
                table: "quality_nonconformances",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ai_task_id",
                table: "quality_nonconformances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "quality_ncr_follow_up_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ncr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_ncr_follow_up_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_quality_ncr_follow_up_audit_events_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_ncr_follow_up_audit_events_quality_nonconformances_~",
                        column: x => x.ncr_id,
                        principalTable: "quality_nonconformances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_ncr_follow_up_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quality_ncr_follow_up_audit_events_ncr_id",
                table: "quality_ncr_follow_up_audit_events",
                column: "ncr_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_ncr_follow_up_audit_events_tenant_id_facility_id_nc~",
                table: "quality_ncr_follow_up_audit_events",
                columns: AuditEventIndexColumns);

            migrationBuilder.Sql(
                """
                UPDATE quality_nonconformances
                SET follow_up_status = 'requires-review'
                WHERE follow_up_status = ''
                  AND status IN ('open', 'under-review', 'action-required');

                UPDATE quality_nonconformances
                SET follow_up_status = 'resolved'
                WHERE follow_up_status = ''
                  AND status IN ('resolved', 'closed', 'cancelled');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quality_ncr_follow_up_audit_events");

            migrationBuilder.DropColumn(
                name: "follow_up_status",
                table: "quality_nonconformances");

            migrationBuilder.DropColumn(
                name: "follow_up_updated_by",
                table: "quality_nonconformances");

            migrationBuilder.DropColumn(
                name: "linked_ai_task_id",
                table: "quality_nonconformances");
        }
    }
}
