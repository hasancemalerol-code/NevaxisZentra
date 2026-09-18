using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialProfitabilityAlertsFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityAlertAuditColumns = ["tenant_id", "facility_id", "alert_id", "occurred_at_utc"];
        private static readonly string[] TenantFacilityCategoryColumns = ["tenant_id", "facility_id", "alert_category"];
        private static readonly string[] TenantFacilityLifecycleColumns = ["tenant_id", "facility_id", "lifecycle_status"];
        private static readonly string[] TenantFacilitySeverityColumns = ["tenant_id", "facility_id", "severity"];
        private static readonly string[] TenantFacilityComparisonCategoryColumns = ["tenant_id", "facility_id", "source_comparison_id", "alert_category"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_profitability_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    alert_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_comparison_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_comparison_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evidence_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    comparison_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    materialized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alerts_commercial_profitability_co~",
                        column: x => x.source_comparison_id,
                        principalTable: "commercial_profitability_comparison_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alerts_facilities_tenant_id_facili~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alerts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_profitability_alert_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_alert_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alert_audit_events_commercial_prof~",
                        column: x => x.alert_id,
                        principalTable: "commercial_profitability_alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alert_audit_events_facilities_tena~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_alert_audit_events_tenants_tenant_~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_alert_audit_events_alert_id",
                table: "commercial_profitability_alert_audit_events",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit6_alert_audit_tenant_facility_alert_occurred",
                table: "commercial_profitability_alert_audit_events",
                columns: TenantFacilityAlertAuditColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_alerts_source_comparison_id",
                table: "commercial_profitability_alerts",
                column: "source_comparison_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit6_alerts_tenant_facility_category",
                table: "commercial_profitability_alerts",
                columns: TenantFacilityCategoryColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commprofit6_alerts_tenant_facility_lifecycle",
                table: "commercial_profitability_alerts",
                columns: TenantFacilityLifecycleColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commprofit6_alerts_tenant_facility_severity",
                table: "commercial_profitability_alerts",
                columns: TenantFacilitySeverityColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commprofit6_alerts_active_comparison_category",
                table: "commercial_profitability_alerts",
                columns: TenantFacilityComparisonCategoryColumns,
                unique: true,
                filter: "lifecycle_status = 'active'");

            migrationBuilder.CreateIndex(
                name: "ux_commprofit6_alerts_tenant_idempotency",
                table: "commercial_profitability_alerts",
                columns: TenantIdempotencyColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_profitability_alerts
                    ADD CONSTRAINT ck_commprofit6_alerts_category
                        CHECK (alert_category IN (
                            'product-contribution-deterioration',
                            'customer-contribution-deterioration',
                            'material-contribution-deterioration',
                            'cost-mix-deterioration',
                            'data-review')),
                    ADD CONSTRAINT ck_commprofit6_alerts_severity
                        CHECK (severity IN ('low', 'medium', 'high', 'critical')),
                    ADD CONSTRAINT ck_commprofit6_alerts_lifecycle
                        CHECK (lifecycle_status IN ('active', 'acknowledged', 'task-created', 'dismissed', 'resolved'));

                REVOKE ALL ON TABLE commercial_profitability_alerts FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE commercial_profitability_alerts TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_profitability_alert_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_profitability_alert_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "commercial_profitability_alerts", "cprofit6a");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_profitability_alert_audit_events", "cprofit6ae");
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
                name: "commercial_profitability_alert_audit_events");

            migrationBuilder.DropTable(
                name: "commercial_profitability_alerts");
        }
    }
}
