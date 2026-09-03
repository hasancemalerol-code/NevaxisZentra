import "./shift-handover-server-boundary";

import { cache } from "react";

import {
  getShiftHandover,
  listShiftHandovers,
  type RawShiftHandoverDetail,
  type RawShiftHandoverListResponse,
  type ShiftHandoverApiResult,
} from "./shift-handover-api-client";
import { resolveShiftHandoverRequestContext } from "./shift-handover-request-context";

export type ShiftHandoverReadResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string; code: string }>;

async function withContext<T>(
  action: (context: Awaited<ReturnType<typeof resolveShiftHandoverRequestContext>> & { status: "success" }) => Promise<ShiftHandoverApiResult<T>>,
): Promise<ShiftHandoverReadResult<T>> {
  const contextResult = await resolveShiftHandoverRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await action(contextResult);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", value: result.value };
}

export const readShiftHandoverList = cache(async (status?: string): Promise<ShiftHandoverReadResult<RawShiftHandoverListResponse>> =>
  withContext((ctx) => listShiftHandovers(ctx.context, status)));

export const readShiftHandoverDetail = cache(async (handoverId: string): Promise<ShiftHandoverReadResult<RawShiftHandoverDetail>> =>
  withContext((ctx) => getShiftHandover(ctx.context, handoverId)));
