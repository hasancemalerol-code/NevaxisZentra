namespace NevaxisFactOS.Persistence.FactoryDecisionCenter;

public static class FactoryDecisionCenterConstants
{
    public const string SourceModule = "factory-decision-center";
    public const string ScenarioCriticalOrderRecovery = "critical-order-recovery";
    public const string OrderReference = "ORD-2026-0915-047";
    public const string CustomerName = "Atlas Automotive";
    public const string MachineCode = "INJ-001";
    public const string MoldCode = "MOLD-PP-240";
    public const string MaterialCode = "PP granül";
    public const string ProductCode = "PP-KAPAK-240";
    public const int RequiredQuantity = 480;
    public const int InitialProjectedQuantity = 412;
    public const int InitialGapQuantity = 68;
    public const int RecoveredProjectedQuantity = 493;
    public const double CycleTimeReferenceSec = 28.0;
    public const double CycleTimeCurrentSec = 31.2;
    public const int MicroStopCount = 8;
    public const int MicroStopDurationSec = 120;
    public const string CorrelationKey = "order:ord-2026-0915-047:facility";
}

public static class FactoryDecisionCenterMetadataKeys
{
    public const string ScenarioKey = "scenarioKey";
    public const string OrderReference = "orderReference";
    public const string CustomerName = "customerName";
    public const string RequiredQuantity = "requiredQuantity";
    public const string ProjectedQuantity = "projectedQuantity";
    public const string GapQuantity = "gapQuantity";
    public const string DeliveryCommitmentUtc = "deliveryCommitmentUtc";
    public const string MachineCode = "machineCode";
    public const string MoldCode = "moldCode";
    public const string MaterialCode = "materialCode";
    public const string PrimaryCause = "primaryCause";
    public const string ContributingFactors = "contributingFactors";
    public const string Confidence = "confidence";
    public const string RecommendedStrategy = "recommendedStrategy";
    public const string AlternativeStrategy = "alternativeStrategy";
    public const string VerifiedProjection = "verifiedProjection";
    public const string DeliveryProtected = "deliveryProtected";
    public const string RecoveryGain = "recoveryGain";
}
