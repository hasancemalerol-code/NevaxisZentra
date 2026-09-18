import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { ACTIVE_PRODUCTION_ORDER_STATUSES } from "@/features/production-execution/domain/production-order";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";

export type FabrikaHubEntry = Readonly<{
  label: string;
  description: string;
  href: string;
  metric: string;
}>;

export type FabrikaHubWorkspace = Readonly<{
  entries: readonly FabrikaHubEntry[];
}>;

export async function readFabrikaHubWorkspace(): Promise<FabrikaHubWorkspace> {
  const access = await fetchCurrentEffectiveAccess();
  const entries: FabrikaHubEntry[] = [];

  if (hasGrantedPermission(access, "production.execution.read")) {
    const ordersResult = await readProductionOrders();
    const activeCount =
      ordersResult.status === "success"
        ? ordersResult.orders.filter((order) => ACTIVE_PRODUCTION_ORDER_STATUSES.has(order.status)).length
        : 0;

    entries.push({
      label: "Üretim Emirleri",
      description: "Canlı üretim emirleri ve durumları",
      href: "/uretim",
      metric: ordersResult.status === "success" ? `${activeCount} aktif` : "—",
    });
  }

  if (hasGrantedPermission(access, "mixed-production.read")) {
    const linesResult = await loadMixedProductionLinesWorkspace();
    const runningCount =
      linesResult.status === "success"
        ? linesResult.lines.filter((line) => line.lineState.toLowerCase() !== "idle").length
        : 0;

    entries.push({
      label: "Hatlar",
      description: "Hat → istasyon → kaynak hiyerarşisi",
      href: "/uretim/hatlar",
      metric: linesResult.status === "success" ? `${linesResult.lines.length} hat · ${runningCount} çalışıyor` : "—",
    });

    entries.push({
      label: "Kaynaklar",
      description: "Evrensel kaynak ve kapasite görünümü",
      href: "/uretim/kaynaklar",
      metric: linesResult.status === "success" ? `${linesResult.lines.length} hat bağlamı` : "—",
    });
  }

  if (hasGrantedPermission(access, "edge.read")) {
    entries.push({
      label: "Makineler",
      description: "Makine telemetrisi ve edge durumu",
      href: "/makineler",
      metric: "Canlı",
    });
  }

  if (hasGrantedPermission(access, "process-oee.read")) {
    entries.push({
      label: "Proses & OEE",
      description: "Döngü süresi, OEE ve proses derinliği",
      href: "/proses-oee",
      metric: "Operasyonel",
    });
  }

  return { entries };
}
