export type PackingStatus = "draft" | "packing" | "complete" | "verified";

export type PackingUnitType =
  | "pallet"
  | "carton"
  | "crate"
  | "container-package"
  | "other";

export type PackingReconciliationStatus =
  | "not-started"
  | "incomplete"
  | "matched"
  | "overpacked"
  | "mismatch"
  | "unable-to-validate";

export type ShipmentStatus =
  | "draft"
  | "preparing"
  | "ready"
  | "handed-to-broker"
  | "cleared-for-dispatch"
  | "dispatched"
  | "closed"
  | "cancelled";

export type TransportMode =
  | "road"
  | "air"
  | "sea"
  | "rail"
  | "courier"
  | "multimodal"
  | "other";

export type BrokerWorkspaceStatus =
  | "draft"
  | "package-ready"
  | "sent"
  | "acknowledged"
  | "correction-requested"
  | "ready"
  | "closed"
  | "cancelled";

export type BrokerPackageVersionStatus = "draft" | "sent" | "stale";

export type BrokerCorrectionSeverity = "info" | "warning" | "blocking";

export type BrokerCorrectionStatus = "open" | "resolved" | "cancelled";

export type ExportPackingItemSummary = Readonly<{
  id: string;
  packingUnitId: string;
  sourceOrderLineId?: string;
  productCode: string;
  productName: string;
  quantity: number;
  unitOfMeasure: string;
  lotBatchReference?: string;
  createdAtUtc: string;
}>;

export type ExportPackingUnitSummary = Readonly<{
  id: string;
  exportCaseId: string;
  unitCode: string;
  unitType: string;
  parentPackingUnitId?: string;
  grossWeight?: number;
  netWeight?: number;
  weightUnit?: string;
  length?: number;
  width?: number;
  height?: number;
  dimensionUnit?: string;
  volume?: number;
  marksAndNumbers?: string;
  status: PackingStatus;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  items: readonly ExportPackingItemSummary[];
}>;

export type ExportPackingWorkspace = Readonly<{
  exportCaseId: string;
  packingStatus: PackingStatus;
  packingCompletedAtUtc?: string;
  packingCompletedBySubjectId?: string;
  packingVerifiedAtUtc?: string;
  packingVerifiedBySubjectId?: string;
  units: readonly ExportPackingUnitSummary[];
}>;

export type ExportPackingReconciliation = Readonly<{
  exportCaseId: string;
  reconciliationStatus: PackingReconciliationStatus;
  explanation?: string;
  itemCount: number;
  totalQuantity: number;
}>;

