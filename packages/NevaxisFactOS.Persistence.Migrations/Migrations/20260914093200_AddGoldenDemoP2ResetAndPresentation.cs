using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldenDemoP2ResetAndPresentation : Migration
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
                    ('10000000-0000-4000-8000-000000000f7b', 'golden-demo-p2.read', 'golden-demo-p2.read',
                     'Read golden demo presentation orchestration status.',
                     'Altın demo sunum durumu oku', 'golden-demo-p2', 'read',
                     'standard', FALSE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00'),
                    ('10000000-0000-4000-8000-000000000f7c', 'golden-demo-p2.reset', 'golden-demo-p2.reset',
                     'Reset golden demo pilot event to repeatable starting state.',
                     'Altın demo pilot olayını sıfırla', 'golden-demo-p2', 'reset',
                     'sensitive', TRUE, 'tenant', 'Active', TIMESTAMPTZ '2026-09-14 00:00:00+00')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, read_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.read', 'production-manager.read', 'production.execution.read')
                JOIN permission_definitions read_perm ON read_perm.key = 'golden-demo-p2.read'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = read_perm.id);

                INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
                SELECT DISTINCT existing.tenant_id, existing.tenant_role_id, reset_perm.id
                FROM tenant_role_permissions existing
                JOIN permission_definitions source_perm ON source_perm.id = existing.permission_definition_id
                   AND source_perm.key IN ('golden-demo-p1.manage', 'golden-demo-p1.execute', 'production-manager.manage')
                JOIN permission_definitions reset_perm ON reset_perm.key = 'golden-demo-p2.reset'
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant_role_permissions already
                    WHERE already.tenant_id = existing.tenant_id
                      AND already.tenant_role_id = existing.tenant_role_id
                      AND already.permission_definition_id = reset_perm.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM tenant_role_permissions
                WHERE permission_definition_id IN (
                    '10000000-0000-4000-8000-000000000f7b',
                    '10000000-0000-4000-8000-000000000f7c');

                DELETE FROM permission_definitions WHERE id IN (
                    '10000000-0000-4000-8000-000000000f7b',
                    '10000000-0000-4000-8000-000000000f7c');
                """);
        }
    }
}
