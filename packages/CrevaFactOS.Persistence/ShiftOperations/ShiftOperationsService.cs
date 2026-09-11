using System.Text.Json;
using CrevaFactOS.Persistence.ShiftHandover;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ShiftOperations;

public interface IShiftOperationsService
{
    Task<OperationalShiftInstance> StartShiftAsync(Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken);
    Task<ShiftClosingPreview> GetClosingPreviewAsync(Guid shiftInstanceId, CancellationToken cancellationToken);
    Task<ShiftHandoverPackage> PrepareHandoverAsync(Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken);
    Task<ShiftHandoverPackage> SubmitHandoverAsync(Guid shiftInstanceId, Guid membershipId, string? comments, CancellationToken cancellationToken);
    Task<ShiftHandoverAcceptance> AcceptHandoverAsync(Guid shiftInstanceId, AcceptHandoverRequest request, CancellationToken cancellationToken);
    Task<ShiftOpenItem> DispositionOpenItemAsync(Guid shiftInstanceId, Guid itemId, DispositionOpenItemRequest request, CancellationToken cancellationToken);
    Task<ShiftMetricsSnapshot> GetMetricsAsync(Guid shiftInstanceId, CancellationToken cancellationToken);
}

public sealed class ShiftOperationsService : IShiftOperationsService
{
    private readonly CrevaFactOSDbContext dbContext;
    private readonly IShiftOperationsRepository repository;
    private readonly IShiftHandoverCandidateCollector candidateCollector;
    private readonly IShiftHandoverRepository handoverRepository;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ShiftOperationsService(
        CrevaFactOSDbContext dbContext,
        IShiftOperationsRepository repository,
        IShiftHandoverCandidateCollector candidateCollector,
        IShiftHandoverRepository handoverRepository,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.repository = repository;
        this.candidateCollector = candidateCollector;
        this.handoverRepository = handoverRepository;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<OperationalShiftInstance> StartShiftAsync(
        Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => StartShiftCoreAsync(shiftInstanceId, membershipId, ct),
            cancellationToken);

    public Task<ShiftClosingPreview> GetClosingPreviewAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetClosingPreviewCoreAsync(shiftInstanceId, ct),
            cancellationToken);

