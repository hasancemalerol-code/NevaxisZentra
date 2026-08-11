import type {
  MaintenancePredictiveRecommendation,
  MaintenancePredictiveRecommendationCategory,
  MaintenancePredictiveReviewPriority,
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskBand,
  MaintenancePredictiveRiskDriver,
  MaintenancePredictiveRiskEvidence,
  MaintenancePredictiveRiskOverview,
  MaintenancePredictiveRiskWorkspace,
} from "../domain/maintenance-predictive-risk";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function parseString(value: unknown, field: string): ParseResult<string> {
  if (typeof value !== "string" || value.trim().length === 0) {
    return { status: "failure", message: `${field} is required.` };
  }
  return { status: "success", value };
}

function parseNumber(value: unknown, field: string): ParseResult<number> {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return { status: "failure", message: `${field} is required.` };
  }
  return { status: "success", value };
}

function parseOptionalString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

const RISK_BANDS = new Set<MaintenancePredictiveRiskBand>([
  "insufficient-data",
  "low",
  "watch",
  "high",
  "critical",
]);

const REVIEW_PRIORITIES = new Set<MaintenancePredictiveReviewPriority>([
  "routine",
  "elevated",
  "planned",
  "urgent",
]);

const RECOMMENDATION_CATEGORIES = new Set<MaintenancePredictiveRecommendationCategory>([
  "review-data-quality",
  "continue-routine-monitoring",
  "close-monitoring",
  "plan-technical-review",
  "urgent-authorized-technical-review",
]);

function parseRiskBand(value: unknown): ParseResult<MaintenancePredictiveRiskBand> {
  if (typeof value !== "string" || !RISK_BANDS.has(value as MaintenancePredictiveRiskBand)) {
    return { status: "failure", message: "riskBand is invalid." };
  }
  return { status: "success", value: value as MaintenancePredictiveRiskBand };
}

function parseReviewPriority(value: unknown): ParseResult<MaintenancePredictiveReviewPriority> {
  if (typeof value !== "string" || !REVIEW_PRIORITIES.has(value as MaintenancePredictiveReviewPriority)) {
    return { status: "failure", message: "reviewPriority is invalid." };
  }
  return { status: "success", value: value as MaintenancePredictiveReviewPriority };
}

function parseRecommendationCategory(
  value: unknown,
): ParseResult<MaintenancePredictiveRecommendationCategory> {
  if (
    typeof value !== "string" ||
    !RECOMMENDATION_CATEGORIES.has(value as MaintenancePredictiveRecommendationCategory)
  ) {
    return { status: "failure", message: "category is invalid." };
  }
  return { status: "success", value: value as MaintenancePredictiveRecommendationCategory };
}

function parseDriver(value: unknown): ParseResult<MaintenancePredictiveRiskDriver> {
  if (typeof value !== "object" || value === null) {
    return { status: "failure", message: "riskDriver is invalid." };
  }
  const record = value as Record<string, unknown>;
  const driverCode = parseString(record.driverCode, "driverCode");
  const summary = parseString(record.summary, "summary");
  if (driverCode.status === "failure") {
    return driverCode;
  }
  if (summary.status === "failure") {
    return summary;
  }
  return { status: "success", value: { driverCode: driverCode.value, summary: summary.value } };
}

function parseEvidence(value: unknown): ParseResult<MaintenancePredictiveRiskEvidence> {
  if (typeof value !== "object" || value === null) {
    return { status: "failure", message: "evidence is invalid." };
  }
  const record = value as Record<string, unknown>;
  const evidenceType = parseString(record.evidenceType, "evidenceType");
  const evidenceReference = parseString(record.evidenceReference, "evidenceReference");
  const summary = parseString(record.summary, "summary");
  if (evidenceType.status === "failure") {
    return evidenceType;
  }
  if (evidenceReference.status === "failure") {
    return evidenceReference;
  }
  if (summary.status === "failure") {
    return summary;
  }
  return {
    status: "success",
    value: {
      evidenceType: evidenceType.value,
      evidenceReference: evidenceReference.value,
      summary: summary.value,
    },
  };
}

