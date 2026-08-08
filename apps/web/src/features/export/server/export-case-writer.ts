"use server";

import "./export-case-server-boundary";

import type {
  AddPackingItemInput,
  CreateBrokerCorrectionInput,
  CreateBrokerWorkspaceInput,
  CreatePackingUnitInput,
  CreateShipmentInput,
  PrepareBrokerPackageInput,
  ResolveBrokerCorrectionInput,
  TransitionShipmentInput,
  UpdatePackingUnitInput,
  UpdateShipmentInput,
} from "../domain/export-2l";
import type {
  AddResearchFindingInput,
  CreateKnowledgeEntryInput,
  CreateResearchCaseInput,
  EvaluateComplianceInput,
  SatisfyComplianceRequirementInput,
} from "../domain/export-3l";
import type {
  EnsureExportCaseFromOrderInput,
  ExportCaseWriteSuccess,
  RegisterExportDocumentInput,
} from "../domain/export-case";
import type {
  BrokerCorrectionWriteSuccess,
  BrokerPackageWriteSuccess,
  BrokerWorkspaceWriteSuccess,
  PackingItemWriteSuccess,
  PackingStatusWriteSuccess,
  PackingUnitWriteSuccess,
  ShipmentWriteSuccess,
} from "./export-2l-api-contracts";
import type {
  ComplianceRequirementWriteSuccess,
  ComplianceWriteSuccess,
  KnowledgeWriteSuccess,
  ResearchFindingWriteSuccess,
  ResearchWriteSuccess,
} from "./export-3l-api-contracts";
import {
  acknowledgeExportBroker,
  addExportPackingItem,
  addCountryResearchFinding,
  completeExportPacking,
  completeCountryResearchCase,
  createCountryKnowledgeEntry,
  createCountryResearchCase,
  createExportBrokerCorrection,
  createExportBrokerWorkspace,
  createExportPackingUnit,
  createExportShipment,
  ensureExportCaseFromOrder as ensureExportCaseFromOrderApi,
  evaluateExportCompliance,
  prepareExportBrokerPackage,
  registerExportDocument,
  rejectExportDocument,
  revalidateExportCompliance,
  resolveExportBrokerCorrection,
  reviewCountryResearchCase,
  satisfyExportComplianceRequirement,
  sendExportBrokerPackage,
  transitionExportShipment,
  updateExportPackingUnit,
  updateExportShipment,
  verifyCountryKnowledgeEntry,
  verifyExportDocument,
  verifyExportPacking,
  type ExportCaseApiResult,
} from "./export-case-api-client";
import type { ExportDocumentWriteSuccess } from "./export-case-api-contracts";
import {
  resolveExportCaseRequestContext,
  type ExportCaseRequestContext,
} from "./export-case-request-context";
import { parseExportRuntimeConfig } from "./export-case-runtime-config";

export type ExportCaseActionResult =
  | Readonly<{
      status: "success";
      value: ExportCaseWriteSuccess;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
      failureCode?: string;
    }>;

export type ExportDocumentActionResult =
  | Readonly<{
      status: "success";
      value: ExportDocumentWriteSuccess;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
      failureCode?: string;
    }>;

export type ExportActionFailure = Readonly<{
  status: "failure";
  code: string;
  message: string;
  failureCode?: string;
}>;

export type Export2LActionResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ExportActionFailure;

type ResolvedWriteContext =
  | Readonly<{ kind: "context"; context: ExportCaseRequestContext }>
  | Readonly<{
      kind: "failure";
      failure: Readonly<{
        status: "failure";
        code: string;
        message: string;
      }>;
    }>;

function resolveWriteContext(): ResolvedWriteContext {
  const configResult = parseExportRuntimeConfig();

  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: configResult.message,
      },
    };
  }

  if (configResult.config.mode === "development-preview") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "preview-writes-blocked",
        message: "Export writes are blocked in preview mode.",
      },
    };
  }

  const contextResult = resolveExportCaseRequestContext(configResult.config);

  if (contextResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: contextResult.code,
        message: contextResult.message,
      },
    };
  }

  return { kind: "context", context: contextResult.context };
}

function mapCaseResult(
  result: ExportCaseApiResult<ExportCaseWriteSuccess>,
): ExportCaseActionResult {
  if (result.status === "failure") {
    return {
      status: "failure",
      code: result.code,
      message: result.message,
      failureCode: result.failureCode,
    };
  }

  return { status: "success", value: result.value };
}

