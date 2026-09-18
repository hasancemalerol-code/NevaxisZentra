using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixSaasIdentityInvitationLookupFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION resolve_saas_identity_invitation_by_token_hash(p_token_hash text)
                RETURNS TABLE (
                    invitation_id uuid,
                    tenant_id uuid,
                    intended_email_normalized text,
                    expires_at_utc timestamptz,
                    consumed_at_utc timestamptz,
                    invitation_reference text
                )
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog, public
                AS $function$
                    SELECT
                        invitation_row.id,
                        invitation_row.tenant_id,
                        invitation_row.intended_email_normalized,
                        invitation_row.expires_at_utc,
                        invitation_row.consumed_at_utc,
                        invitation_row.invitation_reference
                    FROM public.saas_identity_invitations AS invitation_row
                    WHERE invitation_row.token_hash = p_token_hash
                    LIMIT 1
                $function$;

                REVOKE ALL ON FUNCTION resolve_saas_identity_invitation_by_token_hash(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION resolve_saas_identity_invitation_by_token_hash(text) TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE ALL ON FUNCTION resolve_saas_identity_invitation_by_token_hash(text) FROM PUBLIC;
                REVOKE ALL ON FUNCTION resolve_saas_identity_invitation_by_token_hash(text) FROM crevafactos_runtime;
                DROP FUNCTION IF EXISTS resolve_saas_identity_invitation_by_token_hash(text);
                """);
        }
    }
}
