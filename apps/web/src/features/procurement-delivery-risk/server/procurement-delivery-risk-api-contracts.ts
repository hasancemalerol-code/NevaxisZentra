import type {
  ProcurementDeliveryRiskDetail,
  ProcurementDeliveryRiskDriver,
  ProcurementDeliveryRiskEvaluateResult,
  ProcurementDeliveryRiskEvidence,
  ProcurementDeliveryRiskHistoryEntry,
  ProcurementDeliveryRiskResult,
  ProcurementDeliveryRiskRow,
  ProcurementDeliveryRiskWorkspacePayload,
} from "../domain/procurement-delivery-risk";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

const RESULTS = new Set<ProcurementDeliveryRiskResult>([
  "on-track",
  "delivery-risk",
  "overdue",
  "insufficient-data",
  "source-conflict",
]);

function parseString(value: unknown, field: string): ParseResult<string> {
  if (typeof value !== "string" || value.trim().length === 0) {
    return { status: "failure", message: `${field} is required.` };
  }
  return { status: "success", value };
}

function parseOptionalString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function parseNumber(value: unknown): number | undefined {
  return typeof value === "number" && !Number.isNaN(value) ? value : undefined;
}

function parseResult(value: unknown): ParseResult<ProcurementDeliveryRiskResult> {
  if (typeof value !== "string" || !RESULTS.has(value as ProcurementDeliveryRiskResult)) {
    return { status: "failure", message: "result is invalid." };
  }
  return { status: "success", value: value as ProcurementDeliveryRiskResult };
}

function parseDriver(value: unknown): ParseResult<ProcurementDeliveryRiskDriver> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "driver is invalid." };
  }
  const row = value as Record<string, unknown>;
  const code = parseString(row.code, "code");
  if (code.status === "failure") return code;
  const summary = parseString(row.summary, "summary");
  if (summary.status === "failure") return summary;
  const state = parseString(row.state, "state");
  if (state.status === "failure") return state;
  return {
    status: "success",
    value: {
      code: code.value,
      summary: summary.value,
      reference: parseOptionalString(row.reference),
      state: state.value,
    },
  };
}

function parseEvidence(value: unknown): ParseResult<ProcurementDeliveryRiskEvidence> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "evidence is invalid." };
  }
  const row = value as Record<string, unknown>;
  const field = parseString(row.field, "field");
  if (field.status === "failure") return field;
  return {
    status: "success",
    value: {
      field: field.value,
      value: parseOptionalString(row.value),
      source: parseOptionalString(row.source),
      recordedAtUtc: parseOptionalString(row.recordedAtUtc),
      isUnknown: row.isUnknown === true,
    },
  };
}

function parseRow(value: unknown): ParseResult<ProcurementDeliveryRiskRow> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "assessment row is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const purchaseOrderId = parseString(row.purchaseOrderId, "purchaseOrderId");
  if (purchaseOrderId.status === "failure") return purchaseOrderId;
  const purchaseOrderLineId = parseString(row.purchaseOrderLineId, "purchaseOrderLineId");
  if (purchaseOrderLineId.status === "failure") return purchaseOrderLineId;
  const purchaseOrderNumber = parseString(row.purchaseOrderNumber, "purchaseOrderNumber");
  if (purchaseOrderNumber.status === "failure") return purchaseOrderNumber;
  const result = parseResult(row.result);
  if (result.status === "failure") return result;
  const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
  if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
  const version = parseNumber(row.version);
  if (version === undefined) return { status: "failure", message: "version is required." };
  return {
    status: "success",
    value: {
      id: id.value,
      purchaseOrderId: purchaseOrderId.value,
      purchaseOrderLineId: purchaseOrderLineId.value,
      purchaseOrderNumber: purchaseOrderNumber.value,
      externalSupplierCode: parseOptionalString(row.externalSupplierCode),
      supplierName: parseOptionalString(row.supplierName),
      commitmentDateUtc: parseOptionalString(row.commitmentDateUtc),
      receivedQuantity: parseNumber(row.receivedQuantity),
      openQuantity: parseNumber(row.openQuantity),
      unit: parseOptionalString(row.unit),
      result: result.value,
      sourceFreshness: parseOptionalString(row.sourceFreshness),
      evaluatedAtUtc: evaluatedAtUtc.value,
      version,
    },
  };
}