export type ExportShipment = Readonly<{
  id: string;
  exportCaseId: string;
  shipmentReference: string;
  status: ShipmentStatus;
  transportMode?: string;
  incotermCode?: string;
  destinationCountryCode: string;
  destinationCity?: string;
  deliveryAddress?: string;
  forwarderName?: string;
  carrierName?: string;
  plannedPickupAtUtc?: string;
  plannedDepartureAtUtc?: string;
  plannedArrivalAtUtc?: string;
  actualPickupAtUtc?: string;
  actualDepartureAtUtc?: string;
  actualArrivalAtUtc?: string;
  bookingReference?: string;
  trackingReference?: string;
  totalGrossWeight?: number;
  totalNetWeight?: number;
  totalPackageCount?: number;
  cancellationReason?: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type ExportShipmentReadinessRequirement = Readonly<{
  key: string;
  label: string;
  isBlocking: boolean;
  isSatisfied: boolean;
  status: string;
}>;

export type ExportShipmentReadiness = Readonly<{
  shipmentId: string;
  readinessStatus: string;
  readinessPercent: number;
  blockingIssueCount: number;
  missingRequirementCount: number;
  requirements: readonly ExportShipmentReadinessRequirement[];
}>;

export type ExportBrokerPackageDocument = Readonly<{
  id: string;
  documentId: string;
  documentType: string;
  documentStatusSnapshot: string;
  displayNameSnapshot: string;
  included: boolean;
}>;

export type ExportBrokerPackageVersion = Readonly<{
  id: string;
  versionNumber: number;
  status: BrokerPackageVersionStatus;
  notes?: string;
  createdAtUtc: string;
  sentAtUtc?: string;
  sentBySubjectId?: string;
  documents: readonly ExportBrokerPackageDocument[];
}>;

export type ExportBrokerCorrection = Readonly<{
  id: string;
  brokerWorkspaceId: string;
  category: string;
  title: string;
  description: string;
  severity: BrokerCorrectionSeverity;
  status: BrokerCorrectionStatus;
  relatedDocumentId?: string;
  createdAtUtc: string;
  resolvedAtUtc?: string;
  resolvedBySubjectId?: string;
  resolutionNote?: string;
}>;

export type ExportBrokerWorkspace = Readonly<{
  id: string;
  exportCaseId: string;
  shipmentId?: string;
  brokerCompanyName: string;
  brokerContactName?: string;
  brokerEmail?: string;
  brokerPhone?: string;
  status: BrokerWorkspaceStatus;
  preparedAtUtc?: string;
  preparedBySubjectId?: string;
  sentAtUtc?: string;
  sentBySubjectId?: string;
  acknowledgedAtUtc?: string;
  completedAtUtc?: string;
  brokerReference?: string;
  notes?: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  packageVersions: readonly ExportBrokerPackageVersion[];
  corrections: readonly ExportBrokerCorrection[];
}>;

export type CreatePackingUnitInput = Readonly<{
  unitCode: string;
  unitType: string;
  parentPackingUnitId?: string;
  grossWeight?: number;
  netWeight?: number;
  weightUnit?: string;
  length?: number;
  width?: number;
  height?: number;
  dimensionUnit?: string;
  marksAndNumbers?: string;
}>;

export type UpdatePackingUnitInput = Readonly<{
  expectedVersion: number;
  grossWeight?: number;
  netWeight?: number;
  weightUnit?: string;
  length?: number;
  width?: number;
  height?: number;
  dimensionUnit?: string;
  marksAndNumbers?: string;
}>;

export type AddPackingItemInput = Readonly<{
  productCode: string;
  productName: string;
  quantity: number;
  unitOfMeasure: string;
  sourceOrderLineId?: string;
  lotBatchReference?: string;
}>;

export type CreateShipmentInput = Readonly<{
  shipmentReference: string;
  transportMode?: string;
  incotermCode?: string;
  destinationCountryCode: string;
  destinationCity?: string;
  deliveryAddress?: string;
  forwarderName?: string;
  carrierName?: string;
  plannedPickupAtUtc?: string;
  plannedDepartureAtUtc?: string;
  plannedArrivalAtUtc?: string;
  bookingReference?: string;
}>;

export type UpdateShipmentInput = Readonly<{
  expectedVersion: number;
  transportMode?: string;
  incotermCode?: string;
  destinationCountryCode?: string;
  destinationCity?: string;
  deliveryAddress?: string;
  forwarderName?: string;
  carrierName?: string;
  plannedPickupAtUtc?: string;
  plannedDepartureAtUtc?: string;
  plannedArrivalAtUtc?: string;
  actualPickupAtUtc?: string;
  actualDepartureAtUtc?: string;
  actualArrivalAtUtc?: string;
  bookingReference?: string;
  trackingReference?: string;
  totalGrossWeight?: number;
  totalNetWeight?: number;
  totalPackageCount?: number;
}>;

export type TransitionShipmentInput = Readonly<{
  expectedVersion: number;
  targetStatus: string;
  cancellationReason?: string;
}>;

export type CreateBrokerWorkspaceInput = Readonly<{
  shipmentId?: string;
  brokerCompanyName: string;
  brokerContactName?: string;
  brokerEmail?: string;
  brokerPhone?: string;
  notes?: string;
}>;

export type PrepareBrokerPackageInput = Readonly<{
  expectedWorkspaceVersion: number;
  documentIds: readonly string[];
  notes?: string;
}>;

export type CreateBrokerCorrectionInput = Readonly<{
  category: string;
  title: string;
  description: string;
  severity: string;
  relatedDocumentId?: string;
}>;

export type ResolveBrokerCorrectionInput = Readonly<{
  expectedWorkspaceVersion: number;
  resolution: string;
  resolutionNote?: string;
}>;

export function getPackingStatusLabel(status: PackingStatus): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "packing":
      return "Paketleniyor";
    case "complete":
      return "Tamamlandı";
    case "verified":
      return "Doğrulandı";
    default:
      return status;
  }
}

