using CrevaFactOS.Persistence.DigitalWorkforce;

namespace CrevaFactOS.Persistence.FactoryOrchestration;

public static class FactoryDependencyGraphBuilder
{
    public static List<FactoryCaseDependency> BuildInitialGraph(
        Guid tenantId,
        Guid facilityId,
        Guid rootCaseId,
        FactoryDomainEvent eventData,
        IReadOnlyList<FactoryImpactNode> impacts,
        Guid? specialistWorkerId)
    {
        var now = DateTimeOffset.UtcNow;
        var dependencies = new List<FactoryCaseDependency>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                RootCaseId = rootCaseId,
                FromNodeKey = $"event:{eventData.SourceType}:{eventData.SourceId}",
                ToNodeKey = $"case:{rootCaseId}",
                FromNodeType = FactoryDependencyNodeTypes.Event,
                ToNodeType = FactoryDependencyNodeTypes.DomainIssue,
                EdgeType = FactoryDependencyEdgeTypes.Triggers,
                CreatedAtUtc = now,
            },
        };

        foreach (var impact in impacts)
        {
            dependencies.Add(new FactoryCaseDependency
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                RootCaseId = rootCaseId,
                FromNodeKey = $"case:{rootCaseId}",
                ToNodeKey = $"domain:{impact.Domain}",
                FromNodeType = FactoryDependencyNodeTypes.DomainIssue,
                ToNodeType = FactoryDependencyNodeTypes.DomainIssue,
                EdgeType = FactoryDependencyEdgeTypes.DependsOn,
                MetadataJson = $"{{\"summary\":\"{impact.Summary}\"}}",
                CreatedAtUtc = now,
            });
        }

        if (specialistWorkerId.HasValue)
        {
            dependencies.Add(new FactoryCaseDependency
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                RootCaseId = rootCaseId,
                FromNodeKey = $"domain:{FactoryCaseCorrelationEngine.ResolvePrimaryDomain(eventData.EventType)}",
                ToNodeKey = $"worker:{specialistWorkerId}",
                FromNodeType = FactoryDependencyNodeTypes.DomainIssue,
                ToNodeType = FactoryDependencyNodeTypes.DigitalWorker,
                EdgeType = FactoryDependencyEdgeTypes.HandoffTo,
                LinkedWorkerId = specialistWorkerId.Value.ToString(),
                CreatedAtUtc = now,
            });
        }

        dependencies.Add(new FactoryCaseDependency
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            RootCaseId = rootCaseId,
            FromNodeKey = $"case:{rootCaseId}",
            ToNodeKey = "outcome:verified-factory-close",
            FromNodeType = FactoryDependencyNodeTypes.DomainIssue,
            ToNodeType = FactoryDependencyNodeTypes.Outcome,
            EdgeType = FactoryDependencyEdgeTypes.Verifies,
            CreatedAtUtc = now,
        });

        return dependencies;
    }

    public static void AddChildCaseEdge(
        List<FactoryCaseDependency> dependencies,
        Guid tenantId,
        Guid facilityId,
        Guid rootCaseId,
        string fromDomain,
        Guid childCaseId,
        Guid workerId)
    {
        dependencies.Add(new FactoryCaseDependency
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            RootCaseId = rootCaseId,
            FromNodeKey = $"domain:{fromDomain}",
            ToNodeKey = $"case:{childCaseId}",
            FromNodeType = FactoryDependencyNodeTypes.DomainIssue,
            ToNodeType = FactoryDependencyNodeTypes.DigitalWorker,
            EdgeType = FactoryDependencyEdgeTypes.HandoffTo,
            LinkedCaseId = childCaseId.ToString(),
            LinkedWorkerId = workerId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
    }
}
