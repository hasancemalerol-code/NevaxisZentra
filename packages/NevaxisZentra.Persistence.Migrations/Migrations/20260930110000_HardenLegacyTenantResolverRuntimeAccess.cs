using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// Closes a second pre-tenant-context enumeration path found while verifying the
    /// identity_subjects hardening (P1). Migration 20260804143343 originally revoked
    /// crevafactos_runtime's EXECUTE on resolve_active_tenant(text) because it lets any
    /// caller probe arbitrary tenant identifiers for existence/active-status. Migration
    /// 20260814055239 re-granted it for the SaaS sign-in flow, silently reopening the gap.
    /// The sign-in flow now calls a differently named, identically scoped, SECURITY DEFINER
    /// replacement, and the legacy function is fully unreachable by the runtime role again.
    /// </summary>
    public partial class HardenLegacyTenantResolverRuntimeAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.resolve_active_tenant_for_signin(tenant_identifier text)
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

                REVOKE ALL ON FUNCTION public.resolve_active_tenant_for_signin(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.resolve_active_tenant_for_signin(text) TO crevafactos_runtime;

                REVOKE EXECUTE ON FUNCTION public.resolve_active_tenant(text) FROM crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT EXECUTE ON FUNCTION public.resolve_active_tenant(text) TO crevafactos_runtime;
                DROP FUNCTION IF EXISTS public.resolve_active_tenant_for_signin(text);
                """);
        }
    }
}
