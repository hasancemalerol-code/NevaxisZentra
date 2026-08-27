using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialProfitabilityIntelligenceFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityAssessmentSortColumns = ["tenant_id", "facility_id", "assessment_id", "sort_order"];
        private static readonly string[] TenantFacilityMaterialOutcomeColumns = ["tenant_id", "facility_id", "material_id", "outcome"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantAssessmentReferenceColumns = ["tenant_id", "assessment_reference"];
        private static readonly string[] TenantSourceFingerprintColumns = ["tenant_id", "source_fingerprint"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantProfileCodeColumns = ["tenant_id", "profile_code"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];
        private static readonly string[] TenantFacilityEvidenceSetKindColumns = ["tenant_id", "facility_id", "evidence_set_reference", "observation_kind"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_profitability_evidence_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_set_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quotation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comparable_net_sales_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    component_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    freight_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cost_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    allocation_basis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_cancelled_or_returned = table.Column<bool>(type: "boolean", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ingest_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ingest_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_evidence_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_evidence_observations_facilities_t~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_evidence_observations_tenants_tena~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_profitability_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum_contribution_margin_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    maximum_evidence_age_days = table.Column<int>(type: "integer", nullable: false),
                    required_cost_component_kinds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    required_currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    required_unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    required_gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    allowed_evidence_statuses = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    create_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    create_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_profiles_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_profitability_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sales_evidence_observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quotation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    evidence_set_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    comparable_net_sales_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    total_included_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_contribution = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    contribution_margin_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    markup_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    candidate_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    included_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    required_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    available_required_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    missing_required_cost_component_count = table.Column<int>(type: "integer", nullable: false),
                    missing_required_component_kinds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cost_coverage_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    freight_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_commercial_profitability_assessments", x => x.id);
                    table.CheckConstraint("ck_commercial_profitability_assessments_margin_ratio", "contribution_margin_ratio IS NULL OR contribution_margin_ratio < 1");
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessments_commercial_profitabili~",
                        column: x => x.profile_id,
                        principalTable: "commercial_profitability_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessments_commercial_profitabil~1",
                        column: x => x.sales_evidence_observation_id,
                        principalTable: "commercial_profitability_evidence_observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessments_facilities_tenant_id_f~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_profitability_assessment_cost_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_evidence_observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    component_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cost_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    allocation_basis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    evidence_timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    inclusion_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    exclusion_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_assessment_cost_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_cost_components_commerc~",
                        column: x => x.assessment_id,
                        principalTable: "commercial_profitability_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_cost_components_facilit~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_cost_components_tenants~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_profitability_assessment_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    threshold_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    normalized_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    affected_component_count = table.Column<int>(type: "integer", nullable: false),
                    supporting_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_leading_driver = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_profitability_assessment_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_drivers_commercial_prof~",
                        column: x => x.assessment_id,
                        principalTable: "commercial_profitability_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_drivers_facilities_tena~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_profitability_assessment_drivers_tenants_tenant_~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_assessment_cost_components_assessm~",
                table: "commercial_profitability_assessment_cost_components",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit1_components_tenant_facility_assessment_sort",
                table: "commercial_profitability_assessment_cost_components",
                columns: TenantFacilityAssessmentSortColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_assessment_drivers_assessment_id",
                table: "commercial_profitability_assessment_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit1_drivers_tenant_facility_assessment_sort",
                table: "commercial_profitability_assessment_drivers",
                columns: TenantFacilityAssessmentSortColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_assessments_profile_id",
                table: "commercial_profitability_assessments",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_profitability_assessments_sales_evidence_observa~",
                table: "commercial_profitability_assessments",
                column: "sales_evidence_observation_id");

            migrationBuilder.CreateIndex(
                name: "ix_commprofit1_assessments_tenant_facility_material_outcome",
                table: "commercial_profitability_assessments",
                columns: TenantFacilityMaterialOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_profitability_assessments_tenant_idempotency",
                table: "commercial_profitability_assessments",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commercial_profitability_assessments_tenant_reference",
                table: "commercial_profitability_assessments",
                columns: TenantAssessmentReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commprofit1_evidence_tenant_facility_set_kind",
                table: "commercial_profitability_evidence_observations",
                columns: TenantFacilityEvidenceSetKindColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commprofit1_evidence_tenant_source_fingerprint",
                table: "commercial_profitability_evidence_observations",
                columns: TenantSourceFingerprintColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_profitability_profiles_tenant_facility_status",
                table: "commercial_profitability_profiles",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_profitability_profiles_tenant_code",
                table: "commercial_profitability_profiles",
                columns: TenantProfileCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_profitability_profiles_tenant_create_idempotency",
                table: "commercial_profitability_profiles",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_profitability_profiles
                    ADD CONSTRAINT ck_commercial_profitability_profiles_status
                        CHECK (status IN ('draft', 'approved', 'inactive')),
                    ADD CONSTRAINT ck_commercial_profitability_profiles_minimum_margin
                        CHECK (minimum_contribution_margin_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_profitability_profiles_evidence_age
                        CHECK (maximum_evidence_age_days >= 1);

                ALTER TABLE commercial_profitability_evidence_observations
                    ADD CONSTRAINT ck_commercial_profitability_evidence_observation_kind
                        CHECK (observation_kind IN ('sales', 'cost-component')),
                    ADD CONSTRAINT ck_commercial_profitability_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_profitability_assessments
                    ADD CONSTRAINT ck_commercial_profitability_assessments_sales_price
                        CHECK (comparable_net_sales_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_total_cost
                        CHECK (total_included_unit_cost >= 0),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_markup_cost
                        CHECK (markup_ratio IS NULL OR total_included_unit_cost > 0),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_counts
                        CHECK (
                            candidate_cost_component_count >= 0
                            AND included_cost_component_count >= 0
                            AND excluded_cost_component_count >= 0
                            AND required_cost_component_count >= 0
                            AND available_required_cost_component_count >= 0
                            AND missing_required_cost_component_count >= 0
                            AND candidate_cost_component_count = included_cost_component_count + excluded_cost_component_count
                            AND required_cost_component_count = available_required_cost_component_count + missing_required_cost_component_count),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_coverage_status
                        CHECK (cost_coverage_status IN (
                            'complete-required-cost-coverage', 'partial-cost-coverage',
                            'insufficient-cost-evidence', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_outcome
                        CHECK (outcome IN (
                            'within-threshold', 'below-threshold', 'negative-contribution',
                            'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_profitability_assessments_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_profitability_assessment_cost_components
                    ADD CONSTRAINT ck_commercial_profitability_assessment_cost_components_unit_cost
                        CHECK (unit_cost > 0),
                    ADD CONSTRAINT ck_commercial_profitability_assessment_cost_components_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commercial_profitability_assessment_cost_components_kind
                        CHECK (component_kind IN (
                            'direct-material', 'direct-labor', 'machine', 'energy', 'tooling', 'quality',
                            'scrap-rework', 'packaging', 'logistics', 'allocated-overhead', 'other')),
                    ADD CONSTRAINT ck_commercial_profitability_assessment_cost_components_inclusion
                        CHECK (inclusion_status IN ('included', 'excluded'));

                ALTER TABLE commercial_profitability_assessment_drivers
                    ADD CONSTRAINT ck_commercial_profitability_assessment_drivers_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commercial_profitability_assessment_drivers_affected_count
                        CHECK (affected_component_count >= 0),
                    ADD CONSTRAINT ck_commercial_profitability_assessment_drivers_reason_code
                        CHECK (reason_code IN (
                            'negative-unit-contribution', 'contribution-margin-below-threshold',
                            'incomplete-cost-coverage', 'missing-direct-material-cost',
                            'missing-direct-labor-cost', 'missing-machine-cost', 'missing-energy-cost',
                            'missing-other-required-cost', 'stale-sales-evidence', 'stale-cost-evidence',
                            'source-conflict', 'incompatible-currency', 'incompatible-unit',
                            'incompatible-price-basis', 'invalid-allocation-basis', 'within-profile'));

                REVOKE ALL ON TABLE commercial_profitability_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE commercial_profitability_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_profitability_evidence_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_profitability_evidence_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_profitability_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_profitability_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_profitability_assessment_cost_components FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_profitability_assessment_cost_components TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_profitability_assessment_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_profitability_assessment_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "commercial_profitability_profiles", "cprofitp");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_profitability_evidence_observations", "cprofite");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_profitability_assessments", "cprofita");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_profitability_assessment_cost_components", "cprofitc");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_profitability_assessment_drivers", "cprofitd");
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
                name: "commercial_profitability_assessment_cost_components");

            migrationBuilder.DropTable(
                name: "commercial_profitability_assessment_drivers");

            migrationBuilder.DropTable(
                name: "commercial_profitability_assessments");

            migrationBuilder.DropTable(
                name: "commercial_profitability_profiles");

            migrationBuilder.DropTable(
                name: "commercial_profitability_evidence_observations");
        }
    }
}
