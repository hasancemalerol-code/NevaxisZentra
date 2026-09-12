namespace NevaxisFactOS.Persistence.AiTasks;

public static class AiTaskPlaybookGraphValidator
{
    public static AiTaskPlaybookGraphValidationResult Validate(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies)
    {
        var errors = new List<string>();
        var stepKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.StepKey))
            {
                errors.Add("Step key is required.");
                continue;
            }

            if (!stepKeys.Add(step.StepKey.Trim()))
            {
                errors.Add($"Duplicate step key '{step.StepKey}'.");
            }
        }

        var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.StepKey) || string.IsNullOrWhiteSpace(dependency.DependsOnStepKey))
            {
                errors.Add("Dependency step keys are required.");
                continue;
            }

            if (!stepKeys.Contains(dependency.StepKey))
            {
                errors.Add($"Unknown dependent step '{dependency.StepKey}'.");
            }

            if (!stepKeys.Contains(dependency.DependsOnStepKey))
            {
                errors.Add($"Unknown predecessor step '{dependency.DependsOnStepKey}'.");
            }

            if (string.Equals(dependency.StepKey, dependency.DependsOnStepKey, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Step '{dependency.StepKey}' cannot depend on itself.");
            }

            if (!AiTaskPlaybookDependencyTypes.All.Contains(dependency.DependencyType))
            {
                errors.Add($"Invalid dependency type '{dependency.DependencyType}'.");
            }

            var signature = $"{dependency.StepKey}->{dependency.DependsOnStepKey}";
            if (!seenDependencies.Add(signature))
            {
                errors.Add($"Duplicate dependency '{signature}'.");
            }
        }

        if (errors.Count > 0)
        {
            return new AiTaskPlaybookGraphValidationResult(false, errors);
        }

        var adjacency = BuildAdjacency(steps, dependencies);
        if (HasCycle(adjacency))
        {
            errors.Add("Dependency graph contains a cycle.");
        }

        return new AiTaskPlaybookGraphValidationResult(errors.Count == 0, errors);
    }

    private static Dictionary<string, List<string>> BuildAdjacency(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies)
    {
        var adjacency = steps.ToDictionary(
            step => step.StepKey,
            _ => new List<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var dependency in dependencies)
        {
            if (!adjacency.TryGetValue(dependency.DependsOnStepKey, out var targets))
            {
                continue;
            }

            targets.Add(dependency.StepKey);
        }

        return adjacency;
    }

    private static bool HasCycle(Dictionary<string, List<string>> adjacency)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in adjacency.Keys)
        {
            if (Visit(node))
            {
                return true;
            }
        }

        return false;

        bool Visit(string node)
        {
            if (visited.Contains(node))
            {
                return false;
            }

            if (!visiting.Add(node))
            {
                return true;
            }

            if (adjacency.TryGetValue(node, out var children))
            {
                foreach (var child in children)
                {
                    if (Visit(child))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }
}
