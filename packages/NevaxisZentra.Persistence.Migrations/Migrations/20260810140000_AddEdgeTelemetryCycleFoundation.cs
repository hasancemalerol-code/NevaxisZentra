using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddEdgeTelemetryCycleFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS machine_telemetry (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    metric_key VARCHAR(80) NOT NULL,
                    numeric_value DOUBLE PRECISION NULL,
                    text_value VARCHAR(500) NULL,
                    boolean_value BOOLEAN NULL,
                    unit VARCHAR(30) NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    transport VARCHAR(30) NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_telemetry PRIMARY KEY (id),
                    CONSTRAINT fk_mt_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mt_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_telemetry_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_telemetry_idempotency
                    ON machine_telemetry (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_telemetry_machine_metric_time
                    ON machine_telemetry (tenant_id, facility_id, machine_id, metric_key, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_latest_telemetry (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    metric_key VARCHAR(80) NOT NULL,
                    numeric_value DOUBLE PRECISION NULL,
                    text_value VARCHAR(500) NULL,
                    boolean_value BOOLEAN NULL,
                    unit VARCHAR(30) NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_latest_telemetry PRIMARY KEY (id),
                    CONSTRAINT fk_mlt_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mlt_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_latest_telemetry_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_latest_telemetry_machine_metric
                    ON machine_latest_telemetry (tenant_id, facility_id, machine_id, metric_key);

                CREATE TABLE IF NOT EXISTS machine_observed_cycles (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    cycle_identity VARCHAR(200) NOT NULL,
                    cycle_duration_ms BIGINT NOT NULL,
                    observed_counter BIGINT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_observed_cycles PRIMARY KEY (id),
                    CONSTRAINT fk_moc_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_moc_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_observed_cycles_duration CHECK (cycle_duration_ms > 0),
                    CONSTRAINT ck_machine_observed_cycles_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_observed_cycles_idempotency
                    ON machine_observed_cycles (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_observed_cycles_machine_time
                    ON machine_observed_cycles (tenant_id, facility_id, machine_id, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_cycle_projection (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    observed_cycle_count BIGINT NOT NULL DEFAULT 0,
                    latest_observed_counter BIGINT NULL,
                    last_cycle_at_utc TIMESTAMPTZ NULL,
                    last_cycle_duration_ms BIGINT NULL,
                    average_recent_cycle_duration_ms BIGINT NULL,
                    last_source_sequence BIGINT NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_cycle_projection PRIMARY KEY (id),
                    CONSTRAINT fk_mcp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mcp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_cycle_projection_count CHECK (observed_cycle_count >= 0)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_cycle_projection_machine
                    ON machine_cycle_projection (tenant_id, facility_id, machine_id);

                CREATE TABLE IF NOT EXISTS machine_counter_observations (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    gateway_id UUID NOT NULL,
                    counter_key VARCHAR(80) NOT NULL,
                    counter_value BIGINT NOT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    counter_reset_suspected BOOLEAN NOT NULL DEFAULT FALSE,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    idempotency_key VARCHAR(200) NOT NULL,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_counter_observations PRIMARY KEY (id),
                    CONSTRAINT fk_mco_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mco_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_counter_observations_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_counter_observations_idempotency
                    ON machine_counter_observations (tenant_id, facility_id, idempotency_key);
                CREATE INDEX IF NOT EXISTS ix_machine_counter_observations_machine_key_time
                    ON machine_counter_observations (tenant_id, facility_id, machine_id, counter_key, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_state_transitions (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    from_state VARCHAR(20) NOT NULL,
                    to_state VARCHAR(20) NOT NULL,
                    reason VARCHAR(200) NULL,
                    source_timestamp_utc TIMESTAMPTZ NOT NULL,
                    received_at_utc TIMESTAMPTZ NOT NULL,
                    source_sequence BIGINT NULL,
                    is_out_of_order BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_state_transitions PRIMARY KEY (id),
                    CONSTRAINT fk_mst_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_mst_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ix_machine_state_transitions_machine_time
                    ON machine_state_transitions (tenant_id, facility_id, machine_id, source_timestamp_utc);

                CREATE TABLE IF NOT EXISTS machine_signal_projection (
                    id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    facility_id UUID NOT NULL,
                    machine_id UUID NOT NULL,
                    connectivity VARCHAR(20) NOT NULL DEFAULT 'offline',
                    observed_state VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    last_cycle_at_utc TIMESTAMPTZ NULL,
                    last_cycle_duration_ms BIGINT NULL,
                    observed_cycle_count BIGINT NOT NULL DEFAULT 0,
                    latest_counter BIGINT NULL,
                    quality VARCHAR(20) NOT NULL DEFAULT 'unknown',
                    last_telemetry_at_utc TIMESTAMPTZ NULL,
                    updated_at_utc TIMESTAMPTZ NOT NULL,
                    CONSTRAINT pk_machine_signal_projection PRIMARY KEY (id),
                    CONSTRAINT fk_msp_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                    CONSTRAINT fk_msp_facility FOREIGN KEY (tenant_id, facility_id) REFERENCES facilities(tenant_id, id) ON DELETE RESTRICT,
                    CONSTRAINT ck_machine_signal_projection_quality CHECK (quality IN ('good', 'uncertain', 'bad', 'unknown'))
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_machine_signal_projection_machine
                    ON machine_signal_projection (tenant_id, facility_id, machine_id);

                -- Grant permissions (append-only history, read-write projections)
                REVOKE ALL ON TABLE machine_telemetry FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_telemetry TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_latest_telemetry FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_latest_telemetry TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_observed_cycles FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_observed_cycles TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_cycle_projection FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_cycle_projection TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_counter_observations FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_counter_observations TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_state_transitions FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE machine_state_transitions TO crevafactos_runtime;
                REVOKE ALL ON TABLE machine_signal_projection FROM crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE ON TABLE machine_signal_projection TO crevafactos_runtime;
                """);

            // Append-only RLS for history tables
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_telemetry", "mtel");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_observed_cycles", "mocyc");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_counter_observations", "mcobs");
            ApplyTenantRlsAppendOnly(migrationBuilder, "machine_state_transitions", "mstr");

            // Read-write RLS for projections
            ApplyTenantRls(migrationBuilder, "machine_latest_telemetry", "mltel");
            ApplyTenantRls(migrationBuilder, "machine_cycle_projection", "mcproj");
            ApplyTenantRls(migrationBuilder, "machine_signal_projection", "msig");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS machine_signal_projection;
                DROP TABLE IF EXISTS machine_state_transitions;
                DROP TABLE IF EXISTS machine_counter_observations;
                DROP TABLE IF EXISTS machine_cycle_projection;
                DROP TABLE IF EXISTS machine_observed_cycles;
                DROP TABLE IF EXISTS machine_latest_telemetry;
                DROP TABLE IF EXISTS machine_telemetry;
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
