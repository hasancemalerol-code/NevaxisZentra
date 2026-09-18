using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialSalesPriceComparisonIntelligence : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityRunSortColumns = ["tenant_id", "facility_id", "comparison_run_id", "sort_order"];
        private static readonly string[] TenantFacilityRunCustomerColumns = ["tenant_id", "facility_id", "comparison_run_id", "customer_reference"];
        private static readonly string[] TenantFacilityMaterialWindowColumns = ["tenant_id", "facility_id", "material_id", "current_window_start_utc", "current_window_end_utc"];
        private static readonly string[] TenantFacilityTypeOutcomeColumns = ["tenant_id", "facility_id", "comparison_type", "comparison_outcome"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantComparisonReferenceColumns = ["tenant_id", "comparison_reference"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "customer_spread_threshold",
                table: "commercial_sales_price_profiles",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "commercial_sales_price_comparison_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comparison_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_evidence_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reference_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reference_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    normalized_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price_basis_gross_net = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    comparison_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    current_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    reference_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    signed_variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    customer_spread_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    candidate_observation_count = table.Column<int>(type: "integer", nullable: false),
                    comparable_observation_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_observation_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_commercial_sales_price_comparison_runs", x => x.id);
                    table.CheckConstraint("ck_commercial_sales_price_comparison_runs_signed_variance", "signed_variance_ratio IS NULL OR signed_variance_ratio > -1");
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_runs_commercial_sales_pri~",
                        column: x => x.profile_id,
                        principalTable: "commercial_sales_price_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_runs_facilities_tenant_id~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_sales_price_comparison_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comparable_observation_count = table.Column<int>(type: "integer", nullable: false),
                    customer_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    lowest_comparable_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    relative_price_position_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    price_rank = table.Column<int>(type: "integer", nullable: false),
                    is_tied = table.Column<bool>(type: "boolean", nullable: false),
                    position_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_sales_price_comparison_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_results_commercial_sales_~",
                        column: x => x.comparison_run_id,
                        principalTable: "commercial_sales_price_comparison_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_results_facilities_tenant~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_results_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_sales_price_comparison_drivers",
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
                    affected_observation_count = table.Column<int>(type: "integer", nullable: false),
                    supporting_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_leading_driver = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_sales_price_comparison_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_drivers_commercial_sales_~",
                        column: x => x.comparison_result_id,
                        principalTable: "commercial_sales_price_comparison_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_drivers_commercial_sales~1",
                        column: x => x.comparison_run_id,
                        principalTable: "commercial_sales_price_comparison_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_drivers_facilities_tenant~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_sales_price_comparison_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_sales_price_comparison_drivers_comparison_result~",
                table: "commercial_sales_price_comparison_drivers",
                column: "comparison_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_sales_price_comparison_drivers_comparison_run_id",
                table: "commercial_sales_price_comparison_drivers",
                column: "comparison_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_commsale2_driver_tenant_facility_run",
                table: "commercial_sales_price_comparison_drivers",
                columns: TenantFacilityRunSortColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_sales_price_comparison_results_comparison_run_id",
                table: "commercial_sales_price_comparison_results",
                column: "comparison_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_commsale2_result_tenant_facility_run_customer",
                table: "commercial_sales_price_comparison_results",
                columns: TenantFacilityRunCustomerColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_sales_price_comparison_runs_profile_id",
                table: "commercial_sales_price_comparison_runs",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_commsale2_comparison_tenant_facility_material_window",
                table: "commercial_sales_price_comparison_runs",
                columns: TenantFacilityMaterialWindowColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commsale2_comparison_tenant_facility_type_outcome",
                table: "commercial_sales_price_comparison_runs",
                columns: TenantFacilityTypeOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_sales_price_comparison_runs_tenant_idempotency",
                table: "commercial_sales_price_comparison_runs",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commercial_sales_price_comparison_runs_tenant_reference",
                table: "commercial_sales_price_comparison_runs",
                columns: TenantComparisonReferenceColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_sales_price_comparison_runs
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_current_window
                        CHECK (current_window_end_utc > current_window_start_utc),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_reference_window
                        CHECK (
                            (reference_window_start_utc IS NULL AND reference_window_end_utc IS NULL)
                            OR (reference_window_start_utc IS NOT NULL
                                AND reference_window_end_utc IS NOT NULL
                                AND reference_window_end_utc > reference_window_start_utc)),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_counts
                        CHECK (
                            candidate_observation_count >= 0
                            AND comparable_observation_count >= 0
                            AND excluded_observation_count >= 0
                            AND candidate_observation_count = comparable_observation_count + excluded_observation_count),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_customer_spread
                        CHECK (customer_spread_ratio IS NULL OR customer_spread_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_current_median
                        CHECK (current_median_unit_price IS NULL OR current_median_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_reference_median
                        CHECK (reference_median_unit_price IS NULL OR reference_median_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_comparison_type
                        CHECK (comparison_type IN ('temporal-sales-price-deviation', 'customer-price-position')),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_outcome
                        CHECK (comparison_outcome IN (
                            'within-reference-range', 'attention', 'positive-price-variance', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_runs_evidence_status
                        CHECK (evidence_status IN (
                            'measured-transaction', 'calculated', 'simulated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_sales_price_comparison_results
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_counts
                        CHECK (comparable_observation_count >= 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_rank
                        CHECK (price_rank >= 1),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_customer_median
                        CHECK (customer_median_unit_price IS NULL OR customer_median_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_lowest_median
                        CHECK (lowest_comparable_median_unit_price IS NULL OR lowest_comparable_median_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_position_ratio
                        CHECK (relative_price_position_ratio IS NULL OR relative_price_position_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_position_state
                        CHECK (position_state IN (
                            'lowest-comparable-price', 'within-spread-threshold', 'higher-comparable-price',
                            'tied-comparable-price', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_sales_price_comparison_drivers
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_drivers_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_drivers_affected_count
                        CHECK (affected_observation_count >= 0);

                REVOKE ALL ON TABLE commercial_sales_price_comparison_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_sales_price_comparison_runs TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_sales_price_comparison_results FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_sales_price_comparison_results TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_sales_price_comparison_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_sales_price_comparison_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_sales_price_comparison_runs", "csalecr");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_sales_price_comparison_results", "csalecres");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_sales_price_comparison_drivers", "csalecd");
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
                name: "commercial_sales_price_comparison_drivers");

            migrationBuilder.DropTable(
                name: "commercial_sales_price_comparison_results");

            migrationBuilder.DropTable(
                name: "commercial_sales_price_comparison_runs");

            migrationBuilder.DropColumn(
                name: "customer_spread_threshold",
                table: "commercial_sales_price_profiles");
        }
    }
}
