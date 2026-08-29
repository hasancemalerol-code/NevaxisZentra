using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantMembershipColumns = ["tenant_id", "tenant_membership_id"];
        private static readonly string[] TenantChannelMembershipColumns = ["tenant_id", "channel_id", "tenant_membership_id"];
        private static readonly string[] TenantFacilityTypeColumns = ["tenant_id", "facility_id", "channel_type"];
        private static readonly string[] TenantFacilityTaskColumns = ["tenant_id", "facility_id", "ai_task_id"];
        private static readonly string[] TenantFacilityChannelOccurredColumns = ["tenant_id", "facility_id", "channel_id", "occurred_at_utc"];
        private static readonly string[] TenantMessageMembershipColumns = ["tenant_id", "message_id", "mentioned_tenant_membership_id"];
        private static readonly string[] TenantFacilityChannelCreatedColumns = ["tenant_id", "facility_id", "channel_id", "created_at_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_channel_read_states",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_read_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_channel_read_states", x => new { x.tenant_id, x.channel_id, x.tenant_membership_id });
                    table.ForeignKey(
                        name: "FK_teams_channel_read_states_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_channel_read_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    channel_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_channels_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_channels_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_message_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_tenant_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_message_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_message_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_message_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_channel_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    added_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_channel_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_channel_members_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_channel_members_teams_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "teams_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_channel_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_messages_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_messages_teams_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "teams_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams_message_mentions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_tenant_membership_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_message_mentions", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_message_mentions_teams_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "teams_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_message_mentions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_channel_members_channel_id",
                table: "teams_channel_members",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_channel_members_tenant_id_facility_id",
                table: "teams_channel_members",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_teams_channel_members_tenant_membership",
                table: "teams_channel_members",
                columns: TenantMembershipColumns);

            migrationBuilder.CreateIndex(
                name: "ux_teams_channel_members_channel_membership",
                table: "teams_channel_members",
                columns: TenantChannelMembershipColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_channel_read_states_tenant_id_facility_id",
                table: "teams_channel_read_states",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_teams_channels_tenant_facility",
                table: "teams_channels",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_teams_channels_tenant_facility_task",
                table: "teams_channels",
                columns: TenantFacilityTaskColumns,
                filter: "ai_task_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_teams_channels_tenant_facility_type",
                table: "teams_channels",
                columns: TenantFacilityTypeColumns);

            migrationBuilder.CreateIndex(
                name: "ix_teams_message_audit_events_tenant_facility_channel",
                table: "teams_message_audit_events",
                columns: TenantFacilityChannelOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_teams_message_mentions_message_id",
                table: "teams_message_mentions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ux_teams_message_mentions_message_membership",
                table: "teams_message_mentions",
                columns: TenantMessageMembershipColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_messages_channel_id",
                table: "teams_messages",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_teams_messages_tenant_facility_channel_created",
                table: "teams_messages",
                columns: TenantFacilityChannelCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_teams_messages_tenant_idempotency",
                table: "teams_messages",
                columns: TenantIdempotencyColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000093',
                     'teams.read', 'teams.read',
                     'Read Teams channels, messages and read state where member.',
                     'Teams kanallarını görüntüle', 'teams', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000094',
                     'teams.write', 'teams.write',
                     'Post Teams messages and mark read state where member.',
                     'Teams mesajı gönder', 'teams', 'write',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000095',
                     'teams.manage', 'teams.manage',
                     'Create and manage Teams channels and membership where authorized.',
                     'Teams kanallarını yönet', 'teams', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-30 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE teams_channels
                    ADD CONSTRAINT ck_teams_channels_channel_type
                        CHECK (channel_type IN ('department', 'operational', 'task-context'));

                ALTER TABLE teams_channel_members
                    ADD CONSTRAINT ck_teams_channel_members_role
                        CHECK (role IN ('owner', 'moderator', 'member'));

                REVOKE ALL ON TABLE teams_channels FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE teams_channels TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_channel_members FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE teams_channel_members TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_messages FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_messages TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_message_mentions FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_message_mentions TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_channel_read_states FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE teams_channel_read_states TO crevafactos_runtime;
                REVOKE ALL ON TABLE teams_message_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE teams_message_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "teams_channels", "tc");
            ApplyTenantRls(migrationBuilder, "teams_channel_members", "tcm");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_messages", "tm");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_message_mentions", "tmm");
            ApplyTenantRls(migrationBuilder, "teams_channel_read_states", "tcrs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "teams_message_audit_events", "tmae");
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
                name: "teams_channel_members");

            migrationBuilder.DropTable(
                name: "teams_channel_read_states");

            migrationBuilder.DropTable(
                name: "teams_message_audit_events");

            migrationBuilder.DropTable(
                name: "teams_message_mentions");

            migrationBuilder.DropTable(
                name: "teams_messages");

            migrationBuilder.DropTable(
                name: "teams_channels");
        }
    }
}
