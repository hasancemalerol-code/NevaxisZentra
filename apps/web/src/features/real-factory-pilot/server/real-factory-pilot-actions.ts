"use server";

import "server-only";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { initializeRealFactoryPilot } from "./real-factory-pilot-api-client";

const REAL_FACTORY_PILOT_ROUTE = "/ayarlar/entegrasyonlar/gercek-fabrika";

/**
 * The only way the synthetic pilot fixture is created: an explicit POST by a user holding
 * real-factory-pilot.manage. The API additionally refuses it outside Development/Demo/Presentation deployments and
 * writes an audit record for every call.
 */
export async function initializeRealFactoryPilotAction(): Promise<void> {
  const authorization = await denyUnlessGranted("real-factory-pilot.manage");
  if (authorization.status === "failure") {
    redirect(`${REAL_FACTORY_PILOT_ROUTE}?pilot=${authorization.code === "authorization-denied" ? "denied" : "unavailable"}`);
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    redirect(`${REAL_FACTORY_PILOT_ROUTE}?pilot=unavailable`);
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    redirect(`${REAL_FACTORY_PILOT_ROUTE}?pilot=unavailable`);
  }

  const result = await initializeRealFactoryPilot(contextResult.context);
  if (result.status === "failure") {
    redirect(`${REAL_FACTORY_PILOT_ROUTE}?pilot=failed`);
  }

  revalidatePath(REAL_FACTORY_PILOT_ROUTE);
  revalidatePath("/ayarlar/entegrasyonlar");
  redirect(REAL_FACTORY_PILOT_ROUTE);
}
