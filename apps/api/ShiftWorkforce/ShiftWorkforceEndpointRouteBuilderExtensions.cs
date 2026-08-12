using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.ShiftWorkforce;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.ShiftWorkforce;

public static class ShiftWorkforceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShiftWorkforceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shift-workforce").RequireAuthorization();

        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/persons", ListPersonsAsync);
        group.MapGet("/persons/{personId:guid}", GetPersonAsync);
        group.MapGet("/competency-definitions", ListCompetencyDefinitionsAsync);
        group.MapGet("/persons/{personId:guid}/competencies", ListPersonCompetenciesAsync);
        group.MapPost("/persons/{personId:guid}/competencies", CreateCompetencyAsync);
        group.MapPut("/persons/{personId:guid}/competencies/{assignmentId:guid}", UpdateCompetencyAsync);
        group.MapPost("/synchronize", SynchronizeAsync);

        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetOverviewAsync(context, cancellationToken);
        var canManage = await ShiftWorkforceEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            totalWorkforce = overview.TotalWorkforce,
            activeWorkforce = overview.ActiveWorkforce,
            currentlyAvailable = overview.CurrentlyAvailable,
            unavailableOrLeave = overview.UnavailableOrLeave,
            staleSourceRecords = overview.StaleSourceRecords,
            competencyAttentionCount = overview.CompetencyAttentionCount,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListPersonsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        [FromQuery] string? isActive,
        [FromQuery] string? availability,
        [FromQuery] string? competencyCode,
        [FromQuery] string? freshness,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IShiftWorkforceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        bool? activeFilter = isActive switch
        {
            "true" => true,
            "false" => false,
            _ => null,
        };

        var result = await repository.ListPersonsAsync(
            context,
            new ShiftWorkforcePersonListFilters(
                search, activeFilter, availability, competencyCode, freshness, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapPersonSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> GetPersonAsync(
        Guid personId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var person = await repository.GetPersonByIdAsync(context, personId, cancellationToken);
        return person is null
            ? ShiftWorkforceEndpointHelpers.MapFailure(ShiftWorkforceFailureCodes.NotFound, "Workforce person not found.")
            : TypedResults.Ok(MapPersonDetail(person));
    }

    private static async Task<IResult> ListCompetencyDefinitionsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var definitions = await repository.ListCompetencyDefinitionsAsync(context, cancellationToken);
        return TypedResults.Ok(definitions.Select(d => new
        {
            code = d.Code,
            displayLabelTr = d.DisplayLabelTr,
            descriptionTr = d.DescriptionTr,
            isActive = d.IsActive,
            sortOrder = d.SortOrder,
        }));
    }

    private static async Task<IResult> ListPersonCompetenciesAsync(
        Guid personId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var competencies = await repository.ListPersonCompetenciesAsync(context, personId, cancellationToken);
        return TypedResults.Ok(competencies.Select(MapCompetency));
    }

    private static async Task<IResult> CreateCompetencyAsync(
        Guid personId,
        [FromBody] CreateShiftWorkforceCompetencyRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateCompetencyAssignmentAsync(
            context,
            personId,
            new CreateShiftWorkforceCompetencyCommand(
                request.CompetencyDefinitionCode,
                request.MachineFamilyScope,
                request.ProcessScope,
                request.MaterialScope,
                request.EquipmentScope,
                request.CompetencyLevel,
                request.VerificationStatus,
                request.ValidFromUtc,
                request.ValidUntilUtc,
                request.OperationalNote,
                request.IdempotencyKey),
            cancellationToken);

        return ShiftWorkforceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateCompetencyAsync(
        Guid personId,
        Guid assignmentId,
        [FromBody] UpdateShiftWorkforceCompetencyRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateCompetencyAssignmentAsync(
            context,
            personId,
            new UpdateShiftWorkforceCompetencyCommand(
                assignmentId,
                request.ExpectedVersion,
                request.CompetencyLevel,
                request.VerificationStatus,
                request.ValidFromUtc,
                request.ValidUntilUtc,
                request.OperationalNote),
            cancellationToken);

        return ShiftWorkforceEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> SynchronizeAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftWorkforceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftWorkforceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.SynchronizeAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            createdCount = result.CreatedCount,
            updatedCount = result.UpdatedCount,
            unchangedCount = result.UnchangedCount,
        });
    }

    private static object MapPersonSummary(ShiftWorkforcePersonSummaryReadModel person) => new
    {
        id = person.Id,
        employeeCode = person.EmployeeCode,
        displayName = person.DisplayName,
        department = person.Department,
        jobTitle = person.JobTitle,
        isActive = person.IsActive,
        availabilityState = person.AvailabilityState,
        availabilityLabel = ShiftWorkforceValueConversions.ToDisplayAvailabilityState(person.AvailabilityState),
        shiftLabel = person.ShiftLabel,
        sourceSystem = person.SourceSystem,
        sourceLabel = ShiftWorkforceValueConversions.ToDisplaySourceSystem(person.SourceSystem),
        freshnessState = person.FreshnessState,
        freshnessLabel = ShiftWorkforceValueConversions.ToDisplayFreshnessState(person.FreshnessState),
        competencySummary = person.CompetencySummary,
        competencyAttention = person.CompetencyAttention,
    };

    private static object MapPersonDetail(ShiftWorkforcePersonDetailReadModel person) => new
    {
        id = person.Id,
        employeeCode = person.EmployeeCode,
        displayName = person.DisplayName,
        department = person.Department,
        jobTitle = person.JobTitle,
        isActive = person.IsActive,
        availabilityState = person.AvailabilityState,
        availabilityLabel = ShiftWorkforceValueConversions.ToDisplayAvailabilityState(person.AvailabilityState),
        shiftLabel = person.ShiftLabel,
        sourceSystem = person.SourceSystem,
        sourceLabel = ShiftWorkforceValueConversions.ToDisplaySourceSystem(person.SourceSystem),
        freshnessState = person.FreshnessState,
        freshnessLabel = ShiftWorkforceValueConversions.ToDisplayFreshnessState(person.FreshnessState),
        sourceShiftReference = person.SourceShiftReference,
        sourceCalendarReference = person.SourceCalendarReference,
        sourceUpdatedAtUtc = person.SourceUpdatedAtUtc,
        synchronizedAtUtc = person.SynchronizedAtUtc,
        sourceFingerprint = person.SourceFingerprint,
        sourceLimitations = person.SourceLimitations,
        isPilotData = person.IsPilotData,
        competencies = person.Competencies.Select(MapCompetency),
        version = person.Version,
    };

    private static object MapCompetency(ShiftWorkforceCompetencyAssignmentReadModel competency) => new
    {
        id = competency.Id,
        competencyCode = competency.CompetencyCode,
        competencyLabel = competency.CompetencyLabel,
        machineFamilyScope = competency.MachineFamilyScope,
        processScope = competency.ProcessScope,
        materialScope = competency.MaterialScope,
        equipmentScope = competency.EquipmentScope,
        competencyLevel = competency.CompetencyLevel,
        competencyLevelLabel = ShiftWorkforceValueConversions.ToDisplayCompetencyLevel(competency.CompetencyLevel),
        verificationStatus = competency.VerificationStatus,
        verificationStatusLabel = ShiftWorkforceValueConversions.ToDisplayVerificationStatus(competency.VerificationStatus),
        validFromUtc = competency.ValidFromUtc,
        validUntilUtc = competency.ValidUntilUtc,
        verifiedAtUtc = competency.VerifiedAtUtc,
        operationalNote = competency.OperationalNote,
        version = competency.Version,
        requiresAttention = competency.RequiresAttention,
    };
}

public sealed record CreateShiftWorkforceCompetencyRequest(
    string CompetencyDefinitionCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string? MaterialScope,
    string? EquipmentScope,
    string CompetencyLevel,
    string VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    string? OperationalNote,
    string IdempotencyKey);

public sealed record UpdateShiftWorkforceCompetencyRequest(
    uint ExpectedVersion,
    string? CompetencyLevel,
    string? VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    string? OperationalNote);

public static class ShiftWorkforceServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSShiftWorkforce(this IServiceCollection services)
    {
        services.AddScoped<IWorkforceSourceProvider, LocalDevelopmentWorkforceSourceProvider>();
        services.AddScoped<ShiftWorkforceSourceSynchronizer>();
        services.AddScoped<IShiftWorkforceRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableShiftWorkforceRepository()
                : new ShiftWorkforceRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<ShiftWorkforceSourceSynchronizer>());
        });

        return services;
    }
}
