import { classifyApiFailure, failureForRefusal } from "@/lib/api/api-failure-classifier";
import "./export-case-server-boundary";

import type {
  AddPackingItemInput,
  CreateBrokerCorrectionInput,
  CreateBrokerWorkspaceInput,
  CreatePackingUnitInput,
  CreateShipmentInput,
  ExportBrokerWorkspace,
  ExportPackingReconciliation,
  ExportPackingWorkspace,
  ExportShipment,
  ExportShipmentReadiness,
  PrepareBrokerPackageInput,
  ResolveBrokerCorrectionInput,
  TransitionShipmentInput,
  UpdatePackingUnitInput,
  UpdateShipmentInput,
} from "../domain/export-2l";
import type {
  EnsureExportCaseFromOrderInput,
  ExportActivity,
  ExportCaseDetail,
  ExportCaseSummary,
  ExportCaseWriteSuccess,
  ExportDocument,
  ExportReadiness,
  RegisterExportDocumentInput,
} from "../domain/export-case";
import {
  parseBrokerCorrectionWriteSuccessApiResponse,
  parseBrokerPackageWriteSuccessApiResponse,
  parseBrokerWorkspaceWriteSuccessApiResponse,
  parseExportBrokerWorkspaceApiResponse,
  parseExportPackingReconciliationApiResponse,
  parseExportPackingWorkspaceApiResponse,
  parseExportShipmentApiResponse,
  parseExportShipmentListApiResponse,
  parseExportShipmentReadinessApiResponse,
  parsePackingItemWriteSuccessApiResponse,
  parsePackingStatusWriteSuccessApiResponse,
  parsePackingUnitWriteSuccessApiResponse,
  parseShipmentWriteSuccessApiResponse,
  type BrokerCorrectionWriteSuccess,
  type BrokerPackageWriteSuccess,
  type BrokerWorkspaceWriteSuccess,
  type PackingItemWriteSuccess,
  type PackingStatusWriteSuccess,
  type PackingUnitWriteSuccess,
  type ShipmentWriteSuccess,
} from "./export-2l-api-contracts";
import {
  parseComplianceRequirementWriteSuccessApiResponse,
  parseComplianceWriteSuccessApiResponse,
  parseCountryKnowledgeListApiResponse,
  parseCountryResearchDetailApiResponse,
  parseCountryResearchListApiResponse,
  parseExportComplianceSummaryApiResponse,
  parseExportTraceabilityApiResponse,
  parseKnowledgeWriteSuccessApiResponse,
  parseResearchFindingWriteSuccessApiResponse,
  parseResearchWriteSuccessApiResponse,
} from "./export-3l-api-contracts";
import {
  parseExportActivityListApiResponse,
  parseExportCaseDetailApiResponse,
  parseExportCaseListApiResponse,
  parseExportCaseWriteSuccessApiResponse,
  parseExportDocumentListApiResponse,
  parseExportDocumentWriteSuccessApiResponse,
  parseExportReadinessApiResponse,
  type ExportDocumentWriteSuccess,
} from "./export-case-api-contracts";
import type { ExportCaseRequestContext } from "./export-case-request-context";

export type ExportCaseApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "subscription-required"
    | "not-found"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "conflict";
  message: string;
  retryable: boolean;
  failureCode?: string;
}>;

export type ExportCaseApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ExportCaseApiFailure;

function fail(
  code: ExportCaseApiFailure["code"],
  message: string,
  retryable: boolean,
  failureCode?: string,
): ExportCaseApiFailure {
  return { status: "failure", code, message, retryable, failureCode };
}

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

async function readResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();

  if (!text.trim()) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function extractFailureCode(body: unknown): string | undefined {
  if (typeof body !== "object" || body === null) {
    return undefined;
  }

  const record = body as Record<string, unknown>;

  if (typeof record.failureCode === "string") {
    return record.failureCode;
  }

  const extensions = record.extensions;
  if (typeof extensions === "object" && extensions !== null) {
    const ext = extensions as Record<string, unknown>;
    if (typeof ext.failureCode === "string") {
      return ext.failureCode;
    }
  }

  return undefined;
}

