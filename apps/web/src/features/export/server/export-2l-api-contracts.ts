import "./export-case-server-boundary";

import type {
  BrokerCorrectionSeverity,
  BrokerCorrectionStatus,
  BrokerPackageVersionStatus,
  BrokerWorkspaceStatus,
  ExportBrokerCorrection,
  ExportBrokerPackageDocument,
  ExportBrokerPackageVersion,
  ExportBrokerWorkspace,
  ExportPackingItemSummary,
  ExportPackingReconciliation,
  ExportPackingUnitSummary,
  ExportPackingWorkspace,
  ExportShipment,
  ExportShipmentReadiness,
  ExportShipmentReadinessRequirement,
  PackingReconciliationStatus,
  PackingStatus,
  ShipmentStatus,
} from "../domain/export-2l";

const PACKING_STATUSES = new Set<PackingStatus>([
  "draft",
  "packing",
  "complete",
  "verified",
]);

const RECONCILIATION_STATUSES = new Set<PackingReconciliationStatus>([
  "not-started",
  "incomplete",
  "matched",
  "overpacked",
  "mismatch",
  "unable-to-validate",
]);

const SHIPMENT_STATUSES = new Set<ShipmentStatus>([
  "draft",
  "preparing",
  "ready",
  "handed-to-broker",
  "cleared-for-dispatch",
  "dispatched",
  "closed",
  "cancelled",
]);

const BROKER_WORKSPACE_STATUSES = new Set<BrokerWorkspaceStatus>([
  "draft",
  "package-ready",
  "sent",
  "acknowledged",
  "correction-requested",
  "ready",
  "closed",
  "cancelled",
]);

const PACKAGE_VERSION_STATUSES = new Set<BrokerPackageVersionStatus>([
  "draft",
  "sent",
  "stale",
]);

const CORRECTION_SEVERITIES = new Set<BrokerCorrectionSeverity>([
  "info",
  "warning",
  "blocking",
]);

const CORRECTION_STATUSES = new Set<BrokerCorrectionStatus>([
  "open",
  "resolved",
  "cancelled",
]);

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function fail<T>(message: string): ParseResult<T> {
  return { status: "failure", message };
}

function ok<T>(value: T): ParseResult<T> {
  return { status: "success", value };
}

function isString(v: unknown): v is string {
  return typeof v === "string";
}

function isNumber(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

function isBoolean(v: unknown): v is boolean {
  return typeof v === "boolean";
}

function optionalString(value: unknown): string | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }

  return typeof value === "string" ? value : undefined;
}

function optionalNumber(value: unknown): number | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }

  return typeof value === "number" && Number.isFinite(value) ? value : undefined;
}

function parsePackingItem(raw: unknown): ParseResult<ExportPackingItemSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("packing item must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("item.id is required.");
  if (!isString(o.packingUnitId)) return fail("item.packingUnitId is required.");
  if (!isString(o.productCode)) return fail("item.productCode is required.");
  if (!isString(o.productName)) return fail("item.productName is required.");
  if (!isNumber(o.quantity)) return fail("item.quantity is required.");
  if (!isString(o.unitOfMeasure)) return fail("item.unitOfMeasure is required.");
  if (!isString(o.createdAtUtc)) return fail("item.createdAtUtc is required.");

  return ok({
    id: o.id,
    packingUnitId: o.packingUnitId,
    sourceOrderLineId: optionalString(o.sourceOrderLineId),
    productCode: o.productCode,
    productName: o.productName,
    quantity: o.quantity,
    unitOfMeasure: o.unitOfMeasure,
    lotBatchReference: optionalString(o.lotBatchReference),
    createdAtUtc: o.createdAtUtc,
  });
}

