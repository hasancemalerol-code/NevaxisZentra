"use server";

import "./teams-server-boundary";

import { randomUUID } from "node:crypto";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  acknowledgeTeamsHandover,
  createTeamsDirectConversation,
  markTeamsDirectConversationRead,
  postTeamsDirectMessage,
  assignTeamsApprovalReviewer,
  cancelTeamsApproval,
  createMessageTaskDraft,
  createTeamsApproval,
  createTeamsHandover,
  decideTeamsApproval,
  markTeamsChannelRead,
  postTeamsMessage,
  publishTeamsHandover,
  updateTeamsHandoverDraft,
  type TeamsHandoverItemWriteBody,
} from "./teams-api-client";
import { resolveTeamsRequestContext } from "./teams-request-context";

type ActionResult =
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>;

export async function sendTeamsMessageAction(input: Readonly<{
  channelId: string;
  body: string;
  idempotencyKey?: string;
  mentionedMembershipIds?: readonly string[];
}>): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("teams.write");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Mesaj gönderme yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await postTeamsMessage(contextResult.context, input.channelId, {
    body: input.body,
    idempotencyKey: input.idempotencyKey ?? randomUUID(),
    mentionedMembershipIds: input.mentionedMembershipIds,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams");
  return { status: "success" };
}

export async function markTeamsChannelReadAction(input: Readonly<{
  channelId: string;
  lastReadMessageId: string | null;
}>): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("teams.write");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Okundu işaretleme yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await markTeamsChannelRead(
    contextResult.context,
    input.channelId,
    input.lastReadMessageId,
  );

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams");
  return { status: "success" };
}

type TaskDraftActionResult =
  | Readonly<{ status: "success"; taskId: string; idempotentReplay: boolean }>
  | Readonly<{ status: "failure"; message: string }>;

