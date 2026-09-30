using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <summary>
    /// Closes the identity_subjects runtime-enumeration defect (P1 tenant isolation).
    /// Migration 20260808130000 granted crevafactos_runtime column-level SELECT on
    /// identity_subjects (no RLS on that table), which let the runtime role enumerate
    /// every tenant's identities with or without tenant context. The only legitimate
    /// reader (facility membership validation) now goes through a constrained
    /// SECURITY DEFINER function that enforces the current tenant context.
    /// </summary>
    public partial class HardenIdentitySubjectsRuntimeAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.is_active_facility_member_by_subject(
                    p_tenant_id uuid,
                    p_facility_id uuid,
                    p_external_subject text)
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog, pg_temp
                AS $function$
                    SELECT COALESCE(
                        p_tenant_id IS NOT NULL
                        AND p_facility_id IS NOT NULL
                        AND p_external_subject IS NOT NULL
                        AND pg_catalog.length(p_external_subject) BETWEEN 1 AND 200
                        AND p_tenant_id = public.current_tenant_id()
                        AND EXISTS (
                            SELECT 1
                            FROM public.facility_memberships AS facility_membership
                            INNER JOIN public.tenant_memberships AS tenant_membership
                                ON tenant_membership.tenant_id = facility_membership.tenant_id
                               AND tenant_membership.id = facility_membership.tenant_membership_id
                            INNER JOIN public.identity_subjects AS identity_subject
                                ON identity_subject.id = tenant_membership.identity_subject_id
                            WHERE facility_membership.tenant_id = p_tenant_id
                              AND facility_membership.facility_id = p_facility_id
                              AND facility_membership.status = 'Active'
                              AND identity_subject.external_subject = p_external_subject),
                        false)
                $function$;

                REVOKE ALL ON FUNCTION public.is_active_facility_member_by_subject(uuid, uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.is_active_facility_member_by_subject(uuid, uuid, text) TO crevafactos_runtime;

                REVOKE SELECT (id, external_issuer, external_subject, status, created_at_utc)
                    ON TABLE public.identity_subjects FROM crevafactos_runtime;
                REVOKE SELECT ON TABLE public.identity_subjects FROM crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT SELECT (id, external_issuer, external_subject, status)
                    ON TABLE public.identity_subjects TO crevafactos_runtime;
                DROP FUNCTION IF EXISTS public.is_active_facility_member_by_subject(uuid, uuid, text);
                """);
        }
    }
}
