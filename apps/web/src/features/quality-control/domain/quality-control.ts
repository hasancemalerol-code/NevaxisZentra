export type QualityControlPlanSummary = Readonly<{
  id: string;
  code: string;
  name: string;
  status: string;
  revision: string;
  productReference?: string;
  operationReference?: string;
  definitionCount: number;
  version: number;
  updatedAtUtc: string;
}>;

export type QualityInspectionDefinition = Readonly<{
  id: string;
  code: string;
  name: string;
  description?: string;
  inspectionType: string;
  sequence: number;
  unit?: string;
  nominalValue?: number;
  lowerLimit?: number;
  upperLimit?: number;
  required: boolean;
  critical: boolean;
  instruction?: string;
  version: number;
}>;

export type QualityControlPlanDetail = Readonly<{
  id: string;
  code: string;
  name: string;
  description?: string;
  status: string;
  revision: string;
  productReference?: string;
  productGroup?: string;
  operationReference?: string;
  version: number;
  inspectionDefinitions: readonly QualityInspectionDefinition[];
}>;

export type QualityInspectionSummary = Readonly<{
  id: string;
  inspectionNumber: string;
  status: string;
  overallResult: string;
  releaseStatus: string;
  productionOrderId: string;
  productionOrderReference?: string;
  productReference?: string;
  qualityControlPlanId: string;
  planCode?: string;
  version: number;
  updatedAtUtc: string;
}>;

export type ProductionOrderQualityContext = Readonly<{
  productionOrderId: string;
  referenceCode: string;
  productReference?: string;
  status: string;
  plannedQuantity?: number;
  goodQuantity: number;
  scrapQuantity: number;
  available: boolean;
}>;

export type QualityInspectionResultRow = Readonly<{
  id: string;
  inspectionDefinitionId: string;
  definitionCode: string;
  definitionName: string;
  inspectionType: string;
  unit?: string;
  nominalValue?: number;
  lowerLimit?: number;
  upperLimit?: number;
  required: boolean;
  critical: boolean;
  measuredNumericValue?: number;
  measuredTextValue?: string;
  booleanResult?: boolean;
  result: string;
  comment?: string;
}>;

export type LinkedNonconformanceSummary = Readonly<{
  id: string;
  ncrNumber: string;
  status: string;
}>;

export type QualityInspectionDetail = Readonly<{
  id: string;
  inspectionNumber: string;
  status: string;
  overallResult: string;
  releaseStatus: string;
  productionOrderId: string;
  productionContext: ProductionOrderQualityContext;
  qualityControlPlanId: string;
  planCode: string;
  planName: string;
  inspectorSubjectId?: string;
  notes?: string;
  releaseNote?: string;
  version: number;
  results: readonly QualityInspectionResultRow[];
  linkedNonconformances: readonly LinkedNonconformanceSummary[];
}>;

export type QualityWorkspaceSummary = Readonly<{
  openInspectionCount: number;
  pendingReviewCount: number;
  failedInspectionCount: number;
  releasedInspectionCount: number;
  openNcrCount: number;
  activeHoldCount: number;
  actionPendingCount: number;
}>;

export type QualityNcrSummary = Readonly<{
  id: string;
  ncrNumber: string;
  title: string;
  severity: string;
  status: string;
  holdStatus: string;
  dispositionStatus: string;
  dispositionType?: string;
  productionOrderId?: string;
  productionOrderReference?: string;
  productReference?: string;
  ownerSubjectId?: string;
  detectedAtUtc: string;
  createdAtUtc: string;
  version: number;
  updatedAtUtc: string;
}>;

export type QualityHoldDetail = Readonly<{
  id: string;
  scope: string;
  status: string;
  reason: string;
  placedAtUtc: string;
  placedBySubjectId: string;
  releasedAtUtc?: string;
  releasedBySubjectId?: string;
  releaseReason?: string;
  version: number;
}>;

export type QualityCorrectiveActionDetail = Readonly<{
  id: string;
  title: string;
  description: string;
  status: string;
  ownerSubjectId?: string;
  dueAtUtc?: string;
  completedAtUtc?: string;
  completionNote?: string;
  version: number;
  updatedAtUtc: string;
}>;

