using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIotPlcSignalQualityConnectivityIntelligence : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "evaluate_idempotency_key"];
        private static readonly string[] TargetEvaluatedColumns = ["target_id", "evaluated_at_utc"];
        private static readonly string[] AssessmentKindColumns = ["assessment_kind", "assessment_id"];
        private static readonly string[] ProfileCodeColumns = ["tenant_id", "facility_id", "profile_code"];
        private static readonly string[] SignalEvaluatedColumns = ["signal_definition_id", "evaluated_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iot_plc_intelligence_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    outcome_severity = table.Column<int>(type: "integer", nullable: false),
                    threshold_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    affected_observation_count = table.Column<int>(type: "integer", nullable: false),
                    structured_evidence_json = table.Column<string>(type: "text", nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_leading_driver = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_intelligence_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_intelligence_drivers_facilities_tenant_id_facility_~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_intelligence_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_quality_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expected_observation_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    freshness_tolerance_seconds = table.Column<int>(type: "integer", nullable: false),
                    stale_threshold_seconds = table.Column<int>(type: "integer", nullable: false),
                    maximum_clock_skew_tolerance_seconds = table.Column<int>(type: "integer", nullable: false),
                    minimum_expected_observation_count = table.Column<int>(type: "integer", nullable: false),
                    minimum_good_quality_ratio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    maximum_bad_quality_ratio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    maximum_uncertain_quality_ratio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    maximum_source_conflict_ratio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    sequence_gap_evaluation_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_quality_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_quality_profiles_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_quality_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_connectivity_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluation_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quality_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    leading_driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latest_health_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    health_observation_availability_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    freshness_seconds = table.Column<int>(type: "integer", nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_connectivity_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_connectivity_assessments_facilities_tenant_id_facil~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_connectivity_assessments_iot_plc_quality_profiles_q~",
                        column: x => x.quality_profile_id,
                        principalTable: "iot_plc_quality_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_connectivity_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_signal_quality_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluation_window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluation_window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quality_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    leading_driver_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    leading_driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_observation_count = table.Column<int>(type: "integer", nullable: false),
                    expected_observation_count = table.Column<int>(type: "integer", nullable: true),
                    completeness_ratio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    good_count = table.Column<int>(type: "integer", nullable: false),
                    uncertain_count = table.Column<int>(type: "integer", nullable: false),
                    bad_count = table.Column<int>(type: "integer", nullable: false),
                    stale_count = table.Column<int>(type: "integer", nullable: false),
                    source_conflict_count = table.Column<int>(type: "integer", nullable: false),
                    good_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    uncertain_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    bad_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    stale_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    source_conflict_ratio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    latest_valid_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    freshness_seconds = table.Column<int>(type: "integer", nullable: true),
                    max_clock_skew_seconds = table.Column<int>(type: "integer", nullable: true),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_signal_quality_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_quality_assessments_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_quality_assessments_iot_plc_quality_profiles~",
                        column: x => x.quality_profile_id,
                        principalTable: "iot_plc_quality_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_quality_assessments_iot_plc_signals_signal_d~",
                        column: x => x.signal_definition_id,
                        principalTable: "iot_plc_signals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_quality_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_connectivity_assessments_quality_profile_id",
                table: "iot_plc_connectivity_assessments",
                column: "quality_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_connectivity_assessments_tenant_id_facility_id",
                table: "iot_plc_connectivity_assessments",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_iot_plc_connectivity_assessments_target_evaluated",
                table: "iot_plc_connectivity_assessments",
                columns: TargetEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_connectivity_assessments_tenant_idempotency",
                table: "iot_plc_connectivity_assessments",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_intelligence_drivers_tenant_id_facility_id",
                table: "iot_plc_intelligence_drivers",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_iot_plc_intelligence_drivers_assessment",
                table: "iot_plc_intelligence_drivers",
                columns: AssessmentKindColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_quality_profiles_tenant_facility_code",
                table: "iot_plc_quality_profiles",
                columns: ProfileCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_signal_quality_assessments_quality_profile_id",
                table: "iot_plc_signal_quality_assessments",
                column: "quality_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_signal_quality_assessments_tenant_id_facility_id",
                table: "iot_plc_signal_quality_assessments",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_iot_plc_signal_quality_assessments_signal_evaluated",
                table: "iot_plc_signal_quality_assessments",
                columns: SignalEvaluatedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_signal_quality_assessments_tenant_idempotency",
                table: "iot_plc_signal_quality_assessments",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE ON TABLE iot_plc_quality_profiles TO crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_connectivity_assessments TO crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_signal_quality_assessments TO crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_intelligence_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "iot_plc_quality_profiles", "iplcqp");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_connectivity_assessments", "iplcca");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_signal_quality_assessments", "iplcsqa");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_intelligence_drivers", "iplcid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iot_plc_connectivity_assessments");

            migrationBuilder.DropTable(
                name: "iot_plc_intelligence_drivers");

            migrationBuilder.DropTable(
                name: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropTable(
                name: "iot_plc_quality_profiles");
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
    }
}
