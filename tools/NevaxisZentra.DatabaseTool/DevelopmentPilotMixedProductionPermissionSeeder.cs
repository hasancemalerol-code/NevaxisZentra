using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Development;
using Npgsql;

namespace NevaxisZentra.DatabaseTool;

internal static class DevelopmentPilotMixedProductionPermissionSeeder
{
    public static async Task EnsureAsync()
    {
        var options = DatabaseConfiguration.LoadMigrations();
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options));
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO permission_definitions (
                id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
            SELECT *
            FROM (
                VALUES
                    (@read_id::uuid, 'mixed-production.read', 'mixed-production.read',
                     'Read mixed production lines, timeline, and connected operations.',
                     'Karma üretim okuma', 'mixed-production', 'read',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00'),
                    (@manage_id::uuid, 'mixed-production.manage', 'mixed-production.manage',
                     'Manage mixed production routing, resources, and configuration.',
                     'Karma üretim yönetimi', 'mixed-production', 'manage',
                     'sensitive', TRUE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00'),
                    (@operate_id::uuid, 'mixed-production.operate', 'mixed-production.operate',
                     'Operate human stations and record production events.',
                     'Karma üretim operasyon', 'mixed-production', 'operate',
                     'standard', FALSE, 'facility', 'Active', TIMESTAMPTZ '2026-09-16 16:00:00+00')
            ) AS seed(
                id, key, normalized_key, description, turkish_label, module_resource_code, action_code,
                sensitivity_classification, requires_recent_auth, supported_record_scopes, status, created_at_utc)
            WHERE NOT EXISTS (
                SELECT 1 FROM permission_definitions existing WHERE existing.key = seed.key);
            """,
            connection);

        command.Parameters.AddWithValue("read_id", DevelopmentPilotMixedProductionIds.ReadPermissionDefinitionId);
        command.Parameters.AddWithValue("manage_id", DevelopmentPilotMixedProductionIds.ManagePermissionDefinitionId);
        command.Parameters.AddWithValue("operate_id", DevelopmentPilotMixedProductionIds.OperatePermissionDefinitionId);

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Mixed production permission definitions ensured.");
    }
}
