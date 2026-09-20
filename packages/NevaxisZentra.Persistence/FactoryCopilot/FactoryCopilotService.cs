using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.ModelGateway;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.FactoryCopilot;

public interface IFactoryCopilotService
{
    Task<FactoryCopilotQueryResult> QueryAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FactoryCopilotSuggestedQuestionsResult> GetSuggestedQuestionsAsync(
        Guid facilityId,
        FactoryCopilotContextInput context,
        CancellationToken cancellationToken = default);
}

public sealed class FactoryCopilotService(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IContextKnowledgeService contextKnowledge,
    IThroughputIntelligenceService throughputIntelligence,
    IScenarioRecoveryService scenarioRecovery,
    IDigitalWorkforceWorksService worksService,
    IModelGatewayService modelGateway,
    IAuditWriter auditWriter) : IFactoryCopilotService
{
    private readonly FactoryCopilotToolExecutor toolExecutor = new(
        dbContext,
        contextKnowledge,
        throughputIntelligence,
        scenarioRecovery,
        worksService);

    public Task<FactoryCopilotQueryResult> QueryAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => QueryCoreAsync(facilityId, tenantId, subjectId, request, ct),
            cancellationToken);

    public Task<FactoryCopilotSuggestedQuestionsResult> GetSuggestedQuestionsAsync(
        Guid facilityId,
        FactoryCopilotContextInput context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var resolved = await ResolveContextAsync(facilityId, context, ct);
                return new FactoryCopilotSuggestedQuestionsResult(
                    FactoryCopilotIntentClassifier.SuggestQuestions(context),
                    BuildContextHeader(context, resolved));
            },
            cancellationToken);

    private async Task<FactoryCopilotQueryResult> QueryCoreAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var queryId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(request.QueryText))
        {
            throw new FactoryCopilotException(
                FactoryCopilotFailureCodes.ValidationFailed,
                "Query text is required.");
        }

        var classification = FactoryCopilotIntentClassifier.Classify(request.QueryText, request.Context);
        var resolved = await ResolveContextAsync(facilityId, request.Context, cancellationToken);
        var contextQuality = await AssessContextQualityAsync(
            facilityId, tenantId, subjectId, resolved, request, cancellationToken);
        var contextHeader = BuildContextHeader(request.Context, resolved);

        if (classification.Intent == FactoryCopilotIntents.Unsupported)
        {
            var unsupported = FactoryCopilotResponseComposer.Compose(
                classification.Intent,
                new FactoryCopilotToolBundle(),
                resolved,
                contextQuality);
            stopwatch.Stop();
            return BuildResult(
                queryId,
                createdAt,
                classification,
                false,
                null,
                unsupported,
                [],
                contextQuality,
                contextHeader,
                request.Context,
                null,
                null,
                stopwatch.ElapsedMilliseconds);
        }

        var (bundle, toolInvocations) = await toolExecutor.ExecuteAsync(
            facilityId,
            tenantId,
            subjectId,
            classification.Intent,
            classification.RequiredTools,
            request.Context,
            resolved,
            request.QueryText,
            cancellationToken);

        string? modelAnswer = null;
        string? providerId = null;
        string? modelName = null;
        var modelInvoked = false;
        string? failureState = null;

        if (classification.RequiresModel &&
            contextQuality == FactoryCopilotContextQualityStates.Sufficient)
        {
            try
            {
                var primaryConstraintCode = ResolvePrimaryConstraintCode(bundle.ThroughputOverview);
                var modelResponse = await modelGateway.InvokeAsync(
                    facilityId,
                    tenantId,
                    new ModelGatewayRequest(
                        tenantId,
                        facilityId,
                        null,
                        request.Context.DigitalWorkCaseId,
                        "factory-copilot-synthesis",
                        classification.Intent,
                        new ModelGatewayContextReference(
                            null,
                            $"{resolved.LineCode} throughput and bounded operational context",
                            request.Context.DigitalWorkCaseId,
                            primaryConstraintCode,
                            bundle.SimulationSnapshot?.PredictedGoodThroughputPerHour.ToString("0.#", CultureInfo.InvariantCulture),
                            contextQuality),
                        1,
                        true),
                    cancellationToken);

                modelInvoked = modelResponse.Succeeded;
                providerId = modelResponse.Routing.ProviderId;
                modelName = modelResponse.Routing.ModelName;
                modelAnswer = modelResponse.Proposal?.Hypothesis;
                failureState = modelResponse.FailureState;
            }
            catch (ModelGatewayException ex)
            {
                failureState = ex.FailureCode;
            }
        }

        var composed = FactoryCopilotResponseComposer.Compose(
            classification.Intent,
            bundle,
            resolved,
            contextQuality,
            modelAnswer);

        var (accepted, rejectionReason, guarded) = FactoryCopilotHallucinationGuard.Validate(
            composed,
            bundle,
            request.QueryText);

        if (!accepted)
        {
            failureState ??= rejectionReason;
            composed = guarded;
        }

        stopwatch.Stop();
        var provenanceTools = toolInvocations;
        var result = BuildResult(
            queryId,
            createdAt,
            classification,
            modelInvoked,
            failureState,
            composed,
            provenanceTools,
            contextQuality,
            contextHeader,
            request.Context,
            providerId,
            modelName,
            stopwatch.ElapsedMilliseconds);

        await WriteAuditAsync(facilityId, tenantId, subjectId, result, cancellationToken);
        return result;
    }

    private async Task<FactoryCopilotResolvedContext> ResolveContextAsync(
        Guid facilityId,
        FactoryCopilotContextInput context,
        CancellationToken cancellationToken)
    {
        ProductionLine? line = null;
        if (context.LineId.HasValue)
        {
            line = await dbContext.ProductionLines.AsNoTracking()
                .FirstOrDefaultAsync(l => l.FacilityId == facilityId && l.Id == context.LineId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(context.LineCode))
        {
            line = await dbContext.ProductionLines.AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.FacilityId == facilityId && l.Code == context.LineCode,
                    cancellationToken);
        }
        else if (context.EntryPoint is FactoryCopilotEntryPoints.Fabrika
                 or FactoryCopilotEntryPoints.Line
                 or FactoryCopilotEntryPoints.Scenario)
        {
            line = await dbContext.ProductionLines.AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.FacilityId == facilityId && l.Code == MixedProductionFixtureCodes.LineCode,
                    cancellationToken);
        }

        if (line is null)
        {
            throw new FactoryCopilotException(
                FactoryCopilotFailureCodes.InsufficientContext,
                "Line context could not be resolved.");
        }

        string? resourceCode = context.ResourceCode;
        if (context.ResourceId.HasValue && string.IsNullOrWhiteSpace(resourceCode))
        {
            resourceCode = await dbContext.ManufacturingResources.AsNoTracking()
                .Where(r => r.FacilityId == facilityId && r.Id == context.ResourceId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new FactoryCopilotResolvedContext(
            line.Id,
            line.Code,
            line.Name,
            resourceCode,
            context.OrderReference);
    }

    private async Task<string> AssessContextQualityAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotResolvedContext resolved,
        FactoryCopilotQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var package = await contextKnowledge.BuildWorkerContextAsync(
                new ContextBuildRequest(
                    new ContextScope(
                        tenantId,
                        facilityId,
                        ContextKnowledgeScopeEntityTypes.Line,
                        resolved.LineId.ToString(),
                        resolved.LineId,
                        DigitalWorkCaseId: request.Context.DigitalWorkCaseId),
                    new ContextBudget(MaxEventCount: 50, MaxKnowledgeRecords: 5),
                    request.Context.DigitalWorkCaseId),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(package.FailureState))
            {
                return FactoryCopilotContextQualityStates.SourceUnavailable;
            }

            if (package.Metrics.ConflictCount > 0)
            {
                return FactoryCopilotContextQualityStates.Conflicted;
            }

            if (package.Metrics.StaleCount > 0)
            {
                return FactoryCopilotContextQualityStates.Stale;
            }

            await contextKnowledge.CreateSnapshotAsync(
                facilityId,
                tenantId,
                new ContextSnapshotRequest(
                    new ContextScope(
                        tenantId,
                        facilityId,
                        ContextKnowledgeScopeEntityTypes.Line,
                        resolved.LineId.ToString(),
                        resolved.LineId,
                        DigitalWorkCaseId: request.Context.DigitalWorkCaseId),
                    package,
                    request.Context.DigitalWorkCaseId,
                    subjectId,
                    $"factory-copilot-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"),
                cancellationToken);

            return FactoryCopilotContextQualityStates.Sufficient;
        }
        catch
        {
            return FactoryCopilotContextQualityStates.Insufficient;
        }
    }

    private static FactoryCopilotContextHeader BuildContextHeader(
        FactoryCopilotContextInput context,
        FactoryCopilotResolvedContext resolved) =>
        new(
            context.EntryPoint,
            FactoryLabel: "Pilot tesis",
            LineLabel: resolved.LineCode,
            ResourceLabel: resolved.ResourceCode,
            OrderLabel: resolved.OrderReference,
            WorkLabel: context.DigitalWorkCaseId?.ToString(),
            ScenarioLabel: context.ScenarioRunId?.ToString());

    private static FactoryCopilotQueryResult BuildResult(
        Guid queryId,
        DateTimeOffset createdAt,
        FactoryCopilotIntentClassifier.IntentClassification classification,
        bool modelInvoked,
        string? failureState,
        FactoryCopilotStructuredResponse response,
        IReadOnlyList<FactoryCopilotToolInvocation> tools,
        string contextQuality,
        FactoryCopilotContextHeader contextHeader,
        FactoryCopilotContextInput requestContext,
        string? providerId,
        string? modelName,
        long latencyMs) =>
        new(
            classification.Intent,
            classification.RoutingClass,
            modelInvoked,
            failureState,
            response,
            new FactoryCopilotProvenance(
                null,
                tools,
                response.Evidence.Select(e => e.ReferenceId).Distinct().ToList(),
                providerId,
                modelName,
                modelInvoked,
                classification.RoutingClass,
                $"Intent {classification.Intent} routed as {classification.RoutingClass}",
                contextQuality,
                latencyMs),
            FactoryCopilotIntentClassifier.SuggestQuestions(requestContext),
            contextHeader,
            queryId,
            createdAt);

    private static string? ResolvePrimaryConstraintCode(ThroughputIntelligenceOverview? overview)
    {
        if (overview?.Constraints is not { Count: > 0 } constraints)
        {
            return null;
        }

        foreach (var constraint in constraints)
        {
            if (constraint.Role == "primary")
            {
                return constraint.StationCode;
            }
        }

        return null;
    }

    private Task<AuditWriteResult> WriteAuditAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryResult result,
        CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(
            new AuditWriteRequest(
                "factory-copilot.query",
                result.Intent,
                "factory-copilot",
                AuditOutcome.Success,
                result.QueryId.ToString(),
                FactoryCopilotPermissionKeys.Query,
                Metadata: new Dictionary<string, object?>
                {
                    ["routingClass"] = result.RoutingClass,
                    ["modelInvoked"] = result.ModelInvoked,
                    ["contextQuality"] = result.Provenance.ContextQualityState,
                    ["toolCount"] = result.Provenance.Tools.Count,
                    ["latencyMs"] = result.Provenance.TotalLatencyMs,
                    ["evidenceCount"] = result.Response.Evidence.Count,
                    ["failureState"] = result.FailureState,
                    ["summary"] = JsonSerializer.Serialize(new
                    {
                        result.Intent,
                        result.Response.Answer,
                        tools = result.Provenance.Tools.Select(t => t.ToolId),
                    }),
                }),
            cancellationToken);
}

public static class FactoryCopilotServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraFactoryCopilot(this IServiceCollection services)
    {
        services.AddScoped<IFactoryCopilotService, FactoryCopilotService>();
        return services;
    }
}
