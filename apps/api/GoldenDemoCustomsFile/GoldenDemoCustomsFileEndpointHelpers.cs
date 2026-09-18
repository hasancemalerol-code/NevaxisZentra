using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.PresentationMode;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoCustomsFile;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.GoldenDemoCustomsFile;

internal static class GoldenDemoCustomsFileEndpointHelpers
{
    internal static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var presentationFailure = await PresentationModeEndpointHelpers.ResolvePresentationAccessFailureAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (presentationFailure is not null)
        {
            return (null, presentationFailure);
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (accessResult.Access, null);
    }

    internal static IResult MapException(GoldenDemoCustomsFileException ex) =>
        ex.Code switch
        {
            GoldenDemoCustomsFileFailureCodes.OutcomeNotFound =>
                TypedResults.NotFound(new { code = ex.Code, message = ex.Message }),
            GoldenDemoCustomsFileFailureCodes.Unauthorized => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.Code, message = ex.Message }),
        };

    internal static object MapWorkspace(GoldenDemoCustomsFileWorkspaceSnapshot snapshot) => new
    {
        outcomeId = snapshot.OutcomeId,
        workerCode = snapshot.WorkerCode,
        workerLabelEn = snapshot.WorkerLabelEn,
        workerLabelTr = snapshot.WorkerLabelTr,
        assignedOutcomeText = snapshot.AssignedOutcomeText,
        status = snapshot.Status,
        statusLabel = snapshot.StatusLabel,
        orderNumber = snapshot.OrderNumber,
        customerName = snapshot.CustomerName,
        destinationCountry = snapshot.DestinationCountry,
        deliveryAddress = snapshot.DeliveryAddress,
        currency = snapshot.Currency,
        incoterm = snapshot.Incoterm,
        packageCount = snapshot.PackageCount,
        shipmentNetWeightKg = snapshot.ShipmentNetWeightKg,
        shipmentGrossWeightKg = snapshot.ShipmentGrossWeightKg,
        shipmentReference = snapshot.ShipmentReference,
        productLines = snapshot.ProductLines,
        tradeRequirements = snapshot.TradeRequirements,
        documents = snapshot.Documents.Select(d => new
        {
            id = d.Id,
            documentType = d.DocumentType,
            versionNumber = d.VersionNumber,
            isCurrent = d.IsCurrent,
            status = d.Status,
            content = d.Content,
            createdAtUtc = d.CreatedAtUtc,
        }),
        requirements = snapshot.Requirements,
        consistencyChecks = snapshot.ConsistencyChecks,
        blockers = snapshot.Blockers,
        correctiveWorks = snapshot.CorrectiveWorks,
        activityTrace = snapshot.ActivityTrace,
        approval = snapshot.Approval,
        package = snapshot.Package is null
            ? null
            : new
            {
                id = snapshot.Package.Id,
                packageVersion = snapshot.Package.PackageVersion,
                documentManifest = snapshot.Package.DocumentManifest,
                documentReferenceIds = snapshot.Package.DocumentReferenceIds,
                assembledAtUtc = snapshot.Package.AssembledAtUtc,
                artifactLabel = snapshot.Package.ArtifactLabel,
            },
        verification = snapshot.Verification,
        hero = snapshot.Hero,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik doğrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Yetki reddedildi.",
                statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Problem(
                title: accessResult.InternalReason,
                statusCode: StatusCodes.Status403Forbidden),
        };
}