function mapDocumentResult(
  result: ExportCaseApiResult<ExportDocumentWriteSuccess>,
): ExportDocumentActionResult {
  if (result.status === "failure") {
    return {
      status: "failure",
      code: result.code,
      message: result.message,
      failureCode: result.failureCode,
    };
  }

  return { status: "success", value: result.value };
}

/** Plain async bridge for readers / order summary panels (not only form actions). */
export async function ensureExportCaseFromOrder(
  input: EnsureExportCaseFromOrderInput,
): Promise<ExportCaseActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!input.sourceOrderId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Kaynak sipariş kimliği zorunludur.",
    };
  }

  if (!input.isExport) {
    return {
      status: "failure",
      code: "not-export-order",
      message: "Sipariş ihracat siparişi değil.",
    };
  }

  if (!input.destinationCountryCode.trim()) {
    return {
      status: "failure",
      code: "destination-country-required",
      message: "Hedef ülke kodu zorunludur.",
    };
  }

  const facilityId =
    input.facilityId.trim() || resolved.context.facilityId;

  const result = await ensureExportCaseFromOrderApi(resolved.context, {
    ...input,
    facilityId,
    sourceOrderId: input.sourceOrderId.trim(),
    sourceOrderReference: input.sourceOrderReference.trim(),
    customerName: input.customerName.trim(),
    destinationCountryCode: input.destinationCountryCode.trim().toUpperCase(),
    originCountryCode: input.originCountryCode?.trim().toUpperCase() || undefined,
    incotermCode: input.incotermCode?.trim().toUpperCase() || undefined,
  });

  return mapCaseResult(result);
}

export async function ensureExportCaseFromOrderAction(
  input: EnsureExportCaseFromOrderInput,
): Promise<ExportCaseActionResult> {
  return ensureExportCaseFromOrder(input);
}

export async function registerExportDocumentAction(
  exportCaseId: string,
  input: RegisterExportDocumentInput,
): Promise<ExportDocumentActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!exportCaseId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "İhracat dosyası kimliği zorunludur.",
    };
  }

  if (!input.documentType.trim()) {
    return {
      status: "failure",
      code: "invalid-document-type",
      message: "Belge türü zorunludur.",
    };
  }

  if (!input.displayName.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Belge adı zorunludur.",
    };
  }

  const result = await registerExportDocument(
    resolved.context,
    exportCaseId.trim(),
    resolved.context.facilityId,
    {
      documentType: input.documentType.trim(),
      displayName: input.displayName.trim(),
      fileReference: input.fileReference?.trim() || undefined,
      documentNumber: input.documentNumber?.trim() || undefined,
      issuedAtUtc: input.issuedAtUtc?.trim() || undefined,
      expiresAtUtc: input.expiresAtUtc?.trim() || undefined,
      source: input.source?.trim() || "manual",
      initialStatus: input.initialStatus?.trim() || undefined,
    },
  );

  return mapDocumentResult(result);
}

export async function verifyExportDocumentAction(
  exportCaseId: string,
  documentId: string,
  expectedVersion: number,
): Promise<ExportDocumentActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!exportCaseId.trim() || !documentId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Dosya ve belge kimliği zorunludur.",
    };
  }

  if (!Number.isFinite(expectedVersion) || expectedVersion < 0) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Beklenen sürüm geçersiz.",
    };
  }

  const result = await verifyExportDocument(
    resolved.context,
    exportCaseId.trim(),
    documentId.trim(),
    resolved.context.facilityId,
    expectedVersion,
  );

  return mapDocumentResult(result);
}

export async function rejectExportDocumentAction(
  exportCaseId: string,
  documentId: string,
  expectedVersion: number,
  reason: string,
): Promise<ExportDocumentActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!exportCaseId.trim() || !documentId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Dosya ve belge kimliği zorunludur.",
    };
  }

  if (!reason.trim()) {
    return {
      status: "failure",
      code: "rejection-reason-required",
      message: "Red nedeni zorunludur.",
    };
  }

  if (!Number.isFinite(expectedVersion) || expectedVersion < 0) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Beklenen sürüm geçersiz.",
    };
  }

  const result = await rejectExportDocument(
    resolved.context,
    exportCaseId.trim(),
    documentId.trim(),
    resolved.context.facilityId,
    expectedVersion,
    reason.trim(),
  );

  return mapDocumentResult(result);
}

