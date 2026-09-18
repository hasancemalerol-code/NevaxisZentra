using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixSaasIdentityTenantMembershipRlsWritePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE POLICY tenant_memberships_insert_current
                    ON tenant_memberships
                    FOR INSERT
                    TO crevafactos_runtime
                    WITH CHECK (tenant_id = current_tenant_id());

                CREATE POLICY tenant_memberships_update_current
                    ON tenant_memberships
                    FOR UPDATE
                    TO crevafactos_runtime
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_memberships_update_current ON tenant_memberships;
                DROP POLICY IF EXISTS tenant_memberships_insert_current ON tenant_memberships;
                """);
        }
    }
}
