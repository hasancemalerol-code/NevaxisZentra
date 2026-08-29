import type {
  MaterialAvailabilityRiskDetail,
  MaterialAvailabilityRiskDriver,
  MaterialAvailabilityRiskEvaluateResult,
  MaterialAvailabilityRiskEvidence,
  MaterialAvailabilityRiskHistoryEntry,
  MaterialAvailabilityRiskRow,
  MaterialAvailabilityRiskStatus,
  MaterialAvailabilityRiskWorkspacePayload,
} from "../domain/material-availability-risk";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

const RISK_STATUSES = new Set<MaterialAvailabilityRiskStatus>([
  "sufficient",
  "shortage",
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

function parseBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function parseRiskStatus(value: unknown): ParseResult<MaterialAvailabilityRiskStatus> {
  if (typeof value !== "string" || !RISK_STATUSES.has(value as MaterialAvailabilityRiskStatus)) {
    return { status: "failure", message: "status is invalid." };
  }
  return { status: "success", value: value as MaterialAvailabilityRiskStatus };
}

function parseDriver(value: unknown): ParseResult<MaterialAvailabilityRiskDriver> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "driver is invalid." };
  }
  const row = value as Record<string, unknown>;
  const code = parseString(row.code, "code");
  if (code.status === "failure") return code;
  const label = parseString(row.label, "label");
  if (label.status === "failure") return label;
  const state = parseString(row.state, "state");
  if (state.status === "failure") return state;
  return {
    status: "success",
    value: {
      code: code.value,
      label: label.value,
      value: parseOptionalString(row.value),
      state: state.value,
    },
  };
}

function parseEvidence(value: unknown): ParseResult<MaterialAvailabilityRiskEvidence> {
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
      isMissing: parseBoolean(row.isMissing),
    },
  };
}

function parseRow(value: unknown): ParseResult<MaterialAvailabilityRiskRow> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "assessment row is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const materialId = parseString(row.materialId, "materialId");
  if (materialId.status === "failure") return materialId;
  const status = parseRiskStatus(row.status);
  if (status.status === "failure") return status;
  const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
  if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
  const version = parseNumber(row.version);
  if (version === undefined) {
    return { status: "failure", message: "version is required." };
  }

  return {
    status: "success",
    value: {
      id: id.value,
      materialId: materialId.value,
      materialCode: typeof row.materialCode === "string" ? row.materialCode : "",
      materialName: typeof row.materialName === "string" ? row.materialName : "",
      requirementProfileId: parseOptionalString(row.requirementProfileId),
      productionOrderReference: parseOptionalString(row.productionOrderReference),
      status: status.value,
      requiredQuantityGrams: parseNumber(row.requiredQuantityGrams),
      erpAvailableQuantity: parseNumber(row.erpAvailableQuantity),
      erpReservedQuantity: parseNumber(row.erpReservedQuantity),
      erpUnit: parseOptionalString(row.erpUnit),
      availableAfterReservationQuantity: parseNumber(row.availableAfterReservationQuantity),
      shortageQuantity: parseNumber(row.shortageQuantity),
      sourceFreshness: parseOptionalString(row.sourceFreshness),
      evaluatedAtUtc: evaluatedAtUtc.value,
      version,
    },
  };
}

export function parseMaterialAvailabilityRiskWorkspacePayload(
  payload: unknown,
): ParseResult<MaterialAvailabilityRiskWorkspacePayload> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "workspace payload is invalid." };
  }
  const data = payload as Record<string, unknown>;
  const ruleSetVersion = typeof data.ruleSetVersion === "string" ? data.ruleSetVersion : "";
  const automationDisclaimer =
    typeof data.automationDisclaimer === "string" ? data.automationDisclaimer : "";
  const kpisRaw = data.kpis;
  if (!kpisRaw || typeof kpisRaw !== "object") {
    return { status: "failure", message: "kpis is required." };
  }
  const kpis = kpisRaw as Record<string, unknown>;
  const capabilitiesRaw = data.capabilities;
  if (!capabilitiesRaw || typeof capabilitiesRaw !== "object") {
    return { status: "failure", message: "capabilities is required." };
  }
  const capabilities = capabilitiesRaw as Record<string, unknown>;
  if (!Array.isArray(data.assessments)) {
    return { status: "failure", message: "assessments is required." };
  }

  const assessments: MaterialAvailabilityRiskRow[] = [];
  for (const item of data.assessments) {
    const parsed = parseRow(item);
    if (parsed.status === "failure") return parsed;
    assessments.push(parsed.value);
  }

  return {
    status: "success",
    value: {
      ruleSetVersion,
      automationDisclaimer,
      kpis: {
        shortageCount: parseNumber(kpis.shortageCount) ?? 0,
        sufficientCount: parseNumber(kpis.sufficientCount) ?? 0,
        insufficientDataCount: parseNumber(kpis.insufficientDataCount) ?? 0,
        sourceConflictCount: parseNumber(kpis.sourceConflictCount) ?? 0,
        latestEvaluatedAtUtc: parseOptionalString(kpis.latestEvaluatedAtUtc),
      },
      capabilities: {
        canRead: parseBoolean(capabilities.canRead),
        canManage: parseBoolean(capabilities.canManage),
      },
      assessments,
    },
  };
}

