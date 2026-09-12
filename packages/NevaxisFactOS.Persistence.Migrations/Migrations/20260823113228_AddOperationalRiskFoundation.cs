using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalRiskFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantFacilityCategoryColumns = ["tenant_id", "facility_id", "category"];
        private static readonly string[] TenantFacilityFirstDetectedColumns = ["tenant_id", "facility_id", "first_detected_at_utc"];
        private static readonly string[] TenantFacilityLastObservedColumns = ["tenant_id", "facility_id", "last_observed_at_utc"];
        private static readonly string[] TenantFacilityScopeColumns = ["tenant_id", "facility_id", "scope_type", "scope_reference"];
        private static readonly string[] TenantFacilitySeverityColumns = ["tenant_id", "facility_id", "severity_band"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "lifecycle_status"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantReferenceColumns = ["tenant_id", "case_reference"];
        private static readonly string[] TenantFacilityCaseColumns = ["tenant_id", "facility_id", "case_id"];
        private static readonly string[] TenantFacilityCaseOccurredColumns = ["tenant_id", "facility_id", "case_id", "occurred_at_utc"];
        private static readonly string[] TenantFacilityOccurredColumns = ["tenant_id", "facility_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operational_risk_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    actor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_audit_events_facilities_tenant_id_facility~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    likelihood_level = table.Column<int>(type: "integer", nullable: true),
                    impact_level = table.Column<int>(type: "integer", nullable: true),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    severity_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    assessment_outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    first_detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_cases_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_cases_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    numeric_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    comparison_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_evidence_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_evidence_operational_risk_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "operational_risk_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operational_risk_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expected_case_version = table.Column<long>(type: "bigint", nullable: false),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_risk_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_risk_status_history_facilities_tenant_id_facili~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_status_history_operational_risk_cases_case~",
                        column: x => x.case_id,
                        principalTable: "operational_risk_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operational_risk_status_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_audit_events_tenant_facility_occurred",
                table: "operational_risk_audit_events",
                columns: TenantFacilityOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility",
                table: "operational_risk_cases",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_category",
                table: "operational_risk_cases",
                columns: TenantFacilityCategoryColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_first_detected",
                table: "operational_risk_cases",
                columns: TenantFacilityFirstDetectedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_last_observed",
                table: "operational_risk_cases",
                columns: TenantFacilityLastObservedColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_scope",
                table: "operational_risk_cases",
                columns: TenantFacilityScopeColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_severity",
                table: "operational_risk_cases",
                columns: TenantFacilitySeverityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_cases_tenant_facility_status",
                table: "operational_risk_cases",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_cases_tenant_idempotency",
                table: "operational_risk_cases",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_cases_tenant_reference",
                table: "operational_risk_cases",
                columns: TenantReferenceColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_evidence_case_id",
                table: "operational_risk_evidence",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_evidence_tenant_facility_case",
                table: "operational_risk_evidence",
                columns: TenantFacilityCaseColumns);

            migrationBuilder.CreateIndex(
                name: "ux_operational_risk_evidence_tenant_idempotency",
                table: "operational_risk_evidence",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_operational_risk_status_history_case_id",
                table: "operational_risk_status_history",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_operational_risk_status_history_tenant_facility_case",
                table: "operational_risk_status_history",
                columns: TenantFacilityCaseOccurredColumns);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000080',
                     'operational-risk.read', 'operational-risk.read',
                     'Read operational risk cases, evidence, and lifecycle history.',
                     'Operasyonel riskleri görüntüle', 'operational-risk', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-23 11:32:28+00'),
                    ('10000000-0000-4000-8000-000000000081',
                     'operational-risk.manage', 'operational-risk.manage',
                     'Create and manage operational risk cases, evidence, assessments, and transitions.',
                     'Operasyonel riskleri yönet', 'operational-risk', 'manage-settings',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-23 11:32:28+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE operational_risk_cases
                    ADD CONSTRAINT ck_operational_risk_cases_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_operational_risk_cases_likelihood
                        CHECK (likelihood_level IS NULL OR (likelihood_level >= 1 AND likelihood_level <= 5)),
                    ADD CONSTRAINT ck_operational_risk_cases_impact
                        CHECK (impact_level IS NULL OR (impact_level >= 1 AND impact_level <= 5)),
                    ADD CONSTRAINT ck_operational_risk_cases_score
                        CHECK (risk_score IS NULL OR (risk_score >= 1 AND risk_score <= 25)),
                    ADD CONSTRAINT ck_operational_risk_cases_category
                        CHECK (category IN (
                            'material-availability','supplier','order-delivery','cost-deviation',
                            'production-continuity','quality','maintenance','energy','iot-connectivity','other')),
                    ADD CONSTRAINT ck_operational_risk_cases_scope_type
                        CHECK (scope_type IN (
                            'facility','material','supplier','purchase-order','customer-order',
                            'production-order','machine','mold','shipment','energy-measurement-point',
                            'iot-endpoint','other')),
                    ADD CONSTRAINT ck_operational_risk_cases_evidence_state
                        CHECK (evidence_state IN (
                            'measured','calculated','estimated','simulated','insufficient-data','source-conflict')),
                    ADD CONSTRAINT ck_operational_risk_cases_lifecycle_status
                        CHECK (lifecycle_status IN (
                            'detected','under-review','accepted','mitigation-planned','monitoring','resolved','dismissed')),
                    ADD CONSTRAINT ck_operational_risk_cases_assessment_outcome
                        CHECK (assessment_outcome IN ('scored','insufficient-data','source-conflict')),
                    ADD CONSTRAINT ck_operational_risk_cases_severity_band
                        CHECK (severity_band IS NULL OR severity_band IN ('low','moderate','high','critical')),
                    ADD CONSTRAINT ck_operational_risk_cases_observed_window
                        CHECK (last_observed_at_utc >= first_detected_at_utc);

                ALTER TABLE operational_risk_evidence
                    ADD CONSTRAINT ck_operational_risk_evidence_state
                        CHECK (evidence_state IN (
                            'measured','calculated','estimated','simulated','insufficient-data','source-conflict'));

                ALTER TABLE operational_risk_status_history
                    ADD CONSTRAINT ck_operational_risk_status_history_statuses
                        CHECK (
                            previous_status IN (
                                'detected','under-review','accepted','mitigation-planned','monitoring','resolved','dismissed')
                            AND new_status IN (
                                'detected','under-review','accepted','mitigation-planned','monitoring','resolved','dismissed'));

                REVOKE ALL ON TABLE operational_risk_cases FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE operational_risk_cases TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_evidence FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_evidence TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_status_history FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_status_history TO crevafactos_runtime;
                REVOKE ALL ON TABLE operational_risk_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE operational_risk_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "operational_risk_cases", "orc");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_evidence", "ore");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_status_history", "orsh");
            ApplyTenantRlsAppendOnly(migrationBuilder, "operational_risk_audit_events", "orae");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operational_risk_audit_events");

            migrationBuilder.DropTable(
                name: "operational_risk_evidence");

            migrationBuilder.DropTable(
                name: "operational_risk_status_history");

            migrationBuilder.DropTable(
                name: "operational_risk_cases");
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
