using System.Text.Json;
using NevaxisZentra.Persistence.AiTasks;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotAiTaskPresentationSeed
{
    internal static readonly Guid[] GoldenDemoLinkedPresentationTaskIds =
    [
        DevelopmentPilotIds.PresentationDemoAiTaskCycleDeviationId,
        DevelopmentPilotIds.PresentationDemoAiTaskMicroStopId,
        DevelopmentPilotIds.PresentationDemoAiTaskCoolingApprovalId,
        DevelopmentPilotIds.PresentationDemoAiTaskGainVerificationId,
    ];

    private const string MoldReference = "MOLD-PP-240";
    private const string MachineReference = "INJ-001";
    private const string GoldenDemoSourceModule = "golden-demo-p1";
    private const string DigitalWorkerModelVersion = "factos-digital-worker-presentation-1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static async Task EnsurePresentationDemoTasksAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        var goldenDemoEventId = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId.ToString();
        var dueSoon = seedTime.AddHours(6);
        var overdueDue = seedTime.AddHours(-4);
        var inserted = false;

        foreach (var task in BuildPresentationTasks(
                     tenantId,
                     facilityId,
                     actorSubjectId,
                     seedTime,
                     goldenDemoEventId,
                     dueSoon,
                     overdueDue))
        {
            if (await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(t => t.Id == task.Id, cancellationToken))
            {
                continue;
            }

            dbContext.AiTasks.Add(task);
            inserted = true;
        }

        if (inserted)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    internal static async Task ResetGoldenDemoLinkedPresentationTasksAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        var goldenDemoEventId = DevelopmentPilotGoldenDemoP1Ids.ProductionEventId.ToString();
        var dueSoon = seedTime.AddHours(6);
        var overdueDue = seedTime.AddHours(-4);
        var templates = BuildPresentationTasks(
                tenantId,
                facilityId,
                actorSubjectId,
                seedTime,
                goldenDemoEventId,
                dueSoon,
                overdueDue)
            .Where(task => GoldenDemoLinkedPresentationTaskIds.Contains(task.Id))
            .ToDictionary(task => task.Id);

        var linkedTaskIds = GoldenDemoLinkedPresentationTaskIds.ToList();
        var evidence = await dbContext.AiTaskEvidence
            .Where(item => linkedTaskIds.Contains(item.TaskId))
            .ToListAsync(cancellationToken);
        if (evidence.Count > 0)
        {
            dbContext.AiTaskEvidence.RemoveRange(evidence);
        }

        var tasks = await dbContext.AiTasks
            .Where(task => linkedTaskIds.Contains(task.Id))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            if (!templates.TryGetValue(task.Id, out var template))
            {
                continue;
            }

            ApplyInitialPresentationState(task, template, actorSubjectId, seedTime);
        }

        if (tasks.Count > 0 || evidence.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ApplyInitialPresentationState(
        AiTask task,
        AiTask template,
        string actorSubjectId,
        DateTimeOffset seedTime)
    {
        task.Status = template.Status;
        task.ApprovalState = template.ApprovalState;
        task.ClosureState = template.ClosureState;
        task.ResolutionSummary = template.ResolutionSummary;
        task.ResolutionCode = null;
        task.ClosureReason = null;
        task.ClosureApprovalRequired = false;
        task.ClosureRequestedAtUtc = null;
        task.ClosureRequestedByMembershipId = null;
        task.ClosureApprovedAtUtc = null;
        task.ClosureApprovedByMembershipId = null;
        task.ClosureRejectedAtUtc = null;
        task.ClosureRejectedByMembershipId = null;
        task.ClosureRejectionReason = null;
        task.ReopenedAtUtc = null;
        task.ReopenedByMembershipId = null;
        task.ReopenReason = null;
        task.FinalClosedAtUtc = null;
        task.CompletedAtUtc = null;
        task.ResolvedAtUtc = null;
        task.StartedAtUtc = template.StartedAtUtc;
        task.AcknowledgedAtUtc = template.AcknowledgedAtUtc;
        task.ApprovalRequestedAtUtc = template.ApprovalRequestedAtUtc;
        task.ApprovalRequestedBy = template.ApprovalRequestedBy;
        task.ApprovalDecidedAtUtc = null;
        task.ApprovalDecidedBy = null;
        task.ApprovalDecisionNote = null;
        task.UpdatedAtUtc = seedTime;
        task.UpdatedBy = actorSubjectId;
        task.Version += 1;
    }

    private static IEnumerable<AiTask> BuildPresentationTasks(
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        string goldenDemoEventId,
        DateTimeOffset dueSoon,
        DateTimeOffset overdueDue)
    {
        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskCycleDeviationId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-001",
            title: "INJ-001 çevrim süresi sapmasını gider",
            summary: "Cycle time 31,2 sn / referans 28,0 sn. Vardiya üretim kaybını geri kazan.",
            category: AiTaskCategories.LiveProduction,
            priority: AiTaskPriorities.Critical,
            riskLevel: AiTaskRiskLevels.Critical,
            status: AiTaskStatuses.AwaitingReview,
            detectedIssue: "INJ-001 çevrim süresi 31,2 sn; referans 28,0 sn (+3,2 sn sapma).",
            reasoning: "Üretim Kurtarma Dijital Çalışanı sapmayı tespit etti, aksiyonları oluşturdu ve üretim ile bakım ekibine aktardı.",
            digitalWorkerLabel: "Üretim Kurtarma Dijital Çalışanı",
            digitalWorkerId: "production-recovery",
            valueSummary: "Cycle time 31,2 sn / referans 28,0 sn",
            recommendationTitle: "Çevrim süresi iyileştirmesini uygula",
            recommendationDescription: "Soğutma, vida geri dönüşü ve kalıp hareketi aksiyonlarını koordineli uygulayarak vardiya üretim kaybını geri kazan.",
            actionType: AiTaskActionTypes.UpdatePlan,
            requiresApproval: false,
            impactSummary: "Vardiya üretim kaybını geri kazan",
            confidenceScore: 0.91m,
            confidenceExplanation: "INJ-001 gerçek zamanlı çevrim ve vardiya projeksiyonu ile doğrulandı.",
            assigneeDisplayName: "Üretim + Bakım",
            assigneeRole: "operations",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelUretimId,
            sourceModule: GoldenDemoSourceModule,
            sourceEntityId: goldenDemoEventId,
            machineId: MachineReference,
            moldId: MoldReference,
            dueAtUtc: dueSoon,
            approvalState: AiTaskApprovalStates.NotRequired);

        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskMicroStopId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-002",
            title: "INJ-001 mikro duruş kök nedenini doğrula",
            summary: "8 mikro duruş / 120 sn. Bakım ekibi kök neden analizini tamamlıyor.",
            category: AiTaskCategories.LiveProduction,
            priority: AiTaskPriorities.High,
            riskLevel: AiTaskRiskLevels.High,
            status: AiTaskStatuses.AwaitingReview,
            detectedIssue: "8 mikro duruş / 120 sn toplam kayıp.",
            reasoning: "Makine Performansı Dijital Çalışanı mikro duruş kümesini tespit etti ve bakım ekibine inceleme görevi oluşturdu.",
            digitalWorkerLabel: "Makine Performansı Dijital Çalışanı",
            digitalWorkerId: "machine-performance",
            valueSummary: "8 mikro duruş / 120 sn",
            recommendationTitle: "Kök neden analizini tamamla",
            recommendationDescription: "Mikro duruş desenini PLC olay kayıtları ve makine sağlık sinyalleri ile doğrula.",
            actionType: AiTaskActionTypes.Review,
            requiresApproval: false,
            impactSummary: "Mikro duruş kaynaklı çevrim kaybını azalt",
            confidenceScore: 0.84m,
            confidenceExplanation: "Makine duruş telemetrisi ve olay penceresi ile desteklendi.",
            assigneeDisplayName: "Bakım Ekibi",
            assigneeRole: "maintenance",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelBakimId,
            sourceModule: GoldenDemoSourceModule,
            sourceEntityId: goldenDemoEventId,
            machineId: MachineReference,
            moldId: MoldReference,
            dueAtUtc: dueSoon,
            approvalState: AiTaskApprovalStates.NotRequired);

        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskCoolingApprovalId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-003",
            title: "Soğutma çevrim kaybını kontrol et",
            summary: "Soğutma kaynaklı +1,5 sn kayıp. Üretim sorumlusu onayı gerekiyor.",
            category: AiTaskCategories.LiveProduction,
            priority: AiTaskPriorities.High,
            riskLevel: AiTaskRiskLevels.Medium,
            status: AiTaskStatuses.AwaitingApproval,
            detectedIssue: "Soğutma fazında +1,5 sn çevrim kaybı.",
            reasoning: "Proses Optimizasyonu Dijital Çalışanı soğutma parametresi ayarını önerdi; uygulama için üretim sorumlusu onayı bekleniyor.",
            digitalWorkerLabel: "Proses Optimizasyonu Dijital Çalışanı",
            digitalWorkerId: "process-optimization",
            valueSummary: "Soğutma kaynaklı +1,5 sn kayıp",
            recommendationTitle: "Soğutma süresi ayarını onayla",
            recommendationDescription: "Önerilen proses ayarı uygulanmadan önce üretim sorumlusu human-in-the-loop onayı vermelidir.",
            actionType: AiTaskActionTypes.UpdatePlan,
            requiresApproval: true,
            impactSummary: "Soğutma kaynaklı çevrim kaybını geri kazan",
            confidenceScore: 0.88m,
            confidenceExplanation: "Referans ve güncel proses parametreleri karşılaştırması ile doğrulandı.",
            assigneeDisplayName: "Üretim sorumlusu",
            assigneeRole: "production-supervisor",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelUretimId,
            sourceModule: GoldenDemoSourceModule,
            sourceEntityId: goldenDemoEventId,
            machineId: MachineReference,
            moldId: MoldReference,
            dueAtUtc: dueSoon,
            approvalState: AiTaskApprovalStates.Pending,
            approvalRequestedAtUtc: seedTime.AddMinutes(-12),
            approvalRequestedBy: actorSubjectId);

        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskGainVerificationId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-004",
            title: "+37 iyi parça/vardiya kazanımını doğrula",
            summary: "Uygulanan iyileştirmeler sonrası +37 iyi parça/vardiya kazanım projeksiyonu. Aksiyon uygulanana kadar bekliyor.",
            category: AiTaskCategories.LiveProduction,
            priority: AiTaskPriorities.Medium,
            riskLevel: AiTaskRiskLevels.Low,
            status: AiTaskStatuses.Approved,
            detectedIssue: "Uygulanan iyileştirmeler sonrası +37 iyi parça/vardiya kazanım projeksiyonu.",
            reasoning: "Üretim Kurtarma Dijital Çalışanı kazanımı hesapladı; aksiyon uygulandıktan sonra üretim verisi ile doğrulanacak.",
            digitalWorkerLabel: "Üretim Kurtarma Dijital Çalışanı",
            digitalWorkerId: "production-recovery",
            valueSummary: "+37 iyi parça/vardiya · production data / cycle-time trend",
            recommendationTitle: "Kazanımı doğrula ve kapat",
            recommendationDescription: "Vardiya projeksiyonu hedefin üzerinde stabil kaldığında görev sonucu doğrulanarak kapatılabilir.",
            actionType: AiTaskActionTypes.Review,
            requiresApproval: false,
            impactSummary: "Vardiya projeksiyonu hedefin üzerinde stabil",
            confidenceScore: 0.86m,
            confidenceExplanation: "Üretim sayımı ve çevrim trendi kanıtları toplandı.",
            assigneeDisplayName: "Üretim",
            assigneeRole: "operations",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelUretimId,
            sourceModule: GoldenDemoSourceModule,
            sourceEntityId: goldenDemoEventId,
            machineId: MachineReference,
            moldId: MoldReference,
            dueAtUtc: dueSoon,
            approvalState: AiTaskApprovalStates.NotRequired);

        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskMoldRiskId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-005",
            title: "Kalıp bakım riskini kontrol et",
            summary: "Kalıp Sağlığı Dijital Çalışanı artan aşınma sinyali tespit etti; bakım ekibine atandı.",
            category: AiTaskCategories.Maintenance,
            priority: AiTaskPriorities.High,
            riskLevel: AiTaskRiskLevels.High,
            status: AiTaskStatuses.Approved,
            detectedIssue: "Kalıp bakım riski artışı — planlı bakım penceresi değerlendirilmeli.",
            reasoning: "Kalıp Sağlığı Dijital Çalışanı risk sinyalini doğruladı ve bakım ekibine kontrol görevi atadı.",
            digitalWorkerLabel: "Kalıp Sağlığı Dijital Çalışanı",
            digitalWorkerId: "mold-health",
            valueSummary: "Kalıp bakım riski yükseldi",
            recommendationTitle: "Kalıp bakım riskini kontrol et",
            recommendationDescription: "Bakım ekibi kalıp sağlık sayaçlarını ve son üretim kalitesini incelemelidir.",
            actionType: AiTaskActionTypes.CreateMaintenanceRequest,
            requiresApproval: false,
            impactSummary: "Plansız duruş riskini azalt",
            confidenceScore: 0.82m,
            confidenceExplanation: "Kalıp sayaçları ve üretim kalitesi sinyalleri ile desteklendi.",
            assigneeDisplayName: "Bakım",
            assigneeRole: "maintenance",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelBakimId,
            assignedMembershipId: DevelopmentPilotIds.OperatorTenantMembershipId,
            sourceModule: "mold",
            sourceEntityId: MoldReference,
            machineId: MachineReference,
            moldId: MoldReference,
            dueAtUtc: overdueDue,
            acknowledgedAtUtc: seedTime.AddMinutes(-45),
            approvalState: AiTaskApprovalStates.NotRequired);

        yield return BuildTask(
            DevelopmentPilotIds.PresentationDemoAiTaskMaterialLevelId,
            tenantId,
            facilityId,
            actorSubjectId,
            seedTime,
            taskReference: "AIT-PRES-006",
            title: "Kritik malzeme seviyesini kontrol et",
            summary: "PP granül stok seviyesi kritik eşiğe yaklaştı; planlama ve satın alma izliyor.",
            category: AiTaskCategories.MaterialRisk,
            priority: AiTaskPriorities.Medium,
            riskLevel: AiTaskRiskLevels.Medium,
            status: AiTaskStatuses.InProgress,
            detectedIssue: "PP granül stok seviyesi kritik eşiğe yaklaşıyor.",
            reasoning: "Malzeme Açığı Dijital Çalışanı tüketim hızına göre stok riskini tespit etti ve planlama/satın alma ekibine izleme görevi oluşturdu.",
            digitalWorkerLabel: "Malzeme Açığı Dijital Çalışanı",
            digitalWorkerId: "material-shortage",
            valueSummary: "Kritik malzeme seviyesi izleniyor",
            recommendationTitle: "Stok seviyesini izle",
            recommendationDescription: "Planlama ve satın alma tedarik durumunu takip ederek üretim kesintisi riskini yönetmelidir.",
            actionType: AiTaskActionTypes.CreatePurchaseRequest,
            requiresApproval: true,
            impactSummary: "Malzeme kesintisi riskini önle",
            confidenceScore: 0.79m,
            confidenceExplanation: "Stok, tüketim ve açık satın alma verileri ile desteklendi.",
            assigneeDisplayName: "Planlama / Satın Alma",
            assigneeRole: "planning",
            assignedTeamChannelId: DevelopmentPilotIds.TeamsPilotChannelSatinAlmaId,
            sourceModule: "material",
            sourceEntityId: DevelopmentPilotIds.MaterialPilotPpId.ToString(),
            materialId: "PP-GRANUL",
            dueAtUtc: dueSoon,
            startedAtUtc: seedTime.AddMinutes(-20),
            approvalState: AiTaskApprovalStates.NotRequired);
    }

    private static AiTask BuildTask(
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        string taskReference,
        string title,
        string summary,
        string category,
        string priority,
        string riskLevel,
        string status,
        string detectedIssue,
        string reasoning,
        string digitalWorkerLabel,
        string digitalWorkerId,
        string valueSummary,
        string recommendationTitle,
        string recommendationDescription,
        string actionType,
        bool requiresApproval,
        string impactSummary,
        decimal confidenceScore,
        string confidenceExplanation,
        string assigneeDisplayName,
        string assigneeRole,
        Guid? assignedTeamChannelId = null,
        Guid? assignedMembershipId = null,
        string? sourceModule = null,
        string? sourceEntityId = null,
        string? machineId = null,
        string? moldId = null,
        string? materialId = null,
        DateTimeOffset? dueAtUtc = null,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? acknowledgedAtUtc = null,
        string approvalState = AiTaskApprovalStates.NotRequired,
        DateTimeOffset? approvalRequestedAtUtc = null,
        string? approvalRequestedBy = null,
        string closureState = AiTaskClosureStates.None,
        string? resolutionSummary = null)
    {
        return new AiTask
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            TaskReference = taskReference,
            Title = title,
            Summary = summary,
            Category = category,
            Priority = priority,
            RiskLevel = riskLevel,
            Status = status,
            DetectedIssue = detectedIssue,
            Reasoning = reasoning,
            RecommendationJson = Serialize(new AiTaskRecommendationModel(
                recommendationTitle,
                recommendationDescription,
                actionType,
                requiresApproval,
                true,
                sourceModule)),
            ConfidenceJson = Serialize(new AiTaskConfidenceModel(
                confidenceScore,
                confidenceExplanation,
                DigitalWorkerModelVersion)),
            ImpactJson = Serialize(new AiTaskImpactModel(
                null, null, 37m, "adet", null, null, null, impactSummary)),
            SourcesJson = Serialize(new[]
            {
                new AiTaskSourceReferenceModel(
                    "digital-worker",
                    digitalWorkerId,
                    digitalWorkerLabel,
                    seedTime.AddMinutes(-5),
                    valueSummary),
            }),
            AssigneeJson = Serialize(new AiTaskAssigneeModel(
                assignedMembershipId?.ToString() ?? assigneeRole,
                assigneeDisplayName,
                assigneeRole)),
            AssignedMembershipId = assignedMembershipId,
            AssignedTeamChannelId = assignedTeamChannelId,
            AcknowledgedAtUtc = acknowledgedAtUtc,
            StartedAtUtc = startedAtUtc,
            ApprovalState = approvalState,
            ApprovalRequestedAtUtc = approvalRequestedAtUtc,
            ApprovalRequestedBy = approvalRequestedBy,
            ClosureState = closureState,
            ResolutionSummary = resolutionSummary,
            SiteId = facilityId.ToString(),
            MachineId = machineId,
            MoldId = moldId,
            MaterialId = materialId,
            SourceModule = sourceModule,
            SourceEntityId = sourceEntityId,
            DueAtUtc = dueAtUtc,
            RuleSetVersion = AiTaskConstants.RuleSetVersion,
            CreatedAtUtc = seedTime.AddMinutes(-60),
            UpdatedAtUtc = seedTime,
            CreatedBy = actorSubjectId,
            UpdatedBy = actorSubjectId,
        };
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
