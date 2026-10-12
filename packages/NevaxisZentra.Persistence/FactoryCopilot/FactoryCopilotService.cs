using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.ContinuousOptimization;
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
    IContinuousOptimizationService continuousOptimization,
    IModelGatewayService modelGateway,
    IAuditWriter auditWriter,
    ILogger<FactoryCopilotService> logger) : IFactoryCopilotService
{
    private readonly FactoryCopilotToolExecutor toolExecutor = new(
        dbContext,
        contextKnowledge,
        throughputIntelligence,
        scenarioRecovery,
        worksService,
        continuousOptimization);

    public Task<FactoryCopilotQueryResult> QueryAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryRequest request,
        CancellationToken cancellationToken = default) =>
        // A write session: the model gateway's usage/cost/audit rows and the context snapshot are written during a query,
        // and a read session rolls them back.
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => QueryCoreAsync(facilityId, tenantId, subjectId, request, ct),
            cancellationToken);

    public Task<FactoryCopilotSuggestedQuestionsResult> GetSuggestedQuestionsAsync(
        Guid facilityId,
        FactoryCopilotContextInput context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                FactoryCopilotResolvedContext? resolved = null;
                try
                {
                    resolved = await ResolveContextAsync(facilityId, context, ct);
                }
                catch (FactoryCopilotException ex) when (ex.FailureCode == FactoryCopilotFailureCodes.InsufficientContext)
                {
                    // A facility without a resolvable line still gets the suggestions and an honest, empty header;
                    // the query itself reports the missing context.
                }

                return new FactoryCopilotSuggestedQuestionsResult(
                    FactoryCopilotIntentClassifier.SuggestQuestions(context),
                    resolved is null
                        ? new FactoryCopilotContextHeader(
                            context.EntryPoint,
                            FactoryLabel: "Pilot tesis",
                            LineLabel: null,
                            ResourceLabel: null,
                            OrderLabel: null,
                            WorkLabel: context.DigitalWorkCaseId?.ToString(),
                            ScenarioLabel: context.ScenarioRunId?.ToString())
                        : BuildContextHeader(context, resolved));
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

        if (request.QueryText.Length > FactoryCopilotGrounding.MaxQueryCharacters)
        {
            throw new FactoryCopilotException(
                FactoryCopilotFailureCodes.ValidationFailed,
                $"Query text exceeds {FactoryCopilotGrounding.MaxQueryCharacters} characters.");
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
            var unsupportedResult = BuildResult(
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
            var unsupportedAudit = await WriteAuditAsync(facilityId, tenantId, subjectId, unsupportedResult, cancellationToken);
            if (!unsupportedAudit.Succeeded)
            {
                AuditNotWritten(logger, unsupportedAudit.FailureCode ?? "unknown", null);
            }

            return unsupportedResult;
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
        IReadOnlyList<string>? modelInference = null;
        IReadOnlyList<string>? citedFacts = null;
        string? providerId = null;
        string? modelName = null;
        var modelInvoked = false;
        string? failureState = null;

        if (classification.RequiresModel)
        {
            if (FactoryCopilotHallucinationGuard.IsInjection(request.QueryText))
            {
                // Never forward an instruction-override attempt to the provider.
                failureState = FactoryCopilotAiStates.InjectionBlocked;
            }
            else if (!IsModelEligible(classification.Intent, contextQuality, bundle))
            {
                failureState = FactoryCopilotAiStates.InsufficientData;
            }
            else
            {
                var facts = FactoryCopilotGrounding.BuildFacts(bundle, resolved, contextQuality);
                var question = FactoryCopilotGrounding.SanitizeQuestion(request.QueryText);
                var primaryConstraintCode = ResolvePrimaryConstraintCode(bundle.ThroughputOverview);
                try
                {
                    var modelResponse = await modelGateway.InvokeAsync(
                        facilityId,
                        tenantId,
                        new ModelGatewayRequest(
                            tenantId,
                            facilityId,
                            null,
                            request.Context.DigitalWorkCaseId,
                            ModelGatewayRequestClassifications.GroundedFactorySynthesis,
                            classification.Intent,
                            new ModelGatewayContextReference(
                                null,
                                $"{resolved.LineCode} throughput and bounded operational context",
                                request.Context.DigitalWorkCaseId,
                                primaryConstraintCode,
                                bundle.SimulationSnapshot?.PredictedGoodThroughputPerHour.ToString("0.#", CultureInfo.InvariantCulture),
                                contextQuality),
                            1,
                            true,
                            Grounded: new ModelGroundedInput(question, facts)),
                        cancellationToken);

                    var aiState = FactoryCopilotGrounding.MapAiState(modelResponse);
                    if (aiState is not null)
                    {
                        // The real model did not answer: report why. A fallback or synthetic proposal is never an answer.
                        failureState = aiState;
                    }
                    else if (modelResponse.Proposal is { } proposal)
                    {
                        providerId = modelResponse.Routing.ProviderId;
                        modelName = modelResponse.Routing.ModelName;
                        if (proposal.EvidenceRefs.Contains(OpenAiInsufficientDataMarker, StringComparer.Ordinal))
                        {
                            modelInvoked = true;
                            failureState = FactoryCopilotAiStates.InsufficientData;
                            modelAnswer = proposal.Hypothesis;
                        }
                        else if (FactoryCopilotGrounding.FindUngroundedClaim(proposal.Hypothesis, facts, question) is not null)
                        {
                            modelInvoked = true;
                            failureState = FactoryCopilotAiStates.UngroundedResponse;
                        }
                        else
                        {
                            modelInvoked = true;
                            modelAnswer = proposal.Hypothesis;
                            modelInference = proposal.Assumptions;
                            citedFacts = facts
                                .Where(f => proposal.EvidenceRefs.Contains(f.Id, StringComparer.OrdinalIgnoreCase))
                                .Select(f => f.Text)
                                .ToList();
                        }
                    }
                    else
                    {
                        failureState = FactoryCopilotAiStates.InvalidOutput;
                    }
                }
                catch (ModelGatewayException)
                {
                    failureState = FactoryCopilotAiStates.ProviderUnavailable;
                }
            }
        }

        var composed = FactoryCopilotResponseComposer.Compose(
            classification.Intent,
            bundle,
            resolved,
            contextQuality,
            modelAnswer,
            modelInference,
            modelName,
            failureState,
            citedFacts);

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

        var audit = await WriteAuditAsync(facilityId, tenantId, subjectId, result, cancellationToken);
        if (!audit.Succeeded)
        {
            // The answer is still returned, but a missing audit record must be visible to operators.
            AuditNotWritten(logger, audit.FailureCode ?? "unknown", null);
        }

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

            if (package.FailureState == ContextKnowledgeFailureStates.InsufficientContext)
            {
                // No recent trusted state for the line: "insufficient", which is not the same as an unavailable source.
                return FactoryCopilotContextQualityStates.Insufficient;
            }

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed assessment is an unavailable source, not "no data": the user must not be told the data is missing.
            // Never log the message (it can carry data); the type and the failing step are enough to diagnose.
            ContextAssessmentFailed(logger, ex.GetType().Name, null);
            return FactoryCopilotContextQualityStates.SourceUnavailable;
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

    /// <summary>
    /// The model is asked only with trusted context. A free-form question about the available period may also be
    /// answered when there is no live (recent) context but the machine aggregate has recorded events; conflicted or
    /// unavailable sources never reach the model.
    /// </summary>
    private static bool IsModelEligible(string intent, string contextQuality, FactoryCopilotToolBundle bundle) =>
        contextQuality == FactoryCopilotContextQualityStates.Sufficient
            ? bundle.ThroughputOverview is not null || bundle.MachinePerformance is not null
            : intent == FactoryCopilotIntents.GeneralQuestion
              && contextQuality == FactoryCopilotContextQualityStates.Insufficient
              && bundle.MachinePerformance is { Machines.Count: > 0 };

    private static readonly Action<ILogger, string, Exception?> AuditNotWritten = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4102, nameof(AuditNotWritten)),
        "Factory Copilot query audit record was not written: {FailureCode}");

    private static readonly Action<ILogger, string, Exception?> ContextAssessmentFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4101, nameof(ContextAssessmentFailed)),
        "Factory Copilot context assessment failed: {ExceptionType}");

    private const string OpenAiInsufficientDataMarker = "insufficient-data";

    /// <summary>
    /// A blocked, unavailable, ungrounded or otherwise unanswered query is a failure of the assistant, not a success. The
    /// reason is in the failureState metadata.
    /// </summary>
    internal static AuditOutcome ResolveAuditOutcome(string? failureState) =>
        string.IsNullOrEmpty(failureState) || failureState == FactoryCopilotAiStates.InsufficientData
            ? AuditOutcome.Success
            : AuditOutcome.Failure;

    private Task<AuditWriteResult> WriteAuditAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        FactoryCopilotQueryResult result,
        CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(
            new AuditWriteRequest(
                AuditEventTypes.FactoryCopilotQueryAnswered,
                AuditActions.FactoryCopilotQuery,
                "factorycopilot",
                ResolveAuditOutcome(result.FailureState),
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
                    ["providerId"] = result.Provenance.ProviderId,
                    ["modelName"] = result.Provenance.ModelName,
                    // Which tools ran and how long the answer is: never the question or the answer text.
                    ["toolIds"] = string.Join(',', result.Provenance.Tools.Select(t => t.ToolId)).Truncate256(),
                    ["answerLength"] = result.Response.Answer.Length,
                }),
            cancellationToken);
}

internal static class FactoryCopilotAuditStringExtensions
{
    /// <summary>audit metadata strings are capped at 256 characters.</summary>
    public static string Truncate256(this string value) => value.Length <= 256 ? value : value[..256];
}

public static class FactoryCopilotServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraFactoryCopilot(this IServiceCollection services)
    {
        services.AddScoped<IFactoryCopilotService, FactoryCopilotService>();
        return services;
    }
}