function parseRecommendation(value: unknown): ParseResult<MaintenancePredictiveRecommendation | undefined> {
  if (value === null || value === undefined) {
    return { status: "success", value: undefined };
  }
  if (typeof value !== "object") {
    return { status: "failure", message: "recommendation is invalid." };
  }
  const record = value as Record<string, unknown>;
  const category = parseRecommendationCategory(record.category);
  const reviewPriority = parseReviewPriority(record.reviewPriority);
  const explanation = parseString(record.explanation, "explanation");
  const ruleSetVersion = parseString(record.ruleSetVersion, "ruleSetVersion");
  const generatedAtUtc = parseString(record.generatedAtUtc, "generatedAtUtc");
  const status = parseString(record.status, "status");
  if (
    category.status === "failure" ||
    reviewPriority.status === "failure" ||
    explanation.status === "failure" ||
    ruleSetVersion.status === "failure" ||
    generatedAtUtc.status === "failure" ||
    status.status === "failure"
  ) {
    return { status: "failure", message: "recommendation is invalid." };
  }
  return {
    status: "success",
    value: {
      category: category.value,
      reviewPriority: reviewPriority.value,
      explanation: explanation.value,
      ruleSetVersion: ruleSetVersion.value,
      generatedAtUtc: generatedAtUtc.value,
      status: status.value,
    },
  };
}

export function parseMaintenancePredictiveRiskOverview(payload: unknown): ParseResult<MaintenancePredictiveRiskOverview> {
  if (typeof payload !== "object" || payload === null) {
    return { status: "failure", message: "overview is invalid." };
  }
  const record = payload as Record<string, unknown>;
  const fields = [
    parseNumber(record.monitoredAssetCount, "monitoredAssetCount"),
    parseNumber(record.insufficientDataCount, "insufficientDataCount"),
    parseNumber(record.lowRiskCount, "lowRiskCount"),
    parseNumber(record.watchCount, "watchCount"),
    parseNumber(record.highRiskCount, "highRiskCount"),
    parseNumber(record.criticalRiskCount, "criticalRiskCount"),
  ];
  if (fields.some((field) => field.status === "failure")) {
    return { status: "failure", message: "overview is invalid." };
  }
  return {
    status: "success",
    value: {
      monitoredAssetCount: fields[0].status === "success" ? fields[0].value : 0,
      insufficientDataCount: fields[1].status === "success" ? fields[1].value : 0,
      lowRiskCount: fields[2].status === "success" ? fields[2].value : 0,
      watchCount: fields[3].status === "success" ? fields[3].value : 0,
      highRiskCount: fields[4].status === "success" ? fields[4].value : 0,
      criticalRiskCount: fields[5].status === "success" ? fields[5].value : 0,
      lastAssessedAtUtc: parseOptionalString(record.lastAssessedAtUtc),
    },
  };
}

export function parseMaintenancePredictiveRiskAssetRow(payload: unknown): ParseResult<MaintenancePredictiveRiskWorkspace["assets"][number]> {
  if (typeof payload !== "object" || payload === null) {
    return { status: "failure", message: "asset row is invalid." };
  }
  const record = payload as Record<string, unknown>;
  const assetReference = parseString(record.assetReference, "assetReference");
  const riskBand = parseRiskBand(record.riskBand);
  const reviewPriority = parseReviewPriority(record.reviewPriority);
  const dataFreshness = parseString(record.dataFreshness, "dataFreshness");
  const dataSufficiency = parseString(record.dataSufficiency, "dataSufficiency");
  if (
    assetReference.status === "failure" ||
    riskBand.status === "failure" ||
    reviewPriority.status === "failure" ||
    dataFreshness.status === "failure" ||
    dataSufficiency.status === "failure"
  ) {
    return { status: "failure", message: "asset row is invalid." };
  }
  return {
    status: "success",
    value: {
      assetReference: assetReference.value,
      assetName: parseOptionalString(record.assetName),
      machineCode: parseOptionalString(record.machineCode),
      machineName: parseOptionalString(record.machineName),
      riskBand: riskBand.value,
      reviewPriority: reviewPriority.value,
      dataFreshness: dataFreshness.value,
      dataSufficiency: dataSufficiency.value,
      assessedAtUtc: parseOptionalString(record.assessedAtUtc),
    },
  };
}

