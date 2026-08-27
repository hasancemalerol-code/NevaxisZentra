using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialQuotationRiskTrendComparisonIntelligence : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityRunSortColumns = ["tenant_id", "facility_id", "comparison_run_id", "sort_order"];
        private static readonly string[] TenantFacilityRunCustomerColumns = ["tenant_id", "facility_id", "comparison_run_id", "customer_reference"];
        private static readonly string[] TenantFacilityRunRankColumns = ["tenant_id", "facility_id", "comparison_run_id", "risk_rank"];
        private static readonly string[] TenantFacilityTypeOutcomeColumns = ["tenant_id", "facility_id", "comparison_type", "comparison_outcome"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantComparisonReferenceColumns = ["tenant_id", "comparison_reference"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "maximum_risk_incidence_delta",
                table: "commercial_quotation_risk_profiles",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_comparison_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comparison_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reference_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reference_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    current_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    comparison_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    candidate_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    comparable_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    risk_bearing_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    insufficient_data_count = table.Column<int>(type: "integer", nullable: false),
                    source_conflict_count = table.Column<int>(type: "integer", nullable: false),
                    price_risk_count = table.Column<int>(type: "integer", nullable: false),
                    expiry_risk_count = table.Column<int>(type: "integer", nullable: false),
                    payment_term_risk_count = table.Column<int>(type: "integer", nullable: false),
                    reference_candidate_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    reference_comparable_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    reference_excluded_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    reference_risk_bearing_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    current_candidate_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    current_comparable_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    current_excluded_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    current_risk_bearing_assessment_count = table.Column<int>(type: "integer", nullable: true),
                    reference_risk_incidence = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    current_risk_incidence = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    risk_incidence_delta = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    leading_driver_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_quotation_risk_comparison_runs", x => x.id);
                    table.CheckConstraint("ck_commquote2_comparison_runs_assessment_counts", "candidate_assessment_count = comparable_assessment_count + excluded_assessment_count");
                    table.CheckConstraint("ck_commquote2_comparison_runs_current_incidence", "current_risk_incidence IS NULL OR (current_risk_incidence >= 0 AND current_risk_incidence <= 1)");
                    table.CheckConstraint("ck_commquote2_comparison_runs_incidence_delta", "risk_incidence_delta IS NULL OR (risk_incidence_delta >= -1 AND risk_incidence_delta <= 1)");
                    table.CheckConstraint("ck_commquote2_comparison_runs_reference_incidence", "reference_risk_incidence IS NULL OR (reference_risk_incidence >= 0 AND reference_risk_incidence <= 1)");
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_runs_commercial_quotat~",
                        column: x => x.profile_id,
                        principalTable: "commercial_quotation_risk_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_runs_facilities_tenant~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_comparison_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    candidate_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    comparable_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    risk_bearing_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    risk_incidence_fraction = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    price_risk_count = table.Column<int>(type: "integer", nullable: false),
                    expiry_risk_count = table.Column<int>(type: "integer", nullable: false),
                    payment_term_risk_count = table.Column<int>(type: "integer", nullable: false),
                    position_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    risk_rank = table.Column<int>(type: "integer", nullable: true),
                    is_tied = table.Column<bool>(type: "boolean", nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_quotation_risk_comparison_results", x => x.id);
                    table.CheckConstraint("ck_commquote2_comparison_results_assessment_counts", "candidate_assessment_count = comparable_assessment_count + excluded_assessment_count");
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_results_commercial_quo~",
                        column: x => x.comparison_run_id,
                        principalTable: "commercial_quotation_risk_comparison_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_results_facilities_ten~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_results_tenants_tenant~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_comparison_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_result_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    reference_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    threshold_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    normalized_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    affected_assessment_count = table.Column<int>(type: "integer", nullable: false),
                    supporting_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_leading_driver = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_quotation_risk_comparison_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_drivers_commercial_quo~",
                        column: x => x.comparison_result_id,
                        principalTable: "commercial_quotation_risk_comparison_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_drivers_commercial_qu~1",
                        column: x => x.comparison_run_id,
                        principalTable: "commercial_quotation_risk_comparison_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_drivers_facilities_ten~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_comparison_drivers_tenants_tenant~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_comparison_drivers_comparison_res~",
                table: "commercial_quotation_risk_comparison_drivers",
                column: "comparison_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_comparison_drivers_comparison_run~",
                table: "commercial_quotation_risk_comparison_drivers",
                column: "comparison_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_commquote2_comparison_drivers_tenant_facility_run_sort",
                table: "commercial_quotation_risk_comparison_drivers",
                columns: TenantFacilityRunSortColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_comparison_results_comparison_run~",
                table: "commercial_quotation_risk_comparison_results",
                column: "comparison_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_commquote2_comparison_results_tenant_facility_run_customer",
                table: "commercial_quotation_risk_comparison_results",
                columns: TenantFacilityRunCustomerColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commquote2_comparison_results_tenant_facility_run_rank",
                table: "commercial_quotation_risk_comparison_results",
                columns: TenantFacilityRunRankColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_comparison_runs_profile_id",
                table: "commercial_quotation_risk_comparison_runs",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_commquote2_comparison_runs_tenant_facility_type_outcome",
                table: "commercial_quotation_risk_comparison_runs",
                columns: TenantFacilityTypeOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commquote2_comparison_runs_tenant_idempotency",
                table: "commercial_quotation_risk_comparison_runs",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commquote2_comparison_runs_tenant_reference",
                table: "commercial_quotation_risk_comparison_runs",
                columns: TenantComparisonReferenceColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_quotation_risk_profiles
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_risk_incidence_delta
                        CHECK (maximum_risk_incidence_delta >= 0 AND maximum_risk_incidence_delta <= 1);

                ALTER TABLE commercial_quotation_risk_comparison_runs
                    ADD CONSTRAINT ck_commquote2_comparison_runs_comparison_type
                        CHECK (comparison_type IN ('temporal-risk-trend', 'customer-risk-position')),
                    ADD CONSTRAINT ck_commquote2_comparison_runs_outcome
                        CHECK (comparison_outcome IN (
                            'within-risk-band', 'risk-increase', 'risk-decrease',
                            'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commquote2_comparison_runs_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_quotation_risk_comparison_results
                    ADD CONSTRAINT ck_commquote2_comparison_results_risk_rank
                        CHECK (risk_rank IS NULL OR risk_rank >= 1),
                    ADD CONSTRAINT ck_commquote2_comparison_results_position_state
                        CHECK (position_state IN (
                            'lowest-observed-risk-position', 'within-risk-band',
                            'higher-observed-risk-position', 'tied-risk-position',
                            'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commquote2_comparison_results_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commquote2_comparison_results_incidence_fraction
                        CHECK (risk_incidence_fraction IS NULL OR (risk_incidence_fraction >= 0 AND risk_incidence_fraction <= 1));

                ALTER TABLE commercial_quotation_risk_comparison_drivers
                    ADD CONSTRAINT ck_commquote2_comparison_drivers_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commquote2_comparison_drivers_affected_count
                        CHECK (affected_assessment_count >= 0),
                    ADD CONSTRAINT ck_commquote2_comparison_drivers_reason_code
                        CHECK (reason_code IN (
                            'risk-incidence-increase', 'risk-incidence-decrease',
                            'price-risk-concentration', 'expiry-risk-concentration',
                            'payment-term-risk-concentration', 'customer-risk-concentration',
                            'customer-risk-tie', 'insufficient-reference-window',
                            'insufficient-current-window', 'insufficient-customer-evidence',
                            'source-conflict', 'within-risk-band'));

                REVOKE ALL ON TABLE commercial_quotation_risk_comparison_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_comparison_runs TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_quotation_risk_comparison_results FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_comparison_results TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_quotation_risk_comparison_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_comparison_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_comparison_runs", "cquote2r");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_comparison_results", "cquote2res");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_comparison_drivers", "cquote2d");
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
                name: "commercial_quotation_risk_comparison_drivers");

            migrationBuilder.DropTable(
                name: "commercial_quotation_risk_comparison_results");

            migrationBuilder.DropTable(
                name: "commercial_quotation_risk_comparison_runs");

            migrationBuilder.DropColumn(
                name: "maximum_risk_incidence_delta",
                table: "commercial_quotation_risk_profiles");
        }
    }
}
