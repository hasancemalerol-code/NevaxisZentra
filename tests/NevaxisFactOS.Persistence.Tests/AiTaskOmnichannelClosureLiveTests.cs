using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.AiTasks.Delivery;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class AiTaskOmnichannelClosureLiveTests
{
    private static readonly Guid TenantAId = Guid.Parse("bb000001-0001-4000-8000-000000000001");
    private static readonly Guid FacilityAId = Guid.Parse("bb000001-0005-4000-8000-000000000005");
    private static readonly Guid TaskAId = Guid.Parse("bb000001-03d0-4000-8000-0000000003d0");
    private static readonly Guid MembershipAId = Guid.Parse("bb000001-0003-4000-8000-000000000003");
    private static readonly Guid SubjectAId = Guid.Parse("bb000001-0002-4000-8000-000000000002");

    [Fact]
    public async Task DualWorkerConcurrencyOnlyOneClaimsDelivery()
    {
        if (!IsLiveEnabled())
        {
            return;
        }

        var idempotencyKey = "closure-dual-worker-001";
        await CleanupDeliveryAsync(idempotencyKey);

        var deliveryId = await EnqueueQueuedDeliveryAsync(
            idempotencyKey,
            maxAttempts: 5,
            projectionJson: null);

        await using (var verifyScope = CreateWorkerScope())
        {
            var session = verifyScope.Scope.ServiceProvider.GetRequiredService<TenantDatabaseSession>();
            var before = await session.ExecuteReadAsync(async ct =>
            {
                var db = verifyScope.Scope.ServiceProvider.GetRequiredService<NevaxisFactOSDbContext>();
                return await db.AiTaskDeliveries.AsNoTracking()
                    .FirstAsync(d => d.Id == deliveryId, ct);
            });
            Assert.Equal(AiTaskDeliveryStatuses.Queued, before.Status);
        }

        var workerA = CreateWorkerScope();
        var workerB = CreateWorkerScope();

        var taskA = Task.Run(() => workerA.Worker.ProcessBatchAsync("closure-worker-a", 1));
        var taskB = Task.Run(() => workerB.Worker.ProcessBatchAsync("closure-worker-b", 1));
        await Task.WhenAll(taskA, taskB);

        var claimedA = await taskA;
        var claimedB = await taskB;
        Assert.Equal(1, claimedA + claimedB);

        await using (var afterScope = CreateWorkerScope())
        {
            var session = afterScope.Scope.ServiceProvider.GetRequiredService<TenantDatabaseSession>();
            var (delivery, attempts) = await session.ExecuteReadAsync(async ct =>
            {
                var db = afterScope.Scope.ServiceProvider.GetRequiredService<NevaxisFactOSDbContext>();
                var deliveryRow = await db.AiTaskDeliveries
                    .AsNoTracking()
                    .FirstAsync(d => d.Id == deliveryId, ct);
                var attemptRows = await db.AiTaskDeliveryAttempts
                    .AsNoTracking()
                    .Where(a => a.DeliveryId == deliveryId)
                    .ToListAsync(ct);
                return (deliveryRow, attemptRows);
            });

            Assert.Equal(AiTaskDeliveryStatuses.Sent, delivery.Status);
            Assert.Single(attempts);
            Assert.Equal(1, delivery.AttemptCount);
        }

        await workerA.DisposeAsync();
        await workerB.DisposeAsync();
    }

    [Fact]
    public async Task LeaseRecoveryReclaimsExpiredProcessingDelivery()
    {
        if (!IsLiveEnabled())
        {
            return;
        }

        var idempotencyKey = "closure-lease-recovery-001";
        await CleanupDeliveryAsync(idempotencyKey);

        var deliveryId = await EnqueueQueuedDeliveryAsync(
            idempotencyKey,
            maxAttempts: 5,
            projectionJson: null);

        await using (var leaseScope = CreateWorkerScope())
        {
            var session = leaseScope.Scope.ServiceProvider.GetRequiredService<TenantDatabaseSession>();
            await session.ExecuteWriteAsync(async ct =>
            {
                var db = leaseScope.Scope.ServiceProvider.GetRequiredService<NevaxisFactOSDbContext>();
                var delivery = await db.AiTaskDeliveries.FirstAsync(d => d.Id == deliveryId, ct);
                delivery.Status = AiTaskDeliveryStatuses.Processing;
                delivery.LeaseOwner = "dead-worker-sim";
                delivery.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
                delivery.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                return true;
            });
        }

        await using var scope = CreateWorkerScope();
        var processed = await scope.Worker.ProcessBatchAsync("closure-lease-recovery", 1);
        Assert.Equal(1, processed);

        var verifySession = scope.Scope.ServiceProvider.GetRequiredService<TenantDatabaseSession>();
        var (finalDelivery, attempts) = await verifySession.ExecuteReadAsync(async ct =>
        {
            var db = scope.Scope.ServiceProvider.GetRequiredService<NevaxisFactOSDbContext>();
            var deliveryRow = await db.AiTaskDeliveries.AsNoTracking().FirstAsync(d => d.Id == deliveryId, ct);
            var attemptRows = await db.AiTaskDeliveryAttempts.AsNoTracking()
                .Where(a => a.DeliveryId == deliveryId)
                .ToListAsync(ct);
            return (deliveryRow, attemptRows);
        });

        Assert.Equal(AiTaskDeliveryStatuses.Sent, finalDelivery.Status);
        Assert.Single(attempts);
        await scope.DisposeAsync();
    }

    private static bool IsLiveEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_CLOSURE_LIVE"), "1", StringComparison.Ordinal);

    private static async Task CleanupDeliveryAsync(string idempotencyKey)
    {
        var persistence = ReadPersistenceOptions();
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(new PersistenceOptions
            {
                Enabled = true,
                Host = persistence.Host,
                Port = persistence.Port,
                Database = persistence.Database,
                Username = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__USERNAME") ?? "crevafactos",
                Password = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__MIGRATIONS__PASSWORD")
                    ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_POSTGRES_PASSWORD")
                    ?? "local_dev_only_change_me",
                SslMode = "Disable",
                Pooling = false,
            }));
        await connection.OpenAsync();
        await using var deleteAttempts = new NpgsqlCommand(
            """
            DELETE FROM ai_task_delivery_attempts
            WHERE delivery_id IN (
                SELECT id FROM ai_task_deliveries
                WHERE tenant_id = @tenant_id AND idempotency_key = @idempotency_key);
            """,
            connection);
        deleteAttempts.Parameters.AddWithValue("tenant_id", TenantAId);
        deleteAttempts.Parameters.AddWithValue("idempotency_key", idempotencyKey);
        await deleteAttempts.ExecuteNonQueryAsync();

        await using var deleteDeliveries = new NpgsqlCommand(
            """
            DELETE FROM ai_task_deliveries
            WHERE tenant_id = @tenant_id AND idempotency_key = @idempotency_key;
            """,
            connection);
        deleteDeliveries.Parameters.AddWithValue("tenant_id", TenantAId);
        deleteDeliveries.Parameters.AddWithValue("idempotency_key", idempotencyKey);
        await deleteDeliveries.ExecuteNonQueryAsync();
    }

    private static ServiceProvider BuildRootProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> EnqueueQueuedDeliveryAsync(
        string idempotencyKey,
        int maxAttempts,
        string? projectionJson)
    {
        var root = BuildRootProvider();
        using var scope = root.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
        accessor.Set(new TenantContext(TenantAId, "omnichannel-closure-tenant-a", MembershipAId));
        var repository = scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryRepository>();
        var context = CreateOperationContext();
        var result = await repository.EnqueueAsync(
            context,
            new EnqueueAiTaskDeliveryCommand(
                TaskAId,
                AiTaskDeliveryChannels.DevelopmentLoopback,
                AiTaskDeliveryRecipientKinds.User,
                SubjectAId,
                null,
                idempotencyKey,
                projectionJson,
                null,
                maxAttempts));

        Assert.True(result.Success, result.FailureMessage);
        Assert.NotNull(result.Delivery);
        return result.Delivery!.Id;
    }

    private static AiTaskOperationContext CreateOperationContext() =>
        new(
            TenantAId,
            FacilityAId,
            MembershipAId,
            SubjectAId,
            AiTaskPermissionKeys.DeliveryManage,
            Guid.NewGuid());

    private static NevaxisFactOSDbContext CreateDbContext(TenantContextAccessor accessor)
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(ReadPersistenceOptions()),
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options, accessor);
    }

    private static TenantContextAccessor CreateTenantAccessor()
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(TenantAId, "omnichannel-closure-tenant-a", MembershipAId));
        return accessor;
    }

    private static WorkerScope CreateWorkerScope()
    {
        var root = BuildRootProvider();
        var scope = root.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
        accessor.Set(new TenantContext(TenantAId, "omnichannel-closure-tenant-a", MembershipAId));
        return new WorkerScope(root, scope, scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryWorker>());
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var persistence = ReadPersistenceOptions();
        var delivery = new AiTaskDeliveryOptions
        {
            LeaseSeconds = ReadIntEnv("NEVAXIS_FACTOS__AI_TASK_DELIVERY__LEASE_SECONDS", 5),
            RetryBaseDelaySeconds = ReadIntEnv("NEVAXIS_FACTOS__AI_TASK_DELIVERY__RETRY_BASE_DELAY_SECONDS", 1),
            EnableClosureTtlPolicies = true,
            WorkerEnabled = true,
        };

        services.AddSingleton(Options.Create(persistence));
        services.AddSingleton(Options.Create(delivery));
        services.AddSingleton<TenantContextAccessor>();
        services.AddSingleton<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        services.AddSingleton<IHostEnvironment>(new TestingHostEnvironment());
        services.AddDbContext<NevaxisFactOSDbContext>((sp, options) =>
            options.UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(persistence),
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName)));
        services.AddScoped<TenantDatabaseSession>();
        services.AddScoped<IAiTaskDeliveryChannelAdapter, DevelopmentLoopbackAiTaskDeliveryChannelAdapter>();
        services.AddScoped<IAiTaskDeliveryChannelRegistry, AiTaskDeliveryChannelRegistry>();
        services.AddScoped<IAiTaskDeliveryRepository, AiTaskDeliveryRepository>();
        services.AddScoped<IAiTaskDeliveryWorker, AiTaskDeliveryWorker>();
    }

    private static PersistenceOptions ReadPersistenceOptions() => new()
    {
        Enabled = true,
        Host = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__HOST") ?? "127.0.0.1",
        Port = ReadIntEnv("NEVAXIS_FACTOS__PERSISTENCE__PORT", 5433),
        Database = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__DATABASE")
            ?? "NEVAXIS_FACTOS_omnichannel_closure",
        Username = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__USERNAME") ?? "crevafactos_runtime",
        Password = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__PERSISTENCE__PASSWORD") ?? "local_runtime_only_change_me",
        SslMode = "Disable",
        Pooling = false,
    };

    private static int ReadIntEnv(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;

    private sealed class WorkerScope : IAsyncDisposable
    {
        private readonly ServiceProvider root;
        private readonly IServiceScope scope;

        public WorkerScope(ServiceProvider rootProvider, IServiceScope serviceScope, IAiTaskDeliveryWorker worker)
        {
            root = rootProvider;
            scope = serviceScope;
            Worker = worker;
        }

        public IServiceScope Scope => scope;

        public IAiTaskDeliveryWorker Worker { get; }

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            root.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestingHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "omnichannel-closure-live-tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
