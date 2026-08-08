using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.AiUsage;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Export;

public static class Export4LEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapExport4LEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var casesGroup = endpoints.MapGroup("/api/export/cases").RequireAuthorization();
        var caseAi = casesGroup.MapGroup("/{exportCaseId:guid}/ai");
        caseAi.MapPost("/analyze", AnalyzeExportCaseAsync);
        caseAi.MapGet("/runs", ListExportCaseAiRunsAsync);
        caseAi.MapGet("/runs/{runId:guid}", GetExportCaseAiRunAsync);
        caseAi.MapPost("/compliance-explanation", ExplainComplianceAsync);
        caseAi.MapPost("/next-actions", SuggestNextActionsAsync);
        caseAi.MapGet("/recommendations", ListRecommendationsAsync);
        caseAi.MapPost("/recommendations/{id:guid}/accept", AcceptRecommendationAsync);
        caseAi.MapPost("/recommendations/{id:guid}/reject", RejectRecommendationAsync);
        caseAi.MapPost("/recommendations/{id:guid}/dismiss", DismissRecommendationAsync);

        var researchGroup = endpoints.MapGroup("/api/export/country-research").RequireAuthorization();
        var researchAi = researchGroup.MapGroup("/{researchCaseId:guid}/ai");
        researchAi.MapPost("/analyze", AnalyzeResearchCaseAsync);
        researchAi.MapGet("/runs", ListResearchCaseAiRunsAsync);

        endpoints.MapGet("/api/ai/usage/current", GetCurrentAiUsageAsync).RequireAuthorization();

        return endpoints;
    }

    // ─── Export case AI ─────────────────────────────────────────────────────────

    private static async Task<IResult> AnalyzeExportCaseAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] AnalyzeExportCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRun, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!TryParseAnalysisRunType(body.RunType, out var runType))
        {
            return ValidationProblem($"Unknown run type '{body.RunType}'.");
        }

        var result = await writeRepository.StartAnalysisAsync(
            BuildContext(access.Access!, correlationContext),
            new StartExportAiAnalysisCommand(exportCaseId, null, runType),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportAiAnalysisWriteSuccessDto(
            result.AnalysisRunId!.Value,
            result.RunVersion!.Value));
    }

    private static async Task<IResult> ListExportCaseAiRunsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListRunsForExportCaseAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ExportCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(MapRun).ToArray());
    }

    private static async Task<IResult> GetExportCaseAiRunAsync(
        Guid exportCaseId,
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetRunDetailAsync(
            BuildContext(access.Access!, correlationContext), runId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return AnalysisRunNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        if (result.Detail!.Run.ExportCaseId != exportCaseId)
        {
            return AnalysisRunNotFound();
        }

        return TypedResults.Ok(MapRunDetail(result.Detail));
    }

    private static async Task<IResult> ExplainComplianceAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRun, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.StartAnalysisAsync(
            BuildContext(access.Access!, correlationContext),
            new StartExportAiAnalysisCommand(
                exportCaseId,
                null,
                ExportAiAnalysisRunType.ComplianceExplanation),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportAiAnalysisWriteSuccessDto(
            result.AnalysisRunId!.Value,
            result.RunVersion!.Value));
    }

    private static async Task<IResult> SuggestNextActionsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRun, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.StartAnalysisAsync(
            BuildContext(access.Access!, correlationContext),
            new StartExportAiAnalysisCommand(
                exportCaseId,
                null,
                ExportAiAnalysisRunType.NextActions),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportAiAnalysisWriteSuccessDto(
            result.AnalysisRunId!.Value,
            result.RunVersion!.Value));
    }

    private static async Task<IResult> ListRecommendationsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListRecommendationsForExportCaseAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(MapRecommendation).ToArray());
    }

    private static Task<IResult> AcceptRecommendationAsync(
        Guid exportCaseId,
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] ReviewExportAiRecommendationRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        ReviewRecommendationAsync(
            exportCaseId,
            id,
            facilityId,
            body,
            ExportAiReviewDecision.Accepted,
            facilityAccessResolver,
            writeRepository,
            correlationContext,
            cancellationToken);

    private static Task<IResult> RejectRecommendationAsync(
        Guid exportCaseId,
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] ReviewExportAiRecommendationRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        ReviewRecommendationAsync(
            exportCaseId,
            id,
            facilityId,
            body,
            ExportAiReviewDecision.Rejected,
            facilityAccessResolver,
            writeRepository,
            correlationContext,
            cancellationToken);

    private static Task<IResult> DismissRecommendationAsync(
        Guid exportCaseId,
        Guid id,
        [FromQuery] Guid? facilityId,
        [FromBody] ReviewExportAiRecommendationRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        ReviewRecommendationAsync(
            exportCaseId,
            id,
            facilityId,
            body,
            ExportAiReviewDecision.Dismissed,
            facilityAccessResolver,
            writeRepository,
            correlationContext,
            cancellationToken);

    private static async Task<IResult> ReviewRecommendationAsync(
        Guid exportCaseId,
        Guid recommendationId,
        Guid? facilityId,
        ReviewExportAiRecommendationRequest body,
        ExportAiReviewDecision decision,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiReview, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.ReviewRecommendationAsync(
            BuildContext(access.Access!, correlationContext),
            new ReviewExportAiRecommendationCommand(
                exportCaseId,
                recommendationId,
                body.ExpectedVersion,
                decision,
                body.ReviewNote),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportAiRecommendationReviewSuccessDto(
            result.RecommendationVersion!.Value,
            result.CaseVersion!.Value));
    }

    // ─── Country research AI ────────────────────────────────────────────────────

    private static async Task<IResult> AnalyzeResearchCaseAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRun, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.StartAnalysisAsync(
            BuildContext(access.Access!, correlationContext),
            new StartExportAiAnalysisCommand(
                null,
                researchCaseId,
                ExportAiAnalysisRunType.CountryResearchAssist),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportAiAnalysisWriteSuccessDto(
            result.AnalysisRunId!.Value,
            result.RunVersion!.Value));
    }

    private static async Task<IResult> ListResearchCaseAiRunsAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportAiReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListRunsForResearchCaseAsync(
            BuildContext(access.Access!, correlationContext), researchCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ResearchCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(MapRun).ToArray());
    }

    // ─── AI usage ───────────────────────────────────────────────────────────────

    private static async Task<IResult> GetCurrentAiUsageAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiUsageMeteringService usageMeteringService,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.AiRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var summary = await usageMeteringService.GetCurrentUsageAsync(
            access.Access!.TenantId,
            access.Access!.FacilityId,
            cancellationToken);

        return TypedResults.Ok(new AiUsageSummaryDto(
            summary.CreditsConsumedThisPeriod,
            summary.MonthlyQuota,
            summary.UsagePercent,
            summary.WarningLevel));
    }

    // ─── Mapping ────────────────────────────────────────────────────────────────

    private static ExportAiAnalysisRunDto MapRun(ExportAiAnalysisRunReadModel model) =>
        new(
            model.Id,
            model.ExportCaseId,
            model.CountryResearchCaseId,
            model.ContextSnapshotId,
            model.RunType,
            model.Status,
            model.Provider,
            model.Model,
            model.PromptVersion,
            model.StartedAtUtc,
            model.CompletedAtUtc,
            model.RequestedBySubjectId,
            model.InputUsageUnits,
            model.OutputUsageUnits,
            model.AiCreditsConsumed,
            model.FailureCode,
            model.FailureMessage,
            model.ReasoningSummary,
            model.Version);

    private static ExportAiFindingDto MapFinding(ExportAiFindingReadModel model) =>
        new(
            model.Id,
            model.AnalysisRunId,
            model.Category,
            model.Title,
            model.Summary,
            model.Severity,
            model.Confidence,
            model.SourceType,
            model.SourceReferences.Select(MapSourceReference).ToArray(),
            model.RelatedRequirementId,
            model.RelatedDocumentId,
            model.RelatedShipmentId,
            model.Actionable,
            model.HumanReviewRequired,
            model.CreatedAtUtc);

    private static ExportAiRecommendationDto MapRecommendation(ExportAiRecommendationReadModel model) =>
        new(
            model.Id,
            model.AnalysisRunId,
            model.Title,
            model.Description,
            model.Priority,
            model.Confidence,
            model.ReasoningSummary,
            model.ProposedActionType,
            model.ProposedActionPayloadJson,
            model.Status,
            model.CreatedAtUtc,
            model.ReviewedAtUtc,
            model.ReviewedBySubjectId,
            model.ReviewDecision,
            model.ReviewNote,
            model.Version);

    private static ExportAiSourceReferenceDto MapSourceReference(ExportAiSourceReference model) =>
        new(model.SourceType, model.SourceId, model.Label, model.Detail);

    private static ExportAiRunDetailDto MapRunDetail(ExportAiRunDetailReadModel model) =>
        new(
            MapRun(model.Run),
            model.Findings.Select(MapFinding).ToArray(),
            model.Recommendations.Select(MapRecommendation).ToArray(),
            model.ContextSnapshot is null ? null : MapContextSnapshot(model.ContextSnapshot));

    private static ExportAiContextSnapshotDto MapContextSnapshot(ExportAiContextSnapshotModel model) =>
        new(
            model.ExportCaseId,
            model.CountryResearchCaseId,
            model.ExportCase is null ? null : MapContextCase(model.ExportCase),
            model.SourceOrderReference,
            model.CustomerName,
            model.Documents.Select(d => new ExportAiContextDocumentRefDto(
                d.Id, d.DocumentType, d.Status, d.FileName)).ToArray(),
            model.PackingStatus,
            model.Shipments.Select(s => new ExportAiContextShipmentRefDto(
                s.Id, s.ShipmentNumber, s.Status, s.PlannedDepartureUtc, s.PackingDeadlineUtc)).ToArray(),
            model.Broker is null
                ? null
                : new ExportAiContextBrokerRefDto(
                    model.Broker.WorkspaceId, model.Broker.Status, model.Broker.OpenCorrectionCount),
            model.Compliance is null ? null : MapContextCompliance(model.Compliance),
            model.Research is null ? null : MapContextResearch(model.Research),
            model.KnowledgeEntries.Select(k => new ExportAiContextKnowledgeRefDto(
                k.Id, k.Title, k.Authority, k.VerificationStatus)).ToArray(),
            model.CrossModuleRisks.Select(r => new ExportCrossModuleRiskDto(
                MapCrossModuleRiskType(r.Type),
                MapCrossModuleRiskSeverity(r.Severity),
                MapCrossModuleSourceModule(r.SourceModule),
                r.SourceRecordReference,
                r.Description,
                r.DetectedAtUtc,
                r.Blocking)).ToArray(),
            model.CapturedAtUtc);

    private static ExportAiContextCaseRefDto MapContextCase(ExportAiContextCaseRef model) =>
        new(
            model.ExportCaseId,
            model.CaseNumber,
            model.DestinationCountryCode,
            model.OriginCountryCode,
            model.ReadinessStatus,
            model.ReadinessPercent,
            model.ComplianceStatus,
            model.HsCode,
            model.HsCodeStatus,
            model.OriginConfirmationStatus);

    private static ExportAiContextComplianceRefDto MapContextCompliance(
        ExportAiContextComplianceRef model) =>
        new(
            model.EvaluationId,
            model.EvaluationNumber,
            model.Result,
            model.RulePackVersion,
            model.RulePackName,
            model.Requirements.Select(r => new ExportAiContextRequirementRefDto(
                r.Id,
                r.RequirementCode,
                r.Title,
                r.Status,
                r.Blocking,
                r.RequiredDocumentType,
                r.SourceReference)).ToArray());

    private static ExportAiContextResearchRefDto MapContextResearch(ExportAiContextResearchRef model) =>
        new(
            model.ResearchCaseId,
            model.ResearchNumber,
            model.Status,
            model.FindingCount,
            model.UnverifiedFindingCount);

    private static string MapCrossModuleRiskType(ExportCrossModuleRiskType type) =>
        type switch
        {
            ExportCrossModuleRiskType.ProductionDelay => "production-delay",
            ExportCrossModuleRiskType.PlanningDeadlineRisk => "planning-deadline-risk",
            ExportCrossModuleRiskType.QualityHold => "quality-hold",
            ExportCrossModuleRiskType.MissingMaterial => "missing-material",
            ExportCrossModuleRiskType.ErpDataMissing => "erp-data-missing",
            ExportCrossModuleRiskType.ShipmentCutoffRisk => "shipment-cutoff-risk",
            ExportCrossModuleRiskType.ContextUnavailable => "context-unavailable",
            _ => "other",
        };

    private static string MapCrossModuleRiskSeverity(ExportCrossModuleRiskSeverity severity) =>
        severity switch
        {
            ExportCrossModuleRiskSeverity.Info => "info",
            ExportCrossModuleRiskSeverity.Warning => "warning",
            ExportCrossModuleRiskSeverity.Critical => "critical",
            _ => "info",
        };

    private static string MapCrossModuleSourceModule(ExportCrossModuleSourceModule module) =>
        module switch
        {
            ExportCrossModuleSourceModule.Production => "production",
            ExportCrossModuleSourceModule.Planning => "planning",
            ExportCrossModuleSourceModule.Quality => "quality",
            ExportCrossModuleSourceModule.Erp => "erp",
            ExportCrossModuleSourceModule.Shipment => "shipment",
            ExportCrossModuleSourceModule.Export => "export",
            _ => "export",
        };

    // ─── Access & context helpers ─────────────────────────────────────────────────

    private static ExportOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    private sealed record AccessResolution(
        FacilityAccessContext? Access,
        IResult? Error);

    private static async Task<AccessResolution> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return new AccessResolution(
                null,
                MapAccessDenied(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return new AccessResolution(null, MapFacilityAccessFailure(accessResult));
        }

        return new AccessResolution(accessResult.Access, null);
    }

    // ─── Enum parsing ───────────────────────────────────────────────────────────

    private static bool TryParseAnalysisRunType(string value, out ExportAiAnalysisRunType runType)
    {
        runType = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "case-overview" => Assign(ExportAiAnalysisRunType.CaseOverview, out runType),
            "compliance-explanation" => Assign(ExportAiAnalysisRunType.ComplianceExplanation, out runType),
            "readiness-analysis" => Assign(ExportAiAnalysisRunType.ReadinessAnalysis, out runType),
            "missing-data-analysis" => Assign(ExportAiAnalysisRunType.MissingDataAnalysis, out runType),
            "country-research-assist" => Assign(ExportAiAnalysisRunType.CountryResearchAssist, out runType),
            "next-actions" => Assign(ExportAiAnalysisRunType.NextActions, out runType),
            "document-gap-analysis" => Assign(ExportAiAnalysisRunType.DocumentGapAnalysis, out runType),
            "cross-module-risk-analysis" => Assign(ExportAiAnalysisRunType.CrossModuleRiskAnalysis, out runType),
            _ => false,
        };
    }

    private static bool Assign<T>(T value, out T result)
    {
        result = value;
        return true;
    }

    // ─── Failure mapping ──────────────────────────────────────────────────────────

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult ExportCaseNotFound() =>
        TypedResults.Problem(
            title: "Export case not found.",
            statusCode: StatusCodes.Status404NotFound,
            extensions: FailureExt(ExportApiFailureCodes.NotFound));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult ResearchCaseNotFound() =>
        TypedResults.Problem(
            title: "Research case not found.",
            statusCode: StatusCodes.Status404NotFound,
            extensions: FailureExt(Export3LFailureCodes.ResearchCaseNotFound));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult AnalysisRunNotFound() =>
        TypedResults.Problem(
            title: "Analysis run not found.",
            statusCode: StatusCodes.Status404NotFound,
            extensions: FailureExt(Export4LFailureCodes.AnalysisRunNotFound));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult ValidationProblem(string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(ExportApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(ExportApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ExportApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ExportApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(
        string facilityFailureCode,
        string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.FacilityAccessDenied));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFailure(
        string failureCode,
        string? reason) =>
        failureCode switch
        {
            ExportFailureCodes.InvalidScope
                or ExportFailureCodes.ValidationFailed
                or Export4LFailureCodes.ContextUnavailable => TypedResults.Problem(
                title: reason ?? "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ExportFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Export case not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ExportApiFailureCodes.NotFound)),
            Export4LFailureCodes.AnalysisRunNotFound => TypedResults.Problem(
                title: reason ?? "Analysis run not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export4LFailureCodes.AnalysisRunNotFound)),
            Export4LFailureCodes.RecommendationNotFound => TypedResults.Problem(
                title: reason ?? "Recommendation not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export4LFailureCodes.RecommendationNotFound)),
            Export4LFailureCodes.InvalidRecommendationStatus => TypedResults.Problem(
                title: reason ?? "Recommendation cannot be reviewed in its current status.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(Export4LFailureCodes.InvalidRecommendationStatus)),
            ExportFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The export resource was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.ConcurrencyConflict)),
            ExportFailureCodes.CaseClosed => TypedResults.Problem(
                title: reason ?? "Export case is closed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.CaseClosed)),
            Export4LFailureCodes.ProviderUnavailable => TypedResults.Problem(
                title: reason ?? "The AI provider failed to complete analysis.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(Export4LFailureCodes.ProviderUnavailable)),
            ExportFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Export AI service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ExportApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ExportApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}