export function getPackingUnitTypeLabel(unitType: string): string {
  switch (unitType) {
    case "pallet":
      return "Palet";
    case "carton":
      return "Karton";
    case "crate":
      return "Kasa";
    case "container-package":
      return "Konteyner paketi";
    case "other":
      return "Diğer";
    default:
      return unitType;
  }
}

export function getPackingReconciliationStatusLabel(
  status: PackingReconciliationStatus,
): string {
  switch (status) {
    case "not-started":
      return "Başlamadı";
    case "incomplete":
      return "Eksik";
    case "matched":
      return "Eşleşti";
    case "overpacked":
      return "Fazla paket";
    case "mismatch":
      return "Uyumsuz";
    case "unable-to-validate":
      return "Doğrulanamadı";
    default:
      return status;
  }
}

export function getShipmentStatusLabel(status: ShipmentStatus): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "preparing":
      return "Hazırlanıyor";
    case "ready":
      return "Hazır";
    case "handed-to-broker":
      return "Brokera verildi";
    case "cleared-for-dispatch":
      return "Sevkiyata hazır";
    case "dispatched":
      return "Sevk edildi";
    case "closed":
      return "Kapalı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getTransportModeLabel(mode: string): string {
  switch (mode) {
    case "road":
      return "Karayolu";
    case "air":
      return "Hava";
    case "sea":
      return "Deniz";
    case "rail":
      return "Demiryolu";
    case "courier":
      return "Kurye";
    case "multimodal":
      return "Çok modlu";
    case "other":
      return "Diğer";
    default:
      return mode;
  }
}

export function getBrokerWorkspaceStatusLabel(
  status: BrokerWorkspaceStatus,
): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "package-ready":
      return "Paket hazır";
    case "sent":
      return "Gönderildi";
    case "acknowledged":
      return "Alındı";
    case "correction-requested":
      return "Düzeltme istendi";
    case "ready":
      return "Hazır";
    case "closed":
      return "Kapalı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getBrokerPackageVersionStatusLabel(
  status: BrokerPackageVersionStatus,
): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "sent":
      return "Gönderildi";
    case "stale":
      return "Eski";
    default:
      return status;
  }
}

export function getBrokerCorrectionSeverityLabel(
  severity: BrokerCorrectionSeverity,
): string {
  switch (severity) {
    case "info":
      return "Bilgi";
    case "warning":
      return "Uyarı";
    case "blocking":
      return "Blokaj";
    default:
      return severity;
  }
}

export function getBrokerCorrectionStatusLabel(
  status: BrokerCorrectionStatus,
): string {
  switch (status) {
    case "open":
      return "Açık";
    case "resolved":
      return "Çözüldü";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export const PACKING_UNIT_TYPE_OPTIONS: readonly PackingUnitType[] = [
  "pallet",
  "carton",
  "crate",
  "container-package",
  "other",
] as const;

export const TRANSPORT_MODE_OPTIONS: readonly TransportMode[] = [
  "road",
  "air",
  "sea",
  "rail",
  "courier",
  "multimodal",
  "other",
] as const;

export const SHIPMENT_TRANSITION_OPTIONS: Readonly<
  Record<ShipmentStatus, readonly ShipmentStatus[]>
> = {
  draft: ["preparing", "cancelled"],
  preparing: ["ready", "cancelled"],
  ready: ["handed-to-broker", "preparing", "cancelled"],
  "handed-to-broker": ["cleared-for-dispatch", "cancelled"],
  "cleared-for-dispatch": ["dispatched"],
  dispatched: ["closed"],
  closed: [],
  cancelled: [],
};
