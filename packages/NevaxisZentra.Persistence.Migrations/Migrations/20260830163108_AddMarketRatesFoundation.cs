using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketRatesFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "market_rate_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_tenant_membership_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_rate_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_market_rate_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "market_rate_snapshots",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_attribution_url = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    provider_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    synchronized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    availability_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    configuration_message = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    last_fetch_failure_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    source_fingerprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_rate_snapshots", x => x.tenant_id);
                    table.ForeignKey(
                        name: "FK_market_rate_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "market_rate_quote_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    pair_label = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    change_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_rate_quote_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_market_rate_quote_lines_market_rate_snapshots_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "market_rate_snapshots",
                        principalColumn: "tenant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_market_rate_audit_tenant_occurred",
                table: "market_rate_audit_events",
                columns: new[] { "tenant_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_market_rate_quote_lines_tenant_pair",
                table: "market_rate_quote_lines",
                columns: new[] { "tenant_id", "pair_label" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-0000000000a1',
                     'dashboard.market-rates.read', 'dashboard.market-rates.read',
                     'Read dashboard market-rate context summary.',
                     'Dashboard piyasa kurlarını görüntüle', 'dashboard', 'read',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-08-30 19:20:00+00')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm
                    ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key = 'teams.read'
                JOIN permission_definitions read_perm
                    ON read_perm.key = 'dashboard.market-rates.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                REVOKE ALL ON TABLE market_rate_snapshots FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE market_rate_snapshots TO crevafactos_runtime;
                REVOKE ALL ON TABLE market_rate_quote_lines FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE market_rate_quote_lines TO crevafactos_runtime;
                REVOKE ALL ON TABLE market_rate_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE market_rate_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "market_rate_snapshots", "mrs");
            ApplyTenantRls(migrationBuilder, "market_rate_quote_lines", "mrql");
            ApplyTenantRlsAppendOnly(migrationBuilder, "market_rate_audit_events", "mrae");
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
                            USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
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
                name: "market_rate_audit_events");

            migrationBuilder.DropTable(
                name: "market_rate_quote_lines");

            migrationBuilder.DropTable(
                name: "market_rate_snapshots");
        }
    }
}
