using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsDirectMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_direct_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_low_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_high_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiated_by_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_purpose = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_direct_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_direct_conversations_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_conversations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_direct_read_states",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_read_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_direct_read_states", x => new { x.tenant_id, x.conversation_id, x.tenant_membership_id });
                    table.ForeignKey(
                        name: "FK_teams_direct_read_states_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_read_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_direct_conversation_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_direct_conversation_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_direct_conversation_members_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_conversation_members_teams_direct_conversation~",
                        column: x => x.conversation_id,
                        principalTable: "teams_direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_conversation_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_direct_message_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_tenant_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_direct_message_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_direct_message_audit_events_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_message_audit_events_teams_direct_conversation~",
                        column: x => x.conversation_id,
                        principalTable: "teams_direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_message_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_direct_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_direct_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_direct_messages_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_messages_teams_direct_conversations_conversati~",
                        column: x => x.conversation_id,
                        principalTable: "teams_direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_direct_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_direct_conversation_members_conversation_id",
                table: "teams_direct_conversation_members",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_direct_conversation_members_tenant_facility_membership",
                table: "teams_direct_conversation_members",
                columns: new[] { "tenant_id", "facility_id", "tenant_membership_id" });

            migrationBuilder.CreateIndex(
                name: "ux_teams_direct_conversation_members_conversation_membership",
                table: "teams_direct_conversation_members",
                columns: new[] { "tenant_id", "conversation_id", "tenant_membership_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_teams_direct_conversations_tenant_facility_pair",
                table: "teams_direct_conversations",
                columns: new[] { "tenant_id", "facility_id", "participant_low_membership_id", "participant_high_membership_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_teams_direct_conversations_tenant_idempotency",
                table: "teams_direct_conversations",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_direct_message_audit_events_conversation_id",
                table: "teams_direct_message_audit_events",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_direct_message_audit_tenant_conversation",
                table: "teams_direct_message_audit_events",
                columns: new[] { "tenant_id", "facility_id", "conversation_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_direct_messages_conversation_id",
                table: "teams_direct_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_direct_messages_tenant_facility_conversation_created",
                table: "teams_direct_messages",
                columns: new[] { "tenant_id", "facility_id", "conversation_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_teams_direct_messages_tenant_idempotency",
                table: "teams_direct_messages",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teams_direct_read_states_tenant_facility_membership",
                table: "teams_direct_read_states",
                columns: new[] { "tenant_id", "facility_id", "tenant_membership_id" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-00000000009d',
                     'teams.direct.read', 'teams.direct.read',
                     'Read own Teams direct conversations and messages.',
                     'Özel mesajları görüntüle', 'teams', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 14:00:00+00'),
                    ('10000000-0000-4000-8000-00000000009e',
                     'teams.direct.write', 'teams.direct.write',
                     'Start direct conversations, post messages and mark read state.',
                     'Özel mesaj gönder', 'teams', 'write',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 14:00:00+00'),
                    ('10000000-0000-4000-8000-00000000009f',
                     'teams.direct.manage', 'teams.direct.manage',
                     'Limited administrative inspection of Teams direct messaging.',
                     'Özel mesajları yönet', 'teams', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 14:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'teams.read'
                JOIN permission_definitions read_perm
                    ON read_perm.key = 'teams.direct.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, write_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions write_source
                    ON write_source.id = existing.permission_definition_id
                   AND write_source.key = 'teams.write'
                JOIN permission_definitions write_perm
                    ON write_perm.key = 'teams.direct.write'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = write_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, manage_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions manage_source
                    ON manage_source.id = existing.permission_definition_id
                   AND manage_source.key = 'teams.manage'
                JOIN permission_definitions manage_perm
                    ON manage_perm.key = 'teams.direct.manage'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = manage_perm.id);

                REVOKE ALL ON TABLE teams_direct_conversations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_direct_conversations TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_direct_conversation_members FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_direct_conversation_members TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_direct_messages FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_direct_messages TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_direct_read_states FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE teams_direct_read_states TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_direct_message_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_direct_message_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_direct_conversations", "tdc");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_direct_conversation_members", "tdcm");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_direct_messages", "tdm");
            ApplyTenantRls(migrationBuilder, "teams_direct_read_states", "tdrs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_direct_message_audit_events", "tdmae");
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
                name: "teams_direct_conversation_members");

            migrationBuilder.DropTable(
                name: "teams_direct_message_audit_events");

            migrationBuilder.DropTable(
                name: "teams_direct_messages");

            migrationBuilder.DropTable(
                name: "teams_direct_read_states");

            migrationBuilder.DropTable(
                name: "teams_direct_conversations");
        }
    }
}