    private async Task<ShiftClosingPreview> GetClosingPreviewCoreAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken)
    {
        var shift = await repository.GetShiftInstanceAsync(shiftInstanceId, cancellationToken)
            ?? throw new ShiftOperationsException(ShiftOperationsFailureCodes.NotFound, "Shift not found.");

        var candidates = await candidateCollector.CollectAsync(shift, cancellationToken);
        var existing = await repository.ListOpenItemsAsync(shiftInstanceId, cancellationToken);
        var merged = MergeCandidates(existing, candidates);
        var conflicts = ShiftHandoverConflictDetector.Detect(merged, new Dictionary<string, string>());
        var readiness = ComputeReadiness(merged, conflicts);
        var mustKnow = BuildNextShiftMustKnow(merged);

        return new ShiftClosingPreview(
            shiftInstanceId,
            candidates,
            existing,
            conflicts,
            readiness,
            mustKnow,
            PreviewOnly: true);
    }

    public Task<ShiftHandoverPackage> PrepareHandoverAsync(
        Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => PrepareHandoverCoreAsync(shiftInstanceId, membershipId, ct),
            cancellationToken);

    public Task<ShiftHandoverPackage> SubmitHandoverAsync(
        Guid shiftInstanceId, Guid membershipId, string? comments, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SubmitHandoverCoreAsync(shiftInstanceId, membershipId, comments, ct),
            cancellationToken);

    public Task<ShiftHandoverAcceptance> AcceptHandoverAsync(
        Guid shiftInstanceId, AcceptHandoverRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => AcceptHandoverCoreAsync(shiftInstanceId, request, ct),
            cancellationToken);

    public Task<ShiftOpenItem> DispositionOpenItemAsync(
        Guid shiftInstanceId, Guid itemId, DispositionOpenItemRequest request, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DispositionOpenItemCoreAsync(shiftInstanceId, itemId, request, ct),
            cancellationToken);

    public Task<ShiftMetricsSnapshot> GetMetricsAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetMetricsCoreAsync(shiftInstanceId, ct),
            cancellationToken);

    private async Task<ShiftMetricsSnapshot> GetMetricsCoreAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken)
    {
        var items = await repository.ListOpenItemsAsync(shiftInstanceId, cancellationToken);
        var verifiedHandovers = await dbContext.ShiftHandoverAcceptances.CountAsync(
            a => a.ShiftInstanceId == shiftInstanceId && a.Verified,
            cancellationToken);
        return new ShiftMetricsSnapshot(
            items.Count(i => i.Status == ShiftOpenItemStatuses.Open),
            items.Count(i => i.HandoverPriority == ShiftHandoverPriorities.Critical),
            items.Count(i => i.CarryoverCount > 0),
            items.Count(i => i.DigitalWorkCaseId.HasValue),
            items.Count(i => i.Category == ShiftOpenItemCategories.Approval),
            items.Count == 0 ? 100m : ComputeReadiness(items, []),
            verifiedHandovers,
            items.Count(i => i.CarryoverCount >= 2));
    }

    private async Task<OperationalShiftInstance> StartShiftCoreAsync(
        Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken)
    {
        var shift = await dbContext.OperationalShiftInstances
            .FirstOrDefaultAsync(s => s.Id == shiftInstanceId, cancellationToken)
            ?? throw new ShiftOperationsException(ShiftOperationsFailureCodes.NotFound, "Shift not found.");

        if (shift.Status is not (ShiftInstanceStatuses.Planned or ShiftInstanceStatuses.Ready))
        {
            throw new ShiftOperationsException(ShiftOperationsFailureCodes.InvalidTransition, "Shift cannot be started.");
        }

        shift.Status = ShiftInstanceStatuses.Active;
        shift.ActualStartUtc = DateTimeOffset.UtcNow;
        shift.SupervisorMembershipId = membershipId;
        shift.UpdatedAtUtc = DateTimeOffset.UtcNow;
        shift.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return shift;
    }

    private async Task<ShiftHandoverPackage> PrepareHandoverCoreAsync(
        Guid shiftInstanceId, Guid membershipId, CancellationToken cancellationToken)
    {
        var shift = await LoadShiftForWriteAsync(shiftInstanceId, cancellationToken);
        var preview = await BuildPreviewInternalAsync(shift, cancellationToken);
        await UpsertOpenItemsAsync(shift, preview.Candidates, cancellationToken);

        shift.Status = ShiftInstanceStatuses.Closing;
        shift.HandoverState = "preparing";
        shift.HandoverReadinessScore = preview.ReadinessScore;
        shift.AiSummaryJson = BuildAiSummary(preview);
        shift.UpdatedAtUtc = DateTimeOffset.UtcNow;
        shift.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);

        var items = await dbContext.ShiftOpenItems
            .Where(i => i.ShiftInstanceId == shiftInstanceId)
            .ToListAsync(cancellationToken);

        return new ShiftHandoverPackage(
            shiftInstanceId,
            shift.FactoryHandoverId,
            shift.AiSummaryJson ?? "{}",
            items,
            preview.NextShiftMustKnow,
            preview.Conflicts,
            preview.ReadinessScore,
            shift.AiSummaryJson);
    }

    private async Task<ShiftHandoverPackage> SubmitHandoverCoreAsync(
        Guid shiftInstanceId, Guid membershipId, string? comments, CancellationToken cancellationToken)
    {
        var shift = await LoadShiftForWriteAsync(shiftInstanceId, cancellationToken);
        var items = await dbContext.ShiftOpenItems
            .Where(i => i.ShiftInstanceId == shiftInstanceId)
            .ToListAsync(cancellationToken);

        var gate = ShiftNothingFallsThroughGate.Evaluate(
            items.Where(i => i.RequiresHandover),
            items.Select(i => $"{i.SourceType}:{i.SourceId}"));

        if (!gate.Allowed)
        {
            throw new ShiftOperationsException(
                ShiftOperationsFailureCodes.SilentDropBlocked,
                $"Mandatory handover items missing: {string.Join(", ", gate.MissingMandatoryItems)}");
        }

        var acceptance = await dbContext.ShiftHandoverAcceptances
            .FirstOrDefaultAsync(a => a.ShiftInstanceId == shiftInstanceId, cancellationToken);

        if (acceptance is null)
        {
            acceptance = new ShiftHandoverAcceptance
            {
                Id = Guid.NewGuid(),
                TenantId = shift.TenantId,
                FacilityId = shift.FacilityId,
                ShiftInstanceId = shiftInstanceId,
                FactoryHandoverId = shift.FactoryHandoverId ?? Guid.Empty,
                OutgoingMembershipId = membershipId,
                IncomingMembershipId = Guid.Empty,
                OutgoingSupervisorLabel = shift.SupervisorLabel,
                PreparedAtUtc = DateTimeOffset.UtcNow,
                OutgoingComments = comments,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            dbContext.ShiftHandoverAcceptances.Add(acceptance);
        }
        else
        {
            acceptance.OutgoingComments = comments;
            acceptance.HandedOverAtUtc = DateTimeOffset.UtcNow;
            acceptance.ConcurrencyToken++;
        }

        shift.Status = ShiftInstanceStatuses.HandoverPending;
        shift.HandoverState = "submitted";
        shift.UpdatedAtUtc = DateTimeOffset.UtcNow;
        shift.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShiftHandoverPackage(
            shiftInstanceId,
            shift.FactoryHandoverId,
            shift.AiSummaryJson ?? "{}",
            items,
            BuildNextShiftMustKnow(items),
            ShiftHandoverConflictDetector.Detect(items, new Dictionary<string, string>()),
            shift.HandoverReadinessScore,
            shift.AiSummaryJson);
    }

    private async Task<ShiftHandoverAcceptance> AcceptHandoverCoreAsync(
        Guid shiftInstanceId, AcceptHandoverRequest request, CancellationToken cancellationToken)
    {
        var shift = await LoadShiftForWriteAsync(shiftInstanceId, cancellationToken);
        if (shift.ConcurrencyToken != request.ShiftConcurrencyToken)
        {
            throw new ShiftOperationsException(ShiftOperationsFailureCodes.ConcurrencyConflict, "Concurrency conflict.");
        }

        var acceptance = await dbContext.ShiftHandoverAcceptances
            .FirstOrDefaultAsync(a => a.ShiftInstanceId == shiftInstanceId, cancellationToken)
            ?? throw new ShiftOperationsException(ShiftOperationsFailureCodes.NotFound, "Handover acceptance not found.");

        if (acceptance.AcceptedAtUtc.HasValue)
        {
            throw new ShiftOperationsException(ShiftOperationsFailureCodes.ValidationFailed, "Handover already accepted.");
        }

        acceptance.IncomingMembershipId = request.IncomingMembershipId;
        acceptance.IncomingSupervisorLabel = request.IncomingSupervisorLabel;
        acceptance.IncomingAcknowledgement = request.IncomingAcknowledgement;
        acceptance.AcceptedAtUtc = DateTimeOffset.UtcNow;
        acceptance.Verified = true;
        acceptance.ConcurrencyToken++;

        var openItems = await dbContext.ShiftOpenItems
            .Where(i => i.ShiftInstanceId == shiftInstanceId && i.Status == ShiftOpenItemStatuses.Open)
            .ToListAsync(cancellationToken);

        var nextShift = await dbContext.OperationalShiftInstances
            .Where(s => s.FacilityId == shift.FacilityId &&
                        s.Status == ShiftInstanceStatuses.Planned &&
                        s.ShiftDate >= shift.ShiftDate)
            .OrderBy(s => s.PlannedStartUtc)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var item in openItems.Where(i => i.RequiresHandover))
        {
            item.Status = ShiftOpenItemStatuses.Transferred;
            item.CarriedToShiftInstanceId = nextShift?.Id;
            item.CarryoverCount++;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;
            item.ConcurrencyToken++;

            if (nextShift is not null)
            {
                dbContext.ShiftOpenItems.Add(new ShiftOpenItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = item.TenantId,
                    FacilityId = item.FacilityId,
                    ShiftInstanceId = nextShift.Id,
                    SourceType = item.SourceType,
                    SourceId = item.SourceId,
                    Category = item.Category,
                    Title = item.Title,
                    Description = item.Description,
                    Severity = item.Severity,
                    Status = ShiftOpenItemStatuses.Open,
                    OwnerType = item.OwnerType,
                    OwnerId = item.OwnerId,
                    PrimaryWorkerId = item.PrimaryWorkerId,
                    LinkedAiTaskId = item.LinkedAiTaskId,
                    DigitalWorkCaseId = item.DigitalWorkCaseId,
                    DeadlineUtc = item.DeadlineUtc,
                    RequiresHandover = true,
                    HandoverPriority = item.HandoverPriority,
                    VerificationRequired = item.VerificationRequired,
                    ProvenanceJson = item.ProvenanceJson,
                    CarriedFromShiftInstanceId = shiftInstanceId,
                    CarryoverCount = item.CarryoverCount,
                    CreatedAtUtc = item.CreatedAtUtc,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                });
            }
        }

        shift.Status = ShiftInstanceStatuses.HandedOver;
        shift.HandoverState = "accepted";
        shift.ActualEndUtc = DateTimeOffset.UtcNow;
        shift.UpdatedAtUtc = DateTimeOffset.UtcNow;
        shift.ConcurrencyToken++;

        if (nextShift is not null)
        {
            nextShift.Status = ShiftInstanceStatuses.Active;
            nextShift.ActualStartUtc = DateTimeOffset.UtcNow;
            nextShift.SupervisorMembershipId = request.IncomingMembershipId;
            nextShift.SupervisorLabel = request.IncomingSupervisorLabel;
            nextShift.UpdatedAtUtc = DateTimeOffset.UtcNow;
            nextShift.ConcurrencyToken++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return acceptance;
    }

    private async Task<ShiftOpenItem> DispositionOpenItemCoreAsync(
        Guid shiftInstanceId, Guid itemId, DispositionOpenItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ShiftOpenItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ShiftInstanceId == shiftInstanceId, cancellationToken)
            ?? throw new ShiftOperationsException(ShiftOperationsFailureCodes.NotFound, "Open item not found.");

        if (item.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new ShiftOperationsException(ShiftOperationsFailureCodes.ConcurrencyConflict, "Concurrency conflict.");
        }

        if (item.RequiresHandover && request.Disposition == ShiftHandoverDispositions.Cancelled &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ShiftOperationsException(ShiftOperationsFailureCodes.ValidationFailed, "Reason required.");
        }

        item.Status = request.Disposition switch
        {
            ShiftHandoverDispositions.Resolved => ShiftOpenItemStatuses.Resolved,
            ShiftHandoverDispositions.Transfer => ShiftOpenItemStatuses.Transferred,
            ShiftHandoverDispositions.AcceptedRisk => ShiftOpenItemStatuses.AcceptedRisk,
            ShiftHandoverDispositions.Escalated => ShiftOpenItemStatuses.Escalated,
            ShiftHandoverDispositions.Cancelled => ShiftOpenItemStatuses.CancelledWithReason,
            _ => item.Status,
        };
        if (item.Status != ShiftOpenItemStatuses.Open)
        {
            item.ResolvedAtUtc = DateTimeOffset.UtcNow;
        }

        item.Description = string.IsNullOrWhiteSpace(request.Reason)
            ? item.Description
            : $"{item.Description}\nDisposition: {request.Reason}";
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        item.ConcurrencyToken++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    private async Task<OperationalShiftInstance> LoadShiftForWriteAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        await dbContext.OperationalShiftInstances
            .FirstOrDefaultAsync(s => s.Id == shiftInstanceId, cancellationToken)
            ?? throw new ShiftOperationsException(ShiftOperationsFailureCodes.NotFound, "Shift not found.");

    private async Task<ShiftClosingPreview> BuildPreviewInternalAsync(
        OperationalShiftInstance shift, CancellationToken cancellationToken)
    {
        var candidates = await candidateCollector.CollectAsync(shift, cancellationToken);
        var existing = shift.OpenItems.ToList();
        var merged = MergeCandidates(existing, candidates);
        var conflicts = ShiftHandoverConflictDetector.Detect(merged, new Dictionary<string, string>());
        return new ShiftClosingPreview(
            shift.Id,
            candidates,
            existing,
            conflicts,
            ComputeReadiness(merged, conflicts),
            BuildNextShiftMustKnow(merged),
            true);
    }

    private static List<ShiftOpenItem> MergeCandidates(
        IReadOnlyList<ShiftOpenItem> existing,
        IReadOnlyList<ShiftOpenItem> candidates)
    {
        var map = existing.ToDictionary(i => $"{i.SourceType}:{i.SourceId}", StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var key = $"{candidate.SourceType}:{candidate.SourceId}";
            if (!map.ContainsKey(key))
            {
                map[key] = candidate;
            }
        }

        return map.Values.ToList();
    }

    private async Task UpsertOpenItemsAsync(
        OperationalShiftInstance shift,
        IReadOnlyList<ShiftOpenItem> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var candidate in candidates)
        {
            var exists = await dbContext.ShiftOpenItems.AnyAsync(
                i => i.ShiftInstanceId == shift.Id &&
                     i.SourceType == candidate.SourceType &&
                     i.SourceId == candidate.SourceId,
                cancellationToken);
            if (!exists)
            {
                dbContext.ShiftOpenItems.Add(candidate);
            }
        }
    }

    private static decimal ComputeReadiness(IReadOnlyList<ShiftOpenItem> items, IReadOnlyList<ShiftHandoverConflict> conflicts)
    {
        if (items.Count == 0)
        {
            return 100m;
        }

        var mandatory = items.Count(i => i.RequiresHandover);
        var classified = items.Count(i => !string.IsNullOrWhiteSpace(i.HandoverPriority));
        var baseScore = mandatory == 0 ? 100m : (decimal)classified / mandatory * 100m;
        return Math.Max(0m, Math.Min(100m, baseScore - conflicts.Count * 5m));
    }

    private static List<string> BuildNextShiftMustKnow(IReadOnlyList<ShiftOpenItem> items) =>
        items
            .Where(i => i.RequiresHandover && i.Status == ShiftOpenItemStatuses.Open)
            .OrderByDescending(i => i.HandoverPriority)
            .ThenByDescending(i => i.CarryoverCount)
            .Take(8)
            .Select(i => i.Title)
            .ToList();

    private static string BuildAiSummary(ShiftClosingPreview preview) =>
        JsonSerializer.Serialize(new
        {
            summary = "Vardiya özeti kanıt tabanlı kayıtlardan oluşturuldu.",
            significantChanges = preview.Candidates.Count,
            unresolvedRisks = preview.Conflicts.Count,
            nextShiftPriorities = preview.NextShiftMustKnow,
            grounded = true,
        });
}
