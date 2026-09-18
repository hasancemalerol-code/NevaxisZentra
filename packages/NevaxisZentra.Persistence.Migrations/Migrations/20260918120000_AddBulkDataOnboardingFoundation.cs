using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations;

public partial class AddBulkDataOnboardingFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateTenantFacilityTable(migrationBuilder, "bulk_data_import_jobs", "bdi", new[]
        {
            "name character varying(256) NOT NULL",
            "status character varying(32) NOT NULL",
            "source_provider_kind character varying(64) NOT NULL",
            "source_kind character varying(32) NOT NULL",
            "source_system character varying(128) NOT NULL",
            "source_version character varying(32) NOT NULL",
            "import_version_number integer NOT NULL DEFAULT 1",
            "import_version_kind character varying(32) NOT NULL",
            "safety_mode character varying(32) NOT NULL",
            "connector_id uuid",
            "mapping_profile_id uuid",
            "checkpoint_json text",
            "watermark_json text",
            "impact_preview_json text",
            "reconciliation_json text",
            "observability_json text",
            "total_source_rows integer NOT NULL DEFAULT 0",
            "staged_rows integer NOT NULL DEFAULT 0",
            "valid_rows integer NOT NULL DEFAULT 0",
            "invalid_rows integer NOT NULL DEFAULT 0",
            "duplicate_rows integer NOT NULL DEFAULT 0",
            "quarantined_rows integer NOT NULL DEFAULT 0",
            "imported_rows integer NOT NULL DEFAULT 0",
            "updated_rows integer NOT NULL DEFAULT 0",
            "failed_rows integer NOT NULL DEFAULT 0",
            "idempotency_key character varying(256)",
            "created_by_subject_id character varying(128) NOT NULL",
            "created_at_utc timestamp with time zone NOT NULL",
            "updated_at_utc timestamp with time zone NOT NULL",
            "completed_at_utc timestamp with time zone",
            "failure_code character varying(64)",
            "safe_failure_message character varying(512)",
            "version integer NOT NULL DEFAULT 1",
        }, "tenant_id, facility_id, idempotency_key");

        CreateTenantFacilityTable(migrationBuilder, "factory_data_mapping_profiles", "fdm", new[]
        {
            "name character varying(256) NOT NULL",
            "source_system character varying(128) NOT NULL",
            "source_version character varying(32) NOT NULL",
            "status character varying(32) NOT NULL",
            "mapping_json text NOT NULL",
            "created_by_subject_id character varying(128) NOT NULL",
            "created_at_utc timestamp with time zone NOT NULL",
            "updated_at_utc timestamp with time zone NOT NULL",
            "version integer NOT NULL DEFAULT 1",
        }, "tenant_id, facility_id, source_system, source_version");

        CreateTenantFacilityTable(migrationBuilder, "bulk_import_source_catalog_entries", "bsc", new[]
        {
            "import_job_id uuid NOT NULL",
            "source_database character varying(128) NOT NULL",
            "source_schema character varying(128) NOT NULL",
            "source_table character varying(128) NOT NULL",
            "role_hint character varying(64) NOT NULL",
            "columns_json text NOT NULL",
            "estimated_row_count bigint NOT NULL DEFAULT 0",
            "primary_key_columns character varying(256)",
            "foreign_key_json text",
            "timestamp_columns character varying(256)",
            "statistics_json text",
            "discovered_at_utc timestamp with time zone NOT NULL",
        }, "tenant_id, import_job_id, source_table");

        CreateTenantFacilityTable(migrationBuilder, "bulk_import_staging_records", "bis", new[]
        {
            "import_job_id uuid NOT NULL",
            "source_system character varying(128) NOT NULL",
            "source_database character varying(128) NOT NULL",
            "source_table character varying(128) NOT NULL",
            "source_key character varying(256) NOT NULL",
            "record_checksum character varying(128) NOT NULL",
            "canonical_entity_type character varying(64) NOT NULL",
            "validation_state character varying(32) NOT NULL",
            "validation_reason_code character varying(64)",
            "payload_json text NOT NULL",
            "source_timestamp_raw character varying(128)",
            "source_timestamp_utc timestamp with time zone",
            "ingested_at_utc timestamp with time zone NOT NULL",
            "historical_tier character varying(32) NOT NULL",
            "batch_number integer NOT NULL DEFAULT 0",
        }, "tenant_id, import_job_id, source_table, source_key, record_checksum");

        CreateTenantFacilityTable(migrationBuilder, "bulk_import_quarantine_records", "biq", new[]
        {
            "import_job_id uuid NOT NULL",
            "staging_record_id uuid",
            "source_table character varying(128) NOT NULL",
            "source_key character varying(256) NOT NULL",
            "reason_code character varying(64) NOT NULL",
            "safe_reason character varying(512) NOT NULL",
            "severity character varying(16) NOT NULL",
            "status character varying(32) NOT NULL",
            "mapping_context_json text",
            "resolution_json text",
            "occurred_at_utc timestamp with time zone NOT NULL",
            "resolved_at_utc timestamp with time zone",
            "resolved_by_subject_id character varying(128)",
        }, "tenant_id, import_job_id, source_key");

        CreateTenantFacilityTable(migrationBuilder, "bulk_import_lineage_records", "bil", new[]
        {
            "import_job_id uuid NOT NULL",
            "canonical_entity_type character varying(64) NOT NULL",
            "canonical_entity_id uuid NOT NULL",
            "source_system character varying(128) NOT NULL",
            "source_database character varying(128) NOT NULL",
            "source_table character varying(128) NOT NULL",
            "source_key character varying(256) NOT NULL",
            "recorded_at_utc timestamp with time zone NOT NULL",
        }, "tenant_id, canonical_entity_type, canonical_entity_id");

        CreateTenantFacilityTable(migrationBuilder, "bulk_import_entity_mutations", "bim", new[]
        {
            "import_job_id uuid NOT NULL",
            "canonical_entity_type character varying(64) NOT NULL",
            "canonical_entity_id uuid NOT NULL",
            "mutation_kind character varying(32) NOT NULL",
            "recorded_at_utc timestamp with time zone NOT NULL",
            "rollback_eligible boolean NOT NULL DEFAULT TRUE",
            "rolled_back boolean NOT NULL DEFAULT FALSE",
        }, "tenant_id, import_job_id, canonical_entity_id");

        migrationBuilder.Sql(
            """
            INSERT INTO permission_definitions (
                id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
            VALUES
                ('10000000-0000-4000-8000-000000000fb0', 'bulk-onboarding.read', 'bulk-onboarding.read',
                 'Read bulk data onboarding jobs, catalog, staging summaries, and reconciliation.',
                 'Toplu veri aktarım okuma', 'bulk-onboarding', 'read',
                 'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-18 12:00:00+00'),
                ('10000000-0000-4000-8000-000000000fb1', 'bulk-onboarding.manage', 'bulk-onboarding.manage',
                 'Create and manage bulk data onboarding jobs and mapping profiles.',
                 'Toplu veri aktarım yönetimi', 'bulk-onboarding', 'manage',
                 'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-18 12:00:00+00'),
                ('10000000-0000-4000-8000-000000000fb2', 'bulk-onboarding.mapping.manage', 'bulk-onboarding.mapping.manage',
                 'Manage factory data mapping profiles for bulk onboarding.',
                 'Toplu veri aktarım eşleme yönetimi', 'bulk-onboarding', 'mapping.manage',
                 'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-18 12:00:00+00'),
                ('10000000-0000-4000-8000-000000000fb3', 'bulk-onboarding.run', 'bulk-onboarding.run',
                 'Run discovery, validation, dry-run, and staging for bulk onboarding jobs.',
                 'Toplu veri aktarım çalıştırma', 'bulk-onboarding', 'run',
                 'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-18 12:00:00+00'),
                ('10000000-0000-4000-8000-000000000fb4', 'bulk-onboarding.commit', 'bulk-onboarding.commit',
                 'Commit validated bulk onboarding data into canonical FactOS domains.',
                 'Toplu veri aktarım onaylı içe aktarma', 'bulk-onboarding', 'commit',
                 'critical', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-18 12:00:00+00')
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bulk_import_entity_mutations");
        migrationBuilder.DropTable(name: "bulk_import_lineage_records");
        migrationBuilder.DropTable(name: "bulk_import_quarantine_records");
        migrationBuilder.DropTable(name: "bulk_import_staging_records");
        migrationBuilder.DropTable(name: "bulk_import_source_catalog_entries");
        migrationBuilder.DropTable(name: "factory_data_mapping_profiles");
        migrationBuilder.DropTable(name: "bulk_data_import_jobs");
    }

    private static void CreateTenantFacilityTable(
        MigrationBuilder migrationBuilder,
        string tableName,
        string prefix,
        string[] extraColumns,
        string uniqueIndexColumns)
    {
        var columnsSql = string.Join(",\n                    ", extraColumns);
        migrationBuilder.Sql(
            $"""
            CREATE TABLE {tableName} (
                id uuid NOT NULL,
                tenant_id uuid NOT NULL,
                facility_id uuid NOT NULL,
                {columnsSql},
                CONSTRAINT "PK_{tableName}" PRIMARY KEY (id),
                CONSTRAINT "FK_{tableName}_facilities" FOREIGN KEY (tenant_id, facility_id)
                    REFERENCES facilities (tenant_id, id) ON DELETE RESTRICT,
                CONSTRAINT "FK_{tableName}_tenants" FOREIGN KEY (tenant_id)
                    REFERENCES tenants (id) ON DELETE RESTRICT
            );
            CREATE INDEX "IX_{tableName}_{uniqueIndexColumns.Replace(", ", "_")}"
                ON {tableName} ({uniqueIndexColumns});
            """);

        ApplyTenantRls(migrationBuilder, tableName, prefix);

        migrationBuilder.Sql(
            $"""
            REVOKE ALL ON TABLE {tableName} FROM crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE {tableName} TO crevafactos_runtime;
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
                    SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_select') THEN
                    CREATE POLICY rls_{prefix}_tenant_select ON {tableName}
                        FOR SELECT TO crevafactos_runtime
                        USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                END IF;
            END $$;

            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_insert') THEN
                    CREATE POLICY rls_{prefix}_tenant_insert ON {tableName}
                        FOR INSERT TO crevafactos_runtime
                        WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                END IF;
            END $$;

            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_update') THEN
                    CREATE POLICY rls_{prefix}_tenant_update ON {tableName}
                        FOR UPDATE TO crevafactos_runtime
                        USING (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid)
                        WITH CHECK (tenant_id = current_setting('NevaxisZentra.tenant_id', true)::uuid);
                END IF;
            END $$;
            """);
    }
}
