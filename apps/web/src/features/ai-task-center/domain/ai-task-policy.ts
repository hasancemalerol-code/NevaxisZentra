import type {
  AiTask,
  AiTaskActionType,
  AiTaskApprovalState,
  AiTaskPriority,
  AiTaskStatus,
} from "./ai-task";

export type AiTaskActorRole =
  | "operator"
  | "supervisor"
  | "department-manager"
  | "plant-manager"
  | "system-administrator";

export type AiTaskTransitionDecision = Readonly<{
  allowed: boolean;
  reason?: string;
}>;

const FINAL_STATUSES: readonly AiTaskStatus[] = [
  "completed",
  "rejected",
  "failed",
  "cancelled",
];

const STATUS_TRANSITIONS: Readonly<
  Record<AiTaskStatus, readonly AiTaskStatus[]>
> = {
  draft: [
    "awaiting-review",
    "cancelled",
  ],
  "awaiting-review": [
    "draft",
    "awaiting-approval",
    "approved",
    "deferred",
    "rejected",
    "cancelled",
  ],
  "awaiting-approval": [
    "approved",
    "rejected",
    "deferred",
    "cancelled",
  ],
  approved: [
    "in-progress",
    "cancelled",
  ],
  "in-progress": [
    "completed",
    "failed",
    "deferred",
    "cancelled",
  ],
  completed: [],
  rejected: [],
  deferred: [
    "awaiting-review",
    "awaiting-approval",
    "cancelled",
  ],
  failed: [
    "awaiting-review",
    "approved",
    "cancelled",
  ],
  cancelled: [],
};

const HIGH_IMPACT_ACTIONS: readonly AiTaskActionType[] = [
  "update-plan",
  "create-purchase-request",
  "create-transfer-request",
  "sync-to-erp",
];

const APPROVER_ROLES: readonly AiTaskActorRole[] = [
  "supervisor",
  "department-manager",
  "plant-manager",
  "system-administrator",
];

const CRITICAL_APPROVER_ROLES: readonly AiTaskActorRole[] = [
  "department-manager",
  "plant-manager",
  "system-administrator",
];

export function isFinalAiTaskStatus(
  status: AiTaskStatus,
): boolean {
  return FINAL_STATUSES.includes(status);
}

export function getAllowedAiTaskTransitions(
  status: AiTaskStatus,
): readonly AiTaskStatus[] {
  return STATUS_TRANSITIONS[status];
}

export function canTransitionAiTask(
  currentStatus: AiTaskStatus,
  nextStatus: AiTaskStatus,
): AiTaskTransitionDecision {
  if (currentStatus === nextStatus) {
    return {
      allowed: false,
      reason: "Görev zaten seçilen durumda.",
    };
  }

  if (isFinalAiTaskStatus(currentStatus)) {
    return {
      allowed: false,
      reason: "Tamamlanmış bir görev yeniden açılamaz.",
    };
  }

  if (!STATUS_TRANSITIONS[currentStatus].includes(nextStatus)) {
    return {
      allowed: false,
      reason: `${currentStatus} durumundan ${nextStatus} durumuna geçiş yapılamaz.`,
    };
  }

  return {
    allowed: true,
  };
}

export function doesActionRequireApproval(
  actionType: AiTaskActionType,
): boolean {
  return HIGH_IMPACT_ACTIONS.includes(actionType);
}

export function resolveInitialApprovalState(
  actionType: AiTaskActionType,
  explicitlyRequiresApproval: boolean,
): AiTaskApprovalState {
  if (
    explicitlyRequiresApproval ||
    doesActionRequireApproval(actionType)
  ) {
    return "required";
  }

  return "not-required";
}

export function canApproveAiTask(
  role: AiTaskActorRole,
  priority: AiTaskPriority,
): AiTaskTransitionDecision {
  const permittedRoles =
    priority === "critical"
      ? CRITICAL_APPROVER_ROLES
      : APPROVER_ROLES;

  if (!permittedRoles.includes(role)) {
    return {
      allowed: false,
      reason:
        priority === "critical"
          ? "Kritik görevler yalnızca bölüm yöneticisi veya üzeri tarafından onaylanabilir."
          : "Bu rol görev onaylama yetkisine sahip değil.",
    };
  }

  return {
    allowed: true,
  };
}

export function canExecuteAiTask(
  task: Pick<
    AiTask,
    "status" | "approval" | "recommendation"
  >,
): AiTaskTransitionDecision {
  if (task.status !== "approved") {
    return {
      allowed: false,
      reason: "Yalnızca onaylanmış görevler uygulanabilir.",
    };
  }

  const approvalRequired =
    task.recommendation.requiresApproval ||
    doesActionRequireApproval(
      task.recommendation.actionType,
    );

  if (
    approvalRequired &&
    task.approval.state !== "approved"
  ) {
    return {
      allowed: false,
      reason: "Görev uygulanmadan önce insan onayı gerekiyor.",
    };
  }

  return {
    allowed: true,
  };
}

export function canCancelAiTask(
  status: AiTaskStatus,
): AiTaskTransitionDecision {
  if (isFinalAiTaskStatus(status)) {
    return {
      allowed: false,
      reason: "Son durumdaki görev iptal edilemez.",
    };
  }

  return {
    allowed: true,
  };
}