namespace CrevaFactOS.Persistence.ProcessOee;

public static class ProcessOeeConstants
{
    public const string RuleSetVersion = "PROCESS-OEE-V1";
    public const int MaxWindowDurationSeconds = 604_800;
    public const int PercentDecimalPlaces = 2;
    public const string CalculationHonestyDisclaimer =
        "Hesaplama mevcut operasyon verilerine dayanır; gerçek saha doğrulaması tamamlanmamıştır.";
    public const string SimulatorHonestyDisclaimer =
        "Bu sonuç simülatör verisine dayanır; gerçek makine doğrulaması değildir.";
}

public static class ProcessOeeFailureCodes
{
    public const string ValidationFailed = "process-oee.validation-failed";
    public const string NotFound = "process-oee.not-found";
    public const string ConcurrencyConflict = "process-oee.concurrency-conflict";
    public const string IdempotencyConflict = "process-oee.window.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "process-oee.window.idempotency-legacy-unverifiable";
    public const string AssessmentIdempotencyConflict = "process-oee.assessment.idempotency-conflict";
    public const string AssessmentLegacyUnverifiable = "process-oee.assessment.idempotency-legacy-unverifiable";
    public const string InsufficientData = "process-oee.assessment.insufficient-data";
    public const string EvidenceConflict = "process-oee.assessment.evidence-conflict";
}

public static class ProcessOeeAssessmentStatuses
{
    public const string Calculated = "calculated";
    public const string InsufficientData = "insufficient-data";
    public const string ReviewRequired = "review-required";
    public const string StaleData = "stale-data";
}

public static class ProcessOeeSourceTypes
{
    public const string Manual = "manual";
    public const string Imported = "imported";
    public const string Erp = "erp";
    public const string DevelopmentPilot = "development-pilot";
    public const string Unknown = "unknown";
}