function mapHttpStatus(status: number, body: unknown): ExportCaseApiFailure {
  const failureCode = extractFailureCode(body);
  const title =
    typeof body === "object" &&
    body !== null &&
    typeof (body as Record<string, unknown>).title === "string"
      ? String((body as Record<string, unknown>).title)
      : undefined;

  if (status === 401) {
    return fail(
      "unauthenticated",
      title ?? "Authentication required.",
      false,
      failureCode,
    );
  }

  if (status === 403) {
    const refusal = failureForRefusal(classifyApiFailure(status, body), title ?? "Authorization denied.");
    return fail(refusal.code, refusal.message, false, failureCode);
  }

  if (status === 404) {
    return fail(
      "not-found",
      title ?? "Export case not found.",
      false,
      failureCode,
    );
  }

  if (status === 409) {
    return fail(
      "conflict",
      `${failureCode ?? "conflict"}: ${title ?? "Conflict."}`,
      false,
      failureCode,
    );
  }

  if (status === 400) {
    return fail(
      "invalid-request",
      title ?? `${failureCode ?? "invalid-request"}`,
      false,
      failureCode,
    );
  }

  if (status >= 500) {
    return fail(
      "source-unavailable",
      "Export service is unavailable.",
      true,
      failureCode,
    );
  }

  return fail(
    "invalid-response",
    `Unexpected status: ${status}`,
    false,
    failureCode,
  );
}

export async function executeGet<T>(
  context: ExportCaseRequestContext,
  url: string,
  parse: (body: unknown) => ExportCaseApiResult<T>,
): Promise<ExportCaseApiResult<T>> {
  if (!context.apiBaseUrl.trim()) {
    return fail("configuration-error", "API base URL is not configured.", false);
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "GET",
      headers: {
        Accept: "application/json",
        ...context.requestHeaders,
      },
      cache: "no-store",
    });
  } catch {
    return fail("source-unavailable", "The export service is unavailable.", true);
  }

  const body = await readResponseBody(response);

  if (!response.ok) {
    return mapHttpStatus(response.status, body);
  }

  return parse(body);
}

export async function executePost<T>(
  context: ExportCaseRequestContext,
  url: string,
  requestBody: unknown,
  parse: (body: unknown) => ExportCaseApiResult<T>,
): Promise<ExportCaseApiResult<T>> {
  if (!context.apiBaseUrl.trim()) {
    return fail("configuration-error", "API base URL is not configured.", false);
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...context.requestHeaders,
      },
      body: JSON.stringify(requestBody),
      cache: "no-store",
    });
  } catch {
    return fail("source-unavailable", "The export service is unavailable.", true);
  }

  const body = await readResponseBody(response);

  if (!response.ok) {
    return mapHttpStatus(response.status, body);
  }

  return parse(body);
}

async function executePut<T>(
  context: ExportCaseRequestContext,
  url: string,
  requestBody: unknown,
  parse: (body: unknown) => ExportCaseApiResult<T>,
): Promise<ExportCaseApiResult<T>> {
  if (!context.apiBaseUrl.trim()) {
    return fail("configuration-error", "API base URL is not configured.", false);
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...context.requestHeaders,
      },
      body: JSON.stringify(requestBody),
      cache: "no-store",
    });
  } catch {
    return fail("source-unavailable", "The export service is unavailable.", true);
  }

  const body = await readResponseBody(response);

  if (!response.ok) {
    return mapHttpStatus(response.status, body);
  }

  return parse(body);
}

export function caseFacilityUrl(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  suffix: string,
  extraParams?: Record<string, string>,
): string {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  if (extraParams) {
    for (const [key, value] of Object.entries(extraParams)) {
      if (value.trim()) {
        params.set(key, value.trim());
      }
    }
  }

  return `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}${suffix}?${params.toString()}`;
}

export function mapParsed<T>(
  parsed: { status: "success"; value: T } | { status: "failure"; message: string },
): ExportCaseApiResult<T> {
  if (parsed.status === "failure") {
    return fail("invalid-response", `invalid-response ${parsed.message}`, false);
  }

  return { status: "success", value: parsed.value };
}