export function parseMaterialAvailabilityRiskDetail(payload: unknown): ParseResult<MaterialAvailabilityRiskDetail> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "detail payload is invalid." };
  }
  const row = payload as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const facilityId = parseString(row.facilityId, "facilityId");
  if (facilityId.status === "failure") return facilityId;
  const materialId = parseString(row.materialId, "materialId");
  if (materialId.status === "failure") return materialId;
  const status = parseRiskStatus(row.status);
  if (status.status === "failure") return status;
  const ruleSetVersion = parseString(row.ruleSetVersion, "ruleSetVersion");
  if (ruleSetVersion.status === "failure") return ruleSetVersion;
  const inputFingerprint = parseString(row.inputFingerprint, "inputFingerprint");
  if (inputFingerprint.status === "failure") return inputFingerprint;
  const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
  if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
  const createdAtUtc = parseString(row.createdAtUtc, "createdAtUtc");
  if (createdAtUtc.status === "failure") return createdAtUtc;
  const version = parseNumber(row.version);
  if (version === undefined) {
    return { status: "failure", message: "version is required." };
  }

  const drivers: MaterialAvailabilityRiskDriver[] = [];
  if (Array.isArray(row.drivers)) {
    for (const item of row.drivers) {
      const parsed = parseDriver(item);
      if (parsed.status === "failure") return parsed;
      drivers.push(parsed.value);
    }
  }

  const evidence: MaterialAvailabilityRiskEvidence[] = [];
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
      materialId: materialId.value,
      materialCode: typeof row.materialCode === "string" ? row.materialCode : "",
      materialName: typeof row.materialName === "string" ? row.materialName : "",
      requirementProfileId: parseOptionalString(row.requirementProfileId),
      productionOrderReference: parseOptionalString(row.productionOrderReference),
      productReference: parseOptionalString(row.productReference),
      moldReference: parseOptionalString(row.moldReference),
      status: status.value,
      ruleSetVersion: ruleSetVersion.value,
      inputFingerprint: inputFingerprint.value,
      requiredQuantityGrams: parseNumber(row.requiredQuantityGrams),
      erpAvailableQuantity: parseNumber(row.erpAvailableQuantity),
      erpReservedQuantity: parseNumber(row.erpReservedQuantity),
      erpUnit: parseOptionalString(row.erpUnit),
      availableAfterReservationQuantity: parseNumber(row.availableAfterReservationQuantity),
      shortageQuantity: parseNumber(row.shortageQuantity),
      erpSourceUpdatedAtUtc: parseOptionalString(row.erpSourceUpdatedAtUtc),
      sourceFreshness: parseOptionalString(row.sourceFreshness),
      drivers,
      evidence,
      evaluatedAtUtc: evaluatedAtUtc.value,
      createdAtUtc: createdAtUtc.value,
      version,
    },
  };
}

export function parseMaterialAvailabilityRiskHistory(
  payload: unknown,
): ParseResult<readonly MaterialAvailabilityRiskHistoryEntry[]> {
  if (!Array.isArray(payload)) {
    return { status: "failure", message: "history payload is invalid." };
  }

  const history: MaterialAvailabilityRiskHistoryEntry[] = [];
  for (const item of payload) {
    if (!item || typeof item !== "object") {
      return { status: "failure", message: "history entry is invalid." };
    }
    const row = item as Record<string, unknown>;
    const id = parseString(row.id, "id");
    if (id.status === "failure") return id;
    const status = parseRiskStatus(row.status);
    if (status.status === "failure") return status;
    const evaluatedAtUtc = parseString(row.evaluatedAtUtc, "evaluatedAtUtc");
    if (evaluatedAtUtc.status === "failure") return evaluatedAtUtc;
    const ruleSetVersion = parseString(row.ruleSetVersion, "ruleSetVersion");
    if (ruleSetVersion.status === "failure") return ruleSetVersion;
    history.push({
      id: id.value,
      status: status.value,
      evaluatedAtUtc: evaluatedAtUtc.value,
      ruleSetVersion: ruleSetVersion.value,
    });
  }

  return { status: "success", value: history };
}

export function parseMaterialAvailabilityRiskEvaluateResponse(
  payload: unknown,
): ParseResult<MaterialAvailabilityRiskEvaluateResult> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "evaluate response is invalid." };
  }
  const row = payload as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const version = parseNumber(row.version);
  if (version === undefined) {
    return { status: "failure", message: "version is required." };
  }
  return {
    status: "success",
    value: {
      assessmentId: id.value,
      version,
      idempotentReplay: parseBoolean(row.idempotentReplay),
    },
  };
}
