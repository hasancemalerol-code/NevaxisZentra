using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    public partial class FixScenarioRecoveryPermissionDefinitions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (
                    id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                    sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000fa2', 'scenario-recovery.read', 'scenario-recovery.read',
                     'Read scenario recovery workspace, comparison, and history.',
                     'Senaryo kurtarma okuma', 'scenario-recovery', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 22:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fa3', 'scenario-recovery.manage', 'scenario-recovery.manage',
                     'Manage scenario recovery configuration and lifecycle metadata.',
                     'Senaryo kurtarma yönetimi', 'scenario-recovery', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 22:00:00+00'),
                    ('10000000-0000-4000-8000-000000000fa4', 'scenario-recovery.simulate', 'scenario-recovery.simulate',
                     'Run scenario simulation and option comparison.',
                     'Senaryo simülasyonu', 'scenario-recovery', 'simulate',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-19 22:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM permission_definitions
                WHERE id IN (
                    '10000000-0000-4000-8000-000000000fa2',
                    '10000000-0000-4000-8000-000000000fa3',
                    '10000000-0000-4000-8000-000000000fa4',
                    '10000000-0000-4000-8000-000000000fa5');
                """);
        }
    }
}
