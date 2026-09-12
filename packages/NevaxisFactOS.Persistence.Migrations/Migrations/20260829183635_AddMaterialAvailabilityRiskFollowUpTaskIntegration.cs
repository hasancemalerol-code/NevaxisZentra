using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialAvailabilityRiskFollowUpTaskIntegration : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] AuditEventIndexColumns = ["tenant_id", "facility_id", "assessment_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "follow_up_status",
                table: "material_requirement_assessments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "follow_up_updated_by",
                table: "material_requirement_assessments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ai_task_id",
                table: "material_requirement_assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_availability_risk_follow_up_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_availability_risk_follow_up_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_material_availability_risk_follow_up_audit_events_facilitie~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_availability_risk_follow_up_audit_events_material_~",
                        column: x => x.assessment_id,
                        principalTable: "material_requirement_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_availability_risk_follow_up_audit_events_tenants_t~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_material_availability_risk_follow_up_audit_events_assessmen~",
                table: "material_availability_risk_follow_up_audit_events",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_material_availability_risk_follow_up_audit_events_tenant_id~",
                table: "material_availability_risk_follow_up_audit_events",
                columns: AuditEventIndexColumns);

            migrationBuilder.Sql(
                """
                UPDATE material_requirement_assessments
                SET follow_up_status = 'requires-review'
                WHERE follow_up_status = ''
                  AND assessment_type = 'availability-risk';

                ALTER TABLE material_availability_risk_follow_up_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE material_availability_risk_follow_up_audit_events FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE material_availability_risk_follow_up_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE material_availability_risk_follow_up_audit_events TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'material_availability_risk_follow_up_audit_events'
                          AND policyname = 'rls_marfae_tenant_select') THEN
                        CREATE POLICY rls_marfae_tenant_select
                            ON material_availability_risk_follow_up_audit_events
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'material_availability_risk_follow_up_audit_events'
                          AND policyname = 'rls_marfae_tenant_insert') THEN
                        CREATE POLICY rls_marfae_tenant_insert
                            ON material_availability_risk_follow_up_audit_events
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                REVOKE ALL ON TABLE material_requirement_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE material_requirement_assessments TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'material_requirement_assessments'
                          AND policyname = 'rls_mra_tenant_update') THEN
                        CREATE POLICY rls_mra_tenant_update
                            ON material_requirement_assessments
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_availability_risk_follow_up_audit_events");

            migrationBuilder.DropColumn(
                name: "follow_up_status",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "follow_up_updated_by",
                table: "material_requirement_assessments");

            migrationBuilder.DropColumn(
                name: "linked_ai_task_id",
                table: "material_requirement_assessments");
        }
    }
}
