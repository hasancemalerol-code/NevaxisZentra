using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.AiTasks.Delivery;
using NevaxisZentra.Persistence.AiTasks.Delivery.Email;
using NevaxisZentra.Persistence.AiTasks.Delivery.WhatsApp;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskDeliveryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var deliveries = endpoints.MapGroup("/api/ai-task-deliveries").RequireAuthorization();
        deliveries.MapGet("/{deliveryId:guid}", GetDeliveryAsync);
        deliveries.MapPost("/{deliveryId:guid}/retry", RetryDeliveryAsync);
        deliveries.MapPost("/{deliveryId:guid}/cancel", CancelDeliveryAsync);

        var tasks = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();
        tasks.MapPost("/{taskId:guid}/deliveries", EnqueueDeliveryAsync);
        tasks.MapGet("/{taskId:guid}/deliveries", ListTaskDeliveriesAsync);

        var channels = endpoints.MapGroup("/api/ai-task-delivery").RequireAuthorization();
        channels.MapGet("/channels", ListChannelsAsync);
        channels.MapGet("/health", GetWorkerHealthAsync);

        var management = endpoints.MapGroup("/api/ai-task-deliveries-management").RequireAuthorization();
        management.MapGet("/", ListDeliveriesAsync);

        return endpoints;
    }

    private static async Task<IResult> EnqueueDeliveryAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] EnqueueDeliveryRequest request,
        HttpContext httpContext,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryManage,
            cancellationToken);
        if (context is null) return failResult!;

        var idempotencyKey = httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var header)
            && !string.IsNullOrWhiteSpace(header.ToString())
            ? header.ToString()
            : request.IdempotencyKey;

        var result = await repository.EnqueueAsync(
            context,
            new EnqueueAiTaskDeliveryCommand(
                taskId,
                request.Channel,
                request.RecipientKind,
                request.RecipientUserId,
                request.MaskedRecipient,
                idempotencyKey ?? Guid.NewGuid().ToString("N"),
                request.DeliveryProjectionJson,
                request.SecureAccessGrantId,
                request.MaxAttempts,
                request.RecipientE164,
                request.ConsentStatus,
                request.SecureTaskUrl,
                request.RecipientEmail,
                request.RecipientEligibility),
            cancellationToken);

        return result.Success
            ? TypedResults.Ok(result.Delivery)
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
    }

    private static async Task<IResult> ListTaskDeliveriesAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryRead,
            cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListForTaskAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(new { items });
    }

    private static async Task<IResult> GetDeliveryAsync(
        Guid deliveryId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryRead,
            cancellationToken);
        if (context is null) return failResult!;

        var delivery = await repository.GetByIdAsync(context, deliveryId, cancellationToken);
        return delivery is null
            ? AiTaskEndpointHelpers.MapFailure(AiTaskDeliveryFailureCodes.NotFound, "Delivery not found.")
            : TypedResults.Ok(delivery);
    }

    private static async Task<IResult> RetryDeliveryAsync(
        Guid deliveryId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryManage,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RetryAsync(context, deliveryId, cancellationToken);
        return result.Success
            ? TypedResults.Ok(result.Delivery)
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
    }

    private static async Task<IResult> CancelDeliveryAsync(
        Guid deliveryId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryManage,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CancelAsync(context, deliveryId, cancellationToken);
        return result.Success
            ? TypedResults.Ok(result.Delivery)
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
    }

    private static async Task<IResult> ListChannelsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryChannelRegistry registry,
        CorrelationContext correlationContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryRead,
            cancellationToken);
        if (context is null) return failResult!;

        var includeDev = environment.IsDevelopment() || environment.EnvironmentName == "Testing";
        return TypedResults.Ok(new { channels = registry.ListReadiness(includeDev) });
    }

    private static async Task<IResult> GetWorkerHealthAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDeliveryRepository repository,
        CorrelationContext correlationContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryRead,
            cancellationToken);
        if (context is null) return failResult!;

        var includeDev = environment.IsDevelopment() || environment.EnvironmentName == "Testing";
        var health = await repository.GetWorkerHealthAsync(context, includeDev, cancellationToken);
        return TypedResults.Ok(health);
    }

    private static async Task<IResult> ListDeliveriesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] Guid? taskId,
        [FromQuery] Guid? recipientUserId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IAiTaskDeliveryRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.DeliveryRead,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListAsync(
            context,
            new AiTaskDeliveryListFilters(channel, status, taskId, recipientUserId, fromUtc, toUtc, page, pageSize),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    public static IServiceCollection AddNevaxisZentraAiTaskDelivery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AiTaskDeliveryOptions>()
            .Bind(configuration.GetSection(AiTaskDeliveryOptions.SectionName))
            .PostConfigure(ApplyAiTaskDeliveryEnvironmentOverrides);
        services.AddScoped<AiTaskSecureAccessTokenService>();
        services.AddOptions<WhatsAppBusinessDeliveryOptions>()
            .Bind(configuration.GetSection(WhatsAppBusinessDeliveryOptions.SectionName))
            .PostConfigure(ApplyWhatsAppEnvironmentOverrides);
        services.AddHttpClient(nameof(MetaWhatsAppCloudApiClient));
        services.AddScoped<WhatsAppSecretProtector>();
        services.AddScoped<IWhatsAppChannelConfigurationResolver, WhatsAppChannelConfigurationResolver>();
        services.AddScoped<IMetaWhatsAppCloudApiClient, MetaWhatsAppCloudApiClient>();
        services.AddScoped<IWhatsAppWebhookProcessor, WhatsAppWebhookProcessor>();
        services.AddOptions<EmailDeliveryOptions>()
            .Bind(configuration.GetSection(EmailDeliveryOptions.SectionName))
            .PostConfigure(ApplyEmailEnvironmentOverrides);
        services.AddScoped<EmailSecretProtector>();
        services.AddScoped<IEmailChannelConfigurationResolver, EmailChannelConfigurationResolver>();
        services.AddScoped<IEmailTransport, SmtpEmailTransport>();
        services.AddScoped<IAiTaskDeliveryChannelAdapter, EmailDeliveryAdapter>();
        services.AddScoped<IAiTaskDeliveryChannelAdapter, WhatsAppBusinessDeliveryAdapter>();
        services.AddScoped<IAiTaskDeliveryChannelAdapter, NotConfiguredAiTaskDeliveryChannelAdapter>(_
            => new NotConfiguredAiTaskDeliveryChannelAdapter(AiTaskDeliveryChannels.NevaxisZentraTeams, "NevaxisZentra Teams"));
        services.AddScoped<IAiTaskDeliveryChannelAdapter, NotConfiguredAiTaskDeliveryChannelAdapter>(_
            => new NotConfiguredAiTaskDeliveryChannelAdapter(AiTaskDeliveryChannels.MicrosoftTeams, "Microsoft Teams"));
        services.AddScoped<IAiTaskDeliveryChannelAdapter, NotConfiguredAiTaskDeliveryChannelAdapter>(_
            => new NotConfiguredAiTaskDeliveryChannelAdapter(AiTaskDeliveryChannels.Sms, "SMS"));
        services.AddScoped<IAiTaskDeliveryChannelAdapter, DevelopmentLoopbackAiTaskDeliveryChannelAdapter>();
        services.AddScoped<IAiTaskDeliveryChannelRegistry, AiTaskDeliveryChannelRegistry>();

        services.AddScoped<IAiTaskDeliveryRepository>(sp =>
        {
            var db = sp.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
            return db is null
                ? new UnavailableAiTaskDeliveryRepository()
                : new AiTaskDeliveryRepository(
                    db,
                    sp.GetRequiredService<TenantDatabaseSession>(),
                    sp.GetRequiredService<IAiTaskDeliveryChannelRegistry>(),
                    sp.GetRequiredService<IHostEnvironment>(),
                    sp.GetRequiredService<WhatsAppSecretProtector>(),
                    sp.GetRequiredService<EmailSecretProtector>());
        });

        services.AddScoped<IAiTaskSecureAccessRepository>(sp =>
        {
            var db = sp.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
            return db is null
                ? new UnavailableAiTaskSecureAccessRepository()
                : new AiTaskSecureAccessRepository(
                    db,
                    sp.GetRequiredService<TenantDatabaseSession>(),
                    sp.GetRequiredService<AiTaskSecureAccessTokenService>(),
                    sp.GetRequiredService<TenantContextAccessor>(),
                    sp.GetRequiredService<IOptions<AiTaskDeliveryOptions>>());
        });

        services.AddScoped<IAiTaskDeliveryWorker>(sp =>
        {
            var db = sp.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
            return db is null
                ? new UnavailableAiTaskDeliveryWorker()
                : new AiTaskDeliveryWorker(
                    db,
                    sp.GetRequiredService<TenantDatabaseSession>(),
                    sp.GetRequiredService<IAiTaskDeliveryChannelRegistry>(),
                    sp.GetRequiredService<ITenantContextAccessor>(),
                    sp.GetRequiredService<IOptions<AiTaskDeliveryOptions>>());
        });

        services.AddScoped<IAiTaskDeliveryWorkTenantDiscoverer, PostgreSqlAiTaskDeliveryWorkTenantDiscoverer>();

        services.AddHostedService<AiTaskDeliveryBackgroundService>();
        return services;
    }

    private static void ApplyEmailEnvironmentOverrides(EmailDeliveryOptions options)
    {
        options.Enabled = GetEmailEnvBoolean("ENABLED", options.Enabled);
        options.SmtpHost = GetEmailEnvValue("SMTP__HOST", options.SmtpHost);
        options.SmtpPort = GetEmailEnvInt32("SMTP__PORT", options.SmtpPort);
        options.SecurityMode = GetEmailEnvValue("SMTP__SECURITY_MODE", options.SecurityMode) ?? options.SecurityMode;
        options.Username = GetEmailEnvValue("SMTP__USERNAME", options.Username);
        options.Password = GetEmailEnvValue("SMTP__PASSWORD", options.Password);
        options.FromAddress = GetEmailEnvValue("SMTP__FROM_ADDRESS", options.FromAddress);
        options.FromDisplayName = GetEmailEnvValue("SMTP__FROM_DISPLAY_NAME", options.FromDisplayName);
        options.ReplyToAddress = GetEmailEnvValue("SMTP__REPLY_TO_ADDRESS", options.ReplyToAddress);
        options.TestSmtpHost = GetEmailEnvValue("TEST_SMTP__HOST", options.TestSmtpHost);
        options.TimeoutSeconds = GetEmailEnvInt32("TIMEOUT_SECONDS", options.TimeoutSeconds);
        var testPort = Environment.GetEnvironmentVariable("NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__PORT");
        if (!string.IsNullOrWhiteSpace(testPort))
        {
            options.TestSmtpPort = int.Parse(testPort, provider: null);
        }
    }

    private static string? GetEmailEnvValue(string suffix, string? fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__{suffix}") ?? fallback;

    private static int GetEmailEnvInt32(string suffix, int fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

    private static bool GetEmailEnvBoolean(string suffix, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }

    private static void ApplyWhatsAppEnvironmentOverrides(WhatsAppBusinessDeliveryOptions options)
    {
        options.Enabled = GetWhatsAppEnvBoolean("ENABLED", options.Enabled);
        options.PhoneNumberId = GetWhatsAppEnvValue("PHONE_NUMBER_ID", options.PhoneNumberId);
        options.BusinessAccountId = GetWhatsAppEnvValue("BUSINESS_ACCOUNT_ID", options.BusinessAccountId);
        options.AccessToken = GetWhatsAppEnvValue("ACCESS_TOKEN", options.AccessToken);
        options.AppSecret = GetWhatsAppEnvValue("APP_SECRET", options.AppSecret);
        options.WebhookVerifyToken = GetWhatsAppEnvValue("WEBHOOK_VERIFY_TOKEN", options.WebhookVerifyToken);
        options.DefaultTemplateName = GetWhatsAppEnvValue("DEFAULT_TEMPLATE_NAME", options.DefaultTemplateName);
        options.DefaultTemplateLanguageCode = GetWhatsAppEnvValue("DEFAULT_TEMPLATE_LANGUAGE_CODE", options.DefaultTemplateLanguageCode);
        options.TestApiBaseUrl = GetWhatsAppEnvValue("TEST_API_BASE_URL", options.TestApiBaseUrl);
    }

    private static string? GetWhatsAppEnvValue(string suffix, string? fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__{suffix}") ?? fallback;

    private static bool GetWhatsAppEnvBoolean(string suffix, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }

    private static void ApplyAiTaskDeliveryEnvironmentOverrides(AiTaskDeliveryOptions options)
    {
        options.WorkerEnabled = GetDeliveryEnvBoolean("WORKER_ENABLED", options.WorkerEnabled);
        options.LeaseSeconds = GetDeliveryEnvInt32("LEASE_SECONDS", options.LeaseSeconds);
        options.RetryBaseDelaySeconds = GetDeliveryEnvInt32("RETRY_BASE_DELAY_SECONDS", options.RetryBaseDelaySeconds);
        options.EnableClosureTtlPolicies = GetDeliveryEnvBoolean(
            "ENABLE_CLOSURE_TTL_POLICIES",
            options.EnableClosureTtlPolicies);
    }

    private static string GetDeliveryEnvValue(string suffix, string fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__{suffix}") ?? fallback;

    private static int GetDeliveryEnvInt32(string suffix, int fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

    private static bool GetDeliveryEnvBoolean(string suffix, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__AI_TASK_DELIVERY__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }
}

public sealed record EnqueueDeliveryRequest(
    string Channel,
    string RecipientKind,
    Guid? RecipientUserId,
    string? MaskedRecipient,
    string? IdempotencyKey,
    string? DeliveryProjectionJson,
    Guid? SecureAccessGrantId,
    int? MaxAttempts,
    string? RecipientE164 = null,
    string? ConsentStatus = null,
    string? SecureTaskUrl = null,
    string? RecipientEmail = null,
    string? RecipientEligibility = null);

public static class AiTaskSecureAccessEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskSecureAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grants = endpoints.MapGroup("/api/ai-task-secure-access").RequireAuthorization();
        grants.MapPost("/{grantId:guid}/revoke", RevokeGrantAsync);
        grants.MapPost("/{grantId:guid}/rotate", RotateGrantAsync);

        var tasks = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();
        tasks.MapPost("/{taskId:guid}/secure-access", IssueGrantAsync);
        tasks.MapGet("/{taskId:guid}/secure-access", ListGrantsAsync);

        var publicGroup = endpoints.MapGroup("/api/public/ai-task-access");
        publicGroup.MapPost("/resolve", ResolvePublicAsync)
            .AllowAnonymous()
            .RequireRateLimiting("ai-task-secure-access-resolve");

        return endpoints;
    }

    private static async Task<IResult> IssueGrantAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] IssueSecureAccessRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskSecureAccessRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.SecureAccessManage,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.IssueAsync(
            context,
            new IssueAiTaskSecureAccessCommand(taskId, request.TtlPolicy, request.Scope),
            cancellationToken);

        if (!result.Success)
        {
            return AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
        }

        return TypedResults.Ok(new
        {
            grant = result.Grant,
            secureLinkPath = result.SecureLinkPath,
            accessToken = result.RawToken,
        });
    }

    private static async Task<IResult> ListGrantsAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskSecureAccessRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.SecureAccessRead,
            cancellationToken);
        if (context is null) return failResult!;

        var items = await repository.ListForTaskAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(new { items });
    }

    private static async Task<IResult> RevokeGrantAsync(
        Guid grantId,
        [FromQuery] Guid? facilityId,
        [FromBody] RevokeSecureAccessRequest? request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskSecureAccessRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.SecureAccessManage,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RevokeAsync(context, grantId, request?.Reason, cancellationToken);
        return result.Success
            ? TypedResults.Ok(result.Grant)
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
    }

    private static async Task<IResult> RotateGrantAsync(
        Guid grantId,
        [FromQuery] Guid? facilityId,
        [FromBody] RotateSecureAccessRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskSecureAccessRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId,
            facilityAccessResolver,
            correlationContext,
            AiTaskPermissionKeys.SecureAccessManage,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RotateAsync(context, grantId, request.TtlPolicy, cancellationToken);
        if (!result.Success)
        {
            return AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureMessage!);
        }

        return TypedResults.Ok(new
        {
            grant = result.Grant,
            secureLinkPath = result.SecureLinkPath,
            accessToken = result.RawToken,
        });
    }

    private static async Task<IResult> ResolvePublicAsync(
        [FromBody] ResolveSecureAccessRequest request,
        IAiTaskSecureAccessRepository repository,
        CancellationToken cancellationToken)
    {
        var result = await repository.ResolvePublicAsync(request.AccessToken, cancellationToken);
        if (!result.Success)
        {
            return TypedResults.Json(
                new { code = result.FailureCode, message = "Görev bağlantısı kullanılamıyor." },
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(result.Projection);
    }

    public static void AddAiTaskSecureAccessRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("ai-task-secure-access-resolve", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }
}

public sealed record IssueSecureAccessRequest(string TtlPolicy, string Scope = AiTaskSecureAccessScopes.ReadOnly);
public sealed record RevokeSecureAccessRequest(string? Reason);
public sealed record RotateSecureAccessRequest(string TtlPolicy);
public sealed record ResolveSecureAccessRequest(string AccessToken);

internal sealed class AiTaskDeliveryBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<AiTaskDeliveryOptions> deliveryOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!deliveryOptions.Value.WorkerEnabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
                if (db is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                var discoverer = scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryWorkTenantDiscoverer>();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
                var worker = scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryWorker>();
                var contexts = await discoverer.DiscoverWorkerContextsAsync(stoppingToken);

                foreach (var context in contexts)
                {
                    tenantAccessor.Set(context);
                    await worker.ProcessBatchAsync(Environment.MachineName, 10, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Worker batch failures are surfaced via delivery records and health endpoint.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
