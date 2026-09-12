using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations;

/// <inheritdoc />
public partial class IntegrationWaveCConnectorFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE integration_connectors
            ADD COLUMN IF NOT EXISTS ingress_key character varying(100);
            """);

        migrationBuilder.Sql(
            """
            DO $$ BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_indexes
                    WHERE schemaname = current_schema()
                      AND indexname = 'IX_integration_connectors_ingress_key') THEN
                    CREATE UNIQUE INDEX "IX_integration_connectors_ingress_key"
                        ON integration_connectors (ingress_key)
                        WHERE ingress_key IS NOT NULL;
                END IF;
            END $$;
            """);

        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS resolve_integration_ingress_connector(text);
            CREATE OR REPLACE FUNCTION resolve_integration_ingress_connector(p_ingress_key text)
            RETURNS TABLE(
                tenant_id uuid,
                facility_id uuid,
                connector_id uuid,
                configuration_json text,
                secret_reference text,
                checkpoint_json text,
                active_mapping_version_id uuid,
                owner_subject_id text,
                tenant_key text)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, public
            AS $$
                SELECT
                    c.tenant_id,
                    c.facility_id,
                    c.id,
                    c.configuration_json,
                    c.secret_reference,
                    c.checkpoint_json,
                    c.active_mapping_version_id,
                    c.owner_subject_id,
                    t.tenant_key
                FROM public.integration_connectors c
                INNER JOIN public.tenants t
                    ON t.id = c.tenant_id
                WHERE c.ingress_key = p_ingress_key
                  AND c.connector_kind = 'webhook'
                  AND c.enabled = true
                LIMIT 1;
            $$;
            """);

        migrationBuilder.Sql(
            """
            REVOKE ALL ON FUNCTION resolve_integration_ingress_connector(text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION resolve_integration_ingress_connector(text) TO crevafactos_runtime;
            GRANT EXECUTE ON FUNCTION resolve_integration_ingress_connector(text) TO NevaxisFactOS;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS resolve_integration_ingress_connector(text);");

        migrationBuilder.DropIndex(
            name: "IX_integration_connectors_ingress_key",
            table: "integration_connectors");

        migrationBuilder.DropColumn(
            name: "ingress_key",
            table: "integration_connectors");
    }
}
