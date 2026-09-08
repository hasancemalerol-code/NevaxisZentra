using CrevaFactOS.Persistence.AiTasks.Delivery.Email;
using CrevaFactOS.Persistence.AiTasks.Delivery.WhatsApp;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed class AiTaskDeliveryWorker(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskDeliveryChannelRegistry channelRegistry,
    ITenantContextAccessor tenantContextAccessor,
    IOptions<AiTaskDeliveryOptions> deliveryOptions) : IAiTaskDeliveryWorker
{
    private readonly AiTaskDeliveryOptions options = deliveryOptions.Value;
    public Task<int> ProcessBatchAsync(
        string workerId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (!tenantContextAccessor.IsAvailable)
        {
            return Task.FromResult(0);
        }

        return tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var tenantId = tenantContextAccessor.RequireTenantId();
                var now = DateTimeOffset.UtcNow;
                var claimedIds = await ClaimDeliveriesAsync(tenantId, workerId, batchSize, now, ct);
                var processed = 0;

                foreach (var deliveryId in claimedIds)
                {
                    var delivery = await dbContext.AiTaskDeliveries
                        .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == deliveryId, ct);

                    if (delivery is null)
                    {
                        continue;
                    }

                    await ProcessDeliveryAsync(delivery, workerId, now, ct);
                    processed++;
                }

                AiTaskDeliveryWorkerState.LastHeartbeatUtc = DateTimeOffset.UtcNow;
                return processed;
            },
            cancellationToken);
    }

    public Task RecordHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        AiTaskDeliveryWorkerState.LastHeartbeatUtc = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<Guid>> ClaimDeliveriesAsync(
        Guid tenantId,
        string workerId,
        int batchSize,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH claimable AS (
                SELECT id
                FROM ai_task_deliveries
                WHERE tenant_id = @tenant_id
                  AND (
                        status = 'queued'
                        OR (status = 'failed-retryable' AND next_attempt_at_utc <= @now_utc)
                        OR (status = 'processing' AND lease_expires_at_utc IS NOT NULL AND lease_expires_at_utc <= @now_utc)
                      )
                  AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= @now_utc)
                ORDER BY next_attempt_at_utc NULLS FIRST, requested_at_utc
                LIMIT @batch_size
                FOR UPDATE SKIP LOCKED
            )
            UPDATE ai_task_deliveries AS delivery
            SET status = 'processing',
                lease_owner = @worker_id,
                lease_expires_at_utc = @lease_expires,
                updated_at_utc = @now_utc
            FROM claimable
            WHERE delivery.id = claimable.id
            RETURNING delivery.id;
            """;

        command.Parameters.Add(new NpgsqlParameter("tenant_id", tenantId));
        command.Parameters.Add(new NpgsqlParameter("worker_id", workerId));
        command.Parameters.Add(new NpgsqlParameter("now_utc", nowUtc));
        command.Parameters.Add(new NpgsqlParameter("lease_expires", nowUtc.AddSeconds(options.LeaseSeconds)));
        command.Parameters.Add(new NpgsqlParameter("batch_size", batchSize));

        var claimed = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            claimed.Add(reader.GetGuid(0));
        }

        return claimed;
    }

    private async Task ProcessDeliveryAsync(
        AiTaskDelivery delivery,
        string workerId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        delivery.AttemptCount += 1;
        var attemptNumber = delivery.AttemptCount;
        var adapter = channelRegistry.Resolve(delivery.Channel);
        var request = new AiTaskDeliveryDispatchRequest(
            delivery.Id,
            delivery.TenantId,
            delivery.AiTaskId,
            delivery.Channel,
            delivery.RecipientKind,
            delivery.RecipientUserId,
            delivery.MaskedRecipient,
            delivery.DeliveryProjectionJson,
            delivery.SecureAccessGrantId);

        var validationError = adapter.ValidateRequest(request);
        var startedAt = DateTimeOffset.UtcNow;
        AiTaskDeliveryDispatchResult result;

        if (validationError is not null)
        {
            result = new AiTaskDeliveryDispatchResult(
                false,
                AiTaskDeliveryAttemptOutcomes.ConfigurationFailure,
                AiTaskDeliveryErrorCategories.Configuration,
                "validation-failed",
                null,
                null,
                null);
        }
        else
        {
            result = await adapter.DispatchAsync(request, cancellationToken);
        }

        var completedAt = DateTimeOffset.UtcNow;
        dbContext.AiTaskDeliveryAttempts.Add(new AiTaskDeliveryAttempt
        {
            Id = Guid.NewGuid(),
            TenantId = delivery.TenantId,
            DeliveryId = delivery.Id,
            AttemptNumber = attemptNumber,
            AdapterKey = adapter.ChannelKey,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Outcome = result.Outcome,
            ProviderStatusCode = result.ProviderStatusCode,
            ProviderMessageId = result.ProviderMessageId,
            ErrorCategory = result.ErrorCategory,
            SanitizedErrorCode = result.SanitizedErrorCode,
        });

        delivery.LeaseOwner = workerId;
        delivery.LeaseExpiresAtUtc = nowUtc.AddSeconds(options.LeaseSeconds);
        delivery.UpdatedAtUtc = completedAt;

        if (result.Success)
        {
            AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.Sent);
            delivery.Status = AiTaskDeliveryStatuses.Sent;
            delivery.SentAtUtc = completedAt;
            delivery.ProviderMessageId = result.ProviderMessageId;
            delivery.ProviderCorrelationId = result.ProviderCorrelationId;
            if (delivery.Channel == AiTaskDeliveryChannels.WhatsApp)
            {
                delivery.Provider = WhatsAppBusinessProviders.MetaWhatsAppCloud;
                delivery.ProviderStatus = WhatsAppProviderStatuses.Accepted;
                delivery.ProviderStatusAtUtc = completedAt;
            }
            else if (delivery.Channel == AiTaskDeliveryChannels.Email)
            {
                delivery.Provider = EmailDeliveryProviders.Smtp;
                delivery.ProviderStatus = EmailProviderStatuses.Accepted;
                delivery.ProviderStatusAtUtc = completedAt;
            }
            delivery.LastErrorCode = null;
            delivery.LastErrorCategory = null;
            delivery.LeaseOwner = null;
            delivery.LeaseExpiresAtUtc = null;
        }
        else if (result.ErrorCategory == AiTaskDeliveryErrorCategories.Permanent)
        {
            AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.FailedPermanent);
            delivery.Status = AiTaskDeliveryStatuses.FailedPermanent;
            delivery.LastErrorCode = result.SanitizedErrorCode;
            delivery.LastErrorCategory = result.ErrorCategory;
            delivery.LeaseOwner = null;
            delivery.LeaseExpiresAtUtc = null;
        }
        else if (result.ErrorCategory == AiTaskDeliveryErrorCategories.Configuration)
        {
            AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.FailedPermanent);
            delivery.Status = AiTaskDeliveryStatuses.FailedPermanent;
            delivery.LastErrorCode = result.SanitizedErrorCode;
            delivery.LastErrorCategory = result.ErrorCategory;
            delivery.LeaseOwner = null;
            delivery.LeaseExpiresAtUtc = null;
        }
        else if (delivery.AttemptCount >= delivery.MaxAttempts)
        {
            if (delivery.Status == AiTaskDeliveryStatuses.Processing)
            {
                AiTaskDeliveryStateMachine.EnsureTransition(
                    delivery.Status,
                    AiTaskDeliveryStatuses.FailedRetryable);
                delivery.Status = AiTaskDeliveryStatuses.FailedRetryable;
            }

            AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.DeadLetter);
            delivery.Status = AiTaskDeliveryStatuses.DeadLetter;
            delivery.LastErrorCode = result.SanitizedErrorCode;
            delivery.LastErrorCategory = result.ErrorCategory;
            delivery.LeaseOwner = null;
            delivery.LeaseExpiresAtUtc = null;
        }
        else
        {
            var nextAttempt = AiTaskDeliveryRetryPolicy.ComputeNextAttemptUtc(
                delivery.AttemptCount,
                completedAt,
                options.RetryBaseDelaySeconds);
            AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.FailedRetryable);
            delivery.Status = AiTaskDeliveryStatuses.FailedRetryable;
            delivery.NextAttemptAtUtc = nextAttempt;
            delivery.LastErrorCode = result.SanitizedErrorCode;
            delivery.LastErrorCategory = result.ErrorCategory;
            delivery.LeaseOwner = null;
            delivery.LeaseExpiresAtUtc = null;
            dbContext.AiTaskDeliveryAttempts
                .Local
                .Select(a => a)
                .Last()
                .RetryScheduledForUtc = nextAttempt;
        }

        await dbContext.SetTenantSessionContextAsync(
            tenantContextAccessor.RequireTenantId(),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