function map2LResult<T>(
  result: ExportCaseApiResult<T>,
): Export2LActionResult<T> {
  if (result.status === "failure") {
    return {
      status: "failure",
      code: result.code,
      message: result.message,
      failureCode: result.failureCode,
    };
  }

  return { status: "success", value: result.value };
}

function invalidRequest(message: string): ExportActionFailure {
  return { status: "failure", code: "invalid-request", message };
}

// ─── EXPORT-2L — Packing actions ─────────────────────────────────────────────

export async function createPackingUnitAction(
  exportCaseId: string,
  input: CreatePackingUnitInput,
): Promise<Export2LActionResult<PackingUnitWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!input.unitCode.trim() || !input.unitType.trim()) {
    return invalidRequest("Birim kodu ve tipi zorunludur.");
  }

  const result = await createExportPackingUnit(
    resolved.context,
    exportCaseId.trim(),
    {
      ...input,
      unitCode: input.unitCode.trim(),
      unitType: input.unitType.trim(),
      parentPackingUnitId: input.parentPackingUnitId?.trim() || undefined,
      weightUnit: input.weightUnit?.trim() || undefined,
      dimensionUnit: input.dimensionUnit?.trim() || undefined,
      marksAndNumbers: input.marksAndNumbers?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function updatePackingUnitAction(
  exportCaseId: string,
  packingUnitId: string,
  input: UpdatePackingUnitInput,
): Promise<Export2LActionResult<PackingUnitWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !packingUnitId.trim()) {
    return invalidRequest("Dosya ve paket birimi kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion) || input.expectedVersion < 0) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await updateExportPackingUnit(
    resolved.context,
    exportCaseId.trim(),
    packingUnitId.trim(),
    input,
  );

  return map2LResult(result);
}

export async function addPackingItemAction(
  exportCaseId: string,
  packingUnitId: string,
  input: AddPackingItemInput,
): Promise<Export2LActionResult<PackingItemWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !packingUnitId.trim()) {
    return invalidRequest("Dosya ve paket birimi kimliği zorunludur.");
  }
  if (
    !input.productCode.trim() ||
    !input.productName.trim() ||
    !input.unitOfMeasure.trim()
  ) {
    return invalidRequest("Ürün kodu, adı ve ölçü birimi zorunludur.");
  }
  if (!Number.isFinite(input.quantity) || input.quantity <= 0) {
    return invalidRequest("Miktar pozitif olmalıdır.");
  }

  const result = await addExportPackingItem(
    resolved.context,
    exportCaseId.trim(),
    packingUnitId.trim(),
    {
      productCode: input.productCode.trim(),
      productName: input.productName.trim(),
      quantity: input.quantity,
      unitOfMeasure: input.unitOfMeasure.trim(),
      sourceOrderLineId: input.sourceOrderLineId?.trim() || undefined,
      lotBatchReference: input.lotBatchReference?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function completePackingAction(
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<Export2LActionResult<PackingStatusWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!Number.isFinite(expectedCaseVersion) || expectedCaseVersion < 0) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await completeExportPacking(
    resolved.context,
    exportCaseId.trim(),
    expectedCaseVersion,
  );

  return map2LResult(result);
}

export async function verifyPackingAction(
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<Export2LActionResult<PackingStatusWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!Number.isFinite(expectedCaseVersion) || expectedCaseVersion < 0) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await verifyExportPacking(
    resolved.context,
    exportCaseId.trim(),
    expectedCaseVersion,
  );

  return map2LResult(result);
}

// ─── EXPORT-2L — Shipment actions ────────────────────────────────────────────

