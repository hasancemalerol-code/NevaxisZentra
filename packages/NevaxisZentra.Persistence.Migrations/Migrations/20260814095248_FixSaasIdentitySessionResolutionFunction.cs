using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixSaasIdentitySessionResolutionFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION resolve_saas_identity_session_by_token_hash(p_token_hash text)
                RETURNS TABLE (
                    session_id uuid,
                    session_reference text,
                    account_id uuid,
                    account_reference text,
                    tenant_id uuid,
                    tenant_key text,
                    tenant_display_name text,
                    tenant_membership_id uuid,
                    identity_subject_id uuid,
                    security_stamp_snapshot uuid,
                    mfa_satisfied boolean,
                    pending_mfa boolean,
                    idle_expires_at_utc timestamptz,
                    absolute_expires_at_utc timestamptz,
                    revoked_at_utc timestamptz,
                    lifecycle_status text,
                    account_security_stamp uuid,
                    created_at_utc timestamptz,
                    mfa_required boolean
                )
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog, public
                AS $function$
                    SELECT
                        session_row.id,
                        session_row.session_reference,
                        session_row.account_id,
                        account_row.account_reference,
                        session_row.tenant_id,
                        tenant_row.tenant_key,
                        tenant_row.display_name,
                        session_row.tenant_membership_id,
                        account_row.identity_subject_id,
                        session_row.security_stamp_snapshot,
                        session_row.mfa_satisfied,
                        session_row.pending_mfa,
                        session_row.idle_expires_at_utc,
                        session_row.absolute_expires_at_utc,
                        session_row.revoked_at_utc,
                        account_row.lifecycle_status,
                        account_row.security_stamp,
                        session_row.created_at_utc,
                        COALESCE(mfa_row.is_required, false)
                    FROM public.saas_identity_sessions AS session_row
                    INNER JOIN public.saas_identity_accounts AS account_row
                        ON account_row.id = session_row.account_id
                    INNER JOIN public.tenants AS tenant_row
                        ON tenant_row.id = session_row.tenant_id
                    LEFT JOIN public.saas_identity_mfa_methods AS mfa_row
                        ON mfa_row.account_id = account_row.id
                    WHERE session_row.token_hash = p_token_hash
                    LIMIT 1
                $function$;

                REVOKE ALL ON FUNCTION resolve_saas_identity_session_by_token_hash(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_saas_identity_session_by_token_hash(text) TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE ALL ON FUNCTION resolve_saas_identity_session_by_token_hash(text) FROM PUBLIC;
                REVOKE ALL ON FUNCTION resolve_saas_identity_session_by_token_hash(text) FROM crevafactos_runtime;
                DROP FUNCTION IF EXISTS resolve_saas_identity_session_by_token_hash(text);
                """);
        }
    }
}
