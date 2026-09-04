import type { AiTask } from "../domain/ai-task";
import { AI_TASK_CATEGORIES, type AiTaskCategory, type AiTaskPriority, type AiTaskStatus } from "../domain/ai-task";
import type {
  AiTaskDecisionClosureReview,
  AiTaskDecisionRelatedCandidate,
  AiTaskDecisionTriage,
} from "../domain/ai-task-decision-intelligence";
import {
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskStatusLabel,
} from "./ai-task-labels";
import {
  getAiTaskDecisionClosureReadinessLabel,
  getAiTaskDecisionConfidenceLabel,
  getAiTaskDecisionImpactLabel,
  getAiTaskDecisionRelationshipLabel,
  getAiTaskDecisionSlaRiskLabel,
} from "./ai-task-decision-intelligence-labels";
import { getAiTaskSlaDisplayText } from "./ai-task-sla-labels";
import { resolveAutomationTeamChannelLabel } from "./ai-task-automation-entity-lookup";

const URGENCY_LABELS: Readonly<Record<string, string>> = {
  critical: "Kritik",
  high: "Yüksek",
  medium: "Orta",
  low: "Düşük",
};

const SOURCE_MODULE_LABELS: Readonly<Record<string, string>> = {
  "shift-handover": "Vardiya devir teslimi",
  teams: "Teams",
  maintenance: "Bakım",
  quality: "Kalite",
};

const SLA_STATE_LABELS: Readonly<Record<string, string>> = {
  "on-track": "Zamanında",
  "at-risk": "Risk altında",
  breached: "SLA ihlali",
  paused: "Duraklatıldı",
  "completed-within-sla": "SLA içinde tamamlandı",
  "completed-after-sla": "SLA sonrası tamamlandı",
};

const RELATIONSHIP_STATUS_LABELS: Readonly<Record<string, string>> = {
  confirmed: "İlişkili",
  "marked-duplicate": "Yinelenen",
  rejected: "Reddedildi",
  suggested: "Öneri",
};

const DEPARTMENT_LABELS: Readonly<Record<string, string>> = {
  Bakım: "Bakım",
  Kalite: "Kalite",
  Üretim: "Üretim",
  Operasyon: "Operasyon",
};

const ACTION_TYPE_LABELS: Readonly<Record<string, string>> = {
  "assign-team": "Ekip ataması",
  "set-priority": "Öncelik güncelleme",
  escalate: "Eskalasyon",
  "request-evidence": "Kanıt isteme",
  "request-closure-approval": "Kapanış onayı",
  "review-production-order": "Üretim emri incelemesi",
};

const FIXTURE_TEXT_LABELS: Readonly<Record<string, string>> = {
  "Review profitability comparison": "Kârlılık karşılaştırmasını incele",
  "Human review before operational action": "Operasyonel aksiyon öncesi insan incelemesi",
  "Live flow detected issue": "Canlı akışta sorun tespit edildi",
  "Live flow reasoning": "Canlı akış değerlendirmesi",
  "Shift handover critical machine stoppage": "Vardiya devir teslimi kritik makine duruşu",
  "Critical machine vibration detected during shift handover": "Vardiya devir teslimi sırasında kritik makine titreşimi tespit edildi",
  "Operational impact requires maintenance triage": "Operasyonel etki bakım triyajı gerektiriyor",
  "Maintenance inspection": "Bakım incelemesi",
  "Fixture confidence": "Test güven değerlendirmesi",
  "Production line impact": "Üretim hattı etkisi",
};

export function localizeAiTaskFixtureText(text: string | null | undefined): string {
  if (!text) return "—";
  const trimmed = text.trim();
  if (!trimmed) return "—";
  if (FIXTURE_TEXT_LABELS[trimmed]) return FIXTURE_TEXT_LABELS[trimmed];
  return localizeDecisionText(trimmed);
}

const KEY_TOKEN_LABELS: Readonly<Record<string, string>> = {
  low: "düşük",
  medium: "orta",
  high: "yüksek",
  critical: "kritik",
  watch: "izleme",
  breached: "SLA ihlali",
  "on-track": "zamanında",
  "at-risk": "risk altında",
  maintenance: "bakım",
  quality: "kalite",
  "commercial-intelligence": "ticari zekâ",
  "shift-handover": "vardiya devir teslimi",
  "production-planning": "üretim planlama",
  "live-production": "canlı üretim",
};

export function localizeDecisionToken(value: string | null | undefined): string {
  if (!value) return "—";
  const trimmed = value.trim();
  if (!trimmed) return "—";

  if ((AI_TASK_CATEGORIES as readonly string[]).includes(trimmed)) {
    return getAiTaskCategoryLabel(trimmed as AiTaskCategory);
  }

  const lowered = trimmed.toLowerCase();
  if (lowered in KEY_TOKEN_LABELS) {
    return KEY_TOKEN_LABELS[lowered];
  }

  if (lowered in SOURCE_MODULE_LABELS) {
    return SOURCE_MODULE_LABELS[lowered];
  }

  if (lowered in SLA_STATE_LABELS) {
    return SLA_STATE_LABELS[lowered];
  }

  return trimmed;
}

