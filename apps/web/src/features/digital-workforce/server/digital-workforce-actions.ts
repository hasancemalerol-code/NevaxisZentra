"use server";

import "./digital-workforce-server-boundary";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import type { DigitalAutonomyMode } from "../domain/digital-workforce-types";
import {
  applyDefaultPolicies,
  decideApproval,
  fetchWorkerDetail,
  globalKill,
  globalPause,
  globalResume,
  globalSuggestMode,
  pauseWorker,
  resumeHumanControl,
  resumeWorker,
  takeOverCase,
  updateWorkerAutonomy,
  updateWorkerPolicy,
  type UpdatePolicyPayload,
} from "./digital-workforce-api-client";

type ActionResult = Readonly<{ status: "success" } | { status: "failure"; message: string }>;

async function resolveContext() {
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

function revalidateDigitalWorkforce() {
  revalidatePath("/dijital-is-gucu");
  revalidatePath("/dashboard");
}

export async function digitalWorkforceUpdateAutonomyAction(
  workerApiId: string,
  autonomyMode: DigitalAutonomyMode,
  concurrencyToken: number,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.autonomy.manage");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Otonomi değiştirme yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  try {
    await updateWorkerAutonomy(ctx.context, workerApiId, autonomyMode, concurrencyToken);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Otonomi güncellenemedi." };
  }
}

export async function digitalWorkforcePauseWorkerAction(
  workerApiId: string,
  concurrencyToken: number,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  try {
    await pauseWorker(ctx.context, workerApiId, concurrencyToken);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Duraklatma başarısız." };
  }
}

export async function digitalWorkforceResumeWorkerAction(
  workerApiId: string,
  concurrencyToken: number,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") {
    return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;

  try {
    await resumeWorker(ctx.context, workerApiId, concurrencyToken);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Başlatma başarısız." };
  }
}

export async function digitalWorkforceGlobalPauseAction(): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await globalPause(ctx.context);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Global duraklatma başarısız." };
  }
}

export async function digitalWorkforceGlobalResumeAction(): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await globalResume(ctx.context);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Global başlatma başarısız." };
  }
}

export async function digitalWorkforceGlobalKillAction(): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.kill");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Acil durdur yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await globalKill(ctx.context);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Acil durdur başarısız." };
  }
}

export async function digitalWorkforceGlobalSuggestModeAction(): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.autonomy.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Otonomi yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await globalSuggestMode(ctx.context);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Öneri modu uygulanamadı." };
  }
}

export async function digitalWorkforceApplyPoliciesAction(): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.policy.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Politika yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await applyDefaultPolicies(ctx.context);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Politikalar uygulanamadı." };
  }
}

export async function digitalWorkforceTakeoverCaseAction(caseApiId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await takeOverCase(ctx.context, caseApiId);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "İş devralma başarısız." };
  }
}

export async function digitalWorkforceResumeHumanControlAction(caseApiId: string): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await resumeHumanControl(ctx.context, caseApiId);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Otonom devam başarısız." };
  }
}

export async function digitalWorkforceDecideApprovalAction(
  approvalId: string,
  decision: "approved" | "rejected" | "modified",
  concurrencyToken: number,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.approval.decide");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Onay kararı yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await decideApproval(ctx.context, approvalId, decision, concurrencyToken);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Onay kararı kaydedilemedi." };
  }
}

export async function digitalWorkforceUpdatePolicyAction(
  workerApiId: string,
  payload: UpdatePolicyPayload,
): Promise<ActionResult> {
  const auth = await denyUnlessGranted("digital-workforce.policy.manage");
  if (auth.status === "failure") return { status: "failure", message: auth.code === "authorization-denied" ? "Politika yönetim yetkiniz yok." : auth.message };
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    await updateWorkerPolicy(ctx.context, workerApiId, payload);
    revalidateDigitalWorkforce();
    return { status: "success" };
  } catch (error) {
    return { status: "failure", message: error instanceof Error ? error.message : "Politika güncellenemedi." };
  }
}

export async function digitalWorkforceReadWorkerDetailAction(workerApiId: string) {
  const auth = await denyUnlessGranted("digital-workforce.read");
  if (auth.status === "failure") {
    return { status: "failure" as const, message: auth.code === "authorization-denied" ? "Görüntüleme yetkiniz yok." : auth.message };
  }
  const ctx = await resolveContext();
  if (ctx.status === "failure") return ctx;
  try {
    const detail = await fetchWorkerDetail(ctx.context, workerApiId);
    return { status: "success" as const, detail };
  } catch (error) {
    return {
      status: "failure" as const,
      message: error instanceof Error ? error.message : "Çalışan detayı yüklenemedi.",
    };
  }
}
