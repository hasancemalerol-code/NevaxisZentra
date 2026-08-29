using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionFollowUpTaskIntegration : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] AuditEventIndexColumns = ["tenant_id", "facility_id", "exception_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "follow_up_status",
                table: "production_execution_exceptions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "follow_up_updated_by",
                table: "production_execution_exceptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ai_task_id",
                table: "production_execution_exceptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "production_follow_up_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exception_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_follow_up_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_follow_up_audit_events_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_follow_up_audit_events_production_execution_exce~",
                        column: x => x.exception_id,
                        principalTable: "production_execution_exceptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_follow_up_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_follow_up_audit_events_exception_id",
                table: "production_follow_up_audit_events",
                column: "exception_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_follow_up_audit_events_tenant_id_facility_id_exc~",
                table: "production_follow_up_audit_events",
                columns: AuditEventIndexColumns);

            migrationBuilder.Sql(
                """
                UPDATE production_execution_exceptions
                SET follow_up_status = 'requires-review'
                WHERE follow_up_status = ''
                  AND status IN ('open', 'acknowledged');

                ALTER TABLE production_follow_up_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE production_follow_up_audit_events FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE production_follow_up_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE production_follow_up_audit_events TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_follow_up_audit_events'
                          AND policyname = 'rls_pfae_tenant_select') THEN
                        CREATE POLICY rls_pfae_tenant_select
                            ON production_follow_up_audit_events
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'production_follow_up_audit_events'
                          AND policyname = 'rls_pfae_tenant_insert') THEN
                        CREATE POLICY rls_pfae_tenant_insert
                            ON production_follow_up_audit_events
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_follow_up_audit_events");

            migrationBuilder.DropColumn(
                name: "follow_up_status",
                table: "production_execution_exceptions");

            migrationBuilder.DropColumn(
                name: "follow_up_updated_by",
                table: "production_execution_exceptions");

            migrationBuilder.DropColumn(
                name: "linked_ai_task_id",
                table: "production_execution_exceptions");
        }
    }
}
