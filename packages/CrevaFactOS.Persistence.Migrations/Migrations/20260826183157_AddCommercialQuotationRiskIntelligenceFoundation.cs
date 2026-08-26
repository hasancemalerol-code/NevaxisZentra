using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialQuotationRiskIntelligenceFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityAssessmentSortColumns = ["tenant_id", "facility_id", "assessment_id", "sort_order"];
        private static readonly string[] TenantFacilityMaterialOutcomeColumns = ["tenant_id", "facility_id", "material_id", "outcome"];
        private static readonly string[] TenantFacilityWindowColumns = ["tenant_id", "facility_id", "reference_window_start_utc", "reference_window_end_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantAssessmentReferenceColumns = ["tenant_id", "assessment_reference"];
        private static readonly string[] TenantFacilityMaterialQuotedColumns = ["tenant_id", "facility_id", "material_id", "quoted_at_utc"];
        private static readonly string[] TenantSourceFingerprintColumns = ["tenant_id", "source_fingerprint"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantProfileCodeColumns = ["tenant_id", "profile_code"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_evidence_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quoted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quoted_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    freight_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_term_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_commercial_quotation_risk_evidence_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_evidence_observations_facilities_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_evidence_observations_tenants_ten~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_window_duration_days = table.Column<int>(type: "integer", nullable: false),
                    minimum_comparable_observations = table.Column<int>(type: "integer", nullable: false),
                    maximum_negative_price_variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    maximum_positive_price_variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    maximum_payment_term_days = table.Column<int>(type: "integer", nullable: false),
                    expiry_warning_days = table.Column<int>(type: "integer", nullable: false),
                    maximum_evidence_age_days = table.Column<int>(type: "integer", nullable: false),
                    allowed_evidence_statuses = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    allowed_sales_evidence_kinds = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    required_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_commercial_quotation_risk_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_profiles_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reference_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reference_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    quoted_comparable_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    signed_price_variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    candidate_observation_count = table.Column<int>(type: "integer", nullable: false),
                    comparable_observation_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_observation_count = table.Column<int>(type: "integer", nullable: false),
                    payment_term_days = table.Column<int>(type: "integer", nullable: false),
                    remaining_validity_days = table.Column<int>(type: "integer", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gross_net_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    freight_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_commercial_quotation_risk_assessments", x => x.id);
                    table.CheckConstraint("ck_commercial_quotation_risk_assessments_signed_variance", "signed_price_variance_ratio IS NULL OR signed_price_variance_ratio > -1");
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessments_commercial_quotation_~",
                        column: x => x.evidence_observation_id,
                        principalTable: "commercial_quotation_risk_evidence_observations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessments_commercial_quotation~1",
                        column: x => x.profile_id,
                        principalTable: "commercial_quotation_risk_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessments_facilities_tenant_id_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_quotation_risk_assessment_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
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
                    table.PrimaryKey("PK_commercial_quotation_risk_assessment_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessment_drivers_commercial_quo~",
                        column: x => x.assessment_id,
                        principalTable: "commercial_quotation_risk_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessment_drivers_facilities_ten~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_quotation_risk_assessment_drivers_tenants_tenant~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_assessment_drivers_assessment_id",
                table: "commercial_quotation_risk_assessment_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_commquote1_driver_tenant_facility_assessment",
                table: "commercial_quotation_risk_assessment_drivers",
                columns: TenantFacilityAssessmentSortColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_assessments_evidence_observation_~",
                table: "commercial_quotation_risk_assessments",
                column: "evidence_observation_id");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_quotation_risk_assessments_profile_id",
                table: "commercial_quotation_risk_assessments",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_commquote1_assessment_tenant_facility_material_outcome",
                table: "commercial_quotation_risk_assessments",
                columns: TenantFacilityMaterialOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commquote1_assessment_tenant_facility_window",
                table: "commercial_quotation_risk_assessments",
                columns: TenantFacilityWindowColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_quotation_risk_assessments_tenant_idempotency",
                table: "commercial_quotation_risk_assessments",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commercial_quotation_risk_assessments_tenant_reference",
                table: "commercial_quotation_risk_assessments",
                columns: TenantAssessmentReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commquote1_evidence_tenant_facility_material_quoted",
                table: "commercial_quotation_risk_evidence_observations",
                columns: TenantFacilityMaterialQuotedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commquote1_evidence_tenant_source_fingerprint",
                table: "commercial_quotation_risk_evidence_observations",
                columns: TenantSourceFingerprintColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_quotation_risk_profiles_tenant_facility_status",
                table: "commercial_quotation_risk_profiles",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_quotation_risk_profiles_tenant_code",
                table: "commercial_quotation_risk_profiles",
                columns: TenantProfileCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_quotation_risk_profiles_tenant_create_idempotency",
                table: "commercial_quotation_risk_profiles",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_quotation_risk_profiles
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_status
                        CHECK (status IN ('draft', 'approved', 'inactive')),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_min_observations
                        CHECK (minimum_comparable_observations >= 1),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_negative_variance
                        CHECK (maximum_negative_price_variance_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_positive_variance
                        CHECK (maximum_positive_price_variance_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_payment_term
                        CHECK (maximum_payment_term_days >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_expiry_warning
                        CHECK (expiry_warning_days >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_evidence_age
                        CHECK (maximum_evidence_age_days >= 1),
                    ADD CONSTRAINT ck_commercial_quotation_risk_profiles_gross_net
                        CHECK (required_gross_net_basis IN ('gross', 'net'));

                ALTER TABLE commercial_quotation_risk_evidence_observations
                    ADD CONSTRAINT ck_commercial_quotation_risk_evidence_quoted_price
                        CHECK (quoted_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_evidence_payment_term
                        CHECK (payment_term_days >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_quotation_risk_evidence_source_kind
                        CHECK (source_kind IN ('manual-development-simulator', 'erp-quotation-line')),
                    ADD CONSTRAINT ck_commercial_quotation_risk_evidence_price_basis
                        CHECK (
                            gross_net_basis IN ('gross', 'net')
                            AND tax_basis IN ('excluded', 'included', 'unknown')
                            AND discount_basis IN ('excluded', 'included', 'unknown')
                            AND freight_basis IN ('excluded', 'included', 'unknown'));

                ALTER TABLE commercial_quotation_risk_assessments
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_window
                        CHECK (reference_window_end_utc > reference_window_start_utc),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_quoted_price
                        CHECK (quoted_comparable_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_counts
                        CHECK (
                            candidate_observation_count >= 0
                            AND comparable_observation_count >= 0
                            AND excluded_observation_count >= 0
                            AND candidate_observation_count = comparable_observation_count + excluded_observation_count),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_payment_term
                        CHECK (payment_term_days >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_outcome
                        CHECK (outcome IN (
                            'within-threshold', 'attention', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessments_evidence_status
                        CHECK (evidence_status IN (
                            'simulated', 'measured', 'calculated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_quotation_risk_assessment_drivers
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessment_drivers_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessment_drivers_affected_count
                        CHECK (affected_observation_count >= 0),
                    ADD CONSTRAINT ck_commercial_quotation_risk_assessment_drivers_reason_code
                        CHECK (reason_code IN (
                            'quoted-price-below-reference', 'quoted-price-above-reference',
                            'quotation-expired', 'quotation-expiring-soon', 'payment-term-exceeded',
                            'insufficient-comparable-sales-evidence', 'stale-reference-evidence',
                            'source-conflict', 'incompatible-currency', 'incompatible-unit',
                            'incompatible-price-basis', 'within-profile'));

                REVOKE ALL ON TABLE commercial_quotation_risk_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE commercial_quotation_risk_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_quotation_risk_evidence_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_evidence_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_quotation_risk_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_quotation_risk_assessment_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_quotation_risk_assessment_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "commercial_quotation_risk_profiles", "cquotep");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_evidence_observations", "cquotee");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_assessments", "cquotea");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_quotation_risk_assessment_drivers", "cquoted");
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
                name: "commercial_quotation_risk_assessment_drivers");

            migrationBuilder.DropTable(
                name: "commercial_quotation_risk_assessments");

            migrationBuilder.DropTable(
                name: "commercial_quotation_risk_evidence_observations");

            migrationBuilder.DropTable(
                name: "commercial_quotation_risk_profiles");
        }
    }
}
