namespace CrevaFactOS.Persistence.ShiftRisk;

public static class ShiftRiskConstants
{
    public const string RuleSetVersion = "SHIFT-COVERAGE-RISK-V1";
    public const int MaxMinimumOperatorCount = 50;
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
            _ => "Risk değerlendirme nedeni",
        };
}
