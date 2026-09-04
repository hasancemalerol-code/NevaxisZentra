export const AI_TASK_DECISION_CONFIDENCE_LABELS: Readonly<Record<string, string>> = {
  high: "Yüksek",
  medium: "Orta",
  low: "Düşük",
  "insufficient-data": "Yetersiz veri",
};

export const AI_TASK_DECISION_SLA_RISK_LABELS: Readonly<Record<string, string>> = {
  low: "Düşük",
  watch: "İzleme",
  high: "Yüksek",
  critical: "Kritik",
  "insufficient-data": "Yetersiz veri",
};

export const AI_TASK_DECISION_IMPACT_LABELS: Readonly<Record<string, string>> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  critical: "Kritik",
  "insufficient-data": "Yetersiz veri",
};

export const AI_TASK_DECISION_RELATIONSHIP_LABELS: Readonly<Record<string, string>> = {
  "possible-duplicate": "Olası yinelenen",
  related: "İlişkili",
  "same-incident": "Aynı olay",
  "same-source": "Aynı kaynak",
  "follow-up": "Takip",
};

export const AI_TASK_DECISION_CLOSURE_READINESS_LABELS: Readonly<Record<string, string>> = {
  ready: "Hazır",
  "review-needed": "İnceleme gerekli",
  "risk-detected": "Risk tespit edildi",
  "insufficient-data": "Yetersiz veri",
};

export function getAiTaskDecisionConfidenceLabel(band: string): string {
  return AI_TASK_DECISION_CONFIDENCE_LABELS[band] ?? "Belirsiz";
}

export function getAiTaskDecisionSlaRiskLabel(band: string): string {
  return AI_TASK_DECISION_SLA_RISK_LABELS[band] ?? "Belirsiz";
}

export function getAiTaskDecisionImpactLabel(band: string): string {
  return AI_TASK_DECISION_IMPACT_LABELS[band] ?? "Belirsiz";
}

export function getAiTaskDecisionRelationshipLabel(type: string): string {
  return AI_TASK_DECISION_RELATIONSHIP_LABELS[type] ?? "İlişkili";
}

export function getAiTaskDecisionClosureReadinessLabel(readiness: string): string {
  return AI_TASK_DECISION_CLOSURE_READINESS_LABELS[readiness] ?? "İnceleme gerekli";
}

export const AI_TASK_DECISION_URGENCY_LABELS: Readonly<Record<string, string>> = {
  critical: "Kritik",
  high: "Yüksek",
  medium: "Orta",
  low: "Düşük",
};

export function getAiTaskDecisionUrgencyLabel(band: string): string {
  return AI_TASK_DECISION_URGENCY_LABELS[band] ?? "Orta";
}
