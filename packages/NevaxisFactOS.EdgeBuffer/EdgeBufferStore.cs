using System.Globalization;
using System.Text.Json;

namespace NevaxisFactOS.EdgeBuffer;

public sealed class EdgeBufferStore
{
    private readonly string _storePath;
    private readonly EdgeReliabilityState _state;
    private readonly EdgeRetryPolicy _retryPolicy;
    private readonly object _writeLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public EdgeBufferStore(string storePath, EdgeReliabilityState state, EdgeRetryPolicy? retryPolicy = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
        _storePath = storePath;
        _state = state;
        _retryPolicy = retryPolicy ?? new EdgeRetryPolicy { UseDeterministicJitter = true };
        EnsureSchema();
    }

    public static string ResolveDefaultDbPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "crevafactos", "edge-buffer", "simulator-gw-001.json");
    }

    public EdgeBufferEnqueueResult Enqueue(EdgeBufferedEvent evt)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            var max = _state.GetEffectiveMaxBufferedEvents();
            var activeCount = snapshot.Events.Count(e => e.DeliveryStatus != EdgeDeliveryStatuses.Delivered);
            if (activeCount >= max)
            {
                return new EdgeBufferEnqueueResult(false, EdgeBufferErrorCodes.CapacityExceeded, null);
            }

            if (snapshot.Events.Any(e => e.IdempotencyKey == evt.IdempotencyKey))
            {
                return new EdgeBufferEnqueueResult(false, EdgeBufferErrorCodes.MalformedEvent, null);
            }

            snapshot.Events.Add(evt);
            IncrementCounter(snapshot, "events_buffered_total");
            SaveSnapshot(snapshot);
            return new EdgeBufferEnqueueResult(true, null, evt.Id);
        }
    }

    public IReadOnlyList<EdgeBufferedEvent> ClaimBatch(int batchSize, DateTimeOffset nowUtc, string leaseId)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            RecoverStaleInFlight(snapshot, nowUtc);

            var candidates = snapshot.Events
                .Where(e => e.DeliveryStatus is EdgeDeliveryStatuses.Pending or EdgeDeliveryStatuses.RetryWait)
                .Where(e => e.NextAttemptAtUtc is null || e.NextAttemptAtUtc <= nowUtc)
                .Where(e => e.InFlightLeaseId is null)
                .OrderBy(e => e.GatewayId)
                .ThenBy(e => e.MachineId)
                .ThenBy(e => e.SourceSequence ?? 0)
                .ThenBy(e => e.CreatedAtUtc)
                .Take(batchSize)
                .ToList();

            var claimed = new List<EdgeBufferedEvent>();
            foreach (var evt in candidates)
            {
                var idx = snapshot.Events.FindIndex(e => e.Id == evt.Id);
                if (idx < 0) continue;

                snapshot.Events[idx] = evt with
                {
                    DeliveryStatus = EdgeDeliveryStatuses.InFlight,
                    InFlightClaimedAtUtc = nowUtc,
                    InFlightLeaseId = leaseId,
                };
                claimed.Add(snapshot.Events[idx]);
            }

            if (claimed.Count > 0) SaveSnapshot(snapshot);
            return claimed;
        }
    }

    public void MarkDelivered(Guid eventId, DateTimeOffset nowUtc, string acknowledgementId)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            IncrementCounter(snapshot, "events_delivered_total");
            UpdateEvent(snapshot, eventId, evt => evt with
            {
                DeliveryStatus = EdgeDeliveryStatuses.Delivered,
                DeliveredAtUtc = nowUtc,
                AcknowledgementId = acknowledgementId,
                LastAttemptAtUtc = nowUtc,
                InFlightClaimedAtUtc = null,
                InFlightLeaseId = null,
                LastErrorCode = null,
            });
            snapshot.Meta["last_successful_delivery_at_utc"] = nowUtc.UtcDateTime.ToString("O");
            SaveSnapshot(snapshot);
        }
    }

    public void MarkRetryWait(Guid eventId, int attemptCount, DateTimeOffset nowUtc, DateTimeOffset nextAttemptUtc, string errorCode)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            IncrementCounter(snapshot, "retries_total");
            UpdateEvent(snapshot, eventId, evt => evt with
            {
                DeliveryStatus = EdgeDeliveryStatuses.RetryWait,
                AttemptCount = attemptCount,
                LastAttemptAtUtc = nowUtc,
                NextAttemptAtUtc = nextAttemptUtc,
                LastErrorCode = errorCode,
                InFlightClaimedAtUtc = null,
                InFlightLeaseId = null,
            });
            snapshot.Meta["last_delivery_attempt_at_utc"] = nowUtc.UtcDateTime.ToString("O");
            SaveSnapshot(snapshot);
        }
    }

    public void MarkDeadLetter(Guid eventId, int attemptCount, DateTimeOffset nowUtc, string errorCode)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            IncrementCounter(snapshot, "dead_letters_total");
            UpdateEvent(snapshot, eventId, evt => evt with
            {
                DeliveryStatus = EdgeDeliveryStatuses.DeadLetter,
                AttemptCount = attemptCount,
                LastAttemptAtUtc = nowUtc,
                LastErrorCode = errorCode,
                DeadLetteredAtUtc = nowUtc,
                InFlightClaimedAtUtc = null,
                InFlightLeaseId = null,
            });
            snapshot.Meta["last_delivery_attempt_at_utc"] = nowUtc.UtcDateTime.ToString("O");
            SaveSnapshot(snapshot);
        }
    }

    public void RecordDeliveryFailure()
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            IncrementCounter(snapshot, "delivery_failures_total");
            SaveSnapshot(snapshot);
        }
    }

    public EdgeBufferHealth GetHealth(DateTimeOffset nowUtc)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshotUnsafe();
            return BuildHealth(snapshot, nowUtc);
        }
    }

    public IReadOnlyList<EdgeBufferedEvent> ListEvents(string? status, int limit)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshotUnsafe();
            var q = snapshot.Events.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(e => e.DeliveryStatus == status);
            }

            return q
                .OrderBy(e => e.GatewayId)
                .ThenBy(e => e.MachineId)
                .ThenBy(e => e.SourceSequence ?? 0)
                .ThenBy(e => e.CreatedAtUtc)
                .Take(Math.Clamp(limit, 1, 500))
                .ToList();
        }
    }

    public IReadOnlyList<EdgeBufferedEvent> ListDeadLetters(int limit) =>
        ListEvents(EdgeDeliveryStatuses.DeadLetter, limit);

    public EdgeBufferedEvent? GetByIdempotencyKey(string idempotencyKey)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshotUnsafe();
            return snapshot.Events.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        }
    }

    public int CountByStatus(string status)
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshotUnsafe();
            return snapshot.Events.Count(e => e.DeliveryStatus == status);
        }
    }

    public void ClearAll()
    {
        lock (_writeLock)
        {
            SaveSnapshot(new BufferSnapshot { SchemaVersion = EdgeBufferConstants.SchemaVersion });
        }
    }

    public int ResetDeliveredForReplay()
    {
        lock (_writeLock)
        {
            var snapshot = LoadSnapshot();
            var now = DateTimeOffset.UtcNow;
            var count = 0;
            for (var i = 0; i < snapshot.Events.Count; i++)
            {
                if (snapshot.Events[i].DeliveryStatus != EdgeDeliveryStatuses.Delivered) continue;
                snapshot.Events[i] = snapshot.Events[i] with
                {
                    DeliveryStatus = EdgeDeliveryStatuses.Pending,
                    AttemptCount = 0,
                    LastAttemptAtUtc = null,
                    NextAttemptAtUtc = now,
                    LastErrorCode = null,
                    DeliveredAtUtc = null,
                    AcknowledgementId = null,
                    DeadLetteredAtUtc = null,
                    InFlightClaimedAtUtc = null,
                    InFlightLeaseId = null,
                };
                count++;
            }

            if (count > 0) SaveSnapshot(snapshot);
            return count;
        }
    }

    public EdgeRetryPolicy RetryPolicy => _retryPolicy;

    private void EnsureSchema()
    {
        if (File.Exists(_storePath)) return;
        SaveSnapshot(new BufferSnapshot { SchemaVersion = EdgeBufferConstants.SchemaVersion });
    }

    private BufferSnapshot LoadSnapshotUnsafe()
    {
        if (!File.Exists(_storePath))
        {
            return new BufferSnapshot { SchemaVersion = EdgeBufferConstants.SchemaVersion };
        }

        var json = File.ReadAllText(_storePath);
        var snapshot = JsonSerializer.Deserialize<BufferSnapshot>(json, JsonOptions)
            ?? new BufferSnapshot { SchemaVersion = EdgeBufferConstants.SchemaVersion };
        snapshot.SchemaVersion = EdgeBufferConstants.SchemaVersion;
        return snapshot;
    }

    private BufferSnapshot LoadSnapshot() => LoadSnapshotUnsafe();

    private void SaveSnapshot(BufferSnapshot snapshot)
    {
        snapshot.SchemaVersion = EdgeBufferConstants.SchemaVersion;
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        var tempPath = $"{_storePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(_storePath))
        {
            File.Replace(tempPath, _storePath, null);
        }
        else
        {
            File.Move(tempPath, _storePath);
        }
    }

    private EdgeBufferHealth BuildHealth(BufferSnapshot snapshot, DateTimeOffset nowUtc)
    {
        var pending = snapshot.Events.Count(e => e.DeliveryStatus is EdgeDeliveryStatuses.Pending or EdgeDeliveryStatuses.InFlight);
        var retry = snapshot.Events.Count(e => e.DeliveryStatus == EdgeDeliveryStatuses.RetryWait);
        var dead = snapshot.Events.Count(e => e.DeliveryStatus == EdgeDeliveryStatuses.DeadLetter);
        var delivered = snapshot.Events.Count(e => e.DeliveryStatus == EdgeDeliveryStatuses.Delivered);
        var oldest = snapshot.Events
            .Where(e => e.DeliveryStatus is EdgeDeliveryStatuses.Pending or EdgeDeliveryStatuses.RetryWait or EdgeDeliveryStatuses.InFlight)
            .Select(e => (DateTimeOffset?)e.CreatedAtUtc)
            .Min();

        var max = _state.GetEffectiveMaxBufferedEvents();
        var active = pending + retry;
        var apiConnected = _state.IsApiAvailable();

        return new EdgeBufferHealth(
            apiConnected,
            !apiConnected ? "disconnected" : dead > 0 ? "degraded" : active > 0 ? "syncing" : "healthy",
            pending,
            retry,
            dead,
            delivered,
            oldest.HasValue ? nowUtc - oldest.Value : null,
            ParseMetaDateTime(snapshot, "last_successful_delivery_at_utc"),
            ParseMetaDateTime(snapshot, "last_delivery_attempt_at_utc"),
            max > 0 ? (double)active / max : 0,
            !apiConnected ? "offline-buffering" : active > 0 ? "replaying" : "idle",
            max,
            GetCounter(snapshot, "events_buffered_total"),
            GetCounter(snapshot, "events_delivered_total"),
            GetCounter(snapshot, "retries_total"),
            GetCounter(snapshot, "delivery_failures_total"));
    }

    private static void RecoverStaleInFlight(BufferSnapshot snapshot, DateTimeOffset nowUtc)
    {
        var cutoff = nowUtc - EdgeBufferConstants.InFlightLeaseTimeout;
        for (var i = 0; i < snapshot.Events.Count; i++)
        {
            var evt = snapshot.Events[i];
            if (evt.DeliveryStatus != EdgeDeliveryStatuses.InFlight) continue;
            if (evt.InFlightClaimedAtUtc is null || evt.InFlightClaimedAtUtc >= cutoff) continue;

            snapshot.Events[i] = evt with
            {
                DeliveryStatus = EdgeDeliveryStatuses.RetryWait,
                LastErrorCode = EdgeBufferErrorCodes.StaleInFlightRecovered,
                NextAttemptAtUtc = nowUtc,
                InFlightClaimedAtUtc = null,
                InFlightLeaseId = null,
            };
        }
    }

    private static void UpdateEvent(BufferSnapshot snapshot, Guid eventId, Func<EdgeBufferedEvent, EdgeBufferedEvent> update)
    {
        var idx = snapshot.Events.FindIndex(e => e.Id == eventId);
        if (idx >= 0) snapshot.Events[idx] = update(snapshot.Events[idx]);
    }

    private static DateTimeOffset? ParseMetaDateTime(BufferSnapshot snapshot, string key) =>
        snapshot.Meta.TryGetValue(key, out var raw)
            ? DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture)
            : null;

    private static long GetCounter(BufferSnapshot snapshot, string key) =>
        snapshot.Meta.TryGetValue(key, out var raw) && long.TryParse(raw, out var v) ? v : 0;

    private static void IncrementCounter(BufferSnapshot snapshot, string key)
    {
        snapshot.Meta[key] = (GetCounter(snapshot, key) + 1).ToString(CultureInfo.InvariantCulture);
    }

    private sealed class BufferSnapshot
    {
        public int SchemaVersion { get; set; } = EdgeBufferConstants.SchemaVersion;
        public Dictionary<string, string> Meta { get; set; } = new(StringComparer.Ordinal);
        public List<EdgeBufferedEvent> Events { get; set; } = [];
    }
}
