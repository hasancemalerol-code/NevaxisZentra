export type ExportAiAnalysisRunType =
  | "case-overview"
  | "compliance-explanation"
  | "readiness-analysis"
  | "missing-data-analysis"
  | "country-research-assist"
  | "next-actions"
  | "document-gap-analysis"
  | "cross-module-risk-analysis";

export type ExportAiAnalysisRunStatus =
  | "queued"
  | "running"
  | "completed"
  | "failed"
  | "cancelled";

export type ExportAiFindingCategory =
  | "compliance"
  | "document"
  | "packing"
  | "shipment"
  | "broker"
  | "country-research"
  | "quality"
  | "production"
  | "planning"
  | "erp"
  | "risk"
  | "data-quality"
  | "other";

export type ExportAiFindingSeverity = "info" | "warning" | "critical";

export type ExportAiSourceType =
  | "verified-rule"
  | "verified-knowledge"
  | "historical-observation"
  | "broker-input"
  | "operational-data"
  | "ai-interpretation"
  | "system-deterministic";

export type ExportAiRecommendationPriority =
  | "low"
  | "medium"
  | "high"
  | "critical";

export type ExportAiRecommendationStatus =
  | "proposed"
  | "accepted"
  | "rejected"
  | "dismissed"
  | "expired";

export type ExportAiReviewDecision = "accepted" | "rejected" | "dismissed";

export type ExportCrossModuleRiskType =
  | "production-delay"
  | "planning-deadline-risk"
  | "quality-hold"
  | "missing-material"
  | "erp-data-missing"
  | "shipment-cutoff-risk"
  | "context-unavailable";

export type ExportCrossModuleRiskSeverity = "info" | "warning" | "critical";

export type ExportCrossModuleSourceModule =
  | "production"
  | "planning"
  | "quality"
  | "erp"
  | "shipment"
  | "export";

export const EXPORT_AI_DEVELOPMENT_PROVIDER = "deterministic-development";

export type ExportAiSourceReference = Readonly<{
  sourceType: string;
  sourceId: string;
  label: string;
  detail?: string;
}>;

export type ExportAiAnalysisRun = Readonly<{
  id: string;
  exportCaseId?: string;
  countryResearchCaseId?: string;
  contextSnapshotId: string;
  runType: ExportAiAnalysisRunType;
  status: ExportAiAnalysisRunStatus;
  provider: string;
  model: string;
  promptVersion: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  requestedBySubjectId: string;
  inputUsageUnits: number;
  outputUsageUnits: number;
  aiCreditsConsumed: number;
  failureCode?: string;
  failureMessage?: string;
  reasoningSummary: string;
  version: number;
}>;

export type ExportAiFinding = Readonly<{
  id: string;
  analysisRunId: string;
  category: ExportAiFindingCategory;
  title: string;
  summary: string;
  severity: ExportAiFindingSeverity;
  confidence: number;
  sourceType: ExportAiSourceType;
  sourceReferences: readonly ExportAiSourceReference[];
  relatedRequirementId?: string;
  relatedDocumentId?: string;
  relatedShipmentId?: string;
  actionable: boolean;
  humanReviewRequired: boolean;
  createdAtUtc: string;
}>;

export type ExportAiRecommendation = Readonly<{
  id: string;
  analysisRunId: string;
  title: string;
  description: string;
  priority: ExportAiRecommendationPriority;
  confidence: number;
  reasoningSummary: string;
  proposedActionType?: string;
  proposedActionPayloadJson?: string;
  status: ExportAiRecommendationStatus;
  createdAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBySubjectId?: string;
  reviewDecision?: ExportAiReviewDecision;
  reviewNote?: string;
  version: number;
}>;

export type ExportCrossModuleRisk = Readonly<{
  type: ExportCrossModuleRiskType;
  severity: ExportCrossModuleRiskSeverity;
  sourceModule: ExportCrossModuleSourceModule;
  sourceRecordReference: string;
  description: string;
  detectedAtUtc: string;
  blocking: boolean;
}>;

export type ExportAiContextCaseRef = Readonly<{
  exportCaseId: string;
  caseNumber: string;
  destinationCountryCode: string;
  originCountryCode?: string;
  readinessStatus: string;
  readinessPercent: number;
  complianceStatus: string;
  hsCode?: string;
  hsCodeStatus: string;
  originConfirmationStatus: string;
}>;

