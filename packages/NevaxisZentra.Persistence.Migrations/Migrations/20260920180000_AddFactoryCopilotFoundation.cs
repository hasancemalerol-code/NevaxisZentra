using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class AddFactoryCopilotFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000fb1', 'factory-copilot.read', 'factory-copilot.read',
                     'Read Factory Copilot context, suggestions, and session metadata.',
                     'Factory Copilot okuma', 'factory-copilot', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-20 18:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fb2', 'factory-copilot.query', 'factory-copilot.query',
                     'Run contextual Factory Copilot operational queries.',
                     'Factory Copilot sorgu', 'factory-copilot', 'query',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-20 18:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000fb1',
                    '10000000-0000-4000-8000-000000000fb2');
                """);
        }
    }
}
