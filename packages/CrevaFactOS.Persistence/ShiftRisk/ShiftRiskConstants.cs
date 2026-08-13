namespace CrevaFactOS.Persistence.ShiftRisk;

public static class ShiftRiskConstants
{
    public const string RuleSetVersion = "SHIFT-COVERAGE-RISK-V1";
    public const string ContinuityRuleSetVersion = "SHIFT-WORKFORCE-CONTINUITY-RISK-V1";
    public const int MaxMinimumOperatorCount = 50;
}

public static class ShiftRiskAssessmentTypes
{
    public const string Coverage = "coverage";
    public const string WorkforceContinuity = "workforce-continuity";
}

public static class ShiftRiskOutcomes
{
    public const string Clear = "clear";
    public const string Attention = "attention";
    public const string Critical = "critical";
    public const string InsufficientData = "insufficient-data";
}

public static class ShiftRiskDriverCategories
{
    public const string CoverageGap = "coverage-gap";
    public const string CriticalRoleGap = "critical-role-gap";
    public const string AssignmentConflict = "assignment-conflict";
    public const string AssignmentIneligible = "assignment-ineligible";
    public const string RequirementMissing = "requirement-missing";
    public const string SourceStale = "source-stale";
    public const string EvidenceConflict = "evidence-conflict";
    public const string AbsenceImpact = "absence-impact";
    public const string AvailabilityUnknown = "availability-unknown";
    public const string CompetencyExpired = "competency-expired";
    public const string CompetencyExpiring = "competency-expiring";
    public const string CompetencyPendingVerification = "competency-pending-verification";
    public const string AssignmentOverlap = "assignment-overlap";
    public const string AssignmentLoadPressure = "assignment-load-pressure";
    public const string CoverageImpact = "coverage-impact";
}

public static class ShiftRiskDriverSeverities
{
    public const string Pass = "pass";
    public const string Warn = "warn";
    public const string Fail = "fail";
    public const string Info = "info";
}

public static class ShiftRiskReasonCodes
{
    public const string CoverageSatisfied = "coverage-satisfied";
    public const string CoverageGap = "coverage-gap";
    public const string CriticalRoleGap = "critical-role-gap";
    public const string AssignmentConflict = "assignment-conflict";
    public const string AssignmentIneligible = "assignment-ineligible";
    public const string AssignmentExcludedDraft = "assignment-excluded-draft";
    public const string AssignmentExcludedCancelled = "assignment-excluded-cancelled";
    public const string AssignmentExcludedWindow = "assignment-excluded-window";
    public const string AssignmentExcludedDuplicatePerson = "assignment-excluded-duplicate-person";
    public const string RequirementMissing = "requirement-missing";
    public const string SourceStale = "source-stale";
    public const string EvidenceConflict = "evidence-conflict";
    public const string CompetencyMismatch = "competency-mismatch";
    public const string EligibilityInsufficient = "eligibility-insufficient";
    public const string AbsenceImpact = "absence-impact";
    public const string AvailabilityUnknown = "availability-unknown";
    public const string CompetencyExpired = "competency-expired";
    public const string CompetencyExpiringUrgent = "competency-expiring-urgent";
    public const string CompetencyExpiringUpcoming = "competency-expiring-upcoming";
    public const string CompetencyPendingVerification = "competency-pending-verification";
    public const string AssignmentOverlap = "assignment-overlap";
    public const string AssignmentLoadFacts = "assignment-load-facts";
    public const string AssignmentLoadThresholdMissing = "assignment-load-threshold-missing";
    public const string CoverageImpact = "coverage-impact";
    public const string ContinuityClear = "continuity-clear";
    public const string EmploymentInactive = "employment-inactive";
}

public static class ShiftRiskEvidenceSources
{
    public const string CoverageRequirement = "shift-coverage-requirement";
    public const string AssignmentRegistry = "assignment-registry";
    public const string ShiftWorkforce = "shift-workforce";
    public const string EligibilityEngine = "eligibility-engine";
    public const string RiskEngine = "shift-risk-engine";
    public const string LocalDevelopmentPilot = "local-development-pilot";
}

