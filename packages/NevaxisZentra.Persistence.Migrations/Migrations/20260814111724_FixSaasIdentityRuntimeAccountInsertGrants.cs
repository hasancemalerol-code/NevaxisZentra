using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixSaasIdentityRuntimeAccountInsertGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                GRANT INSERT ON TABLE saas_identity_accounts TO crevafactos_runtime;
                GRANT INSERT ON TABLE saas_identity_password_credentials TO crevafactos_runtime;
                GRANT INSERT ON TABLE tenant_memberships TO crevafactos_runtime;
                GRANT INSERT ON TABLE identity_subjects TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE INSERT ON TABLE saas_identity_accounts FROM crevafactos_runtime;
                REVOKE INSERT ON TABLE saas_identity_password_credentials FROM crevafactos_runtime;
                REVOKE INSERT ON TABLE tenant_memberships FROM crevafactos_runtime;
                REVOKE INSERT ON TABLE identity_subjects FROM crevafactos_runtime;
                """);
        }
    }
}