export type QualityNcrDetail = Readonly<{
  id: string;
  ncrNumber: string;
  title: string;
  description: string;
  category?: string;
  severity: string;
  status: string;
  holdStatus: string;
  dispositionStatus: string;
  dispositionType?: string;
  dispositionNote?: string;
  dispositionDecidedBySubjectId?: string;
  dispositionDecidedAtUtc?: string;
  productionOrderId?: string;
  productionContext?: ProductionOrderQualityContext;
  qualityInspectionId?: string;
  qualityInspectionNumber?: string;
  qualityInspectionResultId?: string;
  productReference?: string;
  operationReference?: string;
  detectedBySubjectId: string;
  ownerSubjectId?: string;
  detectedAtUtc: string;
  closedAtUtc?: string;
  closedBySubjectId?: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  activeHold?: QualityHoldDetail;
  correctiveActions: readonly QualityCorrectiveActionDetail[];
}>;

export type ProductionQualitySignal = Readonly<{
  available: boolean;
  qualityHoldActive: boolean;
  blocking: boolean;
  openNcrCount: number;
  criticalNcrCount: number;
  releaseStatus?: string;
  reason?: string;
}>;

export function getQualityStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "active":
      return "Aktif";
    case "in-progress":
      return "Devam ediyor";
    case "completed":
      return "Tamamlandı";
    case "ready-for-review":
      return "İnceleme bekliyor";
    case "released":
      return "Serbest bırakıldı";
    case "rejected":
      return "Reddedildi";
    case "pass":
      return "Geçti";
    case "fail":
      return "Başarısız";
    case "pending":
      return "Bekliyor";
    case "open":
      return "Açık";
    case "under-review":
      return "İncelemede";
    case "action-required":
      return "Aksiyon gerekli";
    case "resolved":
      return "Çözüldü";
    case "closed":
      return "Kapatıldı";
    case "cancelled":
      return "İptal";
    case "minor":
      return "Minör";
    case "major":
      return "Majör";
    case "critical":
      return "Kritik";
    case "none":
      return "Yok";
    case "active":
      return "Aktif";
    case "released":
      return "Serbest";
    case "decided":
      return "Karar verildi";
    case "use-as-is":
      return "Olduğu gibi kullan";
    case "rework":
      return "Yeniden işle";
    case "scrap":
      return "Hurda";
    case "return-to-supplier":
      return "Tedarikçiye iade";
    case "sort-or-screen":
      return "Ayıkla / ele";
    default:
      return status;
  }
}

export function getQualityNcrSeverityTone(
  severity: string,
): "neutral" | "information" | "danger" | "success" {
  switch (severity) {
    case "critical":
      return "danger";
    case "major":
      return "information";
    default:
      return "neutral";
  }
}

export type QualityReportingOverview = Readonly<{
  totalInspectionCount: number;
  openInspectionCount: number;
  completedInspectionCount: number;
  passedInspectionCount: number;
  failedInspectionCount: number;
  releasedInspectionCount: number;
  rejectedInspectionCount: number;
  openNcrCount: number;
  criticalNcrCount: number;
  activeHoldCount: number;
  blockingProductionOrderCount: number;
  openCorrectiveActionCount: number;
  overdueCorrectiveActionCount: number;
  firstPassYieldPercent?: number;
  passRatePercent?: number;
  releaseRatePercent?: number;
}>;

export type QualityTrendPeriod = Readonly<{
  periodKey: string;
  periodStartUtc: string;
  inspectionCount: number;
  passCount: number;
  failCount: number;
  ncrCount: number;
  criticalNcrCount: number;
  holdCount: number;
}>;

export type QualityFailureBreakdownItem = Readonly<{
  dimension: string;
  key: string;
  label?: string;
  count: number;
}>;

export type QualityNcrAnalytics = Readonly<{
  openCount: number;
  resolvedCount: number;
  closedCount: number;
  criticalCount: number;
  majorCount: number;
  minorCount: number;
  averageAgeHours?: number;
  averageResolutionHours?: number;
  overdueCorrectiveActionCount: number;
}>;

