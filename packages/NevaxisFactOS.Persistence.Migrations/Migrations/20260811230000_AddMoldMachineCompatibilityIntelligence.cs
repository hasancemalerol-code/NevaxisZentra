using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldMachineCompatibilityIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE mold_compatibility_requirements (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    required_clamp_force_ton NUMERIC(18,3) NULL,
                    mold_width_mm NUMERIC(18,3) NULL,
                    mold_height_mm NUMERIC(18,3) NULL,
                    mold_depth_mm NUMERIC(18,3) NULL,
                    required_tie_bar_width_mm NUMERIC(18,3) NULL,
                    required_tie_bar_height_mm NUMERIC(18,3) NULL,
                    minimum_shot_capacity_grams NUMERIC(18,3) NULL,
                    required_injection_unit VARCHAR(100) NULL,
                    supported_material_group VARCHAR(100) NULL,
                    nominal_cycle_time_seconds NUMERIC(18,3) NULL,
                    effective_cavity_count INT NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_mold_compatibility_requirements PRIMARY KEY (id),
                    CONSTRAINT fk_mcr_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcr_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcr_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mcr_source_type CHECK (source_type IN ('manual','imported','erp','unknown')),
                    CONSTRAINT ck_mcr_version CHECK (version >= 1),
                    CONSTRAINT ck_mcr_effective_cavity CHECK (effective_cavity_count IS NULL OR effective_cavity_count > 0),
                    CONSTRAINT ck_mcr_cycle_time CHECK (nominal_cycle_time_seconds IS NULL OR nominal_cycle_time_seconds > 0)
                );

                CREATE UNIQUE INDEX ix_mold_compatibility_requirements_tenant_facility_mold
                    ON mold_compatibility_requirements (tenant_id, facility_id, mold_id);

                CREATE TABLE machine_compatibility_capability_profiles (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_asset_id UUID NOT NULL,
                    machine_code VARCHAR(100) NOT NULL,
                    machine_display_name VARCHAR(200) NOT NULL,
                    clamp_force_ton NUMERIC(18,3) NULL,
                    maximum_mold_width_mm NUMERIC(18,3) NULL,
                    maximum_mold_height_mm NUMERIC(18,3) NULL,
                    minimum_mold_depth_mm NUMERIC(18,3) NULL,
                    maximum_mold_depth_mm NUMERIC(18,3) NULL,
                    tie_bar_width_mm NUMERIC(18,3) NULL,
                    tie_bar_height_mm NUMERIC(18,3) NULL,
                    maximum_shot_capacity_grams NUMERIC(18,3) NULL,
                    injection_unit VARCHAR(100) NULL,
                    supported_material_groups VARCHAR(500) NULL,
                    source_type VARCHAR(20) NOT NULL,
                    source_reference VARCHAR(200) NULL,
                    data_recorded_at_utc TIMESTAMPTZ NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    updated_by_subject_id VARCHAR(200) NOT NULL,
                    version BIGINT NOT NULL,
                    CONSTRAINT pk_machine_compatibility_capability_profiles PRIMARY KEY (id),
                    CONSTRAINT fk_mccp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mccp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mccp_source_type CHECK (source_type IN ('manual','imported','erp','unknown')),
                    CONSTRAINT ck_mccp_version CHECK (version >= 1),
                    CONSTRAINT ck_mccp_machine_code CHECK (machine_code <> ''),
                    CONSTRAINT ck_mccp_machine_display_name CHECK (machine_display_name <> '')
                );

                CREATE UNIQUE INDEX ix_machine_compatibility_capability_profiles_tenant_facility_machine
                    ON machine_compatibility_capability_profiles (tenant_id, facility_id, machine_asset_id);

                CREATE TABLE mold_machine_compatibility_assessments (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    mold_id UUID NOT NULL,
                    machine_asset_id UUID NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    rule_set_version VARCHAR(50) NOT NULL,
                    requirement_version BIGINT NOT NULL,
                    machine_profile_version BIGINT NOT NULL,
                    evaluated_at_utc TIMESTAMPTZ NOT NULL,
                    structured_reasons_json JSONB NOT NULL,
                    structured_evidence_json JSONB NOT NULL,
                    theoretical_parts_per_hour NUMERIC(18,3) NULL,
                    input_fingerprint VARCHAR(64) NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    created_by_subject_id VARCHAR(200) NOT NULL,
                    CONSTRAINT pk_mold_machine_compatibility_assessments PRIMARY KEY (id),
                    CONSTRAINT fk_mmca_mold FOREIGN KEY (mold_id) REFERENCES molds(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mmca_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mmca_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_mmca_status CHECK (status IN ('compatible','conditional','incompatible','insufficient-data')),
                    CONSTRAINT ck_mmca_fingerprint CHECK (input_fingerprint <> '')
                );

                CREATE UNIQUE INDEX ix_mold_machine_compatibility_assessments_fingerprint
                    ON mold_machine_compatibility_assessments (tenant_id, mold_id, machine_asset_id, input_fingerprint);
                CREATE INDEX ix_mold_machine_compatibility_assessments_tenant_facility_mold_evaluated
                    ON mold_machine_compatibility_assessments (tenant_id, facility_id, mold_id, evaluated_at_utc DESC);

                REVOKE ALL ON TABLE mold_compatibility_requirements FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE mold_compatibility_requirements TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_compatibility_capability_profiles FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_compatibility_capability_profiles TO crevafactos_runtime;
                REVOKE ALL ON TABLE mold_machine_compatibility_assessments FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE mold_machine_compatibility_assessments TO crevafactos_runtime;
                """);

            ApplyTenantRls(migrationBuilder, "mold_compatibility_requirements", "mcr");
            ApplyTenantRls(migrationBuilder, "machine_compatibility_capability_profiles", "mccp");
            ApplyTenantRlsAppendOnly(migrationBuilder, "mold_machine_compatibility_assessments", "mmca");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS mold_machine_compatibility_assessments;
                DROP TABLE IF EXISTS machine_compatibility_capability_profiles;
                DROP TABLE IF EXISTS mold_compatibility_requirements;
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
