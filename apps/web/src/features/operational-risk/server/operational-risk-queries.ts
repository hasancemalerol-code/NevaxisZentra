import "./operational-risk-server-boundary";

import {
  formatOperationalRiskCompactPeriod,
  formatOperationalRiskCount,
  formatOperationalRiskDateTime,
} from "../domain/operational-risk-formatters";
import {
  getOperationalRiskAssessmentLabel,
  getOperationalRiskCategoryLabel,
  getOperationalRiskDetectionOutcomeLabel,
  getOperationalRiskDriverLabel,
  getOperationalRiskEvidenceLabel,
  getOperationalRiskLifecycleLabel,
  getOperationalRiskProfileStatusLabel,
  getOperationalRiskRunStatusLabel,
  getOperationalRiskScopeLabel,
  getOperationalRiskSeverityLabel,
  getOperationalRiskSourceModuleLabel,
  OPERATIONAL_RISK_ACTIVE_SOURCE_MODULES,
  OPERATIONAL_RISK_ALLOWED_TRANSITIONS,
  OPERATIONAL_RISK_KPI_ORDER,
  OPERATIONAL_RISK_SOURCE_MODULES,
  OPERATIONAL_RISK_TERMINAL_STATUSES,
} from "../domain/operational-risk-labels";
import {
  extractOperationalRiskCaseDrivers,
  localizeOperationalRiskCaseTitle,
  localizeOperationalRiskEvidenceSummary,
  localizeOperationalRiskVisibleText,
  localizeOperationalRiskVisibleToken,
  collectOperationalRiskTechnicalAuditTokens,
  OPERATIONAL_RISK_OUTCOME_COLORS,
  OPERATIONAL_RISK_SEVERITY_COLORS,
  reconcileOperationalRiskDistribution,
} from "../components/operational-risk-presentation";
import type {
  OperationalRiskCaseDetail,
  OperationalRiskCaseSummary,
  OperationalRiskDetectionProfileSummary,
  OperationalRiskDetectionRunDetail,
  OperationalRiskDetectionRunSummary,
  OperationalRiskDetectionSummary,
  OperationalRiskKpiCard,
  OperationalRiskSourceAdapterCoverage,
  OperationalRiskWorkspacePayload,
} from "../types/operational-risk-types";
import {
  getOperationalRiskCase,
  getOperationalRiskDetectionRun,
  listOperationalRiskCases,
  listOperationalRiskDetectionProfiles,
  listOperationalRiskDetectionRuns,
  type RawOperationalRiskCaseDetail,
  type RawOperationalRiskCaseSummary,
  type RawOperationalRiskDetectionProfile,
  type RawOperationalRiskDetectionRunDetail,
  type RawOperationalRiskDetectionRunSummary,
} from "./operational-risk-api-client";
import { resolveOperationalRiskRequestContext } from "./operational-risk-request-context";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type OperationalRiskWorkspaceResult =
  | Readonly<{ status: "success"; workspace: OperationalRiskWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type OperationalRiskCaseDetailResult =
  | Readonly<{ status: "success"; detail: OperationalRiskCaseDetail }>
  | ReadFailure;

export type OperationalRiskRunDetailResult =
  | Readonly<{ status: "success"; detail: OperationalRiskDetectionRunDetail }>
  | ReadFailure;

const SEVERITY_ORDER: Record<string, number> = {
  critical: 0,
  high: 1,
  moderate: 2,
  low: 3,
};

function mapCaseSummary(
  item: RawOperationalRiskCaseSummary,
  sourceModule: string | null = null,
): OperationalRiskCaseSummary {
  return {
    id: item.id,
    caseReference: item.caseReference,
    category: item.category,
    categoryLabel: getOperationalRiskCategoryLabel(item.category),
    scopeType: item.scopeType,
    scopeLabel: getOperationalRiskScopeLabel(item.scopeType),
    scopeReference: item.scopeReference,
    title: localizeOperationalRiskCaseTitle(item.title, item.category),
    evidenceState: item.evidenceState,
    evidenceLabel: getOperationalRiskEvidenceLabel(item.evidenceState),
    likelihoodLevel: item.likelihoodLevel,
    impactLevel: item.impactLevel,
    riskScore: item.riskScore,
    severityBand: item.severityBand,
    severityLabel: getOperationalRiskSeverityLabel(item.severityBand),
    assessmentOutcome: item.assessmentOutcome,
    assessmentLabel: getOperationalRiskAssessmentLabel(item.assessmentOutcome),
    lifecycleStatus: item.lifecycleStatus,
    lifecycleLabel: getOperationalRiskLifecycleLabel(item.lifecycleStatus),
    sourceModule,
    sourceModuleLabel: getOperationalRiskSourceModuleLabel(sourceModule),
    ruleSetVersion: item.ruleSetVersion,
    firstDetectedAtUtc: item.firstDetectedAtUtc,
    lastObservedAtUtc: item.lastObservedAtUtc,
    lastObservedLabel: formatOperationalRiskDateTime(item.lastObservedAtUtc),
    version: item.version,
    isPilotData: item.isPilotData,
    isTerminal: OPERATIONAL_RISK_TERMINAL_STATUSES.has(item.lifecycleStatus),
  };
}

function sortCases(cases: readonly OperationalRiskCaseSummary[]): OperationalRiskCaseSummary[] {
  return [...cases].sort((left, right) => {
    const leftSeverity = SEVERITY_ORDER[left.severityBand ?? "unknown"] ?? 99;
    const rightSeverity = SEVERITY_ORDER[right.severityBand ?? "unknown"] ?? 99;
    if (leftSeverity !== rightSeverity) return leftSeverity - rightSeverity;
    return Date.parse(right.lastObservedAtUtc) - Date.parse(left.lastObservedAtUtc);
  });
}

function buildKpis(cases: readonly OperationalRiskCaseSummary[]): OperationalRiskKpiCard[] {
  const openCases = cases.filter((item) => !item.isTerminal);
  const criticalHigh = openCases.filter(
    (item) => item.severityBand === "critical" || item.severityBand === "high",
  );
  const detected = openCases.filter((item) => item.lifecycleStatus === "detected");
  const underReview = openCases.filter((item) => item.lifecycleStatus === "under-review");
  const insufficient = cases.filter(
    (item) =>
      item.evidenceState === "insufficient-data"
      || item.assessmentOutcome === "insufficient-data",
  );
  const conflict = cases.filter(
    (item) =>
      item.evidenceState === "source-conflict"
      || item.assessmentOutcome === "source-conflict",
  );

  const scopeLabel = "Açık vakalar";
  const evidenceLabel = "Vaka kayıtları";

  const cards: Record<string, OperationalRiskKpiCard> = {
    "open-cases": {
      id: "open-cases",
      label: "Açık risk vakaları",
      value: formatOperationalRiskCount(openCases.length),
      detail: "Terminal olmayan vakalar",
      tone: openCases.length > 0 ? "attention" : "controlled",
      scopeLabel,
      evidenceLabel,
      stateBadge: openCases.length > 0 ? "Açık" : "Kontrollü",
    },
    "critical-high": {
      id: "critical-high",
      label: "Kritik / yüksek öncelik",
      value: formatOperationalRiskCount(criticalHigh.length),
      detail: "Kritik ve yüksek şiddet bantları",
      tone: criticalHigh.length > 0 ? "critical" : "controlled",
      scopeLabel,
      evidenceLabel,
      stateBadge: criticalHigh.length > 0 ? "Öncelikli" : "Düşük",
    },
    "new-detections": {
      id: "new-detections",
      label: "Yeni tespitler",
      value: formatOperationalRiskCount(detected.length),
      detail: "Tespit edildi durumundaki vakalar",
      tone: detected.length > 0 ? "informational" : "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: detected.length > 0 ? "Yeni" : "Yok",
    },
    "under-review": {
      id: "under-review",
      label: "İncelemede",
      value: formatOperationalRiskCount(underReview.length),
      detail: "İnceleme sürecindeki vakalar",
      tone: underReview.length > 0 ? "attention" : "neutral",
      scopeLabel,
      evidenceLabel,
      stateBadge: underReview.length > 0 ? "İncelemede" : "Yok",
    },
    "insufficient-data": {
      id: "insufficient-data",
      label: "Veri yetersiz",
      value: formatOperationalRiskCount(insufficient.length),
      detail: "Yetersiz kanıt veya değerlendirme",
      tone: insufficient.length > 0 ? "insufficient" : "neutral",
      scopeLabel: "Tüm vakalar",
      evidenceLabel,
      stateBadge: insufficient.length > 0 ? "Eksik veri" : "Yok",
    },
    "source-conflict": {
      id: "source-conflict",
      label: "Kaynak uyuşmazlığı",
      value: formatOperationalRiskCount(conflict.length),
      detail: "Çelişkili kaynak kanıtı",
      tone: conflict.length > 0 ? "conflict" : "neutral",
      scopeLabel: "Tüm vakalar",
      evidenceLabel,
      stateBadge: conflict.length > 0 ? "Çelişki" : "Yok",
    },
  };

  return OPERATIONAL_RISK_KPI_ORDER.map((id) => cards[id]);
}

function buildDistribution(cases: readonly OperationalRiskCaseSummary[]) {
  const openCases = cases.filter((item) => !item.isTerminal);
  const buckets = new Map<string, number>();
  for (const item of openCases) {
    const key = item.severityBand ?? "unknown";
    buckets.set(key, (buckets.get(key) ?? 0) + 1);
  }

  const entries = [...buckets.entries()].map(([key, count]) => ({
    key,
    label: getOperationalRiskSeverityLabel(key === "unknown" ? null : key),
    count,
    color: OPERATIONAL_RISK_SEVERITY_COLORS[key] ?? OPERATIONAL_RISK_SEVERITY_COLORS.unknown,
  }));

  const reconciled = reconcileOperationalRiskDistribution(entries);
  return {
    slices: reconciled.slices,
    total: reconciled.total,
    centerLabel: reconciled.total > 0 ? formatOperationalRiskCount(reconciled.total) : "—",
    unavailableReason: reconciled.total > 0 ? null : "Karşılaştırılabilir açık vaka yok",
  };
}

function buildSourceCoverage(
  cases: readonly OperationalRiskCaseSummary[],
  runResults: RawOperationalRiskDetectionRunDetail["results"],
): OperationalRiskSourceAdapterCoverage[] {
  const caseModules = new Set(
    cases.map((item) => item.sourceModule).filter((value): value is string => Boolean(value)),
  );
  const resultModules = new Map<string, Set<string>>();
  for (const result of runResults) {
    const set = resultModules.get(result.sourceModule) ?? new Set<string>();
    set.add(result.detectionOutcome);
    resultModules.set(result.sourceModule, set);
  }

  return OPERATIONAL_RISK_SOURCE_MODULES.map((module) => {
    const label = getOperationalRiskSourceModuleLabel(module);
    const outcomes = resultModules.get(module);
    const hasConflict = outcomes?.has("source-conflict") ?? false;
    const hasInsufficient = outcomes?.has("insufficient-data") ?? false;
    const hasActiveEvidence = OPERATIONAL_RISK_ACTIVE_SOURCE_MODULES.has(module)
      && (caseModules.has(module) || outcomes?.has("risk-detected") || outcomes?.has("within-threshold"));

    if (!OPERATIONAL_RISK_ACTIVE_SOURCE_MODULES.has(module)) {
      return {
        module,
        label,
        state: "unavailable" as const,
        detail: "Kaynak adaptörü henüz kullanılamıyor",
        evidenceLabel: "Kullanılamıyor",
      };
    }

    if (hasConflict) {
      return {
        module,
        label,
        state: "source-conflict" as const,
        detail: "Son tespit çalıştırmasında kaynak uyuşmazlığı",
        evidenceLabel: "Kaynak uyuşmazlığı",
      };
    }

    if (hasInsufficient) {
      return {
        module,
        label,
        state: "insufficient-data" as const,
        detail: "Son tespit çalıştırmasında yetersiz veri",
        evidenceLabel: "Veri yetersiz",
      };
    }

    if (hasActiveEvidence) {
      return {
        module,
        label,
        state: "active" as const,
        detail: "Aktif kaynak kanıtı mevcut",
        evidenceLabel: "Aktif kanıt",
      };
    }

    return {
      module,
      label,
      state: "insufficient-data" as const,
      detail: "Bu pencerede kanıt bulunamadı",
      evidenceLabel: "Veri yetersiz",
    };
  });
}

function mapProfile(profile: RawOperationalRiskDetectionProfile): OperationalRiskDetectionProfileSummary {
  const activeRules = profile.rules.filter((rule) => rule.isActive);
  const sourceModules = [...new Set(profile.rules.map((rule) => rule.sourceModule))];
  const categories = [...new Set(profile.rules.map((rule) => rule.riskCategory))];
  return {
    id: profile.id,
    profileCode: profile.profileCode,
    displayName: profile.displayName,
    status: profile.status,
    statusLabel: getOperationalRiskProfileStatusLabel(profile.status),
    ruleSetVersion: profile.ruleSetVersion,
    sourceProvenance: profile.sourceProvenance,
    version: profile.version,
    ruleCount: profile.rules.length,
    activeRuleCount: activeRules.length,
    sourceModules,
    sourceModuleLabels: sourceModules.map(getOperationalRiskSourceModuleLabel),
    categories,
    categoryLabels: categories.map(getOperationalRiskCategoryLabel),
    rules: profile.rules
      .slice()
      .sort((left, right) => left.priorityOrder - right.priorityOrder)
      .map((rule) => ({
        id: rule.id,
        ruleCode: rule.ruleCode,
        sourceModule: rule.sourceModule,
        sourceModuleLabel: getOperationalRiskSourceModuleLabel(rule.sourceModule),
        riskCategory: rule.riskCategory,
        categoryLabel: getOperationalRiskCategoryLabel(rule.riskCategory),
        thresholdVarianceBand: rule.thresholdVarianceBand,
        driverReasonCode: rule.driverReasonCode,
        driverLabel: getOperationalRiskDriverLabel(rule.driverReasonCode),
        isActive: rule.isActive,
        priorityOrder: rule.priorityOrder,
      })),
    isExecutable: profile.status === "approved",
    isPilotData: profile.isPilotData,
    updatedLabel: profile.sourceProvenance.toLowerCase().includes("simul")
      ? "Simülasyon profili"
      : "Profil kaydı",
  };
}

function countRunOutcomes(
  run: RawOperationalRiskDetectionRunSummary,
  detail: RawOperationalRiskDetectionRunDetail | null,
): Record<string, number> {
  const counts: Record<string, number> = {
    "risk-detected": 0,
    "within-threshold": 0,
    "insufficient-data": 0,
    "source-conflict": 0,
  };
  const results = detail?.results ?? [];
  for (const result of results) {
    counts[result.detectionOutcome] = (counts[result.detectionOutcome] ?? 0) + 1;
  }
  if (results.length === 0 && run.resultCount > 0) {
    return counts;
  }
  return counts;
}

function mapRunSummary(
  run: RawOperationalRiskDetectionRunSummary,
  detail: RawOperationalRiskDetectionRunDetail | null,
): OperationalRiskDetectionRunSummary {
  return {
    id: run.id,
    runReference: run.runReference,
    profileId: run.profileId,
    profileCode: run.profileCode,
    windowStartUtc: run.windowStartUtc,
    windowEndUtc: run.windowEndUtc,
    windowLabel: formatOperationalRiskCompactPeriod(run.windowStartUtc, run.windowEndUtc),
    status: run.status,
    statusLabel: getOperationalRiskRunStatusLabel(run.status),
    sourceCount: run.sourceCount,
    resultCount: run.resultCount,
    outcomeCounts: countRunOutcomes(run, detail),
    ruleSetVersion: run.ruleSetVersion,
    createdAtUtc: run.createdAtUtc,
    createdLabel: formatOperationalRiskDateTime(run.createdAtUtc),
    completedAtUtc: run.completedAtUtc,
    completedLabel: run.completedAtUtc
      ? formatOperationalRiskDateTime(run.completedAtUtc)
      : null,
    isPilotData: run.isPilotData,
  };
}

function buildDetectionSummary(
  runs: readonly OperationalRiskDetectionRunSummary[],
): OperationalRiskDetectionSummary {
  const outcomeTotals: Record<string, number> = {
    "risk-detected": 0,
    "within-threshold": 0,
    "insufficient-data": 0,
    "source-conflict": 0,
  };
  for (const run of runs) {
    for (const [key, value] of Object.entries(run.outcomeCounts)) {
      outcomeTotals[key] = (outcomeTotals[key] ?? 0) + value;
    }
  }
  const reconciledTotal = Object.values(outcomeTotals).reduce((sum, value) => sum + value, 0);
  return {
    recentRuns: runs.slice(0, 5),
    outcomeTotals,
    reconciledTotal,
    hasRuns: runs.length > 0,
  };
}

function reportingPeriodLabel(cases: readonly OperationalRiskCaseSummary[]): string {
  if (cases.length === 0) return "—";
  const timestamps = cases
    .flatMap((item) => [Date.parse(item.firstDetectedAtUtc), Date.parse(item.lastObservedAtUtc)])
    .filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  const min = Math.min(...timestamps);
  const max = Math.max(...timestamps);
  return formatOperationalRiskCompactPeriod(new Date(min).toISOString(), new Date(max).toISOString());
}

function freshnessLabel(cases: readonly OperationalRiskCaseSummary[]): string {
  if (cases.length === 0) return "—";
  const latest = Math.max(...cases.map((item) => Date.parse(item.lastObservedAtUtc)));
  return formatOperationalRiskDateTime(new Date(latest).toISOString());
}

export async function readOperationalRiskWorkspace(): Promise<OperationalRiskWorkspaceResult> {
  const contextResult = await resolveOperationalRiskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "operational-risk.manage");

  const [casesResult, profilesResult, runsResult] = await Promise.all([
    listOperationalRiskCases(contextResult.context, { page: 1, pageSize: 200 }),
    listOperationalRiskDetectionProfiles(contextResult.context),
    listOperationalRiskDetectionRuns(contextResult.context, 1, 50),
  ]);

  if (casesResult.status === "failure") {
    return { status: "failure", message: casesResult.message, code: casesResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }
  if (runsResult.status === "failure") {
    return { status: "failure", message: runsResult.message, code: runsResult.code };
  }

  const caseSummaries = casesResult.value.items.map((item) => mapCaseSummary(item));
  const sortedCases = sortCases(caseSummaries);

  const latestRun = runsResult.value.items[0] ?? null;
  let latestRunDetail: RawOperationalRiskDetectionRunDetail | null = null;
  if (latestRun) {
    const detailResult = await getOperationalRiskDetectionRun(contextResult.context, latestRun.id);
    if (detailResult.status === "success") {
      latestRunDetail = detailResult.value;
    }
  }

  const runDetails = new Map<string, RawOperationalRiskDetectionRunDetail | null>();
  for (const run of runsResult.value.items.slice(0, 10)) {
    const detailResult = await getOperationalRiskDetectionRun(contextResult.context, run.id);
    runDetails.set(run.id, detailResult.status === "success" ? detailResult.value : null);
  }

  const mappedRuns = runsResult.value.items.map((run) =>
    mapRunSummary(run, runDetails.get(run.id) ?? null),
  );

  const hasPilot = caseSummaries.some((item) => item.isPilotData)
    || profilesResult.value.items.some((item) => item.isPilotData)
    || mappedRuns.some((item) => item.isPilotData);

  const workspace: OperationalRiskWorkspacePayload = {
    facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
    reportingPeriodLabel: reportingPeriodLabel(sortedCases),
    freshnessLabel: freshnessLabel(sortedCases),
    evidenceLabel: hasPilot ? "Simülasyon kanıtı" : "Operasyonel risk kanıtı",
    provenanceDisclaimer: casesResult.value.provenanceDisclaimer
      || profilesResult.value.provenanceDisclaimer,
    ruleSetVersion: casesResult.value.ruleSetVersion || runsResult.value.ruleSetVersion,
    hasDevelopmentPilotData: hasPilot,
    kpis: buildKpis(sortedCases),
    distribution: buildDistribution(sortedCases),
    sourceCoverage: buildSourceCoverage(
      sortedCases,
      latestRunDetail?.results ?? [],
    ),
    priorityCases: sortedCases.filter((item) => !item.isTerminal).slice(0, 3),
    detectionSummary: buildDetectionSummary(mappedRuns),
    cases: sortedCases,
    profiles: profilesResult.value.items.map(mapProfile),
    runs: mappedRuns,
  };

  return { status: "success", workspace, canManage };
}

export async function readOperationalRiskCaseDetail(caseId: string): Promise<OperationalRiskCaseDetailResult> {
  const contextResult = await resolveOperationalRiskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getOperationalRiskCase(contextResult.context, caseId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapCaseDetail(result.value) };
}

export async function readOperationalRiskRunDetail(runId: string): Promise<OperationalRiskRunDetailResult> {
  const contextResult = await resolveOperationalRiskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getOperationalRiskDetectionRun(contextResult.context, runId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapRunDetail(result.value) };
}

function mapCaseDetail(item: RawOperationalRiskCaseDetail): OperationalRiskCaseDetail {
  const summary = mapCaseSummary(item, item.sourceModule);
  const evidence = item.evidence.map((entry) => ({
    id: entry.id,
    evidenceTypeCode: entry.evidenceTypeCode,
    evidenceTypeLabel: localizeOperationalRiskVisibleToken(entry.evidenceTypeCode),
    evidenceState: entry.evidenceState,
    evidenceLabel: getOperationalRiskEvidenceLabel(entry.evidenceState),
    observedAtUtc: entry.observedAtUtc,
    observedLabel: formatOperationalRiskDateTime(entry.observedAtUtc),
    sourceModule: entry.sourceModule,
    sourceModuleLabel: getOperationalRiskSourceModuleLabel(entry.sourceModule),
    sourceReference: entry.sourceReference,
    summary: localizeOperationalRiskEvidenceSummary(entry.summary),
    numericValue: entry.numericValue,
    unit: entry.unit,
    comparisonReference: entry.comparisonReference,
    sourceProvenance: localizeOperationalRiskVisibleText(entry.sourceProvenance),
    createdAtUtc: entry.createdAtUtc,
    isPilotData: entry.isPilotData,
    technicalAuditTokens: collectOperationalRiskTechnicalAuditTokens({
      evidenceTypeCode: entry.evidenceTypeCode,
      comparisonReference: entry.comparisonReference,
      sourceReference: entry.sourceReference,
      summary: entry.summary,
    }),
  }));
  const drivers = extractOperationalRiskCaseDrivers(item.title, item.category, evidence);
  return {
    summary,
    description: localizeOperationalRiskVisibleText(item.description),
    sourceProvenance: localizeOperationalRiskVisibleText(item.sourceProvenance),
    evidence,
    statusHistory: item.statusHistory.map((entry) => ({
      id: entry.id,
      previousStatus: entry.previousStatus,
      previousLabel: getOperationalRiskLifecycleLabel(entry.previousStatus),
      newStatus: entry.newStatus,
      newLabel: getOperationalRiskLifecycleLabel(entry.newStatus),
      reasonNote: entry.reasonNote,
      actorSubjectId: entry.actorSubjectId,
      occurredAtUtc: entry.occurredAtUtc,
      occurredLabel: formatOperationalRiskDateTime(entry.occurredAtUtc),
      expectedCaseVersion: entry.expectedCaseVersion,
      sourceProvenance: entry.sourceProvenance,
    })),
    allowedTransitions: OPERATIONAL_RISK_ALLOWED_TRANSITIONS[item.lifecycleStatus] ?? [],
    leadingDriverLabel: drivers.leadingDriverLabel,
    supportingDriverLabels: drivers.supportingDriverLabels,
  };
}

function mapRunDetail(item: RawOperationalRiskDetectionRunDetail): OperationalRiskDetectionRunDetail {
  const summary = mapRunSummary(item, item);
  const results = item.results.map((result) => ({
    id: result.id,
    sourceModule: result.sourceModule,
    sourceModuleLabel: getOperationalRiskSourceModuleLabel(result.sourceModule),
    sourceReference: result.sourceReference,
    riskCategory: result.riskCategory,
    categoryLabel: getOperationalRiskCategoryLabel(result.riskCategory),
    scopeType: result.scopeType,
    scopeLabel: getOperationalRiskScopeLabel(result.scopeType),
    scopeReference: result.scopeReference,
    evidenceState: result.evidenceState,
    evidenceLabel: getOperationalRiskEvidenceLabel(result.evidenceState),
    sourceOutcome: result.sourceOutcome,
    sourceOutcomeLabel: getOperationalRiskDetectionOutcomeLabel(result.sourceOutcome),
    detectionOutcome: result.detectionOutcome,
    detectionOutcomeLabel: getOperationalRiskDetectionOutcomeLabel(result.detectionOutcome),
    actualValue: result.actualValue,
    thresholdValue: result.thresholdValue,
    unit: result.unit,
    normalizedVariance: result.normalizedVariance,
    likelihoodLevel: result.likelihoodLevel,
    impactLevel: result.impactLevel,
    riskScore: result.riskScore,
    severityBand: result.severityBand,
    severityLabel: getOperationalRiskSeverityLabel(result.severityBand),
    leadingDriverCode: result.leadingDriverCode,
    leadingDriverLabel: getOperationalRiskDriverLabel(result.leadingDriverCode),
    linkedCaseId: result.linkedCaseId,
    linkedCaseReference: null,
    detectionKey: result.detectionKey,
    sourceAssessmentReference: result.sourceAssessmentReference,
    evaluatedAtUtc: result.evaluatedAtUtc,
    evaluatedLabel: formatOperationalRiskDateTime(result.evaluatedAtUtc),
    drivers: result.drivers.map((driver) => ({
      id: driver.id,
      reasonCode: driver.reasonCode,
      reasonLabel: getOperationalRiskDriverLabel(driver.reasonCode),
      severity: driver.severity,
      severityLabel: getOperationalRiskSeverityLabel(driver.severity),
      actualValue: driver.actualValue,
      thresholdValue: driver.thresholdValue,
      unit: driver.unit,
      normalizedVariance: driver.normalizedVariance,
      supportingText: driver.supportingText,
      sortOrder: driver.sortOrder,
    })),
  }));

  const groupedOutcomes: Record<string, typeof results> = {
    "risk-detected": [],
    "within-threshold": [],
    "insufficient-data": [],
    "source-conflict": [],
  };
  for (const result of results) {
    groupedOutcomes[result.detectionOutcome]?.push(result);
  }

  return { summary, sourceProvenance: item.sourceProvenance, results, groupedOutcomes };
}

export { OPERATIONAL_RISK_OUTCOME_COLORS };
