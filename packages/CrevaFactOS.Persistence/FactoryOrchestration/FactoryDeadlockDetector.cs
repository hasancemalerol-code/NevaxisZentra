namespace CrevaFactOS.Persistence.FactoryOrchestration;

public static class FactoryDeadlockDetector
{
    public static bool HasCycle(IReadOnlyList<FactoryCaseDependency> dependencies)
    {
        if (dependencies.Count == 0)
        {
            return false;
        }

        var graph = dependencies
            .GroupBy(d => d.FromNodeKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ToNodeKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in graph.Keys)
        {
            if (Visit(node, graph, visiting, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Visit(
        string node,
        IReadOnlyDictionary<string, List<string>> graph,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(node))
        {
            return false;
        }

        if (!visiting.Add(node))
        {
            return true;
        }

        if (graph.TryGetValue(node, out var edges))
        {
            foreach (var next in edges)
            {
                if (Visit(next, graph, visiting, visited))
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
