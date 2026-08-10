using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    public partial class AddEdgeOperationalObservations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS machine_downtime_episodes (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    external_episode_id VARCHAR(200) NULL,
                    started_at_source_utc TIMESTAMPTZ NOT NULL,
                    ended_at_source_utc TIMESTAMPTZ NULL,
                    started_at_received_utc TIMESTAMPTZ NOT NULL,
                    ended_at_received_utc TIMESTAMPTZ NULL,
                    duration_ms BIGINT NULL,
                    reason_code VARCHAR(80) NULL,
                    reason_category VARCHAR(40) NULL,
                    reason_text VARCHAR(500) NULL,
                    is_micro_stop BOOLEAN NOT NULL DEFAULT FALSE,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    start_source_sequence BIGINT NULL,
                    end_source_sequence BIGINT NULL,
                    start_idempotency_key VARCHAR(200) NOT NULL,
                    end_idempotency_key VARCHAR(200) NULL,
                    is_closed BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_downtime_episodes PRIMARY KEY (id),
                    CONSTRAINT fk_mde_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mde_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_downtime_episodes_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_downtime_episodes_start_idempotency
                    ON machine_downtime_episodes (tenant_id, facility_id, start_idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_downtime_episodes_machine_time
                    ON machine_downtime_episodes (tenant_id, facility_id, machine_id, started_at_source_utc);

                CREATE TABLE IF NOT EXISTS machine_downtime_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    episode_id UUID NULL,
                    event_kind VARCHAR(60) NOT NULL,
                    external_episode_id VARCHAR(200) NULL,
                    reason_code VARCHAR(80) NULL,
                    reason_category VARCHAR(40) NULL,
                    reason_text VARCHAR(500) NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    is_unmatched_end BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_downtime_observations PRIMARY KEY (id),
                    CONSTRAINT fk_mdo_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mdo_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_downtime_observations_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_downtime_observations_idempotency
                    ON machine_downtime_observations (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_downtime_observations_machine_time
                    ON machine_downtime_observations (tenant_id, facility_id, machine_id, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_alarm_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    alarm_identity VARCHAR(120) NOT NULL,
                    external_alarm_code VARCHAR(80) NOT NULL,
                    alarm_text VARCHAR(500) NULL,
                    severity VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    event_kind VARCHAR(60) NOT NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    is_unmatched_clear BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_alarm_observations PRIMARY KEY (id),
                    CONSTRAINT fk_mao_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mao_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_alarm_observations_severity CHECK (severity IN ('info', 'warning', 'error', 'critical', 'unknown')),
                    CONSTRAINT ck_machine_alarm_observations_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_alarm_observations_idempotency
                    ON machine_alarm_observations (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_alarm_observations_machine_time
                    ON machine_alarm_observations (tenant_id, facility_id, machine_id, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_active_alarms (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    alarm_identity VARCHAR(120) NOT NULL,
                    external_alarm_code VARCHAR(80) NOT NULL,
                    alarm_text VARCHAR(500) NULL,
                    severity VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    raised_at_source_utc TIMESTAMPTZ NOT NULL,
                    raised_at_received_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    raised_observation_id UUID NOT NULL,
                    CONSTRAINT pk_machine_active_alarms PRIMARY KEY (id),
                    CONSTRAINT fk_maa_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_maa_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_active_alarms_severity CHECK (severity IN ('info', 'warning', 'error', 'critical', 'unknown')),
                    CONSTRAINT ck_machine_active_alarms_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_active_alarms_machine_identity
                    ON machine_active_alarms (tenant_id, facility_id, machine_id, alarm_identity);

                CREATE TABLE IF NOT EXISTS machine_alarm_projection (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    active_alarm_count INT NOT NULL DEFAULT 0,
                    highest_severity VARCHAR(20) NULL,
                    latest_alarm_code VARCHAR(80) NULL,
                    latest_alarm_text VARCHAR(500) NULL,
                    latest_raised_at_utc TIMESTAMPTZ NULL,
                    last_source_sequence BIGINT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_alarm_projection PRIMARY KEY (id),
                    CONSTRAINT fk_map_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_map_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_alarm_projection_count CHECK (active_alarm_count >= 0),
                    CONSTRAINT ck_machine_alarm_projection_severity CHECK (
                        highest_severity IS NULL
                        OR highest_severity IN ('info', 'warning', 'error', 'critical', 'unknown')
                    )
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_alarm_projection_machine
                    ON machine_alarm_projection (tenant_id, facility_id, machine_id);

                CREATE TABLE IF NOT EXISTS machine_setup_episodes (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    external_episode_id VARCHAR(200) NULL,
                    setup_type VARCHAR(80) NULL,
                    from_reference VARCHAR(200) NULL,
                    to_reference VARCHAR(200) NULL,
                    started_at_source_utc TIMESTAMPTZ NOT NULL,
                    completed_at_source_utc TIMESTAMPTZ NULL,
                    started_at_received_utc TIMESTAMPTZ NOT NULL,
                    completed_at_received_utc TIMESTAMPTZ NULL,
                    duration_ms BIGINT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    start_source_sequence BIGINT NULL,
                    complete_source_sequence BIGINT NULL,
                    start_idempotency_key VARCHAR(200) NOT NULL,
                    complete_idempotency_key VARCHAR(200) NULL,
                    is_closed BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_setup_episodes PRIMARY KEY (id),
                    CONSTRAINT fk_mse_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mse_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_setup_episodes_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_setup_episodes_start_idempotency
                    ON machine_setup_episodes (tenant_id, facility_id, start_idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_setup_episodes_machine_time
                    ON machine_setup_episodes (tenant_id, facility_id, machine_id, started_at_source_utc);

                CREATE TABLE IF NOT EXISTS machine_setup_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    episode_id UUID NULL,
                    event_kind VARCHAR(60) NOT NULL,
                    external_episode_id VARCHAR(200) NULL,
                    setup_type VARCHAR(80) NULL,
                    from_reference VARCHAR(200) NULL,
                    to_reference VARCHAR(200) NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    is_unmatched_complete BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_setup_observations PRIMARY KEY (id),
                    CONSTRAINT fk_mso_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mso_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_setup_observations_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_setup_observations_idempotency
                    ON machine_setup_observations (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_setup_observations_machine_time
                    ON machine_setup_observations (tenant_id, facility_id, machine_id, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_scrap_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    external_observation_id VARCHAR(200) NOT NULL,
                    quantity DOUBLE PRECISION NOT NULL,
                    unit VARCHAR(30) NOT NULL,
                    reason_code VARCHAR(80) NULL,
                    reason_category VARCHAR(40) NULL,
                    reason_text VARCHAR(500) NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_scrap_observations PRIMARY KEY (id),
                    CONSTRAINT fk_msc_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_msc_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_scrap_observations_quantity CHECK (quantity > 0),
                    CONSTRAINT ck_machine_scrap_observations_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_scrap_observations_idempotency
                    ON machine_scrap_observations (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_scrap_observations_machine_time
                    ON machine_scrap_observations (tenant_id, facility_id, machine_id, source_timestamp_utc);

                -- EDGE-3 fields on existing signal projection
                ALTER TABLE machine_signal_projection
                    ADD COLUMN IF NOT EXISTS is_in_observed_downtime BOOLEAN NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS current_downtime_started_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS observed_downtime_duration_ms BIGINT NULL,
                    ADD COLUMN IF NOT EXISTS active_alarm_count INT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS highest_alarm_severity VARCHAR(20) NULL,
                    ADD COLUMN IF NOT EXISTS is_in_observed_setup BOOLEAN NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS observed_setup_started_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS observed_scrap_count BIGINT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS observed_scrap_quantity DOUBLE PRECISION NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS last_observed_scrap_at_utc TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS last_operational_source_sequence BIGINT NULL;

                -- Grant permissions (append-only history, read-write projections, delete for active alarms)
                REVOKE ALL ON TABLE machine_downtime_episodes FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_downtime_episodes TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_downtime_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_downtime_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_alarm_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_alarm_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_active_alarms FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE machine_active_alarms TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_alarm_projection FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_alarm_projection TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_setup_episodes FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_setup_episodes TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_setup_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_setup_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_scrap_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_scrap_observations TO crevafactos_runtime;
                """);

            // Append-only RLS for history tables
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_downtime_observations", "mdobs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_alarm_observations", "maobs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_setup_observations", "msobs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_scrap_observations", "mscobs");

            // Read-write RLS for projections
            ApplyTenantRls(migrationBuilder, "machine_downtime_episodes", "mdepi");
            ApplyTenantRls(migrationBuilder, "machine_alarm_projection", "malproj");
            ApplyTenantRls(migrationBuilder, "machine_setup_episodes", "msepi");

            // Active alarms need DELETE for clear
            ApplyTenantRlsWithDelete(migrationBuilder, "machine_active_alarms", "maact");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS machine_scrap_observations;
                DROP TABLE IF EXISTS machine_setup_observations;
                DROP TABLE IF EXISTS machine_setup_episodes;
                DROP TABLE IF EXISTS machine_alarm_projection;
                DROP TABLE IF EXISTS machine_active_alarms;
                DROP TABLE IF EXISTS machine_alarm_observations;
                DROP TABLE IF EXISTS machine_downtime_observations;
                DROP TABLE IF EXISTS machine_downtime_episodes;

                ALTER TABLE machine_signal_projection
                    DROP COLUMN IF EXISTS last_operational_source_sequence,
                    DROP COLUMN IF EXISTS last_observed_scrap_at_utc,
                    DROP COLUMN IF EXISTS observed_scrap_quantity,
                    DROP COLUMN IF EXISTS observed_scrap_count,
                    DROP COLUMN IF EXISTS observed_setup_started_at_utc,
                    DROP COLUMN IF EXISTS is_in_observed_setup,
                    DROP COLUMN IF EXISTS highest_alarm_severity,
                    DROP COLUMN IF EXISTS active_alarm_count,
                    DROP COLUMN IF EXISTS observed_downtime_duration_ms,
                    DROP COLUMN IF EXISTS current_downtime_started_at_utc,
                    DROP COLUMN IF EXISTS is_in_observed_downtime;
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

        private static void ApplyTenantRlsWithDelete(MigrationBuilder migrationBuilder, string tableName, string prefix)
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

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = '{tableName}'
                          AND policyname = 'rls_{prefix}_tenant_delete') THEN
                        CREATE POLICY rls_{prefix}_tenant_delete
                            ON {tableName}
                            FOR DELETE
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
