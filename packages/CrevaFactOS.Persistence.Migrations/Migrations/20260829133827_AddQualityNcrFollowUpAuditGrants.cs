using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityNcrFollowUpAuditGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE ALL ON TABLE quality_ncr_follow_up_audit_events FROM crevafactos_runtime;
                GRANT SELECT, INSERT ON TABLE quality_ncr_follow_up_audit_events TO crevafactos_runtime;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'quality_ncr_follow_up_audit_events'
                          AND policyname = 'rls_qnfae_tenant_select') THEN
                        CREATE POLICY rls_qnfae_tenant_select
                            ON quality_ncr_follow_up_audit_events
                            FOR SELECT
                            TO crevafactos_runtime
                            USING (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname = current_schema()
                          AND tablename = 'quality_ncr_follow_up_audit_events'
                          AND policyname = 'rls_qnfae_tenant_insert') THEN
                        CREATE POLICY rls_qnfae_tenant_insert
                            ON quality_ncr_follow_up_audit_events
                            FOR INSERT
                            TO crevafactos_runtime
                            WITH CHECK (tenant_id = current_setting('crevafactos.tenant_id', true)::uuid);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
