using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.ModelGateway;

public interface IModelGatewayService
{
    Task<ModelGatewayResponse> InvokeAsync(
        Guid facilityId,
        Guid tenantId,
        ModelGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelGatewayAuditSummary>> ListAuditRecordsAsync(
        Guid facilityId,
        Guid? digitalWorkCaseId,
        int take = 20,
        CancellationToken cancellationToken = default);
}

public sealed class ModelGatewayService : IModelGatewayService
{
    private readonly NevaxisZentraDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly Dictionary<string, IModelProvider> providers;

    public ModelGatewayService(
        NevaxisZentraDbContext db,
        TenantDatabaseSession tenantDatabaseSession,
        IEnumerable<IModelProvider> providers)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.providers = providers.ToDictionary(p => p.IntelligencePath, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ModelGatewayResponse> InvokeAsync(
        Guid facilityId,
        Guid tenantId,
        ModelGatewayRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => InvokeCoreAsync(facilityId, tenantId, request, ct),
            cancellationToken);

    public Task<IReadOnlyList<ModelGatewayAuditSummary>> ListAuditRecordsAsync(
        Guid facilityId,
        Guid? digitalWorkCaseId,
        int take = 20,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListAuditCoreAsync(facilityId, digitalWorkCaseId, take, ct),
            cancellationToken);

    private async Task<ModelGatewayResponse> InvokeCoreAsync(
        Guid facilityId,
        Guid tenantId,
        ModelGatewayRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var policy = await db.ModelGatewayBudgetPolicyRecords.AsNoTracking()
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId && p.IsActive, cancellationToken);

        if (ModelCostGovernanceEngine.IsRequestClassRestricted(request.RequestClassification, policy))
        {
            throw new ModelGatewayException(
                ModelGatewayFailureCodes.PolicyRejected,
                $"Request class {request.RequestClassification} is restricted by budget policy.");
        }

        var now = DateTimeOffset.UtcNow;
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var dailySpend = await SumCostAsync(facilityId, dayStart, cancellationToken);
        var monthlySpend = await SumCostAsync(facilityId, monthStart, cancellationToken);
        var caseSpend = request.DigitalWorkCaseId.HasValue
            ? await SumCaseCostAsync(facilityId, request.DigitalWorkCaseId.Value, cancellationToken)
            : 0m;

        var budget = ModelCostGovernanceEngine.Evaluate(dailySpend, monthlySpend, caseSpend, policy);
        var routing = ModelRouterEngine.Route(request with { CostBudgetRemaining = budget.DailyRemaining }, budget);

        if (!providers.TryGetValue(routing.IntelligencePath, out var provider) || !provider.IsAvailable)
        {
            routing = ModelRouterEngine.Fallback(routing, "provider-unavailable");
            provider = providers[routing.IntelligencePath];
        }

        var invokeResult = await provider.InvokeAsync(new ModelProviderInvokeRequest(request, routing), cancellationToken);
        if (!invokeResult.Succeeded)
        {
            routing = ModelRouterEngine.Fallback(routing, invokeResult.FailureState ?? "provider-failure");
            if (providers.TryGetValue(routing.IntelligencePath, out var fallbackProvider))
            {
                invokeResult = await fallbackProvider.InvokeAsync(new ModelProviderInvokeRequest(request, routing), cancellationToken);
            }
        }

        var validation = OperationalProposalValidator.Validate(invokeResult.Proposal);
        stopwatch.Stop();

        var auditId = Guid.NewGuid();
        db.ModelGatewayAuditRecords.Add(new ModelGatewayAuditRecord
        {
            Id = auditId,
            TenantId = tenantId,
            FacilityId = facilityId,
            WorkerId = request.WorkerId,
            DigitalWorkCaseId = request.DigitalWorkCaseId,
            ContextSnapshotId = request.Context.ContextSnapshotId,
            RequestClassification = request.RequestClassification,
            IntelligencePath = routing.IntelligencePath,
            ProviderId = routing.ProviderId,
            ModelName = routing.ModelName,
            ModelVersion = routing.ModelVersion,
            RoutingReason = routing.RoutingReason,
            UsedFallback = routing.UsedFallback,
            ResponseClassification = invokeResult.ResponseClassification,
            ValidationStatus = validation.ValidationStatus,
            StructuredProposalJson = invokeResult.Proposal is null ? "{}" : JsonSerializer.Serialize(invokeResult.Proposal),
            ValidationResultJson = JsonSerializer.Serialize(validation.RejectionReasons),
            LatencyMs = stopwatch.ElapsedMilliseconds,
            EstimatedCost = invokeResult.EstimatedCost,
            InputTokens = invokeResult.InputTokens,
            OutputTokens = invokeResult.OutputTokens,
            CreatedAtUtc = now,
        });

        if (invokeResult.EstimatedCost > 0)
        {
            var existingUsage = await db.ModelGatewayCostUsageRecords
                .FirstOrDefaultAsync(
                    r => r.TenantId == tenantId
                        && r.FacilityId == facilityId
                        && r.WorkerId == request.WorkerId
                        && r.WindowStartUtc == dayStart
                        && r.BudgetWindow == "daily",
                    cancellationToken);

            if (existingUsage is null)
            {
                db.ModelGatewayCostUsageRecords.Add(new ModelGatewayCostUsageRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    WorkerId = request.WorkerId,
                    DigitalWorkCaseId = request.DigitalWorkCaseId,
                    ProviderId = routing.ProviderId,
                    ModelName = routing.ModelName,
                    RequestClassification = request.RequestClassification,
                    RequestCount = 1,
                    InputTokens = invokeResult.InputTokens,
                    OutputTokens = invokeResult.OutputTokens,
                    EstimatedCost = invokeResult.EstimatedCost,
                    BudgetWindow = "daily",
                    WindowStartUtc = dayStart,
                    CreatedAtUtc = now,
                });
            }
            else
            {
                existingUsage.RequestCount += 1;
                existingUsage.InputTokens += invokeResult.InputTokens;
                existingUsage.OutputTokens += invokeResult.OutputTokens;
                existingUsage.EstimatedCost += invokeResult.EstimatedCost;
                existingUsage.DigitalWorkCaseId = request.DigitalWorkCaseId ?? existingUsage.DigitalWorkCaseId;
                existingUsage.ProviderId = routing.ProviderId;
                existingUsage.ModelName = routing.ModelName;
                existingUsage.RequestClassification = request.RequestClassification;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ModelGatewayResponse(
            invokeResult.Succeeded && validation.Accepted,
            invokeResult.FailureState ?? (validation.Accepted ? null : ModelGatewayFailureStates.SchemaFailure),
            validation.Accepted ? null : ModelGatewayFailureCodes.SchemaRejected,
            routing,
            validation.SanitizedProposal,
            validation,
            stopwatch.ElapsedMilliseconds,
            invokeResult.EstimatedCost,
            auditId.ToString());
    }

    private async Task<decimal> SumCostAsync(Guid facilityId, DateTimeOffset since, CancellationToken cancellationToken) =>
        await db.ModelGatewayCostUsageRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.WindowStartUtc >= since)
            .SumAsync(r => r.EstimatedCost, cancellationToken);

    private async Task<decimal> SumCaseCostAsync(Guid facilityId, Guid caseId, CancellationToken cancellationToken) =>
        await db.ModelGatewayCostUsageRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.DigitalWorkCaseId == caseId)
            .SumAsync(r => r.EstimatedCost, cancellationToken);

    private async Task<IReadOnlyList<ModelGatewayAuditSummary>> ListAuditCoreAsync(
        Guid facilityId,
        Guid? digitalWorkCaseId,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.ModelGatewayAuditRecords.AsNoTracking()
            .Where(r => r.FacilityId == facilityId);

        if (digitalWorkCaseId.HasValue)
        {
            query = query.Where(r => r.DigitalWorkCaseId == digitalWorkCaseId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(take)
            .Select(r => new ModelGatewayAuditSummary(
                r.Id,
                r.ProviderId,
                r.ModelName,
                r.IntelligencePath,
                r.RequestClassification,
                r.RoutingReason,
                r.UsedFallback,
                r.ValidationStatus,
                r.LatencyMs,
                r.EstimatedCost,
                r.ContextSnapshotId,
                r.DigitalWorkCaseId,
                r.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
