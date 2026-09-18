using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskEmailDeliveryAdapterFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_task_email_channel_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    security_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_protected = table.Column<string>(type: "text", nullable: true),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    from_display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    reply_to_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    default_locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    template_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    readiness_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    readiness_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_validated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_email_channel_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_email_channel_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_email_channel_configurations_tenant",
                table: "ai_task_email_channel_configurations",
                column: "tenant_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000e01', 'ai-task.delivery.email.view', 'ai-task.delivery.email.view',
                     'View Email delivery status and provider metadata.',
                     'E-posta teslimatlarını görüntüle', 'ai-task', 'delivery.email.view',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-08 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000e02', 'ai-task.delivery.email.send', 'ai-task.delivery.email.send',
                     'Send AI task notifications via Email.',
                     'E-posta ile gönder', 'ai-task', 'delivery.email.send',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-08 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000e03', 'ai-task.delivery.email.configure', 'ai-task.delivery.email.configure',
                     'Configure Email channel settings.',
                     'E-posta kanalını yapılandır', 'ai-task', 'delivery.email.configure',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-08 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "ai_task_email_channel_configurations", "atecc");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE ai_task_email_channel_configurations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_email_channel_configurations TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ai_task_email_channel_configurations");
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
    }
}
