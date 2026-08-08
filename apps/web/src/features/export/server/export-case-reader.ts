import "./export-case-server-boundary";

import type {
  ExportActivity,
  ExportCaseDetail,
  ExportCaseSummary,
  ExportDocument,
  ExportReadiness,
} from "../domain/export-case";
import {
  getExportCaseById,
  getExportCaseByOrder,
  getExportReadiness,
  listExportActivity,
  listExportCases,
  listExportDocuments,
  type ExportCaseApiFailure,
} from "./export-case-api-client";
import {
  resolveExportCaseRequestContext,
  type ExportCaseRequestContext,
} from "./export-case-request-context";
import { parseExportRuntimeConfig } from "./export-case-runtime-config";

type ExportCaseReadFailure =
  | ExportCaseApiFailure
  | Readonly<{
      status: "failure";
      code: "configuration-error" | "invalid-scope" | "unauthenticated";
      message: string;
      retryable: false;
    }>;

export type ExportCaseListResult =
  | Readonly<{ status: "success"; cases: ExportCaseSummary[] }>
  | ExportCaseReadFailure;

export type ExportCaseDetailResult =
  | Readonly<{ status: "success"; exportCase: ExportCaseDetail }>
  | ExportCaseReadFailure;

export type ExportReadinessResult =
  | Readonly<{ status: "success"; readiness: ExportReadiness }>
  | ExportCaseReadFailure;

export type ExportDocumentListResult =
  | Readonly<{ status: "success"; documents: ExportDocument[] }>
  | ExportCaseReadFailure;

export type ExportActivityListResult =
  | Readonly<{ status: "success"; activities: ExportActivity[] }>
  | ExportCaseReadFailure;

type ResolvedContext =
  | Readonly<{ kind: "context"; context: ExportCaseRequestContext }>
  | Readonly<{ kind: "failure"; failure: ExportCaseReadFailure }>;

function resolveReadContext(): ResolvedContext {
  const configResult = parseExportRuntimeConfig();

  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: configResult.message,
        retryable: false,
      },
    };
  }

  if (configResult.config.mode === "development-preview") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: "Development preview mode is not configured for Export.",
        retryable: false,
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
        retryable: false,
      },
    };
  }

  return { kind: "context", context: contextResult.context };
}

export async function readExportCases(query?: {
  status?: string;
  readiness?: string;
  search?: string;
  limit?: number;
}): Promise<ExportCaseListResult> {
  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await listExportCases(context, {
    facilityId: context.facilityId,
    status: query?.status,
    readiness: query?.readiness,
    search: query?.search,
    limit: query?.limit,
  });

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", cases: result.value };
}

export async function readExportCaseById(
  exportCaseId: string,
): Promise<ExportCaseDetailResult> {
  const normalizedId = exportCaseId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "exportCaseId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await getExportCaseById(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", exportCase: result.value };
}

export async function readExportCaseByOrder(
  sourceOrderId: string,
): Promise<ExportCaseDetailResult> {
  const normalizedId = sourceOrderId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "sourceOrderId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await getExportCaseByOrder(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", exportCase: result.value };
}

export async function readExportReadiness(
  exportCaseId: string,
): Promise<ExportReadinessResult> {
  const normalizedId = exportCaseId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "exportCaseId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await getExportReadiness(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", readiness: result.value };
}

export async function readExportDocuments(
  exportCaseId: string,
): Promise<ExportDocumentListResult> {
  const normalizedId = exportCaseId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "exportCaseId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await listExportDocuments(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", documents: result.value };
}

export async function readExportActivity(
  exportCaseId: string,
): Promise<ExportActivityListResult> {
  const normalizedId = exportCaseId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "exportCaseId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await listExportActivity(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", activities: result.value };
}
