using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasTenantSubscriptionEntitlementEnforcement : Migration
    {
        private static readonly string[] AuditTenantOccurredColumns = ["tenant_id", "occurred_at_utc"];
        private static readonly string[] IdempotencyKeyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TransitionHistoryLookupColumns = ["tenant_id", "subscription_id", "occurred_at_utc"];
        private static readonly string[] TransitionHistoryIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantCurrentColumns = ["tenant_id", "is_current"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_subscription_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_subscription_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_subscription_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_subscription_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_subscription_idempotency_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_subscription_idempotency_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_tenant_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    billing_cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_period_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    trial_ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grace_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_tenant_subscriptions", x => x.id);
                    table.CheckConstraint("ck_saas_tenant_subscriptions_status", "subscription_status IN ('trialing', 'active', 'grace-period', 'suspended', 'cancelled', 'expired')");
                    table.ForeignKey(
                        name: "FK_saas_tenant_subscriptions_saas_commercial_plan_versions_com~",
                        column: x => x.commercial_plan_version_id,
                        principalTable: "saas_commercial_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_tenant_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_tenant_subscription_transition_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    transition_reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    evidence_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_tenant_subscription_transition_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_tenant_subscription_transition_history_saas_tenant_sub~",
                        column: x => x.subscription_id,
                        principalTable: "saas_tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_tenant_subscription_transition_history_tenants_tenant_~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saas_subscription_audit_events_tenant_occurred",
                table: "saas_subscription_audit_events",
                columns: AuditTenantOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_subscription_idempotency_key",
                table: "saas_subscription_idempotency_records",
                columns: IdempotencyKeyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_tenant_subscription_transition_history_subscription_id",
                table: "saas_tenant_subscription_transition_history",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_tenant_subscription_transition_history_lookup",
                table: "saas_tenant_subscription_transition_history",
                columns: TransitionHistoryLookupColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_tenant_subscription_transition_history_idempotency",
                table: "saas_tenant_subscription_transition_history",
                columns: TransitionHistoryIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_saas_tenant_subscriptions_commercial_plan_version_id",
                table: "saas_tenant_subscriptions",
                column: "commercial_plan_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_saas_tenant_subscriptions_tenant_current",
                table: "saas_tenant_subscriptions",
                columns: TenantCurrentColumns);

            migrationBuilder.CreateIndex(
                name: "ux_saas_tenant_subscriptions_tenant_current",
                table: "saas_tenant_subscriptions",
                column: "tenant_id",
                unique: true,
                filter: "is_current = TRUE");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000072',
                     'saas-subscription.read', 'saas-subscription.read',
                     'Read tenant subscription status, entitlements, and usage limits.',
                     'Abonelik durumunu görüntüle', 'saas-subscription', 'view',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-14 22:03:44+00'),
                    ('10000000-0000-4000-8000-000000000073',
                     'saas-subscription.manage', 'saas-subscription.manage',
                     'Manage tenant subscription lifecycle in non-production development contexts.',
                     'Abonelik yaşam döngüsünü yönet', 'saas-subscription', 'manage-settings',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-14 22:03:44+00')
                ON CONFLICT (id) DO NOTHING;

                REVOKE ALL ON TABLE saas_tenant_subscriptions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_tenant_subscriptions TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_tenant_subscription_transition_history FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_tenant_subscription_transition_history TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_subscription_idempotency_records FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE saas_subscription_idempotency_records TO crevafactos_runtime;
                REVOKE ALL ON TABLE saas_subscription_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE saas_subscription_audit_events TO crevafactos_runtime;

                ALTER TABLE saas_tenant_subscriptions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_tenant_subscriptions FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_tenant_subscriptions_select_current
                    ON saas_tenant_subscriptions FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_tenant_subscriptions_insert_current
                    ON saas_tenant_subscriptions FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_tenant_subscriptions_update_current
                    ON saas_tenant_subscriptions FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_tenant_subscription_transition_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_tenant_subscription_transition_history FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_tenant_subscription_transition_history_select_current
                    ON saas_tenant_subscription_transition_history FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_tenant_subscription_transition_history_insert_current
                    ON saas_tenant_subscription_transition_history FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_subscription_idempotency_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_subscription_idempotency_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_subscription_idempotency_records_select_current
                    ON saas_subscription_idempotency_records FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_subscription_idempotency_records_insert_current
                    ON saas_subscription_idempotency_records FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_subscription_idempotency_records_update_current
                    ON saas_subscription_idempotency_records FOR UPDATE TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                ALTER TABLE saas_subscription_audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE saas_subscription_audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY saas_subscription_audit_events_select_current
                    ON saas_subscription_audit_events FOR SELECT TO crevafactos_runtime
                    USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY saas_subscription_audit_events_insert_current
                    ON saas_subscription_audit_events FOR INSERT TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saas_subscription_audit_events");

            migrationBuilder.DropTable(
                name: "saas_subscription_idempotency_records");

            migrationBuilder.DropTable(
                name: "saas_tenant_subscription_transition_history");

            migrationBuilder.DropTable(
                name: "saas_tenant_subscriptions");
        }
    }
}