export async function listExportCases(
  context: ExportCaseRequestContext,
  query: {
    facilityId: string;
    status?: string;
    readiness?: string;
    search?: string;
    limit?: number;
  },
): Promise<ExportCaseApiResult<ExportCaseSummary[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", query.facilityId.trim());

  if (query.status?.trim()) {
    params.set("status", query.status.trim());
  }

  if (query.readiness?.trim()) {
    params.set("readiness", query.readiness.trim());
  }

  if (query.search?.trim()) {
    params.set("search", query.search.trim());
  }

  if (query.limit !== undefined) {
    params.set("limit", String(query.limit));
  }

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportCaseListApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function getExportCaseById(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  facilityId: string,
): Promise<ExportCaseApiResult<ExportCaseDetail>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportCaseDetailApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function getExportCaseByOrder(
  context: ExportCaseRequestContext,
  sourceOrderId: string,
  facilityId: string,
): Promise<ExportCaseApiResult<ExportCaseDetail>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/by-order/${encodeURIComponent(sourceOrderId.trim())}?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportCaseDetailApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function ensureExportCaseFromOrder(
  context: ExportCaseRequestContext,
  input: EnsureExportCaseFromOrderInput,
): Promise<ExportCaseApiResult<ExportCaseWriteSuccess>> {
  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/from-order`;

  return executePost(
    context,
    url,
    {
      facilityId: input.facilityId,
      sourceOrderId: input.sourceOrderId,
      sourceOrderReference: input.sourceOrderReference,
      customerName: input.customerName,
      isExport: input.isExport,
      destinationCountryCode: input.destinationCountryCode,
      originCountryCode: input.originCountryCode,
      incotermCode: input.incotermCode,
    },
    (body) => {
      const parsed = parseExportCaseWriteSuccessApiResponse(body);

      if (parsed.status === "failure") {
        return fail(
          "invalid-response",
          `invalid-response ${parsed.message}`,
          false,
        );
      }

      return { status: "success", value: parsed.value };
    },
  );
}

export async function getExportReadiness(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  facilityId: string,
): Promise<ExportCaseApiResult<ExportReadiness>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/readiness?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportReadinessApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function listExportDocuments(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  facilityId: string,
): Promise<ExportCaseApiResult<ExportDocument[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/documents?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportDocumentListApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function registerExportDocument(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  facilityId: string,
  input: RegisterExportDocumentInput,
): Promise<ExportCaseApiResult<ExportDocumentWriteSuccess>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/documents?${params.toString()}`;

  return executePost(
    context,
    url,
    {
      documentType: input.documentType,
      displayName: input.displayName,
      fileReference: input.fileReference,
      documentNumber: input.documentNumber,
      issuedAtUtc: input.issuedAtUtc,
      expiresAtUtc: input.expiresAtUtc,
      source: input.source,
      initialStatus: input.initialStatus,
    },
    (body) => {
      const parsed = parseExportDocumentWriteSuccessApiResponse(body);

      if (parsed.status === "failure") {
        return fail(
          "invalid-response",
          `invalid-response ${parsed.message}`,
          false,
        );
      }

      return { status: "success", value: parsed.value };
    },
  );
}

export async function verifyExportDocument(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  documentId: string,
  facilityId: string,
  expectedVersion: number,
): Promise<ExportCaseApiResult<ExportDocumentWriteSuccess>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/documents/${encodeURIComponent(documentId.trim())}/verify?${params.toString()}`;

  return executePost(context, url, { expectedVersion }, (body) => {
    const parsed = parseExportDocumentWriteSuccessApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function rejectExportDocument(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  documentId: string,
  facilityId: string,
  expectedVersion: number,
  reason: string,
): Promise<ExportCaseApiResult<ExportDocumentWriteSuccess>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/documents/${encodeURIComponent(documentId.trim())}/reject?${params.toString()}`;

  return executePost(context, url, { expectedVersion, reason }, (body) => {
    const parsed = parseExportDocumentWriteSuccessApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

export async function listExportActivity(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  facilityId: string,
): Promise<ExportCaseApiResult<ExportActivity[]>> {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId.trim());

  const url = `${trimTrailingSlash(context.apiBaseUrl)}/api/export/cases/${encodeURIComponent(exportCaseId.trim())}/activity?${params.toString()}`;

  return executeGet(context, url, (body) => {
    const parsed = parseExportActivityListApiResponse(body);

    if (parsed.status === "failure") {
      return fail("invalid-response", `invalid-response ${parsed.message}`, false);
    }

    return { status: "success", value: parsed.value };
  });
}

// ─── EXPORT-2L — Packing ─────────────────────────────────────────────────────

export async function getExportPacking(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportPackingWorkspace>> {
  const url = caseFacilityUrl(context, exportCaseId, "/packing");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportPackingWorkspaceApiResponse(body)),
  );
}

export async function getExportPackingReconciliation(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  orderedQuantityTotal?: number,
): Promise<ExportCaseApiResult<ExportPackingReconciliation>> {
  const extra =
    orderedQuantityTotal !== undefined
      ? { orderedQuantityTotal: String(orderedQuantityTotal) }
      : undefined;
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    "/packing/reconciliation",
    extra,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseExportPackingReconciliationApiResponse(body)),
  );
}

