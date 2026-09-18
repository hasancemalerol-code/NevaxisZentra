using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialPurchasePriceIntelligenceFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityAssessmentSortColumns = ["tenant_id", "facility_id", "assessment_id", "sort_order"];
        private static readonly string[] TenantFacilityMaterialOutcomeColumns = ["tenant_id", "facility_id", "material_id", "assessment_outcome"];
        private static readonly string[] TenantFacilityWindowColumns = ["tenant_id", "facility_id", "window_start_utc", "window_end_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantAssessmentReferenceColumns = ["tenant_id", "assessment_reference"];
        private static readonly string[] TenantFacilityMaterialEventColumns = ["tenant_id", "facility_id", "material_id", "commercial_event_at_utc"];
        private static readonly string[] TenantFacilityObservationSetColumns = ["tenant_id", "facility_id", "observation_set_code"];
        private static readonly string[] TenantSourceFingerprintColumns = ["tenant_id", "source_fingerprint"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantProfileCodeColumns = ["tenant_id", "profile_code"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_purchase_price_evidence_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_document_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_line_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    commercial_event_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    source_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    normalized_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    price_basis_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    observation_set_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_purchase_price_evidence_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_evidence_observations_facilities_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_evidence_observations_tenants_ten~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_purchase_price_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comparison_dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_window_duration_days = table.Column<int>(type: "integer", nullable: false),
                    minimum_comparable_observations = table.Column<int>(type: "integer", nullable: false),
                    maximum_positive_variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    supplier_spread_threshold = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    allowed_evidence_statuses = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    required_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_center = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_commercial_purchase_price_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_profiles_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_purchase_price_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comparison_dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    reference_median_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    absolute_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    variance_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    comparable_observation_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_observation_count = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    normalized_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    price_basis_tax_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_discount_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_basis_freight_inclusion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assessment_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_commercial_purchase_price_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessments_commercial_purchase_p~",
                        column: x => x.profile_id,
                        principalTable: "commercial_purchase_price_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessments_facilities_tenant_id_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_purchase_price_assessment_drivers",
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
                    table.PrimaryKey("PK_commercial_purchase_price_assessment_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessment_drivers_commercial_pur~",
                        column: x => x.assessment_id,
                        principalTable: "commercial_purchase_price_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessment_drivers_facilities_ten~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_purchase_price_assessment_drivers_tenants_tenant~",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_purchase_price_assessment_drivers_assessment_id",
                table: "commercial_purchase_price_assessment_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_commint1_driver_tenant_facility_assessment",
                table: "commercial_purchase_price_assessment_drivers",
                columns: TenantFacilityAssessmentSortColumns);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_purchase_price_assessments_profile_id",
                table: "commercial_purchase_price_assessments",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_commint1_assessment_tenant_facility_material",
                table: "commercial_purchase_price_assessments",
                columns: TenantFacilityMaterialOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commint1_assessment_tenant_facility_window",
                table: "commercial_purchase_price_assessments",
                columns: TenantFacilityWindowColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_purchase_price_assessments_tenant_idempotency",
                table: "commercial_purchase_price_assessments",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commercial_purchase_price_assessments_tenant_reference",
                table: "commercial_purchase_price_assessments",
                columns: TenantAssessmentReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commint1_evidence_tenant_facility_material_evt",
                table: "commercial_purchase_price_evidence_observations",
                columns: TenantFacilityMaterialEventColumns);

            migrationBuilder.CreateIndex(
                name: "ix_commint1_evidence_tenant_facility_obs_set",
                table: "commercial_purchase_price_evidence_observations",
                columns: TenantFacilityObservationSetColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commint1_evidence_tenant_source_fingerprint",
                table: "commercial_purchase_price_evidence_observations",
                columns: TenantSourceFingerprintColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_purchase_price_profiles_tenant_facility_status",
                table: "commercial_purchase_price_profiles",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_purchase_price_profiles_tenant_code",
                table: "commercial_purchase_price_profiles",
                columns: TenantProfileCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_purchase_price_profiles_tenant_create_idempotency",
                table: "commercial_purchase_price_profiles",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000082',
                     'commercial-intelligence.read', 'commercial-intelligence.read',
                     'Read commercial intelligence purchase price profiles and assessments.',
                     'Ticari zekayı görüntüle', 'commercial-intelligence', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-24 12:15:00+00'),
                    ('10000000-0000-4000-8000-000000000083',
                     'commercial-intelligence.manage', 'commercial-intelligence.manage',
                     'Create and manage commercial intelligence purchase price profiles and assessments.',
                     'Ticari zekayı yönet', 'commercial-intelligence', 'manage-settings',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-24 12:15:00+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE commercial_purchase_price_profiles
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_status
                        CHECK (status IN ('draft', 'approved', 'inactive')),
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_comparison_dimension
                        CHECK (comparison_dimension IN ('material', 'material-supplier', 'purchase-order-line', 'facility')),
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_min_observations
                        CHECK (minimum_comparable_observations >= 1),
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_variance_ratio
                        CHECK (maximum_positive_variance_ratio >= 0),
                    ADD CONSTRAINT ck_commercial_purchase_price_profiles_reference_center
                        CHECK (reference_center = 'median');

                ALTER TABLE commercial_purchase_price_evidence_observations
                    ADD CONSTRAINT ck_commercial_purchase_price_evidence_observations_quantity
                        CHECK (quantity > 0),
                    ADD CONSTRAINT ck_commercial_purchase_price_evidence_observations_unit_price
                        CHECK (unit_price > 0),
                    ADD CONSTRAINT ck_commercial_purchase_price_evidence_observations_status
                        CHECK (evidence_status IN (
                            'measured-transaction', 'calculated', 'simulated', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_purchase_price_evidence_observations_price_basis
                        CHECK (
                            price_basis_tax_inclusion IN ('excluded', 'included', 'unknown')
                            AND price_basis_discount_inclusion IN ('excluded', 'included', 'unknown')
                            AND price_basis_freight_inclusion IN ('excluded', 'included', 'unknown'));

                ALTER TABLE commercial_purchase_price_assessments
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_window
                        CHECK (window_end_utc > window_start_utc),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_current_price
                        CHECK (current_unit_price > 0),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_counts
                        CHECK (comparable_observation_count >= 0 AND excluded_observation_count >= 0),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_variance_ratio
                        CHECK (variance_ratio IS NULL OR (variance_ratio >= -1 AND variance_ratio <= 1)),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_outcome
                        CHECK (assessment_outcome IN (
                            'within-reference-range', 'attention', 'favorable-variance', 'insufficient-data', 'source-conflict')),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_evidence_status
                        CHECK (evidence_status IN (
                            'measured-transaction', 'calculated', 'simulated', 'insufficient-data', 'source-conflict'));

                ALTER TABLE commercial_purchase_price_assessment_drivers
                    ADD CONSTRAINT ck_commercial_purchase_price_assessment_drivers_sort_order
                        CHECK (sort_order >= 1),
                    ADD CONSTRAINT ck_commercial_purchase_price_assessment_drivers_affected_count
                        CHECK (affected_observation_count >= 0);

                REVOKE ALL ON TABLE commercial_purchase_price_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE commercial_purchase_price_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_purchase_price_evidence_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_purchase_price_evidence_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_purchase_price_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_purchase_price_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE commercial_purchase_price_assessment_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE commercial_purchase_price_assessment_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "commercial_purchase_price_profiles", "cipp");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_purchase_price_evidence_observations", "cipeo");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_purchase_price_assessments", "cippa");
            ApplyTenantRlsAppendOnly(migrationBuilder, "commercial_purchase_price_assessment_drivers", "cippad");
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
                name: "commercial_purchase_price_assessment_drivers");

            migrationBuilder.DropTable(
                name: "commercial_purchase_price_evidence_observations");

            migrationBuilder.DropTable(
                name: "commercial_purchase_price_assessments");

            migrationBuilder.DropTable(
                name: "commercial_purchase_price_profiles");
        }
    }
}