export async function createShipmentAction(
  exportCaseId: string,
  input: CreateShipmentInput,
): Promise<Export2LActionResult<ShipmentWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!input.shipmentReference.trim()) {
    return invalidRequest("Sevkiyat referansı zorunludur.");
  }
  if (!input.destinationCountryCode.trim()) {
    return invalidRequest("Hedef ülke kodu zorunludur.");
  }

  const result = await createExportShipment(
    resolved.context,
    exportCaseId.trim(),
    {
      ...input,
      shipmentReference: input.shipmentReference.trim(),
      destinationCountryCode: input.destinationCountryCode.trim().toUpperCase(),
      transportMode: input.transportMode?.trim() || undefined,
      incotermCode: input.incotermCode?.trim().toUpperCase() || undefined,
      destinationCity: input.destinationCity?.trim() || undefined,
      deliveryAddress: input.deliveryAddress?.trim() || undefined,
      forwarderName: input.forwarderName?.trim() || undefined,
      carrierName: input.carrierName?.trim() || undefined,
      bookingReference: input.bookingReference?.trim() || undefined,
      plannedPickupAtUtc: input.plannedPickupAtUtc?.trim() || undefined,
      plannedDepartureAtUtc: input.plannedDepartureAtUtc?.trim() || undefined,
      plannedArrivalAtUtc: input.plannedArrivalAtUtc?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function updateShipmentAction(
  exportCaseId: string,
  shipmentId: string,
  input: UpdateShipmentInput,
): Promise<Export2LActionResult<ShipmentWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !shipmentId.trim()) {
    return invalidRequest("Dosya ve sevkiyat kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion) || input.expectedVersion < 0) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await updateExportShipment(
    resolved.context,
    exportCaseId.trim(),
    shipmentId.trim(),
    {
      ...input,
      destinationCountryCode: input.destinationCountryCode
        ?.trim()
        .toUpperCase(),
      transportMode: input.transportMode?.trim() || undefined,
      incotermCode: input.incotermCode?.trim().toUpperCase() || undefined,
    },
  );

  return map2LResult(result);
}

export async function transitionShipmentAction(
  exportCaseId: string,
  shipmentId: string,
  input: TransitionShipmentInput,
): Promise<Export2LActionResult<ShipmentWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !shipmentId.trim()) {
    return invalidRequest("Dosya ve sevkiyat kimliği zorunludur.");
  }
  if (!input.targetStatus.trim()) {
    return invalidRequest("Hedef durum zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion) || input.expectedVersion < 0) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await transitionExportShipment(
    resolved.context,
    exportCaseId.trim(),
    shipmentId.trim(),
    {
      expectedVersion: input.expectedVersion,
      targetStatus: input.targetStatus.trim(),
      cancellationReason: input.cancellationReason?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

// ─── EXPORT-2L — Broker actions ──────────────────────────────────────────────

export async function createBrokerWorkspaceAction(
  exportCaseId: string,
  input: CreateBrokerWorkspaceInput,
): Promise<Export2LActionResult<BrokerWorkspaceWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!input.brokerCompanyName.trim()) {
    return invalidRequest("Broker şirket adı zorunludur.");
  }

  const result = await createExportBrokerWorkspace(
    resolved.context,
    exportCaseId.trim(),
    {
      shipmentId: input.shipmentId?.trim() || undefined,
      brokerCompanyName: input.brokerCompanyName.trim(),
      brokerContactName: input.brokerContactName?.trim() || undefined,
      brokerEmail: input.brokerEmail?.trim() || undefined,
      brokerPhone: input.brokerPhone?.trim() || undefined,
      notes: input.notes?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function prepareBrokerPackageAction(
  exportCaseId: string,
  workspaceId: string,
  input: PrepareBrokerPackageInput,
): Promise<Export2LActionResult<BrokerPackageWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !workspaceId.trim()) {
    return invalidRequest("Dosya ve workspace kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedWorkspaceVersion)) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await prepareExportBrokerPackage(
    resolved.context,
    exportCaseId.trim(),
    workspaceId.trim(),
    {
      expectedWorkspaceVersion: input.expectedWorkspaceVersion,
      documentIds: input.documentIds,
      notes: input.notes?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function sendBrokerPackageAction(
  exportCaseId: string,
  workspaceId: string,
  expectedWorkspaceVersion: number,
): Promise<Export2LActionResult<BrokerPackageWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !workspaceId.trim()) {
    return invalidRequest("Dosya ve workspace kimliği zorunludur.");
  }
  if (!Number.isFinite(expectedWorkspaceVersion)) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await sendExportBrokerPackage(
    resolved.context,
    exportCaseId.trim(),
    workspaceId.trim(),
    expectedWorkspaceVersion,
  );

  return map2LResult(result);
}

export async function acknowledgeBrokerAction(
  exportCaseId: string,
  workspaceId: string,
  expectedWorkspaceVersion: number,
  brokerReference?: string,
): Promise<Export2LActionResult<BrokerWorkspaceWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !workspaceId.trim()) {
    return invalidRequest("Dosya ve workspace kimliği zorunludur.");
  }
  if (!Number.isFinite(expectedWorkspaceVersion)) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await acknowledgeExportBroker(
    resolved.context,
    exportCaseId.trim(),
    workspaceId.trim(),
    expectedWorkspaceVersion,
    brokerReference?.trim() || undefined,
  );

  return map2LResult(result);
}

export async function createBrokerCorrectionAction(
  exportCaseId: string,
  workspaceId: string,
  input: CreateBrokerCorrectionInput,
): Promise<Export2LActionResult<BrokerCorrectionWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !workspaceId.trim()) {
    return invalidRequest("Dosya ve workspace kimliği zorunludur.");
  }
  if (
    !input.category.trim() ||
    !input.title.trim() ||
    !input.description.trim() ||
    !input.severity.trim()
  ) {
    return invalidRequest("Kategori, başlık, açıklama ve önem zorunludur.");
  }

  const result = await createExportBrokerCorrection(
    resolved.context,
    exportCaseId.trim(),
    workspaceId.trim(),
    {
      category: input.category.trim(),
      title: input.title.trim(),
      description: input.description.trim(),
      severity: input.severity.trim(),
      relatedDocumentId: input.relatedDocumentId?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

export async function resolveBrokerCorrectionAction(
  exportCaseId: string,
  workspaceId: string,
  correctionId: string,
  input: ResolveBrokerCorrectionInput,
): Promise<Export2LActionResult<BrokerCorrectionWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !workspaceId.trim() || !correctionId.trim()) {
    return invalidRequest("Dosya, workspace ve düzeltme kimliği zorunludur.");
  }
  if (!input.resolution.trim()) {
    return invalidRequest("Çözüm durumu zorunludur.");
  }
  if (!Number.isFinite(input.expectedWorkspaceVersion)) {
    return invalidRequest("Beklenen workspace sürümü zorunludur.");
  }

  const result = await resolveExportBrokerCorrection(
    resolved.context,
    exportCaseId.trim(),
    workspaceId.trim(),
    correctionId.trim(),
    {
      expectedWorkspaceVersion: input.expectedWorkspaceVersion,
      resolution: input.resolution.trim(),
      resolutionNote: input.resolutionNote?.trim() || undefined,
    },
  );

  return map2LResult(result);
}

// ─── EXPORT-3L — Compliance actions ──────────────────────────────────────────

export async function evaluateComplianceAction(
  exportCaseId: string,
  input: EvaluateComplianceInput,
): Promise<Export2LActionResult<ComplianceWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedCaseVersion)) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await evaluateExportCompliance(
    resolved.context,
    exportCaseId.trim(),
    input,
  );

  return map2LResult(result);
}

