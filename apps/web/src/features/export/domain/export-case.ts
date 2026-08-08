export type ExportCaseStatus =
  | "open"
  | "preparing"
  | "blocked"
  | "ready"
  | "closed";

export type ExportReadinessStatus =
  | "not-ready"
  | "attention"
  | "ready";

export type ExportDocumentStatus =
  | "missing"
  | "draft"
  | "received"
  | "verified"
  | "rejected";

export type ExportActivitySourceType =
  | "system"
  | "user"
  | "integration"
  | "ai";

export type ExportCaseSummary = Readonly<{
  id: string;
  facilityId: string;
  sourceOrderId: string;
  caseNumber: string;
  sourceOrderReference: string;
  customerName: string;
  destinationCountryCode: string;
  originCountryCode?: string;
  incotermCode?: string;
  status: ExportCaseStatus;
  readinessStatus: ExportReadinessStatus;
  readinessPercent: number;
  blockingIssueCount: number;
  missingRequirementCount: number;
  version: number;
  updatedAtUtc: string;
}>;

export type ExportCaseDetail = Readonly<{
  id: string;
  facilityId: string;
  sourceOrderId: string;
  caseNumber: string;
  sourceOrderReference: string;
  customerName: string;
  destinationCountryCode: string;
  originCountryCode?: string;
  incotermCode?: string;
  status: ExportCaseStatus;
  readinessStatus: ExportReadinessStatus;
  readinessPercent: number;
  blockingIssueCount: number;
  missingRequirementCount: number;
  createdBySubjectId: string;
  lastModifiedBySubjectId: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type ExportRequirement = Readonly<{
  key: string;
  label: string;
  isBlocking: boolean;
  isSatisfied: boolean;
  status: string;
}>;

export type ExportReadiness = Readonly<{
  exportCaseId: string;
  readinessStatus: ExportReadinessStatus;
  readinessPercent: number;
  blockingIssueCount: number;
  missingRequirementCount: number;
  suggestedCaseStatus: ExportCaseStatus;
  requirements: readonly ExportRequirement[];
}>;

export type ExportDocument = Readonly<{
  id: string;
  exportCaseId: string;
  documentType: string;
  documentStatus: ExportDocumentStatus;
  displayName: string;
  fileReference?: string;
  documentNumber?: string;
  issuedAtUtc?: string;
  expiresAtUtc?: string;
  source: string;
  rejectionReason?: string;
  verifiedAtUtc?: string;
  verifiedBySubjectId?: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type ExportActivity = Readonly<{
  id: string;
  exportCaseId: string;
  eventType: string;
  title: string;
  summary?: string;
  occurredAtUtc: string;
  sourceType: ExportActivitySourceType;
  actorSubjectId?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  correlationId: string;
}>;

export type ExportCaseWriteSuccess = Readonly<{
  exportCaseId: string;
  caseNumber: string;
  status: ExportCaseStatus;
  readinessStatus: ExportReadinessStatus;
  readinessPercent: number;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  alreadyExisted: boolean;
}>;

export type EnsureExportCaseFromOrderInput = Readonly<{
  facilityId: string;
  sourceOrderId: string;
  sourceOrderReference: string;
  customerName: string;
  isExport: boolean;
  destinationCountryCode: string;
  originCountryCode?: string;
  incotermCode?: string;
}>;

export type RegisterExportDocumentInput = Readonly<{
  documentType: string;
  displayName: string;
  fileReference?: string;
  documentNumber?: string;
  issuedAtUtc?: string;
  expiresAtUtc?: string;
  source?: string;
  initialStatus?: string;
}>;

export function getExportCaseStatusLabel(status: ExportCaseStatus): string {
  switch (status) {
    case "open":
      return "Açık";
    case "preparing":
      return "Hazırlanıyor";
    case "blocked":
      return "Blokeli";
    case "ready":
      return "Hazır";
    case "closed":
      return "Kapalı";
    default:
      return status;
  }
}

export function getExportReadinessStatusLabel(
  status: ExportReadinessStatus,
): string {
  switch (status) {
    case "not-ready":
      return "Hazır değil";
    case "attention":
      return "Dikkat";
    case "ready":
      return "Hazır";
    default:
      return status;
  }
}

export function getExportDocumentStatusLabel(
  status: ExportDocumentStatus,
): string {
  switch (status) {
    case "missing":
      return "Eksik";
    case "draft":
      return "Taslak";
    case "received":
      return "Alındı";
    case "verified":
      return "Doğrulandı";
    case "rejected":
      return "Reddedildi";
    default:
      return status;
  }
}

export function getExportDocumentTypeLabel(documentType: string): string {
  switch (documentType) {
    case "commercial-invoice":
      return "Ticari fatura";
    case "packing-list":
      return "Çeki listesi";
    case "customer-purchase-order":
      return "Müşteri sipariş formu";
    case "certificate-of-origin":
      return "Menşe belgesi";
    case "circulation-document":
      return "Dolaşım belgesi";
    case "transport-document":
      return "Taşıma belgesi";
    case "material-certificate":
      return "Malzeme sertifikası";
    case "quality-certificate":
      return "Kalite sertifikası";
    case "customs-document":
      return "Gümrük belgesi";
    case "other":
      return "Diğer";
    default:
      return documentType;
  }
}
