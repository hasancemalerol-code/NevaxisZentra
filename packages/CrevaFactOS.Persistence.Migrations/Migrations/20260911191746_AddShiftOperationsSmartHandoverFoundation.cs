using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftOperationsSmartHandoverFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operational_shift_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actual_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    handover_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    supervisor_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supervisor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    team_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    production_context_json = table.Column<string>(type: "text", nullable: false),
                    handover_readiness_score = table.Column<decimal>(type: "numeric", nullable: false),
                    ai_summary_json = table.Column<string>(type: "text", nullable: true),
                    factory_handover_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_shift_instances", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_shift_instances_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_handover_acceptances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factory_handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outgoing_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outgoing_supervisor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    incoming_supervisor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    prepared_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    handed_over_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outgoing_comments = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    incoming_acknowledgement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    verification_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_handover_acceptances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_open_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    primary_worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    digital_work_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deadline_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requires_handover = table.Column<bool>(type: "boolean", nullable: false),
                    handover_priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verification_required = table.Column<bool>(type: "boolean", nullable: false),
                    provenance_json = table.Column<string>(type: "text", nullable: true),
                    freshness_json = table.Column<string>(type: "text", nullable: true),
                    carried_from_shift_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    carried_to_shift_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    carryover_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_open_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_open_items_operational_shift_instances_shift_instance~",
                        column: x => x.shift_instance_id,
                        principalTable: "operational_shift_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operational_shift_instances_tenant_facility_date",
                table: "operational_shift_instances",
                columns: new[] { "tenant_id", "facility_id", "shift_date" });

            migrationBuilder.CreateIndex(
                name: "ix_operational_shift_instances_tenant_facility_status",
                table: "operational_shift_instances",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_handover_acceptances_tenant_shift",
                table: "shift_handover_acceptances",
                columns: new[] { "tenant_id", "shift_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shift_open_items_shift_instance_id",
                table: "shift_open_items",
                column: "shift_instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_open_items_tenant_shift_status",
                table: "shift_open_items",
                columns: new[] { "tenant_id", "shift_instance_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_open_items_tenant_shift_source",
                table: "shift_open_items",
                columns: new[] { "tenant_id", "shift_instance_id", "source_type", "source_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f20', 'shift.read', 'shift.read',
                     'View shift operations, active shift, and handover status.',
                     'Vardiya operasyonlarını görüntüle', 'shift', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f21', 'shift.manage', 'shift.manage',
                     'Manage shift planning and operational shift instances.',
                     'Vardiyayı yönet', 'shift', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f22', 'shift.start', 'shift.start',
                     'Start an operational shift.',
                     'Vardiyayı başlat', 'shift', 'start',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f23', 'shift.close', 'shift.close',
                     'Close an operational shift.',
                     'Vardiyayı kapat', 'shift', 'close',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f24', 'shift.handover.prepare', 'shift.handover.prepare',
                     'Prepare shift handover package.',
                     'Vardiya teslimini hazırla', 'shift', 'handover.prepare',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f25', 'shift.handover.submit', 'shift.handover.submit',
                     'Submit shift handover to incoming shift.',
                     'Vardiya teslimini gönder', 'shift', 'handover.submit',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f26', 'shift.handover.accept', 'shift.handover.accept',
                     'Accept incoming shift handover.',
                     'Vardiya teslimini devral', 'shift', 'handover.accept',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f27', 'shift.open-item.manage', 'shift.open-item.manage',
                     'Manage shift open items and dispositions.',
                     'Vardiya açık konularını yönet', 'shift', 'open-item.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-11 19:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE operational_shift_instances TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_open_items TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE shift_handover_acceptances TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "operational_shift_instances", "osi");
            ApplyTenantRls(migrationBuilder, "shift_open_items", "soi");
            ApplyTenantRls(migrationBuilder, "shift_handover_acceptances", "sha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000f20',
                    '10000000-0000-4000-8000-000000000f21',
                    '10000000-0000-4000-8000-000000000f22',
                    '10000000-0000-4000-8000-000000000f23',
                    '10000000-0000-4000-8000-000000000f24',
                    '10000000-0000-4000-8000-000000000f25',
                    '10000000-0000-4000-8000-000000000f26',
                    '10000000-0000-4000-8000-000000000f27');
                """);

            migrationBuilder.DropTable(
                name: "shift_handover_acceptances");

            migrationBuilder.DropTable(
                name: "shift_open_items");

            migrationBuilder.DropTable(
                name: "operational_shift_instances");
        }

        private static void ApplyTenantRls(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {tableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {tableName} FORCE ROW LEVEL SECURITY;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_select') THEN
                        CREATE POLICY rls_{prefix}_tenant_select
                            ON {tableName}
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert
                            ON {tableName}
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update
                            ON {tableName}
                            FOR UPDATE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
