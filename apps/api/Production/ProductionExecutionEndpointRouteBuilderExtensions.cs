using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Api.Production;

public static class ProductionExecutionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionExecutionReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production/orders").RequireAuthorization();
        group.MapGet("/", ListProductionOrdersAsync);
        group.MapGet("/{productionOrderId:guid}", GetProductionOrderDetailAsync);
        group.MapGet("/{productionOrderId:guid}/quantity-entries", ListQuantityEntriesAsync);
        // 3L read endpoints
        group.MapGet("/{productionOrderId:guid}/downtimes", ListDowntimesAsync);
        group.MapGet("/{productionOrderId:guid}/setups", ListSetupSessionsAsync);
        group.MapGet("/{productionOrderId:guid}/cycles", ListCycleEntriesAsync);
        group.MapGet("/{productionOrderId:guid}/runtime-summary", GetRuntimeSummaryAsync);
        // 4L read endpoints
        group.MapGet("/{productionOrderId:guid}/exceptions", ListExceptionsAsync);
        group.MapGet("/{productionOrderId:guid}/health-summary", GetHealthSummaryAsync);
        // 5L read endpoints
        group.MapGet("/{productionOrderId:guid}/shift-assignments", ListShiftAssignmentsAsync);
        group.MapGet("/{productionOrderId:guid}/operators", ListOperatorAssignmentsAsync);
        group.MapGet("/{productionOrderId:guid}/shift-handovers", ListHandoversAsync);
        return endpoints;
    }

    public static IEndpointRouteBuilder MapProductionResourceExecutionReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production").RequireAuthorization();
        group.MapGet("/shifts/{shiftId:guid}/execution-summary", GetShiftExecutionSummaryAsync);
        group.MapGet("/operators/{subjectId}/execution-summary", GetOperatorExecutionSummaryAsync);
        group.MapProductionFollowUpEndpoints();
        return endpoints;
    }

    public static IEndpointRouteBuilder MapProductionExecutionWriteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production").RequireAuthorization();
        group.MapPost("/handoffs/{handoffId:guid}/consume", ConsumeHandoffAsync);
        group.MapPost("/orders/{productionOrderId:guid}/start", StartProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/pause", PauseProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/resume", ResumeProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/complete", CompleteProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/quantities/good", RecordGoodProductionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/quantities/scrap", RecordScrapAsync);
        group.MapPost("/orders/{productionOrderId:guid}/quantity-entries/{entryId:guid}/reverse", ReverseEntryAsync);
        // 3L write endpoints
        group.MapPost("/orders/{productionOrderId:guid}/downtimes/start", StartDowntimeAsync);
        group.MapPost("/orders/{productionOrderId:guid}/downtimes/{downtimeId:guid}/end", EndDowntimeAsync);
        group.MapPost("/orders/{productionOrderId:guid}/setups/start", StartSetupSessionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/setups/{setupSessionId:guid}/complete", CompleteSetupSessionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/cycles/manual", RecordManualCycleAsync);
        // 4L write endpoints
        group.MapPost("/orders/{productionOrderId:guid}/exceptions", RaiseExceptionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/exceptions/{exceptionId:guid}/acknowledge", AcknowledgeExceptionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/exceptions/{exceptionId:guid}/resolve", ResolveExceptionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/downtimes/{downtimeId:guid}/recover", RecoverDowntimeAsync);
        group.MapPost("/orders/{productionOrderId:guid}/setups/{setupSessionId:guid}/recover", RecoverSetupSessionAsync);
        // 5L write endpoints
        group.MapPost("/orders/{productionOrderId:guid}/shift-assignment", AssignShiftAsync);
        group.MapPost("/orders/{productionOrderId:guid}/shift-assignment/reassign", ReassignShiftAsync);
        group.MapPost("/orders/{productionOrderId:guid}/operators", AssignOperatorAsync);
        group.MapPost("/orders/{productionOrderId:guid}/operators/{assignmentId:guid}/release", ReleaseOperatorAsync);
        group.MapPost("/orders/{productionOrderId:guid}/shift-handovers", CreateHandoverAsync);
        group.MapPost("/orders/{productionOrderId:guid}/exceptions/{exceptionId:guid}/assign", AssignExceptionAsync);
        group.MapPost("/orders/{productionOrderId:guid}/exceptions/{exceptionId:guid}/unassign", UnassignExceptionAsync);
        return endpoints;
    }

    // ─────────────────────────── Read: List ───────────────────────────

    private static async Task<IResult> ListProductionOrdersAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await readRepository.ListAsync(context, status, search, limit, cancellationToken);

        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, null);
        }

        var dtos = result.Items!.Select(o => new ProductionOrderSummaryDto(
            o.Id,
            o.FacilityId,
            o.SourceProductionPlanId,
            o.ReferenceCode,
            o.Title,
            o.Status,
            o.PlannedStartAtUtc,
            o.PlannedEndAtUtc,
            o.ActualStartAtUtc,
            o.MachineReference,
            o.MoldReference,
            o.ShiftName,
            o.PlannedQuantity,
            o.GoodQuantity,
            o.ScrapQuantity,
            o.Version,
            o.UpdatedAtUtc,
            o.TotalDowntimeSeconds,
            o.ManualCycleCount,
            o.CurrentShiftCode,
            o.PrimaryOperatorSubjectId,
            o.ActiveOperatorCount)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── Read: Detail ───────────────────────────

    private static async Task<IResult> GetProductionOrderDetailAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await readRepository.FindByIdAsync(context, productionOrderId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Production order not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ProductionExecutionApiFailureCodes.NotFound));
            }

            return MapWriteFailure(result.FailureCode!, null);
        }

        var o = result.Order!;
        var gross = o.GoodQuantity + o.ScrapQuantity;
        decimal? remaining = o.PlannedQuantity.HasValue
            ? Math.Max(o.PlannedQuantity.Value - o.GoodQuantity, 0)
            : null;
        decimal? percentage = o.PlannedQuantity.HasValue && o.PlannedQuantity.Value > 0
            ? o.GoodQuantity / o.PlannedQuantity.Value * 100
            : null;

        return TypedResults.Ok(new ProductionOrderDetailDto(
            o.Id,
            o.FacilityId,
            o.ProductionPlanHandoffId,
            o.SourceProductionPlanId,
            o.SourceProductionPlanVersion,
            o.ReferenceCode,
            o.Title,
            o.Status,
            o.PlannedStartAtUtc,
            o.PlannedEndAtUtc,
            o.ActualStartAtUtc,
            o.ActualEndAtUtc,
            o.ShiftName,
            o.MachineReference,
            o.MoldReference,
            o.ProductReference,
            new QuantitySummaryDto(
                o.PlannedQuantity,
                o.PlannedQuantityUnit,
                o.GoodQuantity,
                o.ScrapQuantity,
                gross,
                remaining,
                percentage,
                o.LastProductionEntryAtUtc),
            o.CreatedBySubjectId,
            o.LastModifiedBySubjectId,
            o.Version,
            o.CreatedAtUtc,
            o.UpdatedAtUtc,
            o.TotalDowntimeSeconds,
            o.TotalSetupSeconds,
            o.ManualCycleCount,
            o.TotalManualCycleDurationMilliseconds,
            o.LastCycleAtUtc,
            o.CurrentShiftId,
            o.CurrentShiftCode,
            o.CurrentShiftStartAtUtc,
            o.CurrentShiftEndAtUtc,
            o.PrimaryOperatorSubjectId,
            o.ActiveOperatorCount,
            o.LastAssignmentAtUtc));
    }

    // ─────────────────────────── Write: Consume Handoff ───────────────────────────

    private static async Task<IResult> ConsumeHandoffAsync(
        Guid handoffId,
        [FromQuery] Guid? facilityId,
        [FromBody] ConsumeHandoffApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Write),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await writeRepository.ConsumeHandoffAsync(
            context,
            new ConsumeHandoffCommand(handoffId, body.ExpectedHandoffVersion),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Created(
            (string?)null,
            new ProductionOrderWriteSuccessDto(
                result.ProductionOrderId!.Value,
                result.ReferenceCode!,
                result.Status!,
                result.Version!.Value,
                result.CreatedAtUtc!.Value,
                result.UpdatedAtUtc!.Value));
    }

    // ─────────────────────────── Write: Start ───────────────────────────

    private static async Task<IResult> StartProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] StartProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Start,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.StartAsync(
            context, new StartProductionOrderCommand(productionOrderId, body.ExpectedVersion), cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Pause ───────────────────────────

    private static async Task<IResult> PauseProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] PauseProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Pause,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.PauseAsync(
            context,
            new PauseProductionOrderCommand(productionOrderId, body.ExpectedVersion, body.Reason ?? string.Empty),
            cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Resume ───────────────────────────

    private static async Task<IResult> ResumeProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] ResumeProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Start,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.ResumeAsync(
            context, new ResumeProductionOrderCommand(productionOrderId, body.ExpectedVersion), cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Complete ───────────────────────────

    private static async Task<IResult> CompleteProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Complete,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.CompleteAsync(
            context,
            new CompleteProductionOrderCommand(
                productionOrderId,
                body.ExpectedVersion,
                body.AllowShortCompletion,
                body.CompletionReason),
            cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Read: Quantity Entries ───────────────────────────

    private static async Task<IResult> ListQuantityEntriesAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExecutionEntryRepository entryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var effectiveLimit = Math.Clamp(limit ?? 50, 1, 200);
        var result = await entryRepository.ListEntriesAsync(context, productionOrderId, effectiveLimit, cancellationToken);

        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        var dtos = result.Items!.Select(e => new ProductionExecutionEntryDto(
            e.Id,
            e.EntryType,
            e.Quantity,
            e.Unit,
            e.ReasonCode,
            e.Comment,
            e.RecordedAtUtc,
            e.RecordedBySubjectId,
            e.IsReversed,
            e.ReversesEntryId)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── Write: Record Good ───────────────────────────

    private static async Task<IResult> RecordGoodProductionAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordGoodProductionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExecutionEntryRepository entryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Record,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await entryRepository.RecordGoodAsync(
            context,
            new RecordGoodProductionCommand(productionOrderId, body.ExpectedVersion, body.Quantity, body.Comment),
            cancellationToken);

        return MapEntryResult(result);
    }

    // ─────────────────────────── Write: Record Scrap ───────────────────────────

    private static async Task<IResult> RecordScrapAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordScrapApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExecutionEntryRepository entryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Record,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await entryRepository.RecordScrapAsync(
            context,
            new RecordScrapCommand(productionOrderId, body.ExpectedVersion, body.Quantity, body.ReasonCode, body.Comment),
            cancellationToken);

        return MapEntryResult(result);
    }

    // ─────────────────────────── Write: Reverse Entry ───────────────────────────

    private static async Task<IResult> ReverseEntryAsync(
        Guid productionOrderId,
        Guid entryId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReverseEntryApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExecutionEntryRepository entryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Record,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await entryRepository.ReverseAsync(
            context,
            new ReverseProductionEntryCommand(productionOrderId, entryId, body.ExpectedVersion, body.Reason),
            cancellationToken);

        return MapEntryResult(result);
    }

    // ─────────────────────────── 3L Read: Downtimes ───────────────────────────

    private static async Task<IResult> ListDowntimesAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var effective = Math.Clamp(limit ?? 50, 1, 200);
        var result = await operationsRepository.ListDowntimesAsync(context, productionOrderId, effective, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var dtos = result.Items!.Select(d => new DowntimeSummaryDto(
            d.Id, d.Category, d.ReasonCode, d.StartedAtUtc, d.EndedAtUtc, d.DurationSeconds,
            d.Comment, d.StartedBySubjectId, d.EndedBySubjectId, d.IsActive)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── 3L Read: Setup Sessions ───────────────────────────

    private static async Task<IResult> ListSetupSessionsAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var effective = Math.Clamp(limit ?? 50, 1, 200);
        var result = await operationsRepository.ListSetupSessionsAsync(context, productionOrderId, effective, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var dtos = result.Items!.Select(s => new SetupSessionSummaryDto(
            s.Id, s.SetupType, s.StartedAtUtc, s.CompletedAtUtc, s.DurationSeconds,
            s.Comment, s.StartedBySubjectId, s.CompletedBySubjectId, s.IsActive)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── 3L Read: Cycle Entries ───────────────────────────

    private static async Task<IResult> ListCycleEntriesAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var effective = Math.Clamp(limit ?? 50, 1, 200);
        var result = await operationsRepository.ListCycleEntriesAsync(context, productionOrderId, effective, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var dtos = result.Items!.Select(c => new CycleEntrySummaryDto(
            c.Id, c.CycleNumber, c.CycleStartedAtUtc, c.CycleEndedAtUtc, c.CycleDurationMilliseconds,
            c.QuantityProduced, c.RecordedBySubjectId, c.RecordedAtUtc, c.Source, c.Comment)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── 3L Read: Runtime Summary ───────────────────────────

    private static async Task<IResult> GetRuntimeSummaryAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await operationsRepository.GetRuntimeSummaryAsync(context, productionOrderId, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var s = result.Summary!;

        return TypedResults.Ok(new ProductionRuntimeSummaryDto(
            s.ActualStartAtUtc,
            s.ActualEndAtUtc,
            s.CurrentElapsedSeconds,
            s.TotalDowntimeSeconds,
            s.TotalSetupSeconds,
            s.NetRunningSeconds,
            s.ManualCycleCount,
            s.AverageManualCycleMilliseconds,
            s.LastCycleAtUtc,
            s.ActiveDowntime is null ? null : new DowntimeSummaryDto(
                s.ActiveDowntime.Id, s.ActiveDowntime.Category, s.ActiveDowntime.ReasonCode,
                s.ActiveDowntime.StartedAtUtc, s.ActiveDowntime.EndedAtUtc, s.ActiveDowntime.DurationSeconds,
                s.ActiveDowntime.Comment, s.ActiveDowntime.StartedBySubjectId, s.ActiveDowntime.EndedBySubjectId, true),
            s.ActiveSetup is null ? null : new SetupSessionSummaryDto(
                s.ActiveSetup.Id, s.ActiveSetup.SetupType,
                s.ActiveSetup.StartedAtUtc, s.ActiveSetup.CompletedAtUtc, s.ActiveSetup.DurationSeconds,
                s.ActiveSetup.Comment, s.ActiveSetup.StartedBySubjectId, s.ActiveSetup.CompletedBySubjectId, true)));
    }

    // ─────────────────────────── 3L Write: Downtime Start ───────────────────────────

    private static async Task<IResult> StartDowntimeAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] StartDowntimeApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Downtime,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        if (string.IsNullOrWhiteSpace(body.ReasonCode))
        {
            return TypedResults.Problem(
                title: "A downtime reason code is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidDowntimeReason));
        }

        var result = await operationsRepository.StartDowntimeAsync(
            context,
            new StartDowntimeCommand(productionOrderId, body.ExpectedVersion, body.ReasonCode.Trim(), body.Comment?.Trim()),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 3L Write: Downtime End ───────────────────────────

    private static async Task<IResult> EndDowntimeAsync(
        Guid productionOrderId,
        Guid downtimeId,
        [FromQuery] Guid? facilityId,
        [FromBody] EndDowntimeApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Downtime,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await operationsRepository.EndDowntimeAsync(
            context,
            new EndDowntimeCommand(productionOrderId, downtimeId, body.ExpectedVersion),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 3L Write: Setup Start ───────────────────────────

    private static async Task<IResult> StartSetupSessionAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] StartSetupApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Setup,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        if (string.IsNullOrWhiteSpace(body.SetupType))
        {
            return TypedResults.Problem(
                title: "A setup type is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidSetupType));
        }

        var result = await operationsRepository.StartSetupSessionAsync(
            context,
            new StartSetupSessionCommand(productionOrderId, body.ExpectedVersion, body.SetupType.Trim(), body.Comment?.Trim()),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 3L Write: Setup Complete ───────────────────────────

    private static async Task<IResult> CompleteSetupSessionAsync(
        Guid productionOrderId,
        Guid setupSessionId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteSetupApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Setup,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await operationsRepository.CompleteSetupSessionAsync(
            context,
            new CompleteSetupSessionCommand(productionOrderId, setupSessionId, body.ExpectedVersion),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 3L Write: Manual Cycle ───────────────────────────

    private static async Task<IResult> RecordManualCycleAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordManualCycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Record,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await operationsRepository.RecordManualCycleAsync(
            context,
            new RecordManualCycleCommand(
                productionOrderId,
                body.ExpectedVersion,
                body.CycleDurationMilliseconds,
                body.QuantityProduced,
                body.Comment?.Trim()),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 4L: List Exceptions ───────────────────────────

    private static async Task<IResult> ListExceptionsAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await exceptionRepository.ListExceptionsAsync(context, productionOrderId, limit ?? 50, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(e => new ExceptionSummaryDto(
            e.Id, e.ExceptionType, e.Severity, e.Status, e.Message,
            e.RaisedAtUtc, e.RaisedBySubjectId, e.ResolvedAtUtc, e.ResolvedBySubjectId, e.ResolutionNote,
            e.Version, e.AssignedToSubjectId, e.AssignedAtUtc, e.AssignedBySubjectId)).ToList());
    }

    // ─────────────────────────── 4L: Health Summary ────────────────────────────

    private static async Task<IResult> GetHealthSummaryAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        Microsoft.Extensions.Options.IOptions<ProductionExecutionOptions> executionOptions,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
            return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await operationsRepository.GetHealthSummaryAsync(
            context, productionOrderId, executionOptions.Value, cancellationToken);

        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var s = result.Summary!;
        return TypedResults.Ok(new ProductionExecutionHealthSummaryDto(
            s.Status, s.OpenExceptionCount, s.CriticalExceptionCount,
            s.HasActiveDowntime, s.DowntimeIsStale, s.HasActiveSetup, s.SetupIsStale,
            s.QuantityProgress, s.ScheduleOverrun, s.LastOperationalActivityAtUtc, s.RequiresAttention));
    }

    // ─────────────────────────── 4L: Raise Exception ───────────────────────────

    private static async Task<IResult> RaiseExceptionAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] RaiseExceptionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Exception,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await exceptionRepository.RaiseExceptionAsync(
            context,
            new RaiseExceptionCommand(
                productionOrderId,
                body.ExpectedVersion,
                body.ExceptionType,
                body.Severity,
                body.Message?.Trim() ?? string.Empty),
            cancellationToken);

        return MapExceptionResult(result);
    }

    // ─────────────────────────── 4L: Acknowledge Exception ─────────────────────

    private static async Task<IResult> AcknowledgeExceptionAsync(
        Guid productionOrderId,
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        [FromBody] AcknowledgeExceptionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Exception,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await exceptionRepository.AcknowledgeExceptionAsync(
            context,
            new AcknowledgeExceptionCommand(productionOrderId, exceptionId, body.ExpectedVersion),
            cancellationToken);

        return MapExceptionResult(result);
    }

    // ─────────────────────────── 4L: Resolve Exception ─────────────────────────

    private static async Task<IResult> ResolveExceptionAsync(
        Guid productionOrderId,
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        [FromBody] ResolveExceptionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Exception,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await exceptionRepository.ResolveExceptionAsync(
            context,
            new ResolveExceptionCommand(
                productionOrderId, exceptionId, body.ExpectedVersion,
                body.ResolutionNote?.Trim() ?? string.Empty),
            cancellationToken);

        return MapExceptionResult(result);
    }

    // ─────────────────────────── 4L: Recover Downtime ──────────────────────────

    private static async Task<IResult> RecoverDowntimeAsync(
        Guid productionOrderId,
        Guid downtimeId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecoverDowntimeApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Downtime,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await operationsRepository.RecoverDowntimeAsync(
            context,
            new RecoverDowntimeCommand(
                productionOrderId, downtimeId, body.ExpectedVersion,
                body.Resolution?.Trim() ?? string.Empty, body.ResumeProduction),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 4L: Recover Setup ─────────────────────────────

    private static async Task<IResult> RecoverSetupSessionAsync(
        Guid productionOrderId,
        Guid setupSessionId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecoverSetupApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperationsRepository operationsRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Setup,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
            return failResult!;

        var result = await operationsRepository.RecoverSetupSessionAsync(
            context,
            new RecoverSetupSessionCommand(
                productionOrderId, setupSessionId, body.ExpectedVersion,
                body.Resolution?.Trim() ?? string.Empty),
            cancellationToken);

        return MapOperationsResult(result);
    }

    // ─────────────────────────── 5L: Shift Assignments ────────────────────────────

    private static async Task<IResult> AssignShiftAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignShiftApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionShiftRepository shiftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Assign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await shiftRepository.AssignShiftAsync(context,
            new AssignShiftCommand(productionOrderId, body.ExpectedVersion, body.ProductionShiftId, body.ShiftDate),
            cancellationToken);

        return MapAssignmentResult(result);
    }

    private static async Task<IResult> ReassignShiftAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReassignShiftApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionShiftRepository shiftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Assign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await shiftRepository.ReassignShiftAsync(context,
            new ReassignShiftCommand(productionOrderId, body.ExpectedVersion, body.ProductionShiftId, body.ShiftDate, body.Reason?.Trim() ?? string.Empty),
            cancellationToken);

        return MapAssignmentResult(result);
    }

    private static async Task<IResult> ListShiftAssignmentsAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionShiftRepository shiftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null) return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await shiftRepository.ListShiftAssignmentsAsync(context, productionOrderId, limit ?? 20, cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(sa => new ShiftAssignmentDto(
            sa.Id, sa.ProductionShiftId, sa.ShiftCode, sa.ShiftDate,
            sa.ShiftStartAtUtc, sa.ShiftEndAtUtc,
            sa.AssignedAtUtc, sa.AssignedBySubjectId,
            sa.ReleasedAtUtc, sa.ReleasedBySubjectId, sa.ReleaseReason, sa.IsActive)).ToList());
    }

    private static async Task<IResult> GetShiftExecutionSummaryAsync(
        Guid shiftId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? date,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionShiftRepository shiftRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var shiftDate))
            return TypedResults.Problem(title: "date parameter must be yyyy-MM-dd.", statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest));

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null) return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await shiftRepository.GetShiftExecutionSummaryAsync(context, shiftId, shiftDate, cancellationToken: cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var s = result.Summary!;
        return TypedResults.Ok(new ShiftExecutionSummaryDto(s.ShiftId, s.ShiftCode, s.ShiftStartAtUtc, s.ShiftEndAtUtc,
            s.ActiveOrderCount, s.CompletedOrderCount, s.GoodQuantity, s.ScrapQuantity,
            s.DowntimeSeconds, s.SetupSeconds, s.ManualCycleCount, s.OpenExceptionCount, s.CriticalExceptionCount));
    }

    // ─────────────────────────── 5L: Operator Assignments ─────────────────────────

    private static async Task<IResult> AssignOperatorAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignOperatorApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperatorRepository operatorRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Assign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await operatorRepository.AssignOperatorAsync(context,
            new AssignOperatorCommand(productionOrderId, body.ExpectedVersion, body.SubjectId?.Trim() ?? string.Empty, body.AssignmentRole?.Trim() ?? string.Empty),
            cancellationToken);

        return MapAssignmentResult(result);
    }

    private static async Task<IResult> ReleaseOperatorAsync(
        Guid productionOrderId,
        Guid assignmentId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReleaseOperatorApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperatorRepository operatorRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Assign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await operatorRepository.ReleaseOperatorAsync(context,
            new ReleaseOperatorCommand(productionOrderId, assignmentId, body.ExpectedVersion, body.Reason?.Trim() ?? string.Empty),
            cancellationToken);

        return MapAssignmentResult(result);
    }

    private static async Task<IResult> ListOperatorAssignmentsAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperatorRepository operatorRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null) return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await operatorRepository.ListOperatorAssignmentsAsync(context, productionOrderId, limit ?? 20, cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(oa => new OperatorAssignmentDto(
            oa.Id, oa.SubjectId, oa.AssignmentRole,
            oa.AssignedAtUtc, oa.AssignedBySubjectId,
            oa.ReleasedAtUtc, oa.ReleasedBySubjectId, oa.ReleaseReason, oa.IsActive)).ToList());
    }

    private static async Task<IResult> GetOperatorExecutionSummaryAsync(
        string subjectId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOperatorRepository operatorRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null) return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await operatorRepository.GetOperatorExecutionSummaryAsync(context, subjectId, cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        var s = result.Summary!;
        return TypedResults.Ok(new OperatorExecutionSummaryDto(
            s.SubjectId, s.ActiveOrderCount, s.CompletedOrderCount, s.GoodQuantity, s.ScrapQuantity, s.AssignedExceptionCount, s.LastActivityAtUtc));
    }

    // ─────────────────────────── 5L: Handovers ────────────────────────────────────

    private static async Task<IResult> CreateHandoverAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateHandoverApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionHandoverRepository handoverRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Handover,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await handoverRepository.CreateHandoverAsync(context,
            new CreateHandoverCommand(productionOrderId, body.ExpectedVersion, body.Note?.Trim() ?? string.Empty, body.OpenIssuesSummary?.Trim()),
            cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new HandoverWriteSuccessDto(result.HandoverId!.Value));
    }

    private static async Task<IResult> ListHandoversAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionHandoverRepository handoverRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null) return MapFacilityAccessFailure(accessResult);

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await handoverRepository.ListHandoversAsync(context, productionOrderId, limit ?? 10, cancellationToken);

        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(result.Items!.Select(h => new HandoverDto(
            h.Id, h.FromShiftAssignmentId, h.ToShiftAssignmentId,
            h.Note, h.OpenIssuesSummary, h.CreatedAtUtc, h.CreatedBySubjectId)).ToList());
    }

    // ─────────────────────────── 5L: Exception Routing ───────────────────────────

    private static async Task<IResult> AssignExceptionAsync(
        Guid productionOrderId,
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignExceptionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.ExceptionAssign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await exceptionRepository.AssignExceptionAsync(context,
            new AssignExceptionCommand(productionOrderId, exceptionId, body.ExpectedVersion, body.SubjectId?.Trim() ?? string.Empty),
            cancellationToken);

        return MapExceptionResult(result);
    }

    private static async Task<IResult> UnassignExceptionAsync(
        Guid productionOrderId,
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        [FromBody] UnassignExceptionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionExceptionRepository exceptionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.ExceptionAssign,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null) return failResult!;

        var result = await exceptionRepository.UnassignExceptionAsync(context,
            new UnassignExceptionCommand(productionOrderId, exceptionId, body.ExpectedVersion),
            cancellationToken);

        return MapExceptionResult(result);
    }

    private static IResult MapAssignmentResult(ProductionAssignmentWriteResult result)
    {
        if (!result.Succeeded) return MapWriteFailure(result.FailureCode!, result.FailureReason);
        return TypedResults.Ok(new AssignmentWriteSuccessDto(result.AssignmentId!.Value, result.OrderVersion!.Value));
    }

    private static IResult MapExceptionResult(ProductionExceptionWriteResult result)
    {
        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ExceptionWriteSuccessDto(
            result.ExceptionId!.Value,
            result.OrderVersion!.Value,
            result.ExceptionVersion!.Value));
    }

    private static IResult MapOperationsResult(ProductionOperationsWriteResult result)
    {
        if (!result.Succeeded)
            return MapWriteFailure(result.FailureCode!, result.FailureReason);

        return TypedResults.Ok(new ProductionOperationsWriteSuccessDto(
            result.ResourceId!.Value,
            result.OrderVersion!.Value));
    }

    private static IResult MapEntryResult(ProductionExecutionEntryWriteResult result)
    {
        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ExecutionEntryWriteSuccessDto(result.EntryId!.Value, result.OrderVersion!.Value));
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static async Task<(ProductionOrderOperationContext? context, IResult? failResult)> ResolveWrite(
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
                "A facility identifier is required."));
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

    private static ProductionOrderOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    private static IResult MapLifecycleResult(ProductionOrderWriteResult result)
    {
        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ProductionOrderWriteSuccessDto(
            result.ProductionOrderId!.Value,
            result.ReferenceCode!,
            result.Status!,
            result.Version!.Value,
            result.CreatedAtUtc!.Value,
            result.UpdatedAtUtc!.Value));
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(ProductionExecutionApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string facilityFailureCode, string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.FacilityAccessDenied));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapWriteFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ProductionOrderFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest)),
            ProductionOrderFailureCodes.HandoffNotFound => TypedResults.Problem(
                title: "Production plan handoff not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffNotFound)),
            ProductionOrderFailureCodes.HandoffNotReady => TypedResults.Problem(
                title: reason ?? "Handoff is not in a ready state.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffNotReady)),
            ProductionOrderFailureCodes.HandoffAlreadyConsumed => TypedResults.Problem(
                title: reason ?? "This handoff has already been consumed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffAlreadyConsumed)),
            ProductionOrderFailureCodes.HandoffInvalid => TypedResults.Problem(
                title: reason ?? "Handoff payload is invalid.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffInvalid)),
            ProductionOrderFailureCodes.NotFound => TypedResults.Problem(
                title: "Production order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.NotFound)),
            ProductionOrderFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This lifecycle transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidTransition)),
            ProductionOrderFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The production order was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ConcurrencyConflict)),
            ProductionOrderFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Production order service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SourceUnavailable)),
            ProductionOrderFailureCodes.InvalidQuantity => TypedResults.Problem(
                title: reason ?? "Invalid quantity.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidQuantity)),
            ProductionOrderFailureCodes.InvalidScrapReason => TypedResults.Problem(
                title: reason ?? "Invalid scrap reason.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidScrapReason)),
            ProductionOrderFailureCodes.ProductionEntryNotFound => TypedResults.Problem(
                title: "Production entry not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ProductionEntryNotFound)),
            ProductionOrderFailureCodes.ProductionEntryAlreadyReversed => TypedResults.Problem(
                title: reason ?? "Production entry has already been reversed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ProductionEntryAlreadyReversed)),
            ProductionOrderFailureCodes.ProductionTargetNotReached => TypedResults.Problem(
                title: reason ?? "Production target has not been reached.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ProductionTargetNotReached)),
            ProductionOrderFailureCodes.CompletionReasonRequired => TypedResults.Problem(
                title: reason ?? "A completion reason is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.CompletionReasonRequired)),
            ProductionOrderFailureCodes.CommentRequired => TypedResults.Problem(
                title: reason ?? "A comment is required for this scrap reason.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.CommentRequired)),

            // 3L
            ProductionOrderFailureCodes.ActiveDowntimeExists => TypedResults.Problem(
                title: reason ?? "There is already an active downtime for this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ActiveDowntimeExists)),
            ProductionOrderFailureCodes.NoActiveDowntime => TypedResults.Problem(
                title: reason ?? "There is no active downtime for this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.NoActiveDowntime)),
            ProductionOrderFailureCodes.DowntimeNotFound => TypedResults.Problem(
                title: "Downtime record not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.DowntimeNotFound)),
            ProductionOrderFailureCodes.DowntimeWrongOrder => TypedResults.Problem(
                title: reason ?? "Downtime does not belong to this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.DowntimeNotFound)),
            ProductionOrderFailureCodes.DowntimeAlreadyEnded => TypedResults.Problem(
                title: reason ?? "This downtime has already ended.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.DowntimeAlreadyEnded)),
            ProductionOrderFailureCodes.ActiveSetupExists => TypedResults.Problem(
                title: reason ?? "There is already an active setup for this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ActiveSetupExists)),
            ProductionOrderFailureCodes.NoActiveSetup => TypedResults.Problem(
                title: reason ?? "There is no active setup for this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.NoActiveSetup)),
            ProductionOrderFailureCodes.SetupSessionNotFound => TypedResults.Problem(
                title: "Setup session not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SetupSessionNotFound)),
            ProductionOrderFailureCodes.SetupSessionWrongOrder => TypedResults.Problem(
                title: reason ?? "Setup session does not belong to this order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SetupSessionNotFound)),
            ProductionOrderFailureCodes.SetupSessionAlreadyCompleted => TypedResults.Problem(
                title: reason ?? "This setup session has already been completed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SetupSessionAlreadyCompleted)),
            ProductionOrderFailureCodes.InvalidDowntimeReason => TypedResults.Problem(
                title: reason ?? "Invalid downtime reason code.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidDowntimeReason)),
            ProductionOrderFailureCodes.InvalidSetupType => TypedResults.Problem(
                title: reason ?? "Invalid setup type.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidSetupType)),
            ProductionOrderFailureCodes.InvalidCycleDuration => TypedResults.Problem(
                title: reason ?? "Invalid cycle duration.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidCycleDuration)),
            ProductionOrderFailureCodes.InvalidCycleQuantity => TypedResults.Problem(
                title: reason ?? "Invalid cycle quantity.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidCycleQuantity)),

            // 4L
            ProductionOrderFailureCodes.ExceptionNotFound => TypedResults.Problem(
                title: "Exception not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ExceptionNotFound)),
            ProductionOrderFailureCodes.ExceptionAlreadyResolved => TypedResults.Problem(
                title: reason ?? "This exception has already been resolved.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ExceptionAlreadyResolved)),
            ProductionOrderFailureCodes.InvalidExceptionType => TypedResults.Problem(
                title: reason ?? "Invalid exception type.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidExceptionType)),
            ProductionOrderFailureCodes.InvalidExceptionSeverity => TypedResults.Problem(
                title: reason ?? "Invalid exception severity.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidExceptionSeverity)),
            ProductionOrderFailureCodes.ResolutionNoteRequired => TypedResults.Problem(
                title: reason ?? "A resolution note is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ResolutionNoteRequired)),
            ProductionOrderFailureCodes.CriticalExceptionOpen => TypedResults.Problem(
                title: reason ?? "Unresolved critical exceptions must be resolved before completing.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.CriticalExceptionOpen)),
            ProductionOrderFailureCodes.ProductionStateInconsistent => TypedResults.Problem(
                title: reason ?? "Production state is inconsistent.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ProductionStateInconsistent)),
            ProductionOrderFailureCodes.DowntimeNotActive => TypedResults.Problem(
                title: reason ?? "Downtime is not active and cannot be recovered.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.DowntimeNotActive)),
            ProductionOrderFailureCodes.SetupSessionNotActive => TypedResults.Problem(
                title: reason ?? "Setup session is not active and cannot be recovered.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SetupSessionNotActive)),
            ProductionOrderFailureCodes.MessageTooLong => TypedResults.Problem(
                title: reason ?? "Message exceeds maximum allowed length.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest)),

            // 5L
            ProductionOrderFailureCodes.ShiftNotFound => TypedResults.Problem(
                title: "Shift not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ShiftNotFound)),
            ProductionOrderFailureCodes.ShiftNotActive => TypedResults.Problem(
                title: reason ?? "Shift is not active.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ShiftNotActive)),
            ProductionOrderFailureCodes.ShiftAssignmentNotFound => TypedResults.Problem(
                title: "Shift assignment not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ShiftAssignmentNotFound)),
            ProductionOrderFailureCodes.ShiftAssignmentNotActive => TypedResults.Problem(
                title: reason ?? "An active shift assignment already exists.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ShiftAssignmentExists)),
            ProductionOrderFailureCodes.OperatorAssignmentNotFound => TypedResults.Problem(
                title: "Operator assignment not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.OperatorAssignmentNotFound)),
            ProductionOrderFailureCodes.OperatorAssignmentNotActive => TypedResults.Problem(
                title: reason ?? "Operator assignment is not active.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.OperatorAssignmentNotActive)),
            ProductionOrderFailureCodes.InvalidOperatorRole => TypedResults.Problem(
                title: reason ?? "Invalid operator assignment role.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidOperatorRole)),
            ProductionOrderFailureCodes.PrimaryOperatorRequired => TypedResults.Problem(
                title: reason ?? "A primary operator is required before starting production.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.PrimaryOperatorRequired)),
            ProductionOrderFailureCodes.ShiftRequired => TypedResults.Problem(
                title: reason ?? "A shift assignment is required before starting production.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ShiftRequired)),
            ProductionOrderFailureCodes.OperatorNotFacilityMember => TypedResults.Problem(
                title: reason ?? "Target subject is not a member of this facility.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.OperatorNotFacilityMember)),
            ProductionOrderFailureCodes.AssignmentTargetEmpty => TypedResults.Problem(
                title: reason ?? "Assignment target identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.AssignmentTargetEmpty)),
            ProductionOrderFailureCodes.HandoverActiveShiftRequired => TypedResults.Problem(
                title: reason ?? "An active shift assignment is required to create a handover.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoverActiveShiftRequired)),
            ProductionOrderFailureCodes.HandoverNoteRequired => TypedResults.Problem(
                title: reason ?? "Handover note is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoverNoteRequired)),
            ProductionOrderFailureCodes.ExceptionNotAssigned => TypedResults.Problem(
                title: reason ?? "Exception is not currently assigned.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ExceptionNotAssigned)),
            ProductionOrderFailureCodes.OrderAlreadyCompleted => TypedResults.Problem(
                title: reason ?? "Operation not allowed on completed order.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.OrderAlreadyCompleted)),

            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}

public static class ProductionExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSProductionExecution(this IServiceCollection services)
    {
        services.AddScoped<IProductionOrderReadRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOrderReadRepository()
                : new ProductionOrderReadRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionExecutionEntryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<CrevaFactOSDbContext>();
            return new ProductionExecutionEntryRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionOperationsRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOperationsRepository()
                : new ProductionOperationsRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        // 4L — exception repository
        services.AddScoped<IProductionExceptionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionExceptionRepository()
                : new ProductionExceptionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionFollowUpRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionFollowUpRepository()
                : new ProductionFollowUpRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        // 4L — execution options (development defaults; override in appsettings)
        services.AddOptions<ProductionExecutionOptions>()
            .BindConfiguration(ProductionExecutionOptions.SectionName);

        // 5L — shift, operator, handover repositories
        services.AddScoped<IProductionShiftRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionShiftRepository()
                : new ProductionShiftRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionOperatorRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOperatorRepository()
                : new ProductionOperatorRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionHandoverRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionHandoverRepository()
                : new ProductionHandoverRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        // 5L — execution policy options
        services.AddOptions<ProductionExecutionPolicyOptions>()
            .BindConfiguration(ProductionExecutionPolicyOptions.SectionName);

        // Override ProductionOrderWriteRepository to inject policy options
        services.AddScoped<IProductionOrderWriteRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOrderWriteRepository()
                : new ProductionOrderWriteRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ProductionExecutionPolicyOptions>>());
        });

        services.AddScoped<IProductionPlanningLogisticsSignalReader>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanningLogisticsSignalReader()
                : new ProductionPlanningLogisticsSignalReader(dbContext);
        });

        services.AddScoped<IProductionReportingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionReportingRepository()
                : new ProductionReportingRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IProductionPlanningLogisticsSignalReader>());
        });

        return services;
    }
}
