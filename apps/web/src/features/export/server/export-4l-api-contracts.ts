import "./export-case-server-boundary";

import type {
  AiUsageSummary,
  ExportAiAnalysisRun,
  ExportAiAnalysisRunStatus,
  ExportAiAnalysisRunType,
  ExportAiContextSnapshot,
  ExportAiFinding,
  ExportAiFindingCategory,
  ExportAiFindingSeverity,
  ExportAiRecommendation,
  ExportAiRecommendationPriority,
  ExportAiRecommendationStatus,
  ExportAiReviewDecision,
  ExportAiRunDetail,
  ExportAiSourceReference,
  ExportAiSourceType,
  ExportCrossModuleRisk,
  ExportCrossModuleRiskSeverity,
  ExportCrossModuleRiskType,
  ExportCrossModuleSourceModule,
  StartExportAiAnalysisInput,
} from "../domain/export-4l";
import type { ExportCaseApiResult } from "./export-case-api-client";
import {
  caseFacilityUrl,
  executeGet,
  executePost,
  exportFacilityUrl,
  mapParsed,
} from "./export-case-api-client";
import type { ExportCaseRequestContext } from "./export-case-request-context";

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
  if (value === undefined || value === null) return undefined;
  return typeof value === "string" ? value : undefined;
}

const RUN_TYPES = new Set<ExportAiAnalysisRunType>([
  "case-overview",
  "compliance-explanation",
  "readiness-analysis",
  "missing-data-analysis",
  "country-research-assist",
  "next-actions",
  "document-gap-analysis",
  "cross-module-risk-analysis",
]);

const RUN_STATUSES = new Set<ExportAiAnalysisRunStatus>([
  "queued",
  "running",
  "completed",
  "failed",
  "cancelled",
]);

const FINDING_CATEGORIES = new Set<ExportAiFindingCategory>([
  "compliance",
  "document",
  "packing",
  "shipment",
  "broker",
  "country-research",
  "quality",
  "production",
  "planning",
  "erp",
  "risk",
  "data-quality",
  "other",
]);

const FINDING_SEVERITIES = new Set<ExportAiFindingSeverity>([
  "info",
  "warning",
  "critical",
]);

const SOURCE_TYPES = new Set<ExportAiSourceType>([
  "verified-rule",
  "verified-knowledge",
  "historical-observation",
  "broker-input",
  "operational-data",
  "ai-interpretation",
  "system-deterministic",
]);

const RECOMMENDATION_PRIORITIES = new Set<ExportAiRecommendationPriority>([
  "low",
  "medium",
  "high",
  "critical",
]);

const RECOMMENDATION_STATUSES = new Set<ExportAiRecommendationStatus>([
  "proposed",
  "accepted",
  "rejected",
  "dismissed",
  "expired",
]);

const REVIEW_DECISIONS = new Set<ExportAiReviewDecision>([
  "accepted",
  "rejected",
  "dismissed",
]);

const RISK_TYPES = new Set<ExportCrossModuleRiskType>([
  "production-delay",
  "planning-deadline-risk",
  "quality-hold",
  "missing-material",
  "erp-data-missing",
  "shipment-cutoff-risk",
  "context-unavailable",
]);

const RISK_SEVERITIES = new Set<ExportCrossModuleRiskSeverity>([
  "info",
  "warning",
  "critical",
]);

const SOURCE_MODULES = new Set<ExportCrossModuleSourceModule>([
  "production",
  "planning",
  "quality",
  "erp",
  "shipment",
  "export",
]);