function parsePackingUnit(raw: unknown): ParseResult<ExportPackingUnitSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("packing unit must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("unit.id is required.");
  if (!isString(o.exportCaseId)) return fail("unit.exportCaseId is required.");
  if (!isString(o.unitCode)) return fail("unit.unitCode is required.");
  if (!isString(o.unitType)) return fail("unit.unitType is required.");
  if (!isString(o.status) || !PACKING_STATUSES.has(o.status as PackingStatus)) {
    return fail(`unit.status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isNumber(o.version)) return fail("unit.version is required.");
  if (!isString(o.createdAtUtc)) return fail("unit.createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("unit.updatedAtUtc is required.");
  if (!Array.isArray(o.items)) return fail("unit.items must be an array.");

  const items: ExportPackingItemSummary[] = [];

  for (let i = 0; i < o.items.length; i++) {
    const parsed = parsePackingItem(o.items[i]);
    if (parsed.status === "failure") {
      return fail(`unit.items[${i}]: ${parsed.message}`);
    }
    items.push(parsed.value);
  }

  return ok({
    id: o.id,
    exportCaseId: o.exportCaseId,
    unitCode: o.unitCode,
    unitType: o.unitType,
    parentPackingUnitId: optionalString(o.parentPackingUnitId),
    grossWeight: optionalNumber(o.grossWeight),
    netWeight: optionalNumber(o.netWeight),
    weightUnit: optionalString(o.weightUnit),
    length: optionalNumber(o.length),
    width: optionalNumber(o.width),
    height: optionalNumber(o.height),
    dimensionUnit: optionalString(o.dimensionUnit),
    volume: optionalNumber(o.volume),
    marksAndNumbers: optionalString(o.marksAndNumbers),
    status: o.status as PackingStatus,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
    items,
  });
}

export function parseExportPackingWorkspaceApiResponse(
  raw: unknown,
): ParseResult<ExportPackingWorkspace> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (
    !isString(o.packingStatus) ||
    !PACKING_STATUSES.has(o.packingStatus as PackingStatus)
  ) {
    return fail(
      `packingStatus has an invalid value: '${String(o.packingStatus)}'.`,
    );
  }
  if (!Array.isArray(o.units)) return fail("units must be an array.");

  const units: ExportPackingUnitSummary[] = [];

  for (let i = 0; i < o.units.length; i++) {
    const parsed = parsePackingUnit(o.units[i]);
    if (parsed.status === "failure") {
      return fail(`units[${i}]: ${parsed.message}`);
    }
    units.push(parsed.value);
  }

  return ok({
    exportCaseId: o.exportCaseId,
    packingStatus: o.packingStatus as PackingStatus,
    packingCompletedAtUtc: optionalString(o.packingCompletedAtUtc),
    packingCompletedBySubjectId: optionalString(o.packingCompletedBySubjectId),
    packingVerifiedAtUtc: optionalString(o.packingVerifiedAtUtc),
    packingVerifiedBySubjectId: optionalString(o.packingVerifiedBySubjectId),
    units,
  });
}

export function parseExportPackingReconciliationApiResponse(
  raw: unknown,
): ParseResult<ExportPackingReconciliation> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (
    !isString(o.reconciliationStatus) ||
    !RECONCILIATION_STATUSES.has(
      o.reconciliationStatus as PackingReconciliationStatus,
    )
  ) {
    return fail(
      `reconciliationStatus has an invalid value: '${String(o.reconciliationStatus)}'.`,
    );
  }
  if (!isNumber(o.itemCount)) return fail("itemCount is required.");
  if (!isNumber(o.totalQuantity)) return fail("totalQuantity is required.");

  return ok({
    exportCaseId: o.exportCaseId,
    reconciliationStatus: o.reconciliationStatus as PackingReconciliationStatus,
    explanation: optionalString(o.explanation),
    itemCount: o.itemCount,
    totalQuantity: o.totalQuantity,
  });
}

export function parseExportShipmentApiResponse(
  raw: unknown,
): ParseResult<ExportShipment> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.shipmentReference)) {
    return fail("shipmentReference is required.");
  }
  if (
    !isString(o.status) ||
    !SHIPMENT_STATUSES.has(o.status as ShipmentStatus)
  ) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isString(o.destinationCountryCode)) {
    return fail("destinationCountryCode is required.");
  }
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  return ok({
    id: o.id,
    exportCaseId: o.exportCaseId,
    shipmentReference: o.shipmentReference,
    status: o.status as ShipmentStatus,
    transportMode: optionalString(o.transportMode),
    incotermCode: optionalString(o.incotermCode),
    destinationCountryCode: o.destinationCountryCode,
    destinationCity: optionalString(o.destinationCity),
    deliveryAddress: optionalString(o.deliveryAddress),
    forwarderName: optionalString(o.forwarderName),
    carrierName: optionalString(o.carrierName),
    plannedPickupAtUtc: optionalString(o.plannedPickupAtUtc),
    plannedDepartureAtUtc: optionalString(o.plannedDepartureAtUtc),
    plannedArrivalAtUtc: optionalString(o.plannedArrivalAtUtc),
    actualPickupAtUtc: optionalString(o.actualPickupAtUtc),
    actualDepartureAtUtc: optionalString(o.actualDepartureAtUtc),
    actualArrivalAtUtc: optionalString(o.actualArrivalAtUtc),
    bookingReference: optionalString(o.bookingReference),
    trackingReference: optionalString(o.trackingReference),
    totalGrossWeight: optionalNumber(o.totalGrossWeight),
    totalNetWeight: optionalNumber(o.totalNetWeight),
    totalPackageCount: optionalNumber(o.totalPackageCount),
    cancellationReason: optionalString(o.cancellationReason),
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
  });
}

export function parseExportShipmentListApiResponse(
  raw: unknown,
): ParseResult<ExportShipment[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ExportShipment[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseExportShipmentApiResponse(raw[i]);
    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }
    items.push(parsed.value);
  }

  return ok(items);
}

function parseShipmentRequirement(
  raw: unknown,
): ParseResult<ExportShipmentReadinessRequirement> {
  if (typeof raw !== "object" || raw === null) {
    return fail("requirement must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.key)) return fail("requirement.key is required.");
  if (!isString(o.label)) return fail("requirement.label is required.");
  if (!isBoolean(o.isBlocking)) return fail("requirement.isBlocking is required.");
  if (!isBoolean(o.isSatisfied)) {
    return fail("requirement.isSatisfied is required.");
  }
  if (!isString(o.status)) return fail("requirement.status is required.");

  return ok({
    key: o.key,
    label: o.label,
    isBlocking: o.isBlocking,
    isSatisfied: o.isSatisfied,
    status: o.status,
  });
}

export function parseExportShipmentReadinessApiResponse(
  raw: unknown,
): ParseResult<ExportShipmentReadiness> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.shipmentId)) return fail("shipmentId is required.");
  if (!isString(o.readinessStatus)) return fail("readinessStatus is required.");
  if (!isNumber(o.readinessPercent)) return fail("readinessPercent is required.");
  if (!isNumber(o.blockingIssueCount)) {
    return fail("blockingIssueCount is required.");
  }
  if (!isNumber(o.missingRequirementCount)) {
    return fail("missingRequirementCount is required.");
  }
  if (!Array.isArray(o.requirements)) {
    return fail("requirements must be an array.");
  }

  const requirements: ExportShipmentReadinessRequirement[] = [];

  for (let i = 0; i < o.requirements.length; i++) {
    const parsed = parseShipmentRequirement(o.requirements[i]);
    if (parsed.status === "failure") {
      return fail(`requirements[${i}]: ${parsed.message}`);
    }
    requirements.push(parsed.value);
  }

  return ok({
    shipmentId: o.shipmentId,
    readinessStatus: o.readinessStatus,
    readinessPercent: o.readinessPercent,
    blockingIssueCount: o.blockingIssueCount,
    missingRequirementCount: o.missingRequirementCount,
    requirements,
  });
}

function parsePackageDocument(
  raw: unknown,
): ParseResult<ExportBrokerPackageDocument> {
  if (typeof raw !== "object" || raw === null) {
    return fail("package document must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("document.id is required.");
  if (!isString(o.documentId)) return fail("document.documentId is required.");
  if (!isString(o.documentType)) return fail("document.documentType is required.");
  if (!isString(o.documentStatusSnapshot)) {
    return fail("document.documentStatusSnapshot is required.");
  }
  if (!isString(o.displayNameSnapshot)) {
    return fail("document.displayNameSnapshot is required.");
  }
  if (!isBoolean(o.included)) return fail("document.included is required.");

  return ok({
    id: o.id,
    documentId: o.documentId,
    documentType: o.documentType,
    documentStatusSnapshot: o.documentStatusSnapshot,
    displayNameSnapshot: o.displayNameSnapshot,
    included: o.included,
  });
}

function parsePackageVersion(
  raw: unknown,
): ParseResult<ExportBrokerPackageVersion> {
  if (typeof raw !== "object" || raw === null) {
    return fail("package version must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("packageVersion.id is required.");
  if (!isNumber(o.versionNumber)) {
    return fail("packageVersion.versionNumber is required.");
  }
  if (
    !isString(o.status) ||
    !PACKAGE_VERSION_STATUSES.has(o.status as BrokerPackageVersionStatus)
  ) {
    return fail(
      `packageVersion.status has an invalid value: '${String(o.status)}'.`,
    );
  }
  if (!isString(o.createdAtUtc)) {
    return fail("packageVersion.createdAtUtc is required.");
  }
  if (!Array.isArray(o.documents)) {
    return fail("packageVersion.documents must be an array.");
  }

  const documents: ExportBrokerPackageDocument[] = [];

  for (let i = 0; i < o.documents.length; i++) {
    const parsed = parsePackageDocument(o.documents[i]);
    if (parsed.status === "failure") {
      return fail(`documents[${i}]: ${parsed.message}`);
    }
    documents.push(parsed.value);
  }

  return ok({
    id: o.id,
    versionNumber: o.versionNumber,
    status: o.status as BrokerPackageVersionStatus,
    notes: optionalString(o.notes),
    createdAtUtc: o.createdAtUtc,
    sentAtUtc: optionalString(o.sentAtUtc),
    sentBySubjectId: optionalString(o.sentBySubjectId),
    documents,
  });
}

function parseCorrection(raw: unknown): ParseResult<ExportBrokerCorrection> {
  if (typeof raw !== "object" || raw === null) {
    return fail("correction must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("correction.id is required.");
  if (!isString(o.brokerWorkspaceId)) {
    return fail("correction.brokerWorkspaceId is required.");
  }
  if (!isString(o.category)) return fail("correction.category is required.");
  if (!isString(o.title)) return fail("correction.title is required.");
  if (!isString(o.description)) {
    return fail("correction.description is required.");
  }
  if (
    !isString(o.severity) ||
    !CORRECTION_SEVERITIES.has(o.severity as BrokerCorrectionSeverity)
  ) {
    return fail(
      `correction.severity has an invalid value: '${String(o.severity)}'.`,
    );
  }
  if (
    !isString(o.status) ||
    !CORRECTION_STATUSES.has(o.status as BrokerCorrectionStatus)
  ) {
    return fail(
      `correction.status has an invalid value: '${String(o.status)}'.`,
    );
  }
  if (!isString(o.createdAtUtc)) {
    return fail("correction.createdAtUtc is required.");
  }

  return ok({
    id: o.id,
    brokerWorkspaceId: o.brokerWorkspaceId,
    category: o.category,
    title: o.title,
    description: o.description,
    severity: o.severity as BrokerCorrectionSeverity,
    status: o.status as BrokerCorrectionStatus,
    relatedDocumentId: optionalString(o.relatedDocumentId),
    createdAtUtc: o.createdAtUtc,
    resolvedAtUtc: optionalString(o.resolvedAtUtc),
    resolvedBySubjectId: optionalString(o.resolvedBySubjectId),
    resolutionNote: optionalString(o.resolutionNote),
  });
}

export function parseExportBrokerWorkspaceApiResponse(
  raw: unknown,
): ParseResult<ExportBrokerWorkspace> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.brokerCompanyName)) {
    return fail("brokerCompanyName is required.");
  }
  if (
    !isString(o.status) ||
    !BROKER_WORKSPACE_STATUSES.has(o.status as BrokerWorkspaceStatus)
  ) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");
  if (!Array.isArray(o.packageVersions)) {
    return fail("packageVersions must be an array.");
  }
  if (!Array.isArray(o.corrections)) {
    return fail("corrections must be an array.");
  }

  const packageVersions: ExportBrokerPackageVersion[] = [];

  for (let i = 0; i < o.packageVersions.length; i++) {
    const parsed = parsePackageVersion(o.packageVersions[i]);
    if (parsed.status === "failure") {
      return fail(`packageVersions[${i}]: ${parsed.message}`);
    }
    packageVersions.push(parsed.value);
  }

  const corrections: ExportBrokerCorrection[] = [];

  for (let i = 0; i < o.corrections.length; i++) {
    const parsed = parseCorrection(o.corrections[i]);
    if (parsed.status === "failure") {
      return fail(`corrections[${i}]: ${parsed.message}`);
    }
    corrections.push(parsed.value);
  }

  return ok({
    id: o.id,
    exportCaseId: o.exportCaseId,
    shipmentId: optionalString(o.shipmentId),
    brokerCompanyName: o.brokerCompanyName,
    brokerContactName: optionalString(o.brokerContactName),
    brokerEmail: optionalString(o.brokerEmail),
    brokerPhone: optionalString(o.brokerPhone),
    status: o.status as BrokerWorkspaceStatus,
    preparedAtUtc: optionalString(o.preparedAtUtc),
    preparedBySubjectId: optionalString(o.preparedBySubjectId),
    sentAtUtc: optionalString(o.sentAtUtc),
    sentBySubjectId: optionalString(o.sentBySubjectId),
    acknowledgedAtUtc: optionalString(o.acknowledgedAtUtc),
    completedAtUtc: optionalString(o.completedAtUtc),
    brokerReference: optionalString(o.brokerReference),
    notes: optionalString(o.notes),
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
    packageVersions,
    corrections,
  });
}

export type PackingUnitWriteSuccess = Readonly<{
  packingUnitId: string;
  unitVersion: number;
  caseVersion: number;
}>;

export type PackingItemWriteSuccess = Readonly<{
  itemId: string;
  caseVersion: number;
}>;

export type PackingStatusWriteSuccess = Readonly<{
  caseVersion: number;
  packingStatus: PackingStatus;
}>;

export type ShipmentWriteSuccess = Readonly<{
  shipmentId: string;
  shipmentVersion: number;
  status: ShipmentStatus;
}>;

export type BrokerWorkspaceWriteSuccess = Readonly<{
  workspaceId: string;
  workspaceVersion: number;
  status: BrokerWorkspaceStatus;
}>;

export type BrokerPackageWriteSuccess = Readonly<{
  packageVersionId: string;
  versionNumber: number;
  workspaceVersion: number;
}>;

export type BrokerCorrectionWriteSuccess = Readonly<{
  correctionId: string;
}>;

export function parsePackingUnitWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<PackingUnitWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.packingUnitId)) return fail("packingUnitId is required.");
  if (!isNumber(o.unitVersion)) return fail("unitVersion is required.");
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");

  return ok({
    packingUnitId: o.packingUnitId,
    unitVersion: o.unitVersion,
    caseVersion: o.caseVersion,
  });
}

export function parsePackingItemWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<PackingItemWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.itemId)) return fail("itemId is required.");
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");

  return ok({
    itemId: o.itemId,
    caseVersion: o.caseVersion,
  });
}

export function parsePackingStatusWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<PackingStatusWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");
  if (
    !isString(o.packingStatus) ||
    !PACKING_STATUSES.has(o.packingStatus as PackingStatus)
  ) {
    return fail(
      `packingStatus has an invalid value: '${String(o.packingStatus)}'.`,
    );
  }

  return ok({
    caseVersion: o.caseVersion,
    packingStatus: o.packingStatus as PackingStatus,
  });
}

export function parseShipmentWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ShipmentWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.shipmentId)) return fail("shipmentId is required.");
  if (!isNumber(o.shipmentVersion)) return fail("shipmentVersion is required.");
  if (
    !isString(o.status) ||
    !SHIPMENT_STATUSES.has(o.status as ShipmentStatus)
  ) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }

  return ok({
    shipmentId: o.shipmentId,
    shipmentVersion: o.shipmentVersion,
    status: o.status as ShipmentStatus,
  });
}

export function parseBrokerWorkspaceWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<BrokerWorkspaceWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.workspaceId)) return fail("workspaceId is required.");
  if (!isNumber(o.workspaceVersion)) {
    return fail("workspaceVersion is required.");
  }
  if (
    !isString(o.status) ||
    !BROKER_WORKSPACE_STATUSES.has(o.status as BrokerWorkspaceStatus)
  ) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }

  return ok({
    workspaceId: o.workspaceId,
    workspaceVersion: o.workspaceVersion,
    status: o.status as BrokerWorkspaceStatus,
  });
}

export function parseBrokerPackageWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<BrokerPackageWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.packageVersionId)) {
    return fail("packageVersionId is required.");
  }
  if (!isNumber(o.versionNumber)) return fail("versionNumber is required.");
  if (!isNumber(o.workspaceVersion)) {
    return fail("workspaceVersion is required.");
  }

  return ok({
    packageVersionId: o.packageVersionId,
    versionNumber: o.versionNumber,
    workspaceVersion: o.workspaceVersion,
  });
}

export function parseBrokerCorrectionWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<BrokerCorrectionWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.correctionId)) return fail("correctionId is required.");

  return ok({ correctionId: o.correctionId });
}
