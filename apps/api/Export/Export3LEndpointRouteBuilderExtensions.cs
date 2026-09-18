using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Export;

public static class Export3LEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapExport3LEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var complianceGroup = endpoints.MapGroup("/api/export/compliance").RequireAuthorization();
        var rulePacks = complianceGroup.MapGroup("/rule-packs");
        rulePacks.MapGet("/", ListRulePacksAsync);
        rulePacks.MapPost("/", CreateRulePackAsync);
        rulePacks.MapGet("/{rulePackId:guid}", GetRulePackAsync);
        rulePacks.MapPost("/{rulePackId:guid}/rules", AddRuleAsync);
        rulePacks.MapPost("/{rulePackId:guid}/verify", VerifyRulePackAsync);
        rulePacks.MapPost("/{rulePackId:guid}/supersede", SupersedeRulePackAsync);

        var casesGroup = endpoints.MapGroup("/api/export/cases").RequireAuthorization();
        var caseCompliance = casesGroup.MapGroup("/{exportCaseId:guid}/compliance");
        caseCompliance.MapGet("/", GetComplianceSummaryAsync);
        caseCompliance.MapGet("/evaluations", ListComplianceEvaluationsAsync);
        caseCompliance.MapGet("/requirements", ListComplianceRequirementsAsync);
        caseCompliance.MapPost("/evaluate", EvaluateComplianceAsync);
        caseCompliance.MapPost("/revalidate", RevalidateComplianceAsync);
        caseCompliance.MapPost("/requirements/{requirementId:guid}/satisfy", SatisfyRequirementAsync);
        casesGroup.MapGet("/{exportCaseId:guid}/traceability", GetTraceabilityAsync);

        var researchGroup = endpoints.MapGroup("/api/export/country-research").RequireAuthorization();
        researchGroup.MapGet("/", ListResearchCasesAsync);
        researchGroup.MapPost("/", CreateResearchCaseAsync);
        researchGroup.MapGet("/{researchCaseId:guid}", GetResearchCaseAsync);
        researchGroup.MapPost("/{researchCaseId:guid}/findings", AddResearchFindingAsync);
        researchGroup.MapPost("/{researchCaseId:guid}/review", ReviewResearchCaseAsync);
        researchGroup.MapPost("/{researchCaseId:guid}/complete", CompleteResearchCaseAsync);
        researchGroup.MapPost("/{researchCaseId:guid}/link", LinkResearchToExportCaseAsync);

        var knowledgeGroup = endpoints.MapGroup("/api/export/country-knowledge").RequireAuthorization();
        knowledgeGroup.MapGet("/", ListKnowledgeEntriesAsync);
        knowledgeGroup.MapGet("/match", MatchKnowledgeEntriesAsync);
        knowledgeGroup.MapPost("/", CreateKnowledgeEntryAsync);
        knowledgeGroup.MapPost("/{knowledgeEntryId:guid}/verify", VerifyKnowledgeEntryAsync);
        knowledgeGroup.MapPost("/{knowledgeEntryId:guid}/supersede", SupersedeKnowledgeEntryAsync);

        return endpoints;
    }

    // ─── Rule packs ─────────────────────────────────────────────────────────────

    private static async Task<IResult> ListRulePacksAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? countryCode,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListRulePacksAsync(
            BuildContext(access.Access!, correlationContext), countryCode, status, cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(MapRulePack).ToArray());
    }

    private static async Task<IResult> GetRulePackAsync(
        Guid rulePackId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetRulePackAsync(
            BuildContext(access.Access!, correlationContext), rulePackId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Rule pack not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(Export3LFailureCodes.RulePackNotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new CountryRulePackDetailDto(
            MapRulePack(result.RulePack!),
            result.Rules!.Select(MapRule).ToArray()));
    }

    private static async Task<IResult> CreateRulePackAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCountryRulePackRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.CreateRulePackAsync(
            BuildContext(access.Access!, correlationContext),
            new CreateCountryRulePackCommand(
                body.CountryCode,
                body.Name,
                body.EffectiveFrom,
                body.EffectiveTo,
                body.SourceTitle,
                body.SourceUrl,
                body.SourcePublisher,
                body.SourcePublicationDate,
                body.SourceVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Created(
            $"/api/export/compliance/rule-packs/{result.RulePackId}?facilityId={facilityId}",
            new CountryRulePackWriteSuccessDto(result.RulePackId!.Value, result.Version!.Value));
    }

    private static async Task<IResult> AddRuleAsync(
        Guid rulePackId,
        [FromQuery] Guid? facilityId,
        [FromBody] AddCountryComplianceRuleRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!TryParseComplianceRuleType(body.RuleType, out var ruleType))
        {
            return ValidationProblem($"Unknown rule type '{body.RuleType}'.");
        }

        if (!TryParseComplianceSeverity(body.Severity, out var severity))
        {
            return ValidationProblem($"Unknown severity '{body.Severity}'.");
        }

        if (!TryParseRequirementMode(body.RequirementMode, out var requirementMode))
        {
            return ValidationProblem($"Unknown requirement mode '{body.RequirementMode}'.");
        }

        var result = await writeRepository.AddRuleAsync(
            BuildContext(access.Access!, correlationContext),
            new AddCountryComplianceRuleCommand(
                rulePackId,
                body.ExpectedRulePackVersion,
                body.RuleCode,
                body.Title,
                body.Description,
                ruleType,
                severity,
                requirementMode,
                body.Blocking,
                body.RequiredDocumentType,
                body.ProductScope,
                body.ProductGroup,
                body.HsCodePattern,
                body.SourceReference),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryComplianceRuleWriteSuccessDto(
            result.RuleId!.Value,
            result.RulePackVersion!.Value));
    }

    private static async Task<IResult> VerifyRulePackAsync(
        Guid rulePackId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyCountryRulePackRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceVerify, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.VerifyRulePackAsync(
            BuildContext(access.Access!, correlationContext),
            new VerifyCountryRulePackCommand(rulePackId, body.ExpectedVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryRulePackWriteSuccessDto(
            result.RulePackId!.Value,
            result.Version!.Value));
    }

    private static async Task<IResult> SupersedeRulePackAsync(
        Guid rulePackId,
        [FromQuery] Guid? facilityId,
        [FromBody] SupersedeCountryRulePackRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryRulePackWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.SupersedeRulePackAsync(
            BuildContext(access.Access!, correlationContext),
            new SupersedeCountryRulePackCommand(
                rulePackId,
                body.ExpectedVersion,
                body.NewRulePackId),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryRulePackWriteSuccessDto(
            result.RulePackId!.Value,
            result.Version!.Value));
    }

    // ─── Case compliance ────────────────────────────────────────────────────────

    private static async Task<IResult> GetComplianceSummaryAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetComplianceSummaryAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ExportCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(MapComplianceSummary(result.Summary!));
    }

    private static async Task<IResult> ListComplianceEvaluationsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListEvaluationsAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ExportCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(MapComplianceEvaluation).ToArray());
    }

    private static async Task<IResult> ListComplianceRequirementsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetComplianceSummaryAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ExportCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Summary!.Requirements.Select(MapRequirement).ToArray());
    }

    private static async Task<IResult> EvaluateComplianceAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateExportComplianceRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.EvaluateComplianceAsync(
            BuildContext(access.Access!, correlationContext),
            new EvaluateExportComplianceCommand(
                exportCaseId,
                body.ExpectedCaseVersion,
                body.ShipmentId),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportComplianceWriteSuccessDto(
            result.EvaluationId!.Value,
            result.CaseVersion!.Value,
            result.Result!));
    }

    private static async Task<IResult> RevalidateComplianceAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] RevalidateExportComplianceRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.RevalidateComplianceAsync(
            BuildContext(access.Access!, correlationContext),
            new RevalidateExportComplianceCommand(exportCaseId, body.ExpectedCaseVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportComplianceWriteSuccessDto(
            result.EvaluationId!.Value,
            result.CaseVersion!.Value,
            result.Result!));
    }

    private static async Task<IResult> SatisfyRequirementAsync(
        Guid exportCaseId,
        Guid requirementId,
        [FromQuery] Guid? facilityId,
        [FromBody] SatisfyComplianceRequirementRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportComplianceWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.SatisfyRequirementAsync(
            BuildContext(access.Access!, correlationContext),
            new SatisfyComplianceRequirementCommand(
                exportCaseId,
                requirementId,
                body.ExpectedRequirementVersion,
                body.ResolutionType,
                body.ResolutionNote),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExportComplianceRequirementWriteSuccessDto(
            result.RequirementVersion!.Value,
            result.CaseVersion!.Value));
    }

    // ─── Traceability ───────────────────────────────────────────────────────────

    private static async Task<IResult> GetTraceabilityAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportTraceabilityReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.ComplianceRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetTraceabilityAsync(
            BuildContext(access.Access!, correlationContext), exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound) return ExportCaseNotFound();
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(MapTraceability(result.Traceability!));
    }

    // ─── Country research ───────────────────────────────────────────────────────

    private static async Task<IResult> ListResearchCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? countryCode,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListResearchCasesAsync(
            BuildContext(access.Access!, correlationContext), countryCode, status, cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(MapResearchCase).ToArray());
    }

    private static async Task<IResult> GetResearchCaseAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.GetResearchCaseAsync(
            BuildContext(access.Access!, correlationContext), researchCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Research case not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(Export3LFailureCodes.ResearchCaseNotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new CountryResearchDetailDto(
            MapResearchCase(result.ResearchCase!),
            result.Findings!.Select(MapFinding).ToArray()));
    }

    private static async Task<IResult> CreateResearchCaseAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCountryResearchCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.CreateResearchCaseAsync(
            BuildContext(access.Access!, correlationContext),
            new CreateCountryResearchCaseCommand(
                body.CountryCode,
                body.ProductReference,
                body.ProductDescription,
                body.ProductGroup,
                body.HsCode,
                body.OriginCountryCode,
                body.PurposeNotes),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Created(
            $"/api/export/country-research/{result.ResearchCaseId}?facilityId={facilityId}",
            new CountryResearchWriteSuccessDto(
                result.ResearchCaseId!.Value,
                result.Version!.Value));
    }

    private static async Task<IResult> AddResearchFindingAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] AddCountryResearchFindingRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!TryParseResearchFindingCategory(body.Category, out var category))
        {
            return ValidationProblem($"Unknown finding category '{body.Category}'.");
        }

        var result = await writeRepository.AddFindingAsync(
            BuildContext(access.Access!, correlationContext),
            new AddCountryResearchFindingCommand(
                researchCaseId,
                body.ExpectedResearchVersion,
                category,
                body.Title,
                body.Summary,
                body.SourcePublisher,
                body.SourceTitle,
                body.SourceUrl,
                body.SourcePublicationDate,
                body.Confidence,
                body.BlockingPotential),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryResearchFindingWriteSuccessDto(
            result.FindingId!.Value,
            result.ResearchVersion!.Value));
    }

    private static async Task<IResult> ReviewResearchCaseAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReviewCountryResearchCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchVerify, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.ReviewResearchCaseAsync(
            BuildContext(access.Access!, correlationContext),
            new ReviewCountryResearchCaseCommand(researchCaseId, body.ExpectedVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryResearchWriteSuccessDto(
            result.ResearchCaseId!.Value,
            result.Version!.Value));
    }

    private static async Task<IResult> CompleteResearchCaseAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteCountryResearchCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchVerify, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.CompleteResearchCaseAsync(
            BuildContext(access.Access!, correlationContext),
            new CompleteCountryResearchCaseCommand(researchCaseId, body.ExpectedVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryResearchWriteSuccessDto(
            result.ResearchCaseId!.Value,
            result.Version!.Value));
    }

    private static async Task<IResult> LinkResearchToExportCaseAsync(
        Guid researchCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] LinkResearchToExportCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryResearchWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.LinkToExportCaseAsync(
            BuildContext(access.Access!, correlationContext),
            new LinkResearchToExportCaseCommand(
                researchCaseId,
                body.ExpectedResearchVersion,
                body.ExportCaseId,
                body.ExpectedCaseVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryResearchWriteSuccessDto(
            result.ResearchCaseId!.Value,
            result.Version!.Value));
    }

    // ─── Country knowledge ──────────────────────────────────────────────────────

    private static async Task<IResult> ListKnowledgeEntriesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? countryCode,
        [FromQuery] string? verificationStatus,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryKnowledgeReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await readRepository.ListKnowledgeEntriesAsync(
            BuildContext(access.Access!, correlationContext),
            countryCode,
            verificationStatus,
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(MapKnowledgeEntry).ToArray());
    }

    private static async Task<IResult> MatchKnowledgeEntriesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string countryCode,
        [FromQuery] string? hsCode,
        [FromQuery] string? productScope,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryKnowledgeReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchRead, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return ValidationProblem("countryCode query parameter is required.");
        }

        var result = await readRepository.MatchKnowledgeAsync(
            BuildContext(access.Access!, correlationContext),
            countryCode.Trim(),
            hsCode,
            productScope,
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(MapKnowledgeEntry).ToArray());
    }

    private static async Task<IResult> CreateKnowledgeEntryAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCountryKnowledgeEntryRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryKnowledgeWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        if (!TryParseResearchFindingCategory(body.Category, out var category))
        {
            return ValidationProblem($"Unknown category '{body.Category}'.");
        }

        if (!TryParseKnowledgeAuthority(body.Authority, out var authority))
        {
            return ValidationProblem($"Unknown authority '{body.Authority}'.");
        }

        var result = await writeRepository.CreateKnowledgeEntryAsync(
            BuildContext(access.Access!, correlationContext),
            new CreateCountryKnowledgeEntryCommand(
                body.CountryCode,
                category,
                body.Title,
                body.Summary,
                body.SourcePublisher,
                body.SourceTitle,
                body.SourceUrl,
                authority,
                body.ProductScope,
                body.ProductGroup,
                body.HsCodePattern,
                body.CreatedFromResearchCaseId),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Created(
            $"/api/export/country-knowledge?facilityId={facilityId}",
            new CountryKnowledgeWriteSuccessDto(
                result.KnowledgeEntryId!.Value,
                result.Version!.Value));
    }

    private static async Task<IResult> VerifyKnowledgeEntryAsync(
        Guid knowledgeEntryId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyCountryKnowledgeEntryRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryKnowledgeWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchVerify, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.VerifyKnowledgeEntryAsync(
            BuildContext(access.Access!, correlationContext),
            new VerifyCountryKnowledgeEntryCommand(knowledgeEntryId, body.ExpectedVersion),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryKnowledgeWriteSuccessDto(
            result.KnowledgeEntryId!.Value,
            result.Version!.Value));
    }

    private static async Task<IResult> SupersedeKnowledgeEntryAsync(
        Guid knowledgeEntryId,
        [FromQuery] Guid? facilityId,
        [FromBody] SupersedeCountryKnowledgeEntryRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCountryKnowledgeWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(
            facilityId, ExportPermissionKeys.CountryResearchManage, facilityAccessResolver, cancellationToken);
        if (access.Error is not null) return access.Error;

        var result = await writeRepository.SupersedeKnowledgeEntryAsync(
            BuildContext(access.Access!, correlationContext),
            new SupersedeCountryKnowledgeEntryCommand(
                knowledgeEntryId,
                body.ExpectedVersion,
                body.NewEntryId),
            cancellationToken);

        if (!result.Succeeded) return MapFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new CountryKnowledgeWriteSuccessDto(
            result.KnowledgeEntryId!.Value,
            result.Version!.Value));
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────────

    private static CountryRulePackDto MapRulePack(CountryRulePackReadModel model) =>
        new(
            model.Id,
            model.FacilityId,
            model.CountryCode,
            model.Name,
            model.Status,
            model.VersionNumber,
            model.EffectiveFrom,
            model.EffectiveTo,
            model.SourceTitle,
            model.SourceUrl,
            model.SourcePublisher,
            model.VerifiedAtUtc,
            model.VerifiedBySubjectId,
            model.Version,
            model.UpdatedAtUtc);

    private static CountryComplianceRuleDto MapRule(CountryComplianceRuleReadModel model) =>
        new(
            model.Id,
            model.RulePackId,
            model.RuleCode,
            model.Title,
            model.Description,
            model.RuleType,
            model.Severity,
            model.RequirementMode,
            model.Blocking,
            model.RequiredDocumentType,
            model.ProductScope,
            model.HsCodePattern,
            model.SourceReference,
            model.Version);

    private static ExportComplianceRequirementDto MapRequirement(
        ExportComplianceRequirementReadModel model) =>
        new(
            model.Id,
            model.ExportCaseId,
            model.EvaluationId,
            model.RequirementCode,
            model.Title,
            model.Description,
            model.Status,
            model.Severity,
            model.Blocking,
            model.RequiredDocumentType,
            model.RelatedDocumentId,
            model.SourceReference,
            model.Version);

    private static ExportComplianceSummaryDto MapComplianceSummary(
        ExportComplianceSummaryReadModel model) =>
        new(
            model.ExportCaseId,
            model.ComplianceStatus,
            model.ComplianceReadinessPercent,
            model.RevalidationRequired,
            model.CurrentEvaluationId,
            model.BlockingCount,
            model.MissingCount,
            model.Requirements.Select(MapRequirement).ToArray(),
            model.RulePack is null ? null : MapRulePack(model.RulePack));

    private static ExportComplianceEvaluationDto MapComplianceEvaluation(
        ExportComplianceEvaluationReadModel model) =>
        new(
            model.Id,
            model.ExportCaseId,
            model.ShipmentId,
            model.EvaluationNumber,
            model.CountryCode,
            model.HsCodeSnapshot,
            model.RulePackId,
            model.RulePackVersionNumber,
            model.Result,
            model.ReadinessPercent,
            model.IsStale,
            model.RevalidationRequired,
            model.SupersedesEvaluationId,
            model.EvaluatedAtUtc,
            model.Version);

    private static CountryResearchCaseDto MapResearchCase(CountryResearchCaseReadModel model) =>
        new(
            model.Id,
            model.FacilityId,
            model.ResearchNumber,
            model.CountryCode,
            model.ProductReference,
            model.ProductDescription,
            model.ProductGroup,
            model.HsCode,
            model.OriginCountryCode,
            model.Status,
            model.PurposeNotes,
            model.LinkedExportCaseId,
            model.CreatedAtUtc,
            model.ReviewedAtUtc,
            model.CompletedAtUtc,
            model.Version,
            model.UpdatedAtUtc);

    private static CountryResearchFindingDto MapFinding(CountryResearchFindingReadModel model) =>
        new(
            model.Id,
            model.CountryResearchCaseId,
            model.Category,
            model.Title,
            model.Summary,
            model.SourcePublisher,
            model.SourceTitle,
            model.SourceUrl,
            model.Confidence,
            model.VerificationStatus,
            model.Version,
            model.CreatedAtUtc);

    private static CountryKnowledgeEntryDto MapKnowledgeEntry(CountryKnowledgeEntryReadModel model) =>
        new(
            model.Id,
            model.CountryCode,
            model.Category,
            model.Title,
            model.Summary,
            model.SourcePublisher,
            model.SourceTitle,
            model.SourceUrl,
            model.Authority,
            model.VerificationStatus,
            model.VerifiedAtUtc,
            model.RevalidationRequired,
            model.Version,
            model.UpdatedAtUtc);

    private static ExportTraceabilityDto MapTraceability(ExportTraceabilityReadModel model) =>
        new(
            model.ExportCaseId,
            model.CaseNumber,
            model.SourceOrderId,
            model.SourceOrderReference,
            model.CustomerName,
            model.DestinationCountryCode,
            model.Documents.Select(d => new ExportTraceabilityDocumentRefDto(
                d.Id, d.DocumentType, d.Status, d.Version)).ToArray(),
            model.PackingUnits.Select(p => new ExportTraceabilityPackingRefDto(
                p.Id, p.UnitCode, p.Status)).ToArray(),
            model.Shipments.Select(s => new ExportTraceabilityShipmentRefDto(
                s.Id, s.ShipmentReference, s.Status, s.Version)).ToArray(),
            model.Broker is null
                ? null
                : new ExportTraceabilityBrokerRefDto(
                    model.Broker.WorkspaceId,
                    model.Broker.PackageVersionNumber,
                    model.Broker.Status),
            model.Compliance is null
                ? null
                : new ExportTraceabilityComplianceRefDto(
                    model.Compliance.EvaluationId,
                    model.Compliance.EvaluationNumber,
                    model.Compliance.Result,
                    model.Compliance.RulePackVersion),
            model.Research is null
                ? null
                : new ExportTraceabilityResearchRefDto(
                    model.Research.ResearchCaseId,
                    model.Research.ResearchNumber,
                    model.Research.Status),
            model.ActivityCount);

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

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult ExportCaseNotFound() =>
        TypedResults.Problem(
            title: "Export case not found.",
            statusCode: StatusCodes.Status404NotFound,
            extensions: FailureExt(ExportApiFailureCodes.NotFound));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult ValidationProblem(string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));

    // ─── Enum parsing ───────────────────────────────────────────────────────────

    private static bool TryParseComplianceRuleType(string value, out ExportComplianceRuleType ruleType)
    {
        ruleType = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "document" => Assign(ExportComplianceRuleType.Document, out ruleType),
            "certification" => Assign(ExportComplianceRuleType.Certification, out ruleType),
            "labeling" => Assign(ExportComplianceRuleType.Labeling, out ruleType),
            "packaging" => Assign(ExportComplianceRuleType.Packaging, out ruleType),
            "origin" => Assign(ExportComplianceRuleType.Origin, out ruleType),
            "customs" => Assign(ExportComplianceRuleType.Customs, out ruleType),
            "technical" => Assign(ExportComplianceRuleType.Technical, out ruleType),
            "restriction" => Assign(ExportComplianceRuleType.Restriction, out ruleType),
            "inspection" => Assign(ExportComplianceRuleType.Inspection, out ruleType),
            "transport" => Assign(ExportComplianceRuleType.Transport, out ruleType),
            "other" => Assign(ExportComplianceRuleType.Other, out ruleType),
            _ => false,
        };
    }

    private static bool TryParseComplianceSeverity(string value, out ExportComplianceSeverity severity)
    {
        severity = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "info" => Assign(ExportComplianceSeverity.Info, out severity),
            "warning" => Assign(ExportComplianceSeverity.Warning, out severity),
            "blocking" => Assign(ExportComplianceSeverity.Blocking, out severity),
            _ => false,
        };
    }

    private static bool TryParseRequirementMode(
        string value,
        out ExportComplianceRequirementMode mode)
    {
        mode = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "mandatory" => Assign(ExportComplianceRequirementMode.Mandatory, out mode),
            "conditional" => Assign(ExportComplianceRequirementMode.Conditional, out mode),
            "informational" => Assign(ExportComplianceRequirementMode.Informational, out mode),
            _ => false,
        };
    }

    private static bool TryParseResearchFindingCategory(
        string value,
        out ExportCountryResearchFindingCategory category)
    {
        category = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "customs" => Assign(ExportCountryResearchFindingCategory.Customs, out category),
            "tariff" => Assign(ExportCountryResearchFindingCategory.Tariff, out category),
            "documentation" => Assign(ExportCountryResearchFindingCategory.Documentation, out category),
            "origin" => Assign(ExportCountryResearchFindingCategory.Origin, out category),
            "technical" => Assign(ExportCountryResearchFindingCategory.Technical, out category),
            "certification" => Assign(ExportCountryResearchFindingCategory.Certification, out category),
            "labeling" => Assign(ExportCountryResearchFindingCategory.Labeling, out category),
            "packaging" => Assign(ExportCountryResearchFindingCategory.Packaging, out category),
            "restricted-goods" => Assign(ExportCountryResearchFindingCategory.RestrictedGoods, out category),
            "trade-agreement" => Assign(ExportCountryResearchFindingCategory.TradeAgreement, out category),
            "logistics" => Assign(ExportCountryResearchFindingCategory.Logistics, out category),
            "broker-question" => Assign(ExportCountryResearchFindingCategory.BrokerQuestion, out category),
            "other" => Assign(ExportCountryResearchFindingCategory.Other, out category),
            _ => false,
        };
    }

    private static bool TryParseKnowledgeAuthority(
        string value,
        out ExportCountryKnowledgeAuthority authority)
    {
        authority = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "verified-rule" => Assign(ExportCountryKnowledgeAuthority.VerifiedRule, out authority),
            "historical-observation" => Assign(
                ExportCountryKnowledgeAuthority.HistoricalObservation,
                out authority),
            "customer-requirement" => Assign(
                ExportCountryKnowledgeAuthority.CustomerRequirement,
                out authority),
            "broker-input" => Assign(ExportCountryKnowledgeAuthority.BrokerInput, out authority),
            "manual-entry" => Assign(ExportCountryKnowledgeAuthority.ManualEntry, out authority),
            _ => false,
        };
    }

    private static bool Assign<T>(T value, out T result)
    {
        result = value;
        return true;
    }

    // ─── Failure mapping ──────────────────────────────────────────────────────────

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
                or Export3LFailureCodes.SourceRequired
                or Export3LFailureCodes.InvalidRulePackStatus
                or Export3LFailureCodes.InvalidResearchStatus => TypedResults.Problem(
                title: reason ?? "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ExportFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Export case not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ExportApiFailureCodes.NotFound)),
            Export3LFailureCodes.RulePackNotFound => TypedResults.Problem(
                title: reason ?? "Rule pack not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.RulePackNotFound)),
            Export3LFailureCodes.RuleNotFound => TypedResults.Problem(
                title: reason ?? "Rule not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.RuleNotFound)),
            Export3LFailureCodes.EvaluationNotFound => TypedResults.Problem(
                title: reason ?? "Evaluation not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.EvaluationNotFound)),
            Export3LFailureCodes.RequirementNotFound => TypedResults.Problem(
                title: reason ?? "Requirement not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.RequirementNotFound)),
            Export3LFailureCodes.ResearchCaseNotFound => TypedResults.Problem(
                title: reason ?? "Research case not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.ResearchCaseNotFound)),
            Export3LFailureCodes.FindingNotFound => TypedResults.Problem(
                title: reason ?? "Finding not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.FindingNotFound)),
            Export3LFailureCodes.KnowledgeEntryNotFound => TypedResults.Problem(
                title: reason ?? "Knowledge entry not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(Export3LFailureCodes.KnowledgeEntryNotFound)),
            ExportFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The export resource was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.ConcurrencyConflict)),
            Export3LFailureCodes.AlreadyVerified => TypedResults.Problem(
                title: reason ?? "Resource is already verified.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(Export3LFailureCodes.AlreadyVerified)),
            Export3LFailureCodes.EvaluationImmutable => TypedResults.Problem(
                title: reason ?? "Evaluation is immutable.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(Export3LFailureCodes.EvaluationImmutable)),
            ExportFailureCodes.CaseClosed => TypedResults.Problem(
                title: reason ?? "Export case is closed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.CaseClosed)),
            ExportFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Export service is unavailable.",
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