function parseSourceReference(raw: unknown): ParseResult<ExportAiSourceReference> {
  if (typeof raw !== "object" || raw === null) {
    return fail("source reference must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.sourceType)) return fail("sourceType is required.");
  if (!isString(o.sourceId)) return fail("sourceId is required.");
  if (!isString(o.label)) return fail("label is required.");

  return ok({
    sourceType: o.sourceType,
    sourceId: o.sourceId,
    label: o.label,
    detail: optionalString(o.detail),
  });
}

export function parseExportAiRunApiResponse(
  raw: unknown,
): ParseResult<ExportAiAnalysisRun> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("run.id is required.");
  if (!isString(o.contextSnapshotId)) {
    return fail("run.contextSnapshotId is required.");
  }
  if (
    !isString(o.runType) ||
    !RUN_TYPES.has(o.runType as ExportAiAnalysisRunType)
  ) {
    return fail(`run.runType has an invalid value: '${String(o.runType)}'.`);
  }
  if (
    !isString(o.status) ||
    !RUN_STATUSES.has(o.status as ExportAiAnalysisRunStatus)
  ) {
    return fail(`run.status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isString(o.provider)) return fail("run.provider is required.");
  if (!isString(o.model)) return fail("run.model is required.");
  if (!isString(o.promptVersion)) return fail("run.promptVersion is required.");
  if (!isString(o.startedAtUtc)) return fail("run.startedAtUtc is required.");
  if (!isString(o.requestedBySubjectId)) {
    return fail("run.requestedBySubjectId is required.");
  }
  if (!isNumber(o.inputUsageUnits)) return fail("run.inputUsageUnits is required.");
  if (!isNumber(o.outputUsageUnits)) {
    return fail("run.outputUsageUnits is required.");
  }
  if (!isNumber(o.aiCreditsConsumed)) {
    return fail("run.aiCreditsConsumed is required.");
  }
  if (!isString(o.reasoningSummary)) {
    return fail("run.reasoningSummary is required.");
  }
  if (!isNumber(o.version)) return fail("run.version is required.");

  return ok({
    id: o.id,
    exportCaseId: optionalString(o.exportCaseId),
    countryResearchCaseId: optionalString(o.countryResearchCaseId),
    contextSnapshotId: o.contextSnapshotId,
    runType: o.runType as ExportAiAnalysisRunType,
    status: o.status as ExportAiAnalysisRunStatus,
    provider: o.provider,
    model: o.model,
    promptVersion: o.promptVersion,
    startedAtUtc: o.startedAtUtc,
    completedAtUtc: optionalString(o.completedAtUtc),
    requestedBySubjectId: o.requestedBySubjectId,
    inputUsageUnits: o.inputUsageUnits,
    outputUsageUnits: o.outputUsageUnits,
    aiCreditsConsumed: o.aiCreditsConsumed,
    failureCode: optionalString(o.failureCode),
    failureMessage: optionalString(o.failureMessage),
    reasoningSummary: o.reasoningSummary,
    version: o.version,
  });
}

export function parseExportAiFindingApiResponse(
  raw: unknown,
): ParseResult<ExportAiFinding> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("finding.id is required.");
  if (!isString(o.analysisRunId)) return fail("finding.analysisRunId is required.");
  if (
    !isString(o.category) ||
    !FINDING_CATEGORIES.has(o.category as ExportAiFindingCategory)
  ) {
    return fail(`finding.category has an invalid value: '${String(o.category)}'.`);
  }
  if (!isString(o.title)) return fail("finding.title is required.");
  if (!isString(o.summary)) return fail("finding.summary is required.");
  if (
    !isString(o.severity) ||
    !FINDING_SEVERITIES.has(o.severity as ExportAiFindingSeverity)
  ) {
    return fail(`finding.severity has an invalid value: '${String(o.severity)}'.`);
  }
  if (!isNumber(o.confidence)) return fail("finding.confidence is required.");
  if (
    !isString(o.sourceType) ||
    !SOURCE_TYPES.has(o.sourceType as ExportAiSourceType)
  ) {
    return fail(`finding.sourceType has an invalid value: '${String(o.sourceType)}'.`);
  }
  if (!Array.isArray(o.sourceReferences)) {
    return fail("finding.sourceReferences must be an array.");
  }
  if (!isBoolean(o.actionable)) return fail("finding.actionable is required.");
  if (!isBoolean(o.humanReviewRequired)) {
    return fail("finding.humanReviewRequired is required.");
  }
  if (!isString(o.createdAtUtc)) return fail("finding.createdAtUtc is required.");

  const sourceReferences: ExportAiSourceReference[] = [];
  for (const item of o.sourceReferences) {
    const parsed = parseSourceReference(item);
    if (parsed.status === "failure") return parsed;
    sourceReferences.push(parsed.value);
  }

  return ok({
    id: o.id,
    analysisRunId: o.analysisRunId,
    category: o.category as ExportAiFindingCategory,
    title: o.title,
    summary: o.summary,
    severity: o.severity as ExportAiFindingSeverity,
    confidence: o.confidence,
    sourceType: o.sourceType as ExportAiSourceType,
    sourceReferences,
    relatedRequirementId: optionalString(o.relatedRequirementId),
    relatedDocumentId: optionalString(o.relatedDocumentId),
    relatedShipmentId: optionalString(o.relatedShipmentId),
    actionable: o.actionable,
    humanReviewRequired: o.humanReviewRequired,
    createdAtUtc: o.createdAtUtc,
  });
}

export function parseExportAiRecommendationApiResponse(
  raw: unknown,
): ParseResult<ExportAiRecommendation> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("recommendation.id is required.");
  if (!isString(o.analysisRunId)) {
    return fail("recommendation.analysisRunId is required.");
  }
  if (!isString(o.title)) return fail("recommendation.title is required.");
  if (!isString(o.description)) return fail("recommendation.description is required.");
  if (
    !isString(o.priority) ||
    !RECOMMENDATION_PRIORITIES.has(o.priority as ExportAiRecommendationPriority)
  ) {
    return fail(`recommendation.priority has an invalid value: '${String(o.priority)}'.`);
  }
  if (!isNumber(o.confidence)) return fail("recommendation.confidence is required.");
  if (!isString(o.reasoningSummary)) {
    return fail("recommendation.reasoningSummary is required.");
  }
  if (
    !isString(o.status) ||
    !RECOMMENDATION_STATUSES.has(o.status as ExportAiRecommendationStatus)
  ) {
    return fail(`recommendation.status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isString(o.createdAtUtc)) return fail("recommendation.createdAtUtc is required.");
  if (!isNumber(o.version)) return fail("recommendation.version is required.");

  const reviewDecision = optionalString(o.reviewDecision);
  if (
    reviewDecision !== undefined &&
    !REVIEW_DECISIONS.has(reviewDecision as ExportAiReviewDecision)
  ) {
    return fail(
      `recommendation.reviewDecision has an invalid value: '${reviewDecision}'.`,
    );
  }

  return ok({
    id: o.id,
    analysisRunId: o.analysisRunId,
    title: o.title,
    description: o.description,
    priority: o.priority as ExportAiRecommendationPriority,
    confidence: o.confidence,
    reasoningSummary: o.reasoningSummary,
    proposedActionType: optionalString(o.proposedActionType),
    proposedActionPayloadJson: optionalString(o.proposedActionPayloadJson),
    status: o.status as ExportAiRecommendationStatus,
    createdAtUtc: o.createdAtUtc,
    reviewedAtUtc: optionalString(o.reviewedAtUtc),
    reviewedBySubjectId: optionalString(o.reviewedBySubjectId),
    reviewDecision: reviewDecision as ExportAiReviewDecision | undefined,
    reviewNote: optionalString(o.reviewNote),
    version: o.version,
  });
}

function parseCrossModuleRisk(raw: unknown): ParseResult<ExportCrossModuleRisk> {
  if (typeof raw !== "object" || raw === null) {
    return fail("cross-module risk must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.type) || !RISK_TYPES.has(o.type as ExportCrossModuleRiskType)) {
    return fail(`risk.type has an invalid value: '${String(o.type)}'.`);
  }
  if (
    !isString(o.severity) ||
    !RISK_SEVERITIES.has(o.severity as ExportCrossModuleRiskSeverity)
  ) {
    return fail(`risk.severity has an invalid value: '${String(o.severity)}'.`);
  }
  if (
    !isString(o.sourceModule) ||
    !SOURCE_MODULES.has(o.sourceModule as ExportCrossModuleSourceModule)
  ) {
    return fail(`risk.sourceModule has an invalid value: '${String(o.sourceModule)}'.`);
  }
  if (!isString(o.sourceRecordReference)) {
    return fail("risk.sourceRecordReference is required.");
  }
  if (!isString(o.description)) return fail("risk.description is required.");
  if (!isString(o.detectedAtUtc)) return fail("risk.detectedAtUtc is required.");
  if (!isBoolean(o.blocking)) return fail("risk.blocking is required.");

  return ok({
    type: o.type as ExportCrossModuleRiskType,
    severity: o.severity as ExportCrossModuleRiskSeverity,
    sourceModule: o.sourceModule as ExportCrossModuleSourceModule,
    sourceRecordReference: o.sourceRecordReference,
    description: o.description,
    detectedAtUtc: o.detectedAtUtc,
    blocking: o.blocking,
  });
}

function parseContextSnapshot(raw: unknown): ParseResult<ExportAiContextSnapshot> {
  if (typeof raw !== "object" || raw === null) {
    return fail("context snapshot must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!Array.isArray(o.crossModuleRisks)) {
    return fail("contextSnapshot.crossModuleRisks must be an array.");
  }
  if (!isString(o.capturedAtUtc)) {
    return fail("contextSnapshot.capturedAtUtc is required.");
  }

  const crossModuleRisks: ExportCrossModuleRisk[] = [];
  for (const item of o.crossModuleRisks) {
    const parsed = parseCrossModuleRisk(item);
    if (parsed.status === "failure") return parsed;
    crossModuleRisks.push(parsed.value);
  }

  let exportCase: ExportAiContextSnapshot["exportCase"];
  if (o.exportCase !== undefined && o.exportCase !== null) {
    const ec = o.exportCase as Record<string, unknown>;
    if (!isString(ec.exportCaseId)) return fail("exportCase.exportCaseId is required.");
    if (!isString(ec.caseNumber)) return fail("exportCase.caseNumber is required.");
    if (!isString(ec.destinationCountryCode)) {
      return fail("exportCase.destinationCountryCode is required.");
    }
    if (!isString(ec.readinessStatus)) {
      return fail("exportCase.readinessStatus is required.");
    }
    if (!isNumber(ec.readinessPercent)) {
      return fail("exportCase.readinessPercent is required.");
    }
    if (!isString(ec.complianceStatus)) {
      return fail("exportCase.complianceStatus is required.");
    }
    if (!isString(ec.hsCodeStatus)) return fail("exportCase.hsCodeStatus is required.");
    if (!isString(ec.originConfirmationStatus)) {
      return fail("exportCase.originConfirmationStatus is required.");
    }

    exportCase = {
      exportCaseId: ec.exportCaseId,
      caseNumber: ec.caseNumber,
      destinationCountryCode: ec.destinationCountryCode,
      originCountryCode: optionalString(ec.originCountryCode),
      readinessStatus: ec.readinessStatus,
      readinessPercent: ec.readinessPercent,
      complianceStatus: ec.complianceStatus,
      hsCode: optionalString(ec.hsCode),
      hsCodeStatus: ec.hsCodeStatus,
      originConfirmationStatus: ec.originConfirmationStatus,
    };
  }

  return ok({
    exportCaseId: optionalString(o.exportCaseId),
    countryResearchCaseId: optionalString(o.countryResearchCaseId),
    exportCase,
    sourceOrderReference: optionalString(o.sourceOrderReference),
    customerName: optionalString(o.customerName),
    crossModuleRisks,
    capturedAtUtc: o.capturedAtUtc,
  });
}

export function parseExportAiRunDetailApiResponse(
  raw: unknown,
): ParseResult<ExportAiRunDetail> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  const runResult = parseExportAiRunApiResponse(o.run ?? o);
  if (runResult.status === "failure") return runResult;

  const findings: ExportAiFinding[] = [];
  if (o.findings !== undefined) {
    if (!Array.isArray(o.findings)) return fail("findings must be an array.");
    for (const item of o.findings) {
      const parsed = parseExportAiFindingApiResponse(item);
      if (parsed.status === "failure") return parsed;
      findings.push(parsed.value);
    }
  }

  const recommendations: ExportAiRecommendation[] = [];
  if (o.recommendations !== undefined) {
    if (!Array.isArray(o.recommendations)) {
      return fail("recommendations must be an array.");
    }
    for (const item of o.recommendations) {
      const parsed = parseExportAiRecommendationApiResponse(item);
      if (parsed.status === "failure") return parsed;
      recommendations.push(parsed.value);
    }
  }

  let contextSnapshot: ExportAiContextSnapshot | undefined;
  if (o.contextSnapshot !== undefined && o.contextSnapshot !== null) {
    const parsed = parseContextSnapshot(o.contextSnapshot);
    if (parsed.status === "failure") return parsed;
    contextSnapshot = parsed.value;
  }

  return ok({
    run: runResult.value,
    findings,
    recommendations,
    contextSnapshot,
  });
}

export function parseExportAiRunListApiResponse(
  raw: unknown,
): ParseResult<ExportAiAnalysisRun[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const runs: ExportAiAnalysisRun[] = [];
  for (const item of raw) {
    const parsed = parseExportAiRunApiResponse(item);
    if (parsed.status === "failure") return parsed;
    runs.push(parsed.value);
  }

  return ok(runs);
}

export function parseExportAiRecommendationListApiResponse(
  raw: unknown,
): ParseResult<ExportAiRecommendation[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const recommendations: ExportAiRecommendation[] = [];
  for (const item of raw) {
    const parsed = parseExportAiRecommendationApiResponse(item);
    if (parsed.status === "failure") return parsed;
    recommendations.push(parsed.value);
  }

  return ok(recommendations);
}

export function parseAiUsageSummaryApiResponse(
  raw: unknown,
): ParseResult<AiUsageSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isNumber(o.creditsConsumedThisPeriod)) {
    return fail("creditsConsumedThisPeriod is required.");
  }
  if (!isNumber(o.usagePercent)) return fail("usagePercent is required.");
  if (!isString(o.warningLevel)) return fail("warningLevel is required.");

  return ok({
    creditsConsumedThisPeriod: o.creditsConsumedThisPeriod,
    monthlyQuota:
      o.monthlyQuota === undefined || o.monthlyQuota === null
        ? undefined
        : isNumber(o.monthlyQuota)
          ? o.monthlyQuota
          : undefined,
    usagePercent: o.usagePercent,
    warningLevel: o.warningLevel,
  });
}

export type ExportAiAnalysisWriteSuccess = Readonly<{
  analysisRunId: string;
  runVersion: number;
}>;

export type ExportAiRecommendationWriteSuccess = Readonly<{
  recommendationVersion: number;
  caseVersion: number;
}>;

export function parseExportAiAnalysisWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ExportAiAnalysisWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.analysisRunId)) return fail("analysisRunId is required.");
  if (!isNumber(o.runVersion)) return fail("runVersion is required.");

  return ok({
    analysisRunId: o.analysisRunId,
    runVersion: o.runVersion,
  });
}

