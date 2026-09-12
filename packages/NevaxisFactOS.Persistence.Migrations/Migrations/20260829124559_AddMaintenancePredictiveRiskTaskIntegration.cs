using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenancePredictiveRiskTaskIntegration : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] AuditEventIndexColumns = ["tenant_id", "facility_id", "assessment_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "follow_up_status",
                table: "maintenance_predictive_risk_assessments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ai_task_id",
                table: "maintenance_predictive_risk_assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "maintenance_predictive_risk_assessments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "maintenance_predictive_risk_assessments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "maintenance_predictive_risk_audit_events",
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
                    table.PrimaryKey("PK_maintenance_predictive_risk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_maintenance_predictive_risk_audit_events_facilities_tenant_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_predictive_risk_audit_events_maintenance_predic~",
                        column: x => x.assessment_id,
                        principalTable: "maintenance_predictive_risk_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_predictive_risk_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_predictive_risk_audit_events_assessment_id",
                table: "maintenance_predictive_risk_audit_events",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_predictive_risk_audit_events_tenant_id_facility~",
                table: "maintenance_predictive_risk_audit_events",
                columns: AuditEventIndexColumns);

            migrationBuilder.Sql(
                """
                UPDATE maintenance_predictive_risk_assessments
                SET follow_up_status = CASE
                    WHEN risk_band = 'insufficient-data' THEN 'data-review-needed'
                    WHEN risk_band = 'low' THEN 'monitoring'
                    ELSE 'active'
                END
                WHERE follow_up_status = '' OR follow_up_status = 'active';

                ALTER TABLE maintenance_predictive_risk_assessments
                    ADD CONSTRAINT ck_mpra_follow_up_status CHECK (
                        follow_up_status IN (
                            'active',
                            'data-review-needed',
                            'monitoring',
                            'task-draft-created',
                            'human-reviewed',
                            'resolved'));

                ALTER TABLE maintenance_predictive_risk_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_predictive_risk_audit_events FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE maintenance_predictive_risk_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE maintenance_predictive_risk_audit_events TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'maintenance_predictive_risk_audit_events'
                          AND policyname = 'rls_mprae_tenant_select') THEN
                        CREATE POLICY rls_mprae_tenant_select
                            ON maintenance_predictive_risk_audit_events
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'maintenance_predictive_risk_audit_events'
                          AND policyname = 'rls_mprae_tenant_insert') THEN
                        CREATE POLICY rls_mprae_tenant_insert
                            ON maintenance_predictive_risk_audit_events
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_predictive_risk_audit_events");

            migrationBuilder.DropColumn(
                name: "follow_up_status",
                table: "maintenance_predictive_risk_assessments");

            migrationBuilder.DropColumn(
                name: "linked_ai_task_id",
                table: "maintenance_predictive_risk_assessments");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "maintenance_predictive_risk_assessments");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "maintenance_predictive_risk_assessments");
        }
    }
}
