using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Npgsql;
using NevaxisZentra.Configuration.Bootstrap;
using NevaxisZentra.Persistence.Bootstrap;

namespace NevaxisZentra.Persistence.Tests;

internal static class PackagedPostgres
{
    /// <summary>bin\ + template\ of a packaged PostgreSQL (ZENTRA_TEST_PG_ROOT or release\staging\postgres).</summary>
    internal static string? Root { get; } = Resolve();

    private static string? Resolve()
    {
        var candidates = new List<string?> { Environment.GetEnvironmentVariable("ZENTRA_TEST_PG_ROOT") };
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
            {
                candidates.Add(Path.Combine(directory.FullName, "release", "staging", "postgres"));
                break;
            }
        }

        return candidates.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate)
            && File.Exists(Path.Combine(candidate, "bin", "postgres.exe"))
            && File.Exists(Path.Combine(candidate, "template", "PG_VERSION")));
    }
}

public sealed class PackagedPostgresFactAttribute : FactAttribute
{
    public PackagedPostgresFactAttribute()
    {
        if (PackagedPostgres.Root is null)
        {
            Skip = "No packaged PostgreSQL (release\\staging\\postgres or ZENTRA_TEST_PG_ROOT) is available.";
        }
    }
}

[Collection("DataProtectionEnvironment")]
public sealed class RealPostgresEnrollmentTests : IDisposable
{
    private const string StrongPassword = "Str0ng-Passphrase-For-Tests!";
    private static readonly string[] RequiredEmptyDirectories =
    [
        "pg_commit_ts", "pg_dynshmem", "pg_notify", "pg_replslot", "pg_serial", "pg_snapshots", "pg_stat_tmp",
        "pg_tblspc", "pg_twophase", "pg_logical/mappings", "pg_logical/snapshots", "pg_wal/archive_status", "pg_wal/summaries",
    ];

    private static readonly string[] EnvironmentKeys =
    [
        "NEVAXIS_FACTOS__MIGRATIONS__HOST", "NEVAXIS_FACTOS__MIGRATIONS__PORT", "NEVAXIS_FACTOS__MIGRATIONS__DATABASE",
        "NEVAXIS_FACTOS__MIGRATIONS__USERNAME", "NEVAXIS_FACTOS__MIGRATIONS__PASSWORD",
        "NEVAXIS_ZENTRA__MIGRATIONS__HOST", "NEVAXIS_ZENTRA__MIGRATIONS__PORT", "NEVAXIS_ZENTRA__MIGRATIONS__DATABASE",
        "NEVAXIS_ZENTRA__MIGRATIONS__USERNAME", "NEVAXIS_ZENTRA__MIGRATIONS__PASSWORD",
    ];

    private readonly string root = Path.Combine(Path.GetTempPath(), $"zentra-enroll-pg-{Guid.NewGuid():N}");
    private readonly Dictionary<string, string?> previousEnvironment = EnvironmentKeys
        .ToDictionary(key => key, Environment.GetEnvironmentVariable);
    private string? dataDirectory;

