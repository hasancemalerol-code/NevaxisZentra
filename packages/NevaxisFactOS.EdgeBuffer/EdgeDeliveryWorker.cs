using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NevaxisFactOS.EdgeBuffer;

public sealed class EdgeDeliveryWorker : BackgroundService
{
    private readonly EdgeBufferStore _store;
    private readonly EdgeReliabilityState _state;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EdgeDeliveryWorker(
        EdgeBufferStore store,
        EdgeReliabilityState state,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _store = store;
        _state = state;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Delivery iteration failure is surfaced via buffer health metrics.
                _ = ex;
            }

            await Task.Delay(EdgeBufferConstants.WorkerPollInterval, stoppingToken);
        }
    }

    internal async Task<int> ProcessPendingBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var leaseId = Guid.NewGuid().ToString("N");
        var batch = _store.ClaimBatch(EdgeBufferConstants.DefaultBatchSize, now, leaseId);
        var processed = 0;

        foreach (var evt in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessEventAsync(evt, now, cancellationToken);
            processed++;
        }

        return processed;
    }

    internal async Task<EdgeDeliveryAttemptResult> ProcessEventAsync(
        EdgeBufferedEvent evt,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var attemptCount = evt.AttemptCount + 1;

        if (evt.PayloadVersion > EdgeBufferConstants.SupportedPayloadVersion)
        {
            _store.MarkDeadLetter(evt.Id, attemptCount, nowUtc, EdgeBufferErrorCodes.UnsupportedPayloadVersion);
            return new EdgeDeliveryAttemptResult(false, false, false, EdgeBufferErrorCodes.UnsupportedPayloadVersion, null, 422);
        }

        if (_state.IsNonRetryableKey(evt.IdempotencyKey))
        {
            _store.MarkDeadLetter(evt.Id, attemptCount, nowUtc, EdgeBufferErrorCodes.MalformedEvent);
            return new EdgeDeliveryAttemptResult(false, false, false, EdgeBufferErrorCodes.MalformedEvent, null, 400);
        }

        if (!_state.IsApiAvailable())
        {
            _store.RecordDeliveryFailure();
            await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, EdgeBufferErrorCodes.NetworkUnavailable);
            return new EdgeDeliveryAttemptResult(false, false, true, EdgeBufferErrorCodes.NetworkUnavailable, null, null);
        }

        var simulatedStatus = _state.GetSimulatedHttpStatus();
        if (simulatedStatus > 0)
        {
            _store.RecordDeliveryFailure();
            if (EdgeDeliveryClassifier.IsRetryableHttpStatus(simulatedStatus))
            {
                await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, EdgeDeliveryClassifier.ClassifyHttpStatus(simulatedStatus));
                return new EdgeDeliveryAttemptResult(false, false, true, EdgeDeliveryClassifier.ClassifyHttpStatus(simulatedStatus), null, simulatedStatus);
            }

            _store.MarkDeadLetter(evt.Id, attemptCount, nowUtc, EdgeDeliveryClassifier.ClassifyHttpStatus(simulatedStatus));
            return new EdgeDeliveryAttemptResult(false, false, false, EdgeDeliveryClassifier.ClassifyHttpStatus(simulatedStatus), null, simulatedStatus);
        }

        if (_state.TryConsumeRetryableOnce(evt.IdempotencyKey))
        {
            _store.RecordDeliveryFailure();
            await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, EdgeBufferErrorCodes.RetryableHttp);
            return new EdgeDeliveryAttemptResult(false, false, true, EdgeBufferErrorCodes.RetryableHttp, null, 503);
        }

        try
        {
            var result = await DeliverToApiAsync(evt, cancellationToken);
            if (result.Success)
            {
                var ackId = result.AcknowledgementId ?? evt.EventId.ToString("N");
                _store.MarkDelivered(evt.Id, nowUtc, ackId);
                return result;
            }

            _store.RecordDeliveryFailure();
            if (result.IsRetryable && !_store.RetryPolicy.ShouldDeadLetter(attemptCount))
            {
                await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, result.ErrorCode ?? EdgeBufferErrorCodes.RetryableHttp);
            }
            else
            {
                _store.MarkDeadLetter(evt.Id, attemptCount, nowUtc, result.ErrorCode ?? EdgeBufferErrorCodes.NonRetryableHttp);
            }

            return result;
        }
        catch (TaskCanceledException)
        {
            _store.RecordDeliveryFailure();
            await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, EdgeBufferErrorCodes.Timeout);
            return new EdgeDeliveryAttemptResult(false, false, true, EdgeBufferErrorCodes.Timeout, null, 408);
        }
        catch (HttpRequestException)
        {
            _store.RecordDeliveryFailure();
            await HandleRetryableFailureAsync(evt, attemptCount, nowUtc, EdgeBufferErrorCodes.NetworkUnavailable);
            return new EdgeDeliveryAttemptResult(false, false, true, EdgeBufferErrorCodes.NetworkUnavailable, null, null);
        }
    }

    private Task HandleRetryableFailureAsync(EdgeBufferedEvent evt, int attemptCount, DateTimeOffset nowUtc, string errorCode)
    {
        if (_store.RetryPolicy.ShouldDeadLetter(attemptCount))
        {
            _store.MarkDeadLetter(evt.Id, attemptCount, nowUtc, errorCode);
            return Task.CompletedTask;
        }

        var next = _store.RetryPolicy.ComputeNextAttemptUtc(attemptCount, nowUtc, evt.IdempotencyKey);
        _store.MarkRetryWait(evt.Id, attemptCount, nowUtc, next, errorCode);
        return Task.CompletedTask;
    }

    private async Task<EdgeDeliveryAttemptResult> DeliverToApiAsync(EdgeBufferedEvent evt, CancellationToken cancellationToken)
    {
        var apiBase = _configuration["NEVAXIS_FACTOS_API_URL"]
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_API_URL")
            ?? "http://localhost:5000";
        var token = _configuration["NEVAXIS_FACTOS_DEV_TOKEN"]
            ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_DEV_TOKEN");

        var client = _httpClientFactory.CreateClient("EdgeDelivery");
        client.Timeout = TimeSpan.FromSeconds(15);
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var payload = new
        {
            facilityId = evt.FacilityId,
            machineId = evt.MachineId,
            gatewayId = evt.GatewayId,
            eventKind = evt.EventType,
            idempotencyKey = evt.IdempotencyKey,
            sourceTimestampUtc = evt.SourceTimestampUtc,
            sourceSequence = evt.SourceSequence ?? 0L,
            payload = evt.Payload,
        };

        var url = $"{apiBase.TrimEnd('/')}/api/edge/events/ingest?facilityId={evt.FacilityId}";
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        var bodyText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            string? ackId = null;
            var isDuplicate = false;
            try
            {
                using var doc = JsonDocument.Parse(bodyText);
                if (doc.RootElement.TryGetProperty("eventId", out var eventIdProp) && eventIdProp.ValueKind == JsonValueKind.String)
                {
                    ackId = eventIdProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("isDuplicate", out var dupProp))
                {
                    isDuplicate = dupProp.GetBoolean();
                }
            }
            catch (JsonException)
            {
                ackId = evt.EventId.ToString("N");
            }

            return new EdgeDeliveryAttemptResult(true, isDuplicate, false, null, ackId, (int)response.StatusCode);
        }

        var status = (int)response.StatusCode;
        var errorCode = status switch
        {
            401 or 403 => EdgeBufferErrorCodes.AuthorizationDenied,
            422 => EdgeBufferErrorCodes.UnsupportedPayloadVersion,
            400 => EdgeBufferErrorCodes.MalformedEvent,
            _ => EdgeDeliveryClassifier.ClassifyHttpStatus(status),
        };
        var retryable = EdgeDeliveryClassifier.IsRetryableHttpStatus(status);
        return new EdgeDeliveryAttemptResult(false, false, retryable, errorCode, null, status);
    }
}
