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
}>;

export type QualityWorkspaceSummary = Readonly<{
  openInspectionCount: number;
  pendingReviewCount: number;
  failedInspectionCount: number;
  releasedInspectionCount: number;
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
    default:
      return status;
  }
}
