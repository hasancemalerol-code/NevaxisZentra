using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.OperationalRisk;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.Teams;

internal static class TeamsEndpointHelpers
{
    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "Tesis tanımlayıcısı gerekli."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, TeamsPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "Tesis tanımlayıcısı gerekli."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, TeamsPermissionKeys.Write),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "Tesis tanımlayıcısı gerekli."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, TeamsPermissionKeys.Manage),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(bool CanWrite, bool CanManage)> ResolveMutationAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var canManage = await HasPermissionAsync(
            facilityId,
            TeamsPermissionKeys.Manage,
            facilityAccessResolver,
            cancellationToken);
        var canWrite = canManage || await HasPermissionAsync(
            facilityId,
            TeamsPermissionKeys.Write,
            facilityAccessResolver,
            cancellationToken);

        return (canWrite, canManage);
    }

    internal static async Task<TeamsAccessCapabilities> ResolveCapabilitiesAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken) =>
        new(
            CanReadAiTasks: await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadMaintenance: await HasPermissionAsync(facilityId, MaintenancePermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadQuality: await HasPermissionAsync(facilityId, QualityPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadMaterial: await HasPermissionAsync(facilityId, MaterialPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadProcurement: await HasPermissionAsync(facilityId, ErpPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadProduction: await HasPermissionAsync(facilityId, ProductionExecutionPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadMold: await HasPermissionAsync(facilityId, MoldPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReadOperationalRisk: await HasPermissionAsync(facilityId, OperationalRiskPermissionKeys.Read, facilityAccessResolver, cancellationToken));

    internal static TeamsOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(TeamsWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        if (result.Message is not null)
        {
            return result.IdempotentReplay
                ? TypedResults.Ok(MapMessage(result.Message))
                : TypedResults.Created((string?)null, MapMessage(result.Message));
        }

        var dto = new TeamsWriteSuccessDto(result.EntityId!.Value, result.IdempotentReplay);
        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static IResult MapFailure(string? failureCode, string? reason) =>
        failureCode switch
        {
            TeamsFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Kayıt bulunamadı.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            TeamsFailureCodes.IdempotencyConflict
                or TeamsFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "İstek çakışması.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            TeamsFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Doğrulama başarısız.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            TeamsFailureCodes.ChannelAccessDenied
                or TeamsFailureCodes.AccessDenied
                or TeamsFailureCodes.FacilityScopeDenied => TypedResults.Problem(
                title: reason ?? "Erişim reddedildi.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(failureCode)),
            _ => TypedResults.Problem(
                title: reason ?? "İstek başarısız.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode!)),
        };

    private static async Task<bool> HasPermissionAsync(
        Guid facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, permissionKey),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string reason) =>
        TypedResults.Problem(
            title: reason,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik doğrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("teams.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Bu işlem için yetkiniz yok.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("teams.permission-denied")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Bu tesise erişiminiz yok.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("teams.facility-access-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Tesis tanımlayıcısı gerekli.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("teams.invalid-scope")),
            _ => TypedResults.Problem(
                title: "Erişim reddedildi.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("teams.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["failureCode"] = code };

    internal static object MapMessage(TeamsMessageReadModel message) => new
    {
        id = message.Id,
        channelId = message.ChannelId,
        authorTenantMembershipId = message.AuthorTenantMembershipId,
        authorLabel = message.AuthorLabel,
        body = message.Body,
        sourceContext = message.SourceContext is null ? null : new
        {
            contextType = message.SourceContext.ContextType,
            label = message.SourceContext.Label,
            deepLinkPath = message.SourceContext.DeepLinkPath,
            aiTaskId = message.SourceContext.AiTaskId,
            sourceModule = message.SourceContext.SourceModule,
            sourceEntityId = message.SourceContext.SourceEntityId,
            canOpen = message.SourceContext.CanOpen,
        },
        mentions = message.Mentions.Select(m => new
        {
            tenantMembershipId = m.TenantMembershipId,
            displayLabel = m.DisplayLabel,
        }),
        createdAt = message.CreatedAtUtc,
    };
}

internal sealed record TeamsWriteSuccessDto(Guid Id, bool IdempotentReplay);
