namespace NevaxisZentra.Persistence.ShiftAssignment;

public static class ShiftAssignmentConstants
{
    public const string RuleSetVersion = "SHIFT-OPERATOR-ELIGIBILITY-V1";
}

public static class ShiftAssignmentEligibilityOutcomes
{
    public const string Eligible = "eligible";
    public const string Conditional = "conditional";
    public const string Ineligible = "ineligible";
    public const string InsufficientData = "insufficient-data";
}

public static class ShiftAssignmentStatuses
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string Cancelled = "cancelled";
}

public static class ShiftAssignmentReasonSeverities
{
    public const string Pass = "pass";
    public const string Warn = "warn";
    public const string Fail = "fail";
    public const string Info = "info";
}

public static class ShiftAssignmentReasonCodes
{
    public const string EmploymentInactive = "employment-inactive";
    public const string AvailabilityLeave = "availability-leave";
    public const string AvailabilityAbsent = "availability-absent";
    public const string AvailabilityUnknown = "availability-unknown";
    public const string SourceStale = "source-stale";
    public const string SourceUnknown = "source-unknown";
    public const string CompetencyMissing = "competency-missing";
    public const string CompetencyPendingVerification = "competency-pending-verification";
    public const string CompetencyExpiring = "competency-expiring";
    public const string CompetencyExpired = "competency-expired";
    public const string ShiftReferenceMismatch = "shift-reference-mismatch";
    public const string AssignmentOverlap = "assignment-overlap";
    public const string FacilityScopeMismatch = "facility-scope-mismatch";
    public const string RequiredDataMissing = "required-data-missing";
    public const string AllCriteriaMet = "all-criteria-met";
}

public static class ShiftAssignmentEvidenceSources
{
    public const string ShiftWorkforce = "shift-workforce";
    public const string CompetencyEnrichment = "competency-enrichment";
    public const string AssignmentRegistry = "assignment-registry";
    public const string EligibilityEngine = "eligibility-engine";
    public const string LocalDevelopmentPilot = "local-development-pilot";
}

public static class ShiftAssignmentAuditEventTypes
{
    public const string AssignmentCreated = "assignment-created";
    public const string AssignmentApproved = "assignment-approved";
    public const string AssignmentCancelled = "assignment-cancelled";
}

public static class ShiftAssignmentEntityTypes
{
    public const string Assignment = "shift-operator-assignment";
}

public static class ShiftAssignmentReasonLabels
{
    public static string GetLabelTr(string reasonCode) =>
      reasonCode switch
      {
          ShiftAssignmentReasonCodes.EmploymentInactive => "Personel pasif",
          ShiftAssignmentReasonCodes.AvailabilityLeave => "İzinli",
          ShiftAssignmentReasonCodes.AvailabilityAbsent => "Devamsız",
          ShiftAssignmentReasonCodes.AvailabilityUnknown => "Müsaitlik bilinmiyor",
          ShiftAssignmentReasonCodes.SourceStale => "Bayat kaynak verisi",
          ShiftAssignmentReasonCodes.SourceUnknown => "Kaynak tazeliği bilinmiyor",
          ShiftAssignmentReasonCodes.CompetencyMissing => "Gerekli yetkinlik eksik",
          ShiftAssignmentReasonCodes.CompetencyPendingVerification => "Yetkinlik doğrulaması bekliyor",
          ShiftAssignmentReasonCodes.CompetencyExpiring => "Yetkinlik süresi yakında doluyor",
          ShiftAssignmentReasonCodes.CompetencyExpired => "Yetkinlik süresi dolmuş",
          ShiftAssignmentReasonCodes.ShiftReferenceMismatch => "Vardiya referansı uyuşmuyor",
          ShiftAssignmentReasonCodes.AssignmentOverlap => "Çakışan onaylı atama",
          ShiftAssignmentReasonCodes.FacilityScopeMismatch => "Tesis kapsamı dışında",
          ShiftAssignmentReasonCodes.RequiredDataMissing => "Gerekli veri eksik",
          ShiftAssignmentReasonCodes.AllCriteriaMet => "Tüm kriterler karşılandı",
          _ => "Değerlendirme nedeni",
      };
}
