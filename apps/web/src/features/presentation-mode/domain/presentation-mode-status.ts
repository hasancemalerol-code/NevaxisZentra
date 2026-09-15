import type { GoldenDemoP1WorkspaceApi } from "@/features/golden-demo-p1/server/golden-demo-p1-api-client";
import type { GoldenDemoP2PresentationStatusApi } from "@/features/golden-demo-p2/server/golden-demo-p2-api-client";
import type { GoldenDemoCustomsFileWorkspaceApi } from "@/features/golden-demo-customs-file/server/golden-demo-customs-file-api-client";
import type { FactoryDecisionCaseQueueItemApi } from "@/features/factory-decision-center/server/factory-decision-center-api-client";

import {
  CRITICAL_ORDER_RECOVERY_ORDER_REFERENCE,
  type PresentationScenarioStatus,
} from "./presentation-mode-labels";

export function mapProductionRecoveryPresentationStatus(
  workspace: GoldenDemoP1WorkspaceApi | null,
  presentation: GoldenDemoP2PresentationStatusApi | null,
): PresentationScenarioStatus {
  if (!workspace) {
    return "Hazır";
  }

  const eventStatus = workspace.event.status;
  if (eventStatus === "verified" || presentation?.isComplete) {
    return "VERIFIED";
  }

  if (eventStatus === "validation-run") {
    return "Doğrulama";
  }

  if (
    workspace.plan?.requiresApproval &&
    workspace.plan.approvedAtUtc == null &&
    (eventStatus === "active" || eventStatus === "intervention-in-progress")
  ) {
    return "İnsan Onayı Bekliyor";
  }

  if (eventStatus === "intervention-in-progress" || eventStatus === "active") {
    return presentation?.stepNumber != null && presentation.stepNumber <= 3 ? "Hazır" : "Devam Ediyor";
  }

  return "Hazır";
}

export function mapCustomsPresentationStatus(
  workspace: GoldenDemoCustomsFileWorkspaceApi | null,
): PresentationScenarioStatus {
  if (!workspace) {
    return "Hazır";
  }

  switch (workspace.status) {
    case "verified-ready":
      return "VERIFIED";
    case "approval-pending":
      return "İnsan Onayı Bekliyor";
    case "package-assembled":
    case "approved":
    case "evidence-rechecked":
    case "corrective-work-completed":
      return "Doğrulama";
    case "assigned":
    case "context-loaded":
      return "Hazır";
    default:
      return "Devam Ediyor";
  }
}

export function mapCriticalOrderPresentationStatus(
  caseItem: FactoryDecisionCaseQueueItemApi | null,
): PresentationScenarioStatus {
  if (!caseItem) {
    return "Hazır";
  }

  if (
    caseItem.status === "closed" &&
    caseItem.verificationState.toLowerCase() === "passed"
  ) {
    return "VERIFIED";
  }

  if (caseItem.status === "awaiting-approval") {
    return "İnsan Onayı Bekliyor";
  }

  if (caseItem.status === "verifying" || caseItem.verificationState.toLowerCase() === "pending") {
    return "Doğrulama";
  }

  if (caseItem.status === "executing" || caseItem.status === "planning") {
    return "Devam Ediyor";
  }

  return "Hazır";
}

export function findCriticalOrderRecoveryCase(
  cases: readonly FactoryDecisionCaseQueueItemApi[],
): FactoryDecisionCaseQueueItemApi | null {
  return (
    cases.find((item) => item.orderReference === CRITICAL_ORDER_RECOVERY_ORDER_REFERENCE) ??
    cases[0] ??
    null
  );
}

export function summarizePresentationStatuses(
  statuses: readonly PresentationScenarioStatus[],
): Readonly<{
  total: number;
  ready: number;
  inProgress: number;
  awaitingApproval: number;
  verification: number;
  verified: number;
}> {
  return {
    total: statuses.length,
    ready: statuses.filter((status) => status === "Hazır").length,
    inProgress: statuses.filter((status) => status === "Devam Ediyor").length,
    awaitingApproval: statuses.filter((status) => status === "İnsan Onayı Bekliyor").length,
    verification: statuses.filter((status) => status === "Doğrulama").length,
    verified: statuses.filter((status) => status === "VERIFIED").length,
  };
}
