using System.Text.Json;

namespace CrevaFactOS.Persistence.AiTasks;

public interface IAiTaskClosureEngine
{
    AiTaskClosurePolicyReadModel ResolvePolicy(
        AiTask task,
        IReadOnlyList<AiTaskClosurePolicy> policies);

    AiTaskClosureValidationResult ValidateClosureRequest(
        AiTask task,
        IReadOnlyList<AiTaskEvidence> evidenceItems,
        AiTaskClosurePolicyReadModel policy);
}

public sealed class AiTaskClosureEngine : IAiTaskClosureEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AiTaskClosurePolicyReadModel ResolvePolicy(
        AiTask task,
        IReadOnlyList<AiTaskClosurePolicy> policies)
    {
        var enabled = policies
            .Where(p => p.Enabled)
            .OrderByDescending(p => ScorePolicy(p, task))
            .FirstOrDefault();

        if (enabled is not null)
        {
            return MapPolicy(enabled);
        }

        return ResolveFallbackPolicy(task);
    }

    public AiTaskClosureValidationResult ValidateClosureRequest(
        AiTask task,
        IReadOnlyList<AiTaskEvidence> evidenceItems,
        AiTaskClosurePolicyReadModel policy)
    {
        var issues = new List<AiTaskClosureValidationIssue>();

        if (AiTaskStatuses.Final.Contains(task.Status)
            && task.ClosureState != AiTaskClosureStates.Reopened)
        {
            issues.Add(new("task-final", "Görev zaten sonlandırılmış durumda."));
        }

        if (task.ClosureState == AiTaskClosureStates.AwaitingClosureApproval)
        {
            issues.Add(new("closure-pending", "Görev zaten kapanış onayı bekliyor."));
        }

        if (task.Status is not AiTaskStatuses.InProgress and not AiTaskStatuses.Approved)
        {
            issues.Add(new("invalid-status", "Kapanış talebi yalnızca uygulanmakta olan görevler için yapılabilir."));
        }

        if (policy.ResolutionSummaryRequired && string.IsNullOrWhiteSpace(task.ResolutionSummary))
        {
            issues.Add(new("resolution-summary-required", "Çözüm özeti zorunludur."));
        }

        if (string.IsNullOrWhiteSpace(task.ResolutionCode))
        {
            issues.Add(new("resolution-code-required", "Çözüm kodu zorunludur."));
        }
        else if (!AiTaskResolutionCodes.All.Contains(task.ResolutionCode))
        {
            issues.Add(new("resolution-code-invalid", "Geçersiz çözüm kodu."));
        }

        if (evidenceItems.Count < policy.MinimumEvidenceCount)
        {
            issues.Add(new(
                "evidence-count",
                $"En az {policy.MinimumEvidenceCount} kanıt kaydı gereklidir."));
        }

        foreach (var requiredType in policy.RequiredEvidenceTypes)
        {
            if (!evidenceItems.Any(item => item.EvidenceType == requiredType))
            {
                issues.Add(new(
                    "evidence-type-missing",
                    $"Gerekli kanıt türü eksik: {requiredType}"));
            }
        }

        return issues.Count == 0
            ? AiTaskClosureValidationResult.Success()
            : AiTaskClosureValidationResult.Fail(issues.ToArray());
    }

    private static AiTaskClosurePolicyReadModel ResolveFallbackPolicy(AiTask task)
    {
        var approvalRequired = task.Priority is AiTaskPriorities.Critical or AiTaskPriorities.High
            || task.Category is AiTaskCategories.Maintenance or AiTaskCategories.Quality;
        var minimumEvidence = task.Priority == AiTaskPriorities.Critical ? 1 : 0;
        var requiredTypes = task.Category == AiTaskCategories.Quality
            ? new[] { AiTaskEvidenceTypes.QualityRecord, AiTaskEvidenceTypes.Note }
            : Array.Empty<string>();

        if (task.Category == AiTaskCategories.Quality && minimumEvidence == 0)
        {
            minimumEvidence = 1;
        }

        return new AiTaskClosurePolicyReadModel(
            "fallback-default",
            "Varsayılan kapanış politikası",
            minimumEvidence,
            requiredTypes,
            approvalRequired,
            approvalRequired,
            true);
    }

    private static int ScorePolicy(AiTaskClosurePolicy policy, AiTask task)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(policy.SourceModule)
            && policy.SourceModule == task.SourceModule)
        {
            score += 8;
        }

        if (!string.IsNullOrWhiteSpace(policy.Category) && policy.Category == task.Category)
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace(policy.Priority) && policy.Priority == task.Priority)
        {
            score += 2;
        }

        if (policy.FacilityId.HasValue && policy.FacilityId == task.FacilityId)
        {
            score += 1;
        }

        return score;
    }

    private static AiTaskClosurePolicyReadModel MapPolicy(AiTaskClosurePolicy policy)
    {
        IReadOnlyList<string> requiredTypes = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(policy.RequiredEvidenceTypesJson))
        {
            try
            {
                requiredTypes = JsonSerializer.Deserialize<string[]>(policy.RequiredEvidenceTypesJson, JsonOptions)
                    ?? Array.Empty<string>();
            }
            catch (JsonException)
            {
                requiredTypes = Array.Empty<string>();
            }
        }

        return new AiTaskClosurePolicyReadModel(
            policy.PolicyKey,
            policy.Name,
            policy.MinimumEvidenceCount,
            requiredTypes,
            policy.ApprovalRequired,
            policy.FourEyesRequired,
            policy.ResolutionSummaryRequired);
    }
}
