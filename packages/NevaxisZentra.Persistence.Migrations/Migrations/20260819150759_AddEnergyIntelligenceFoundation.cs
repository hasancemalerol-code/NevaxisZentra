using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergyIntelligenceFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantEvaluateIdempotencyColumns = ["tenant_id", "evaluate_idempotency_key"];
        private static readonly string[] TenantFacilityBaselineReferenceColumns = ["tenant_id", "facility_id", "baseline_reference"];
        private static readonly string[] TenantFacilityEvaluatedColumns = ["tenant_id", "facility_id", "evaluated_at_utc"];
        private static readonly string[] TenantFacilityAssessmentReferenceColumns = ["tenant_id", "facility_id", "assessment_reference"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantAssessmentReasonColumns = ["tenant_id", "assessment_id", "reason_code"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];
        private static readonly string[] TenantFacilityTargetReferenceColumns = ["tenant_id", "facility_id", "target_reference"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "energy_baseline_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    baseline_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    comparison_dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    measurement_point_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    production_context_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reporting_window_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    sample_count = table.Column<int>(type: "integer", nullable: false),
                    baseline_consumed_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    baseline_maximum_demand_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    baseline_kwh_per_kg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    baseline_kwh_per_1000_parts = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    baseline_kwh_per_1000_good_parts = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    median_center = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    mad_dispersion = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    normalized_dispersion = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    relative_tolerance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    upper_threshold_consumed_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    upper_threshold_demand_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    calculation_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_window_references_json = table.Column<string>(type: "text", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_baseline_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_baseline_profiles_energy_measurement_points_measurem~",
                        column: x => x.measurement_point_id,
                        principalTable: "energy_measurement_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_baseline_profiles_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_baseline_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tolerance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_energy_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_targets_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_targets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_intelligence_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    comparison_dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    measurement_point_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumption_window_id = table.Column<Guid>(type: "uuid", nullable: true),
                    baseline_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluation_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluation_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actual_consumed_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_maximum_demand_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_kwh_per_1000_good_parts = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    variance_consumed_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    variance_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    potential_reducible_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_cost_impact = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    cost_currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    opportunity_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    leading_driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_intelligence_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_assessments_energy_baseline_profiles_ba~",
                        column: x => x.baseline_profile_id,
                        principalTable: "energy_baseline_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_assessments_energy_consumption_windows_~",
                        column: x => x.consumption_window_id,
                        principalTable: "energy_consumption_windows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_assessments_energy_targets_target_id",
                        column: x => x.target_id,
                        principalTable: "energy_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_assessments_facilities_tenant_id_facili~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_intelligence_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actual_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actual_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    comparison_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comparison_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    normalized_variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    structured_evidence_json = table.Column<string>(type: "text", nullable: false),
                    evidence_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_leading_driver = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_intelligence_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_drivers_energy_intelligence_assessments~",
                        column: x => x.assessment_id,
                        principalTable: "energy_intelligence_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_drivers_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_intelligence_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_energy_baseline_profiles_measurement_point_id",
                table: "energy_baseline_profiles",
                column: "measurement_point_id");

            migrationBuilder.CreateIndex(
                name: "ux_energy_baseline_profiles_tenant_evaluate_idempotency",
                table: "energy_baseline_profiles",
                columns: TenantEvaluateIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_energy_baseline_profiles_tenant_facility_reference",
                table: "energy_baseline_profiles",
                columns: TenantFacilityBaselineReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_energy_intelligence_assessments_baseline_profile_id",
                table: "energy_intelligence_assessments",
                column: "baseline_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_energy_intelligence_assessments_consumption_window_id",
                table: "energy_intelligence_assessments",
                column: "consumption_window_id");

            migrationBuilder.CreateIndex(
                name: "IX_energy_intelligence_assessments_target_id",
                table: "energy_intelligence_assessments",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "ix_energy_intelligence_assessments_tenant_facility_evaluated",
                table: "energy_intelligence_assessments",
                columns: TenantFacilityEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_energy_intelligence_assessments_tenant_evaluate_idempotency",
                table: "energy_intelligence_assessments",
                columns: TenantEvaluateIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_energy_intelligence_assessments_tenant_facility_reference",
                table: "energy_intelligence_assessments",
                columns: TenantFacilityAssessmentReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_energy_intelligence_drivers_assessment_id",
                table: "energy_intelligence_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_energy_intelligence_drivers_tenant_id_facility_id",
                table: "energy_intelligence_drivers",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_energy_intelligence_drivers_assessment_reason",
                table: "energy_intelligence_drivers",
                columns: TenantAssessmentReasonColumns);

            migrationBuilder.CreateIndex(
                name: "ux_energy_targets_tenant_create_idempotency",
                table: "energy_targets",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_energy_targets_tenant_facility_reference",
                table: "energy_targets",
                columns: TenantFacilityTargetReferenceColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE energy_baseline_profiles
                    ADD CONSTRAINT ck_energy_baseline_profiles_sample_count
                        CHECK (sample_count >= 0),
                    ADD CONSTRAINT ck_energy_baseline_profiles_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_energy_baseline_profiles_validity
                        CHECK (valid_to_utc > valid_from_utc);

                ALTER TABLE energy_targets
                    ADD CONSTRAINT ck_energy_targets_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_energy_targets_validity
                        CHECK (effective_to_utc > effective_from_utc);

                ALTER TABLE energy_intelligence_assessments
                    ADD CONSTRAINT ck_energy_intelligence_assessments_version
                        CHECK (version >= 1);

                REVOKE ALL ON TABLE energy_baseline_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE energy_baseline_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_targets FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE energy_targets TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_intelligence_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_intelligence_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_intelligence_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_intelligence_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "energy_baseline_profiles", "ebp");
            ApplyTenantRls(migrationBuilder, "energy_targets", "etg");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_intelligence_assessments", "eia");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_intelligence_drivers", "eid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "energy_intelligence_drivers");

            migrationBuilder.DropTable(
                name: "energy_intelligence_assessments");

            migrationBuilder.DropTable(
                name: "energy_baseline_profiles");

            migrationBuilder.DropTable(
                name: "energy_targets");
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
    }
}
