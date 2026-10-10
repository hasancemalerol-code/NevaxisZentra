import "./erp-server-boundary";

import { getErpWriteback, listErpWritebacks } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type { ErpWritebackDetail, ErpWritebackSummary } from "../domain/erp";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type ErpWritebacksResult =
  | Readonly<{ status: "success"; writebacks: readonly ErpWritebackSummary[] }>
  | ReadFailure;

export type ErpWritebackDetailResult =
  | Readonly<{ status: "success"; writeback: ErpWritebackDetail }>
  | ReadFailure;

export async function readErpWritebacks(): Promise<ErpWritebacksResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const writebacksResult = await listErpWritebacks(contextResult.context);
  if (writebacksResult.status === "failure") {
    return { status: "failure", message: writebacksResult.message, code: writebacksResult.code };
  }

  return { status: "success", writebacks: writebacksResult.value };
}

export async function readErpWritebackDetail(writebackId: string): Promise<ErpWritebackDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const writebackResult = await getErpWriteback(contextResult.context, writebackId);
  if (writebackResult.status === "failure") {
    return { status: "failure", message: writebackResult.message, code: writebackResult.code };
  }

  return { status: "success", writeback: writebackResult.value };
}
