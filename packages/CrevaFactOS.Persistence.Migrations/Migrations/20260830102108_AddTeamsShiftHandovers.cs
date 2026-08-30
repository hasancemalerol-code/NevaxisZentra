using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsShiftHandovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_shift_handovers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    safety_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_purpose = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_shift_handovers", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_shift_handovers_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_shift_handovers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_shift_handover_acknowledgements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_by_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_shift_handover_acknowledgements", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_acknowledgements_teams_shift_handovers~",
                        column: x => x.handover_id,
                        principalTable: "teams_shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_acknowledgements_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_shift_handover_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_tenant_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_shift_handover_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_audit_events_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_audit_events_teams_shift_handovers_han~",
                        column: x => x.handover_id,
                        principalTable: "teams_shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_shift_handover_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    item_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_shift_handover_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_items_teams_shift_handovers_handover_id",
                        column: x => x.handover_id,
                        principalTable: "teams_shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_shift_handover_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_shift_handover_acknowledgements_handover_id",
                table: "teams_shift_handover_acknowledgements",
                column: "handover_id");

            migrationBuilder.CreateIndex(
                name: "ux_teams_shift_handover_acks_tenant_handover_member",
                table: "teams_shift_handover_acknowledgements",
                columns: new[] { "tenant_id", "handover_id", "acknowledged_by_tenant_membership_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_shift_handover_audit_events_handover_id",
                table: "teams_shift_handover_audit_events",
                column: "handover_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_shift_handover_audit_tenant_handover",
                table: "teams_shift_handover_audit_events",
                columns: new[] { "tenant_id", "facility_id", "handover_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_shift_handover_items_handover_id",
                table: "teams_shift_handover_items",
                column: "handover_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_shift_handover_items_tenant_handover",
                table: "teams_shift_handover_items",
                columns: new[] { "tenant_id", "handover_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_teams_shift_handovers_tenant_facility_channel_date",
                table: "teams_shift_handovers",
                columns: new[] { "tenant_id", "facility_id", "channel_id", "shift_date" });

            migrationBuilder.CreateIndex(
                name: "ix_teams_shift_handovers_tenant_facility_status",
                table: "teams_shift_handovers",
                columns: new[] { "tenant_id", "facility_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_teams_shift_handovers_tenant_idempotency",
                table: "teams_shift_handovers",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE teams_shift_handovers
                    ADD CONSTRAINT ck_teams_shift_handovers_status
                        CHECK (status IN ('draft', 'published'));
                ALTER TABLE teams_shift_handovers
                    ADD CONSTRAINT ck_teams_shift_handovers_shift_type
                        CHECK (shift_type IN ('morning', 'evening', 'night', 'custom'));
                ALTER TABLE teams_shift_handover_items
                    ADD CONSTRAINT ck_teams_shift_handover_items_kind
                        CHECK (item_kind IN ('attention', 'incident', 'pending-work', 'quality-note', 'maintenance-note', 'material-note'));
                ALTER TABLE teams_shift_handover_items
                    ADD CONSTRAINT ck_teams_shift_handover_items_exclusive_source
                        CHECK (NOT (message_id IS NOT NULL AND ai_task_id IS NOT NULL));

                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000099',
                     'teams.handover.read', 'teams.handover.read',
                     'Read accessible channel-scoped Teams shift handovers.',
                     'Vardiya devrini görüntüle', 'teams', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-00000000009a',
                     'teams.handover.create', 'teams.handover.create',
                     'Create and publish own Teams shift handovers.',
                     'Vardiya devri oluştur', 'teams', 'create',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-00000000009b',
                     'teams.handover.acknowledge', 'teams.handover.acknowledge',
                     'Acknowledge published Teams shift handovers.',
                     'Vardiya devrini teslim al', 'teams', 'acknowledge',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-00000000009c',
                     'teams.handover.manage', 'teams.handover.manage',
                     'View and manage all authorized Teams shift handovers in a channel.',
                     'Vardiya devirlerini yönet', 'teams', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'teams.read'
                JOIN permission_definitions read_perm
                    ON read_perm.key = 'teams.handover.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, create_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions write_perm
                    ON write_perm.id = existing.permission_definition_id
                   AND write_perm.key = 'teams.write'
                JOIN permission_definitions create_perm
                    ON create_perm.key = 'teams.handover.create'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = create_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, ack_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions write_perm
                    ON write_perm.id = existing.permission_definition_id
                   AND write_perm.key = 'teams.write'
                JOIN permission_definitions ack_perm
                    ON ack_perm.key = 'teams.handover.acknowledge'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = ack_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions manage_source
                    ON manage_source.id = existing.permission_definition_id
                   AND manage_source.key = 'teams.manage'
                JOIN permission_definitions manage_perm
                    ON manage_perm.key = 'teams.handover.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                REVOKE ALL ON TABLE teams_shift_handovers FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE teams_shift_handovers TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_shift_handover_items FROM crevafactos_runtime;
                GRANT SELECT, INSERT, DELETE ON TABLE teams_shift_handover_items TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_shift_handover_acknowledgements FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_shift_handover_acknowledgements TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_shift_handover_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_shift_handover_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "teams_shift_handovers", "tsh");
            ApplyTenantRlsWithDelete(migrationBuilder, "teams_shift_handover_items", "tshi");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_shift_handover_acknowledgements", "tsha");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_shift_handover_audit_events", "tshae");
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

        private static void ApplyTenantRlsWithDelete(MigrationBuilder migrationBuilder, string tableName, string prefix)
        {
            ApplyTenantRls(migrationBuilder, tableName, prefix);
            migrationBuilder.Sql(
                $"""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_delete') THEN
                        CREATE POLICY rls_{prefix}_tenant_delete
                            ON {tableName}
                            FOR DELETE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teams_shift_handover_acknowledgements");

            migrationBuilder.DropTable(
                name: "teams_shift_handover_audit_events");

            migrationBuilder.DropTable(
                name: "teams_shift_handover_items");

            migrationBuilder.DropTable(
                name: "teams_shift_handovers");
        }
    }
}
