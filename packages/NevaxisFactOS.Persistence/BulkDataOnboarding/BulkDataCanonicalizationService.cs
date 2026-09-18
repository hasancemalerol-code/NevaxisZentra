using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.MixedProduction;

namespace NevaxisFactOS.Persistence.BulkDataOnboarding;

public interface IBulkDataCanonicalizationService
{
    Task<BulkCanonicalizationResult> CommitRecordAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        CancellationToken cancellationToken = default);
}

public sealed record BulkCanonicalizationResult(
    bool Success,
    bool Created,
    bool Updated,
    Guid? CanonicalEntityId,
    string? ErrorCode,
    string? SafeMessage);

public sealed class BulkDataCanonicalizationService(NevaxisFactOSDbContext dbContext) : IBulkDataCanonicalizationService
{
    private readonly Dictionary<string, string> _areaCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lineCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _stationCodes = new(StringComparer.OrdinalIgnoreCase);

    public async Task<BulkCanonicalizationResult> CommitRecordAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        CancellationToken cancellationToken = default)
    {
        var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(staging.PayloadJson)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        return staging.CanonicalEntityType switch
        {
            BulkCanonicalEntityTypes.ManufacturingArea => await CommitAreaAsync(context, staging, fields, cancellationToken),
            BulkCanonicalEntityTypes.ProductionLine => await CommitLineAsync(context, staging, fields, cancellationToken),
            BulkCanonicalEntityTypes.ProductionStation => await CommitStationAsync(context, staging, fields, cancellationToken),
            BulkCanonicalEntityTypes.ManufacturingResource => await CommitResourceAsync(context, staging, fields, cancellationToken),
            BulkCanonicalEntityTypes.Material => await CommitMaterialAsync(context, staging, fields, cancellationToken),
            BulkCanonicalEntityTypes.ProductionEvent => await CommitEventAsync(context, staging, fields, cancellationToken),
            _ => new BulkCanonicalizationResult(false, false, false, null, BulkFailureCodes.ValidationFailed, "Unsupported canonical entity type."),
        };
    }

    private async Task<BulkCanonicalizationResult> CommitAreaAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var code = GetRequired(fields, "code");
        var existing = await dbContext.ManufacturingAreas
            .FirstOrDefaultAsync(a => a.FacilityId == context.FacilityId && a.Code == code, cancellationToken);
        if (existing is not null)
        {
            _areaCodes[code] = existing.Id.ToString();
            return new BulkCanonicalizationResult(true, false, true, existing.Id, null, null);
        }

        var entity = new ManufacturingArea
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Code = code,
            Name = GetRequired(fields, "name"),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.ManufacturingAreas.Add(entity);
        _areaCodes[code] = entity.Id.ToString();
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private async Task<BulkCanonicalizationResult> CommitLineAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var code = GetRequired(fields, "code");
        var areaCode = GetRequired(fields, "area_code");
        var area = await dbContext.ManufacturingAreas
            .FirstOrDefaultAsync(a => a.FacilityId == context.FacilityId && a.Code == areaCode, cancellationToken);
        if (area is null)
        {
            return new BulkCanonicalizationResult(false, false, false, null, BulkFailureCodes.ValidationFailed, "Parent area not found.");
        }

        var existing = await dbContext.ProductionLines
            .FirstOrDefaultAsync(l => l.FacilityId == context.FacilityId && l.Code == code, cancellationToken);
        if (existing is not null)
        {
            _lineCodes[code] = existing.Id.ToString();
            return new BulkCanonicalizationResult(true, false, true, existing.Id, null, null);
        }

        var entity = new ProductionLine
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            AreaId = area.Id,
            Code = code,
            Name = GetRequired(fields, "name"),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.ProductionLines.Add(entity);
        _lineCodes[code] = entity.Id.ToString();
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private async Task<BulkCanonicalizationResult> CommitStationAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var code = GetRequired(fields, "code");
        var lineCode = GetRequired(fields, "line_code");
        var line = await dbContext.ProductionLines
            .FirstOrDefaultAsync(l => l.FacilityId == context.FacilityId && l.Code == lineCode, cancellationToken);
        if (line is null)
        {
            return new BulkCanonicalizationResult(false, false, false, null, BulkFailureCodes.ValidationFailed, "Parent line not found.");
        }

        var existing = await dbContext.ProductionStations
            .FirstOrDefaultAsync(s => s.FacilityId == context.FacilityId && s.Code == code, cancellationToken);
        if (existing is not null)
        {
            _stationCodes[code] = existing.Id.ToString();
            return new BulkCanonicalizationResult(true, false, true, existing.Id, null, null);
        }

        var entity = new ProductionStation
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            LineId = line.Id,
            Code = code,
            Name = GetRequired(fields, "name"),
            StationKind = fields.GetValueOrDefault("station_kind") ?? MixedProductionStationKinds.Process,
            SequenceOrder = int.TryParse(fields.GetValueOrDefault("sequence_order"), out var seq) ? seq : 0,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.ProductionStations.Add(entity);
        _stationCodes[code] = entity.Id.ToString();
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private async Task<BulkCanonicalizationResult> CommitResourceAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var code = GetRequired(fields, "code");
        var stationCode = GetRequired(fields, "station_code");
        var station = await dbContext.ProductionStations
            .FirstOrDefaultAsync(s => s.FacilityId == context.FacilityId && s.Code == stationCode, cancellationToken);
        if (station is null)
        {
            return new BulkCanonicalizationResult(false, false, false, null, BulkFailureCodes.ValidationFailed, "Parent station not found.");
        }

        var existing = await dbContext.ManufacturingResources
            .FirstOrDefaultAsync(r => r.FacilityId == context.FacilityId && r.Code == code, cancellationToken);
        if (existing is not null)
        {
            return new BulkCanonicalizationResult(true, false, true, existing.Id, null, null);
        }

        var entity = new ManufacturingResource
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            StationId = station.Id,
            Code = code,
            Name = GetRequired(fields, "name"),
            ResourceType = fields.GetValueOrDefault("resource_type") ?? "machine",
            MachineRegistryCode = fields.GetValueOrDefault("machine_registry_code"),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.ManufacturingResources.Add(entity);
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private async Task<BulkCanonicalizationResult> CommitMaterialAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var code = GetRequired(fields, "code");
        var existing = await dbContext.Materials
            .FirstOrDefaultAsync(m => m.FacilityId == context.FacilityId && m.Code == code, cancellationToken);
        if (existing is not null)
        {
            return new BulkCanonicalizationResult(true, false, true, existing.Id, null, null);
        }

        var entity = new Material.MaterialRegistryEntry
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Code = code,
            CodeNormalized = code.ToUpperInvariant(),
            Name = GetRequired(fields, "name"),
            UnitOfMeasure = fields.GetValueOrDefault("unit_of_measure") ?? "kg",
            SourceType = Material.MaterialSourceType.Imported,
            SourceSystem = staging.SourceSystem,
            SourceReference = staging.SourceKey,
            ExternalMaterialCode = code,
            DataRecordedAtUtc = DateTimeOffset.UtcNow,
            CreateIdempotencyKey = BulkIdempotencyEngine.BuildImportIdempotencyKey(
                staging.SourceSystem,
                staging.SourceTable,
                staging.SourceKey,
                staging.RecordChecksum),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = context.SubjectId,
            UpdatedBySubjectId = context.SubjectId,
        };
        dbContext.Materials.Add(entity);
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private async Task<BulkCanonicalizationResult> CommitEventAsync(
        BulkDataOperationContext context,
        BulkImportStagingRecord staging,
        Dictionary<string, string?> fields,
        CancellationToken cancellationToken)
    {
        var resourceCode = GetRequired(fields, "resource_code");
        var resource = await dbContext.ManufacturingResources
            .FirstOrDefaultAsync(r => r.FacilityId == context.FacilityId && r.Code == resourceCode, cancellationToken);
        if (resource is null)
        {
            return new BulkCanonicalizationResult(false, false, false, null, BulkFailureCodes.ValidationFailed, "Resource not found.");
        }

        var station = await dbContext.ProductionStations
            .FirstOrDefaultAsync(s => s.Id == resource.StationId, cancellationToken);
        var lineId = station?.LineId;
        var eventType = fields.GetValueOrDefault("event_type") ?? MixedProductionEventTypes.Good;
        var occurredAt = staging.SourceTimestampUtc ?? DateTimeOffset.UtcNow;
        var idempotencyKey = BulkIdempotencyEngine.BuildImportIdempotencyKey(
            staging.SourceSystem,
            staging.SourceTable,
            staging.SourceKey,
            staging.RecordChecksum);

        var existing = await dbContext.CanonicalProductionEvents
            .AnyAsync(e => e.FacilityId == context.FacilityId && e.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing)
        {
            return new BulkCanonicalizationResult(true, false, false, null, null, null);
        }

        var entity = new CanonicalProductionEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            LineId = lineId,
            StationId = resource.StationId,
            ResourceId = resource.Id,
            ResourceType = resource.ResourceType,
            EventType = eventType,
            Quantity = int.TryParse(fields.GetValueOrDefault("good_qty"), out var qty) ? qty : null,
            OccurredAtUtc = occurredAt,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            IdempotencyKey = idempotencyKey,
            Source = staging.SourceSystem,
            SourceEventId = staging.SourceKey,
        };
        dbContext.CanonicalProductionEvents.Add(entity);
        return new BulkCanonicalizationResult(true, true, false, entity.Id, null, null);
    }

    private static string GetRequired(Dictionary<string, string?> fields, string key) =>
        fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new InvalidOperationException($"Required field '{key}' is missing.");
}
