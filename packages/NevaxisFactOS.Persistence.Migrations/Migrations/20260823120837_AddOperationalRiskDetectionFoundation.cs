using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalRiskDetectionFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantDetectionKeyColumns = ["tenant_id", "detection_key"];
        private static readonly string[] TenantFacilityResultSortColumns = ["tenant_id", "facility_id", "result_id", "sort_order"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantProfileCodeColumns = ["tenant_id", "profile_code"];
        private static readonly string[] TenantCreateIdempotencyColumns = ["tenant_id", "create_idempotency_key"];
        private static readonly string[] TenantFacilityCaseColumns = ["tenant_id", "facility_id", "linked_case_id"];
        private static readonly string[] TenantFacilityDetectionKeyColumns = ["tenant_id", "facility_id", "detection_key"];
        private static readonly string[] TenantFacilityOutcomeColumns = ["tenant_id", "facility_id", "detection_outcome", "severity_band"];
        private static readonly string[] TenantFacilitySourceColumns = ["tenant_id", "facility_id", "source_module", "source_reference"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantProfileRuleColumns = ["tenant_id", "profile_id", "rule_code"];
        private static readonly string[] TenantFacilityWindowColumns = ["tenant_id", "facility_id", "window_start_utc", "window_end_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantRunReferenceColumns = ["tenant_id", "run_reference"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "detection_key",
                table: "operational_risk_cases",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "detection_rule_code",
                table: "operational_risk_cases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "occurrence_sequence",
                table: "operational_risk_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "operational_risk_detection_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_operational_risk_detection_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_profiles_facilities_tenant_id_fa~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_detection_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    risk_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_likelihood = table.Column<int>(type: "integer", nullable: false),
                    default_impact = table.Column<int>(type: "integer", nullable: false),
                    threshold_variance_band = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    driver_reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_detection_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_rules_facilities_tenant_id_facil~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_rules_operational_risk_detection~",
                        column: x => x.profile_id,
                        principalTable: "operational_risk_detection_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_rules_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_detection_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_count = table.Column<int>(type: "integer", nullable: false),
                    result_count = table.Column<int>(type: "integer", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_detection_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_runs_facilities_tenant_id_facili~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_runs_operational_risk_detection_~",
                        column: x => x.profile_id,
                        principalTable: "operational_risk_detection_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_detection_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    risk_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    detection_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    threshold_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    normalized_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    likelihood_level = table.Column<int>(type: "integer", nullable: true),
                    impact_level = table.Column<int>(type: "integer", nullable: true),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    severity_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    leading_driver_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    linked_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detection_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_assessment_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_detection_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_results_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_results_operational_risk_cases_l~",
                        column: x => x.linked_case_id,
                        principalTable: "operational_risk_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_results_operational_risk_detecti~",
                        column: x => x.run_id,
                        principalTable: "operational_risk_detection_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_results_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_detection_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    threshold_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    normalized_variance = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    supporting_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_detection_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_drivers_facilities_tenant_id_fac~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_drivers_operational_risk_detecti~",
                        column: x => x.result_id,
                        principalTable: "operational_risk_detection_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_detection_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_cases_tenant_detection_key_open",
                table: "operational_risk_cases",
                columns: TenantDetectionKeyColumns,
                unique: true,
                filter: "detection_key IS NOT NULL AND lifecycle_status NOT IN ('resolved','dismissed')");

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_drivers_result_id",
                table: "operational_risk_detection_drivers",
                column: "result_id");

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_drivers_tenant_facility_result",
                table: "operational_risk_detection_drivers",
                columns: TenantFacilityResultSortColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_profiles_tenant_facility_status",
                table: "operational_risk_detection_profiles",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_detection_profiles_tenant_code",
                table: "operational_risk_detection_profiles",
                columns: TenantProfileCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_detection_profiles_tenant_create_idempotency",
                table: "operational_risk_detection_profiles",
                columns: TenantCreateIdempotencyColumns,
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_results_linked_case_id",
                table: "operational_risk_detection_results",
                column: "linked_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_results_run_id",
                table: "operational_risk_detection_results",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_results_tenant_facility_case",
                table: "operational_risk_detection_results",
                columns: TenantFacilityCaseColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_results_tenant_facility_detection_key",
                table: "operational_risk_detection_results",
                columns: TenantFacilityDetectionKeyColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_results_tenant_facility_outcome",
                table: "operational_risk_detection_results",
                columns: TenantFacilityOutcomeColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_results_tenant_facility_source",
                table: "operational_risk_detection_results",
                columns: TenantFacilitySourceColumns);

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_rules_profile_id",
                table: "operational_risk_detection_rules",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_rules_tenant_id_facility_id",
                table: "operational_risk_detection_rules",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_detection_rules_tenant_profile_code",
                table: "operational_risk_detection_rules",
                columns: TenantProfileRuleColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_detection_runs_profile_id",
                table: "operational_risk_detection_runs",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_detection_runs_tenant_facility_window",
                table: "operational_risk_detection_runs",
                columns: TenantFacilityWindowColumns);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_detection_runs_tenant_idempotency",
                table: "operational_risk_detection_runs",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_detection_runs_tenant_reference",
                table: "operational_risk_detection_runs",
                columns: TenantRunReferenceColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE operational_risk_cases SET occurrence_sequence = 1 WHERE occurrence_sequence = 0;

                ALTER TABLE operational_risk_cases
                    ADD CONSTRAINT ck_operational_risk_cases_occurrence_sequence
                        CHECK (occurrence_sequence >= 1);

                ALTER TABLE operational_risk_detection_profiles
                    ADD CONSTRAINT ck_operational_risk_detection_profiles_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_operational_risk_detection_profiles_status
                        CHECK (status IN ('draft','approved','inactive'));

                ALTER TABLE operational_risk_detection_rules
                    ADD CONSTRAINT ck_operational_risk_detection_rules_likelihood
                        CHECK (default_likelihood >= 1 AND default_likelihood <= 5),
                    ADD CONSTRAINT ck_operational_risk_detection_rules_impact
                        CHECK (default_impact >= 1 AND default_impact <= 5);

                ALTER TABLE operational_risk_detection_runs
                    ADD CONSTRAINT ck_operational_risk_detection_runs_window
                        CHECK (window_end_utc > window_start_utc),
                    ADD CONSTRAINT ck_operational_risk_detection_runs_status
                        CHECK (status IN ('completed','failed'));

                ALTER TABLE operational_risk_detection_results
                    ADD CONSTRAINT ck_operational_risk_detection_results_likelihood
                        CHECK (likelihood_level IS NULL OR (likelihood_level >= 1 AND likelihood_level <= 5)),
                    ADD CONSTRAINT ck_operational_risk_detection_results_impact
                        CHECK (impact_level IS NULL OR (impact_level >= 1 AND impact_level <= 5)),
                    ADD CONSTRAINT ck_operational_risk_detection_results_score
                        CHECK (risk_score IS NULL OR (risk_score >= 1 AND risk_score <= 25)),
                    ADD CONSTRAINT ck_operational_risk_detection_results_outcome
                        CHECK (detection_outcome IN ('risk-detected','within-threshold','insufficient-data','source-conflict')),
                    ADD CONSTRAINT ck_operational_risk_detection_results_severity
                        CHECK (severity_band IS NULL OR severity_band IN ('low','moderate','high','critical'));

                REVOKE ALL ON TABLE operational_risk_detection_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE operational_risk_detection_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_detection_rules FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE operational_risk_detection_rules TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_detection_runs FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_detection_runs TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_detection_results FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_detection_results TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_detection_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_detection_drivers TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "operational_risk_detection_profiles", "ordp");
            ApplyTenantRls(migrationBuilder, "operational_risk_detection_rules", "ordr");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_detection_runs", "ordrun");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_detection_results", "ordres");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_detection_drivers", "orddrv");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operational_risk_detection_drivers");

            migrationBuilder.DropTable(
                name: "operational_risk_detection_rules");

            migrationBuilder.DropTable(
                name: "operational_risk_detection_results");

            migrationBuilder.DropTable(
                name: "operational_risk_detection_runs");

            migrationBuilder.DropTable(
                name: "operational_risk_detection_profiles");

            migrationBuilder.DropIndex(
                name: "ux_operational_risk_cases_tenant_detection_key_open",
                table: "operational_risk_cases");

            migrationBuilder.DropColumn(
                name: "detection_key",
                table: "operational_risk_cases");

            migrationBuilder.DropColumn(
                name: "detection_rule_code",
                table: "operational_risk_cases");

            migrationBuilder.DropColumn(
                name: "occurrence_sequence",
                table: "operational_risk_cases");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
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
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