export type QualityHoldAnalytics = Readonly<{
  activeCount: number;
  releasedCount: number;
  affectedProductionOrderCount: number;
  averageHoldDurationHours?: number;
}>;

export type QualityCorrectiveActionAnalytics = Readonly<{
  openCount: number;
  inProgressCount: number;
  completedCount: number;
  overdueCount: number;
  averageCompletionLeadTimeHours?: number;
}>;

export type QualityInspectionReportMeasurement = Readonly<{
  definitionCode: string;
  definitionName: string;
  unit?: string;
  nominalValue?: number;
  lowerLimit?: number;
  upperLimit?: number;
  required: boolean;
  critical: boolean;
  measuredNumericValue?: number;
  measuredTextValue?: string;
  booleanResult?: boolean;
  result: string;
}>;

export type QualityInspectionInternalSummary = Readonly<{
  inspectionId: string;
  inspectionNumber: string;
  facilityReference: string;
  productionOrderId: string;
  productionOrderReference?: string;
  productReference?: string;
  controlPlanId: string;
  controlPlanCode: string;
  controlPlanName: string;
  controlPlanRevision: string;
  inspectionDateUtc?: string;
  inspectorSubjectId?: string;
  overallResult: string;
  releaseStatus: string;
  measurements: readonly QualityInspectionReportMeasurement[];
  hasBlockingHold: boolean;
  openNcrCount: number;
  blockingSummary?: string;
}>;

export type QualityReleaseIntelligence = Readonly<{
  available: boolean;
  inspectionStatus?: string;
  overallResult?: string;
  releaseStatus?: string;
  blocking: boolean;
  activeHoldCount: number;
  openNcrCount: number;
  criticalNcrCount: number;
  incompleteCorrectiveActionCount: number;
  readyForRelease: boolean;
  reasonCodes: readonly string[];
  reasonSummary?: string;
}>;

export type QualityExportReadiness = Readonly<{
  qualityReleaseStatus?: string;
  qualityBlocking: boolean;
  qualityReady: boolean;
  reason?: string;
}>;

export type QualityResultTraceabilityItem = Readonly<{
  id: string;
  definitionCode: string;
  definitionName: string;
  result: string;
  recordedAtUtc: string;
  recordedBySubjectId: string;
}>;

export type QualityInspectionTraceabilityItem = Readonly<{
  id: string;
  inspectionNumber: string;
  status: string;
  overallResult: string;
  releaseStatus: string;
  completedAtUtc?: string;
  inspectorSubjectId?: string;
  results: readonly QualityResultTraceabilityItem[];
}>;

export type QualityNcrTraceabilityItem = Readonly<{
  id: string;
  ncrNumber: string;
  severity: string;
  status: string;
  dispositionType?: string;
  holdStatus: string;
  detectedAtUtc: string;
  ownerSubjectId?: string;
}>;

export type QualityHoldTraceabilityItem = Readonly<{
  id: string;
  ncrId: string;
  scope: string;
  status: string;
  reason: string;
  placedAtUtc: string;
  placedBySubjectId: string;
  releasedAtUtc?: string;
  releasedBySubjectId?: string;
}>;

export type QualityCorrectiveActionTraceabilityItem = Readonly<{
  id: string;
  ncrId: string;
  title: string;
  status: string;
  dueAtUtc?: string;
  completedAtUtc?: string;
  ownerSubjectId?: string;
}>;

export type ProductionOrderTraceability = Readonly<{
  productionOrderId: string;
  productionOrderReference?: string;
  productReference?: string;
  inspections: readonly QualityInspectionTraceabilityItem[];
  nonconformances: readonly QualityNcrTraceabilityItem[];
  holds: readonly QualityHoldTraceabilityItem[];
  correctiveActions: readonly QualityCorrectiveActionTraceabilityItem[];
}>;

export type QualityReportingTrendPeriod = "day" | "week" | "month";

export function formatQualityPercent(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return `${value.toFixed(1)}%`;
}