export function parseProcurementDeliveryRiskWorkspacePayload(payload: unknown): ParseResult<ProcurementDeliveryRiskWorkspacePayload> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "workspace payload is invalid." };
  }
  const root = payload as Record<string, unknown>;
  const ruleSetVersion = parseString(root.ruleSetVersion, "ruleSetVersion");
  if (ruleSetVersion.status === "failure") return ruleSetVersion;
  const automationDisclaimer = parseString(root.automationDisclaimer, "automationDisclaimer");
  if (automationDisclaimer.status === "failure") return automationDisclaimer;
  if (!root.kpis || typeof root.kpis !== "object") {
    return { status: "failure", message: "kpis is invalid." };
  }
  const kpis = root.kpis as Record<string, unknown>;
  if (!root.capabilities || typeof root.capabilities !== "object") {
    return { status: "failure", message: "capabilities is invalid." };
  }
  const capabilities = root.capabilities as Record<string, unknown>;
  if (!Array.isArray(root.assessments)) {
    return { status: "failure", message: "assessments is invalid." };
  }
  const assessments: ProcurementDeliveryRiskRow[] = [];
  for (const item of root.assessments) {
    const parsed = parseRow(item);
    if (parsed.status === "failure") return parsed;
    assessments.push(parsed.value);
  }
  return {
    status: "success",
    value: {
      ruleSetVersion: ruleSetVersion.value,
      automationDisclaimer: automationDisclaimer.value,
      kpis: {
        overdueCount: parseNumber(kpis.overdueCount) ?? 0,
        deliveryRiskCount: parseNumber(kpis.deliveryRiskCount) ?? 0,
        onTrackCount: parseNumber(kpis.onTrackCount) ?? 0,
        insufficientDataCount: parseNumber(kpis.insufficientDataCount) ?? 0,
        sourceConflictCount: parseNumber(kpis.sourceConflictCount) ?? 0,
        latestEvaluatedAtUtc: parseOptionalString(kpis.latestEvaluatedAtUtc),
      },
      capabilities: {
        canRead: capabilities.canRead === true,
        canManage: capabilities.canManage === true,
      },
      assessments,
    },
  };
}

