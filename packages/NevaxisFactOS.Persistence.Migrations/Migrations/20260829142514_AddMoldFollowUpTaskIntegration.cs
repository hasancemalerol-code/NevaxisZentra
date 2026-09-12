using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldFollowUpTaskIntegration : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] AuditEventIndexColumns = ["tenant_id", "facility_id", "mold_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "follow_up_status",
                table: "molds",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "follow_up_updated_by",
                table: "molds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ai_task_id",
                table: "molds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mold_follow_up_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mold_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mold_follow_up_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_mold_follow_up_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mold_follow_up_audit_events_molds_mold_id",
                        column: x => x.mold_id,
                        principalTable: "molds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mold_follow_up_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mold_follow_up_audit_events_mold_id",
                table: "mold_follow_up_audit_events",
                column: "mold_id");

            migrationBuilder.CreateIndex(
                name: "IX_mold_follow_up_audit_events_tenant_id_facility_id_mold_id_o~",
                table: "mold_follow_up_audit_events",
                columns: AuditEventIndexColumns);

            migrationBuilder.Sql(
                """
                UPDATE molds
                SET follow_up_status = 'requires-review'
                WHERE follow_up_status = ''
                  AND lifecycle_state IN ('maintenance', 'active', 'trial', 'quarantined');

                ALTER TABLE mold_follow_up_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE mold_follow_up_audit_events FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE mold_follow_up_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE mold_follow_up_audit_events TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'mold_follow_up_audit_events'
                          AND policyname = 'rls_mfae_tenant_select') THEN
                        CREATE POLICY rls_mfae_tenant_select
                            ON mold_follow_up_audit_events
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'mold_follow_up_audit_events'
                          AND policyname = 'rls_mfae_tenant_insert') THEN
                        CREATE POLICY rls_mfae_tenant_insert
                            ON mold_follow_up_audit_events
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
                name: "mold_follow_up_audit_events");

            migrationBuilder.DropColumn(
                name: "follow_up_status",
                table: "molds");

            migrationBuilder.DropColumn(
                name: "follow_up_updated_by",
                table: "molds");

            migrationBuilder.DropColumn(
                name: "linked_ai_task_id",
                table: "molds");
        }
    }
}