export function parseExportAiRecommendationWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ExportAiRecommendationWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isNumber(o.recommendationVersion)) {
    return fail("recommendationVersion is required.");
  }
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");

  return ok({
    recommendationVersion: o.recommendationVersion,
    caseVersion: o.caseVersion,
  });
}

// ─── EXPORT-4L fetch helpers ─────────────────────────────────────────────────

export async function listExportAiRuns(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportAiAnalysisRun[]>> {
  const url = caseFacilityUrl(context, exportCaseId, "/ai/runs");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportAiRunListApiResponse(body)),
  );
}

export async function getExportAiRunDetail(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  runId: string,
): Promise<ExportCaseApiResult<ExportAiRunDetail>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/ai/runs/${encodeURIComponent(runId.trim())}`,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseExportAiRunDetailApiResponse(body)),
  );
}

export async function listExportAiRecommendations(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportAiRecommendation[]>> {
  const url = caseFacilityUrl(context, exportCaseId, "/ai/recommendations");
  return executeGet(context, url, (body) =>
    mapParsed(parseExportAiRecommendationListApiResponse(body)),
  );
}

export async function startExportAiAnalysis(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  input: StartExportAiAnalysisInput,
): Promise<ExportCaseApiResult<ExportAiAnalysisWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/ai/analyze");
  return executePost(context, url, input, (body) =>
    mapParsed(parseExportAiAnalysisWriteSuccessApiResponse(body)),
  );
}

export async function explainExportReadiness(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportAiAnalysisWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/ai/compliance-explanation");
  return executePost(context, url, {}, (body) =>
    mapParsed(parseExportAiAnalysisWriteSuccessApiResponse(body)),
  );
}

export async function suggestExportNextActions(
  context: ExportCaseRequestContext,
  exportCaseId: string,
): Promise<ExportCaseApiResult<ExportAiAnalysisWriteSuccess>> {
  const url = caseFacilityUrl(context, exportCaseId, "/ai/next-actions");
  return executePost(context, url, {}, (body) =>
    mapParsed(parseExportAiAnalysisWriteSuccessApiResponse(body)),
  );
}

export async function acceptExportAiRecommendation(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  recommendationId: string,
  expectedVersion: number,
  reviewNote?: string,
): Promise<ExportCaseApiResult<ExportAiRecommendationWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/ai/recommendations/${encodeURIComponent(recommendationId.trim())}/accept`,
  );
  return executePost(
    context,
    url,
    { expectedVersion, reviewNote: reviewNote?.trim() || undefined },
    (body) => mapParsed(parseExportAiRecommendationWriteSuccessApiResponse(body)),
  );
}

