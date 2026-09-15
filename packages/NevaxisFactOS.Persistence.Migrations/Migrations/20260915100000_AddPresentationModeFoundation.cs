using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationModeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000f83', 'presentation-mode.access', 'presentation-mode.access',
                     'Access FactOS presentation mode and controlled golden demo scenarios.',
                     'Sunum modu erişimi', 'presentation-mode', 'access',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-15 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM tenant_role_permissions
                WHERE permission_definition_id = '10000000-0000-4000-8000-000000000f83';

                DELETE FROM permission_definitions WHERE id = '10000000-0000-4000-8000-000000000f83';
                """);
        }
    }
}
