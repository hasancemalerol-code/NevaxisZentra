namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public static class DigitalWorkforceClosureContracts
{
    public const string ProductionRecovery = "dw-fact-001-production-recovery";
    public const string MaintenanceResolution = "dw-fact-002-maintenance-resolution";
    public const string MaterialShortage = "dw-fact-003-material-shortage";
    public const string QualityNcr = "dw-fact-004-quality-ncr";
    public const string SupplierRecovery = "dw-fact-005-supplier-recovery";
    public const string MoldLifecycle = "dw-fact-006-mold-lifecycle";
    public const string Coordinator = "dw-fact-007-coordinator";
    public const string ThroughputOptimizer = "dw-fact-008-throughput-optimizer";
}

public static class DigitalWorkforceVerificationEngine
{
    public static string ResolveClosureContractKey(string workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.ProductionRecovery => DigitalWorkforceClosureContracts.ProductionRecovery,
        DigitalWorkforceWorkerCodes.MaintenanceResolution => DigitalWorkforceClosureContracts.MaintenanceResolution,
        DigitalWorkforceWorkerCodes.MaterialShortage => DigitalWorkforceClosureContracts.MaterialShortage,
        DigitalWorkforceWorkerCodes.QualityNcr => DigitalWorkforceClosureContracts.QualityNcr,
        DigitalWorkforceWorkerCodes.SupplierRecovery => DigitalWorkforceClosureContracts.SupplierRecovery,
        DigitalWorkforceWorkerCodes.MoldLifecycle => DigitalWorkforceClosureContracts.MoldLifecycle,
        DigitalWorkforceWorkerCodes.FactoryCoordinator => DigitalWorkforceClosureContracts.Coordinator,
        DigitalWorkforceWorkerCodes.ThroughputOptimizer => DigitalWorkforceClosureContracts.ThroughputOptimizer,
        _ => "unknown",
    };

    public static bool CanCloseCase(DigitalWorkCase workCase, IEnumerable<DigitalWorkVerification> verifications)
    {
        if (!string.Equals(workCase.VerificationState, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))
        {
            var required = GetRequiredVerificationTypes(workCase.ClosureContractKey);
            var passed = verifications
                .Where(v => string.Equals(v.Result, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase))
                .Select(v => v.VerificationType)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!required.All(passed.Contains))
            {
                return false;
            }
        }

        return workCase.ClosureContractKey switch
        {
            DigitalWorkforceClosureContracts.Coordinator => EvaluateCoordinatorClosure(workCase),
            _ => verifications.Any(v =>
                string.Equals(v.Result, DigitalWorkVerificationResults.Passed, StringComparison.OrdinalIgnoreCase)),
        };
    }

    public static IReadOnlyList<string> GetRequiredVerificationTypes(string closureContractKey) => closureContractKey switch
    {
        DigitalWorkforceClosureContracts.ProductionRecovery => ["production-plan-validated"],
        DigitalWorkforceClosureContracts.MaintenanceResolution => ["machine-operational"],
        DigitalWorkforceClosureContracts.MaterialShortage => ["material-available"],
        DigitalWorkforceClosureContracts.QualityNcr => ["quality-outcome-verified"],
        DigitalWorkforceClosureContracts.SupplierRecovery => ["supply-risk-resolved"],
        DigitalWorkforceClosureContracts.MoldLifecycle => ["mold-production-ready"],
        DigitalWorkforceClosureContracts.Coordinator => ["coordinator-subcases-resolved"],
        DigitalWorkforceClosureContracts.ThroughputOptimizer => ["throughput-target-met"],
        _ => ["outcome-verified"],
    };

    private static bool EvaluateCoordinatorClosure(DigitalWorkCase workCase)
    {
        if (string.IsNullOrWhiteSpace(workCase.MetadataJson))
        {
            return false;
        }

        return workCase.MetadataJson.Contains("\"allSubcasesResolved\":true", StringComparison.OrdinalIgnoreCase) ||
               workCase.MetadataJson.Contains("\"allSubcasesResolved\": true", StringComparison.OrdinalIgnoreCase);
    }
}
