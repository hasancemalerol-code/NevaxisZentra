using System.Text.Json;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeRecoveryPlanEngine
{
    public static FactoryOutcomeRecoveryPlanDocument Build(
        DigitalWorkCase rootCase,
        FactoryOutcomeImpactSnapshot impact,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var machine = impact.MachineCode ?? "makine";
        var order = impact.OrderReference ?? "sipariş";
        var primarySteps = BuildPrimarySteps(rootCase, impact, childCases);
        var alternativeSteps = BuildAlternativeSteps(rootCase, impact, childCases);

        var primary = new FactoryOutcomeRecoveryPlanOption(
            FactoryOutcomeExecutionPlanKeys.Primary,
            "Kaynak kurtarma + kapasite koruma",
            $"{machine} performansını geri kazan ve {order} teslimatını koru",
            impact.QuantityAtRisk,
            impact.TimeToBreachLabel,
            "Orta",
            ["Üretim sırası değişikliği", "Kapasite yeniden dağıtımı"],
            primarySteps);

        var alternative = new FactoryOutcomeRecoveryPlanOption(
            FactoryOutcomeExecutionPlanKeys.Alternative,
            "Alternatif kapasiteye kaydırma",
            $"Hacmi alternatif enjeksiyon hattına taşı ve {order} taahhüdünü koru",
            impact.QuantityAtRisk,
            impact.TimeToBreachLabel,
            "Düşük-orta",
            ["Kapasite transferi", "Sipariş önceliklendirme"],
            alternativeSteps);

        return new FactoryOutcomeRecoveryPlanDocument(
            $"Sonuç kurtarma: {impact.OutcomeAtRisk}",
            FactoryOutcomeExecutionPlanKeys.Primary,
            [primary, alternative],
            [
                "Üretim çıktısı hedef bandında",
                "Teslimat taahhüdü korunuyor",
                "Bloklayıcılar giderildi",
                "Kanıt paketi tamamlandı",
            ]);
    }

    public static string ToJson(FactoryOutcomeRecoveryPlanDocument document)
    {
        var selected = ResolveSelectedPlan(document);

        return JsonSerializer.Serialize(new
        {
            objective = document.Objective,
            selectedPlanKey = document.SelectedPlanKey,
            verificationCriteria = document.VerificationCriteria,
            plans = document.Plans.Select(plan => new
            {
                planKey = plan.PlanKey,
                strategy = plan.Strategy,
                summary = plan.Summary,
                expectedRecoveredQuantity = plan.ExpectedRecoveredQuantity,
                projectedCompletionLabel = plan.ProjectedCompletionLabel,
                operationalRisk = plan.OperationalRisk,
                requiredApprovals = plan.RequiredApprovals,
                steps = plan.Steps.Select(step => new
                {
                    order = step.Order,
                    actionKey = step.ActionKey,
                    title = step.Title,
                    workerCode = step.WorkerCode,
                    workerName = step.WorkerName,
                    dependsOnActionKeys = step.DependsOnActionKeys,
                    requiresApproval = step.RequiresApproval,
                    verificationCriterion = step.VerificationCriterion,
                    linkedCaseId = step.LinkedCaseId,
                    status = "planned",
                }),
            }),
            steps = (selected?.Steps ?? []).Select(step => new
            {
                order = step.Order,
                actionKey = step.ActionKey,
                title = step.Title,
                workerCode = step.WorkerCode,
                workerName = step.WorkerName,
                dependsOnActionKeys = step.DependsOnActionKeys,
                requiresApproval = step.RequiresApproval,
                verificationCriterion = step.VerificationCriterion,
                linkedCaseId = step.LinkedCaseId,
                status = "planned",
            }),
        });
    }

    public static FactoryOutcomeRecoveryPlanDocument FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FactoryOutcomeRecoveryPlanDocument(string.Empty, FactoryOutcomeExecutionPlanKeys.Primary, [], []);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var objective = root.TryGetProperty("objective", out var objectiveEl) ? objectiveEl.GetString() ?? string.Empty : string.Empty;
            var selectedPlanKey = root.TryGetProperty("selectedPlanKey", out var selectedEl)
                ? selectedEl.GetString() ?? FactoryOutcomeExecutionPlanKeys.Primary
                : FactoryOutcomeExecutionPlanKeys.Primary;
            var verificationCriteria = ReadStringArray(root, "verificationCriteria");
            var plans = new List<FactoryOutcomeRecoveryPlanOption>();

            if (root.TryGetProperty("plans", out var plansEl) && plansEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var planEl in plansEl.EnumerateArray())
                {
                    plans.Add(ReadPlanOption(planEl));
                }
            }

            return new FactoryOutcomeRecoveryPlanDocument(objective, selectedPlanKey, plans, verificationCriteria);
        }
        catch (JsonException)
        {
            return new FactoryOutcomeRecoveryPlanDocument(string.Empty, FactoryOutcomeExecutionPlanKeys.Primary, [], []);
        }
    }

    public static FactoryOutcomeRecoveryPlanOption? ResolveSelectedPlan(FactoryOutcomeRecoveryPlanDocument document)
    {
        foreach (var plan in document.Plans)
        {
            if (plan.PlanKey == document.SelectedPlanKey)
            {
                return plan;
            }
        }

        return document.Plans.Count > 0 ? document.Plans[0] : null;
    }

    private static List<FactoryOutcomeRecoveryStep> BuildPrimarySteps(
        DigitalWorkCase rootCase,
        FactoryOutcomeImpactSnapshot impact,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var productionCase = FindChild(childCases, DigitalWorkforceWorkerCodes.ProductionRecovery);
        var maintenanceCase = FindChild(childCases, DigitalWorkforceWorkerCodes.MaintenanceResolution);
        var materialCase = FindChild(childCases, DigitalWorkforceWorkerCodes.MaterialShortage);

        return
        [
            new FactoryOutcomeRecoveryStep(
                1,
                "assess-cycle-loss",
                $"{impact.MachineCode ?? "Makine"} çevrim kaybını geri kazan",
                DigitalWorkforceWorkerCodes.ProductionRecovery,
                "Üretim Kurtarma",
                [],
                false,
                "Çevrim süresi referans bandına döndü",
                productionCase?.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                2,
                "secure-material-reservation",
                $"{impact.OrderReference ?? "Sipariş"} malzeme rezervasyonunu güvenceye al",
                DigitalWorkforceWorkerCodes.MaterialShortage,
                "Malzeme Açığı",
                ["assess-cycle-loss"],
                false,
                "Malzeme kapsaması taahhüt süresini destekliyor",
                materialCase?.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                3,
                "resolve-reliability-warning",
                $"{impact.MachineCode ?? "Makine"} güvenilirlik uyarısını gider",
                DigitalWorkforceWorkerCodes.MaintenanceResolution,
                "Bakım Çözüm",
                ["assess-cycle-loss"],
                false,
                "Makine stabil operasyona döndü",
                maintenanceCase?.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                4,
                "approve-recovery-plan",
                $"{impact.OrderReference ?? "Sipariş"} kurtarma planını onayla",
                DigitalWorkforceWorkerCodes.FactoryCoordinator,
                "Fabrika Operasyon Koordinatörü",
                ["secure-material-reservation", "resolve-reliability-warning"],
                true,
                "İnsan kararı kayıtlı",
                rootCase.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                5,
                "reproject-delivery",
                "Teslimat projeksiyonunu yeniden hesapla",
                DigitalWorkforceWorkerCodes.FactoryCoordinator,
                "Fabrika Operasyon Koordinatörü",
                ["approve-recovery-plan"],
                false,
                "Projeksiyon gerekli çıktıyı karşılıyor",
                rootCase.Id.ToString()),
        ];
    }

    private static List<FactoryOutcomeRecoveryStep> BuildAlternativeSteps(
        DigitalWorkCase rootCase,
        FactoryOutcomeImpactSnapshot impact,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var productionCase = FindChild(childCases, DigitalWorkforceWorkerCodes.ProductionRecovery);

        return
        [
            new FactoryOutcomeRecoveryStep(
                1,
                "validate-alternate-capacity",
                "Alternatif kapasite uygunluğunu doğrula",
                DigitalWorkforceWorkerCodes.ProductionRecovery,
                "Üretim Kurtarma",
                [],
                false,
                "Alternatif hat uygun",
                productionCase?.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                2,
                "shift-order-volume",
                $"{impact.OrderReference ?? "Sipariş"} hacmini alternatif hatta kaydır",
                DigitalWorkforceWorkerCodes.ProductionRecovery,
                "Üretim Kurtarma",
                ["validate-alternate-capacity"],
                true,
                "Kapasite transferi onaylandı",
                productionCase?.Id.ToString()),
            new FactoryOutcomeRecoveryStep(
                3,
                "reproject-delivery-alt",
                "Teslimat projeksiyonunu yeniden hesapla",
                DigitalWorkforceWorkerCodes.FactoryCoordinator,
                "Fabrika Operasyon Koordinatörü",
                ["shift-order-volume"],
                false,
                "Projeksiyon gerekli çıktıyı karşılıyor",
                rootCase.Id.ToString()),
        ];
    }

    private static DigitalWorkCase? FindChild(IReadOnlyList<DigitalWorkCase> childCases, string workerCode) =>
        childCases.FirstOrDefault(c => string.Equals(c.PrimaryWorker?.Code, workerCode, StringComparison.OrdinalIgnoreCase));

    private static FactoryOutcomeRecoveryPlanOption ReadPlanOption(JsonElement planEl)
    {
        var steps = new List<FactoryOutcomeRecoveryStep>();
        if (planEl.TryGetProperty("steps", out var stepsEl) && stepsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var stepEl in stepsEl.EnumerateArray())
            {
                steps.Add(new FactoryOutcomeRecoveryStep(
                    stepEl.TryGetProperty("order", out var orderEl) && orderEl.TryGetInt32(out var order) ? order : steps.Count + 1,
                    stepEl.TryGetProperty("actionKey", out var actionKeyEl) ? actionKeyEl.GetString() ?? string.Empty : string.Empty,
                    stepEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                    stepEl.TryGetProperty("workerCode", out var workerCodeEl) ? workerCodeEl.GetString() ?? string.Empty : string.Empty,
                    stepEl.TryGetProperty("workerName", out var workerNameEl) ? workerNameEl.GetString() ?? string.Empty : string.Empty,
                    ReadStringArray(stepEl, "dependsOnActionKeys"),
                    stepEl.TryGetProperty("requiresApproval", out var approvalEl) && approvalEl.GetBoolean(),
                    stepEl.TryGetProperty("verificationCriterion", out var criterionEl) ? criterionEl.GetString() ?? string.Empty : string.Empty,
                    stepEl.TryGetProperty("linkedCaseId", out var linkedEl) ? linkedEl.GetString() : null));
            }
        }

        return new FactoryOutcomeRecoveryPlanOption(
            planEl.TryGetProperty("planKey", out var planKeyEl) ? planKeyEl.GetString() ?? string.Empty : string.Empty,
            planEl.TryGetProperty("strategy", out var strategyEl) ? strategyEl.GetString() ?? string.Empty : string.Empty,
            planEl.TryGetProperty("summary", out var summaryEl) ? summaryEl.GetString() ?? string.Empty : string.Empty,
            planEl.TryGetProperty("expectedRecoveredQuantity", out var qtyEl) && qtyEl.TryGetInt32(out var qty) ? qty : null,
            planEl.TryGetProperty("projectedCompletionLabel", out var completionEl) ? completionEl.GetString() : null,
            planEl.TryGetProperty("operationalRisk", out var riskEl) ? riskEl.GetString() ?? string.Empty : string.Empty,
            ReadStringArray(planEl, "requiredApprovals"),
            steps);
    }

    private static List<string> ReadStringArray(JsonElement root, string property) =>
        root.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(s => s.Length > 0).ToList()
            : [];
}
