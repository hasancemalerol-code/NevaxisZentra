"use server";

import "./teams-server-boundary";

import { randomUUID } from "node:crypto";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { markTeamsChannelRead, postTeamsMessage } from "./teams-api-client";
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