export function parseProcurementDeliveryRiskDetail(payload: unknown): ParseResult<ProcurementDeliveryRiskDetail> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "detail payload is invalid." };
  }
  const row = payload as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const facilityId = parseString(row.facilityId, "facilityId");
  if (facilityId.status === "failure") return facilityId;
  const purchaseOrderId = parseString(row.purchaseOrderId, "purchaseOrderId");
  if (purchaseOrderId.status === "failure") return purchaseOrderId;
  const purchaseOrderLineId = parseString(row.purchaseOrderLineId, "purchaseOrderLineId");
  if (purchaseOrderLineId.status === "failure") return purchaseOrderLineId;
  const purchaseOrderNumber = parseString(row.purchaseOrderNumber, "purchaseOrderNumber");
  if (purchaseOrderNumber.status === "failure") return purchaseOrderNumber;
  const orderStatus = parseString(row.orderStatus, "orderStatus");
  if (orderStatus.status === "failure") return orderStatus;
  const lineNumber = parseNumber(row.lineNumber);
  if (lineNumber === undefined) return { status: "failure", message: "lineNumber is required." };
  const result = parseResult(row.result);
  if (result.status === "failure") return result;
  const ruleSetVersion = parseString(row.ruleSetVersion, "ruleSetVersion");
  if (ruleSetVersion.status === "failure") return ruleSetVersion;
  const inputFingerprint = parseString(row.inputFingerprint, "inputFingerprint");
  if (inputFingerprint.status === "failure") return inputFingerprint;
  const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
  if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
  const version = parseNumber(row.version);
  if (version === undefined) return { status: "failure", message: "version is required." };
  const drivers: ProcurementDeliveryRiskDriver[] = [];
  if (Array.isArray(row.drivers)) {
    for (const item of row.drivers) {
      const parsed = parseDriver(item);
      if (parsed.status === "failure") return parsed;
      drivers.push(parsed.value);
    }
  }
  const evidence: ProcurementDeliveryRiskEvidence[] = [];
  if (Array.isArray(row.evidence)) {
    for (const item of row.evidence) {
      const parsed = parseEvidence(item);
      if (parsed.status === "failure") return parsed;
      evidence.push(parsed.value);
    }
  }
  return {
    status: "success",
    value: {
      id: id.value,
      facilityId: facilityId.value,
      purchaseOrderId: purchaseOrderId.value,
      purchaseOrderLineId: purchaseOrderLineId.value,
      purchaseOrderNumber: purchaseOrderNumber.value,
      externalSupplierId: parseOptionalString(row.externalSupplierId),
      externalSupplierCode: parseOptionalString(row.externalSupplierCode),
      supplierName: parseOptionalString(row.supplierName),
      orderStatus: orderStatus.value,
      lineNumber,
      externalProductId: parseOptionalString(row.externalProductId),
      externalProductCode: parseOptionalString(row.externalProductCode),
      lineDescription: parseOptionalString(row.lineDescription),
      commitmentDateUtc: parseOptionalString(row.commitmentDateUtc),
      orderedQuantity: parseNumber(row.orderedQuantity),
      receivedQuantity: parseNumber(row.receivedQuantity),
      openQuantity: parseNumber(row.openQuantity),
      unit: parseOptionalString(row.unit),
      result: result.value,
      ruleSetVersion: ruleSetVersion.value,
      inputFingerprint: inputFingerprint.value,
      sourceUpdatedAtUtc: parseOptionalString(row.sourceUpdatedAtUtc),
      sourceFreshness: parseOptionalString(row.sourceFreshness),
      drivers,
      evidence,
      evaluatedAtUtc: evaluatedAtUtc.value,
      version,
    },
  };
}

export function parseProcurementDeliveryRiskHistory(payload: unknown): ParseResult<readonly ProcurementDeliveryRiskHistoryEntry[]> {
  if (!Array.isArray(payload)) {
    return { status: "failure", message: "history payload is invalid." };
  }
  const entries: ProcurementDeliveryRiskHistoryEntry[] = [];
  for (const item of payload) {
    if (!item || typeof item !== "object") {
      return { status: "failure", message: "history entry is invalid." };
    }
    const row = item as Record<string, unknown>;
    const id = parseString(row.id, "id");
    if (id.status === "failure") return id;
    const result = parseResult(row.result);
    if (result.status === "failure") return result;
    const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
    if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
    const ruleSetVersion = parseString(row.ruleSetVersion, "ruleSetVersion");
    if (ruleSetVersion.status === "failure") return ruleSetVersion;
    entries.push({
      id: id.value,
      result: result.value,
      evaluatedAtUtc: evaluatedAtUtc.value,
      ruleSetVersion: ruleSetVersion.value,
    });
  }
  return { status: "success", value: entries };
}

export function parseProcurementDeliveryRiskEvaluateResponse(payload: unknown): ParseResult<ProcurementDeliveryRiskEvaluateResult> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "evaluate response is invalid." };
  }
  const row = payload as Record<string, unknown>;
  const assessmentId = parseString(row.assessmentId, "assessmentId");
  if (assessmentId.status === "failure") return assessmentId;
  const version = parseNumber(row.version);
  if (version === undefined) return { status: "failure", message: "version is required." };
  return {
    status: "success",
    value: {
      assessmentId: assessmentId.value,
      version,
      idempotentReplay: row.idempotentReplay === true,
    },
  };
}
