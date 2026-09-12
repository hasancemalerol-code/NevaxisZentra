using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessOeeStabilityIntelligence : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantAssessmentReasonColumns = ["tenant_id", "assessment_id", "reason_code"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] BatchAssessmentColumns = ["batch_id", "assessment_id"];
        private static readonly string[] TenantAssessmentColumns = ["tenant_id", "assessment_id"];
        private static readonly string[] TenantFacilityStateColumns = ["tenant_id", "facility_id", "result_state"];
        private static readonly string[] TenantFingerprintColumns = ["tenant_id", "evidence_fingerprint"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_oee_shift_comparison_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comparison_period_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    comparison_period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_comparable = table.Column<bool>(type: "boolean", nullable: false),
                    comparability_warning_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    summary_json = table.Column<string>(type: "text", nullable: false),
                    evidence_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_shift_comparison_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_shift_comparison_batches_facilities_tenant_id_f~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_shift_comparison_batches_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_oee_stability_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    median_cycle_ms = table.Column<long>(type: "bigint", nullable: true),
                    mad_cycle_ms = table.Column<long>(type: "bigint", nullable: true),
                    sample_count = table.Column<int>(type: "integer", nullable: false),
                    drift_direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    micro_stop_count = table.Column<int>(type: "integer", nullable: false),
                    micro_stop_total_seconds = table.Column<int>(type: "integer", nullable: false),
                    micro_stop_median_seconds = table.Column<int>(type: "integer", nullable: false),
                    classified_downtime_seconds = table.Column<int>(type: "integer", nullable: false),
                    unclassified_downtime_seconds = table.Column<int>(type: "integer", nullable: false),
                    evidence_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_stability_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_assessments_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_assessments_process_oee_assessments_a~",
                        column: x => x.assessment_id,
                        principalTable: "process_oee_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_oee_shift_comparison_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shift_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_production_seconds = table.Column<int>(type: "integer", nullable: true),
                    operating_seconds = table.Column<int>(type: "integer", nullable: true),
                    availability_percent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    performance_percent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    quality_percent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    oee_percent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    stability_result_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    downtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    micro_stop_count = table.Column<int>(type: "integer", nullable: false),
                    data_sufficiency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_comparable = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_shift_comparison_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_shift_comparison_rows_facilities_tenant_id_faci~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_shift_comparison_rows_process_oee_shift_compari~",
                        column: x => x.batch_id,
                        principalTable: "process_oee_shift_comparison_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_shift_comparison_rows_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_oee_downtime_breakdown_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stability_assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    sort_rank = table.Column<int>(type: "integer", nullable: false),
                    is_classified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_downtime_breakdown_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_downtime_breakdown_rows_facilities_tenant_id_fa~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_downtime_breakdown_rows_process_oee_assessments~",
                        column: x => x.assessment_id,
                        principalTable: "process_oee_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_downtime_breakdown_rows_process_oee_stability_a~",
                        column: x => x.stability_assessment_id,
                        principalTable: "process_oee_stability_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_downtime_breakdown_rows_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_oee_stability_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stability_assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    result_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operational_priority = table.Column<int>(type: "integer", nullable: false),
                    observed_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    reference_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sample_count = table.Column<int>(type: "integer", nullable: false),
                    time_range_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    time_range_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_references_json = table.Column<string>(type: "text", nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    explanation_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_limitations_json = table.Column<string>(type: "text", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_oee_stability_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_drivers_facilities_tenant_id_facility~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_drivers_process_oee_assessments_asses~",
                        column: x => x.assessment_id,
                        principalTable: "process_oee_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_drivers_process_oee_stability_assessm~",
                        column: x => x.stability_assessment_id,
                        principalTable: "process_oee_stability_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_process_oee_stability_drivers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_downtime_breakdown_rows_assessment_id",
                table: "process_oee_downtime_breakdown_rows",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_downtime_breakdown_rows_stability_assessment_id",
                table: "process_oee_downtime_breakdown_rows",
                column: "stability_assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_downtime_breakdown_rows_tenant_id_facility_id",
                table: "process_oee_downtime_breakdown_rows",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_downtime_breakdown_tenant_assessment_reason",
                table: "process_oee_downtime_breakdown_rows",
                columns: TenantAssessmentReasonColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_shift_comparison_batches_tenant_id_facility_id",
                table: "process_oee_shift_comparison_batches",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_shift_comparison_batches_tenant_idempotency",
                table: "process_oee_shift_comparison_batches",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_shift_comparison_rows_tenant_id_facility_id",
                table: "process_oee_shift_comparison_rows",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_shift_comparison_rows_batch_assessment",
                table: "process_oee_shift_comparison_rows",
                columns: BatchAssessmentColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_stability_assessments_assessment_id",
                table: "process_oee_stability_assessments",
                column: "assessment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_stability_assessments_tenant_assessment",
                table: "process_oee_stability_assessments",
                columns: TenantAssessmentColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_stability_assessments_tenant_facility_state",
                table: "process_oee_stability_assessments",
                columns: TenantFacilityStateColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_stability_assessments_tenant_fingerprint",
                table: "process_oee_stability_assessments",
                columns: TenantFingerprintColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_stability_drivers_assessment_id",
                table: "process_oee_stability_drivers",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_stability_drivers_stability_assessment_id",
                table: "process_oee_stability_drivers",
                column: "stability_assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_process_oee_stability_drivers_tenant_id_facility_id",
                table: "process_oee_stability_drivers",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_oee_stability_drivers_tenant_assessment_reason",
                table: "process_oee_stability_drivers",
                columns: TenantAssessmentReasonColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE process_oee_stability_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_stability_assessments TO crevafactos_runtime;
                ALTER TABLE process_oee_stability_assessments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_stability_assessments FORCE ROW LEVEL SECURITY;
                CREATE POLICY posa_tenant_select ON process_oee_stability_assessments FOR SELECT USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY posa_tenant_insert ON process_oee_stability_assessments FOR INSERT WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                REVOKE ALL ON TABLE process_oee_stability_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_stability_drivers TO crevafactos_runtime;
                ALTER TABLE process_oee_stability_drivers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_stability_drivers FORCE ROW LEVEL SECURITY;
                CREATE POLICY posd_tenant_select ON process_oee_stability_drivers FOR SELECT USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY posd_tenant_insert ON process_oee_stability_drivers FOR INSERT WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                REVOKE ALL ON TABLE process_oee_downtime_breakdown_rows FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_downtime_breakdown_rows TO crevafactos_runtime;
                ALTER TABLE process_oee_downtime_breakdown_rows ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_downtime_breakdown_rows FORCE ROW LEVEL SECURITY;
                CREATE POLICY podb_tenant_select ON process_oee_downtime_breakdown_rows FOR SELECT USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY podb_tenant_insert ON process_oee_downtime_breakdown_rows FOR INSERT WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                REVOKE ALL ON TABLE process_oee_shift_comparison_batches FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_shift_comparison_batches TO crevafactos_runtime;
                ALTER TABLE process_oee_shift_comparison_batches ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_shift_comparison_batches FORCE ROW LEVEL SECURITY;
                CREATE POLICY poscb_tenant_select ON process_oee_shift_comparison_batches FOR SELECT USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY poscb_tenant_insert ON process_oee_shift_comparison_batches FOR INSERT WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);

                REVOKE ALL ON TABLE process_oee_shift_comparison_rows FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE process_oee_shift_comparison_rows TO crevafactos_runtime;
                ALTER TABLE process_oee_shift_comparison_rows ENABLE ROW LEVEL SECURITY;
                ALTER TABLE process_oee_shift_comparison_rows FORCE ROW LEVEL SECURITY;
                CREATE POLICY poscr_tenant_select ON process_oee_shift_comparison_rows FOR SELECT USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                CREATE POLICY poscr_tenant_insert ON process_oee_shift_comparison_rows FOR INSERT WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_oee_downtime_breakdown_rows");

            migrationBuilder.DropTable(
                name: "process_oee_shift_comparison_rows");

            migrationBuilder.DropTable(
                name: "process_oee_stability_drivers");

            migrationBuilder.DropTable(
                name: "process_oee_shift_comparison_batches");

            migrationBuilder.DropTable(
                name: "process_oee_stability_assessments");
        }
    }
}
