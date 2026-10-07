import "./digital-workforce-server-boundary";



import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";

import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";

import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";



import {

  fetchActivity,

  fetchApprovals,

  fetchDigitalWorkforceControlCenter,

  fetchWorkerDetail,

  type DigitalWorkActivityApiSummary,

  type DigitalWorkApprovalApiSummary,

  type DigitalWorkforceControlCenterSnapshot,

  type DigitalWorkerDetailSnapshot,

} from "./digital-workforce-api-client";



export type DigitalWorkforceHubResult =

  | Readonly<{ status: "success"; snapshot: DigitalWorkforceControlCenterSnapshot }>

  | Readonly<{ status: "failure"; message: string }>;



export type DigitalWorkforceApprovalsResult =

  | Readonly<{ status: "success"; approvals: readonly DigitalWorkApprovalApiSummary[] }>

  | Readonly<{ status: "failure"; message: string }>;



export type DigitalWorkforceActivityResult =

  | Readonly<{ status: "success"; activity: readonly DigitalWorkActivityApiSummary[] }>

  | Readonly<{ status: "failure"; message: string }>;



export type DigitalWorkforceWorkerDetailResult =

  | Readonly<{ status: "success"; detail: DigitalWorkerDetailSnapshot }>

  | Readonly<{ status: "failure"; message: string }>;



async function resolveAuthorizedContext() {

  const authorization = await denyUnlessGranted("digital-workforce.read");

  if (authorization.status === "failure") {

    return { status: "failure" as const, message: authorization.code === "authorization-denied" ? "Dijital İş Gücü görüntüleme yetkiniz yok." : authorization.message };

  }



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



export async function readDigitalWorkforceControlCenter(): Promise<DigitalWorkforceHubResult> {

  const resolved = await resolveAuthorizedContext();

  if (resolved.status === "failure") return resolved;



  try {

    const snapshot = await fetchDigitalWorkforceControlCenter(resolved.context);

    return { status: "success", snapshot };

  } catch (error) {

    return {

      status: "failure",

      message: error instanceof Error ? error.message : "Dijital İş Gücü verisi yüklenemedi.",

    };

  }

}



export async function readDigitalWorkforceApprovals(

  status?: string,

): Promise<DigitalWorkforceApprovalsResult> {

  const resolved = await resolveAuthorizedContext();

  if (resolved.status === "failure") return resolved;



  try {

    const approvals = await fetchApprovals(resolved.context, status);

    return { status: "success", approvals };

  } catch (error) {

    return {

      status: "failure",

      message: error instanceof Error ? error.message : "Onaylar yüklenemedi.",

    };

  }

}



export async function readDigitalWorkforceActivity(): Promise<DigitalWorkforceActivityResult> {

  const resolved = await resolveAuthorizedContext();

  if (resolved.status === "failure") return resolved;



  try {

    const activity = await fetchActivity(resolved.context);

    return { status: "success", activity };

  } catch (error) {

    return {

      status: "failure",

      message: error instanceof Error ? error.message : "Aktivite yüklenemedi.",

    };

  }

}



export async function readDigitalWorkforceWorkerDetail(

  workerApiId: string,

): Promise<DigitalWorkforceWorkerDetailResult> {

  const resolved = await resolveAuthorizedContext();

  if (resolved.status === "failure") return resolved;



  try {

    const detail = await fetchWorkerDetail(resolved.context, workerApiId);

    return { status: "success", detail };

  } catch (error) {

    return {

      status: "failure",

      message: error instanceof Error ? error.message : "Çalışan detayı yüklenemedi.",

    };

  }

}


