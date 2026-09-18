using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddContextKnowledgeFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "context_knowledge_canonical_states", "ckcs", new[]
            {
                "entity_type character varying(64) NOT NULL",
                "entity_id character varying(128) NOT NULL",
                "resolution_status character varying(32) NOT NULL",
                "resolved_state_value character varying(256)",
                "confidence numeric NOT NULL",
                "source_claims_json text NOT NULL",
                "conflict_reasons_json text NOT NULL",
                "evaluated_at_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, entity_type, entity_id");

            CreateTenantFacilityTable(migrationBuilder, "context_knowledge_factory_records", "ckfr", new[]
            {
                "source_case_id uuid",
                "knowledge_key character varying(240) NOT NULL",
                "problem_symptom character varying(512) NOT NULL",
                "relevant_conditions_json text NOT NULL DEFAULT '[]'",
                "root_cause character varying(512) NOT NULL",
                "decision_summary character varying(512) NOT NULL",
                "actions_json text NOT NULL",
                "evidence_refs_json text NOT NULL",
                "before_state_json text NOT NULL",
                "after_state_json text NOT NULL",
                "verified_outcome character varying(256) NOT NULL",
                "confidence numeric NOT NULL",
                "applicability_constraints_json text NOT NULL",
                "promotion_status character varying(32) NOT NULL",
                "observation_window_start_utc timestamp with time zone",
                "observation_window_end_utc timestamp with time zone",
                "created_at_utc timestamp with time zone NOT NULL",
                "promoted_at_utc timestamp with time zone",
            }, "tenant_id, facility_id, knowledge_key");

            CreateTenantFacilityTable(migrationBuilder, "context_knowledge_snapshots", "cksn", new[]
            {
                "digital_work_case_id uuid",
                "snapshot_code character varying(128) NOT NULL",
                "scope_json text NOT NULL",
                "budget_json text NOT NULL",
                "materialized_context_json text NOT NULL",
                "provenance_json text NOT NULL",
                "metrics_json text NOT NULL",
                "engine_version character varying(64) NOT NULL",
                "created_by_subject_id character varying(128) NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, snapshot_code");

            CreateTenantFacilityTable(migrationBuilder, "context_knowledge_lifecycle_policies", "cklp", new[]
            {
                "data_type character varying(64) NOT NULL",
                "hot_retention_hours integer NOT NULL",
                "warm_retention_days integer NOT NULL",
                "cold_archive_days integer NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, data_type");

            CreateTenantFacilityTable(migrationBuilder, "context_knowledge_telemetry_aggregates", "ckta", new[]
            {
                "resource_id character varying(128) NOT NULL",
                "signal_type character varying(64) NOT NULL",
                "granularity character varying(16) NOT NULL",
                "window_start_utc timestamp with time zone NOT NULL",
                "window_end_utc timestamp with time zone NOT NULL",
                "aggregate_value numeric NOT NULL",
                "sample_count integer NOT NULL",
                "preserve_raw_window boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, resource_id, signal_type, granularity, window_start_utc");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f90', 'context-knowledge.read', 'context-knowledge.read',
                     'Read canonical operational context, snapshots, and verified factory knowledge.',
                     'Bağlam ve bilgi okuma', 'context-knowledge', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 11:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f91', 'context-knowledge.manage', 'context-knowledge.manage',
                     'Manage context lifecycle policies and canonical state resolution.',
                     'Bağlam ve bilgi yönetimi', 'context-knowledge', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 11:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f92', 'context-knowledge.promote', 'context-knowledge.promote',
                     'Promote verified factory outcomes to trusted knowledge records.',
                     'Doğrulanmış bilgi yükseltme', 'context-knowledge', 'promote',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-17 11:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "context_knowledge_telemetry_aggregates");
            migrationBuilder.DropTable(name: "context_knowledge_lifecycle_policies");
            migrationBuilder.DropTable(name: "context_knowledge_snapshots");
            migrationBuilder.DropTable(name: "context_knowledge_factory_records");
            migrationBuilder.DropTable(name: "context_knowledge_canonical_states");
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
                CREATE UNIQUE INDEX "IX_{tableName}_{uniqueIndexColumns.Replace(", ", "_")}"
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
}
