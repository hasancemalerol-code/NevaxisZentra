using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    public partial class AddMaintenancePredictiveRiskFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS maintenance_predictive_risk_assessments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    machine_id UUID NOT NULL,
                    risk_band VARCHAR(30) NOT NULL,
                    review_priority VARCHAR(20) NOT NULL,
                    rule_set_version VARCHAR(40) NOT NULL,
                    condition_evaluation_identity VARCHAR(128) NULL,
                    data_freshness VARCHAR(20) NOT NULL,
                    data_sufficiency VARCHAR(20) NOT NULL,
                    active_anomaly_count INT NOT NULL DEFAULT 0,
                    maintenance_history_summary VARCHAR(2000) NOT NULL,
                    explanation VARCHAR(4000) NOT NULL,
                    creation_source VARCHAR(50) NOT NULL,
                    assessed_at_utc TIMESTAMPTZ NOT NULL,
                    is_current BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_predictive_risk_assessments PRIMARY KEY (id),
                    CONSTRAINT fk_mpra_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mpra_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mpra_machine FOREIGN KEY (machine_id) REFERENCES machine_registry(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mpra_risk_band CHECK (risk_band IN ('insufficient-data', 'low', 'watch', 'high', 'critical')),
                    CONSTRAINT ck_mpra_review_priority CHECK (review_priority IN ('routine', 'elevated', 'planned', 'urgent')),
                    CONSTRAINT ck_mpra_data_freshness CHECK (data_freshness IN ('fresh', 'stale', 'unknown')),
                    CONSTRAINT ck_mpra_data_sufficiency CHECK (data_sufficiency IN ('high', 'medium', 'low', 'insufficient'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mpra_tenant_facility_asset_eval_rule
                    ON maintenance_predictive_risk_assessments (tenant_id, facility_id, asset_reference, condition_evaluation_identity, rule_set_version);

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mpra_tenant_facility_asset_current
                    ON maintenance_predictive_risk_assessments (tenant_id, facility_id, asset_reference)
                    WHERE is_current = TRUE;

                CREATE INDEX IF NOT EXISTS ix_mpra_tenant_facility_assessed
                    ON maintenance_predictive_risk_assessments (tenant_id, facility_id, assessed_at_utc DESC);

                CREATE TABLE IF NOT EXISTS maintenance_predictive_risk_drivers (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    assessment_id UUID NOT NULL,
                    driver_code VARCHAR(80) NOT NULL,
                    summary VARCHAR(2000) NOT NULL,
                    sort_order INT NOT NULL DEFAULT 0,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_predictive_risk_drivers PRIMARY KEY (id),
                    CONSTRAINT fk_mprd_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mprd_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mprd_assessment FOREIGN KEY (assessment_id) REFERENCES maintenance_predictive_risk_assessments(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ix_mprd_tenant_facility_assessment
                    ON maintenance_predictive_risk_drivers (tenant_id, facility_id, assessment_id, sort_order);

                CREATE TABLE IF NOT EXISTS maintenance_predictive_risk_evidence (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    assessment_id UUID NOT NULL,
                    evidence_type VARCHAR(50) NOT NULL,
                    evidence_reference VARCHAR(200) NOT NULL,
                    summary VARCHAR(2000) NOT NULL,
                    sort_order INT NOT NULL DEFAULT 0,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_predictive_risk_evidence PRIMARY KEY (id),
                    CONSTRAINT fk_mpre_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mpre_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mpre_assessment FOREIGN KEY (assessment_id) REFERENCES maintenance_predictive_risk_assessments(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ix_mpre_tenant_facility_assessment
                    ON maintenance_predictive_risk_evidence (tenant_id, facility_id, assessment_id, sort_order);

                CREATE TABLE IF NOT EXISTS maintenance_predictive_recommendations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    asset_reference VARCHAR(200) NOT NULL,
                    assessment_id UUID NOT NULL,
                    category VARCHAR(60) NOT NULL,
                    review_priority VARCHAR(20) NOT NULL,
                    explanation VARCHAR(4000) NOT NULL,
                    rule_set_version VARCHAR(40) NOT NULL,
                    status VARCHAR(20) NOT NULL,
                    generated_at_utc TIMESTAMPTZ NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_maintenance_predictive_recommendations PRIMARY KEY (id),
                    CONSTRAINT fk_mprr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mprr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mprr_assessment FOREIGN KEY (assessment_id) REFERENCES maintenance_predictive_risk_assessments(id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mprr_category CHECK (category IN (
                        'review-data-quality',
                        'continue-routine-monitoring',
                        'close-monitoring',
                        'plan-technical-review',
                        'urgent-authorized-technical-review')),
                    CONSTRAINT ck_mprr_review_priority CHECK (review_priority IN ('routine', 'elevated', 'planned', 'urgent')),
                    CONSTRAINT ck_mprr_status CHECK (status IN ('current', 'superseded'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_mprr_tenant_facility_assessment
                    ON maintenance_predictive_recommendations (tenant_id, facility_id, assessment_id);

                CREATE INDEX IF NOT EXISTS ix_mprr_tenant_facility_asset_status
                    ON maintenance_predictive_recommendations (tenant_id, facility_id, asset_reference, status);

                REVOKE ALL ON TABLE maintenance_predictive_risk_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_predictive_risk_assessments TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_predictive_risk_drivers FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_predictive_risk_drivers TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_predictive_risk_evidence FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_predictive_risk_evidence TO crevafactos_runtime;
                REVOKE ALL ON TABLE maintenance_predictive_recommendations FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_predictive_recommendations TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "maintenance_predictive_risk_assessments", "mpra");
            ApplyTenantRls(migrationBuilder, "maintenance_predictive_risk_drivers", "mprd");
            ApplyTenantRls(migrationBuilder, "maintenance_predictive_risk_evidence", "mpre");
            ApplyTenantRls(migrationBuilder, "maintenance_predictive_recommendations", "mprr");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS maintenance_predictive_recommendations;
                DROP TABLE IF EXISTS maintenance_predictive_risk_evidence;
                DROP TABLE IF EXISTS maintenance_predictive_risk_drivers;
                DROP TABLE IF EXISTS maintenance_predictive_risk_assessments;
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
    }
}
