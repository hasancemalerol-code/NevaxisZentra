using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryShiftHandoverFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factory_shift_handovers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_reference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    outgoing_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    handed_over_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_shift_handovers", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_shift_handovers_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "factory_shift_handover_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    actor_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_shift_handover_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_shift_handover_events_facilities_tenant_id_facility~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_shift_handover_events_factory_shift_handovers_hando~",
                        column: x => x.handover_id,
                        principalTable: "factory_shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "factory_shift_handover_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_module = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requires_follow_up = table.Column<bool>(type: "boolean", nullable: false),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_metadata_json = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_shift_handover_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_shift_handover_items_factory_shift_handovers_handov~",
                        column: x => x.handover_id,
                        principalTable: "factory_shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_factory_shift_handover_events_handover_id",
                table: "factory_shift_handover_events",
                column: "handover_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handover_events_tenant_handover_occurred",
                table: "factory_shift_handover_events",
                columns: new[] { "tenant_id", "facility_id", "handover_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_factory_shift_handover_items_handover_id",
                table: "factory_shift_handover_items",
                column: "handover_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handover_items_tenant_handover_sort",
                table: "factory_shift_handover_items",
                columns: new[] { "tenant_id", "handover_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_tenant_facility_shift_date",
                table: "factory_shift_handovers",
                columns: new[] { "tenant_id", "facility_id", "shift_date" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_tenant_facility_status",
                table: "factory_shift_handovers",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_handovers_tenant_reference",
                table: "factory_shift_handovers",
                columns: new[] { "tenant_id", "handover_reference" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE factory_shift_handovers
                    ADD CONSTRAINT ck_factory_shift_handovers_status
                        CHECK (status IN ('draft', 'ready-for-handover', 'handed-over', 'acknowledged', 'completed', 'cancelled'));
                ALTER TABLE factory_shift_handovers
                    ADD CONSTRAINT ck_factory_shift_handovers_shift_period
                        CHECK (shift_period IN ('morning', 'evening', 'night', 'custom'));
                ALTER TABLE factory_shift_handover_items
                    ADD CONSTRAINT ck_factory_shift_handover_items_category
                        CHECK (category IN ('production', 'machine', 'maintenance', 'quality', 'material', 'order', 'safety', 'general'));
                ALTER TABLE factory_shift_handover_items
                    ADD CONSTRAINT ck_factory_shift_handover_items_severity
                        CHECK (severity IN ('low', 'medium', 'high', 'critical'));
                ALTER TABLE factory_shift_handover_items
                    ADD CONSTRAINT ck_factory_shift_handover_items_status
                        CHECK (status IN ('open', 'resolved', 'escalated'));

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000a2',
                     'shift-handover.read', 'shift-handover.read',
                     'Read factory shift handovers for accessible facilities.',
                     'Vardiya devrini görüntüle', 'shift-handover', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 00:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000a3',
                     'shift-handover.manage', 'shift-handover.manage',
                     'Create and manage factory shift handovers.',
                     'Vardiya devrini yönet', 'shift-handover', 'manage',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 00:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000a4',
                     'shift-handover.acknowledge', 'shift-handover.acknowledge',
                     'Acknowledge incoming factory shift handovers.',
                     'Vardiya devrini teslim al', 'shift-handover', 'acknowledge',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-02 00:00:00+00')
                ON CONFLICT (key) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'shift-workforce.read'
                JOIN permission_definitions read_perm
                    ON read_perm.key = 'shift-handover.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'shift-workforce.manage'
                JOIN permission_definitions manage_perm
                    ON manage_perm.key = 'shift-handover.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, ack_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'shift-workforce.read'
                JOIN permission_definitions ack_perm
                    ON ack_perm.key = 'shift-handover.acknowledge'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = ack_perm.id);

                REVOKE ALL ON TABLE factory_shift_handovers FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_shift_handovers TO crevafactos_runtime;
                REVOKE ALL ON TABLE factory_shift_handover_items FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_shift_handover_items TO crevafactos_runtime;
                REVOKE ALL ON TABLE factory_shift_handover_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE factory_shift_handover_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "factory_shift_handovers", "sh");
            ApplyTenantRls(migrationBuilder, "factory_shift_handover_items", "shi");
            ApplyTenantRlsAppendOnly(migrationBuilder, "factory_shift_handover_events", "she");
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        private static void ApplyTenantRlsAppendOnly(MigrationBuilder migrationBuilder, string tableName, string prefix)
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "factory_shift_handover_events");

            migrationBuilder.DropTable(
                name: "factory_shift_handover_items");

            migrationBuilder.DropTable(
                name: "factory_shift_handovers");
        }
    }
}
