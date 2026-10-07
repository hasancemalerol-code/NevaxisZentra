"use server";

import "./export-case-server-boundary";

import type { ReviewExportAiRecommendationInput } from "../domain/export-4l";
import type {
  ExportAiAnalysisWriteSuccess,
  ExportAiRecommendationWriteSuccess,
} from "./export-4l-api-contracts";
import {
  acceptExportAiRecommendation,
  dismissExportAiRecommendation,
  explainExportReadiness,
  rejectExportAiRecommendation,
  startExportAiAnalysis,
  startResearchAiAnalysis,
  suggestExportNextActions,
} from "./export-4l-api-contracts";
import type { ExportCaseApiResult } from "./export-case-api-client";
import {
  resolveExportCaseRequestContextForIncomingRequest,
  type ExportCaseRequestContext,
} from "./export-case-request-context";
import { parseExportRuntimeConfig } from "./export-case-runtime-config";

export type ExportAiActionFailure = Readonly<{
  status: "failure";
  code: string;
  message: string;
  failureCode?: string;
}>;

export type ExportAiActionResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ExportAiActionFailure;

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

async function resolveWriteContext(): Promise< ResolvedWriteContext> {
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

  const contextResult = await resolveExportCaseRequestContextForIncomingRequest(configResult.config);

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

function invalidRequest(message: string): ExportAiActionFailure {
  return { status: "failure", code: "invalid-request", message };
}

function mapResult<T>(
  result: ExportCaseApiResult<T>,
): ExportAiActionResult<T> {
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

export async function analyzeExportCaseAction(
  exportCaseId: string,
): Promise<ExportAiActionResult<ExportAiAnalysisWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }

  const result = await startExportAiAnalysis(resolved.context, exportCaseId.trim(), {
    runType: "case-overview",
  });

  return mapResult(result);
}

export async function explainExportReadinessAction(
  exportCaseId: string,
): Promise<ExportAiActionResult<ExportAiAnalysisWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }

  const result = await explainExportReadiness(resolved.context, exportCaseId.trim());
  return mapResult(result);
}

export async function suggestExportNextActionsAction(
  exportCaseId: string,
): Promise<ExportAiActionResult<ExportAiAnalysisWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim()) {
    return invalidRequest("İhracat dosyası kimliği zorunludur.");
  }

  const result = await suggestExportNextActions(resolved.context, exportCaseId.trim());
  return mapResult(result);
}

export async function acceptExportAiRecommendationAction(
  exportCaseId: string,
  recommendationId: string,
  input: ReviewExportAiRecommendationInput,
): Promise<ExportAiActionResult<ExportAiRecommendationWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !recommendationId.trim()) {
    return invalidRequest("Dosya ve öneri kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion)) {
    return invalidRequest("Beklenen öneri sürümü geçersiz.");
  }

  const result = await acceptExportAiRecommendation(
    resolved.context,
    exportCaseId.trim(),
    recommendationId.trim(),
    input.expectedVersion,
    input.reviewNote,
  );

  return mapResult(result);
}

export async function rejectExportAiRecommendationAction(
  exportCaseId: string,
  recommendationId: string,
  input: ReviewExportAiRecommendationInput,
): Promise<ExportAiActionResult<ExportAiRecommendationWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !recommendationId.trim()) {
    return invalidRequest("Dosya ve öneri kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion)) {
    return invalidRequest("Beklenen öneri sürümü geçersiz.");
  }

  const result = await rejectExportAiRecommendation(
    resolved.context,
    exportCaseId.trim(),
    recommendationId.trim(),
    input.expectedVersion,
    input.reviewNote,
  );

  return mapResult(result);
}

export async function dismissExportAiRecommendationAction(
  exportCaseId: string,
  recommendationId: string,
  input: ReviewExportAiRecommendationInput,
): Promise<ExportAiActionResult<ExportAiRecommendationWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!exportCaseId.trim() || !recommendationId.trim()) {
    return invalidRequest("Dosya ve öneri kimliği zorunludur.");
  }
  if (!Number.isFinite(input.expectedVersion)) {
    return invalidRequest("Beklenen öneri sürümü geçersiz.");
  }

  const result = await dismissExportAiRecommendation(
    resolved.context,
    exportCaseId.trim(),
    recommendationId.trim(),
    input.expectedVersion,
    input.reviewNote,
  );

  return mapResult(result);
}

export async function summarizeResearchAction(
  researchCaseId: string,
): Promise<ExportAiActionResult<ExportAiAnalysisWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!researchCaseId.trim()) {
    return invalidRequest("Araştırma dosyası kimliği zorunludur.");
  }

  const result = await startResearchAiAnalysis(
    resolved.context,
    researchCaseId.trim(),
    { runType: "country-research-assist" },
  );

  return mapResult(result);
}

export async function findResearchMissingSourcesAction(
  researchCaseId: string,
): Promise<ExportAiActionResult<ExportAiAnalysisWriteSuccess>> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;

  if (!researchCaseId.trim()) {
    return invalidRequest("Araştırma dosyası kimliği zorunludur.");
  }

  const result = await startResearchAiAnalysis(
    resolved.context,
    researchCaseId.trim(),
    { runType: "missing-data-analysis" },
  );

  return mapResult(result);
}
