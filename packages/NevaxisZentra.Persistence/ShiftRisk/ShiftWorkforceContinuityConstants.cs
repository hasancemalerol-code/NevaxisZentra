namespace NevaxisZentra.Persistence.ShiftRisk;

public static class ShiftWorkforceContinuityConstants
{
    public const string RuleSetVersion = "SHIFT-WORKFORCE-CONTINUITY-RISK-V1";
    public const string AssessmentType = ShiftRiskAssessmentTypes.WorkforceContinuity;
    public const int UrgentExpiryDays = 14;
    public const int UpcomingExpiryDays = 30;
}

public static class ShiftWorkforceContinuityDriverCategories
{
    public const string AbsenceImpact = "absence-impact";
    public const string AvailabilityUnknown = "availability-unknown";
    public const string CompetencyExpired = "competency-expired";
    public const string CompetencyExpiring = "competency-expiring";
    public const string CompetencyPendingVerification = "competency-pending-verification";
    public const string SourceStale = "source-stale";
    public const string AssignmentOverlap = "assignment-overlap";
    public const string AssignmentLoadPressure = "assignment-load-pressure";
    public const string CoverageImpact = "coverage-impact";
    public const string EvidenceConflict = "evidence-conflict";
}

public static class ShiftWorkforceContinuityReasonCodes
{
    public const string ContinuityClear = "continuity-clear";
    public const string AbsenceImpact = "absence-impact";
    public const string AvailabilityUnknown = "availability-unknown";
    public const string CompetencyExpired = "competency-expired";
    public const string CompetencyExpiringUrgent = "competency-expiring-urgent";
    public const string CompetencyExpiringUpcoming = "competency-expiring-upcoming";
    public const string CompetencyPendingVerification = "competency-pending-verification";
    public const string SourceStale = "source-stale";
    public const string AssignmentOverlap = "assignment-overlap";
    public const string AssignmentLoadFacts = "assignment-load-facts";
    public const string AssignmentLoadThresholdMissing = "assignment-load-threshold-missing";
    public const string CoverageImpact = "coverage-impact";
    public const string EvidenceConflict = "evidence-conflict";
    public const string InactiveEmployment = "inactive-employment";
}

public static class ShiftWorkforceContinuityReasonLabels
{
    public static string GetLabelTr(string reasonCode) =>
        reasonCode switch
        {
            ShiftWorkforceContinuityReasonCodes.ContinuityClear => "Süreklilik kanıtı yeterli",
            ShiftWorkforceContinuityReasonCodes.AbsenceImpact => "İzin/yokluk etkisi",
            ShiftWorkforceContinuityReasonCodes.AvailabilityUnknown => "Müsaitlik bilinmiyor",
            ShiftWorkforceContinuityReasonCodes.CompetencyExpired => "Süresi dolmuş yetkinlik",
            ShiftWorkforceContinuityReasonCodes.CompetencyExpiringUrgent => "Yakında süresi dolacak yetkinlik (14 gün)",
            ShiftWorkforceContinuityReasonCodes.CompetencyExpiringUpcoming => "Yaklaşan yetkinlik süresi (30 gün)",
            ShiftWorkforceContinuityReasonCodes.CompetencyPendingVerification => "Doğrulama bekleyen yetkinlik",
            ShiftWorkforceContinuityReasonCodes.SourceStale => "Bayat kaynak verisi",
            ShiftWorkforceContinuityReasonCodes.AssignmentOverlap => "Atama çakışması",
            ShiftWorkforceContinuityReasonCodes.AssignmentLoadFacts => "Planlı atama yükü",
            ShiftWorkforceContinuityReasonCodes.AssignmentLoadThresholdMissing => "Yetkili kapasite eşiği yok",
            ShiftWorkforceContinuityReasonCodes.CoverageImpact => "Kapsama etkisi",
            ShiftWorkforceContinuityReasonCodes.EvidenceConflict => "Çelişkili kanıt",
            ShiftWorkforceContinuityReasonCodes.InactiveEmployment => "Pasif istihdam",
            _ => "Personel sürekliliği risk nedeni",
        };
}

public static class ShiftWorkforceContinuityLeadingRiskPriority
{
    public static readonly string[] CategoryOrder =
    [
        ShiftWorkforceContinuityDriverCategories.CompetencyExpired,
        ShiftWorkforceContinuityDriverCategories.AbsenceImpact,
        ShiftWorkforceContinuityDriverCategories.AssignmentOverlap,
        ShiftWorkforceContinuityDriverCategories.CoverageImpact,
        ShiftWorkforceContinuityDriverCategories.CompetencyExpiring,
        ShiftWorkforceContinuityDriverCategories.CompetencyPendingVerification,
        ShiftWorkforceContinuityDriverCategories.AssignmentLoadPressure,
        ShiftWorkforceContinuityDriverCategories.EvidenceConflict,
        ShiftWorkforceContinuityDriverCategories.SourceStale,
        ShiftWorkforceContinuityDriverCategories.AvailabilityUnknown,
    ];
}
