"use server";

import "./teams-server-boundary";

import { randomUUID } from "node:crypto";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { markTeamsChannelRead, postTeamsMessage, createMessageTaskDraft } from "./teams-api-client";
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
