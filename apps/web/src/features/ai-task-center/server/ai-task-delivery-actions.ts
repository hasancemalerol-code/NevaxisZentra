"use server";

import { revalidatePath } from "next/cache";

import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import {
  enqueueAiTaskDelivery,
  issueAiTaskSecureAccess,
  revokeAiTaskSecureAccess,
  rotateAiTaskSecureAccess,
} from "./ai-task-delivery-api-client";

async function requireContext() {
  const result = await resolveAiTaskRequestContextFromIncomingRequest();
  if (result.status === "failure") {
    throw new Error(result.message);
  }
  return result.context;
}

export async function issueAiTaskSecureAccessAction(taskId: string, ttlPolicy: string) {
  const context = await requireContext();
  const result = await issueAiTaskSecureAccess(context, taskId, ttlPolicy);
  revalidatePath(`/ai-gorevleri/${taskId}`);
  return result;
}

export async function revokeAiTaskSecureAccessAction(grantId: string) {
  const context = await requireContext();
  const result = await revokeAiTaskSecureAccess(context, grantId);
  revalidatePath("/ai-gorevleri");
  return result;
}

export async function rotateAiTaskSecureAccessAction(grantId: string, ttlPolicy: string) {
  const context = await requireContext();
  const result = await rotateAiTaskSecureAccess(context, grantId, ttlPolicy);
  revalidatePath("/ai-gorevleri");
  return result;
}

export async function enqueueAiTaskDeliveryAction(
  taskId: string,
  body: Readonly<{
    channel: string;
    recipientKind: string;
    idempotencyKey: string;
    secureAccessGrantId?: string;
    maskedRecipient?: string;
    recipientE164?: string;
    consentStatus?: string;
    secureTaskUrl?: string;
    recipientEmail?: string;
    recipientEligibility?: string;
  }>,
) {
  const context = await requireContext();
  const result = await enqueueAiTaskDelivery(context, taskId, body);
  revalidatePath(`/ai-gorevleri/${taskId}`);
  revalidatePath("/ai-gorevleri/teslimatlar");
  return result;
}
