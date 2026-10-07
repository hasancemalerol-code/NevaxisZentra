using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NevaxisZentra.Configuration;
using NevaxisZentra.Configuration.Bootstrap;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Bootstrap;

public interface IFirstAccountEnrollmentService
{
    BootstrapAvailability GetAvailability();

    Task<FirstAccountEnrollmentResult> EnrollAsync(
        FirstAccountEnrollmentRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Persistence seam: a fake in unit tests, PostgreSQL in the product.</summary>
public interface IFirstAccountEnrollmentDatabase
{
    Task<int> CountTenantsAsync(CancellationToken cancellationToken);

    /// <summary>Creates everything atomically; returns AlreadyEnrolled when a tenant exists.</summary>
    Task<FirstAccountEnrollmentResult> CreateAsync(
        FirstAccountEnrollmentInput input,
        CancellationToken cancellationToken);
}

public sealed class FirstAccountEnrollmentService : IFirstAccountEnrollmentService
{
    private readonly BootstrapEnrollmentStore store;
    private readonly IFirstAccountEnrollmentDatabase database;
    private readonly ILogger<FirstAccountEnrollmentService> logger;
    private readonly Action onCompleted;
    private readonly Func<Guid> facilityIdProvider;

    public FirstAccountEnrollmentService(
        BootstrapEnrollmentStore store,
        IFirstAccountEnrollmentDatabase database,
        ILogger<FirstAccountEnrollmentService> logger,
        Action? onCompleted = null,
        Func<Guid>? facilityIdProvider = null)
    {
        this.store = store;
        this.database = database;
        this.logger = logger;
        this.onCompleted = onCompleted ?? (() => { });
        this.facilityIdProvider = facilityIdProvider ?? ResolveFacilityId;
    }

    public BootstrapAvailability GetAvailability() => store.GetAvailability();

    public async Task<FirstAccountEnrollmentResult> EnrollAsync(
        FirstAccountEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Field validation first: a malformed form must not consume or lock the single-use token.
        var errors = FirstAccountEnrollmentValidator.Validate(request);
        if (errors.Count > 0)
        {
            return new FirstAccountEnrollmentResult(FirstAccountEnrollmentStatus.InvalidRequest, errors);
        }

        var entry = store.TryBegin(request.Token ?? string.Empty);
        switch (entry)
        {
            case BootstrapEntryResult.Unavailable:
                return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.Unavailable);
            case BootstrapEntryResult.InvalidToken:
                return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.InvalidToken);
            case BootstrapEntryResult.Expired:
                return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.Expired);
            case BootstrapEntryResult.InProgress:
                return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.InProgress);
        }

        try
        {
            if (await database.CountTenantsAsync(cancellationToken).ConfigureAwait(false) > 0)
            {
                // Enrollment already happened (e.g. committed, then the state write was lost).
                store.Complete("existing-tenant-detected");
                onCompleted();
                return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.AlreadyEnrolled);
            }

            var issuedCount = store.Read()?.IssuedCount ?? 1;
            var input = FirstAccountEnrollmentValidator.Normalize(request, issuedCount, facilityIdProvider());
            var result = await database.CreateAsync(input, cancellationToken).ConfigureAwait(false);

            if (result.Status is FirstAccountEnrollmentStatus.Enrolled or FirstAccountEnrollmentStatus.AlreadyEnrolled)
            {
                store.Complete(result.Status == FirstAccountEnrollmentStatus.Enrolled ? "enrolled" : "existing-tenant-detected");
                onCompleted();
                if (result.Status == FirstAccountEnrollmentStatus.Enrolled)
                {
                    BootstrapLog.EnrollmentCompleted(logger, result.TenantKey);
                }
            }
            else
            {
                store.Abort();
            }

            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The transaction rolled back: nothing was committed and the token stays usable until it expires.
            store.Abort();
            BootstrapLog.EnrollmentFailed(logger, exception.GetType().Name);
            return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.Failed);
        }
    }

    private static Guid ResolveFacilityId()
    {
        var configured = LegacyEnvironmentVariables.GetPrefixed("FACILITY_ID");
        return Guid.TryParse(configured, out var parsed) ? parsed : Guid.NewGuid();
    }
}

internal sealed class DisabledFirstAccountEnrollmentService : IFirstAccountEnrollmentService
{
    public BootstrapAvailability GetAvailability() => new(false, null);

    public Task<FirstAccountEnrollmentResult> EnrollAsync(
        FirstAccountEnrollmentRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.Unavailable));
}

public sealed class BootstrapPrivilegeUnavailableException : InvalidOperationException
{
    public BootstrapPrivilegeUnavailableException()
        : base("First-account enrollment is not provisioned with a bootstrap database credential.")
    {
    }
}