export function getAiTaskDecisionUrgencyLabel(band: string): string {
  return URGENCY_LABELS[band] ?? localizeDecisionToken(band);
}

export function getAiTaskDecisionRelationshipStatusLabel(status: string): string {
  return RELATIONSHIP_STATUS_LABELS[status] ?? localizeDecisionToken(status);
}

export function getAiTaskDecisionActionTypeLabel(actionType: string): string {
  return ACTION_TYPE_LABELS[actionType] ?? "Desteklenen aksiyon";
}

export function getAiTaskDecisionDepartmentLabel(department: string | null | undefined): string {
  if (!department) return "—";
  return DEPARTMENT_LABELS[department] ?? department;
}

export function cleanFixtureTaskTitle(value: string): string {
  return value
    .replace(/\s+\d{10,}(?:\s+\d{10,})*$/u, "")
    .replace(/\s{2,}/gu, " ")
    .trim();
}

export function isSimilarityExplanation(value: string): boolean {
  const trimmed = value.trim();
  return trimmed.startsWith("Aynı ")
    || trimmed.startsWith("Benzer ")
    || trimmed.startsWith("Ortak ");
}

export function buildDecisionAssessmentSummary(
  triage: AiTaskDecisionTriage,
  taskCategory?: string | null,
): string {
  const categoryKey = triage.suggestedCategory ?? taskCategory ?? null;
  const categoryLabel = categoryKey && (AI_TASK_CATEGORIES as readonly string[]).includes(categoryKey)
    ? getAiTaskCategoryLabel(categoryKey as AiTaskCategory)
    : categoryKey
      ? localizeDecisionToken(categoryKey)
      : "Operasyonel";

  const priorityKey = (triage.suggestedPriority ?? "medium") as AiTaskPriority;
  const priorityLabel = getAiTaskPriorityLabel(priorityKey).toLocaleLowerCase("tr-TR");
  const slaRisk = triage.slaBreachRiskBand;

  let summary = `${categoryLabel} kaynaklı bu görev ${priorityLabel} öncelikte.`;

  if (slaRisk === "critical" || slaRisk === "high") {
    summary += ` Mevcut ${getAiTaskDecisionSlaRiskLabel(slaRisk).toLocaleLowerCase("tr-TR")} SLA riski nedeniyle operasyonel risk yükselmiş değerlendiriliyor.`;
  } else if (slaRisk === "watch") {
    summary += " SLA izleme bandında; operasyonel etki orta seviyede tutulmalı.";
  } else if (triage.operationalImpactBand === "critical" || triage.operationalImpactBand === "high") {
    summary += ` Operasyonel etki ${getAiTaskDecisionImpactLabel(triage.operationalImpactBand).toLocaleLowerCase("tr-TR")} bandında.`;
  } else if (triage.urgencyReason) {
    summary += ` ${localizeDecisionText(triage.urgencyReason)}`;
  }

  return summary;
}

export function localizeDecisionText(text: string): string {
  let output = text;

  for (const [key, label] of Object.entries(KEY_TOKEN_LABELS)) {
    output = output.replace(new RegExp(`\\b${key}\\b`, "gi"), label);
  }

  for (const category of [
    "commercial-intelligence",
    "production-planning",
    "live-production",
    "material-risk",
    "supply-risk",
    "delivery-risk",
    "cost-deviation",
    "erp-integration",
  ] as const) {
    output = output.replace(
      new RegExp(category, "gi"),
      getAiTaskCategoryLabel(category),
    );
  }

  output = output.replace(/Kaynak modül:\s*([a-z0-9-]+)/gi, (_, module: string) =>
    `Kaynak modül: ${localizeDecisionToken(module)}`);

  output = output.replace(/SLA durumu:\s*([a-z0-9-]+)/gi, (_, state: string) =>
    `SLA durumu: ${localizeDecisionToken(state)}`);

  output = output.replace(
    /AI öncelik önerisi:\s*([a-z]+),\s*mevcut:\s*([a-z]+)/gi,
    (_, suggested: string, current: string) =>
      `AI öncelik önerisi: ${localizeDecisionToken(suggested)}, mevcut: ${localizeDecisionToken(current)}`,
  );

  return output.replace(/\s{2,}/g, " ").trim();
}

export function localizeContributingFactor(factor: string): string {
  const localized = localizeDecisionText(factor);
  if (localized !== factor) return localized;

  const moduleMatch = factor.match(/^Kaynak modül:\s*(.+)$/i);
  if (moduleMatch) {
    return `Kaynak modül: ${localizeDecisionToken(moduleMatch[1])}`;
  }

  const slaMatch = factor.match(/^SLA durumu:\s*(.+)$/i);
  if (slaMatch) {
    return `SLA durumu: ${localizeDecisionToken(slaMatch[1])}`;
  }

  return localized;
}

