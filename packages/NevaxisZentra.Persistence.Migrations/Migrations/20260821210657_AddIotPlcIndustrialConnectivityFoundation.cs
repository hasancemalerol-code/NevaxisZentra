using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIotPlcIndustrialConnectivityFoundation : Migration
    {
        private static readonly string[] FacilityCompositePrincipalColumns = ["tenant_id", "id"];
        private static readonly string[] TenantFacilityColumns = ["tenant_id", "facility_id"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] EndpointObservedColumns = ["endpoint_id", "observed_at_utc"];
        private static readonly string[] SignalObservedColumns = ["signal_definition_id", "observed_at_utc"];
        private static readonly string[] SignalSourceRefColumns = ["signal_definition_id", "source_sequence_reference"];
        private static readonly string[] EndpointCodeColumns = ["tenant_id", "facility_id", "endpoint_code"];
        private static readonly string[] EndpointSignalCodeColumns = ["endpoint_id", "signal_code"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iot_plc_audit_events",
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
                    table.PrimaryKey("PK_iot_plc_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_audit_events_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_endpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protocol_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    connection_direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    endpoint_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    configuration_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    external_secret_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    connection_profile_json = table.Column<string>(type: "text", nullable: false),
                    last_observed_connection_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_endpoints", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_endpoints_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_endpoints_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_connection_health_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    health_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: true),
                    last_successful_observation_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_connection_health_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_connection_health_observations_facilities_tenant_id~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_connection_health_observations_iot_plc_endpoints_en~",
                        column: x => x.endpoint_id,
                        principalTable: "iot_plc_endpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_connection_health_observations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    data_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    engineering_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    semantic_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mapped_asset_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mapped_canonical_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sampling_expectation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_signals", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_signals_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signals_iot_plc_endpoints_endpoint_id",
                        column: x => x.endpoint_id,
                        principalTable: "iot_plc_endpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "iot_plc_signal_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapped_asset_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mapped_canonical_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    integer_value = table.Column<long>(type: "bigint", nullable: true),
                    decimal_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    text_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    timestamp_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quality_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_sequence_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_provenance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_plc_signal_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_observations_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: FacilityCompositePrincipalColumns,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_observations_iot_plc_signals_signal_definiti~",
                        column: x => x.signal_definition_id,
                        principalTable: "iot_plc_signals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_iot_plc_signal_observations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_audit_events_tenant_id_facility_id",
                table: "iot_plc_audit_events",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_connection_health_observations_tenant_id_facility_id",
                table: "iot_plc_connection_health_observations",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ix_iot_plc_connection_health_endpoint_observed",
                table: "iot_plc_connection_health_observations",
                columns: EndpointObservedColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_connection_health_tenant_idempotency",
                table: "iot_plc_connection_health_observations",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_endpoints_tenant_facility_code",
                table: "iot_plc_endpoints",
                columns: EndpointCodeColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_signal_observations_tenant_id_facility_id",
                table: "iot_plc_signal_observations",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_signal_observations_signal_observed_at",
                table: "iot_plc_signal_observations",
                columns: SignalObservedColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_signal_observations_signal_source_ref",
                table: "iot_plc_signal_observations",
                columns: SignalSourceRefColumns,
                unique: true,
                filter: "source_sequence_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_signal_observations_tenant_idempotency",
                table: "iot_plc_signal_observations",
                columns: TenantIdempotencyColumns,
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_iot_plc_signals_tenant_id_facility_id",
                table: "iot_plc_signals",
                columns: TenantFacilityColumns);

            migrationBuilder.CreateIndex(
                name: "ux_iot_plc_signals_endpoint_code",
                table: "iot_plc_signals",
                columns: EndpointSignalCodeColumns,
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000078',
                     'iot-plc.read', 'iot-plc.read',
                     'Read industrial connectivity endpoints, signals and observations.',
                     'IoT/PLC verilerini görüntüle', 'iot-plc', 'view',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-08-21 12:00:00+00'),
                    ('10000000-0000-4000-8000-000000000079',
                     'iot-plc.manage', 'iot-plc.manage',
                     'Manage industrial connectivity registry and ingest development observations.',
                     'IoT/PLC bağlantılarını yönet', 'iot-plc', 'manage-settings',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-08-21 12:00:00+00')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE iot_plc_endpoints
                    ADD CONSTRAINT ck_iot_plc_endpoints_version CHECK (version >= 1);

                ALTER TABLE iot_plc_signals
                    ADD CONSTRAINT ck_iot_plc_signals_version CHECK (version >= 1);

                REVOKE ALL ON TABLE iot_plc_endpoints FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE iot_plc_endpoints TO crevafactos_runtime;
                REVOKE ALL ON TABLE iot_plc_signals FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE iot_plc_signals TO crevafactos_runtime;
                REVOKE ALL ON TABLE iot_plc_signal_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_signal_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE iot_plc_connection_health_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_connection_health_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE iot_plc_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE iot_plc_audit_events TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "iot_plc_endpoints", "iplce");
            ApplyTenantRls(migrationBuilder, "iot_plc_signals", "iplcs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_signal_observations", "iplcso");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_connection_health_observations", "iplcch");
            ApplyTenantRlsAppendOnly(migrationBuilder, "iot_plc_audit_events", "iplcae");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000078',
                    '10000000-0000-4000-8000-000000000079');
                """);

            migrationBuilder.DropTable(
                name: "iot_plc_audit_events");

            migrationBuilder.DropTable(
                name: "iot_plc_connection_health_observations");

            migrationBuilder.DropTable(
                name: "iot_plc_signal_observations");

            migrationBuilder.DropTable(
                name: "iot_plc_signals");

            migrationBuilder.DropTable(
                name: "iot_plc_endpoints");
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
