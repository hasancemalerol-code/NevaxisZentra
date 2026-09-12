using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Teams;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Teams;

public static class TeamsApprovalEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsApprovalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/approvals").RequireAuthorization();
        group.MapGet("/", ListApprovalsAsync);
        group.MapPost("/", CreateApprovalAsync);
        group.MapGet("/{requestId:guid}", GetApprovalAsync);
        group.MapPost("/{requestId:guid}/decisions", RecordDecisionAsync);
        group.MapPost("/{requestId:guid}/cancel", CancelApprovalAsync);
        group.MapPost("/{requestId:guid}/reviewers", AssignReviewerAsync);
        return endpoints;
    }

    private static async Task<IResult> ListApprovalsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? channelId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.ListRequestsAsync(
            context, capabilities, channelId, status, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(item => new
            {
                id = item.Id,
                title = item.Title,
                status = item.Status,
                channelName = item.ChannelName,
                channelId = item.ChannelId,
                requestedByLabel = item.RequestedByLabel,
                createdAt = item.CreatedAtUtc,
                sourceKind = item.SourceKind,
                canOpen = item.CanOpen,
            }),
            capabilities = new
            {
                canCreate = result.CanCreate,
                canReview = result.CanReview,
                canManage = result.CanManage,
            },
            disclaimer = TeamsConstants.ApprovalDecisionDisclaimer,
        });
    }

    private static async Task<IResult> GetApprovalAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetRequestAsync(context, capabilities, requestId, cancellationToken);
        return detail is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Onay isteği bulunamadı veya erişim yok.")
            : TypedResults.Ok(TeamsEndpointHelpers.MapApprovalDetail(detail));
    }

    private static async Task<IResult> CreateApprovalAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateTeamsApprovalRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveApprovalCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.CreateRequestAsync(
            context,
            capabilities,
            new CreateTeamsApprovalRequestCommand(
                body.ChannelId,
                body.MessageId,
                body.AiTaskId,
                body.Title ?? string.Empty,
                body.Rationale ?? string.Empty,
                body.IdempotencyKey ?? string.Empty,
                (body.Reviewers ?? [])
                    .Select(r => new TeamsApprovalReviewerAssignment(
                        r.TenantMembershipId,
                        string.IsNullOrWhiteSpace(r.Role) ? TeamsApprovalReviewerRoles.Reviewer : r.Role))
                    .ToList()),
            cancellationToken);

        return TeamsEndpointHelpers.MapApprovalWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> RecordDecisionAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordTeamsApprovalDecisionBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveApprovalReviewAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.RecordDecisionAsync(
            context,
            capabilities,
            new RecordTeamsApprovalDecisionCommand(requestId, body.Decision ?? string.Empty, body.Note),
            cancellationToken);

        return TeamsEndpointHelpers.MapApprovalWriteResult(result);
    }

    private static async Task<IResult> CancelApprovalAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveApprovalCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.CancelRequestAsync(
            context,
            capabilities,
            new CancelTeamsApprovalRequestCommand(requestId),
            cancellationToken);

        return TeamsEndpointHelpers.MapApprovalWriteResult(result);
    }

    private static async Task<IResult> AssignReviewerAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignTeamsApprovalReviewerBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsApprovalRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveApprovalManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveApprovalCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.AssignReviewerAsync(
            context,
            capabilities,
            new AssignTeamsApprovalReviewerCommand(
                requestId,
                body.TenantMembershipId,
                string.IsNullOrWhiteSpace(body.Role) ? TeamsApprovalReviewerRoles.Reviewer : body.Role),
            cancellationToken);

        return TeamsEndpointHelpers.MapApprovalWriteResult(result);
    }
}

public sealed record CreateTeamsApprovalReviewerBody(Guid TenantMembershipId, string? Role);

public sealed record CreateTeamsApprovalRequestBody(
    Guid ChannelId,
    Guid? MessageId,
    Guid? AiTaskId,
    string? Title,
    string? Rationale,
    string? IdempotencyKey,
    IReadOnlyList<CreateTeamsApprovalReviewerBody>? Reviewers);

public sealed record RecordTeamsApprovalDecisionBody(string? Decision, string? Note);

public sealed record AssignTeamsApprovalReviewerBody(Guid TenantMembershipId, string? Role);
