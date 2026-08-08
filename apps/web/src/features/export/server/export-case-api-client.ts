import "./export-case-server-boundary";

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
    return fail(
      "permission-denied",
      title ?? "Authorization denied.",
      false,
      failureCode,
    );
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

async function executeGet<T>(
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

async function executePost<T>(
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
