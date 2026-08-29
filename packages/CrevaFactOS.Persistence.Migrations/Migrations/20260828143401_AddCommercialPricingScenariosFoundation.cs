using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialPricingScenariosFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityScenarioAuditColumns = ["tenant_id", "facility_id", "scenario_id", "occurred_at_utc"];
        private static readonly string[] TenantFacilityGuardrailColumns = ["tenant_id", "facility_id", "guardrail_result"];
        private static readonly string[] TenantFacilityLifecycleColumns = ["tenant_id", "facility_id", "lifecycle_status"];
        private static readonly string[] TenantFacilityProfileColumns = ["tenant_id", "facility_id", "profile_id"];
        private static readonly string[] TenantFacilityScenarioSequenceColumns = ["tenant_id", "facility_id", "scenario_id", "evaluation_sequence"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_pricing_scenario_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    actor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_pricing_scenario_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenario_audit_events_facilities_tenant_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenario_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_pricing_scenario_evaluation_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_version_at_evaluation = table.Column<long>(type: "bigint", nullable: false),
                    evaluation_sequence = table.Column<int>(type: "integer", nullable: false),
                    guardrail_result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assessment_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cost_coverage_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    insufficient_data_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    baseline_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    proposed_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    total_included_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    expected_unit_contribution = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    expected_contribution_margin_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    baseline_contribution_margin_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    contribution_margin_delta = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    profile_minimum_contribution_margin_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    input_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    output_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_pricing_scenario_evaluation_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenario_evaluation_snapshots_facilities~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenario_evaluation_snapshots_tenants_te~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_pricing_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    guardrail_result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sales_evidence_observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    baseline_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    proposed_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    proposed_discount_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    validity_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    validity_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    commercial_rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    latest_evaluation_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    linked_ai_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    evaluation_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_pricing_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenarios_commercial_pricing_scenario_ev~",
                        column: x => x.latest_evaluation_snapshot_id,
                        principalTable: "commercial_pricing_scenario_evaluation_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenarios_commercial_profitability_profi~",
                        column: x => x.profile_id,
                        principalTable: "commercial_profitability_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenarios_facilities_tenant_id_facility_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_pricing_scenarios_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_pricing_scenario_audit_events_scenario_id",
                table: "commercial_pricing_scenario_audit_events",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit7_audit_tenant_facility_scenario_occurred",
                table: "commercial_pricing_scenario_audit_events",
                columns: TenantFacilityScenarioAuditColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_pricing_scenario_evaluation_snapshots_scenario_id",
                table: "commercial_pricing_scenario_evaluation_snapshots",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "ux_commprofit7_snapshots_scenario_sequence",
                table: "commercial_pricing_scenario_evaluation_snapshots",
                columns: TenantFacilityScenarioSequenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_pricing_scenarios_latest_evaluation_snapshot_id",
                table: "commercial_pricing_scenarios",
                column: "latest_evaluation_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_pricing_scenarios_profile_id",
                table: "commercial_pricing_scenarios",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit7_scenarios_tenant_facility_guardrail",
                table: "commercial_pricing_scenarios",
                columns: TenantFacilityGuardrailColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commprofit7_scenarios_tenant_facility_lifecycle",
                table: "commercial_pricing_scenarios",
                columns: TenantFacilityLifecycleColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commprofit7_scenarios_tenant_facility_profile",
                table: "commercial_pricing_scenarios",
                columns: TenantFacilityProfileColumns);

            migrationBuilder.AddForeignKey(
                name: "FK_commercial_pricing_scenario_audit_events_commercial_pricing~",
                table: "commercial_pricing_scenario_audit_events",
                column: "scenario_id",
                principalTable: "commercial_pricing_scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_commercial_pricing_scenario_evaluation_snapshots_commercial~",
                table: "commercial_pricing_scenario_evaluation_snapshots",
                column: "scenario_id",
                principalTable: "commercial_pricing_scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_pricing_scenarios
                    ADD CONSTRAINT ck_commprofit7_scenarios_lifecycle
                        CHECK (lifecycle_status IN (
                            'draft',
                            'evaluated',
                            'needs-review',
                            'eligible-for-approval',
                            'blocked',
                            'insufficient-data',
                            'submitted-for-approval',
                            'approved',
                            'rejected',
                            'withdrawn')),
                    ADD CONSTRAINT ck_commprofit7_scenarios_guardrail
                        CHECK (guardrail_result IS NULL OR guardrail_result IN (
                            'passes',
                            'needs-review',
                            'blocked',
                            'insufficient-data'));

                REVOKE ALL ON TABLE commercial_pricing_scenarios FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE commercial_pricing_scenarios TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_pricing_scenario_evaluation_snapshots FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_pricing_scenario_evaluation_snapshots TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_pricing_scenario_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_pricing_scenario_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "commercial_pricing_scenarios", "cprofit7s");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_pricing_scenario_evaluation_snapshots", "cprofit7es");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_pricing_scenario_audit_events", "cprofit7ae");
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
            migrationBuilder.DropForeignKey(
                name: "FK_commercial_pricing_scenario_evaluation_snapshots_commercial~",
                table: "commercial_pricing_scenario_evaluation_snapshots");

            migrationBuilder.DropTable(
                name: "commercial_pricing_scenario_audit_events");

            migrationBuilder.DropTable(
                name: "commercial_pricing_scenarios");

            migrationBuilder.DropTable(
                name: "commercial_pricing_scenario_evaluation_snapshots");
        }
    }
}
