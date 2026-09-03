import type {
  AiTaskClosureState,
  AiTaskEvidenceType,
  AiTaskResolutionCode,
} from "../domain/ai-task";

export const AI_TASK_CLOSURE_STATE_LABELS: Readonly<
  Record<AiTaskClosureState, string>
> = {
  none: "Kapanış bekleniyor",
  "resolution-requested": "Çözüm kaydedildi",
  "awaiting-closure-approval": "Onay bekliyor",
  "closure-rejected": "Revizyon istendi",
  closed: "Kapandı",
  reopened: "Yeniden açıldı",
};

export const AI_TASK_CLOSURE_STATE_TONES: Readonly<
  Record<AiTaskClosureState, "neutral" | "warning" | "success" | "danger" | "ai">
> = {
  none: "neutral",
  "resolution-requested": "ai",
  "awaiting-closure-approval": "warning",
  "closure-rejected": "danger",
  closed: "success",
  reopened: "warning",
};

export const AI_TASK_EVIDENCE_TYPE_LABELS: Readonly<
  Record<AiTaskEvidenceType, string>
> = {
  photo: "Fotoğraf",
  document: "Belge",
  measurement: "Ölçüm",
  "machine-reading": "Makine okuması",
  "quality-record": "Kalite kaydı",
  "maintenance-record": "Bakım kaydı",
  "production-record": "Üretim kaydı",
  "external-reference": "Harici referans",
  note: "Not",
};

export const AI_TASK_RESOLUTION_CODE_LABELS: Readonly<
  Record<AiTaskResolutionCode, string>
> = {
  resolved: "Çözüldü",
  mitigated: "Hafifletildi",
  "accepted-risk": "Risk kabul edildi",
  "false-positive": "Yanlış pozitif",
  "deferred-follow-up": "Takip ertelendi",
};

export const AI_TASK_CLOSURE_BADGE_LABELS = {
  missingEvidence: "Kanıt gerekli",
  awaitingApproval: "Onay bekliyor",
  revisionPending: "Revizyon istendi",
  closureReady: "Kapanış hazır",
} as const;

export type AiTaskClosureBadgeKey = keyof typeof AI_TASK_CLOSURE_BADGE_LABELS;

export const AI_TASK_CLOSURE_BADGE_TONES: Readonly<
  Record<AiTaskClosureBadgeKey, "neutral" | "warning" | "success" | "danger">
> = {
  missingEvidence: "warning",
  awaitingApproval: "warning",
  revisionPending: "danger",
  closureReady: "success",
};

export function getAiTaskClosureStateLabel(state: AiTaskClosureState): string {
  return AI_TASK_CLOSURE_STATE_LABELS[state] ?? state;
}

export function getAiTaskEvidenceTypeLabel(type: AiTaskEvidenceType): string {
  return AI_TASK_EVIDENCE_TYPE_LABELS[type] ?? type;
}

export function getAiTaskResolutionCodeLabel(code: AiTaskResolutionCode): string {
  return AI_TASK_RESOLUTION_CODE_LABELS[code] ?? code;
}

export function resolveAiTaskClosureBadge(
  input: Readonly<{
    closureState?: AiTaskClosureState;
    evidenceCount?: number;
    status?: string;
    category?: string;
    priority?: string;
    closureValidationIsValid?: boolean;
  }>,
): AiTaskClosureBadgeKey | null {
  const closureState = input.closureState ?? "none";

  if (closureState === "awaiting-closure-approval") {
    return "awaitingApproval";
  }

  if (closureState === "closure-rejected") {
    return "revisionPending";
  }

  if (closureState === "closed") {
    return null;
  }

  const requiresEvidence = input.category === "quality"
    || input.category === "maintenance"
    || input.priority === "critical";
  const missingEvidence = requiresEvidence
    && input.status === "in-progress"
    && (closureState === "none" || closureState === "reopened")
    && (input.evidenceCount ?? 0) === 0;

  if (missingEvidence) {
    return "missingEvidence";
  }

  if (input.closureValidationIsValid) {
    return "closureReady";
  }

  if (
    closureState === "none"
    || closureState === "reopened"
    || closureState === "resolution-requested"
  ) {
    const hasEvidence = (input.evidenceCount ?? 0) > 0;
    const activeStatus = input.status === "in-progress" || input.status === "approved";
    if (hasEvidence && activeStatus) {
      return "closureReady";
    }
  }

  return null;
}
