using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.ProductionPlanning;

public sealed class PlanningLogisticsConstraintRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IPlanningLogisticsSourceReader sourceReader,
    IPlanningMaterialInboundContextReader materialInboundContextReader)
    : IPlanningLogisticsConstraintRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
    };

    public async Task<PlanningLogisticsEvaluationReadResult> GetLatestAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionPlanId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || facilityId == Guid.Empty || productionPlanId == Guid.Empty)
        {
            return PlanningLogisticsEvaluationReadResult.Fail(
                PlanningLogisticsFailureCodes.InvalidScope,
                "Tenant, facility, and production plan identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var entity = await dbContext.PlanningLogisticsConstraintEvaluations
                        .AsNoTracking()
                        .Where(e =>
                            e.TenantId == tenantId
                            && e.FacilityId == facilityId
                            && e.ProductionPlanId == productionPlanId)
                        .OrderByDescending(e => e.EvaluatedAtUtc)
                        .ThenByDescending(e => e.Version)
                        .FirstOrDefaultAsync(ct);

                    return entity is null
                        ? PlanningLogisticsEvaluationReadResult.Missing()
                        : PlanningLogisticsEvaluationReadResult.Success(MapEntity(entity));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PlanningLogisticsEvaluationReadResult.Fail(
                PlanningLogisticsFailureCodes.SourceUnavailable,
                "Planning logistics evaluation service is unavailable.");
        }
    }

    public async Task<PlanningLogisticsEvaluationWriteResult> EvaluateAsync(
        PlanningLogisticsEvaluateContext context,
        PlanningLogisticsEvaluateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return PlanningLogisticsEvaluationWriteResult.Fail(
                PlanningLogisticsFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var plan = await dbContext.ProductionPlans
                        .SingleOrDefaultAsync(
                            p =>
                                p.TenantId == context.TenantId
                                && p.FacilityId == context.FacilityId
                                && p.Id == command.ProductionPlanId,
                            ct);

                    if (plan is null)
                    {
                        return PlanningLogisticsEvaluationWriteResult.Fail(
                            PlanningLogisticsFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    var source = await sourceReader.ReadForPlanAsync(
                        context.TenantId,
                        context.FacilityId,
                        plan,
                        ct);

                    var orderReference = source.SourceOrderReference
                        ?? PlanningLogisticsSourceReader.ExtractOrderReferences(plan).FirstOrDefault();

                    var materialContext = await materialInboundContextReader.ReadAsync(
                        context.TenantId,
                        context.FacilityId,
                        orderReference,
                        ct);

                    var now = DateTimeOffset.UtcNow;
                    var evaluation = PlanningLogisticsConstraintEngine.Evaluate(
                        new PlanningLogisticsConstraintEngine.EvaluateInput(
                            plan,
                            source,
                            PlanningLogisticsBufferPolicy.DevelopmentDefault,
                            materialContext,
                            now));

                    var previous = await dbContext.PlanningLogisticsConstraintEvaluations
                        .Where(e =>
                            e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.ProductionPlanId == plan.Id)
                        .OrderByDescending(e => e.EvaluatedAtUtc)
                        .FirstOrDefaultAsync(ct);

                    var entity = new PlanningLogisticsConstraintEvaluation
                    {
                        Id = evaluation.EvaluationId,
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionPlanId = plan.Id,
                        SourceExportCaseId = evaluation.ExportCaseId,
                        SourceExportCaseNumber = evaluation.ExportCaseNumber,
                        SourceShipmentId = evaluation.ShipmentId,
                        SourceShipmentReference = evaluation.ShipmentReference,
                        SourceOrderReference = evaluation.SourceOrderReference,
                        SourceVersion = evaluation.SourceVersion,
                        PickupDeadlineUtc = evaluation.PickupDeadlineUtc,
                        DepartureDeadlineUtc = evaluation.DepartureDeadlineUtc,
                        LatestProductionCompletionUtc = evaluation.LatestProductionCompletionUtc,
                        PlannedProductionCompletionUtc = evaluation.PlannedProductionCompletionUtc,
                        SlackMinutes = evaluation.SlackMinutes,
                        RiskLevel = PlanningLogisticsRiskPolicy.ToStorage(evaluation.RiskLevel),
                        Blocking = evaluation.Blocking,
                        BlockingReason = evaluation.BlockingReason,
                        LinkageStatus = evaluation.LinkageStatus,
                        ConstraintsJson = SerializeConstraints(evaluation.Constraints),
                        EvaluatedAtUtc = evaluation.EvaluatedAtUtc,
                        Version = (previous?.Version ?? 0) + 1,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    };

                    dbContext.PlanningLogisticsConstraintEvaluations.Add(entity);

                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.PlanningLogisticsConstraintsEvaluated,
                        Action = AuditActions.PlanningLogisticsConstraintsEvaluate,
                        ResourceType = "production.plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            evaluationId = entity.Id.ToString(),
                            productionPlanId = plan.Id.ToString(),
                            sourceExportCaseId = entity.SourceExportCaseId?.ToString(),
                            sourceShipmentId = entity.SourceShipmentId?.ToString(),
                            riskLevel = entity.RiskLevel,
                            slackMinutes = entity.SlackMinutes,
                            blocking = entity.Blocking,
                        }),
                    });

                    var riskChanged = previous is null
                        || !string.Equals(previous.RiskLevel, entity.RiskLevel, StringComparison.Ordinal);

                    if (riskChanged)
                    {
                        dbContext.AuditRecords.Add(new AuditRecord
                        {
                            Id = Guid.NewGuid(),
                            TenantId = context.TenantId,
                            IdentitySubjectId = context.IdentitySubjectId,
                            TenantMembershipId = context.MembershipId,
                            EventType = AuditEventTypes.PlanningLogisticsRiskChanged,
                            Action = AuditActions.PlanningLogisticsRiskChange,
                            ResourceType = "production.plan",
                            ResourceId = plan.Id.ToString(),
                            Outcome = AuditOutcome.Success,
                            PermissionKey = context.PermissionKey,
                            CorrelationId = correlationId,
                            Source = AuditSource.Api,
                            OccurredAtUtc = now,
                            RecordedAtUtc = now,
                            MetadataJson = JsonSerializer.Serialize(new
                            {
                                previousRiskLevel = previous?.RiskLevel,
                                currentRiskLevel = entity.RiskLevel,
                                slackMinutes = entity.SlackMinutes,
                            }),
                        });
                    }

                    if (riskChanged && IsHighOrCritical(entity.RiskLevel))
                    {
                        dbContext.OutboxMessages.Add(new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            TenantId = context.TenantId,
                            MessageType = OutboxMessageTypes.PlanningLogisticsRiskHigh,
                            SchemaVersion = OutboxMessageTypes.SchemaVersion,
                            AggregateType = "production-plan",
                            AggregateId = plan.Id.ToString(),
                            CorrelationId = correlationId,
                            IdempotencyKey = $"{plan.Id}:{entity.Version}:{OutboxMessageTypes.PlanningLogisticsRiskHigh}",
                            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["productionPlanId"] = plan.Id.ToString(),
                                ["evaluationId"] = entity.Id.ToString(),
                                ["riskLevel"] = entity.RiskLevel,
                                ["slackMinutes"] = entity.SlackMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            }),
                            HeadersJson = "{}",
                            Status = OutboxMessageStatus.Pending,
                            OccurredAtUtc = now,
                            RecordedAtUtc = now,
                            AvailableAtUtc = now,
                            AttemptCount = 0,
                        });
                    }

                    if (riskChanged && entity.SlackMinutes < 0)
                    {
                        dbContext.OutboxMessages.Add(new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            TenantId = context.TenantId,
                            MessageType = OutboxMessageTypes.PlanningLogisticsDeadlineBreached,
                            SchemaVersion = OutboxMessageTypes.SchemaVersion,
                            AggregateType = "production-plan",
                            AggregateId = plan.Id.ToString(),
                            CorrelationId = correlationId,
                            IdempotencyKey = $"{plan.Id}:{entity.Version}:{OutboxMessageTypes.PlanningLogisticsDeadlineBreached}",
                            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["productionPlanId"] = plan.Id.ToString(),
                                ["evaluationId"] = entity.Id.ToString(),
                                ["slackMinutes"] = entity.SlackMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            }),
                            HeadersJson = "{}",
                            Status = OutboxMessageStatus.Pending,
                            OccurredAtUtc = now,
                            RecordedAtUtc = now,
                            AvailableAtUtc = now,
                            AttemptCount = 0,
                        });
                    }

                    await dbContext.SaveChangesAsync(ct);

                    var readModel = MapEntity(entity, evaluation.Constraints);
                    return PlanningLogisticsEvaluationWriteResult.Success(readModel);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PlanningLogisticsEvaluationWriteResult.Fail(
                PlanningLogisticsFailureCodes.SourceUnavailable,
                "Planning logistics evaluation service is unavailable.");
        }
    }

    private static bool IsHighOrCritical(string riskLevel) =>
        string.Equals(riskLevel, "high", StringComparison.Ordinal)
        || string.Equals(riskLevel, "critical", StringComparison.Ordinal);

    private static string SerializeConstraints(
        IReadOnlyList<PlanningLogisticsConstraintReadModel> constraints) =>
        JsonSerializer.Serialize(constraints, JsonOptions);

    private static PlanningLogisticsEvaluationReadModel MapEntity(
        PlanningLogisticsConstraintEvaluation entity,
        IReadOnlyList<PlanningLogisticsConstraintReadModel>? constraintsOverride = null)
    {
        var constraints = constraintsOverride ?? DeserializeConstraints(entity.ConstraintsJson);
        return new PlanningLogisticsEvaluationReadModel(
            entity.Id,
            entity.ProductionPlanId,
            entity.SourceExportCaseId,
            entity.SourceExportCaseNumber,
            entity.SourceShipmentId,
            entity.SourceShipmentReference,
            entity.SourceOrderReference,
            entity.PickupDeadlineUtc,
            entity.DepartureDeadlineUtc,
            entity.LatestProductionCompletionUtc,
            entity.PlannedProductionCompletionUtc,
            entity.SlackMinutes,
            PlanningLogisticsRiskPolicy.FromStorage(entity.RiskLevel),
            entity.Blocking,
            entity.BlockingReason,
            entity.SourceVersion,
            entity.EvaluatedAtUtc,
            entity.Version,
            constraints,
            entity.LinkageStatus);
    }

    private static IReadOnlyList<PlanningLogisticsConstraintReadModel> DeserializeConstraints(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<PlanningLogisticsConstraintReadModel>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PlanningLogisticsConstraintReadModel>>(json, JsonOptions)
                ?? new List<PlanningLogisticsConstraintReadModel>();
        }
        catch (JsonException)
        {
            return Array.Empty<PlanningLogisticsConstraintReadModel>();
        }
    }
}

public sealed class UnavailablePlanningLogisticsConstraintRepository : IPlanningLogisticsConstraintRepository
{
    public Task<PlanningLogisticsEvaluationReadResult> GetLatestAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionPlanId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            PlanningLogisticsEvaluationReadResult.Fail(
                PlanningLogisticsFailureCodes.SourceUnavailable,
                "Planning logistics evaluation service is unavailable."));

    public Task<PlanningLogisticsEvaluationWriteResult> EvaluateAsync(
        PlanningLogisticsEvaluateContext context,
        PlanningLogisticsEvaluateCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            PlanningLogisticsEvaluationWriteResult.Fail(
                PlanningLogisticsFailureCodes.SourceUnavailable,
                "Planning logistics evaluation service is unavailable."));
}
