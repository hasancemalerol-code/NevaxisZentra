using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    public partial class AddMixedProductionFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantFacilityTable(migrationBuilder, "mixed_production_areas", "mpa", new[]
            {
                "code character varying(64) NOT NULL",
                "name character varying(200) NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, code");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_lines", "mpl", new[]
            {
                "area_id uuid NOT NULL",
                "code character varying(64) NOT NULL",
                "name character varying(200) NOT NULL",
                "current_order_reference character varying(64)",
                "current_product_code character varying(64)",
                "current_shift_code character varying(32)",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, code");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_stations", "mps", new[]
            {
                "line_id uuid NOT NULL",
                "code character varying(64) NOT NULL",
                "name character varying(200) NOT NULL",
                "station_kind character varying(32) NOT NULL",
                "sequence_order integer NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, line_id, code");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_resources", "mpr", new[]
            {
                "station_id uuid NOT NULL",
                "resource_type character varying(32) NOT NULL",
                "code character varying(64) NOT NULL",
                "name character varying(200) NOT NULL",
                "machine_registry_code character varying(64)",
                "buffer_capacity integer",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, station_id, code");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_product_routings", "mpp", new[]
            {
                "product_code character varying(64) NOT NULL",
                "product_name character varying(200) NOT NULL",
                "revision character varying(16) NOT NULL",
                "is_active boolean NOT NULL",
                "effective_from_utc timestamp with time zone NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, product_code, revision");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_routing_operations", "mpro", new[]
            {
                "routing_id uuid NOT NULL",
                "station_id uuid NOT NULL",
                "sequence_order integer NOT NULL",
                "operation_code character varying(64) NOT NULL",
                "operation_name character varying(200) NOT NULL",
                "work_instruction_id character varying(64)",
                "created_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, routing_id, sequence_order");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_buffer_states", "mpb", new[]
            {
                "resource_id uuid NOT NULL",
                "capacity integer NOT NULL",
                "current_quantity integer NOT NULL",
                "is_blocked boolean NOT NULL",
                "is_starved boolean NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, resource_id");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_events", "mpe", new[]
            {
                "area_id uuid",
                "line_id uuid",
                "station_id uuid",
                "resource_id uuid",
                "resource_type character varying(32)",
                "event_type character varying(64) NOT NULL",
                "operation_code character varying(64)",
                "order_reference character varying(64)",
                "product_code character varying(64)",
                "material_code character varying(64)",
                "tool_code character varying(64)",
                "operator_code character varying(64)",
                "shift_code character varying(32)",
                "quantity numeric",
                "unit character varying(16)",
                "reason_code character varying(64)",
                "correlation_key character varying(256)",
                "source character varying(64)",
                "source_event_id character varying(128)",
                "evidence_json text",
                "idempotency_key character varying(256) NOT NULL",
                "occurred_at_utc timestamp with time zone NOT NULL",
                "received_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, idempotency_key");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_operator_sessions", "mpos", new[]
            {
                "station_id uuid NOT NULL",
                "resource_id uuid NOT NULL",
                "operator_code character varying(64) NOT NULL",
                "operation_code character varying(64) NOT NULL",
                "order_reference character varying(64)",
                "shift_code character varying(32)",
                "status character varying(32) NOT NULL",
                "outcome character varying(32)",
                "reason_code character varying(64)",
                "work_instruction_id uuid",
                "started_at_utc timestamp with time zone",
                "completed_at_utc timestamp with time zone",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, station_id, status");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_work_instructions", "mpwi", new[]
            {
                "operation_code character varying(64) NOT NULL",
                "station_code character varying(64)",
                "product_code character varying(64)",
                "revision character varying(16) NOT NULL",
                "title character varying(256) NOT NULL",
                "steps_json text NOT NULL",
                "quality_checkpoints_json text",
                "safety_notes text",
                "effective_from_utc timestamp with time zone NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, operation_code, revision");

            CreateTenantFacilityTable(migrationBuilder, "mixed_production_connector_mappings", "mpcm", new[]
            {
                "connector_id uuid",
                "connector_code character varying(64) NOT NULL",
                "mapping_kind character varying(32) NOT NULL",
                "station_id uuid",
                "resource_id uuid",
                "operation_code character varying(64)",
                "external_reference character varying(256) NOT NULL",
                "is_active boolean NOT NULL",
                "created_at_utc timestamp with time zone NOT NULL",
                "updated_at_utc timestamp with time zone NOT NULL",
            }, "tenant_id, facility_id, connector_code, external_reference");

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions
                    (id, permission_key, name, description, display_name, module, action,
                     sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f20', 'mixed-production.read', 'mixed-production.read',
                     'Read mixed production lines, timeline, and connected operations.',
                     'Karma üretim okuma', 'mixed-production', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f21', 'mixed-production.manage', 'mixed-production.manage',
                     'Manage mixed production routing, resources, and configuration.',
                     'Karma üretim yönetimi', 'mixed-production', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f22', 'mixed-production.operate', 'mixed-production.operate',
                     'Operate human stations and record production events.',
                     'Karma üretim operasyon', 'mixed-production', 'operate',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "mixed_production_connector_mappings");
            migrationBuilder.DropTable(name: "mixed_production_work_instructions");
            migrationBuilder.DropTable(name: "mixed_production_operator_sessions");
            migrationBuilder.DropTable(name: "mixed_production_events");
            migrationBuilder.DropTable(name: "mixed_production_buffer_states");
            migrationBuilder.DropTable(name: "mixed_production_routing_operations");
            migrationBuilder.DropTable(name: "mixed_production_product_routings");
            migrationBuilder.DropTable(name: "mixed_production_resources");
            migrationBuilder.DropTable(name: "mixed_production_stations");
            migrationBuilder.DropTable(name: "mixed_production_lines");
            migrationBuilder.DropTable(name: "mixed_production_areas");
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
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_insert') THEN
                        CREATE POLICY rls_{prefix}_tenant_insert ON {tableName}
                            FOR INSERT TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies WHERE tablename = '{tableName}' AND policyname = 'rls_{prefix}_tenant_update') THEN
                        CREATE POLICY rls_{prefix}_tenant_update ON {tableName}
                            FOR UPDATE TO crevafactos_runtime
                            USING (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid)
                            WITH CHECK (tenant_id = current_setting('NevaxisFactOS.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }
    }
}