export async function revalidateComplianceAction(
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<Export2LActionResult<ComplianceWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }
  if (!Number.isFinite(expectedCaseVersion)) {
    return invalidRequest("Beklenen sürüm geçersiz.");
  }

  const result = await revalidateExportCompliance(
    resolved.context,
    exportCaseId.trim(),
    expectedCaseVersion,
  );

  return map2LResult(result);
}

export async function satisfyComplianceRequirementAction(
  exportCaseId: string,
  requirementId: string,
  input: SatisfyComplianceRequirementInput,
): Promise<Export2LActionResult<ComplianceRequirementWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !requirementId.trim()) {
    return invalidRequest("Dosya ve gereksinim kimliği zorunludur.");
  }
  if (!input.resolutionType.trim() || !input.resolutionNote.trim()) {
    return invalidRequest("Çözüm tipi ve notu zorunludur.");
  }
  if (!Number.isFinite(input.expectedRequirementVersion)) {
    return invalidRequest("Beklenen gereksinim sürümü geçersiz.");
  }

  const result = await satisfyExportComplianceRequirement(
    resolved.context,
    exportCaseId.trim(),
    requirementId.trim(),
    {
      expectedRequirementVersion: input.expectedRequirementVersion,
      resolutionType: input.resolutionType.trim(),
      resolutionNote: input.resolutionNote.trim(),
    },
  );

  return map2LResult(result);
}

