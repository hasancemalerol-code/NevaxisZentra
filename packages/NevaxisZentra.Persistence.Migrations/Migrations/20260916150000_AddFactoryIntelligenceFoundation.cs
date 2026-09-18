using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddFactoryIntelligenceFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factory_predictive_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    observation_window = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    measured_value = table.Column<decimal>(type: "numeric", nullable: false),
                    baseline_value = table.Column<decimal>(type: "numeric", nullable: true),
                    deviation_percent = table.Column<decimal>(type: "numeric", nullable: true),
                    trend = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_quality = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_references_json = table.Column<string>(type: "text", nullable: false),
                    engine_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_predictive_signals", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_predictive_signals_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_predictive_signals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "factory_operational_baselines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reference_value = table.Column<decimal>(type: "numeric", nullable: false),
                    current_value = table.Column<decimal>(type: "numeric", nullable: true),
                    deviation_percent = table.Column<decimal>(type: "numeric", nullable: true),
                    trend = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_quality = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    baseline_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    engine_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_operational_baselines", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_operational_baselines_facilities_tenant_id_facility~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_operational_baselines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "factory_predicted_risks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    risk_state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    affected_outcome = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    confidence_band = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    horizon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    time_to_impact_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    projected_gap_quantity = table.Column<int>(type: "integer", nullable: true),
                    order_reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    machine_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    material_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    contributing_signals_json = table.Column<string>(type: "text", nullable: false),
                    explanation_json = table.Column<string>(type: "text", nullable: false),
                    assumptions_json = table.Column<string>(type: "text", nullable: false),
                    data_quality = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    correlation_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    linked_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    engine_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_predicted_risks", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_predicted_risks_facilities_tenant_id_facility_id",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_predicted_risks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "factory_prediction_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    predicted_risk_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    predicted_outcome = table.Column<string>(type: "text", nullable: false),
                    actual_outcome = table.Column<string>(type: "text", nullable: true),
                    horizon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    confidence_band = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    intervention_occurred = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    evaluated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factory_prediction_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_factory_prediction_evaluations_factory_predicted_risks_pred~",
                        column: x => x.predicted_risk_id,
                        principalTable: "factory_predicted_risks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_prediction_evaluations_facilities_tenant_id_facilit~",
                        columns: x => new { x.tenant_id, x.facility_id },
                        principalTable: "facilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_factory_prediction_evaluations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_factory_predictive_signals_lookup",
                table: "factory_predictive_signals",
                columns: new[] { "tenant_id", "facility_id", "resource_id", "signal_type", "observed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_factory_operational_baselines_resource",
                table: "factory_operational_baselines",
                columns: new[] { "tenant_id", "facility_id", "resource_id", "signal_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_factory_predicted_risks_idempotency",
                table: "factory_predicted_risks",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_factory_predicted_risks_active",
                table: "factory_predicted_risks",
                columns: new[] { "tenant_id", "facility_id", "is_active", "risk_state" });

            migrationBuilder.CreateIndex(
                name: "ix_factory_prediction_evaluations_risk",
                table: "factory_prediction_evaluations",
                columns: new[] { "tenant_id", "facility_id", "predicted_risk_id" });

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f17', 'factory-intelligence.read', 'factory-intelligence.read',
                     'Read predictive factory intelligence and early warning risks.',
                     'Fabrika zekâsı okuma', 'factory-intelligence', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 15:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f18', 'factory-intelligence.manage', 'factory-intelligence.manage',
                     'Manage predictive evaluation and early warning workflows.',
                     'Fabrika zekâsı yönetimi', 'factory-intelligence', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 15:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f19', 'factory-intelligence.calibration', 'factory-intelligence.calibration',
                     'Manage prediction calibration and quality configuration.',
                     'Fabrika zekâsı kalibrasyon', 'factory-intelligence', 'calibration',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 15:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            ApplyTenantRls(migrationBuilder, "factory_predictive_signals", "fps");
            ApplyTenantRls(migrationBuilder, "factory_operational_baselines", "fob");
            ApplyTenantRls(migrationBuilder, "factory_predicted_risks", "fpr");
            ApplyTenantRls(migrationBuilder, "factory_prediction_evaluations", "fpe");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE factory_predictive_signals FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_predictive_signals TO crevafactos_runtime;
                REVOKE ALL ON TABLE factory_operational_baselines FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_operational_baselines TO crevafactos_runtime;
                REVOKE ALL ON TABLE factory_predicted_risks FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_predicted_risks TO crevafactos_runtime;
                REVOKE ALL ON TABLE factory_prediction_evaluations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE factory_prediction_evaluations TO crevafactos_runtime;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "factory_prediction_evaluations");
            migrationBuilder.DropTable(name: "factory_predicted_risks");
            migrationBuilder.DropTable(name: "factory_operational_baselines");
            migrationBuilder.DropTable(name: "factory_predictive_signals");
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
    }
}