public static class ShiftRiskAuditEventTypes
{
    public const string RequirementCreated = "shift-risk.requirement.created";
    public const string RequirementUpdated = "shift-risk.requirement.updated";
    public const string AssessmentEvaluated = "shift-risk.assessment.evaluated";
}

public static class ShiftRiskEntityTypes
{
    public const string CoverageRequirement = "shift-coverage-requirement";
    public const string Assessment = "shift-risk-assessment";
}

public static class ShiftRiskReasonLabels
{
    public static string GetLabelTr(string reasonCode) =>
        reasonCode switch
        {
            ShiftRiskReasonCodes.CoverageSatisfied => "Kapsama karşılandı",
            ShiftRiskReasonCodes.CoverageGap => "Operatör kapsama açığı",
            ShiftRiskReasonCodes.CriticalRoleGap => "Kritik rol kapsama açığı",
            ShiftRiskReasonCodes.AssignmentConflict => "Atama çakışması",
            ShiftRiskReasonCodes.AssignmentIneligible => "Atama uygun değil",
            ShiftRiskReasonCodes.AssignmentExcludedDraft => "Taslak atama sayılmadı",
            ShiftRiskReasonCodes.AssignmentExcludedCancelled => "İptal edilen atama sayılmadı",
            ShiftRiskReasonCodes.AssignmentExcludedWindow => "Zaman penceresi dışı atama sayılmadı",
            ShiftRiskReasonCodes.AssignmentExcludedDuplicatePerson => "Aynı personel tekrar sayılmadı",
            ShiftRiskReasonCodes.RequirementMissing => "Açık gereksinim bulunamadı",
            ShiftRiskReasonCodes.SourceStale => "Bayat kaynak verisi",
            ShiftRiskReasonCodes.EvidenceConflict => "Çelişkili kanıt",
            ShiftRiskReasonCodes.CompetencyMismatch => "Yetkinlik uyuşmazlığı",
            ShiftRiskReasonCodes.EligibilityInsufficient => "Uygunluk verisi yetersiz",
            ShiftRiskReasonCodes.AbsenceImpact => "İzin/yokluk etkisi",
            ShiftRiskReasonCodes.AvailabilityUnknown => "Müsaitlik bilinmiyor",
            ShiftRiskReasonCodes.CompetencyExpired => "Yetkinlik süresi dolmuş",
            ShiftRiskReasonCodes.CompetencyExpiringUrgent => "Yetkinlik süresi 14 gün içinde doluyor",
            ShiftRiskReasonCodes.CompetencyExpiringUpcoming => "Yetkinlik süresi 30 gün içinde doluyor",
            ShiftRiskReasonCodes.CompetencyPendingVerification => "Yetkinlik doğrulaması bekliyor",
            ShiftRiskReasonCodes.AssignmentOverlap => "Onaylı atama çakışması",
            ShiftRiskReasonCodes.AssignmentLoadFacts => "Planlı atama yükü",
            ShiftRiskReasonCodes.AssignmentLoadThresholdMissing => "Yetkili kapasite eşiği yok",
            ShiftRiskReasonCodes.CoverageImpact => "Kapsama etkisi",
            ShiftRiskReasonCodes.ContinuityClear => "Süreklilik kanıtı güncel",
            ShiftRiskReasonCodes.EmploymentInactive => "Personel pasif",
            _ => "Risk değerlendirme nedeni",
        };
}

public static class ShiftWorkforceContinuityRiskPriority
{
    public static readonly string[] LeadingCategoryOrder =
    [
        ShiftRiskDriverCategories.CompetencyExpired,
        ShiftRiskDriverCategories.AbsenceImpact,
        ShiftRiskDriverCategories.AssignmentOverlap,
        ShiftRiskDriverCategories.CoverageImpact,
        ShiftRiskDriverCategories.CompetencyExpiring,
        ShiftRiskDriverCategories.CompetencyPendingVerification,
        ShiftRiskDriverCategories.AssignmentLoadPressure,
        ShiftRiskDriverCategories.EvidenceConflict,
        ShiftRiskDriverCategories.SourceStale,
        ShiftRiskDriverCategories.AvailabilityUnknown,
    ];
}
