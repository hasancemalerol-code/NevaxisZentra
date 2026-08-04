using Npgsql;

namespace CrevaFactOS.DatabaseTool;

internal static class LocalRuntimeRoleProvisioner
{
    public static async Task ProvisionAsync(NpgsqlConnection connection, string runtimePassword)
    {
        await using (var setPassword = new NpgsqlCommand(
            "SELECT set_config('crevafactos.local_runtime_password', @runtime_password, false)",
            connection))
        {
            setPassword.Parameters.AddWithValue("runtime_password", runtimePassword);
            await setPassword.ExecuteScalarAsync();
        }

        try
        {
            const string alterRoleSql = """
                DO $runtime_role$
                BEGIN
                    EXECUTE format(
                        'ALTER ROLE crevafactos_runtime LOGIN PASSWORD %L',
                        current_setting('crevafactos.local_runtime_password'));
                END
                $runtime_role$;
                """;
            await using var alterRole = new NpgsqlCommand(alterRoleSql, connection);
            await alterRole.ExecuteNonQueryAsync();
        }
        finally
        {
            await using var clearPassword = new NpgsqlCommand(
                "SELECT set_config('crevafactos.local_runtime_password', '', false)",
                connection);
            await clearPassword.ExecuteScalarAsync();
        }
    }
}
