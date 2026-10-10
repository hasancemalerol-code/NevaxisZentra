import "server-only";

import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readAiTaskCenterUnified } from "@/features/ai-task-center/server/ai-task-center-queries";
import { readFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import type { FactoryDecisionCaseQueueItemApi } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { fetchWorksWorkspace } from "@/features/works/server/works-api-client";
import {
  WORKS_QUEUE_PENDING_APPROVAL,
  WORKS_QUEUE_VERIFICATION_PENDING,
} from "@/features/works/domain/works-queues";
import {
  resolveRoleLandingLabel,
  resolveRoleLandingProfile,
  type RoleLandingProfile,
} from "@/lib/navigation/role-landing";
import { loadContinuousOptimizationControlCenter } from "@/features/continuous-optimization/server/continuous-optimization-reader";

export type BugunAttentionItem = Readonly<{
  id: string;
  title: string;
  summary: string;
  href: string;
  tone: "critical" | "warning" | "neutral";
  category: string;
}>;

export type BugunWorkspacePayload = Readonly<{
  profile: RoleLandingProfile;
  profileLabel: string;
  showExecutiveStrip: boolean;
  attentionItems: readonly BugunAttentionItem[];
  decisionQueue: readonly FactoryDecisionCaseQueueItemApi[];
  shiftLabel: string | null;
  openShiftItems: number | null;
  pendingApprovals: number | null;
  pendingVerification: number | null;
  taskCount: number | null;
  generatedAtUtc: string;
}>;

export type BugunWorkspaceResult =
  | Readonly<{ status: "success"; workspace: BugunWorkspacePayload }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveProductionContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readBugunWorkspace(): Promise<BugunWorkspaceResult> {
  const access = await fetchEffectiveAccessCapabilities();
  const profile = resolveRoleLandingProfile(access);
  const profileLabel = resolveRoleLandingLabel(profile);
  const showExecutiveStrip =
    profile === "plant-manager"
    || (hasGrantedPermission(access, "factory-case.read")
      && profile !== "operator");

  const attentionItems: BugunAttentionItem[] = [];
  let decisionQueue: readonly FactoryDecisionCaseQueueItemApi[] = [];
  let shiftLabel: string | null = null;
  let openShiftItems: number | null = null;
  let pendingApprovals: number | null = null;
  let pendingVerification: number | null = null;
  let taskCount: number | null = null;

  if (hasGrantedPermission(access, "factory-case.read") && showExecutiveStrip) {
    const contextResult = await resolveProductionContext();
    if (contextResult.status === "success") {
      const decisionResult = await readFactoryDecisionCenterWorkspace(contextResult.context);
      if (decisionResult.status === "success") {
        const workspace = decisionResult.value;
        decisionQueue = workspace.priorityQueue.slice(0, 4);
        pendingApprovals = workspace.summary.pendingHumanApproval;
        pendingVerification = workspace.summary.pendingVerification;

        if (workspace.summary.criticalDecisions > 0) {
          attentionItems.push({
            id: "fdc-critical",
            title: "Kritik karar bekliyor",
            summary: `${workspace.summary.criticalDecisions} vaka insan kararı veya doğrulama gerektiriyor.`,
            href: "/fabrika-karar-merkezi",
            tone: "critical",
            category: "Karar",
          });
        }

        if (workspace.summary.recoveryOperations > 0) {
          attentionItems.push({
            id: "fdc-recovery",
            title: "Aktif kurtarma operasyonu",
            summary: `${workspace.summary.recoveryOperations} vaka kurtarma altında.`,
            href: "/fabrika-karar-merkezi",
            tone: "warning",
            category: "Kurtarma",
          });
        }

        if (hasGrantedPermission(access, "scenario-recovery.read")
          && workspace.summary.recoveryOperations > 0) {
          attentionItems.push({
            id: "scenario-recovery-evaluate",
            title: "Kurtarma seçeneklerini değerlendir",
            summary: "Operasyonel kurtarma senaryolarını karşılaştırın.",
            href: "/senaryo-kurtarma?entryPoint=BUGUN",
            tone: "warning",
            category: "Senaryo",
          });
        }
      }
    }
  }

  if (hasGrantedPermission(access, "shift-workforce.read")) {
    const shiftResult = await readShiftOperationsWorkspace();
    if (shiftResult.status === "success") {
      shiftLabel = shiftResult.currentShift?.name ?? "Aktif vardiya yok";
      openShiftItems = shiftResult.openItems?.length ?? 0;

      if ((shiftResult.openItems?.length ?? 0) > 0) {
        attentionItems.push({
          id: "shift-open-items",
          title: "Vardiya açık konuları",
          summary: `${shiftResult.openItems?.length ?? 0} konu teslim veya takip bekliyor.`,
          href: "/vardiyalar/acik-konular",
          tone: "warning",
          category: "Vardiya",
        });
      }
    }
  }

  if (hasGrantedPermission(access, "digital-workforce.read")) {
    const contextResult = await resolveProductionContext();
    if (contextResult.status === "success") {
      try {
        const worksSnapshot = await fetchWorksWorkspace(contextResult.context, { limit: 1 });
        const pendingApprovalsCount = worksSnapshot.queueCounts.pendingApproval;
        const verificationPendingCount = worksSnapshot.queueCounts.verificationPending;

        if (pendingApprovalsCount > 0) {
          attentionItems.push({
            id: "works-pending-approval",
            title: "Onay bekleyen işler",
            summary: `${pendingApprovalsCount} iş insan onayı bekliyor.`,
            href: `/isler?kuyruk=${WORKS_QUEUE_PENDING_APPROVAL}`,
            tone: "warning",
            category: "İş",
          });
        }

        if (verificationPendingCount > 0) {
          attentionItems.push({
            id: "works-verification-pending",
            title: "Doğrulama bekleyen işler",
            summary: `${verificationPendingCount} iş sonuç doğrulaması bekliyor.`,
            href: `/isler?kuyruk=${WORKS_QUEUE_VERIFICATION_PENDING}`,
            tone: "warning",
            category: "Doğrulama",
          });
        }
      } catch {
        // Works snapshot is optional for the Bugün attention strip.
      }
    }
  }

  if (hasGrantedPermission(access, "continuous-optimization.read")) {
    const optimizationResult = await loadContinuousOptimizationControlCenter();
    if (optimizationResult.status === "success") {
      const center = optimizationResult.controlCenter;
      if (center.awaitingApproval.length > 0) {
        attentionItems.push({
          id: "continuous-optimization-approval",
          title: "Optimizasyon onayı bekliyor",
          summary: `${center.awaitingApproval.length} sürekli optimizasyon müdahalesi onay bekliyor.`,
          href: "/surekli-optimizasyon",
          tone: "warning",
          category: "Optimizasyon",
        });
      }

      if (center.verifiedGains.length > 0) {
        attentionItems.push({
          id: "continuous-optimization-verified-gain",
          title: "Doğrulanmış optimizasyon kazancı",
          summary: `${center.verifiedGains.length} optimizasyon doğrulandı.`,
          href: "/surekli-optimizasyon",
          tone: "neutral",
          category: "Optimizasyon",
        });
      }
    }
  }

  const taskResult = await readAiTaskCenterUnified();
  if (taskResult.status === "success") {
    taskCount = taskResult.center.totalTaskCount;

    if (taskResult.center.closureQueueCounts["awaiting-approval"] > 0) {
      attentionItems.push({
        id: "tasks-awaiting-approval",
        title: "Onay bekleyen görevler",
        summary: `${taskResult.center.closureQueueCounts["awaiting-approval"]} görev onay kuyruğunda.`,
        href: "/?gorunum=onay",
        tone: "warning",
        category: "Görev",
      });
    }
  }

  if (showExecutiveStrip) {
    attentionItems.push({
      id: "executive-command",
      title: "Yönetici komuta görünümü",
      summary: "Sonuç, risk ve doğrulama özetine geç.",
      href: "/yonetici-komuta-merkezi",
      tone: "neutral",
      category: "Yönetim",
    });
  }

  return {
    status: "success",
    workspace: {
      profile,
      profileLabel,
      showExecutiveStrip,
      attentionItems: attentionItems.slice(0, 6),
      decisionQueue,
      shiftLabel,
      openShiftItems,
      pendingApprovals,
      pendingVerification,
      taskCount,
      generatedAtUtc: new Date().toISOString(),
    },
  };
}