export async function createExportPackingUnit(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  input: CreatePackingUnitInput,
): Promise<ExportCaseApiResult<PackingUnitWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/packing/units");
  return executePost(context, url, input, (body) =>
    mapParsed(parsePackingUnitWriteSuccessApiResponse(body)),
  );
}

export async function updateExportPackingUnit(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  packingUnitId: string,
  input: UpdatePackingUnitInput,
): Promise<ExportCaseApiResult<PackingUnitWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/packing/units/${encodeURIComponent(packingUnitId.trim())}`,
  );
  return executePut(context, url, input, (body) =>
    mapParsed(parsePackingUnitWriteSuccessApiResponse(body)),
  );
}

export async function addExportPackingItem(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  packingUnitId: string,
  input: AddPackingItemInput,
): Promise<ExportCaseApiResult<PackingItemWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/packing/units/${encodeURIComponent(packingUnitId.trim())}/items`,
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parsePackingItemWriteSuccessApiResponse(body)),
  );
}

export async function completeExportPacking(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<ExportCaseApiResult<PackingStatusWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/packing/complete");
  return executePost(context, url, { expectedCaseVersion }, (body) =>
    mapParsed(parsePackingStatusWriteSuccessApiResponse(body)),
  );
}

export async function verifyExportPacking(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<ExportCaseApiResult<PackingStatusWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/packing/verify");
  return executePost(context, url, { expectedCaseVersion }, (body) =>
    mapParsed(parsePackingStatusWriteSuccessApiResponse(body)),
  );
}

// ─── EXPORT-2L — Shipments ───────────────────────────────────────────────────

export async function listExportShipments(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportShipment[]>> {
  const url = caseFacilityUrl(context, exportCaseId, "/shipments");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportShipmentListApiResponse(body)),
  );
}

export async function getExportShipment(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  shipmentId: string,
): Promise<ExportCaseApiResult<ExportShipment>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/shipments/${encodeURIComponent(shipmentId.trim())}`,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseExportShipmentApiResponse(body)),
  );
}

export async function getExportShipmentReadiness(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  shipmentId: string,
): Promise<ExportCaseApiResult<ExportShipmentReadiness>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/shipments/${encodeURIComponent(shipmentId.trim())}/readiness`,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseExportShipmentReadinessApiResponse(body)),
  );
}

export async function createExportShipment(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  input: CreateShipmentInput,
): Promise<ExportCaseApiResult<ShipmentWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/shipments");
  return executePost(context, url, input, (body) =>
    mapParsed(parseShipmentWriteSuccessApiResponse(body)),
  );
}

export async function updateExportShipment(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  shipmentId: string,
  input: UpdateShipmentInput,
): Promise<ExportCaseApiResult<ShipmentWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/shipments/${encodeURIComponent(shipmentId.trim())}`,
  );
  return executePut(context, url, input, (body) =>
    mapParsed(parseShipmentWriteSuccessApiResponse(body)),
  );
}

export async function transitionExportShipment(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  shipmentId: string,
  input: TransitionShipmentInput,
): Promise<ExportCaseApiResult<ShipmentWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/shipments/${encodeURIComponent(shipmentId.trim())}/transition`,
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parseShipmentWriteSuccessApiResponse(body)),
  );
}

// ─── EXPORT-2L — Broker ──────────────────────────────────────────────────────

export async function getExportBrokerWorkspace(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportBrokerWorkspace>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportBrokerWorkspaceApiResponse(body)),
  );
}

export async function createExportBrokerWorkspace(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  input: CreateBrokerWorkspaceInput,
): Promise<ExportCaseApiResult<BrokerWorkspaceWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker");
  return executePost(context, url, input, (body) =>
    mapParsed(parseBrokerWorkspaceWriteSuccessApiResponse(body)),
  );
}

