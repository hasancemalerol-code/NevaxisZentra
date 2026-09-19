import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readAiTaskCenterUnified } from "@/features/ai-task-center/server/ai-task-center-queries";
import { readFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import type { FactoryDecisionCaseQueueItemApi } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import {
  resolveRoleLandingLabel,
  resolveRoleLandingProfile,
  type RoleLandingProfile,
} from "@/lib/navigation/role-landing";

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

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readBugunWorkspace(): Promise<BugunWorkspaceResult> {
  const access = await fetchCurrentEffectiveAccess();
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
