import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readAiTaskCenterUnified } from "@/features/ai-task-center/server/ai-task-center-queries";
import { fetchDigitalWorkforceControlCenter } from "@/features/digital-workforce/server/digital-workforce-api-client";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type IslerHubEntry = Readonly<{
  label: string;
  description: string;
  href: string;
  metric: string;
}>;

export type IslerHubWorkspace = Readonly<{
  entries: readonly IslerHubEntry[];
}>;

async function resolveProductionContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return null;
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return null;
  }

  return contextResult.context;
}

export async function readIslerHubWorkspace(): Promise<IslerHubWorkspace> {
  const access = await fetchCurrentEffectiveAccess();
  const entries: IslerHubEntry[] = [];

  const taskResult = await readAiTaskCenterUnified();
  if (taskResult.status === "success") {
    entries.push({
      label: "Açık İşler",
      description: "Atanan görevler, onaylar ve doğrulama kuyrukları",
      href: "/",
      metric: `${taskResult.center.totalTaskCount} iş`,
    });
  }

  if (hasGrantedPermission(access, "digital-workforce.read")) {
    const context = await resolveProductionContext();
    if (context) {
      try {
        const workforceSnapshot = await fetchDigitalWorkforceControlCenter(context);
        entries.push({
          label: "Onaylar",
          description: "İnsan kararı bekleyen aksiyonlar",
          href: "/dijital-is-gucu/onaylar",
          metric: `${workforceSnapshot.pendingApprovals} bekliyor`,
        });
        entries.push({
          label: "Sonuçlar",
          description: "Doğrulanmış ve kapatılan iş sonuçları",
          href: "/dijital-is-gucu/sonuclar",
          metric: `${workforceSnapshot.openCases} açık iş`,
        });
      } catch {
        // Workforce snapshot is optional for the İşler hub summary.
      }
    }

    entries.push({
      label: "Dijital İş Gücü",
      description: "Gelişmiş kontrol ve denetim görünümü",
      href: "/dijital-is-gucu",
      metric: "Detay",
    });
  }

  entries.push({
    label: "FactOS Teams",
    description: "Ekip mesajları ve işbirliği",
    href: "/teams",
    metric: "İşbirliği",
  });

  return { entries };
}