    public void Dispose()
    {
        foreach (var (key, value) in previousEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        if (dataDirectory is not null && PackagedPostgres.Root is not null)
        {
            Run(Path.Combine(PackagedPostgres.Root, "bin", "pg_ctl.exe"), ["stop", "-D", dataDirectory, "-m", "fast"], null);
        }

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [PackagedPostgresFact]
    public async Task EnrollmentCreatesTheFirstOwnerUnderRlsAndClosesBootstrapPermanently()
    {
        var pg = PackagedPostgres.Root!;
        var bin = Path.Combine(pg, "bin");
        dataDirectory = Path.Combine(root, "data");
        CopyDirectory(Path.Combine(pg, "template"), dataDirectory);
        foreach (var relative in RequiredEmptyDirectories)
        {
            Directory.CreateDirectory(Path.Combine(dataDirectory, relative));
        }

        var migrationPassword = NewSecret();
        var runtimePassword = NewSecret();
        Assert.Equal(0, Run(
            Path.Combine(bin, "postgres.exe"),
            ["--single", "-D", dataDirectory, "-c", "password_encryption=scram-sha-256", "postgres"],
            $"ALTER ROLE \"nevaxiszentra\" WITH LOGIN PASSWORD '{migrationPassword}'\nALTER ROLE \"crevafactos_runtime\" WITH LOGIN PASSWORD '{runtimePassword}'\n"));

        var port = FreePort();
        Assert.Equal(0, Run(
            Path.Combine(bin, "pg_ctl.exe"),
            ["start", "-D", dataDirectory, "-l", Path.Combine(root, "pg.log"), "-w", "-o", $"-p {port}"],
            null));

        Environment.SetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__HOST", "127.0.0.1");
        Environment.SetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__PORT", port.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__DATABASE", "crevafactos_local");
        Environment.SetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__USERNAME", "nevaxiszentra");
        Environment.SetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__PASSWORD", migrationPassword);
        Environment.SetEnvironmentVariable("NEVAXIS_ZENTRA__MIGRATIONS__PASSWORD", null);

        var store = new BootstrapEnrollmentStore(Path.Combine(root, "machine-config"));
        var preparation = store.Prepare(existingTenantCount: 0);
        Assert.True(preparation.TokenIssued);
        var token = File.ReadAllLines(store.TokenFilePath).Last(line => !string.IsNullOrWhiteSpace(line)).Trim();

        var database = new PostgreSqlFirstAccountEnrollmentDatabase(Options.Create(new PersistenceOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = port,
            Database = "crevafactos_local",
            Username = "crevafactos_runtime",
            Password = runtimePassword,
            SslMode = "Disable",
        }));
        var logger = new CapturingLogger<FirstAccountEnrollmentService>();
        var revocations = 0;
        var service = new FirstAccountEnrollmentService(store, database, logger, () => revocations++, () => Guid.NewGuid());

        Assert.Equal(0, await database.CountTenantsAsync(CancellationToken.None));
        var result = await service.EnrollAsync(
            new FirstAccountEnrollmentRequest(token, "acme-fab", "Acme Fabrika", "owner@acme.example", "Ada Owner", StrongPassword),
            CancellationToken.None);

        Assert.Equal(FirstAccountEnrollmentStatus.Enrolled, result.Status);
        Assert.Equal(1, revocations);

        // Everything was committed in one transaction.
        await using var admin = new NpgsqlConnection(Connection(port, "nevaxiszentra", migrationPassword));
        await admin.OpenAsync();
        Assert.Equal(1L, await Scalar(admin, "select count(*) from tenants where tenant_key = 'acme-fab'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_identity_accounts where lifecycle_status = 'Active'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_identity_password_credentials"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from tenant_memberships where status = 'Active'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from tenant_roles where key = 'tenant-owner'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from tenant_membership_roles"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from facilities"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from facility_memberships"));
        Assert.Equal(
            await Scalar(admin, "select count(*) from permission_definitions where status = 'Active'"),
            await Scalar(admin, "select count(*) from tenant_role_permissions"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_identity_security_audit_events where event_type = 'bootstrap.enrollment.completed'"));

        // The first tenant is licensed: without a current subscription every entitlement-gated module is
        // refused with 403 subscription-not-found. One plan + published version (global catalog) and one
        // active, current subscription for the new tenant, with history and audit.
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_commercial_plans where plan_code_normalized = 'zentra-packaged-install'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_commercial_plan_versions where publication_status = 'published' and provenance like 'packaged-install%'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_tenant_subscriptions s join tenants t on t.id = s.tenant_id where t.tenant_key = 'acme-fab' and s.subscription_status = 'active' and s.is_current and s.current_period_end_utc > now() + interval '9 years'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_tenant_subscription_transition_history where to_status = 'active'"));
        Assert.Equal(1L, await Scalar(admin, "select count(*) from saas_subscription_audit_events where event_type = 'saas-subscription.subscription.assigned'"));
        Assert.Equal(0L, await Scalar(admin, "select count(*) from saas_commercial_entitlement_definitions"));
        Assert.Equal(0L, await Scalar(admin, "select count(*) from saas_identity_security_audit_events where payload_json like '%subscription%' and event_type <> 'bootstrap.enrollment.completed'"));

        // The password is stored hashed; audit evidence carries no secret or personal data.
        await using (var hashCommand = new NpgsqlCommand("select password_hash from saas_identity_password_credentials", admin))
        {
            var hash = (string)(await hashCommand.ExecuteScalarAsync())!;
            Assert.DoesNotContain(StrongPassword, hash, StringComparison.Ordinal);
        }

        await using (var auditCommand = new NpgsqlCommand("select payload_json from saas_identity_security_audit_events where event_type = 'bootstrap.enrollment.completed'", admin))
        {
            var payload = (string)(await auditCommand.ExecuteScalarAsync())!;
            Assert.DoesNotContain(StrongPassword, payload, StringComparison.Ordinal);
            Assert.DoesNotContain(token, payload, StringComparison.Ordinal);
            Assert.DoesNotContain("owner@acme.example", payload, StringComparison.Ordinal);
        }

        // RLS: the runtime role sees the new tenant's rows only inside that tenant's session context.
        var tenantId = (Guid)(await new NpgsqlCommand("select id from tenants where tenant_key = 'acme-fab'", admin).ExecuteScalarAsync())!;
        await using var runtime = new NpgsqlConnection(Connection(port, "crevafactos_runtime", runtimePassword));
        await runtime.OpenAsync();
        Assert.Equal(0L, await Scalar(runtime, "select count(*) from tenant_roles"));
        Assert.Equal(0L, await Scalar(runtime, "select count(*) from facilities"));
        await using (var transaction = await runtime.BeginTransactionAsync())
        {
            await using (var setContext = new NpgsqlCommand($"select set_config('NevaxisZentra.tenant_id', '{tenantId}', true)", runtime, transaction))
            {
                await setContext.ExecuteScalarAsync();
            }

            Assert.Equal(1L, await Scalar(runtime, "select count(*) from tenant_roles", transaction));
            Assert.Equal(1L, await Scalar(runtime, "select count(*) from facilities", transaction));
            // The subscription is tenant-scoped and visible to the runtime role in that context (the paid-access gate reads it).
            Assert.Equal(1L, await Scalar(runtime, "select count(*) from saas_tenant_subscriptions where is_current and subscription_status = 'active'", transaction));
        }

        Assert.Equal(0L, await Scalar(runtime, "select count(*) from saas_tenant_subscriptions"));

        // The template grants the runtime role what MFA enrollment needs (it creates and replaces these rows).
        const string privilegeCheck = """
            select count(*) from (select 1
              where has_table_privilege('crevafactos_runtime', 'public.saas_identity_mfa_methods', 'INSERT')
                and has_table_privilege('crevafactos_runtime', 'public.saas_identity_mfa_methods', 'DELETE')
                and has_table_privilege('crevafactos_runtime', 'public.saas_identity_recovery_codes', 'INSERT')
                and has_table_privilege('crevafactos_runtime', 'public.saas_identity_recovery_codes', 'DELETE')
                and has_table_privilege('crevafactos_runtime', 'public."__EFMigrationsHistory"', 'SELECT')) granted
            """;
        Assert.Equal(1L, await Scalar(admin, privilegeCheck));
        // Bootstrap is closed for good: the same (now consumed) token and any other attempt are refused.
        var second = await service.EnrollAsync(
            new FirstAccountEnrollmentRequest(token, "other-org", "Other", "x@y.example", "X", StrongPassword),
            CancellationToken.None);
        Assert.Equal(FirstAccountEnrollmentStatus.Unavailable, second.Status);
        Assert.Equal(
            FirstAccountEnrollmentStatus.AlreadyEnrolled,
            (await database.CreateAsync(
                FirstAccountEnrollmentValidator.Normalize(
                    new FirstAccountEnrollmentRequest("t", "third-org", "Third", "z@y.example", "Z", StrongPassword), 1, Guid.NewGuid()),
                CancellationToken.None)).Status);
        Assert.Equal(1L, await Scalar(admin, "select count(*) from tenants"));
        Assert.False(File.Exists(store.TokenFilePath));
        Assert.Equal(BootstrapEnrollmentStage.Completed, store.Read()!.StageValue);

        // A restart decision with the tenant present never reissues a token.
        Assert.Equal(BootstrapEnrollmentStage.Completed, new BootstrapEnrollmentStore(Path.Combine(root, "machine-config")).Prepare(1).Stage);
        Assert.False(File.Exists(store.TokenFilePath));
    }


    [PackagedPostgresFact]
    public async Task PackagedSchemaContainsEveryColumnTheEntityModelQueries()
    {
        var pg = PackagedPostgres.Root!;
        var bin = Path.Combine(pg, "bin");
        dataDirectory = Path.Combine(root, "data");
        CopyDirectory(Path.Combine(pg, "template"), dataDirectory);
        foreach (var relative in RequiredEmptyDirectories)
        {
            Directory.CreateDirectory(Path.Combine(dataDirectory, relative));
        }

        var migrationPassword = NewSecret();
        Assert.Equal(0, Run(
            Path.Combine(bin, "postgres.exe"),
            ["--single", "-D", dataDirectory, "-c", "password_encryption=scram-sha-256", "postgres"],
            $"ALTER ROLE \"nevaxiszentra\" WITH LOGIN PASSWORD '{migrationPassword}'\n"));
        var port = FreePort();
        Assert.Equal(0, Run(
            Path.Combine(bin, "pg_ctl.exe"),
            ["start", "-D", dataDirectory, "-l", Path.Combine(root, "pg.log"), "-w", "-o", $"-p {port}"],
            null));

        // Actual columns of every table in the packaged schema.
        var actual = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        await using (var admin = new NpgsqlConnection(Connection(port, "nevaxiszentra", migrationPassword)))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand(
                "select table_name, column_name from information_schema.columns where table_schema = 'public'", admin);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                if (!actual.TryGetValue(table, out var columns))
                {
                    actual[table] = columns = new HashSet<string>(StringComparer.Ordinal);
                }

                columns.Add(reader.GetString(1));
            }
        }

        // Columns the EF model will put in its SQL. A name the table does not have fails at query time
        // ("column f.Id does not exist"), which surfaces to users as an unavailable module.
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only", npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        using var context = new NevaxisZentraDbContext(options);
        var missing = new List<string>();
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null || entityType.GetViewName() is not null || !actual.TryGetValue(table, out var columns))
            {
                continue;
            }

            var store = StoreObjectIdentifier.Table(table, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var column = property.GetColumnName(store);
                if (column is not null && !columns.Contains(column))
                {
                    missing.Add($"{table}.{column}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Model columns missing from the packaged schema: " + string.Join(", ", missing.Distinct().Take(40)));
    }

    private static string Connection(int port, string user, string password) =>
        $"Host=127.0.0.1;Port={port};Database=crevafactos_local;Username={user};Password={password};SSL Mode=Disable;Pooling=false";

    private static async Task<long> Scalar(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static string NewSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }

    private static int Run(string fileName, IEnumerable<string> arguments, string? stdin)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = stdin is not null,
            RedirectStandardOutput = stdin is not null,
            RedirectStandardError = stdin is not null,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        if (stdin is not null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            output.GetAwaiter().GetResult();
            error.GetAwaiter().GetResult();
            return process.ExitCode;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
