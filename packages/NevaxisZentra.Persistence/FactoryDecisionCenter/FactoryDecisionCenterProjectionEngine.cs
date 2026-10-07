using System.Globalization;
using System.Text.Json;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryOrchestration;
using NevaxisZentra.Persistence.FactoryOutcomeExecution;
using NevaxisZentra.Persistence.FactoryRecoveryIntelligence;

namespace NevaxisZentra.Persistence.FactoryDecisionCenter;

public static class FactoryDecisionCenterProjectionEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static FactoryDecisionCenterExecutiveSummary BuildSummary(
        IReadOnlyList<DigitalWorkCase> rootCases)
    {
        var decisionCases = rootCases
            .Where(IsDecisionCenterCase)
            .ToList();

        return new FactoryDecisionCenterExecutiveSummary(
            decisionCases.Count(c => c.Severity is "critical" or "high"),
            decisionCases.Count(c => c.Status != DigitalWorkCaseStatuses.Closed),
            decisionCases.Count(c => c.Status == DigitalWorkCaseStatuses.AwaitingApproval || c.RequiresApproval),
            decisionCases.Count(c => c.Status is DigitalWorkCaseStatuses.Executing or DigitalWorkCaseStatuses.Verifying),
            decisionCases.Count(c =>
                c.Status == DigitalWorkCaseStatuses.Verifying ||
                (c.Status == DigitalWorkCaseStatuses.Executing &&
                 !string.Equals(c.VerificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))));
    }

    public static FactoryDecisionCaseQueueItem ProjectQueueItem(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var metadata = ParseMetadata(rootCase.MetadataJson);
        var workers = ResolveWorkerNames(rootCase, childCases);
        var recoveryPlan = FactoryRecoveryPlanProjection.FromJson(rootCase.RecoveryPlanJson);

        return new FactoryDecisionCaseQueueItem(
            rootCase.Id,
            rootCase.Title,
            rootCase.Severity,
            rootCase.Status,
            GetString(metadata, FactoryDecisionCenterMetadataKeys.OrderReference, FactoryDecisionCenterConstants.OrderReference),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.CustomerName, FactoryDecisionCenterConstants.CustomerName),
            FormatDeliveryCommitment(metadata),
            BuildRiskSummary(metadata),
            GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity),
            GetInt(metadata, FactoryDecisionCenterMetadataKeys.ProjectedQuantity, FactoryDecisionCenterConstants.InitialProjectedQuantity),
            GetInt(metadata, FactoryDecisionCenterMetadataKeys.GapQuantity, FactoryDecisionCenterConstants.InitialGapQuantity),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.RecommendedStrategy, recoveryPlan.Summary),
            workers,
            rootCase.RequiresApproval || rootCase.Status == DigitalWorkCaseStatuses.AwaitingApproval
                ? "Kurtarma planı üretim yöneticisi onayı gerektiriyor"
                : null,
            rootCase.CurrentBlockerCode,
            rootCase.VerificationState,
            rootCase.UpdatedAtUtc,
            rootCase.ConcurrencyToken);
    }

    public static FactoryDecisionCaseDetailSnapshot ProjectDetail(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        IReadOnlyList<DigitalWorkVerification> verifications,
        DigitalWorkApproval? pendingApproval,
        string facilityLabel,
        FactoryRecoveryIntelligenceSnapshot? recoveryIntelligence = null)
    {
        var metadata = ParseMetadata(rootCase.MetadataJson);
        var recoveryPlan = FactoryRecoveryPlanProjection.FromJson(rootCase.RecoveryPlanJson);
        var steps = BuildRecoverySteps(recoveryPlan, childCases);
        var workers = BuildWorkerAssignments(rootCase, childCases);
        var execution = BuildExecutionProgress(childCases);
        var evidence = BuildEvidence(verifications, metadata, childCases);
        var verified = BuildVerifiedOutcome(rootCase, metadata);

        return new FactoryDecisionCaseDetailSnapshot(
            rootCase.Id,
            rootCase.Title,
            rootCase.Severity,
            rootCase.Status,
            rootCase.Description,
            BuildBusinessOutcome(metadata),
            facilityLabel,
            GetString(metadata, FactoryDecisionCenterMetadataKeys.OrderReference, FactoryDecisionCenterConstants.OrderReference),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.CustomerName, FactoryDecisionCenterConstants.CustomerName),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.MachineCode, FactoryDecisionCenterConstants.MachineCode),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.MoldCode, FactoryDecisionCenterConstants.MoldCode),
            GetString(metadata, FactoryDecisionCenterMetadataKeys.MaterialCode, FactoryDecisionCenterConstants.MaterialCode),
            FormatDeliveryCommitment(metadata),
            BuildOutcomeRisk(metadata),
            BuildAssessment(metadata),
            steps,
            workers,
            pendingApproval is null
                ? null
                : new FactoryDecisionApprovalCard(
                    pendingApproval.Id,
                    pendingApproval.Action,
                    pendingApproval.BusinessReason,
                    pendingApproval.Status,
                    pendingApproval.ConcurrencyToken),
            execution,
            evidence,
            verified,
            rootCase.Activities
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(20)
                .Select(a => new FactoryDecisionTimelineEntry(
                    a.ActionType,
                    a.Description,
                    a.CreatedAtUtc))
                .ToList(),
            recoveryIntelligence,
            rootCase.ConcurrencyToken);
    }

    public static bool IsDecisionCenterCase(DigitalWorkCase caseRow) =>
        string.Equals(caseRow.Source, FactoryDecisionCenterConstants.SourceModule, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(caseRow.Source, FactoryOutcomeExecutionConstants.SourceModule, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrWhiteSpace(caseRow.CorrelationKey) &&
         (caseRow.CorrelationKey.Contains("ord-2026-0915-047", StringComparison.OrdinalIgnoreCase) ||
          caseRow.CorrelationKey.Contains("outcome:order:", StringComparison.OrdinalIgnoreCase)));

    public static FactoryDecisionCenterLiveQueues BuildLiveQueues(
        IReadOnlyList<DigitalWorkCase> rootCases,
        Func<DigitalWorkCase, FactoryDecisionCaseQueueItem> projectItem)
    {
        var decisionCases = rootCases.Where(IsDecisionCenterCase).ToList();
        var today = DateTime.UtcNow.Date;

        return new FactoryDecisionCenterLiveQueues(
            decisionCases.Where(c => c.Severity is "critical" or "high" && c.Status != DigitalWorkCaseStatuses.Closed)
                .Select(projectItem).ToList(),
            decisionCases.Where(c => c.Status is DigitalWorkCaseStatuses.Executing)
                .Select(projectItem).ToList(),
            decisionCases.Where(c => c.Status == DigitalWorkCaseStatuses.AwaitingApproval || c.RequiresApproval)
                .Select(projectItem).ToList(),
            decisionCases.Where(c => c.CurrentBlockerCode is FactoryCaseBlockerCodes.Deadlock or FactoryCaseBlockerCodes.WaitingHuman &&
                                     c.Status != DigitalWorkCaseStatuses.Closed)
                .Select(projectItem).ToList(),
            decisionCases.Where(c => c.Status == DigitalWorkCaseStatuses.Verifying)
                .Select(projectItem).ToList(),
            decisionCases.Where(c =>
                c.Status == DigitalWorkCaseStatuses.Closed &&
                string.Equals(c.VerificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase) &&
                c.ClosedAtUtc?.Date == today)
                .Select(projectItem).ToList());
    }

    public static Dictionary<string, JsonElement> ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static FactoryDecisionOutcomeRisk BuildOutcomeRisk(Dictionary<string, JsonElement> metadata) =>
        new(
            BuildBusinessOutcome(metadata),
            GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity),
            GetProjectedQuantity(metadata),
            GetGapQuantity(metadata),
            FormatTimeRemaining(metadata),
            GetDecimal(metadata, FactoryDecisionCenterMetadataKeys.Confidence, 0.87m));

    private static FactoryDecisionAssessment BuildAssessment(Dictionary<string, JsonElement> metadata) =>
        new(
            GetString(metadata, FactoryDecisionCenterMetadataKeys.PrimaryCause,
                $"{FactoryDecisionCenterConstants.MachineCode} çevrim süresi bozulması"),
            ReadStringArray(metadata, FactoryDecisionCenterMetadataKeys.ContributingFactors));

    private static FactoryDecisionVerifiedOutcome BuildVerifiedOutcome(
        DigitalWorkCase rootCase,
        Dictionary<string, JsonElement> metadata)
    {
        var verified = string.Equals(rootCase.VerificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase) &&
                       rootCase.Status == DigitalWorkCaseStatuses.Closed;
        var verifiedProjection = GetInt(metadata, FactoryDecisionCenterMetadataKeys.VerifiedProjection, 0);
        var required = GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity);
        var deliveryProtected = metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.DeliveryProtected, out var protectedValue) &&
                                protectedValue.ValueKind == JsonValueKind.True;

        return new FactoryDecisionVerifiedOutcome(
            verified,
            required,
            verifiedProjection > 0 ? verifiedProjection : GetProjectedQuantity(metadata),
            deliveryProtected,
            GetInt(metadata, FactoryDecisionCenterMetadataKeys.RecoveryGain, Math.Max(0, verifiedProjection - required)),
            rootCase.VerifiedAtUtc);
    }

    private static IReadOnlyList<FactoryDecisionRecoveryStep> BuildRecoverySteps(
        FactoryRecoveryPlanProjection recoveryPlan,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        if (recoveryPlan.Steps.Count > 0)
        {
            return recoveryPlan.Steps;
        }

        return
        [
            new(1, "INJ-001 çevrim süresini geri kazan", DigitalWorkforceWorkerCodes.ProductionRecovery, "Üretim Kurtarma Dijital Çalışanı", MapChildStatus(childCases, DigitalWorkforceWorkerCodes.ProductionRecovery), FindChildId(childCases, DigitalWorkforceWorkerCodes.ProductionRecovery), "recover-cycle-time"),
            new(2, "Gerekli malzemeyi rezerve et", DigitalWorkforceWorkerCodes.MaterialShortage, "Malzeme Açığı Çözüm Dijital Çalışanı", MapChildStatus(childCases, DigitalWorkforceWorkerCodes.MaterialShortage), FindChildId(childCases, DigitalWorkforceWorkerCodes.MaterialShortage), "reserve-material"),
            new(3, "Alternatif kapasiteyi hazırla", DigitalWorkforceWorkerCodes.ProductionRecovery, "Üretim Kurtarma Dijital Çalışanı", "planned", null, "alternate-capacity"),
            new(4, "Bakım engelini temizle", DigitalWorkforceWorkerCodes.MaintenanceResolution, "Bakım Çözüm Dijital Çalışanı", MapChildStatus(childCases, DigitalWorkforceWorkerCodes.MaintenanceResolution), FindChildId(childCases, DigitalWorkforceWorkerCodes.MaintenanceResolution), "clear-maintenance"),
            new(5, "Teslimat projeksiyonunu yeniden hesapla", DigitalWorkforceWorkerCodes.FactoryCoordinator, "Fabrika Operasyon Koordinatörü", "planned", null, "recalculate-delivery"),
            new(6, "Kurtarılan çıktıyı doğrula", DigitalWorkforceWorkerCodes.FactoryCoordinator, "Fabrika Operasyon Koordinatörü", "planned", null, "verify-output"),
        ];
    }

    private static List<FactoryDecisionWorkerAssignment> BuildWorkerAssignments(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var assignments = new List<FactoryDecisionWorkerAssignment>
        {
            new(
                DigitalWorkforceWorkerCodes.FactoryCoordinator,
                rootCase.PrimaryWorker?.Name ?? "Fabrika Operasyon Koordinatörü",
                "Genel kurtarma vakasını koordine eder",
                MapStatusLabel(rootCase.Status),
                rootCase.CurrentBlockerCode),
        };

        foreach (var child in childCases.OrderBy(c => c.CreatedAtUtc))
        {
            assignments.Add(new(
                child.PrimaryWorker?.Code ?? string.Empty,
                child.PrimaryWorker?.Name ?? "Dijital Çalışan",
                ResolveResponsibility(child.PrimaryWorker?.Code),
                MapStatusLabel(child.Status),
                child.CurrentBlockerCode));
        }

        return assignments;
    }

    private static List<FactoryDecisionWorkerAssignment> BuildExecutionProgress(
        IReadOnlyList<DigitalWorkCase> childCases) =>
        childCases
            .OrderBy(c => c.CreatedAtUtc)
            .Select(child => new FactoryDecisionWorkerAssignment(
                child.PrimaryWorker?.Code ?? string.Empty,
                child.PrimaryWorker?.Name ?? "Dijital Çalışan",
                child.Title,
                MapExecutionStatus(child.Status, child.VerificationState),
                child.CurrentBlockerCode))
            .ToList();

    private static List<FactoryDecisionEvidenceItem> BuildEvidence(
        IReadOnlyList<DigitalWorkVerification> verifications,
        Dictionary<string, JsonElement> metadata,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var items = new List<FactoryDecisionEvidenceItem>
        {
            new("production-performance", "Üretim Performansı",
                childCases.Any(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.ProductionRecovery && c.Status == DigitalWorkCaseStatuses.Closed)
                    ? "verified" : "pending",
                $"{FactoryDecisionCenterConstants.MachineCode} çevrim süresi {FactoryDecisionCenterConstants.CycleTimeCurrentSec:0.0}s (referans {FactoryDecisionCenterConstants.CycleTimeReferenceSec:0.0}s)",
                childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.ProductionRecovery)?.UpdatedAtUtc),
            new("machine-performance", "Makine Performansı",
                childCases.Any(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaintenanceResolution && c.VerificationState == DigitalWorkVerificationResults.Passed)
                    ? "verified" : "pending",
                $"{FactoryDecisionCenterConstants.MicroStopCount} mikro duruş / {FactoryDecisionCenterConstants.MicroStopDurationSec} sn",
                childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaintenanceResolution)?.UpdatedAtUtc),
            new("material-reservation", "Malzeme Rezervasyonu",
                childCases.Any(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaterialShortage && c.Status == DigitalWorkCaseStatuses.Closed)
                    ? "verified" : "pending",
                $"{FactoryDecisionCenterConstants.MaterialCode} rezervasyon çakışması çözülüyor",
                childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaterialShortage)?.UpdatedAtUtc),
            new("maintenance-check", "Bakım Kontrolü",
                childCases.Any(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaintenanceResolution && c.Status == DigitalWorkCaseStatuses.Closed)
                    ? "verified" : "pending",
                $"{FactoryDecisionCenterConstants.MoldCode} bakım riski değerlendirildi",
                childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == DigitalWorkforceWorkerCodes.MaintenanceResolution)?.UpdatedAtUtc),
            new("order-projection", "Sipariş Projeksiyonu",
                metadata.ContainsKey(FactoryDecisionCenterMetadataKeys.VerifiedProjection) ? "verified" : "pending",
                $"Projeksiyon {GetProjectedQuantity(metadata)} / {GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity)}",
                null),
        };

        foreach (var verification in verifications.OrderByDescending(v => v.VerifiedAtUtc))
        {
            items.Add(new(
                verification.VerificationType,
                verification.VerificationType,
                verification.Result,
                $"{verification.ExpectedState} → {verification.ObservedState}",
                verification.VerifiedAtUtc));
        }

        return items;
    }

    private static List<string> ResolveWorkerNames(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases)
    {
        var names = new List<string> { ShortWorkerName(rootCase.PrimaryWorker?.Name) };
        foreach (var child in childCases)
        {
            var name = ShortWorkerName(child.PrimaryWorker?.Name);
            if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static string ShortWorkerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        return fullName
            .Replace(" Dijital Çalışanı", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" Çözüm", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string ResolveResponsibility(string? workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.ProductionRecovery => "Üretim performansını geri kazanır",
        DigitalWorkforceWorkerCodes.MaintenanceResolution => "Makine güvenilirlik engelini temizler",
        DigitalWorkforceWorkerCodes.MaterialShortage => "Malzeme erişilebilirliğini güvenceye alır",
        DigitalWorkforceWorkerCodes.MoldLifecycle => "Kalıp yaşam döngüsü riskini yönetir",
        DigitalWorkforceWorkerCodes.FactoryCoordinator => "Genel kurtarma vakasını koordine eder",
        _ => "Kurtarma operasyonunu yürütür",
    };

    private static string BuildBusinessOutcome(Dictionary<string, JsonElement> metadata)
    {
        var customer = GetString(metadata, FactoryDecisionCenterMetadataKeys.CustomerName, FactoryDecisionCenterConstants.CustomerName);
        var order = GetString(metadata, FactoryDecisionCenterMetadataKeys.OrderReference, FactoryDecisionCenterConstants.OrderReference);
        var required = GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity);
        var delivery = FormatDeliveryCommitment(metadata);
        return $"{customer} için {required} parçalık {order} siparişinin {delivery} teslimat taahhüdü risk altında.";
    }

    private static string BuildRiskSummary(Dictionary<string, JsonElement> metadata)
    {
        var required = GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity);
        return $"{required} parçalık siparişin zamanında tamamlanamaması";
    }

    private static string FormatDeliveryCommitment(Dictionary<string, JsonElement> metadata)
    {
        if (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.DeliveryCommitmentUtc, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var delivery))
        {
            return delivery.ToLocalTime().ToString("d MMMM · HH:mm", new CultureInfo("tr-TR"));
        }

        return "16 Eylül · 08:00";
    }

    private static string FormatTimeRemaining(Dictionary<string, JsonElement> metadata)
    {
        if (metadata.TryGetValue(FactoryDecisionCenterMetadataKeys.DeliveryCommitmentUtc, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var delivery))
        {
            var remaining = delivery - DateTimeOffset.UtcNow;
            if (remaining.TotalHours < 0)
            {
                return "Taahhüt süresi geçti";
            }

            return remaining.TotalHours >= 24
                ? $"{Math.Floor(remaining.TotalDays)} gün {remaining.Hours} saat"
                : $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalHours))} saat";
        }

        return "18 saat";
    }

    private static int GetProjectedQuantity(Dictionary<string, JsonElement> metadata) =>
        GetInt(metadata, FactoryDecisionCenterMetadataKeys.VerifiedProjection, 0) > 0
            ? GetInt(metadata, FactoryDecisionCenterMetadataKeys.VerifiedProjection, 0)
            : GetInt(metadata, FactoryDecisionCenterMetadataKeys.ProjectedQuantity, FactoryDecisionCenterConstants.InitialProjectedQuantity);

    private static int GetGapQuantity(Dictionary<string, JsonElement> metadata) =>
        Math.Max(0, GetInt(metadata, FactoryDecisionCenterMetadataKeys.RequiredQuantity, FactoryDecisionCenterConstants.RequiredQuantity) -
                     GetProjectedQuantity(metadata));

    private static string MapStatusLabel(string status) => status switch
    {
        DigitalWorkCaseStatuses.AwaitingApproval => "İnsan onayı bekliyor",
        DigitalWorkCaseStatuses.Executing => "Yürütülüyor",
        DigitalWorkCaseStatuses.Verifying => "Doğrulama bekliyor",
        DigitalWorkCaseStatuses.Closed => "Tamamlandı",
        DigitalWorkCaseStatuses.Planning => "Planlanıyor",
        _ => "Bekliyor",
    };

    private static string MapExecutionStatus(string status, string verificationState)
    {
        if (status == DigitalWorkCaseStatuses.Closed &&
            string.Equals(verificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))
        {
            return "DOĞRULANDI";
        }

        return status switch
        {
            DigitalWorkCaseStatuses.Executing => "YÜRÜTÜLÜYOR",
            DigitalWorkCaseStatuses.Closed => "TAMAMLANDI",
            DigitalWorkCaseStatuses.AwaitingApproval => "ONAY BEKLİYOR",
            _ => "BEKLİYOR",
        };
    }

    private static string MapChildStatus(IReadOnlyList<DigitalWorkCase> childCases, string workerCode)
    {
        var child = childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == workerCode);
        return child is null ? "planned" : child.Status;
    }

    private static Guid? FindChildId(IReadOnlyList<DigitalWorkCase> childCases, string workerCode) =>
        childCases.FirstOrDefault(c => c.PrimaryWorker?.Code == workerCode)?.Id;

    private static string GetString(Dictionary<string, JsonElement> metadata, string key, string fallback) =>
        metadata.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static int GetInt(Dictionary<string, JsonElement> metadata, string key, int fallback) =>
        metadata.TryGetValue(key, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : fallback;

    private static decimal GetDecimal(Dictionary<string, JsonElement> metadata, string key, decimal fallback) =>
        metadata.TryGetValue(key, out var value) && value.TryGetDecimal(out var parsed)
            ? parsed
            : fallback;

    private static List<string> ReadStringArray(Dictionary<string, JsonElement> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return
            [
                $"{FactoryDecisionCenterConstants.MicroStopCount} mikro duruş / {FactoryDecisionCenterConstants.MicroStopDurationSec} sn",
                "Malzeme rezervasyon çakışması",
                "Kalıp bakım riski",
            ];
        }

        return value.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToList();
    }
}