// ─── EXPORT-3L — Research actions ────────────────────────────────────────────

export async function createResearchCaseAction(
  input: CreateResearchCaseInput,
): Promise<Export2LActionResult<ResearchWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!input.countryCode.trim()) {
    return invalidRequest("Ülke kodu zorunludur.");
  }

  const result = await createCountryResearchCase(resolved.context, {
    countryCode: input.countryCode.trim().toUpperCase(),
    productReference: input.productReference?.trim() || undefined,
    productDescription: input.productDescription?.trim() || undefined,
    productGroup: input.productGroup?.trim() || undefined,
    hsCode: input.hsCode?.trim() || undefined,
    originCountryCode: input.originCountryCode?.trim().toUpperCase() || undefined,
    purposeNotes: input.purposeNotes?.trim() || undefined,
  });

  return map2LResult(result);
}

export async function addResearchFindingAction(
  researchCaseId: string,
  input: AddResearchFindingInput,
): Promise<Export2LActionResult<ResearchFindingWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!researchCaseId.trim()) {
    return invalidRequest("Araştırma dosyası kimliği zorunludur.");
  }
  if (
    !input.category.trim() ||
    !input.title.trim() ||
    !input.summary.trim() ||
    !input.sourcePublisher.trim() ||
    !input.sourceTitle.trim()
  ) {
    return invalidRequest("Kategori, başlık, özet ve kaynak bilgileri zorunludur.");
  }

  const result = await addCountryResearchFinding(
    resolved.context,
    researchCaseId.trim(),
    input,
  );

  return map2LResult(result);
}

export async function reviewResearchCaseAction(
  researchCaseId: string,
  expectedVersion: number,
): Promise<Export2LActionResult<ResearchWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!researchCaseId.trim()) {
    return invalidRequest("Araştırma dosyası kimliği zorunludur.");
  }

  const result = await reviewCountryResearchCase(
    resolved.context,
    researchCaseId.trim(),
    expectedVersion,
  );

  return map2LResult(result);
}

export async function completeResearchCaseAction(
  researchCaseId: string,
  expectedVersion: number,
): Promise<Export2LActionResult<ResearchWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!researchCaseId.trim()) {
    return invalidRequest("Araştırma dosyası kimliği zorunludur.");
  }

  const result = await completeCountryResearchCase(
    resolved.context,
    researchCaseId.trim(),
    expectedVersion,
  );

  return map2LResult(result);
}

// ─── EXPORT-3L — Knowledge actions ───────────────────────────────────────────

export async function createKnowledgeEntryAction(
  input: CreateKnowledgeEntryInput,
): Promise<Export2LActionResult<KnowledgeWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (
    !input.countryCode.trim() ||
    !input.category.trim() ||
    !input.title.trim() ||
    !input.summary.trim() ||
    !input.sourcePublisher.trim() ||
    !input.sourceTitle.trim() ||
    !input.authority.trim()
  ) {
    return invalidRequest("Zorunlu bilgi alanları eksik.");
  }

  const result = await createCountryKnowledgeEntry(resolved.context, {
    ...input,
    countryCode: input.countryCode.trim().toUpperCase(),
    category: input.category.trim(),
    title: input.title.trim(),
    summary: input.summary.trim(),
    sourcePublisher: input.sourcePublisher.trim(),
    sourceTitle: input.sourceTitle.trim(),
    sourceUrl: input.sourceUrl?.trim() || undefined,
    authority: input.authority.trim(),
    productScope: input.productScope?.trim() || undefined,
    productGroup: input.productGroup?.trim() || undefined,
    hsCodePattern: input.hsCodePattern?.trim() || undefined,
    createdFromResearchCaseId:
      input.createdFromResearchCaseId?.trim() || undefined,
  });

  return map2LResult(result);
}

export async function verifyKnowledgeEntryAction(
  knowledgeEntryId: string,
  expectedVersion: number,
): Promise<Export2LActionResult<KnowledgeWriteSuccess>> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!knowledgeEntryId.trim()) {
    return invalidRequest("Bilgi kaydı kimliği zorunludur.");
  }

  const result = await verifyCountryKnowledgeEntry(
    resolved.context,
    knowledgeEntryId.trim(),
    expectedVersion,
  );

  return map2LResult(result);
}