export function parseMaintenancePredictiveRiskWorkspace(payload: unknown): ParseResult<MaintenancePredictiveRiskWorkspace> {
  if (typeof payload !== "object" || payload === null) {
    return { status: "failure", message: "risk workspace is invalid." };
  }
  const record = payload as Record<string, unknown>;
  const overview = parseMaintenancePredictiveRiskOverview(record.overview);
  if (overview.status === "failure") {
    return overview;
  }
  if (!Array.isArray(record.assets)) {
    return { status: "failure", message: "assets is invalid." };
  }
  const assets: MaintenancePredictiveRiskWorkspace["assets"][number][] = [];
  for (const item of record.assets) {
    const parsed = parseMaintenancePredictiveRiskAssetRow(item);
    if (parsed.status === "failure") {
      return parsed;
    }
    assets.push(parsed.value);
  }
  return { status: "success", value: { overview: overview.value, assets } };
}

export function parseMaintenancePredictiveRiskAssessment(
  payload: unknown,
): ParseResult<MaintenancePredictiveRiskAssessment> {
  if (typeof payload !== "object" || payload === null) {
    return { status: "failure", message: "assessment is invalid." };
  }
  const record = payload as Record<string, unknown>;
  const assetReference = parseString(record.assetReference, "assetReference");
  const riskBand = parseRiskBand(record.riskBand);
  const reviewPriority = parseReviewPriority(record.reviewPriority);
  const ruleSetVersion = parseString(record.ruleSetVersion, "ruleSetVersion");
  const dataFreshness = parseString(record.dataFreshness, "dataFreshness");
  const dataSufficiency = parseString(record.dataSufficiency, "dataSufficiency");
  const activeAnomalyCount = parseNumber(record.activeAnomalyCount, "activeAnomalyCount");
  const maintenanceHistorySummary = parseString(record.maintenanceHistorySummary, "maintenanceHistorySummary");
  const explanation = parseString(record.explanation, "explanation");
  const assessedAtUtc = parseString(record.assessedAtUtc, "assessedAtUtc");
  if (
    assetReference.status === "failure" ||
    riskBand.status === "failure" ||
    reviewPriority.status === "failure" ||
    ruleSetVersion.status === "failure" ||
    dataFreshness.status === "failure" ||
    dataSufficiency.status === "failure" ||
    activeAnomalyCount.status === "failure" ||
    maintenanceHistorySummary.status === "failure" ||
    explanation.status === "failure" ||
    assessedAtUtc.status === "failure"
  ) {
    return { status: "failure", message: "assessment is invalid." };
  }

  const drivers: MaintenancePredictiveRiskDriver[] = [];
  if (Array.isArray(record.riskDrivers)) {
    for (const item of record.riskDrivers) {
      const parsed = parseDriver(item);
      if (parsed.status === "failure") {
        return parsed;
      }
      drivers.push(parsed.value);
    }
  }

  const evidence: MaintenancePredictiveRiskEvidence[] = [];
  if (Array.isArray(record.evidence)) {
    for (const item of record.evidence) {
      const parsed = parseEvidence(item);
      if (parsed.status === "failure") {
        return parsed;
      }
      evidence.push(parsed.value);
    }
  }

  const recommendation = parseRecommendation(record.recommendation);
  if (recommendation.status === "failure") {
    return recommendation;
  }

  return {
    status: "success",
    value: {
      assetReference: assetReference.value,
      assetName: parseOptionalString(record.assetName),
      machineCode: parseOptionalString(record.machineCode),
      machineName: parseOptionalString(record.machineName),
      riskBand: riskBand.value,
      reviewPriority: reviewPriority.value,
      ruleSetVersion: ruleSetVersion.value,
      conditionEvaluationIdentity: parseOptionalString(record.conditionEvaluationIdentity),
      dataFreshness: dataFreshness.value,
      dataSufficiency: dataSufficiency.value,
      activeAnomalyCount: activeAnomalyCount.value,
      maintenanceHistorySummary: maintenanceHistorySummary.value,
      explanation: explanation.value,
      assessedAtUtc: assessedAtUtc.value,
      riskDrivers: drivers,
      evidence,
      recommendation: recommendation.value,
    },
  };
}