internal sealed class FactoryRecoveryPlanProjection
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<FactoryDecisionRecoveryStep> Steps { get; init; } = [];

    public static FactoryRecoveryPlanProjection FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FactoryRecoveryPlanProjection();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var summary = doc.RootElement.TryGetProperty("objective", out var objective)
                ? objective.GetString() ?? string.Empty
                : string.Empty;

            if (!doc.RootElement.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            {
                return new FactoryRecoveryPlanProjection { Summary = summary };
            }

            var projectedSteps = steps.EnumerateArray()
                .Select((step, index) => new FactoryDecisionRecoveryStep(
                    step.TryGetProperty("order", out var order) && order.TryGetInt32(out var orderValue) ? orderValue : index + 1,
                    step.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                    step.TryGetProperty("workerCode", out var workerCode) ? workerCode.GetString() ?? string.Empty : string.Empty,
                    step.TryGetProperty("workerName", out var workerName) ? workerName.GetString() ?? string.Empty : string.Empty,
                    step.TryGetProperty("status", out var status) ? status.GetString() ?? "planned" : "planned",
                    step.TryGetProperty("linkedCaseId", out var linkedCaseId) && linkedCaseId.TryGetGuid(out var linkedId) ? linkedId : null,
                    step.TryGetProperty("actionKey", out var actionKey) ? actionKey.GetString() : null))
                .ToList();

            return new FactoryRecoveryPlanProjection
            {
                Summary = summary,
                Steps = projectedSteps,
            };
        }
        catch (JsonException)
        {
            return new FactoryRecoveryPlanProjection();
        }
    }
}