export async function prepareExportBrokerPackage(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  workspaceId: string,
  input: PrepareBrokerPackageInput,
): Promise<ExportCaseApiResult<BrokerPackageWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker/package/prepare", {
    workspaceId,
  });
  return executePost(context, url, input, (body) =>
    mapParsed(parseBrokerPackageWriteSuccessApiResponse(body)),
  );
}

export async function sendExportBrokerPackage(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  workspaceId: string,
  expectedWorkspaceVersion: number,
): Promise<ExportCaseApiResult<BrokerPackageWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker/package/send", {
    workspaceId,
  });
  return executePost(context, url, { expectedWorkspaceVersion }, (body) =>
    mapParsed(parseBrokerPackageWriteSuccessApiResponse(body)),
  );
}

export async function acknowledgeExportBroker(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  workspaceId: string,
  expectedWorkspaceVersion: number,
  brokerReference?: string,
): Promise<ExportCaseApiResult<BrokerWorkspaceWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker/acknowledge", {
    workspaceId,
  });
  return executePost(
    context,
    url,
    { expectedWorkspaceVersion, brokerReference },
    (body) => mapParsed(parseBrokerWorkspaceWriteSuccessApiResponse(body)),
  );
}

export async function createExportBrokerCorrection(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  workspaceId: string,
  input: CreateBrokerCorrectionInput,
): Promise<ExportCaseApiResult<BrokerCorrectionWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/broker/corrections", {
    workspaceId,
  });
  return executePost(context, url, input, (body) =>
    mapParsed(parseBrokerCorrectionWriteSuccessApiResponse(body)),
  );
}

export async function resolveExportBrokerCorrection(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  workspaceId: string,
  correctionId: string,
  input: ResolveBrokerCorrectionInput,
): Promise<ExportCaseApiResult<BrokerCorrectionWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/broker/corrections/${encodeURIComponent(correctionId.trim())}/resolve`,
    { workspaceId },
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parseBrokerCorrectionWriteSuccessApiResponse(body)),
  );
}

// ─── EXPORT-3L — Compliance, research, knowledge, traceability ─────────────────

export function exportFacilityUrl(
  context: ExportCaseRequestContext,
  path: string,
  extraParams?: Record<string, string>,
): string {
  const params = new URLSearchParams();
  params.set("facilityId", context.facilityId.trim());

  if (extraParams) {
    for (const [key, value] of Object.entries(extraParams)) {
      if (value.trim()) {
        params.set(key, value.trim());
      }
    }
  }

  return `${trimTrailingSlash(context.apiBaseUrl)}${path}?${params.toString()}`;
}

export async function getExportComplianceSummary(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<
  ExportCaseApiResult<import("../domain/export-3l").ExportComplianceSummary>
> {
  const url = caseFacilityUrl(context, exportCaseId, "/compliance");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportComplianceSummaryApiResponse(body)),
  );
}

export async function evaluateExportCompliance(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  input: import("../domain/export-3l").EvaluateComplianceInput,
): Promise<
  ExportCaseApiResult<
    import("./export-3l-api-contracts").ComplianceWriteSuccess
  >
> {
  const url = caseFacilityUrl(context, exportCaseId, "/compliance/evaluate");
  return executePost(context, url, input, (body) =>
    mapParsed(parseComplianceWriteSuccessApiResponse(body)),
  );
}

export async function revalidateExportCompliance(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  expectedCaseVersion: number,
): Promise<
  ExportCaseApiResult<
    import("./export-3l-api-contracts").ComplianceWriteSuccess
  >
> {
  const url = caseFacilityUrl(context, exportCaseId, "/compliance/revalidate");
  return executePost(context, url, { expectedCaseVersion }, (body) =>
    mapParsed(parseComplianceWriteSuccessApiResponse(body)),
  );
}

export async function satisfyExportComplianceRequirement(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  requirementId: string,
  input: import("../domain/export-3l").SatisfyComplianceRequirementInput,
): Promise<
  ExportCaseApiResult<
    import("./export-3l-api-contracts").ComplianceRequirementWriteSuccess
  >
> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/compliance/requirements/${encodeURIComponent(requirementId.trim())}/satisfy`,
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parseComplianceRequirementWriteSuccessApiResponse(body)),
  );
}

export async function getExportTraceability(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<import("../domain/export-3l").ExportTraceability>> {
  const url = caseFacilityUrl(context, exportCaseId, "/traceability");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportTraceabilityApiResponse(body)),
  );
}

