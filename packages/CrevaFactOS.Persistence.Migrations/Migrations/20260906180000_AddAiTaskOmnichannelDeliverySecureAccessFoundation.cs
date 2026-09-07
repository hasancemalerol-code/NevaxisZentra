using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTaskOmnichannelDeliverySecureAccessFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantTaskColumns = ["tenant_id", "ai_task_id"];
        private static readonly string[] TenantStatusNextColumns = ["tenant_id", "status", "next_attempt_at_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] DeliveryAttemptColumns = ["delivery_id", "attempt_number"];
        private static readonly string[] TenantTokenHashColumns = ["tenant_id", "token_hash"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_task_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    masked_recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_version = table.Column<int>(type: "integer", nullable: false),
                    delivery_projection_json = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error_category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    secure_access_grant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_deliveries_ai_tasks_ai_task_id",
                        column: x => x.ai_task_id,
                        principalTable: "ai_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_deliveries_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_deliveries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_delivery_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    adapter_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_status_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error_category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    sanitized_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    retry_scheduled_for_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_delivery_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_delivery_attempts_ai_task_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalTable: "ai_task_deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_delivery_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_task_secure_access_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_accessed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_task_secure_access_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_task_secure_access_grants_ai_tasks_ai_task_id",
                        column: x => x.ai_task_id,
                        principalTable: "ai_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_secure_access_grants_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_task_secure_access_grants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_deliveries_tenant_task",
                table: "ai_task_deliveries",
                columns: TenantTaskColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_deliveries_tenant_status_next",
                table: "ai_task_deliveries",
                columns: TenantStatusNextColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_deliveries_lease_expiry",
                table: "ai_task_deliveries",
                column: "lease_expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_deliveries_tenant_idempotency",
                table: "ai_task_deliveries",
                columns: TenantIdempotencyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_delivery_attempts_delivery_number",
                table: "ai_task_delivery_attempts",
                columns: DeliveryAttemptColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_secure_access_grants_tenant_task",
                table: "ai_task_secure_access_grants",
                columns: TenantTaskColumns);

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_secure_access_grants_expiry",
                table: "ai_task_secure_access_grants",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ux_ai_task_secure_access_grants_tenant_token_hash",
                table: "ai_task_secure_access_grants",
                columns: TenantTokenHashColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000e0', 'ai-task.delivery.read', 'ai-task.delivery.read',
                     'Read AI task delivery history and channel readiness.',
                     'AI görev teslimatlarını görüntüle', 'ai-task', 'delivery.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000e1', 'ai-task.delivery.manage', 'ai-task.delivery.manage',
                     'Enqueue, retry, and cancel AI task deliveries.',
                     'AI görev teslimatlarını yönet', 'ai-task', 'delivery.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000e2', 'ai-task.secure-access.read', 'ai-task.secure-access.read',
                     'Read AI task secure access grants.',
                     'Güvenli bağlantıları görüntüle', 'ai-task', 'secure-access.read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00'),
                    ('10000000-0000-4000-8000-0000000000e3', 'ai-task.secure-access.manage', 'ai-task.secure-access.manage',
                     'Issue, revoke, and rotate AI task secure access grants.',
                     'Güvenli bağlantıları yönet', 'ai-task', 'secure-access.manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-06 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE ai_task_deliveries
                    ADD CONSTRAINT ck_ai_task_deliveries_status
                        CHECK (status IN ('queued','processing','sent','failed-retryable','failed-permanent','dead-letter','cancelled')),
                    ADD CONSTRAINT ck_ai_task_deliveries_channel
                        CHECK (channel IN ('crevafactos-teams','email','whatsapp','microsoft-teams','sms','development-loopback')),
                    ADD CONSTRAINT ck_ai_task_deliveries_recipient_kind
                        CHECK (recipient_kind IN ('user','external','secure-link')),
                    ADD CONSTRAINT ck_ai_task_deliveries_attempts
                        CHECK (attempt_count >= 0 AND max_attempts >= 1);

                ALTER TABLE ai_task_delivery_attempts
                    ADD CONSTRAINT ck_ai_task_delivery_attempts_outcome
                        CHECK (outcome IN ('success','transient-failure','permanent-failure','configuration-failure'));

                ALTER TABLE ai_task_secure_access_grants
                    ADD CONSTRAINT ck_ai_task_secure_access_grants_scope
                        CHECK (scope IN ('read-only'));

                REVOKE ALL ON TABLE ai_task_deliveries FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_deliveries TO crevafactos_runtime;
                REVOKE ALL ON TABLE ai_task_delivery_attempts FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE ai_task_delivery_attempts TO crevafactos_runtime;
                REVOKE ALL ON TABLE ai_task_secure_access_grants FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE ai_task_secure_access_grants TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "ai_task_deliveries", "atd");
            ApplyTenantRlsAppendOnly(migrationBuilder, "ai_task_delivery_attempts", "atda");
            ApplyTenantRls(migrationBuilder, "ai_task_secure_access_grants", "atsag");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION discover_ai_task_delivery_work_tenants()
                RETURNS TABLE(tenant_id uuid)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog, public
                AS $function$
                    SELECT DISTINCT d.tenant_id
                    FROM public.ai_task_deliveries AS d
                    WHERE d.status = 'queued'
                       OR (d.status = 'failed-retryable' AND d.next_attempt_at_utc <= clock_timestamp())
                       OR (d.status = 'processing'
                           AND d.lease_expires_at_utc IS NOT NULL
                           AND d.lease_expires_at_utc <= clock_timestamp());
                $function$;

                CREATE OR REPLACE FUNCTION resolve_ai_task_delivery_worker_context(tenant_value uuid)
                RETURNS TABLE(tenant_id uuid, tenant_key text, membership_id uuid)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog, public
                AS $function$
                    SELECT tenant.id, tenant.tenant_key, membership.id
                    FROM public.tenants AS tenant
                    INNER JOIN public.tenant_memberships AS membership
                        ON membership.tenant_id = tenant.id
                       AND membership.status = 'Active'
                    WHERE tenant.id = tenant_value
                      AND tenant.status = 'Active'
                    ORDER BY membership.created_at_utc, membership.id
                    LIMIT 1;
                $function$;

                REVOKE ALL ON FUNCTION discover_ai_task_delivery_work_tenants() FROM PUBLIC;
                REVOKE ALL ON FUNCTION resolve_ai_task_delivery_worker_context(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION discover_ai_task_delivery_work_tenants() TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION resolve_ai_task_delivery_worker_context(uuid) TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ai_task_delivery_attempts");
            migrationBuilder.DropTable(name: "ai_task_secure_access_grants");
            migrationBuilder.DropTable(name: "ai_task_deliveries");
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
    }
}
