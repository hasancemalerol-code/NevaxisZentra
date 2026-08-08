"use server";

import "./export-case-server-boundary";

import type {
  EnsureExportCaseFromOrderInput,
  ExportCaseWriteSuccess,
  RegisterExportDocumentInput,
} from "../domain/export-case";
import {
  ensureExportCaseFromOrder as ensureExportCaseFromOrderApi,
  registerExportDocument,
  rejectExportDocument,
  verifyExportDocument,
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
