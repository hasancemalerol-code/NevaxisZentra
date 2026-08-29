using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialPricingScenarioRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialPricingScenarioRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<CommercialPricingScenarioWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canReview,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var scenarios = await dbContext.CommercialPricingScenarios.AsNoTracking()
                    .Where(scenario => scenario.TenantId == context.TenantId && scenario.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                return new CommercialPricingScenarioWorkspaceReadModel(
                    CommercialPricingScenarioConstants.RuleSetVersion,
                    CommercialIntelligenceConstants.ProfitabilityDevelopmentPilotProvenance,
                    scenarios.Count(s => s.LifecycleStatus == CommercialPricingScenarioLifecycleStatuses.Draft),
                    scenarios.Count(s => s.LifecycleStatus == CommercialPricingScenarioLifecycleStatuses.NeedsReview),
                    scenarios.Count(s => s.LifecycleStatus == CommercialPricingScenarioLifecycleStatuses.EligibleForApproval),
                    scenarios.Count(s => s.LifecycleStatus == CommercialPricingScenarioLifecycleStatuses.Blocked),
                    scenarios.Count(s => s.LifecycleStatus == CommercialPricingScenarioLifecycleStatuses.Approved),
                    true,
                    canManage,
                    canReview,
                    canCreateTask);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialPricingScenarioSummaryReadModel>> ListScenariosAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPricingScenarioListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialPricingScenarios.AsNoTracking()
                    .Where(scenario => scenario.TenantId == context.TenantId && scenario.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.LifecycleStatus))
                {
                    query = query.Where(scenario => scenario.LifecycleStatus == filters.LifecycleStatus.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.GuardrailResult))
                {
                    query = query.Where(scenario => scenario.GuardrailResult == filters.GuardrailResult.Trim());
                }

                if (filters.ProfileId.HasValue)
                {
                    query = query.Where(scenario => scenario.ProfileId == filters.ProfileId.Value);
                }

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(scenario => scenario.MaterialId == filters.MaterialId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.CustomerReference))
                {
                    query = query.Where(scenario => scenario.CustomerReference == filters.CustomerReference.Trim());
                }

                var scenarios = await query
                    .OrderByDescending(scenario => scenario.UpdatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialPricingScenarioSummaryReadModel>)scenarios.Select(MapSummary).ToList();
            },
            cancellationToken);

    public Task<CommercialPricingScenarioDetailReadModel?> GetScenarioByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid scenarioId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var scenario = await dbContext.CommercialPricingScenarios.AsNoTracking()
                    .Include(s => s.AuditEvents)
                    .Include(s => s.EvaluationSnapshots)
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == scenarioId,
                        ct);

                return scenario is null ? null : MapDetail(scenario);
            },
            cancellationToken);

    public Task<CommercialPricingScenarioWriteResult> CreateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateCommand(command);
                if (validationError is not null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var (profile, salesEvidence, loadError) = await LoadProfileAndSalesEvidenceAsync(
                    context,
                    command.ProfileId,
                    command.ExpectedProfileVersion,
                    command.SalesEvidenceObservationId,
                    ct);

                if (loadError is not null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        loadError.Value.Code,
                        loadError.Value.Reason);
                }

                if (!salesEvidence!.ComparableNetSalesUnitPrice.HasValue
                    || salesEvidence.ComparableNetSalesUnitPrice.Value <= 0m)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Referans satış kanıtında geçerli birim fiyat bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var scenario = new CommercialPricingScenario
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ScenarioReference = await GenerateScenarioReferenceAsync(context, ct),
                    LifecycleStatus = CommercialPricingScenarioLifecycleStatuses.Draft,
                    ProfileId = profile!.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MaterialId = salesEvidence.MaterialId,
                    CustomerReference = salesEvidence.CustomerReference,
                    SalesEvidenceObservationId = salesEvidence.Id,
                    Quantity = command.Quantity,
                    BaselineUnitPrice = salesEvidence.ComparableNetSalesUnitPrice.Value,
                    ProposedUnitPrice = command.ProposedUnitPrice,
                    ProposedDiscountRatio = command.ProposedDiscountRatio,
                    ValidityWindowStartUtc = command.ValidityWindowStartUtc,
                    ValidityWindowEndUtc = command.ValidityWindowEndUtc,
                    CommercialRationale = command.CommercialRationale.Trim(),
                    SourceProvenance = command.SourceProvenance.Trim(),
                    Version = 1,
                    EvaluationCount = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                };

                dbContext.CommercialPricingScenarios.Add(scenario);
                await AppendAuditEventAsync(
                    scenario,
                    CommercialPricingScenarioAuditEventTypes.Created,
                    new { scenario.ProfileId, scenario.ProposedUnitPrice },
                    actor,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                var detail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                return CommercialPricingScenarioWriteResult.Success(scenario.Id, scenario.Version, scenario: detail);
            },
            cancellationToken);

    public Task<CommercialPricingScenarioWriteResult> UpdateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateUpdateCommand(command);
                if (validationError is not null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var scenario = await dbContext.CommercialPricingScenarios
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == command.ScenarioId,
                        ct);

                if (scenario is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Fiyat senaryosu bulunamadı.");
                }

                if (scenario.Version != command.ExpectedVersion)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Senaryo sürümü güncel değil.");
                }

                if (!CanEditScenario(scenario.LifecycleStatus))
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Bu senaryo durumunda düzenleme yapılamaz.");
                }

                var (profile, salesEvidence, loadError) = await LoadProfileAndSalesEvidenceAsync(
                    context,
                    command.ProfileId,
                    command.ExpectedProfileVersion,
                    command.SalesEvidenceObservationId,
                    ct);

                if (loadError is not null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        loadError.Value.Code,
                        loadError.Value.Reason);
                }

                if (!salesEvidence!.ComparableNetSalesUnitPrice.HasValue
                    || salesEvidence.ComparableNetSalesUnitPrice.Value <= 0m)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Referans satış kanıtında geçerli birim fiyat bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var wasEvaluated = scenario.LifecycleStatus != CommercialPricingScenarioLifecycleStatuses.Draft;

                scenario.ProfileId = profile!.Id;
                scenario.ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
                scenario.MaterialId = salesEvidence.MaterialId;
                scenario.CustomerReference = salesEvidence.CustomerReference;
                scenario.SalesEvidenceObservationId = salesEvidence.Id;
                scenario.Quantity = command.Quantity;
                scenario.BaselineUnitPrice = salesEvidence.ComparableNetSalesUnitPrice.Value;
                scenario.ProposedUnitPrice = command.ProposedUnitPrice;
                scenario.ProposedDiscountRatio = command.ProposedDiscountRatio;
                scenario.ValidityWindowStartUtc = command.ValidityWindowStartUtc;
                scenario.ValidityWindowEndUtc = command.ValidityWindowEndUtc;
                scenario.CommercialRationale = command.CommercialRationale.Trim();
                scenario.LifecycleStatus = CommercialPricingScenarioLifecycleStatuses.Draft;
                scenario.GuardrailResult = null;
                scenario.LatestEvaluationSnapshotId = null;
                scenario.UpdatedAtUtc = now;
                scenario.UpdatedBy = actor;
                scenario.Version += 1;

                await AppendAuditEventAsync(
                    scenario,
                    wasEvaluated
                        ? CommercialPricingScenarioAuditEventTypes.ReturnedToDraft
                        : CommercialPricingScenarioAuditEventTypes.Updated,
                    new { scenario.ProposedUnitPrice, returnedFromEvaluation = wasEvaluated },
                    actor,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                var detail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                return CommercialPricingScenarioWriteResult.Success(scenario.Id, scenario.Version, scenario: detail);
            },
            cancellationToken);

    public Task<CommercialPricingScenarioWriteResult> EvaluateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var scenario = await dbContext.CommercialPricingScenarios
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == command.ScenarioId,
                        ct);

                if (scenario is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Fiyat senaryosu bulunamadı.");
                }

                if (scenario.Version != command.ExpectedVersion)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Senaryo sürümü güncel değil.");
                }

                if (scenario.LifecycleStatus != CommercialPricingScenarioLifecycleStatuses.Draft)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Yalnızca taslak senaryolar değerlendirilebilir.");
                }

                var profile = await dbContext.CommercialProfitabilityProfiles
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId
                             && p.Id == scenario.ProfileId,
                        ct);

                if (profile is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Kârlılık profili bulunamadı.");
                }

                if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Yalnızca onaylı kârlılık profilleri kullanılabilir.");
                }

                var salesEvidence = await dbContext.CommercialProfitabilityEvidenceObservations.AsNoTracking()
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId
                             && e.FacilityId == context.FacilityId
                             && e.Id == scenario.SalesEvidenceObservationId
                             && e.ObservationKind == CommercialProfitabilityObservationKinds.Sales,
                        ct);

                if (salesEvidence is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Satış kanıtı bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var costEvidence = await CommercialProfitabilityCostEvidenceReader.ReadAsync(
                    dbContext,
                    context,
                    profile,
                    salesEvidence,
                    now,
                    ct);
                var requiredKinds = CommercialProfitabilityEvaluator.ParseRequiredKinds(profile.RequiredCostComponentKinds);
                var evaluation = CommercialPricingScenarioGuardrailEvaluator.Evaluate(
                    profile,
                    salesEvidence,
                    scenario.ProposedUnitPrice,
                    costEvidence,
                    requiredKinds,
                    now);

                var inputSnapshot = BuildInputSnapshot(scenario, profile);
                var outputSnapshot = BuildOutputSnapshot(evaluation);
                var snapshot = new CommercialPricingScenarioEvaluationSnapshot
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ScenarioId = scenario.Id,
                    ScenarioVersionAtEvaluation = scenario.Version,
                    EvaluationSequence = scenario.EvaluationCount + 1,
                    GuardrailResult = evaluation.GuardrailResult,
                    AssessmentOutcome = evaluation.AssessmentOutcome,
                    EvidenceStatus = evaluation.EvidenceStatus,
                    CostCoverageStatus = evaluation.CostCoverageStatus,
                    LeadingDriverCode = evaluation.LeadingDriverCode,
                    InsufficientDataReason = evaluation.InsufficientDataReason,
                    BaselineUnitPrice = evaluation.BaselineUnitPrice,
                    ProposedUnitPrice = evaluation.ProposedUnitPrice,
                    TotalIncludedUnitCost = evaluation.TotalIncludedUnitCost,
                    ExpectedUnitContribution = evaluation.ExpectedUnitContribution,
                    ExpectedContributionMarginRatio = evaluation.ExpectedContributionMarginRatio,
                    BaselineContributionMarginRatio = evaluation.BaselineContributionMarginRatio,
                    ContributionMarginDelta = evaluation.ContributionMarginDelta,
                    ProfileMinimumContributionMarginRatio = evaluation.ProfileMinimumContributionMarginRatio,
                    ProfileId = profile.Id,
                    ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    InputSnapshotJson = JsonSerializer.Serialize(inputSnapshot, JsonOptions),
                    OutputSnapshotJson = JsonSerializer.Serialize(outputSnapshot, JsonOptions),
                    EvaluatedAtUtc = now,
                    EvaluatedBy = actor,
                };

                dbContext.CommercialPricingScenarioEvaluationSnapshots.Add(snapshot);

                scenario.GuardrailResult = evaluation.GuardrailResult;
                scenario.LifecycleStatus = CommercialPricingScenarioLifecycle.MapGuardrailToLifecycle(evaluation.GuardrailResult);
                scenario.LatestEvaluationSnapshotId = snapshot.Id;
                scenario.EvaluationCount += 1;
                scenario.ProfileVersion = profile.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
                scenario.UpdatedAtUtc = now;
                scenario.UpdatedBy = actor;
                scenario.Version += 1;

                await AppendAuditEventAsync(
                    scenario,
                    CommercialPricingScenarioAuditEventTypes.Evaluated,
                    new
                    {
                        snapshotId = snapshot.Id,
                        evaluation.GuardrailResult,
                        evaluation.AssessmentOutcome,
                    },
                    actor,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                var detail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                return CommercialPricingScenarioWriteResult.Success(scenario.Id, scenario.Version, scenario: detail);
            },
            cancellationToken);

    public Task<CommercialPricingScenarioWriteResult> TransitionScenarioAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.TargetLifecycleStatus))
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Hedef durum gereklidir.");
                }

                var scenario = await dbContext.CommercialPricingScenarios
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == command.ScenarioId,
                        ct);

                if (scenario is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Fiyat senaryosu bulunamadı.");
                }

                if (scenario.Version != command.ExpectedVersion)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Senaryo sürümü güncel değil.");
                }

                var targetStatus = command.TargetLifecycleStatus.Trim();
                if (!CommercialPricingScenarioLifecycle.CanTransition(scenario.LifecycleStatus, targetStatus))
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Bu yaşam döngüsü geçişi desteklenmiyor.");
                }

                if (targetStatus == CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval
                    && scenario.GuardrailResult is not null
                    && !CommercialPricingScenarioGuardrailResults.Submittable.Contains(scenario.GuardrailResult))
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Bu koruma sonucu onaya gönderilemez.");
                }

                var noteError = CommercialPricingScenarioLifecycle.ValidateTransitionNote(targetStatus, command.Note);
                if (noteError is not null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        noteError);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previousStatus = scenario.LifecycleStatus;
                scenario.LifecycleStatus = targetStatus;
                scenario.UpdatedAtUtc = now;
                scenario.UpdatedBy = actor;
                scenario.Version += 1;

                await AppendAuditEventAsync(
                    scenario,
                    MapTransitionEventType(targetStatus),
                    new { previousStatus, targetStatus, note = command.Note?.Trim() },
                    actor,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                var detail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                return CommercialPricingScenarioWriteResult.Success(scenario.Id, scenario.Version, scenario: detail);
            },
            cancellationToken);

    public Task<CommercialPricingScenarioWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialPricingScenarioTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var scenario = await dbContext.CommercialPricingScenarios
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == command.ScenarioId,
                        ct);

                if (scenario is null)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Fiyat senaryosu bulunamadı.");
                }

                if (scenario.Version != command.ExpectedVersion)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Senaryo sürümü güncel değil.");
                }

                if (scenario.LinkedAiTaskId.HasValue)
                {
                    if (scenario.LinkedAiTaskId.Value == command.AiTaskId)
                    {
                        var existingDetail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                        return CommercialPricingScenarioWriteResult.Success(
                            scenario.Id,
                            scenario.Version,
                            idempotentReplay: true,
                            scenario: existingDetail);
                    }

                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Senaryo zaten farklı bir göreve bağlı.");
                }

                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        task => task.TenantId == context.TenantId
                                && task.FacilityId == context.FacilityId
                                && task.Id == command.AiTaskId,
                        ct);

                if (!taskExists)
                {
                    return CommercialPricingScenarioWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Bağlanacak AI görevi bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                scenario.LinkedAiTaskId = command.AiTaskId;
                scenario.UpdatedAtUtc = now;
                scenario.UpdatedBy = actor;
                scenario.Version += 1;

                await AppendAuditEventAsync(
                    scenario,
                    CommercialPricingScenarioAuditEventTypes.TaskLinked,
                    new { aiTaskId = command.AiTaskId },
                    actor,
                    now,
                    ct);
                await dbContext.SaveChangesAsync(ct);

                var detail = await GetScenarioByIdAsync(context, scenario.Id, ct);
                return CommercialPricingScenarioWriteResult.Success(scenario.Id, scenario.Version, scenario: detail);
            },
            cancellationToken);

    private static bool CanEditScenario(string lifecycleStatus) =>
        lifecycleStatus is CommercialPricingScenarioLifecycleStatuses.Draft
            or CommercialPricingScenarioLifecycleStatuses.NeedsReview
            or CommercialPricingScenarioLifecycleStatuses.EligibleForApproval
            or CommercialPricingScenarioLifecycleStatuses.Blocked
            or CommercialPricingScenarioLifecycleStatuses.InsufficientData;

    private async Task<(CommercialProfitabilityProfile? Profile, CommercialProfitabilityEvidenceObservation? SalesEvidence, (string Code, string Reason)? Error)>
        LoadProfileAndSalesEvidenceAsync(
            CommercialIntelligenceOperationContext context,
            Guid profileId,
            uint expectedProfileVersion,
            Guid salesEvidenceObservationId,
            CancellationToken cancellationToken)
    {
        var profile = await dbContext.CommercialProfitabilityProfiles
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.FacilityId == context.FacilityId
                     && p.Id == profileId,
                cancellationToken);

        if (profile is null)
        {
            return (null, null, (CommercialIntelligenceFailureCodes.NotFound, "Kârlılık profili bulunamadı."));
        }

        if (profile.Version != expectedProfileVersion)
        {
            return (null, null, (CommercialIntelligenceFailureCodes.ConcurrencyConflict, "Kârlılık profili sürümü güncel değil."));
        }

        if (!string.Equals(profile.Status, CommercialIntelligenceProfileStatuses.Approved, StringComparison.Ordinal))
        {
            return (null, null, (CommercialIntelligenceFailureCodes.ValidationFailed, "Yalnızca onaylı kârlılık profilleri kullanılabilir."));
        }

        var salesEvidence = await dbContext.CommercialProfitabilityEvidenceObservations.AsNoTracking()
            .SingleOrDefaultAsync(
                e => e.TenantId == context.TenantId
                     && e.FacilityId == context.FacilityId
                     && e.Id == salesEvidenceObservationId
                     && e.ObservationKind == CommercialProfitabilityObservationKinds.Sales,
                cancellationToken);

        if (salesEvidence is null)
        {
            return (null, null, (CommercialIntelligenceFailureCodes.NotFound, "Satış kanıtı bulunamadı."));
        }

        return (profile, salesEvidence, null);
    }

    private static string? ValidateCreateCommand(CreateCommercialPricingScenarioCommand command)
    {
        if (command.ProfileId == Guid.Empty)
        {
            return "Profil gereklidir.";
        }

        if (command.SalesEvidenceObservationId == Guid.Empty)
        {
            return "Satış kanıtı gereklidir.";
        }

        if (command.ProposedUnitPrice <= 0m)
        {
            return "Önerilen birim fiyat pozitif olmalıdır.";
        }

        if (string.IsNullOrWhiteSpace(command.CommercialRationale))
        {
            return "Ticari gerekçe gereklidir.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceProvenance))
        {
            return "Kaynak kökeni gereklidir.";
        }

        return null;
    }

    private static string? ValidateUpdateCommand(UpdateCommercialPricingScenarioCommand command)
    {
        if (command.ScenarioId == Guid.Empty)
        {
            return "Senaryo gereklidir.";
        }

        if (command.ProposedUnitPrice <= 0m)
        {
            return "Önerilen birim fiyat pozitif olmalıdır.";
        }

        if (string.IsNullOrWhiteSpace(command.CommercialRationale))
        {
            return "Ticari gerekçe gereklidir.";
        }

        return null;
    }

    private async Task<string> GenerateScenarioReferenceAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"CI-PS-{suffix}";
            var exists = await dbContext.CommercialPricingScenarios.AsNoTracking()
                .AnyAsync(s => s.TenantId == context.TenantId && s.ScenarioReference == reference, cancellationToken);
            if (!exists)
            {
                return reference;
            }
        }

        return $"CI-PS-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static object BuildInputSnapshot(CommercialPricingScenario scenario, CommercialProfitabilityProfile profile) =>
        new
        {
            scenario.ProfileId,
            profileVersion = profile.Version,
            scenario.MaterialId,
            scenario.CustomerReference,
            scenario.SalesEvidenceObservationId,
            scenario.Quantity,
            scenario.BaselineUnitPrice,
            scenario.ProposedUnitPrice,
            scenario.ProposedDiscountRatio,
            scenario.ValidityWindowStartUtc,
            scenario.ValidityWindowEndUtc,
            scenario.CommercialRationale,
        };

    private static object BuildOutputSnapshot(CommercialPricingScenarioGuardrailEvaluation evaluation) =>
        new
        {
            evaluation.GuardrailResult,
            evaluation.AssessmentOutcome,
            evaluation.EvidenceStatus,
            evaluation.CostCoverageStatus,
            evaluation.LeadingDriverCode,
            evaluation.InsufficientDataReason,
            evaluation.BaselineUnitPrice,
            evaluation.ProposedUnitPrice,
            evaluation.TotalIncludedUnitCost,
            evaluation.ExpectedUnitContribution,
            evaluation.ExpectedContributionMarginRatio,
            evaluation.BaselineContributionMarginRatio,
            evaluation.ContributionMarginDelta,
            evaluation.ProfileMinimumContributionMarginRatio,
            drivers = evaluation.Drivers.Select(driver => new
            {
                driver.ReasonCode,
                driver.Severity,
                driver.ActualValue,
                driver.ThresholdValue,
                driver.Unit,
                driver.NormalizedVariance,
                driver.AffectedComponentCount,
                driver.SupportingText,
            }),
            components = evaluation.ComponentSnapshots.Select(component => new
            {
                component.ObservationId,
                component.ComponentKind,
                component.UnitCost,
                component.CurrencyCode,
                component.UnitOfMeasure,
                component.EvidenceStatus,
                component.InclusionStatus,
            }),
        };

    private async Task AppendAuditEventAsync(
        CommercialPricingScenario scenario,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.CommercialPricingScenarioAuditEvents.Add(new CommercialPricingScenarioAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = scenario.TenantId,
            FacilityId = scenario.FacilityId,
            ScenarioId = scenario.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actor,
            OccurredAtUtc = occurredAtUtc,
        });

        await Task.CompletedTask;
    }

    private static string MapTransitionEventType(string targetStatus) =>
        targetStatus switch
        {
            CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval =>
                CommercialPricingScenarioAuditEventTypes.SubmittedForApproval,
            CommercialPricingScenarioLifecycleStatuses.Approved =>
                CommercialPricingScenarioAuditEventTypes.Approved,
            CommercialPricingScenarioLifecycleStatuses.Rejected =>
                CommercialPricingScenarioAuditEventTypes.Rejected,
            CommercialPricingScenarioLifecycleStatuses.Withdrawn =>
                CommercialPricingScenarioAuditEventTypes.Withdrawn,
            CommercialPricingScenarioLifecycleStatuses.Draft =>
                CommercialPricingScenarioAuditEventTypes.ReturnedToDraft,
            _ => "transitioned",
        };

    private static CommercialPricingScenarioSummaryReadModel MapSummary(CommercialPricingScenario scenario) =>
        new(
            scenario.Id,
            scenario.ScenarioReference,
            scenario.LifecycleStatus,
            scenario.GuardrailResult,
            scenario.ProfileId,
            scenario.MaterialId,
            scenario.CustomerReference,
            scenario.BaselineUnitPrice,
            scenario.ProposedUnitPrice,
            scenario.ProposedDiscountRatio,
            scenario.ValidityWindowStartUtc,
            scenario.ValidityWindowEndUtc,
            scenario.LinkedAiTaskId,
            scenario.EvaluationCount,
            scenario.CreatedAtUtc,
            scenario.UpdatedAtUtc,
            scenario.Version);

    private static CommercialPricingScenarioDetailReadModel MapDetail(CommercialPricingScenario scenario)
    {
        var snapshots = scenario.EvaluationSnapshots
            .OrderByDescending(snapshot => snapshot.EvaluationSequence)
            .Select(MapSnapshot)
            .ToList();

        return new CommercialPricingScenarioDetailReadModel(
            scenario.Id,
            scenario.ScenarioReference,
            scenario.LifecycleStatus,
            scenario.GuardrailResult,
            scenario.ProfileId,
            scenario.ProfileVersion,
            scenario.MaterialId,
            scenario.CustomerReference,
            scenario.SalesEvidenceObservationId,
            scenario.Quantity,
            scenario.BaselineUnitPrice,
            scenario.ProposedUnitPrice,
            scenario.ProposedDiscountRatio,
            scenario.ValidityWindowStartUtc,
            scenario.ValidityWindowEndUtc,
            scenario.CommercialRationale,
            scenario.LatestEvaluationSnapshotId,
            scenario.LinkedAiTaskId,
            scenario.RuleSetVersion,
            scenario.SourceProvenance,
            scenario.EvaluationCount,
            scenario.CreatedAtUtc,
            scenario.UpdatedAtUtc,
            scenario.Version,
            snapshots.FirstOrDefault(),
            snapshots,
            scenario.AuditEvents
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .Select(entry => new CommercialPricingScenarioAuditEventReadModel(
                    entry.Id,
                    entry.EventType,
                    entry.PayloadJson,
                    entry.ActorSubjectId,
                    entry.OccurredAtUtc))
                .ToList());
    }

    private static CommercialPricingScenarioEvaluationSnapshotReadModel MapSnapshot(
        CommercialPricingScenarioEvaluationSnapshot snapshot)
    {
        IReadOnlyList<CommercialPricingScenarioDriverReadModel> drivers = [];
        try
        {
            using var document = JsonDocument.Parse(snapshot.OutputSnapshotJson);
            if (document.RootElement.TryGetProperty("drivers", out var driversElement)
                && driversElement.ValueKind == JsonValueKind.Array)
            {
                drivers = driversElement.EnumerateArray()
                    .Select(element => new CommercialPricingScenarioDriverReadModel(
                        element.GetProperty("reasonCode").GetString() ?? string.Empty,
                        element.GetProperty("severity").GetString() ?? string.Empty,
                        element.TryGetProperty("actualValue", out var actual) && actual.ValueKind != JsonValueKind.Null
                            ? actual.GetDecimal()
                            : null,
                        element.TryGetProperty("thresholdValue", out var threshold) && threshold.ValueKind != JsonValueKind.Null
                            ? threshold.GetDecimal()
                            : null,
                        element.TryGetProperty("unit", out var unit) && unit.ValueKind != JsonValueKind.Null
                            ? unit.GetString()
                            : null,
                        element.TryGetProperty("normalizedVariance", out var variance) && variance.ValueKind != JsonValueKind.Null
                            ? variance.GetDecimal()
                            : null,
                        element.TryGetProperty("affectedComponentCount", out var count) ? count.GetInt32() : 0,
                        element.GetProperty("supportingText").GetString() ?? string.Empty))
                    .ToList();
            }
        }
        catch (JsonException)
        {
            drivers = [];
        }

        return new CommercialPricingScenarioEvaluationSnapshotReadModel(
            snapshot.Id,
            snapshot.ScenarioVersionAtEvaluation,
            snapshot.EvaluationSequence,
            snapshot.GuardrailResult,
            snapshot.AssessmentOutcome,
            snapshot.EvidenceStatus,
            snapshot.CostCoverageStatus,
            snapshot.LeadingDriverCode,
            snapshot.InsufficientDataReason,
            snapshot.BaselineUnitPrice,
            snapshot.ProposedUnitPrice,
            snapshot.TotalIncludedUnitCost,
            snapshot.ExpectedUnitContribution,
            snapshot.ExpectedContributionMarginRatio,
            snapshot.BaselineContributionMarginRatio,
            snapshot.ContributionMarginDelta,
            snapshot.ProfileMinimumContributionMarginRatio,
            snapshot.ProfileId,
            snapshot.ProfileVersion,
            snapshot.InputSnapshotJson,
            snapshot.OutputSnapshotJson,
            snapshot.RuleSetVersion,
            snapshot.EvaluatedAtUtc,
            snapshot.EvaluatedBy,
            drivers);
    }
}

public sealed class UnavailableCommercialPricingScenarioRepository : ICommercialPricingScenarioRepository
{
    private static CommercialPricingScenarioWriteResult Unavailable() =>
        CommercialPricingScenarioWriteResult.Fail(
            CommercialIntelligenceFailureCodes.Unavailable,
            "Commercial pricing scenario persistence is unavailable.");

    public Task<CommercialPricingScenarioWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canReview,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial pricing scenario persistence is unavailable.");

    public Task<IReadOnlyList<CommercialPricingScenarioSummaryReadModel>> ListScenariosAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPricingScenarioListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial pricing scenario persistence is unavailable.");

    public Task<CommercialPricingScenarioDetailReadModel?> GetScenarioByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid scenarioId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialPricingScenarioDetailReadModel?>(null);

    public Task<CommercialPricingScenarioWriteResult> CreateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialPricingScenarioWriteResult> UpdateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialPricingScenarioWriteResult> EvaluateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialPricingScenarioWriteResult> TransitionScenarioAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialPricingScenarioWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialPricingScenarioTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
