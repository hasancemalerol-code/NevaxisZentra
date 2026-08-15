using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAccessMembershipStatusWriteGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                GRANT UPDATE (status, authorization_version) ON TABLE tenant_memberships TO crevafactos_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE UPDATE (status) ON TABLE tenant_memberships FROM crevafactos_runtime;
                GRANT UPDATE (authorization_version) ON TABLE tenant_memberships TO crevafactos_runtime;
                """);
        }
    }
}
