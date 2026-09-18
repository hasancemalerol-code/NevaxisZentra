namespace NevaxisZentra.Persistence.Material;

public static class MaterialRequirementValueConversions
{
    public static string ToStorage(MaterialRequirementType value) => value switch
    {
        MaterialRequirementType.ProductionOrder => "production-order",
        MaterialRequirementType.Trial => "trial",
        MaterialRequirementType.Setup => "setup",
        MaterialRequirementType.ManualPlanning => "manual-planning",
        MaterialRequirementType.Other => "other",
        _ => "other",
    };

    public static MaterialRequirementType RequirementTypeFromStorage(string value) => value switch
    {
        "production-order" => MaterialRequirementType.ProductionOrder,
        "trial" => MaterialRequirementType.Trial,
        "setup" => MaterialRequirementType.Setup,
        "manual-planning" => MaterialRequirementType.ManualPlanning,
        _ => MaterialRequirementType.Other,
    };

    public static string ToStorage(MaterialConsumptionObservationType value) => value switch
    {
        MaterialConsumptionObservationType.ConsumptionObserved => "consumption-observed",
        MaterialConsumptionObservationType.SetupConsumptionObserved => "setup-consumption-observed",
        MaterialConsumptionObservationType.TrialConsumptionObserved => "trial-consumption-observed",
        MaterialConsumptionObservationType.ScrapObserved => "scrap-observed",
        MaterialConsumptionObservationType.RegrindUseObserved => "regrind-use-observed",
        MaterialConsumptionObservationType.CorrectionObserved => "correction-observed",
        _ => "consumption-observed",
    };

    public static MaterialConsumptionObservationType ObservationTypeFromStorage(string value) => value switch
    {
        "consumption-observed" => MaterialConsumptionObservationType.ConsumptionObserved,
        "setup-consumption-observed" => MaterialConsumptionObservationType.SetupConsumptionObserved,
        "trial-consumption-observed" => MaterialConsumptionObservationType.TrialConsumptionObserved,
        "scrap-observed" => MaterialConsumptionObservationType.ScrapObserved,
        "regrind-use-observed" => MaterialConsumptionObservationType.RegrindUseObserved,
        "correction-observed" => MaterialConsumptionObservationType.CorrectionObserved,
        _ => MaterialConsumptionObservationType.ConsumptionObserved,
    };

    public static string ToStorage(MaterialRequirementAssessmentType value) => value switch
    {
        MaterialRequirementAssessmentType.Requirement => "requirement",
        MaterialRequirementAssessmentType.ConsumptionVariance => "consumption-variance",
        MaterialRequirementAssessmentType.RegrindPolicy => "regrind-policy",
        MaterialRequirementAssessmentType.AvailabilityRisk => "availability-risk",
        _ => "requirement",
    };

    public static MaterialRequirementAssessmentType AssessmentTypeFromStorage(string value) => value switch
    {
        "requirement" => MaterialRequirementAssessmentType.Requirement,
        "consumption-variance" => MaterialRequirementAssessmentType.ConsumptionVariance,
        "regrind-policy" => MaterialRequirementAssessmentType.RegrindPolicy,
        "availability-risk" => MaterialRequirementAssessmentType.AvailabilityRisk,
        _ => MaterialRequirementAssessmentType.Requirement,
    };
}