export async function listCountryResearchCases(
  context: ExportCaseRequestContext,
  query?: { countryCode?: string; status?: string },
): Promise<
  ExportCaseApiResult<
    import("../domain/export-3l").CountryResearchCaseSummary[]
  >
> {
  const extra: Record<string, string> = {};
  if (query?.countryCode?.trim()) extra.countryCode = query.countryCode.trim();
  if (query?.status?.trim()) extra.status = query.status.trim();

  const url = exportFacilityUrl(context, "/api/export/country-research", extra);
  return executeGet(context, url, (body) =>
    mapParsed(parseCountryResearchListApiResponse(body)),
  );
}

export async function getCountryResearchCase(
  context: ExportCaseRequestContext,
  researchCaseId: string,
): Promise<
  ExportCaseApiResult<import("../domain/export-3l").CountryResearchDetail>
> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}`,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseCountryResearchDetailApiResponse(body)),
  );
}

export async function createCountryResearchCase(
  context: ExportCaseRequestContext,
  input: import("../domain/export-3l").CreateResearchCaseInput,
): Promise<
  ExportCaseApiResult<import("./export-3l-api-contracts").ResearchWriteSuccess>
> {
  const url = exportFacilityUrl(context, "/api/export/country-research");
  return executePost(context, url, input, (body) =>
    mapParsed(parseResearchWriteSuccessApiResponse(body)),
  );
}

export async function addCountryResearchFinding(
  context: ExportCaseRequestContext,
  researchCaseId: string,
  input: import("../domain/export-3l").AddResearchFindingInput,
): Promise<
  ExportCaseApiResult<
    import("./export-3l-api-contracts").ResearchFindingWriteSuccess
  >
> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}/findings`,
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parseResearchFindingWriteSuccessApiResponse(body)),
  );
}

export async function reviewCountryResearchCase(
  context: ExportCaseRequestContext,
  researchCaseId: string,
  expectedVersion: number,
): Promise<
  ExportCaseApiResult<import("./export-3l-api-contracts").ResearchWriteSuccess>
> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}/review`,
  );
  return executePost(context, url, { expectedVersion }, (body) =>
    mapParsed(parseResearchWriteSuccessApiResponse(body)),
  );
}

export async function completeCountryResearchCase(
  context: ExportCaseRequestContext,
  researchCaseId: string,
  expectedVersion: number,
): Promise<
  ExportCaseApiResult<import("./export-3l-api-contracts").ResearchWriteSuccess>
> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}/complete`,
  );
  return executePost(context, url, { expectedVersion }, (body) =>
    mapParsed(parseResearchWriteSuccessApiResponse(body)),
  );
}

export async function listCountryKnowledgeEntries(
  context: ExportCaseRequestContext,
  query?: { countryCode?: string; verificationStatus?: string },
): Promise<
  ExportCaseApiResult<import("../domain/export-3l").CountryKnowledgeEntry[]>
> {
  const extra: Record<string, string> = {};
  if (query?.countryCode?.trim()) extra.countryCode = query.countryCode.trim();
  if (query?.verificationStatus?.trim()) {
    extra.verificationStatus = query.verificationStatus.trim();
  }

  const url = exportFacilityUrl(context, "/api/export/country-knowledge", extra);
  return executeGet(context, url, (body) =>
    mapParsed(parseCountryKnowledgeListApiResponse(body)),
  );
}

export async function createCountryKnowledgeEntry(
  context: ExportCaseRequestContext,
  input: import("../domain/export-3l").CreateKnowledgeEntryInput,
): Promise<
  ExportCaseApiResult<import("./export-3l-api-contracts").KnowledgeWriteSuccess>
> {
  const url = exportFacilityUrl(context, "/api/export/country-knowledge");
  return executePost(context, url, input, (body) =>
    mapParsed(parseKnowledgeWriteSuccessApiResponse(body)),
  );
}

export async function verifyCountryKnowledgeEntry(
  context: ExportCaseRequestContext,
  knowledgeEntryId: string,
  expectedVersion: number,
): Promise<
  ExportCaseApiResult<import("./export-3l-api-contracts").KnowledgeWriteSuccess>
> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-knowledge/${encodeURIComponent(knowledgeEntryId.trim())}/verify`,
  );
  return executePost(context, url, { expectedVersion }, (body) =>
    mapParsed(parseKnowledgeWriteSuccessApiResponse(body)),
  );
}