internal sealed class PostgreSqlFirstAccountEnrollmentDatabase(
    IOptions<PersistenceOptions> persistenceOptions,
    PackagedInstallLicenseOptions? licenseOptions = null)
    : IFirstAccountEnrollmentDatabase
{
    private const string RuntimeRole = "crevafactos_runtime";
    private const long EnrollmentAdvisoryLock = 727_274_001;

    public async Task<int> CountTenantsAsync(CancellationToken cancellationToken)
    {
        var context = CreateContext();
        await using var dbContext = context.DbContext;
        return await dbContext.Tenants.IgnoreQueryFilters().CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FirstAccountEnrollmentResult> CreateAsync(
        FirstAccountEnrollmentInput input,
        CancellationToken cancellationToken)
    {
        var context = CreateContext();
        await using var dbContext = context.DbContext;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // Cross-process single-flight: concurrent enrollments serialize here.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({EnrollmentAdvisoryLock})",
            cancellationToken).ConfigureAwait(false);

        if (await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.AlreadyEnrolled);
        }

        var permissionIds = await dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(definition => definition.Status == AuthorizationStatus.Active)
            .Select(definition => definition.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var plan = FirstAccountEnrollmentPlan.Build(
            input,
            permissionIds,
            DateTimeOffset.UtcNow,
            new PasswordHasher<SaasIdentityAccount>(),
            licenseOptions);

        // 1. Structural rows the runtime role is not granted to create: the tenant and its first facility.
        //    These are the only privileged writes.
        dbContext.Tenants.Add(plan.Tenant);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        context.TenantContextAccessor.Set(new TenantContext(plan.Tenant.Id, plan.Tenant.TenantKey, plan.Membership.Id));
        dbContext.Facilities.Add(plan.Facility);

        // The packaged-install commercial catalog is global (no tenant, no RLS, read-only for the runtime
        // role), so it is also a privileged write. Idempotent by fixed identifiers.
        if (!await dbContext.SaasCommercialPlanVersions.AnyAsync(
                version => version.Id == PackagedInstallLicense.PlanVersionId, cancellationToken).ConfigureAwait(false))
        {
            var catalog = PackagedInstallLicense.BuildCatalog(plan.Tenant.CreatedAtUtc);
            if (!await dbContext.SaasCommercialPlans.AnyAsync(
                    existing => existing.Id == PackagedInstallLicense.PlanId, cancellationToken).ConfigureAwait(false))
            {
                dbContext.SaasCommercialPlans.Add(catalog.Plan);
            }

            dbContext.SaasCommercialPlanVersions.Add(catalog.PlanVersion);
            dbContext.SaasCommercialAuditEvents.AddRange(catalog.AuditEvents);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // 2. Everything else (identity, membership, owner role, permission grants, facility membership,
        //    audit) runs as the least-privilege runtime role with the tenant session context, so the
        //    existing grants and RLS policies validate every one of those rows.
        await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL ROLE " + RuntimeRole, cancellationToken).ConfigureAwait(false);

        dbContext.IdentitySubjects.Add(plan.IdentitySubject);
        dbContext.SaasIdentityAccounts.Add(plan.Account);
        dbContext.SaasIdentityPasswordCredentials.Add(plan.Credential);
        dbContext.TenantMemberships.Add(plan.Membership);
        dbContext.TenantRoles.Add(plan.Role);
        dbContext.TenantRolePermissions.AddRange(plan.PermissionAssignments);
        dbContext.TenantMembershipRoles.Add(plan.MembershipRole);
        dbContext.FacilityMemberships.Add(plan.FacilityMembership);
        dbContext.SaasIdentitySecurityAuditEvents.Add(plan.AuditEvent);
        dbContext.SaasTenantSubscriptions.Add(plan.License.Subscription);
        dbContext.SaasTenantSubscriptionTransitionHistory.Add(plan.License.Transition);
        dbContext.SaasSubscriptionAuditEvents.Add(plan.License.AuditEvent);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new FirstAccountEnrollmentResult(
            FirstAccountEnrollmentStatus.Enrolled,
            [],
            plan.Tenant.Id,
            plan.Tenant.TenantKey,
            plan.Account.AccountReference);
    }

    private (NevaxisZentraDbContext DbContext, TenantContextAccessor TenantContextAccessor) CreateContext()
    {
        var runtime = persistenceOptions.Value;
        var username = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", "USERNAME");
        var password = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", "PASSWORD");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new BootstrapPrivilegeUnavailableException();
        }

        var options = new PersistenceOptions
        {
            Enabled = true,
            Host = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", "HOST") ?? runtime.Host,
            Port = int.TryParse(LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", "PORT"), out var port) ? port : runtime.Port,
            Database = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", "DATABASE") ?? runtime.Database,
            Username = username,
            Password = password,
            SslMode = runtime.SslMode,
            ConnectionTimeoutSeconds = runtime.ConnectionTimeoutSeconds,
            CommandTimeoutSeconds = runtime.CommandTimeoutSeconds,
            Pooling = false,
        };

        var tenantContextAccessor = new TenantContextAccessor();
        var builder = new DbContextOptionsBuilder<NevaxisZentraDbContext>();
        builder.UseNpgsql(
            PostgreSqlConnectionFactory.BuildConnectionString(options),
            npgsql =>
            {
                npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName);
                npgsql.MigrationsHistoryTable(NevaxisZentraDbContext.MigrationHistoryTableName);
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
            });
        return (new NevaxisZentraDbContext(builder.Options, tenantContextAccessor), tenantContextAccessor);
    }
}