export async function rejectExportAiRecommendation(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  recommendationId: string,
  expectedVersion: number,
  reviewNote?: string,
): Promise<ExportCaseApiResult<ExportAiRecommendationWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/ai/recommendations/${encodeURIComponent(recommendationId.trim())}/reject`,
  );
  return executePost(
    context,
    url,
    { expectedVersion, reviewNote: reviewNote?.trim() || undefined },
    (body) => mapParsed(parseExportAiRecommendationWriteSuccessApiResponse(body)),
  );
}

export async function dismissExportAiRecommendation(
  context: ExportCaseRequestContext,
  exportCaseId: string,
  recommendationId: string,
  expectedVersion: number,
  reviewNote?: string,
): Promise<ExportCaseApiResult<ExportAiRecommendationWriteSuccess>> {
  const url = caseFacilityUrl(
    context,
    exportCaseId,
    `/ai/recommendations/${encodeURIComponent(recommendationId.trim())}/dismiss`,
  );
  return executePost(
    context,
    url,
    { expectedVersion, reviewNote: reviewNote?.trim() || undefined },
    (body) => mapParsed(parseExportAiRecommendationWriteSuccessApiResponse(body)),
  );
}

export async function listResearchAiRuns(
  context: ExportCaseRequestContext,
  researchCaseId: string,
): Promise<ExportCaseApiResult<ExportAiAnalysisRun[]>> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}/ai/runs`,
  );
  return executeGet(context, url, (body) =>
    mapParsed(parseExportAiRunListApiResponse(body)),
  );
}

export async function startResearchAiAnalysis(
  context: ExportCaseRequestContext,
  researchCaseId: string,
  input: StartExportAiAnalysisInput,
): Promise<ExportCaseApiResult<ExportAiAnalysisWriteSuccess>> {
  const url = exportFacilityUrl(
    context,
    `/api/export/country-research/${encodeURIComponent(researchCaseId.trim())}/ai/analyze`,
  );
  return executePost(context, url, input, (body) =>
    mapParsed(parseExportAiAnalysisWriteSuccessApiResponse(body)),
  );
}

export async function getCurrentAiUsage(
  context: ExportCaseRequestContext,
): Promise<ExportCaseApiResult<AiUsageSummary>> {
  const url = exportFacilityUrl(context, "/api/ai/usage/current");
  return executeGet(context, url, (body) =>
    mapParsed(parseAiUsageSummaryApiResponse(body)),
  );
}
