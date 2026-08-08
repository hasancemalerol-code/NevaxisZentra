import "./export-case-server-boundary";

import type {
  AiUsageSummary,
  ExportAiAnalysisRun,
  ExportAiRecommendation,
  ExportAiRunDetail,
} from "../domain/export-4l";
import type { ExportCaseApiFailure } from "./export-case-api-client";
import {
  getCurrentAiUsage,
  getExportAiRunDetail,
  listExportAiRecommendations,
  listExportAiRuns,
  listResearchAiRuns,
} from "./export-4l-api-contracts";
import {
  resolveExportCaseRequestContext,
  type ExportCaseRequestContext,
} from "./export-case-request-context";
import { parseExportRuntimeConfig } from "./export-case-runtime-config";

type ExportAiReadFailure =
  | ExportCaseApiFailure
  | Readonly<{
      status: "failure";
      code: "configuration-error" | "invalid-scope" | "unauthenticated";
      message: string;
      retryable: false;
    }>;

type ResolvedContext =
  | Readonly<{ kind: "context"; context: ExportCaseRequestContext }>
  | Readonly<{ kind: "failure"; failure: ExportAiReadFailure }>;

export type ExportAiRunListResult =
  | Readonly<{ status: "success"; runs: ExportAiAnalysisRun[] }>
  | ExportAiReadFailure;

export type ExportAiRunDetailResult =
  | Readonly<{ status: "success"; detail: ExportAiRunDetail }>
  | ExportAiReadFailure;

export type ExportAiRecommendationListResult =
  | Readonly<{ status: "success"; recommendations: ExportAiRecommendation[] }>
  | ExportAiReadFailure;

export type AiUsageSummaryResult =
  | Readonly<{ status: "success"; usage: AiUsageSummary }>
  | ExportAiReadFailure;

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

export async function readExportAiRuns(
  exportCaseId: string,
): Promise<ExportAiRunListResult> {
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
  if (resolved.kind === "failure") return resolved.failure;

  const result = await listExportAiRuns(resolved.context, normalizedId);
  if (result.status === "failure") return result;

  return { status: "success", runs: result.value };
}

export async function readExportAiRunDetail(
  exportCaseId: string,
  runId: string,
): Promise<ExportAiRunDetailResult> {
  const normalizedCaseId = exportCaseId.trim();
  const normalizedRunId = runId.trim();

  if (!normalizedCaseId || !normalizedRunId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "exportCaseId and runId are required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;

  const result = await getExportAiRunDetail(
    resolved.context,
    normalizedCaseId,
    normalizedRunId,
  );
  if (result.status === "failure") return result;

  return { status: "success", detail: result.value };
}

export async function readExportAiRecommendations(
  exportCaseId: string,
): Promise<ExportAiRecommendationListResult> {
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
  if (resolved.kind === "failure") return resolved.failure;

  const result = await listExportAiRecommendations(resolved.context, normalizedId);
  if (result.status === "failure") return result;

  return { status: "success", recommendations: result.value };
}

export async function readLatestExportAiRunDetail(
  exportCaseId: string,
): Promise<ExportAiRunDetailResult | Readonly<{ status: "success"; detail: null }>> {
  const runsResult = await readExportAiRuns(exportCaseId);

  if (runsResult.status === "failure") {
    return runsResult;
  }

  const latestRun = runsResult.runs[0];
  if (!latestRun) {
    return { status: "success", detail: null };
  }

  return readExportAiRunDetail(exportCaseId, latestRun.id);
}

export async function readResearchAiRuns(
  researchCaseId: string,
): Promise<ExportAiRunListResult> {
  const normalizedId = researchCaseId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "researchCaseId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;

  const result = await listResearchAiRuns(resolved.context, normalizedId);
  if (result.status === "failure") return result;

  return { status: "success", runs: result.value };
}

export async function readCurrentAiUsage(): Promise<AiUsageSummaryResult> {
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;

  const result = await getCurrentAiUsage(resolved.context);
  if (result.status === "failure") return result;

  return { status: "success", usage: result.value };
}