/// <summary>
/// Startup reconciliation: enrollment that committed but whose state write was lost (crash between the
/// two) is closed from the database, which is authoritative.
/// </summary>
internal sealed class FirstAccountEnrollmentReconciler(
    BootstrapEnrollmentStore store,
    IFirstAccountEnrollmentDatabase database,
    ILogger<FirstAccountEnrollmentReconciler> logger,
    Action onCompleted) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var document = store.Read();
            if (document is null || document.StageValue == BootstrapEnrollmentStage.Completed)
            {
                return;
            }

            if (await database.CountTenantsAsync(cancellationToken).ConfigureAwait(false) > 0)
            {
                store.Complete("reconciled-from-database");
                onCompleted();
                BootstrapLog.Reconciled(logger);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Never block API startup on reconciliation; the Desktop re-checks on its next start.
            BootstrapLog.ReconcileSkipped(logger, exception.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class FirstAccountEnrollmentServiceCollectionExtensions
{
    /// <summary>
    /// Registers the localhost-only first-account enrollment. Without the machine-provisioned
    /// bootstrap directory (development, demo, completed installs) the service reports "unavailable".
    /// </summary>
    public static IServiceCollection AddFirstAccountEnrollment(this IServiceCollection services)
    {
        services.AddSingleton<BootstrapAttemptThrottle>();
        services.AddSingleton(PackagedInstallLicenseOptions.FromEnvironment());
        var directory = Environment.GetEnvironmentVariable(BootstrapEnrollmentStore.DirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(directory))
        {
            services.AddSingleton<IFirstAccountEnrollmentService, DisabledFirstAccountEnrollmentService>();
            return services;
        }

        services.AddSingleton(new BootstrapEnrollmentStore(directory));
        services.AddSingleton<IFirstAccountEnrollmentDatabase, PostgreSqlFirstAccountEnrollmentDatabase>();
        services.AddSingleton<Action>(_ => RevokeBootstrapPrivilege);
        services.AddSingleton<IFirstAccountEnrollmentService>(provider => new FirstAccountEnrollmentService(
            provider.GetRequiredService<BootstrapEnrollmentStore>(),
            provider.GetRequiredService<IFirstAccountEnrollmentDatabase>(),
            provider.GetRequiredService<ILogger<FirstAccountEnrollmentService>>(),
            RevokeBootstrapPrivilege));
        services.AddHostedService(provider => new FirstAccountEnrollmentReconciler(
            provider.GetRequiredService<BootstrapEnrollmentStore>(),
            provider.GetRequiredService<IFirstAccountEnrollmentDatabase>(),
            provider.GetRequiredService<ILogger<FirstAccountEnrollmentReconciler>>(),
            RevokeBootstrapPrivilege));
        return services;
    }

    /// <summary>Once bootstrap is closed this process drops the one-time privileged credential.</summary>
    internal static void RevokeBootstrapPrivilege()
    {
        foreach (var prefix in new[] { "NEVAXIS_ZENTRA", "NEVAXIS_FACTOS", "CREVAFACTOS" })
        {
            Environment.SetEnvironmentVariable($"{prefix}__MIGRATIONS__PASSWORD", null);
        }
    }
}

internal static partial class BootstrapLog
{
    [LoggerMessage(EventId = 7101, Level = LogLevel.Information, Message = "First-account enrollment completed for tenant {TenantKey}.")]
    internal static partial void EnrollmentCompleted(ILogger logger, string? tenantKey);

    [LoggerMessage(EventId = 7102, Level = LogLevel.Error, Message = "First-account enrollment failed ({ExceptionType}); no data was committed.")]
    internal static partial void EnrollmentFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 7103, Level = LogLevel.Information, Message = "First-account enrollment closed from the database state.")]
    internal static partial void Reconciled(ILogger logger);

    [LoggerMessage(EventId = 7104, Level = LogLevel.Warning, Message = "First-account enrollment reconciliation skipped ({ExceptionType}).")]
    internal static partial void ReconcileSkipped(ILogger logger, string exceptionType);
}
