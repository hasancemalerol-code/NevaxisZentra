using System.Text.Json;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Development;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryDecisionCenter;

internal static class DevelopmentPilotFactoryDecisionCenterSeed
{
    private static readonly DateTimeOffset DeliveryCommitmentUtc =
        new(2026, 9, 16, 5, 0, 0, TimeSpan.Zero);

    internal static async Task EnsureCriticalOrderRecoveryAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId != DevelopmentPilotIds.TenantId || facilityId != DevelopmentPilotIds.FacilityId)
        {
            return;
        }

        await EnsureAiTasksAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);

        if (await dbContext.DigitalWorkCases.AsNoTracking()
                .AnyAsync(c => c.Id == DevelopmentPilotFactoryDecisionCenterIds.RootCaseId, cancellationToken))
        {
            return;
        }

        var metadata = BuildRootMetadata();
        var recoveryPlan = BuildRecoveryPlanJson();
        var now = seedTimestampUtc;

        var rootCase = new DigitalWorkCase
        {
            Id = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            TenantId = tenantId,
            FacilityId = facilityId,
            Title = "Kritik müşteri siparişi teslimat riski",
            Description = $"{FactoryDecisionCenterConstants.CustomerName} için {FactoryDecisionCenterConstants.OrderReference} siparişi teslimat taahhüdü risk altında.",
            Source = FactoryDecisionCenterConstants.SourceModule,
            SourceClassification = DigitalWorkforceSourceClassifications.Demo,
            Severity = "critical",
            Status = DigitalWorkCaseStatuses.AwaitingApproval,
            CurrentStage = DigitalWorkCaseStages.Approve,
            PrimaryWorkerId = DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
            AutonomyModeAtStart = DigitalWorkforceAutonomyModes.Full,
            RequiresApproval = true,
            IsHumanControlled = false,
            LinkedAiTaskId = DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId,
            StartedAtUtc = now.AddHours(-2),
            TargetAtUtc = DeliveryCommitmentUtc,
            BusinessImpact = $"{FactoryDecisionCenterConstants.InitialGapQuantity} iyi parça açığı · teslimat riski",
            VerificationState = DigitalWorkVerificationResults.Pending,
            ClosureContractKey = DigitalWorkforceClosureContracts.Coordinator,
            MetadataJson = metadata,
            OrchestrationRootCaseId = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            CorrelationKey = FactoryDecisionCenterConstants.CorrelationKey,
            PrimaryDomain = FactoryOrchestrationDomains.Planning,
            ParticipatingDomainsJson = JsonSerializer.Serialize(new[]
            {
                FactoryOrchestrationDomains.Production,
                FactoryOrchestrationDomains.Maintenance,
                FactoryOrchestrationDomains.Material,
                FactoryOrchestrationDomains.Planning,
            }),
            CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman,
            BlockerSinceUtc = now.AddMinutes(-45),
            RecoveryPlanJson = recoveryPlan,
            ImpactGraphJson = "{}",
            DependencyGraphJson = "{}",
            CreatedAtUtc = now.AddHours(-2),
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };

        var productionChild = BuildChildCase(
            DevelopmentPilotFactoryDecisionCenterIds.ProductionChildCaseId,
            tenantId,
            facilityId,
            DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery,
            "INJ-001 üretim performansı kurtarma",
            "Çevrim süresi bozulması ve mikro duruşlar üretim projeksiyonunu düşürüyor.",
            DigitalWorkforceClosureContracts.ProductionRecovery,
            now);

        var maintenanceChild = BuildChildCase(
            DevelopmentPilotFactoryDecisionCenterIds.MaintenanceChildCaseId,
            tenantId,
            facilityId,
            DevelopmentPilotDigitalWorkforceIds.WorkerMaintenanceResolution,
            "INJ-001 güvenilirlik kontrolü",
            "Mikro duruşlar ve kalıp bakım riski makine güvenilirliğini etkiliyor.",
            DigitalWorkforceClosureContracts.MaintenanceResolution,
            now);

        var materialChild = BuildChildCase(
            DevelopmentPilotFactoryDecisionCenterIds.MaterialChildCaseId,
            tenantId,
            facilityId,
            DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage,
            "PP granül malzeme rezervasyonu",
            "Malzeme rezervasyon çakışması üretim sürekliliğini riske atıyor.",
            DigitalWorkforceClosureContracts.MaterialShortage,
            now);

        var approval = new DigitalWorkApproval
        {
            Id = DevelopmentPilotFactoryDecisionCenterIds.RootApprovalId,
            TenantId = tenantId,
            CaseId = rootCase.Id,
            WorkerId = DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator,
            Action = "Alternatif kapasite kullan ve üretim sırasını yeniden önceliklendir",
            BusinessReason = "Kurtarma planı üretim yöneticisi onayı gerektiriyor",
            Impact = $"{FactoryDecisionCenterConstants.OrderReference} teslimatını korur ancak diğer siparişleri ~90 dk kaydırır.",
            RiskLevel = 3,
            Status = DigitalWorkApprovalStatuses.Pending,
            CreatedAtUtc = now.AddMinutes(-30),
            ConcurrencyToken = 1,
        };

        var correlation = new FactoryEventCorrelation
        {
            Id = DevelopmentPilotFactoryDecisionCenterIds.EventCorrelationId,
            TenantId = tenantId,
            FacilityId = facilityId,
            CorrelationKey = FactoryDecisionCenterConstants.CorrelationKey,
            RootCaseId = rootCase.Id,
            SourceType = "order",
            SourceId = FactoryDecisionCenterConstants.OrderReference,
            EventCategory = "critical-order-delay",
            FirstSeenAtUtc = now.AddHours(-2),
            LastSeenAtUtc = now,
        };

        var dependencies = new List<FactoryCaseDependency>
        {
            BuildDependency(tenantId, facilityId, rootCase.Id, "coordinator", "production", productionChild.Id, DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery),
            BuildDependency(tenantId, facilityId, rootCase.Id, "production", "maintenance", maintenanceChild.Id, DevelopmentPilotDigitalWorkforceIds.WorkerMaintenanceResolution),
            BuildDependency(tenantId, facilityId, rootCase.Id, "maintenance", "material", materialChild.Id, DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage),
        };

        var participants = new[]
        {
            BuildParticipant(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator, DigitalWorkforceParticipantRoles.Coordinator, now),
            BuildParticipant(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery, DigitalWorkforceParticipantRoles.Supporting, now),
            BuildParticipant(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerMaintenanceResolution, DigitalWorkforceParticipantRoles.Supporting, now),
            BuildParticipant(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage, DigitalWorkforceParticipantRoles.Supporting, now),
        };

        var activities = new[]
        {
            BuildActivity(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator, "detect", "Kritik sipariş teslimat riski tespit edildi", now.AddHours(-2)),
            BuildActivity(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator, "analyze", "INJ-001 çevrim süresi ve malzeme çakışması analiz edildi", now.AddHours(-1)),
            BuildActivity(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator, "plan", "Çoklu dijital çalışan kurtarma planı oluşturuldu", now.AddMinutes(-45)),
        };

        dbContext.DigitalWorkCases.AddRange(rootCase, productionChild, maintenanceChild, materialChild);
        dbContext.DigitalWorkApprovals.Add(approval);
        dbContext.FactoryEventCorrelations.Add(correlation);
        dbContext.FactoryCaseDependencies.AddRange(dependencies);
        dbContext.DigitalWorkCaseParticipants.AddRange(participants);
        dbContext.DigitalWorkActivities.AddRange(activities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static async Task ResetCriticalOrderRecoveryAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId != DevelopmentPilotIds.TenantId || facilityId != DevelopmentPilotIds.FacilityId)
        {
            return;
        }

        var caseIds = new[]
        {
            DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            DevelopmentPilotFactoryDecisionCenterIds.ProductionChildCaseId,
            DevelopmentPilotFactoryDecisionCenterIds.MaintenanceChildCaseId,
            DevelopmentPilotFactoryDecisionCenterIds.MaterialChildCaseId,
        };

        var verifications = await dbContext.DigitalWorkVerifications
            .Where(v => caseIds.Contains(v.CaseId))
            .ToListAsync(cancellationToken);
        if (verifications.Count > 0)
        {
            dbContext.DigitalWorkVerifications.RemoveRange(verifications);
        }

        var activities = await dbContext.DigitalWorkActivities
            .Where(a => caseIds.Contains(a.CaseId))
            .ToListAsync(cancellationToken);
        if (activities.Count > 0)
        {
            dbContext.DigitalWorkActivities.RemoveRange(activities);
        }

        var approvals = await dbContext.DigitalWorkApprovals
            .Where(a => a.CaseId == DevelopmentPilotFactoryDecisionCenterIds.RootCaseId)
            .ToListAsync(cancellationToken);
        foreach (var approval in approvals)
        {
            approval.Status = DigitalWorkApprovalStatuses.Pending;
            approval.Decision = null;
            approval.DecidedAtUtc = null;
            approval.DecidedBySubjectId = null;
            approval.ConcurrencyToken = 1;
        }

        var rootCase = await dbContext.DigitalWorkCases
            .FirstOrDefaultAsync(c => c.Id == DevelopmentPilotFactoryDecisionCenterIds.RootCaseId, cancellationToken);
        if (rootCase is null)
        {
            await EnsureCriticalOrderRecoveryAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
            return;
        }

        var now = seedTimestampUtc;
        rootCase.Status = DigitalWorkCaseStatuses.AwaitingApproval;
        rootCase.CurrentStage = DigitalWorkCaseStages.Approve;
        rootCase.RequiresApproval = true;
        rootCase.VerificationState = DigitalWorkVerificationResults.Pending;
        rootCase.VerifiedAtUtc = null;
        rootCase.ClosedAtUtc = null;
        rootCase.Outcome = string.Empty;
        rootCase.CurrentBlockerCode = FactoryCaseBlockerCodes.WaitingHuman;
        rootCase.MetadataJson = BuildRootMetadata();
        rootCase.RecoveryPlanJson = BuildRecoveryPlanJson();
        rootCase.UpdatedAtUtc = now;
        rootCase.ConcurrencyToken++;

        foreach (var childId in caseIds.Skip(1))
        {
            var child = await dbContext.DigitalWorkCases.FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);
            if (child is null)
            {
                continue;
            }

            child.Status = DigitalWorkCaseStatuses.Planning;
            child.CurrentStage = DigitalWorkCaseStages.Plan;
            child.VerificationState = DigitalWorkVerificationResults.Pending;
            child.ClosedAtUtc = null;
            child.VerifiedAtUtc = null;
            child.UpdatedAtUtc = now;
            child.ConcurrencyToken++;
        }

        dbContext.DigitalWorkActivities.AddRange(
            BuildActivity(tenantId, rootCase.Id, DevelopmentPilotDigitalWorkforceIds.WorkerFactoryCoordinator, "reset", "Kritik sipariş kurtarma senaryosu başlangıç durumuna döndürüldü", now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureAiTasksAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureAiTasksAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        foreach (var task in BuildAiTasks(tenantId, facilityId, actorSubjectId, seedTime))
        {
            if (await dbContext.AiTasks.AsNoTracking().AnyAsync(t => t.Id == task.Id, cancellationToken))
            {
                continue;
            }

            dbContext.AiTasks.Add(task);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<AiTask> BuildAiTasks(
        Guid tenantId, Guid facilityId, string actorSubjectId, DateTimeOffset seedTime)
    {
        var sourceEntityId = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId.ToString();
        var dueAt = DeliveryCommitmentUtc;

        yield return BuildAiTask(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId,
            tenantId, facilityId, actorSubjectId, seedTime,
            "AIT-FDC-001",
            "Kritik sipariş ORD-2026-0915-047 kurtarma planını onayla",
            "Alternatif kapasite ve üretim sırası yeniden önceliklendirme onayı bekleniyor.",
            AiTaskCategories.DeliveryRisk,
            AiTaskStatuses.AwaitingApproval,
            AiTaskApprovalStates.Pending,
            sourceEntityId,
            dueAt);

        yield return BuildAiTask(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskProductionRecoveryId,
            tenantId, facilityId, actorSubjectId, seedTime,
            "AIT-FDC-002",
            "INJ-001 üretim performansını geri kazan",
            "Çevrim süresi 31,2 sn / referans 28,0 sn. Kurtarma planı onayı sonrası yürütülecek.",
            AiTaskCategories.LiveProduction,
            AiTaskStatuses.AwaitingReview,
            AiTaskApprovalStates.NotRequired,
            sourceEntityId,
            dueAt);

        yield return BuildAiTask(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskMaterialReservationId,
            tenantId, facilityId, actorSubjectId, seedTime,
            "AIT-FDC-003",
            "ORD-2026-0915-047 malzeme rezervasyonunu güvenceye al",
            "PP granül rezervasyon çakışması çözülmeli.",
            AiTaskCategories.MaterialRisk,
            AiTaskStatuses.AwaitingReview,
            AiTaskApprovalStates.NotRequired,
            sourceEntityId,
            dueAt);

        yield return BuildAiTask(
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskDeliveryProjectionId,
            tenantId, facilityId, actorSubjectId, seedTime,
            "AIT-FDC-004",
            "Teslimat projeksiyonunu yeniden doğrula",
            "Kurtarma aksiyonları tamamlandıktan sonra sipariş projeksiyonu doğrulanacak.",
            AiTaskCategories.DeliveryRisk,
            AiTaskStatuses.AwaitingReview,
            AiTaskApprovalStates.NotRequired,
            sourceEntityId,
            dueAt);
    }

    private static AiTask BuildAiTask(
        Guid id, Guid tenantId, Guid facilityId, string actorSubjectId, DateTimeOffset seedTime,
        string taskReference, string title, string summary, string category, string status,
        string approvalState, string sourceEntityId, DateTimeOffset dueAtUtc) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            TaskReference = taskReference,
            Title = title,
            Summary = summary,
            Category = category,
            Priority = AiTaskPriorities.Critical,
            RiskLevel = AiTaskRiskLevels.High,
            Status = status,
            ApprovalState = approvalState,
            ClosureState = AiTaskClosureStates.None,
            EscalationState = AiTaskEscalationStates.None,
            DetectedIssue = summary,
            Reasoning = "Fabrika Karar Merkezi kritik sipariş kurtarma vakasından oluşturuldu.",
            RecommendationJson = JsonSerializer.Serialize(new AiTaskRecommendationModel(
                title,
                summary,
                AiTaskActionTypes.UpdatePlan,
                approvalState == AiTaskApprovalStates.Pending,
                true,
                FactoryDecisionCenterConstants.SourceModule)),
            ConfidenceJson = JsonSerializer.Serialize(new AiTaskConfidenceModel(
                0.87m,
                "Kritik sipariş teslimat riski ve üretim projeksiyonu ile doğrulandı.",
                "factos-factory-decision-center-1")),
            ImpactJson = JsonSerializer.Serialize(new AiTaskImpactModel(
                null, null, FactoryDecisionCenterConstants.InitialGapQuantity, "adet", null, null, null,
                "Teslimat taahhüdü risk altında")),
            SourcesJson = JsonSerializer.Serialize(new[]
            {
                new AiTaskSourceReferenceModel(
                    "factory-decision-case",
                    sourceEntityId,
                    "Fabrika Operasyon Koordinatörü",
                    seedTime.AddHours(-2),
                    FactoryDecisionCenterConstants.OrderReference),
            }),
            AssigneeJson = JsonSerializer.Serialize(new AiTaskAssigneeModel(
                DevelopmentPilotIds.TenantMembershipId.ToString(),
                "Üretim Yöneticisi",
                "production-manager")),
            SourceModule = FactoryDecisionCenterConstants.SourceModule,
            SourceEntityId = sourceEntityId,
            ProductionOrderId = FactoryDecisionCenterConstants.OrderReference,
            MachineId = FactoryDecisionCenterConstants.MachineCode,
            MoldId = FactoryDecisionCenterConstants.MoldCode,
            SiteId = facilityId.ToString(),
            DueAtUtc = dueAtUtc,
            RuleSetVersion = AiTaskConstants.RuleSetVersion,
            CreatedAtUtc = seedTime.AddHours(-2),
            UpdatedAtUtc = seedTime,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
            Version = 1,
        };

    private static DigitalWorkCase BuildChildCase(
        Guid id, Guid tenantId, Guid facilityId, Guid workerId, string title, string description,
        string closureContractKey, DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            Title = title,
            Description = description,
            Source = FactoryDecisionCenterConstants.SourceModule,
            SourceClassification = DigitalWorkforceSourceClassifications.Demo,
            Severity = "high",
            Status = DigitalWorkCaseStatuses.Planning,
            CurrentStage = DigitalWorkCaseStages.Plan,
            PrimaryWorkerId = workerId,
            AutonomyModeAtStart = DigitalWorkforceAutonomyModes.Limited,
            ParentCaseId = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            OrchestrationRootCaseId = DevelopmentPilotFactoryDecisionCenterIds.RootCaseId,
            CorrelationKey = FactoryDecisionCenterConstants.CorrelationKey,
            ClosureContractKey = closureContractKey,
            VerificationState = DigitalWorkVerificationResults.Pending,
            CreatedAtUtc = now.AddHours(-1),
            UpdatedAtUtc = now,
            ConcurrencyToken = 1,
        };

    private static FactoryCaseDependency BuildDependency(
        Guid tenantId, Guid facilityId, Guid rootCaseId, string from, string to, Guid linkedCaseId, Guid linkedWorkerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            RootCaseId = rootCaseId,
            FromNodeKey = from,
            ToNodeKey = to,
            FromNodeType = "domain",
            ToNodeType = "domain",
            EdgeType = FactoryDependencyEdgeTypes.DependsOn,
            LinkedCaseId = linkedCaseId.ToString(),
            LinkedWorkerId = linkedWorkerId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

    private static DigitalWorkCaseParticipant BuildParticipant(
        Guid tenantId, Guid caseId, Guid workerId, string role, DateTimeOffset joinedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            WorkerId = workerId,
            Role = role,
            JoinedAtUtc = joinedAt,
        };

    private static DigitalWorkActivity BuildActivity(
        Guid tenantId, Guid caseId, Guid workerId, string actionType, string description, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            WorkerId = workerId,
            ActionType = actionType,
            Description = description,
            Reason = "factory-decision-center",
            Result = "recorded",
            SourceClassification = DigitalWorkforceSourceClassifications.Demo,
            CreatedAtUtc = createdAt,
        };

    private static string BuildRootMetadata() =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [FactoryDecisionCenterMetadataKeys.ScenarioKey] = FactoryDecisionCenterConstants.ScenarioCriticalOrderRecovery,
            [FactoryDecisionCenterMetadataKeys.OrderReference] = FactoryDecisionCenterConstants.OrderReference,
            [FactoryDecisionCenterMetadataKeys.CustomerName] = FactoryDecisionCenterConstants.CustomerName,
            [FactoryDecisionCenterMetadataKeys.RequiredQuantity] = FactoryDecisionCenterConstants.RequiredQuantity,
            [FactoryDecisionCenterMetadataKeys.ProjectedQuantity] = FactoryDecisionCenterConstants.InitialProjectedQuantity,
            [FactoryDecisionCenterMetadataKeys.GapQuantity] = FactoryDecisionCenterConstants.InitialGapQuantity,
            [FactoryDecisionCenterMetadataKeys.DeliveryCommitmentUtc] = DeliveryCommitmentUtc,
            [FactoryDecisionCenterMetadataKeys.MachineCode] = FactoryDecisionCenterConstants.MachineCode,
            [FactoryDecisionCenterMetadataKeys.MoldCode] = FactoryDecisionCenterConstants.MoldCode,
            [FactoryDecisionCenterMetadataKeys.MaterialCode] = FactoryDecisionCenterConstants.MaterialCode,
            [FactoryDecisionCenterMetadataKeys.PrimaryCause] = $"{FactoryDecisionCenterConstants.MachineCode} çevrim süresi bozulması",
            [FactoryDecisionCenterMetadataKeys.ContributingFactors] = new[]
            {
                $"{FactoryDecisionCenterConstants.MicroStopCount} mikro duruş / {FactoryDecisionCenterConstants.MicroStopDurationSec} sn",
                "Malzeme rezervasyon çakışması",
                "Kalıp bakım riski",
            },
            [FactoryDecisionCenterMetadataKeys.Confidence] = 0.87,
            [FactoryDecisionCenterMetadataKeys.RecommendedStrategy] =
                $"{FactoryDecisionCenterConstants.MachineCode} kurtarma + alternatif makine kapasitesi + malzeme rezervasyonu",
            [FactoryDecisionCenterMetadataKeys.AlternativeStrategy] = "Siparişi alternatif tesise kaydır",
            ["allSubcasesResolved"] = false,
            ["childCaseIds"] = new object[]
            {
                DevelopmentPilotFactoryDecisionCenterIds.ProductionChildCaseId.ToString(),
                DevelopmentPilotFactoryDecisionCenterIds.MaintenanceChildCaseId.ToString(),
                DevelopmentPilotFactoryDecisionCenterIds.MaterialChildCaseId.ToString(),
            },
        });

    private static readonly string[] RecoveryVerificationCriteria =
    [
        "Üretim projeksiyonu >= 480",
        "Teslimat taahhüdü korundu",
    ];

    private static string BuildRecoveryPlanJson() =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["objective"] = $"{FactoryDecisionCenterConstants.OrderReference} teslimat taahhüdünü koru",
            ["steps"] = new object[]
            {
                new Dictionary<string, object?> { ["order"] = 1, ["title"] = "INJ-001 çevrim süresini geri kazan", ["workerCode"] = DigitalWorkforceWorkerCodes.ProductionRecovery, ["workerName"] = "Üretim Kurtarma Dijital Çalışanı", ["status"] = "planned", ["linkedCaseId"] = DevelopmentPilotFactoryDecisionCenterIds.ProductionChildCaseId.ToString(), ["actionKey"] = "recover-cycle-time" },
                new Dictionary<string, object?> { ["order"] = 2, ["title"] = "Gerekli malzemeyi rezerve et", ["workerCode"] = DigitalWorkforceWorkerCodes.MaterialShortage, ["workerName"] = "Malzeme Açığı Çözüm Dijital Çalışanı", ["status"] = "planned", ["linkedCaseId"] = DevelopmentPilotFactoryDecisionCenterIds.MaterialChildCaseId.ToString(), ["actionKey"] = "reserve-material" },
                new Dictionary<string, object?> { ["order"] = 3, ["title"] = "Alternatif kapasiteyi hazırla", ["workerCode"] = DigitalWorkforceWorkerCodes.ProductionRecovery, ["workerName"] = "Üretim Kurtarma Dijital Çalışanı", ["status"] = "planned", ["linkedCaseId"] = null, ["actionKey"] = "alternate-capacity" },
                new Dictionary<string, object?> { ["order"] = 4, ["title"] = "Bakım engelini temizle", ["workerCode"] = DigitalWorkforceWorkerCodes.MaintenanceResolution, ["workerName"] = "Bakım Çözüm Dijital Çalışanı", ["status"] = "planned", ["linkedCaseId"] = DevelopmentPilotFactoryDecisionCenterIds.MaintenanceChildCaseId.ToString(), ["actionKey"] = "clear-maintenance" },
                new Dictionary<string, object?> { ["order"] = 5, ["title"] = "Teslimat projeksiyonunu yeniden hesapla", ["workerCode"] = DigitalWorkforceWorkerCodes.FactoryCoordinator, ["workerName"] = "Fabrika Operasyon Koordinatörü", ["status"] = "planned", ["linkedCaseId"] = null, ["actionKey"] = "recalculate-delivery" },
                new Dictionary<string, object?> { ["order"] = 6, ["title"] = "Kurtarılan çıktıyı doğrula", ["workerCode"] = DigitalWorkforceWorkerCodes.FactoryCoordinator, ["workerName"] = "Fabrika Operasyon Koordinatörü", ["status"] = "planned", ["linkedCaseId"] = null, ["actionKey"] = "verify-output" },
            },
            ["verificationCriteria"] = RecoveryVerificationCriteria,
            ["fallback"] = "Alternatif makine veya tesise kaydırma",
        });
}
