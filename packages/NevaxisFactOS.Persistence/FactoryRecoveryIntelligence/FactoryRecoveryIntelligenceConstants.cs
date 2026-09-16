namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public static class FactoryRecoveryIntelligenceConstants
{
    public const string LogicVersion = "recovery-intelligence-v1";
}

public static class FactoryRecoveryEvidenceStrength
{
    public const string Low = "DUSUK_KANIT";
    public const string Medium = "ORTA_KANIT";
    public const string Strong = "GUC_LU_KANIT";
}

public static class FactoryRecoveryLearningDisposition
{
    public const string VerifiedSuccess = "verified_success";
    public const string VerificationFailed = "verification_failed";
    public const string PlanRejected = "plan_rejected";
    public const string ExecutionFailed = "execution_failed";
    public const string BlockerUnresolved = "blocker_unresolved";
    public const string AlternatePlanRequired = "alternate_plan_required";
}

public static class FactoryRecoverySampleConfidence
{
    public const string Low = "DUSUK";
    public const string Medium = "ORTA";
    public const string Strong = "GUC_LU";
}