export type DecisionPolicyComparisonView = Readonly<{
  policyRows: readonly { label: string; value: string }[];
  aiRows: readonly { label: string; value: string }[];
  policyNote: string;
  aiNote: string;
  differences: readonly string[];
}>;

export function buildPolicyComparisonView(
  task: AiTask,
  triage: AiTaskDecisionTriage | null | undefined,
): DecisionPolicyComparisonView | null {
  const comparison = triage?.policyComparison;
  const hasAutomation = Boolean(comparison) || Boolean(task.assignedTeamChannelId);

  if (!hasAutomation) return null;

  const teamLabel = task.assignedTeamChannelId
    ? resolveAutomationTeamChannelLabel(task.assignedTeamChannelId)
    : "Uygulanmadı";

  const slaLabel = (task.sla
    ? getAiTaskSlaDisplayText(task.sla.state, task.sla.resolutionDueAt ?? task.sla.responseDueAt)
    : getAiTaskSlaDisplayText(task.slaState, null)) ?? localizeDecisionToken(task.slaState);

  const evidenceRequired = task.closurePolicy?.minimumEvidenceCount ?? 0;
  const evidenceCount = task.evidenceCount ?? task.evidence?.length ?? 0;
  const evidenceLabel = evidenceRequired > 0
    ? `Zorunlu (${evidenceCount}/${evidenceRequired})`
    : evidenceCount > 0
      ? `Mevcut (${evidenceCount})`
      : "Gerekli değil";

  const closureLabel = task.closureApprovalRequired || task.closurePolicy?.approvalRequired
    ? "İkinci onay gerekli"
    : "Standart kapanış";

  const policyRows = [
    { label: "Atama", value: teamLabel },
    { label: "SLA", value: slaLabel },
    { label: "Kanıt", value: evidenceLabel },
    { label: "Kapanış onayı", value: closureLabel },
  ];

  const suggestedPriority = triage?.suggestedPriority
    ? getAiTaskPriorityLabel(triage.suggestedPriority as AiTaskPriority)
    : "—";
  const suggestedTeam = triage?.suggestedTeamChannelId
    ? resolveAutomationTeamChannelLabel(triage.suggestedTeamChannelId)
    : getAiTaskDecisionDepartmentLabel(triage?.suggestedDepartment);

  const nextAction = triage?.nextActions[0]?.title ?? comparison?.aiSummary ?? "Ek operasyonel aksiyon önerisi yok";

  const aiRows = [
    { label: "Önerilen öncelik", value: suggestedPriority },
    { label: "SLA riski", value: getAiTaskDecisionSlaRiskLabel(triage?.slaBreachRiskBand ?? "low") },
    { label: "Ek aksiyon", value: nextAction },
    {
      label: "Güven",
      value: getAiTaskDecisionConfidenceLabel(triage?.confidenceBand ?? "medium"),
    },
  ] satisfies ReadonlyArray<{ label: string; value: string }>;

  return {
    policyRows,
    aiRows,
    policyNote: comparison?.policySummary ?? "Deterministik otomasyon politikası uygulandı.",
    aiNote: comparison?.aiSummary ?? "Bağlamsal değerlendirme ve ek öneriler.",
    differences: (comparison?.aiDifferences ?? []).map(localizeDecisionText),
  };
}

export function buildRelatedCandidateDisplay(
  candidate: AiTaskDecisionRelatedCandidate,
): Readonly<{
  title: string;
  status: string | null;
  priority: string | null;
  similarityReason: string;
}> {
  const title = candidate.candidateTaskTitle
    ? cleanFixtureTaskTitle(candidate.candidateTaskTitle)
    : isSimilarityExplanation(candidate.explanation)
      ? "İlişkili görev"
      : cleanFixtureTaskTitle(candidate.explanation);

  const status = candidate.candidateTaskStatus
    ? getAiTaskStatusLabel(candidate.candidateTaskStatus as AiTaskStatus)
    : null;

  const priority = candidate.candidateTaskPriority
    ? getAiTaskPriorityLabel(candidate.candidateTaskPriority as AiTaskPriority)
    : null;

  const similarityReason = isSimilarityExplanation(candidate.explanation)
    ? candidate.explanation
    : getAiTaskDecisionRelationshipLabel(candidate.relationshipType);

  return { title, status, priority, similarityReason };
}

export function buildClosureReviewView(review: AiTaskDecisionClosureReview) {
  return {
    readinessLabel: getAiTaskDecisionClosureReadinessLabel(review.closureReadiness),
    explanation: localizeDecisionText(review.explanation),
    concerns: review.concerns.map(localizeDecisionText),
    recommendations: review.recommendations.map(localizeDecisionText),
    confidenceLabel: getAiTaskDecisionConfidenceLabel(review.confidenceBand),
  };
}

export function looksLikeRawIdentifier(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
    || /^\d{10,}$/.test(value);
}
