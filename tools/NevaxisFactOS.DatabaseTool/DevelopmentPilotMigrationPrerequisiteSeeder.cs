using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Development;
using Npgsql;

namespace NevaxisFactOS.DatabaseTool;

internal static class DevelopmentPilotMigrationPrerequisiteSeeder
{
    public static async Task EnsureAsync()
    {
        var options = DatabaseConfiguration.LoadMigrations();
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options));
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
            VALUES
                (@planner_subject_id, 'crevafactos-dev', 'planning-pilot-user', 'Active', NOW()),
                (@reviewer_subject_id, 'crevafactos-dev', 'planning-pilot-reviewer', 'Active', NOW())
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO tenant_memberships (
                id, tenant_id, identity_subject_id, status, created_at_utc, authorization_version, membership_reference)
            VALUES
                (@planner_membership_id, @tenant_id, @planner_subject_id, 'Active', NOW(), 1, @planner_reference),
                (@reviewer_membership_id, @tenant_id, @reviewer_subject_id, 'Active', NOW(), 1, @reviewer_reference)
            ON CONFLICT (id) DO NOTHING;
            """,
            connection);

        command.Parameters.AddWithValue("tenant_id", DevelopmentPilotIds.TenantId);
        command.Parameters.AddWithValue("planner_subject_id", DevelopmentPilotIds.IdentitySubjectId);
        command.Parameters.AddWithValue("reviewer_subject_id", DevelopmentPilotIds.ReviewerIdentitySubjectId);
        command.Parameters.AddWithValue("planner_membership_id", DevelopmentPilotIds.TenantMembershipId);
        command.Parameters.AddWithValue("reviewer_membership_id", DevelopmentPilotIds.ReviewerTenantMembershipId);
        command.Parameters.AddWithValue(
            "planner_reference",
            $"mem_{DevelopmentPilotIds.TenantMembershipId:N}".Replace("-", string.Empty, StringComparison.Ordinal));
        command.Parameters.AddWithValue(
            "reviewer_reference",
            $"mem_{DevelopmentPilotIds.ReviewerTenantMembershipId:N}".Replace("-", string.Empty, StringComparison.Ordinal));

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Development pilot migration prerequisites ensured.");
    }
}