export type ExportAiContextSnapshot = Readonly<{
  exportCaseId?: string;
  countryResearchCaseId?: string;
  exportCase?: ExportAiContextCaseRef;
  sourceOrderReference?: string;
  customerName?: string;
  crossModuleRisks: readonly ExportCrossModuleRisk[];
  capturedAtUtc: string;
}>;

export type ExportAiRunDetail = Readonly<{
  run: ExportAiAnalysisRun;
  findings: readonly ExportAiFinding[];
  recommendations: readonly ExportAiRecommendation[];
  contextSnapshot?: ExportAiContextSnapshot;
}>;

export type AiUsageSummary = Readonly<{
  creditsConsumedThisPeriod: number;
  monthlyQuota?: number;
  usagePercent: number;
  warningLevel: string;
}>;

export type StartExportAiAnalysisInput = Readonly<{
  runType: ExportAiAnalysisRunType;
}>;

export type ReviewExportAiRecommendationInput = Readonly<{
  expectedVersion: number;
  reviewNote?: string;
}>;

export function getExportAiRunTypeLabel(runType: ExportAiAnalysisRunType): string {
  switch (runType) {
    case "case-overview":
      return "Dosya özeti";
    case "compliance-explanation":
      return "Uygunluk açıklaması";
    case "readiness-analysis":
      return "Hazırlık analizi";
    case "missing-data-analysis":
      return "Eksik veri analizi";
    case "country-research-assist":
      return "Ülke araştırma asistanı";
    case "next-actions":
      return "Sonraki adımlar";
    case "document-gap-analysis":
      return "Belge boşluk analizi";
    case "cross-module-risk-analysis":
      return "Modüller arası risk";
    default:
      return runType;
  }
}

export function getExportAiRunStatusLabel(
  status: ExportAiAnalysisRunStatus,
): string {
  switch (status) {
    case "queued":
      return "Kuyrukta";
    case "running":
      return "Çalışıyor";
    case "completed":
      return "Tamamlandı";
    case "failed":
      return "Başarısız";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getExportAiFindingSeverityLabel(
  severity: ExportAiFindingSeverity,
): string {
  switch (severity) {
    case "info":
      return "Bilgi";
    case "warning":
      return "Uyarı";
    case "critical":
      return "Kritik";
    default:
      return severity;
  }
}

export function getExportAiSourceTypeLabel(sourceType: ExportAiSourceType): string {
  switch (sourceType) {
    case "verified-rule":
      return "Doğrulanmış kural";
    case "verified-knowledge":
      return "Doğrulanmış bilgi";
    case "historical-observation":
      return "Geçmiş gözlem";
    case "broker-input":
      return "Broker girdisi";
    case "operational-data":
      return "Operasyon verisi";
    case "ai-interpretation":
      return "AI yorumu";
    case "system-deterministic":
      return "Sistem (geliştirme)";
    default:
      return sourceType;
  }
}

export function getExportAiRecommendationPriorityLabel(
  priority: ExportAiRecommendationPriority,
): string {
  switch (priority) {
    case "low":
      return "Düşük";
    case "medium":
      return "Orta";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return priority;
  }
}

export function getExportAiRecommendationStatusLabel(
  status: ExportAiRecommendationStatus,
): string {
  switch (status) {
    case "proposed":
      return "Önerildi";
    case "accepted":
      return "Kabul edildi";
    case "rejected":
      return "Reddedildi";
    case "dismissed":
      return "Kapatıldı";
    case "expired":
      return "Süresi doldu";
    default:
      return status;
  }
}

export function getExportAiProviderLabel(provider: string): string {
  if (provider === EXPORT_AI_DEVELOPMENT_PROVIDER) {
    return "Geliştirme (deterministik)";
  }

  return provider;
}

export function getCrossModuleRiskSeverityLabel(
  severity: ExportCrossModuleRiskSeverity,
): string {
  switch (severity) {
    case "info":
      return "Bilgi";
    case "warning":
      return "Uyarı";
    case "critical":
      return "Kritik";
    default:
      return severity;
  }
}

export function formatAiConfidence(confidence: number): string {
  const percent = confidence <= 1 ? Math.round(confidence * 100) : Math.round(confidence);
  return `%${Math.min(Math.max(percent, 0), 100)}`;
}
