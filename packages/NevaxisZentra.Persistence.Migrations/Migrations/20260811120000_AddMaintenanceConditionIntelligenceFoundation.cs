using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddMaintenanceConditionIntelligenceFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS maintenance_asset_machine_mappings (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_type VARCHAR(50) NOT NULL,
                    asset_id VARCHAR(200) NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    asset_name VARCHAR(200) NULL,
                    machine_id UUID NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_maintenance_asset_machine_mappings PRIMARY KEY (id),
                    CONSTRAINT fk_mamm_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mamm_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mamm_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mamm_asset_type CHECK (asset_type IN ('machine', 'mold', 'auxiliary-equipment')),
                    CONSTRAINT ck_mamm_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mamm_tenant_facility_asset_reference
                    ON maintenance_asset_machine_mappings (tenant_id, facility_id, asset_reference);

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mamm_tenant_facility_machine
                    ON maintenance_asset_machine_mappings (tenant_id, facility_id, machine_id);

                CREATE TABLE IF NOT EXISTS maintenance_condition_metric_definitions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    metric_key VARCHAR(100) NOT NULL,
                    display_name VARCHAR(200) NOT NULL,
                    expected_unit VARCHAR(30) NULL,
                    evaluation_window_hours INT NOT NULL,
                    minimum_samples INT NOT NULL,
                    baseline_method VARCHAR(20) NOT NULL,
                    deviation_threshold_percent NUMERIC(9,2) NOT NULL,
                    trend_threshold_percent NUMERIC(9,2) NOT NULL,
                    volatility_threshold_percent NUMERIC(9,2) NULL,
                    enabled BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_maintenance_condition_metric_definitions PRIMARY KEY (id),
                    CONSTRAINT fk_mcmd_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcmd_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mcmd_baseline_method CHECK (baseline_method IN ('median', 'mean')),
                    CONSTRAINT ck_mcmd_minimum_samples CHECK (minimum_samples >= 1),
                    CONSTRAINT ck_mcmd_window_hours CHECK (evaluation_window_hours >= 1),
                    CONSTRAINT ck_mcmd_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mcmd_tenant_facility_metric_key
                    ON maintenance_condition_metric_definitions (tenant_id, facility_id, metric_key);

                CREATE TABLE IF NOT EXISTS maintenance_condition_profiles (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    machine_id UUID NOT NULL,
                    condition_status VARCHAR(20) NOT NULL,
                    overall_condition_score NUMERIC(5,2) NULL,
                    last_evaluated_at_utc TIMESTAMPTZ NULL,
                    data_freshness VARCHAR(20) NOT NULL,
                    data_sufficiency VARCHAR(20) NOT NULL,
                    active_anomaly_count INT NOT NULL DEFAULT 0,
                    highest_anomaly_severity VARCHAR(20) NULL,
                    trend_state VARCHAR(20) NOT NULL,
                    evaluation_identity VARCHAR(128) NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_maintenance_condition_profiles PRIMARY KEY (id),
                    CONSTRAINT fk_mcp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcp_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mcp_condition_status CHECK (condition_status IN ('normal', 'watch', 'degraded', 'critical', 'unknown')),
                    CONSTRAINT ck_mcp_data_freshness CHECK (data_freshness IN ('fresh', 'stale', 'unknown')),
                    CONSTRAINT ck_mcp_data_sufficiency CHECK (data_sufficiency IN ('high', 'medium', 'low', 'insufficient')),
                    CONSTRAINT ck_mcp_trend_state CHECK (trend_state IN ('stable', 'increasing', 'decreasing', 'volatile', 'insufficient')),
                    CONSTRAINT ck_mcp_version CHECK (version >= 1)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mcp_tenant_facility_asset_reference
                    ON maintenance_condition_profiles (tenant_id, facility_id, asset_reference);

                CREATE INDEX IF NOT EXISTS ix_mcp_tenant_facility_machine
                    ON maintenance_condition_profiles (tenant_id, facility_id, machine_id);

                CREATE TABLE IF NOT EXISTS maintenance_condition_anomalies (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    machine_id UUID NOT NULL,
                    metric_key VARCHAR(100) NOT NULL,
                    detected_at_utc TIMESTAMPTZ NOT NULL,
                    window_start_utc TIMESTAMPTZ NOT NULL,
                    window_end_utc TIMESTAMPTZ NOT NULL,
                    observed_value NUMERIC(18,4) NOT NULL,
                    baseline_value NUMERIC(18,4) NOT NULL,
                    deviation_percent NUMERIC(9,2) NOT NULL,
                    severity VARCHAR(20) NOT NULL,
                    direction VARCHAR(20) NOT NULL,
                    evidence_summary VARCHAR(2000) NOT NULL,
                    source_freshness VARCHAR(20) NOT NULL,
                    data_quality VARCHAR(20) NULL,
                    status VARCHAR(20) NOT NULL,
                    evaluation_identity VARCHAR(128) NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_condition_anomalies PRIMARY KEY (id),
                    CONSTRAINT fk_mca_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mca_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mca_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mca_severity CHECK (severity IN ('info', 'warning', 'high', 'critical')),
                    CONSTRAINT ck_mca_direction CHECK (direction IN ('high', 'low', 'volatile', 'trend-up', 'trend-down', 'unknown')),
                    CONSTRAINT ck_mca_status CHECK (status IN ('open', 'normalized', 'acknowledged')),
                    CONSTRAINT ck_mca_source_freshness CHECK (source_freshness IN ('fresh', 'stale', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mca_tenant_facility_evaluation_identity
                    ON maintenance_condition_anomalies (tenant_id, facility_id, evaluation_identity);

                CREATE INDEX IF NOT EXISTS ix_mca_tenant_facility_asset_detected
                    ON maintenance_condition_anomalies (tenant_id, facility_id, asset_reference, detected_at_utc DESC);

                CREATE INDEX IF NOT EXISTS ix_mca_tenant_facility_machine_metric
                    ON maintenance_condition_anomalies (tenant_id, facility_id, machine_id, metric_key);

                CREATE INDEX IF NOT EXISTS ix_mca_tenant_facility_status
                    ON maintenance_condition_anomalies (tenant_id, facility_id, status);

                REVOKE ALL ON TABLE maintenance_asset_machine_mappings FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_asset_machine_mappings TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_condition_metric_definitions FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_condition_metric_definitions TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_condition_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_condition_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_condition_anomalies FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_condition_anomalies TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "maintenance_asset_machine_mappings", "mamm");
            ApplyTenantRls(migrationBuilder, "maintenance_condition_metric_definitions", "mcmd");
            ApplyTenantRls(migrationBuilder, "maintenance_condition_profiles", "mcp");
            ApplyTenantRls(migrationBuilder, "maintenance_condition_anomalies", "mca");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS maintenance_condition_anomalies;
                DROP TABLE IF EXISTS maintenance_condition_profiles;
                DROP TABLE IF EXISTS maintenance_condition_metric_definitions;
                DROP TABLE IF EXISTS maintenance_asset_machine_mappings;
                """);
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
