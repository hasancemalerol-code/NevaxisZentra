import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { listEdgeMachines } from "@/features/edge/server/edge-api-client";
import { loadMachineDetailBundle } from "@/features/edge/server/edge-machine-detail-loader";
import { loadFactoryIntelligenceWorkspace } from "@/features/factory-intelligence/server/factory-intelligence-reader";
import { loadMixedProductionLineWorkspace, loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { ACTIVE_PRODUCTION_ORDER_STATUSES } from "@/features/production-execution/domain/production-order";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import { readProductionResourceWorkspace } from "@/features/production-resources/server/production-resources-api-client";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { loadThroughputIntelligenceWorkspace } from "@/features/throughput-intelligence/server/throughput-intelligence-reader";
import { loadThroughputOptimizerWorkspaceForLine } from "@/features/throughput-optimizer/server/throughput-optimizer-reader";
import { resolveRoleLandingProfile } from "@/lib/navigation/role-landing";

import { buildFactoryHierarchy } from "../application/build-factory-hierarchy";
import { readFactoryWorkspaceSelection } from "../application/factory-workspace-selection";
import type { FactoryWorkspacePayload } from "../domain/factory-workspace-types";

export type FactoryWorkspaceResult =
  | Readonly<{ status: "success"; workspace: FactoryWorkspacePayload }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext() {
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

export async function readFactoryWorkspace(
  searchParams?: Readonly<Record<string, string | string[] | undefined>>,
): Promise<FactoryWorkspaceResult> {
  const access = await fetchCurrentEffectiveAccess();
  const canReadMixedProduction = hasGrantedPermission(access, "mixed-production.read");
  const canReadEdge = hasGrantedPermission(access, "edge.read");
  const canReadFactoryIntelligence = hasGrantedPermission(access, "factory-intelligence.read");

  if (!canReadMixedProduction) {
    return { status: "failure", message: "Fabrika çalışma alanı görüntüleme yetkiniz yok." };
  }

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return contextResult;
  }

  const context = contextResult.context;
  const selection = readFactoryWorkspaceSelection(searchParams);
  const roleProfile = resolveRoleLandingProfile(access);
  const facilityLabel = context.facilityId;

  const [linesResult, resourcesResult, ordersResult, shiftResult, intelligenceResult] = await Promise.all([
    loadMixedProductionLinesWorkspace(),
    readProductionResourceWorkspace(context),
    hasGrantedPermission(access, "production.execution.read")
      ? readProductionOrders({ limit: 100 })
      : Promise.resolve({ status: "failure" as const, message: "forbidden" }),
    hasGrantedPermission(access, "shift-workforce.read")
      ? readShiftOperationsWorkspace()
      : Promise.resolve({ status: "failure" as const, message: "forbidden" }),
    canReadFactoryIntelligence ? loadFactoryIntelligenceWorkspace() : Promise.resolve({ status: "failure" as const, message: "forbidden" }),
  ]);

  if (linesResult.status === "failure") {
    return { status: "failure", message: linesResult.message };
  }

  if (resourcesResult.status === "failure") {
    return { status: "failure", message: resourcesResult.message };
  }

  const hierarchy = buildFactoryHierarchy(
    facilityLabel,
    linesResult.lines,
    resourcesResult.value.resources,
  );

  const activeOrderCount =
    ordersResult.status === "success"
      ? ordersResult.orders.filter((order) => ACTIVE_PRODUCTION_ORDER_STATUSES.has(order.status)).length
      : null;

  const shift =
    shiftResult.status === "success"
      ? {
          shiftLabel: shiftResult.currentShift?.name ?? "Aktif vardiya yok",
          openItems: shiftResult.openItems?.length ?? 0,
          supervisorLabel: shiftResult.currentShift?.supervisorLabel ?? undefined,
        }
      : null;

  const risks = intelligenceResult.status === "success" ? intelligenceResult.risks.slice(0, 8) : [];

  let lineDetail = null;
  let playback = null;
  let throughput = null;
  let optimizer = null;

  if (selection.lineId) {
    const lineResult = await loadMixedProductionLineWorkspace(selection.lineId);
    if (lineResult.status === "success") {
      lineDetail = lineResult.detail;
      playback = lineResult.playback;
    }

    const throughputResult = await loadThroughputIntelligenceWorkspace(selection.lineId);
    if (throughputResult.status === "success") {
      throughput = {
        overview: throughputResult.overview,
        comparison: throughputResult.comparison,
      };
    }

    const optimizerResult = await loadThroughputOptimizerWorkspaceForLine(selection.lineId);
    if (optimizerResult.status === "success") {
      optimizer = optimizerResult.workspace;
    }
  }

  const selectedResource =
    selection.resourceId
      ? resourcesResult.value.resources.find((resource) => resource.resourceId === selection.resourceId) ?? null
      : null;

  let machineRegistry = null;
  let machineDetail = null;

  const machineRegistryCode = selectedResource?.machineRegistryCode;
  if (canReadEdge && machineRegistryCode) {
    const machinesResult = await listEdgeMachines(context);
    if (machinesResult.status === "success") {
      const matchedMachine = machinesResult.value
        .filter((machine): machine is NonNullable<typeof machine> => machine != null)
        .find((machine) => machine.machineCode === machineRegistryCode);
      machineRegistry = matchedMachine ?? null;

      if (matchedMachine) {
        machineDetail = await loadMachineDetailBundle(context, matchedMachine.id);
      }
    }
  }

  return {
    status: "success",
    workspace: {
      facilityLabel,
      roleProfile,
      hierarchy,
      resources: resourcesResult.value,
      selection,
      risks,
      shift,
      activeOrderCount,
      lineDetail,
      playback,
      throughput,
      optimizer,
      selectedResource,
      machineRegistry,
      machineDetail,
      canReadMixedProduction,
      canReadEdge,
      canReadFactoryIntelligence,
    },
  };
}
