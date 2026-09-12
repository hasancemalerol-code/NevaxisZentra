using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.CheckConstraint("ck_tenants_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "tenant_isolation_probes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    probe_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_isolation_probes", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_isolation_probes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_isolation_probes_tenant_id",
                table: "tenant_isolation_probes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_tenant_key",
                table: "tenants",
                column: "tenant_key",
                unique: true);

            migrationBuilder.Sql(
                """
                DO $tenant_role$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'crevafactos_runtime') THEN
                        CREATE ROLE crevafactos_runtime
                            NOLOGIN
                            NOSUPERUSER
                            NOCREATEDB
                            NOCREATEROLE
                            NOINHERIT
                            NOBYPASSRLS;
                    ELSIF EXISTS (
                        SELECT 1
                        FROM pg_roles
                        WHERE rolname = 'crevafactos_runtime'
                          AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls)
                    ) THEN
                        RAISE EXCEPTION 'Existing crevafactos_runtime role has prohibited elevated privileges.';
                    END IF;
                END
                $tenant_role$;

                ALTER ROLE crevafactos_runtime
                    NOSUPERUSER
                    NOCREATEDB
                    NOCREATEROLE
                    NOINHERIT
                    NOBYPASSRLS;

                CREATE FUNCTION current_tenant_id()
                RETURNS uuid
                LANGUAGE sql
                STABLE
                PARALLEL SAFE
                AS $function$
                    SELECT NULLIF(current_setting('NevaxisFactOS.tenant_id', true), '')::uuid
                $function$;

                REVOKE ALL ON FUNCTION current_tenant_id() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION current_tenant_id() TO crevafactos_runtime;

                CREATE FUNCTION resolve_active_tenant(tenant_identifier text)
                RETURNS TABLE (tenant_id uuid, tenant_key text)
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                    SELECT tenant.id, tenant.tenant_key::text
                    FROM public.tenants AS tenant
                    WHERE (tenant.id::text = tenant_identifier OR tenant.tenant_key = tenant_identifier)
                      AND tenant.status = 'Active'
                    LIMIT 1
                $function$;

                REVOKE ALL ON FUNCTION resolve_active_tenant(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_active_tenant(text) TO crevafactos_runtime;

                GRANT USAGE ON SCHEMA public TO crevafactos_runtime;
                GRANT SELECT ON TABLE tenants TO crevafactos_runtime;
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE tenant_isolation_probes TO crevafactos_runtime;

                ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenants FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenants_select_current
                    ON tenants
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (id = current_tenant_id());

                ALTER TABLE tenant_isolation_probes ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_isolation_probes FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation_probes_select_current
                    ON tenant_isolation_probes
                    FOR SELECT
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                CREATE POLICY tenant_isolation_probes_insert_current
                    ON tenant_isolation_probes
                    FOR INSERT
                    TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_isolation_probes_update_current
                    ON tenant_isolation_probes
                    FOR UPDATE
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY tenant_isolation_probes_delete_current
                    ON tenant_isolation_probes
                    FOR DELETE
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION resolve_active_tenant(text);
                """);

            migrationBuilder.DropTable(
                name: "tenant_isolation_probes");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.Sql("DROP FUNCTION current_tenant_id();");
            migrationBuilder.Sql("REVOKE USAGE ON SCHEMA public FROM crevafactos_runtime;");
            migrationBuilder.Sql("DROP ROLE crevafactos_runtime;");
        }
    }
}
