using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.ShiftOperations;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.ShiftOperations;

public static class ShiftOperationsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShiftOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shifts").RequireAuthorization();

        group.MapGet("/", ListShiftsAsync);
        group.MapGet("/current", GetCurrentShiftAsync);
        group.MapGet("/history", ListHistoryAsync);
        group.MapGet("/{shiftId:guid}", GetShiftAsync);
        group.MapPost("/{shiftId:guid}/start", StartShiftAsync);
        group.MapGet("/{shiftId:guid}/open-items", ListOpenItemsAsync);
        group.MapGet("/{shiftId:guid}/closing-preview", GetClosingPreviewAsync);
        group.MapPost("/{shiftId:guid}/handover/prepare", PrepareHandoverAsync);
        group.MapPost("/{shiftId:guid}/handover/submit", SubmitHandoverAsync);
        group.MapPost("/{shiftId:guid}/handover/accept", AcceptHandoverAsync);
        group.MapPatch("/{shiftId:guid}/handover/items/{itemId:guid}", DispositionItemAsync);
        group.MapGet("/{shiftId:guid}/metrics", GetMetricsAsync);
        group.MapGet("/{shiftId:guid}/coordinator-recommendations", GetCoordinatorRecommendationsAsync);

        return endpoints;
    }

    public static IServiceCollection AddCrevaFactOSShiftOperations(this IServiceCollection services)
    {
        services.AddScoped<IShiftOperationsRepository, ShiftOperationsRepository>();
        services.AddScoped<IShiftOperationsService, ShiftOperationsService>();
        services.AddScoped<IShiftHandoverCandidateCollector, ShiftHandoverCandidateCollector>();
        services.AddScoped<IShiftCoordinatorHandoverBridge, ShiftCoordinatorHandoverBridge>();
        return services;
    }

    private static async Task<IResult> ListShiftsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var history = await repository.ListHistoryAsync(access.FacilityId, 30, cancellationToken);
        return TypedResults.Ok(new { items = history.Select(ShiftOperationsEndpointHelpers.MapShift).ToList() });
    }

    private static async Task<IResult> GetCurrentShiftAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var shift = await repository.GetCurrentShiftAsync(access.FacilityId, cancellationToken);
        return shift is null
            ? TypedResults.NotFound(new { code = "shift-not-found", message = "Aktif vardiya bulunamadı." })
            : TypedResults.Ok(ShiftOperationsEndpointHelpers.MapShift(shift));
    }

    private static async Task<IResult> ListHistoryAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var history = await repository.ListHistoryAsync(access.FacilityId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(new { items = history.Select(ShiftOperationsEndpointHelpers.MapShift).ToList() });
    }

    private static async Task<IResult> GetShiftAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var shift = await repository.GetShiftInstanceAsync(shiftId, cancellationToken);
        return shift is null || shift.FacilityId != access.FacilityId
            ? TypedResults.NotFound()
            : TypedResults.Ok(ShiftOperationsEndpointHelpers.MapShift(shift));
    }

    private static async Task<IResult> StartShiftAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Start, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var shift = await service.StartShiftAsync(
                shiftId, authorizationContext.Current!.MembershipId, cancellationToken);
            return TypedResults.Ok(ShiftOperationsEndpointHelpers.MapShift(shift));
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ListOpenItemsAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var items = await repository.ListOpenItemsAsync(shiftId, cancellationToken);
        return TypedResults.Ok(new { items = items.Select(ShiftOperationsEndpointHelpers.MapOpenItem).ToList() });
    }

    private static async Task<IResult> GetClosingPreviewAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.HandoverPrepare, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var preview = await service.GetClosingPreviewAsync(shiftId, cancellationToken);
            return TypedResults.Ok(new
            {
                shiftInstanceId = preview.ShiftInstanceId,
                previewOnly = preview.PreviewOnly,
                readinessScore = preview.ReadinessScore,
                nextShiftMustKnow = preview.NextShiftMustKnow,
                candidates = preview.Candidates.Select(ShiftOperationsEndpointHelpers.MapOpenItem).ToList(),
                existingOpenItems = preview.ExistingOpenItems.Select(ShiftOperationsEndpointHelpers.MapOpenItem).ToList(),
                conflicts = preview.Conflicts,
            });
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> PrepareHandoverAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.HandoverPrepare, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var package = await service.PrepareHandoverAsync(
                shiftId, authorizationContext.Current!.MembershipId, cancellationToken);
            return TypedResults.Ok(MapPackage(package));
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> SubmitHandoverAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        [FromBody] SubmitHandoverApiRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.HandoverSubmit, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var package = await service.SubmitHandoverAsync(
                shiftId, authorizationContext.Current!.MembershipId, body?.Comments, cancellationToken);
            return TypedResults.Ok(MapPackage(package));
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> AcceptHandoverAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        [FromBody] AcceptHandoverApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.HandoverAccept, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var acceptance = await service.AcceptHandoverAsync(
                shiftId,
                new AcceptHandoverRequest(
                    authorizationContext.Current!.MembershipId,
                    body.IncomingSupervisorLabel,
                    body.IncomingAcknowledgement,
                    body.ShiftConcurrencyToken),
                cancellationToken);
            return TypedResults.Ok(new
            {
                id = acceptance.Id,
                verified = acceptance.Verified,
                acceptedAtUtc = acceptance.AcceptedAtUtc,
            });
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> DispositionItemAsync(
        Guid shiftId,
        Guid itemId,
        [FromQuery] Guid? facilityId,
        [FromBody] DispositionOpenItemApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.OpenItemManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var item = await service.DispositionOpenItemAsync(
                shiftId,
                itemId,
                new DispositionOpenItemRequest(body.Disposition, body.Reason, body.ConcurrencyToken),
                cancellationToken);
            return TypedResults.Ok(ShiftOperationsEndpointHelpers.MapOpenItem(item));
        }
        catch (ShiftOperationsException ex)
        {
            return ShiftOperationsEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetMetricsAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var metrics = await service.GetMetricsAsync(shiftId, cancellationToken);
        return TypedResults.Ok(metrics);
    }

    private static async Task<IResult> GetCoordinatorRecommendationsAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftOperationsRepository repository,
        IShiftCoordinatorHandoverBridge coordinatorBridge,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ShiftOperationsEndpointHelpers.ResolveAccessAsync(
            facilityId, ShiftOperationsPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var shift = await repository.GetShiftInstanceAsync(shiftId, cancellationToken);
        if (shift is null || shift.FacilityId != access.FacilityId)
        {
            return TypedResults.NotFound();
        }

        var recommendations = await coordinatorBridge.BuildRecommendationsAsync(shift, cancellationToken);
        return TypedResults.Ok(new { items = recommendations });
    }

    private static object MapPackage(ShiftHandoverPackage package) => new
    {
        shiftInstanceId = package.ShiftInstanceId,
        factoryHandoverId = package.FactoryHandoverId,
        summary = package.Summary,
        readinessScore = package.ReadinessScore,
        nextShiftMustKnow = package.NextShiftMustKnow,
        conflicts = package.Conflicts,
        items = package.Items.Select(ShiftOperationsEndpointHelpers.MapOpenItem).ToList(),
    };
}

internal sealed record SubmitHandoverApiRequest(string? Comments);
internal sealed record AcceptHandoverApiRequest(
    string? IncomingSupervisorLabel,
    string? IncomingAcknowledgement,
    long ShiftConcurrencyToken);
internal sealed record DispositionOpenItemApiRequest(
    string Disposition,
    string? Reason,
    long ConcurrencyToken);
