using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergyMeasurementFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityStatusColumns = ["tenant_id", "facility_id", "status"];
        private static readonly string[] TenantFacilityCodeColumns = ["tenant_id", "facility_id", "point_code"];
        private static readonly string[] TenantFacilityPointOccurredColumns = ["tenant_id", "facility_id", "measurement_point_id", "occurred_at_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantEvaluateIdempotencyColumns = ["tenant_id", "evaluate_idempotency_key"];
        private static readonly string[] TenantFacilityCalculatedColumns = ["tenant_id", "facility_id", "calculated_at_utc"];
        private static readonly string[] TenantFacilityWindowRulesetColumns = ["tenant_id", "facility_id", "measurement_point_id", "window_start_utc", "window_end_utc", "rule_set_version"];
        private static readonly string[] TenantFacilityValidityColumns = ["tenant_id", "facility_id", "valid_from_utc", "valid_to_utc"];
        private static readonly string[] TenantFacilityOccurredColumns = ["tenant_id", "facility_id", "occurred_at_utc"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantWindowTargetColumns = ["tenant_id", "consumption_window_id", "target_type", "target_reference"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "energy_audit_events",
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
                    table.PrimaryKey("PK_energy_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_measurement_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    point_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    measurement_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    machine_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    edge_asset_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    equipment_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    measurement_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_energy_measurement_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_measurement_points_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_measurement_points_machine_registry_machine_asset_id",
                        column: x => x.machine_asset_id,
                        principalTable: "machine_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_measurement_points_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_tariff_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    energy_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    demand_unit_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_tariff_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_tariff_snapshots_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_tariff_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_readings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    measurement_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cumulative_energy_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    interval_energy_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    active_power_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    maximum_demand_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_readings", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_readings_energy_measurement_points_measurement_point~",
                        column: x => x.measurement_point_id,
                        principalTable: "energy_measurement_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_readings_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_readings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_consumption_windows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    measurement_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    average_power_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    maximum_demand_kw = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    calculation_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reading_count = table.Column<int>(type: "integer", nullable: false),
                    freshness_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_references_json = table.Column<string>(type: "text", nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    cost_currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    tariff_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    missing_data_explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    evaluate_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    evaluate_idempotency_payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_consumption_windows", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_consumption_windows_energy_measurement_points_measur~",
                        column: x => x.measurement_point_id,
                        principalTable: "energy_measurement_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_consumption_windows_energy_tariff_snapshots_tariff_s~",
                        column: x => x.tariff_snapshot_id,
                        principalTable: "energy_tariff_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_consumption_windows_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_consumption_windows_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "energy_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumption_window_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    allocated_kwh = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    allocation_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    confidence_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_energy_allocations_energy_consumption_windows_consumption_w~",
                        column: x => x.consumption_window_id,
                        principalTable: "energy_consumption_windows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_allocations_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_energy_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_energy_allocations_consumption_window_id",
                table: "energy_allocations",
                column: "consumption_window_id");

            migrationBuilder.CreateIndex(
                name: "IX_energy_allocations_tenant_id_facility_id",
                table: "energy_allocations",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_energy_allocations_window_target",
                table: "energy_allocations",
                columns: TenantWindowTargetColumns);

            migrationBuilder.CreateIndex(
                name: "ix_energy_audit_events_tenant_facility_occurred",
                table: "energy_audit_events",
                columns: TenantFacilityOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "IX_energy_consumption_windows_measurement_point_id",
                table: "energy_consumption_windows",
                column: "measurement_point_id");

            migrationBuilder.CreateIndex(
                name: "IX_energy_consumption_windows_tariff_snapshot_id",
                table: "energy_consumption_windows",
                column: "tariff_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_energy_consumption_windows_tenant_facility_calculated",
                table: "energy_consumption_windows",
                columns: TenantFacilityCalculatedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_energy_consumption_windows_point_window_ruleset",
                table: "energy_consumption_windows",
                columns: TenantFacilityWindowRulesetColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_energy_consumption_windows_tenant_evaluate_idempotency",
                table: "energy_consumption_windows",
                columns: TenantEvaluateIdempotencyColumns,
                unique: true,
                filter: "evaluate_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_energy_measurement_points_machine_asset_id",
                table: "energy_measurement_points",
                column: "machine_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_energy_measurement_points_tenant_facility_status",
                table: "energy_measurement_points",
                columns: TenantFacilityStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ux_energy_measurement_points_tenant_facility_code",
                table: "energy_measurement_points",
                columns: TenantFacilityCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_energy_readings_measurement_point_id",
                table: "energy_readings",
                column: "measurement_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_energy_readings_tenant_facility_point_occurred",
                table: "energy_readings",
                columns: TenantFacilityPointOccurredColumns);

            migrationBuilder.CreateIndex(
                name: "ux_energy_readings_tenant_idempotency",
                table: "energy_readings",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_energy_tariff_snapshots_tenant_facility_validity",
                table: "energy_tariff_snapshots",
                columns: TenantFacilityValidityColumns);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000076',
                     'energy.read', 'energy.read',
                     'Read energy measurement points, consumption windows and allocations.',
                     'Enerji verilerini görüntüle', 'energy', 'view',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-19 14:00:15+00'),
                    ('10000000-0000-4000-8000-000000000077',
                     'energy.manage', 'energy.manage',
                     'Ingest energy readings and evaluate consumption windows.',
                     'Enerji verilerini yönet', 'energy', 'manage-settings',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-19 14:00:15+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE energy_measurement_points
                    ADD CONSTRAINT ck_energy_measurement_points_version
                        CHECK (version >= 1),
                    ADD CONSTRAINT ck_energy_measurement_points_scope
                        CHECK (measurement_scope IN ('facility','line','machine','auxiliary-equipment'));

                ALTER TABLE energy_consumption_windows
                    ADD CONSTRAINT ck_energy_consumption_windows_window
                        CHECK (window_end_utc > window_start_utc),
                    ADD CONSTRAINT ck_energy_consumption_windows_consumed
                        CHECK (consumed_kwh >= 0);

                ALTER TABLE energy_allocations
                    ADD CONSTRAINT ck_energy_allocations_allocated
                        CHECK (allocated_kwh >= 0);

                ALTER TABLE energy_tariff_snapshots
                    ADD CONSTRAINT ck_energy_tariff_snapshots_validity
                        CHECK (valid_to_utc > valid_from_utc),
                    ADD CONSTRAINT ck_energy_tariff_snapshots_version
                        CHECK (version >= 1);

                REVOKE ALL ON TABLE energy_measurement_points FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE energy_measurement_points TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_tariff_snapshots FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE energy_tariff_snapshots TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_readings FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_readings TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_consumption_windows FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_consumption_windows TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_allocations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_allocations TO crevafactos_runtime;
                REVOKE ALL ON TABLE energy_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE energy_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "energy_measurement_points", "emp");
            ApplyTenantRls(migrationBuilder, "energy_tariff_snapshots", "ets");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_readings", "er");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_consumption_windows", "ecw");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_allocations", "ea");
            ApplyTenantRlsAppendOnly(migrationBuilder, "energy_audit_events", "eae");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "energy_allocations");

            migrationBuilder.DropTable(
                name: "energy_audit_events");

            migrationBuilder.DropTable(
                name: "energy_readings");

            migrationBuilder.DropTable(
                name: "energy_consumption_windows");

            migrationBuilder.DropTable(
                name: "energy_measurement_points");

            migrationBuilder.DropTable(
                name: "energy_tariff_snapshots");
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
