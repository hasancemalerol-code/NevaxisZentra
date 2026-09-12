using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskWhatsAppBusinessAdapterFoundation : Migration
    {
        private static readonly string[] ProviderEventTenantMessageColumns = ["tenant_id", "provider_message_id"];
        private static readonly string[] ProviderEventTenantHashColumns = ["tenant_id", "event_hash"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "ai_task_deliveries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_status",
                table: "ai_task_deliveries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "provider_status_at_utc",
                table: "ai_task_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_address_protected",
                table: "ai_task_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consent_status",
                table: "ai_task_deliveries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_task_whatsapp_channel_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    business_account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sender_display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    default_locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    template_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    template_language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    access_token_protected = table.Column<string>(type: "text", nullable: true),
                    webhook_verify_token_protected = table.Column<string>(type: "text", nullable: true),
                    readiness_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    readiness_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_validated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_whatsapp_channel_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_whatsapp_channel_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_delivery_provider_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    event_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sanitized_payload_json = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_delivery_provider_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_delivery_provider_events_ai_task_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalTable: "ai_task_deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_delivery_provider_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_delivery_provider_events_tenant_message",
                table: "ai_task_delivery_provider_events",
                columns: ProviderEventTenantMessageColumns);

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_delivery_provider_events_tenant_hash",
                table: "ai_task_delivery_provider_events",
                columns: ProviderEventTenantHashColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_whatsapp_channel_configurations_tenant",
                table: "ai_task_whatsapp_channel_configurations",
                column: "tenant_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000d01', 'ai-task.delivery.whatsapp.view', 'ai-task.delivery.whatsapp.view',
                     'View WhatsApp delivery status and provider metadata.',
                     'WhatsApp teslimatlarını görüntüle', 'ai-task', 'delivery.whatsapp.view',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-07 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000d02', 'ai-task.delivery.whatsapp.send', 'ai-task.delivery.whatsapp.send',
                     'Send AI task notifications via WhatsApp Business.',
                     'WhatsApp ile gönder', 'ai-task', 'delivery.whatsapp.send',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-07 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000d03', 'ai-task.delivery.whatsapp.configure', 'ai-task.delivery.whatsapp.configure',
                     'Configure WhatsApp Business channel settings.',
                     'WhatsApp Business yapılandır', 'ai-task', 'delivery.whatsapp.configure',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-07 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "ai_task_whatsapp_channel_configurations", "atwcc");
            ApplyTenantRlsAppendOnly(migrationBuilder, "ai_task_delivery_provider_events", "atdpe");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE ai_task_whatsapp_channel_configurations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_whatsapp_channel_configurations TO crevafactos_runtime;
                REVOKE ALL ON TABLE ai_task_delivery_provider_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE ai_task_delivery_provider_events TO crevafactos_runtime;

                CREATE OR REPLACE FUNCTION resolve_ai_task_delivery_by_provider_message(p_provider_message_id text)
                RETURNS TABLE (
                    delivery_id uuid,
                    tenant_id uuid,
                    provider_status varchar(30))
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = public
                AS $func$
                    SELECT d.id, d.tenant_id, d.provider_status
                    FROM ai_task_deliveries d
                    WHERE d.provider_message_id = p_provider_message_id
                    ORDER BY d.requested_at_utc DESC
                    LIMIT 1;
                $func$;

                REVOKE ALL ON FUNCTION resolve_ai_task_delivery_by_provider_message(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_ai_task_delivery_by_provider_message(text) TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ai_task_delivery_provider_events");
            migrationBuilder.DropTable(name: "ai_task_whatsapp_channel_configurations");
            migrationBuilder.DropColumn(name: "provider", table: "ai_task_deliveries");
            migrationBuilder.DropColumn(name: "provider_status", table: "ai_task_deliveries");
            migrationBuilder.DropColumn(name: "provider_status_at_utc", table: "ai_task_deliveries");
            migrationBuilder.DropColumn(name: "recipient_address_protected", table: "ai_task_deliveries");
            migrationBuilder.DropColumn(name: "consent_status", table: "ai_task_deliveries");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
