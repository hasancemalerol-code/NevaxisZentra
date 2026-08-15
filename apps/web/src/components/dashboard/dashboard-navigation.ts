import { getAiTaskPreviewById } from "@/features/ai-task-center/data/ai-task-preview-data";

import { hasDashboardPermission } from "./dashboard-permission";

export type DashboardNavTarget = Readonly<{
  href: string;
  permissionKey?: string;
  deniedReason: string;
  listScoped?: boolean;
}>;

export const DASHBOARD_AI_TASK_CENTER_HREF = "/";

export function resolveAiTaskNavTarget(taskId: string): DashboardNavTarget {
  const catalogTask = getAiTaskPreviewById(taskId);

  if (catalogTask) {
    return {
      href: `/ai-gorevleri/${catalogTask.id}`,
      deniedReason: "",
    };
  }

  return {
    href: DASHBOARD_AI_TASK_CENTER_HREF,
    deniedReason: "",
    listScoped: true,
  };
}

export function resolveDailyOperationMetricTarget(
  metricId: string,
): DashboardNavTarget {
  switch (metricId) {
    case "planned":
    case "completed":
    case "remaining":
    case "planned-production":
    case "completed-production":
    case "remaining-production":
      return {
        href: "/uretim",
        permissionKey: "production.execution.read",
        deniedReason: "Üretim emirlerini görüntüleme yetkiniz yok.",
      };
    case "quality-exceptions":
      return {
        href: "/kalite",
        permissionKey: "quality.read",
        deniedReason: "Kalite kayıtlarını görüntüleme yetkiniz yok.",
      };
    case "critical-machines":
      return {
        href: "/makineler",
        permissionKey: "edge.read",
        deniedReason: "Makine kayıtlarını görüntüleme yetkiniz yok.",
      };
    case "pending-deliveries":
      return {
        href: "/riskler/siparis-termin",
        deniedReason: "",
      };
    default:
      return {
        href: "/uretim",
        permissionKey: "production.execution.read",
        deniedReason: "Üretim emirlerini görüntüleme yetkiniz yok.",
      };
  }
}

export const DASHBOARD_OPERATIONAL_SUMMARY_TARGET: DashboardNavTarget = {
  href: "/proses-oee",
  permissionKey: "process-oee.read",
  deniedReason: "Proses ve OEE özetini görüntüleme yetkiniz yok.",
};

export function resolveCriticalRiskTarget(
  category: string,
): DashboardNavTarget {
  switch (category) {
    case "delivery":
      return {
        href: "/riskler/siparis-termin",
        deniedReason: "",
      };
    case "material":
      return {
        href: "/malzemeler",
        permissionKey: "material.read",
        deniedReason: "Malzeme kayıtlarını görüntüleme yetkiniz yok.",
      };
    case "mold":
      return {
        href: "/kaliplar",
        permissionKey: "mold.read",
        deniedReason: "Kalıp kayıtlarını görüntüleme yetkiniz yok.",
      };
    case "quality":
      return {
        href: "/kalite",
        permissionKey: "quality.read",
        deniedReason: "Kalite kayıtlarını görüntüleme yetkiniz yok.",
      };
    case "machine":
      return {
        href: "/makineler",
        permissionKey: "edge.read",
        deniedReason: "Makine kayıtlarını görüntüleme yetkiniz yok.",
      };
    default:
      return {
        href: "/riskler/siparis-termin",
        deniedReason: "",
      };
  }
}

export const DASHBOARD_MACHINE_WORKSPACE_TARGET: DashboardNavTarget = {
  href: "/makineler",
  permissionKey: "edge.read",
  deniedReason: "Makine kayıtlarını görüntüleme yetkiniz yok.",
};

export const DASHBOARD_MATERIAL_WORKSPACE_TARGET: DashboardNavTarget = {
  href: "/malzemeler",
  permissionKey: "material.read",
  deniedReason: "Malzeme kayıtlarını görüntüleme yetkiniz yok.",
};

export const DASHBOARD_MOLD_WORKSPACE_TARGET: DashboardNavTarget = {
  href: "/kaliplar",
  permissionKey: "mold.read",
  deniedReason: "Kalıp kayıtlarını görüntüleme yetkiniz yok.",
};

export const DASHBOARD_PRODUCTION_WORKSPACE_TARGET: DashboardNavTarget = {
  href: "/uretim",
  permissionKey: "production.execution.read",
  deniedReason: "Üretim emirlerini görüntüleme yetkiniz yok.",
};

export const DASHBOARD_SHOWCASE_TV_TARGET: DashboardNavTarget = {
  href: "/fabrika-vitrini",
  deniedReason: "",
};

export function canOpenDashboardTarget(
  grantedPermissionKeys: readonly string[],
  target: DashboardNavTarget,
): boolean {
  if (!target.permissionKey) {
    return true;
  }

  return hasDashboardPermission(
    grantedPermissionKeys,
    target.permissionKey,
  );
}