export async function createMessageTaskDraftAction(input: Readonly<{
  messageId: string;
  title: string;
  category: string;
  priority: string;
  humanNote?: string | null;
}>): Promise<TaskDraftActionResult> {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Görev taslağı oluşturma yetkisi yok." };
  }

  const teamsAuthorization = await denyUnlessGranted("teams.read");
  if (teamsAuthorization.status === "failure") {
    return { status: "failure", message: "Teams okuma yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await createMessageTaskDraft(contextResult.context, input.messageId, {
    title: input.title,
    category: input.category,
    priority: input.priority,
    humanNote: input.humanNote,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams");
  revalidatePath(`/ai-gorevleri/${result.value.taskId}`);
  return {
    status: "success",
    taskId: result.value.taskId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

type ApprovalActionResult =
  | Readonly<{ status: "success"; requestId: string }>
  | Readonly<{ status: "failure"; message: string }>;

export async function createTeamsApprovalAction(input: Readonly<{
  channelId: string;
  messageId?: string | null;
  aiTaskId?: string | null;
  title: string;
  rationale: string;
  reviewerMembershipIds: readonly string[];
}>): Promise<ApprovalActionResult> {
  const authorization = await denyUnlessGranted("teams.approvals.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Onay isteği oluşturma yetkisi yok." };
  }

  const readAuthorization = await denyUnlessGranted("teams.read");
  if (readAuthorization.status === "failure") {
    return { status: "failure", message: "Teams okuma yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await createTeamsApproval(contextResult.context, {
    channelId: input.channelId,
    messageId: input.messageId,
    aiTaskId: input.aiTaskId,
    title: input.title,
    rationale: input.rationale,
    idempotencyKey: randomUUID(),
    reviewers: input.reviewerMembershipIds.map((tenantMembershipId) => ({
      tenantMembershipId,
      role: "reviewer",
    })),
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams");
  revalidatePath("/teams/onaylar");
  revalidatePath(`/teams/onaylar/${result.value.id}`);
  return { status: "success", requestId: result.value.id };
}

export async function decideTeamsApprovalAction(input: Readonly<{
  requestId: string;
  decision: string;
  note?: string | null;
}>): Promise<ApprovalActionResult> {
  const authorization = await denyUnlessGranted("teams.approvals.review");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Onay kararı yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await decideTeamsApproval(contextResult.context, input.requestId, {
    decision: input.decision,
    note: input.note,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/onaylar");
  revalidatePath(`/teams/onaylar/${input.requestId}`);
  return { status: "success", requestId: result.value.id };
}

export async function cancelTeamsApprovalAction(input: Readonly<{
  requestId: string;
}>): Promise<ApprovalActionResult> {
  const authorization = await denyUnlessGranted("teams.approvals.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Onay isteği iptal yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await cancelTeamsApproval(contextResult.context, input.requestId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/onaylar");
  revalidatePath(`/teams/onaylar/${input.requestId}`);
  return { status: "success", requestId: result.value.id };
}

export async function assignTeamsApprovalReviewerAction(input: Readonly<{
  requestId: string;
  tenantMembershipId: string;
  role?: string;
}>): Promise<ApprovalActionResult> {
  const authorization = await denyUnlessGranted("teams.approvals.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: "İnceleyen atama yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await assignTeamsApprovalReviewer(contextResult.context, input.requestId, {
    tenantMembershipId: input.tenantMembershipId,
    role: input.role ?? "reviewer",
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath(`/teams/onaylar/${input.requestId}`);
  return { status: "success", requestId: result.value.id };
}

type HandoverActionResult =
  | Readonly<{ status: "success"; handoverId: string }>
  | Readonly<{ status: "failure"; message: string }>;

export async function createTeamsHandoverAction(input: Readonly<{
  channelId: string;
  shiftDate: string;
  shiftType: string;
  summary: string;
  safetyNote: string;
  items: readonly TeamsHandoverItemWriteBody[];
}>): Promise<HandoverActionResult> {
  const authorization = await denyUnlessGranted("teams.handover.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Vardiya devri oluşturma yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await createTeamsHandover(contextResult.context, {
    channelId: input.channelId,
    shiftDate: input.shiftDate,
    shiftType: input.shiftType,
    summary: input.summary,
    safetyNote: input.safetyNote,
    idempotencyKey: randomUUID(),
    items: input.items,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams");
  revalidatePath("/teams/devretme");
  revalidatePath(`/teams/devretme/${result.value.id}`);
  return { status: "success", handoverId: result.value.id };
}

export async function updateTeamsHandoverDraftAction(input: Readonly<{
  handoverId: string;
  shiftDate: string;
  shiftType: string;
  summary: string;
  safetyNote: string;
  items: readonly TeamsHandoverItemWriteBody[];
}>): Promise<HandoverActionResult> {
  const authorization = await denyUnlessGranted("teams.handover.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Vardiya devri güncelleme yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await updateTeamsHandoverDraft(contextResult.context, input.handoverId, {
    shiftDate: input.shiftDate,
    shiftType: input.shiftType,
    summary: input.summary,
    safetyNote: input.safetyNote,
    items: input.items,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/devretme");
  revalidatePath(`/teams/devretme/${input.handoverId}`);
  return { status: "success", handoverId: result.value.id };
}

export async function publishTeamsHandoverAction(input: Readonly<{
  handoverId: string;
}>): Promise<HandoverActionResult> {
  const authorization = await denyUnlessGranted("teams.handover.create");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Vardiya devri yayımlama yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await publishTeamsHandover(contextResult.context, input.handoverId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/devretme");
  revalidatePath(`/teams/devretme/${input.handoverId}`);
  return { status: "success", handoverId: result.value.id };
}

export async function acknowledgeTeamsHandoverAction(input: Readonly<{
  handoverId: string;
}>): Promise<HandoverActionResult> {
  const authorization = await denyUnlessGranted("teams.handover.acknowledge");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Vardiya devri teslim alma yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await acknowledgeTeamsHandover(contextResult.context, input.handoverId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/devretme");
  revalidatePath(`/teams/devretme/${input.handoverId}`);
  return { status: "success", handoverId: result.value.id };
}

type DirectMessageActionResult =
  | Readonly<{ status: "success"; conversationId: string }>
  | Readonly<{ status: "failure"; message: string }>;

export async function createTeamsDirectConversationAction(input: Readonly<{
  recipientTenantMembershipId: string;
}>): Promise<DirectMessageActionResult> {
  const authorization = await denyUnlessGranted("teams.direct.write");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Özel mesaj başlatma yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await createTeamsDirectConversation(contextResult.context, {
    recipientTenantMembershipId: input.recipientTenantMembershipId,
    idempotencyKey: randomUUID(),
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/ozel-mesajlar");
  return { status: "success", conversationId: result.value.id };
}

export async function sendTeamsDirectMessageAction(input: Readonly<{
  conversationId: string;
  body: string;
  idempotencyKey?: string;
}>): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("teams.direct.write");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Özel mesaj gönderme yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await postTeamsDirectMessage(contextResult.context, input.conversationId, {
    body: input.body,
    idempotencyKey: input.idempotencyKey ?? randomUUID(),
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/ozel-mesajlar");
  return { status: "success" };
}

export async function markTeamsDirectConversationReadAction(input: Readonly<{
  conversationId: string;
  lastReadMessageId: string | null;
}>): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("teams.direct.write");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Okundu işaretleme yetkisi yok." };
  }

  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await markTeamsDirectConversationRead(contextResult.context, input.conversationId, {
    lastReadMessageId: input.lastReadMessageId,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/teams/ozel-mesajlar");
  return { status: "success" };
}
