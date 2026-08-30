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

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveApprovalCreateAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.ApprovalsCreate,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveApprovalReviewAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.ApprovalsReview,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveApprovalManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.ApprovalsManage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveHandoverReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.HandoverRead,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveHandoverCreateAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.HandoverCreate,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolveHandoverAcknowledgeAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolvePermissionAccessAsync(
            facilityId,
            TeamsPermissionKeys.HandoverAcknowledge,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<TeamsWorkInboxAccessCapabilities> ResolveWorkInboxCapabilitiesAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken) =>
        new(
            CanReadAiTasks: await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Read, facilityAccessResolver, cancellationToken),
            CanReviewAiTasks: await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Review, facilityAccessResolver, cancellationToken),
            CanReviewApprovals: await HasPermissionAsync(facilityId, TeamsPermissionKeys.ApprovalsReview, facilityAccessResolver, cancellationToken),
            CanAcknowledgeHandovers: await HasPermissionAsync(facilityId, TeamsPermissionKeys.HandoverAcknowledge, facilityAccessResolver, cancellationToken),
            CanReadMentions: await HasPermissionAsync(facilityId, TeamsPermissionKeys.Read, facilityAccessResolver, cancellationToken));

    internal static object MapWorkInbox(TeamsWorkInboxReadModel inbox) => new
    {
        items = inbox.Items.Select(item => new
        {
            sourceKind = item.SourceKind,
            sourceId = item.SourceId,
            title = item.Title,
            reason = item.Reason,
            occurredAt = item.OccurredAtUtc,
            deepLinkPath = item.DeepLinkPath,
        }),
        sources = new
        {
            aiTasks = inbox.Sources.AiTasks,
            approvals = inbox.Sources.Approvals,
            handovers = inbox.Sources.Handovers,
            mentions = inbox.Sources.Mentions,
        },
        disclaimer = inbox.Disclaimer,
    };

    internal static async Task<TeamsHandoverAccessCapabilities> ResolveHandoverCapabilitiesAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken) =>
        new(
            CanRead: await HasPermissionAsync(facilityId, TeamsPermissionKeys.HandoverRead, facilityAccessResolver, cancellationToken),
            CanCreate: await HasPermissionAsync(facilityId, TeamsPermissionKeys.HandoverCreate, facilityAccessResolver, cancellationToken),
            CanAcknowledge: await HasPermissionAsync(facilityId, TeamsPermissionKeys.HandoverAcknowledge, facilityAccessResolver, cancellationToken),
            CanManage: await HasPermissionAsync(facilityId, TeamsPermissionKeys.HandoverManage, facilityAccessResolver, cancellationToken),
            CanReadAiTasks: await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Read, facilityAccessResolver, cancellationToken));

    internal static IResult MapHandoverWriteResult(TeamsHandoverWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded || result.Detail is null)
        {
            return MapFailure(result.FailureCode, result.FailureReason);
        }

        var dto = MapHandoverDetail(result.Detail);
        if (result.IdempotentReplay)
        {
            return TypedResults.Ok(dto);
        }

        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static object MapHandoverDetail(TeamsHandoverDetailReadModel detail) => new
    {
        id = detail.Id,
        channelId = detail.ChannelId,
        channelName = detail.ChannelName,
        authorTenantMembershipId = detail.AuthorTenantMembershipId,
        authorLabel = detail.AuthorLabel,
        shiftDate = detail.ShiftDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        shiftType = detail.ShiftType,
        status = detail.Status,
        summary = detail.Summary,
        safetyNote = detail.SafetyNote,
        items = detail.Items.Select(i => new
        {
            id = i.Id,
            itemKind = i.ItemKind,
            title = i.Title,
            body = i.Body,
            messageId = i.MessageId,
            aiTaskId = i.AiTaskId,
            sourceKind = i.SourceKind,
            sourceLabel = i.SourceLabel,
            deepLinkPath = i.DeepLinkPath,
            canOpen = i.CanOpen,
            unavailableReason = i.UnavailableReason,
        }),
        acknowledgements = detail.Acknowledgements.Select(a => new
        {
            tenantMembershipId = a.TenantMembershipId,
            displayLabel = a.DisplayLabel,
            acknowledgedAt = a.AcknowledgedAtUtc,
        }),
        auditEvents = detail.AuditEvents.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            actorLabel = e.ActorLabel,
            occurredAt = e.OccurredAtUtc,
        }),
        createdAt = detail.CreatedAtUtc,
        publishedAt = detail.PublishedAtUtc,
        canEdit = detail.CanEdit,
        canPublish = detail.CanPublish,
        canAcknowledge = detail.CanAcknowledge,
        disclaimer = detail.Disclaimer,
    };

    internal static async Task<TeamsApprovalAccessCapabilities> ResolveApprovalCapabilitiesAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken) =>
        new(
            CanCreate: await HasPermissionAsync(facilityId, TeamsPermissionKeys.ApprovalsCreate, facilityAccessResolver, cancellationToken),
            CanReview: await HasPermissionAsync(facilityId, TeamsPermissionKeys.ApprovalsReview, facilityAccessResolver, cancellationToken),
            CanManage: await HasPermissionAsync(facilityId, TeamsPermissionKeys.ApprovalsManage, facilityAccessResolver, cancellationToken),
            CanReadAiTasks: await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Read, facilityAccessResolver, cancellationToken));

    internal static IResult MapApprovalWriteResult(TeamsApprovalWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded || result.Detail is null)
        {
            return MapFailure(result.FailureCode, result.FailureReason);
        }

        var dto = MapApprovalDetail(result.Detail);
        if (result.IdempotentReplay)
        {
            return TypedResults.Ok(dto);
        }

        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static object MapApprovalDetail(TeamsApprovalDetailReadModel detail) => new
    {
        id = detail.Id,
        title = detail.Title,
        rationale = detail.Rationale,
        status = detail.Status,
        channelId = detail.ChannelId,
        channelName = detail.ChannelName,
        requestedByTenantMembershipId = detail.RequestedByTenantMembershipId,
        requestedByLabel = detail.RequestedByLabel,
        source = new
        {
            sourceKind = detail.Source.SourceKind,
            messageId = detail.Source.MessageId,
            aiTaskId = detail.Source.AiTaskId,
            channelName = detail.Source.ChannelName,
            label = detail.Source.Label,
            deepLinkPath = detail.Source.DeepLinkPath,
            canOpen = detail.Source.CanOpen,
            unavailableReason = detail.Source.UnavailableReason,
        },
        reviewers = detail.Reviewers.Select(r => new
        {
            tenantMembershipId = r.TenantMembershipId,
            displayLabel = r.DisplayLabel,
            role = r.Role,
        }),
        decision = detail.Decision is null ? null : new
        {
            id = detail.Decision.Id,
            reviewerTenantMembershipId = detail.Decision.ReviewerTenantMembershipId,
            reviewerLabel = detail.Decision.ReviewerLabel,
            decision = detail.Decision.Decision,
            note = detail.Decision.Note,
            createdAt = detail.Decision.CreatedAtUtc,
        },
        auditEvents = detail.AuditEvents.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            actorLabel = e.ActorLabel,
            occurredAt = e.OccurredAtUtc,
        }),
        createdAt = detail.CreatedAtUtc,
        resolvedAt = detail.ResolvedAtUtc,
        cancelledAt = detail.CancelledAtUtc,
        canDecide = detail.CanDecide,
        canCancel = detail.CanCancel,
        canManageReviewers = detail.CanManageReviewers,
        disclaimer = detail.Disclaimer,
    };

    private static async Task<(TeamsOperationContext? Context, IResult? Failure)> ResolvePermissionAccessAsync(
        Guid? facilityId,
        string permissionKey,
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
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(AiTaskOperationContext? Context, IResult? Failure)> ResolveAiTaskCreateAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, AiTaskPermissionKeys.Create),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        var access = accessResult.Access;
        return (new AiTaskOperationContext(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null), null);
    }

    internal static IResult MapMessageTaskDraftResult(TeamsMessageTaskDraftWriteResult result)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new
        {
            taskId = result.TaskId!.Value,
            taskReference = result.TaskReference,
            taskStatus = result.TaskStatus,
            idempotentReplay = result.IdempotentReplay,
            disclaimer = TeamsConstants.MessageTaskDraftDisclaimer,
        };

        return result.IdempotentReplay
            ? TypedResults.Ok(dto)
            : TypedResults.Created((string?)null, dto);
    }

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
        linkedTask = message.LinkedTask is null ? null : new
        {
            taskId = message.LinkedTask.TaskId,
            taskReference = message.LinkedTask.TaskReference,
            taskStatus = message.LinkedTask.TaskStatus,
            taskDeepLinkPath = message.LinkedTask.TaskDeepLinkPath,
            canOpenTask = message.LinkedTask.CanOpenTask,
        },
        createdAt = message.CreatedAtUtc,
    };
}

internal sealed record TeamsWriteSuccessDto(Guid Id, bool IdempotentReplay);
