import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  readMixedProductionLineDetail,
  readMixedProductionLines,
  readMixedProductionPlayback,
  type MixedProductionLineDetailApi,
  type MixedProductionLineSummaryApi,
  type MixedProductionPlaybackApi,
} from "./mixed-production-api-client";

async function resolveContext() {
  const authorization = await denyUnlessGranted(requiredRoutePermission("hatlar"));
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: "Karma üretim hatları görüntüleme yetkiniz yok." };
  }

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

export async function loadMixedProductionLinesWorkspace(): Promise<
  | { status: "success"; lines: readonly MixedProductionLineSummaryApi[] }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const linesResult = await readMixedProductionLines(resolved.context);
  if (linesResult.status === "failure") {
    return { status: "failure", message: linesResult.message };
  }

  return { status: "success", lines: linesResult.value };
}

export async function loadMixedProductionLineWorkspace(lineId: string): Promise<
  | {
      status: "success";
      detail: MixedProductionLineDetailApi;
      playback: MixedProductionPlaybackApi | null;
    }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const detailResult = await readMixedProductionLineDetail(resolved.context, lineId);
  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message };
  }

  const timeline = detailResult.value.timeline;
  let playback: MixedProductionPlaybackApi | null = null;
  if (timeline.length > 0) {
    const fromUtc = timeline[0].occurredAtUtc;
    const toUtc = timeline[timeline.length - 1].occurredAtUtc;
    const playbackResult = await readMixedProductionPlayback(resolved.context, lineId, fromUtc, toUtc);
    if (playbackResult.status === "success") {
      playback = playbackResult.value;
    }
  }

  return {
    status: "success",
    detail: detailResult.value,
    playback,
  };
}
