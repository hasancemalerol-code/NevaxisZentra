import "./mold-server-boundary";

import {
  getMold,
  getMoldWorkspaceSummary,
  listMoldLifecycleHistory,
  listMoldMaintenanceHistory,
  listMoldRevisionHistory,
  listMoldTimeline,
  listMoldTrialHistory,
  listMolds,
  probeMoldManagePermission,
  probeMoldReadPermission,
} from "./mold-api-client";
import { resolveMoldRequestContext } from "./mold-request-context";
import type {
  MoldDetail,
  MoldLifecycleEvent,
  MoldMaintenanceRecord,
  MoldRevisionRecord,
  MoldSummary,
  MoldTimelineEntry,
  MoldTrialRecord,
  MoldWorkspaceSummary,
} from "../domain/molds";
import type { MoldApiFailure, MoldListFilters } from "./mold-api-client";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: MoldApiFailure["code"] | "invalid-scope";
}>;

export type MoldRegistryResult =
  | Readonly<{
      status: "success";
      summary: MoldWorkspaceSummary;
      molds: readonly MoldSummary[];
      canManage: boolean;
    }>
  | ReadFailure;

export type MoldDetailResult =
  | Readonly<{
      status: "success";
      mold: MoldDetail;
      lifecycleHistory: readonly MoldLifecycleEvent[];
      maintenanceHistory: readonly MoldMaintenanceRecord[];
      revisionHistory: readonly MoldRevisionRecord[];
      trialHistory: readonly MoldTrialRecord[];
      timeline: readonly MoldTimelineEntry[];
      canManage: boolean;
    }>
  | ReadFailure;

async function resolveContext() {
  const contextResult = resolveMoldRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code as "configuration-error" | "unauthenticated" | "invalid-scope",
    };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readMoldRegistry(filters: MoldListFilters = {}): Promise<MoldRegistryResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [summaryResult, moldsResult] = await Promise.all([
    getMoldWorkspaceSummary(resolved.context),
    listMolds(resolved.context, filters),
  ]);

  if (summaryResult.status === "failure") {
    return {
      status: "failure",
      message: summaryResult.message,
      code: summaryResult.code,
    };
  }

  if (moldsResult.status === "failure") {
    return {
      status: "failure",
      message: moldsResult.message,
      code: moldsResult.code,
    };
  }

  const canManage = summaryResult.value.capabilities.canManage;

  return {
    status: "success",
    summary: summaryResult.value,
    molds: moldsResult.value,
    canManage,
  };
}

export async function readMoldDetail(moldId: string): Promise<MoldDetailResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [moldResult, historyResult, maintenanceResult, revisionResult, trialResult, timelineResult] =
    await Promise.all([
      getMold(resolved.context, moldId),
      listMoldLifecycleHistory(resolved.context, moldId),
      listMoldMaintenanceHistory(resolved.context, moldId),
      listMoldRevisionHistory(resolved.context, moldId),
      listMoldTrialHistory(resolved.context, moldId),
      listMoldTimeline(resolved.context, moldId),
    ]);

  if (moldResult.status === "failure") {
    return {
      status: "failure",
      message: moldResult.message,
      code: moldResult.code,
    };
  }

  if (historyResult.status === "failure") {
    return { status: "failure", message: historyResult.message, code: historyResult.code };
  }
  if (maintenanceResult.status === "failure") {
    return { status: "failure", message: maintenanceResult.message, code: maintenanceResult.code };
  }
  if (revisionResult.status === "failure") {
    return { status: "failure", message: revisionResult.message, code: revisionResult.code };
  }
  if (trialResult.status === "failure") {
    return { status: "failure", message: trialResult.message, code: trialResult.code };
  }
  if (timelineResult.status === "failure") {
    return { status: "failure", message: timelineResult.message, code: timelineResult.code };
  }

  return {
    status: "success",
    mold: moldResult.value,
    lifecycleHistory: historyResult.value,
    maintenanceHistory: maintenanceResult.value,
    revisionHistory: revisionResult.value,
    trialHistory: trialResult.value,
    timeline: timelineResult.value,
    canManage: await probeMoldManagePermission(resolved.context),
  };
}

export async function resolveMoldNavigationVisibility(): Promise<boolean> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return false;
  }

  return probeMoldReadPermission(resolved.context);
}
