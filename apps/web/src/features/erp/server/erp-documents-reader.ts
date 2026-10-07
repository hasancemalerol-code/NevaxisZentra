import "./erp-server-boundary";

import { getErpDocument, listErpDocuments } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpDocumentDetail,
  CanonicalErpDocumentSummary,
  ErpDocumentsWorkspaceSnapshot,
} from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpDocumentsResult =
  | Readonly<{
      status: "success";
      snapshot: ErpDocumentsWorkspaceSnapshot;
      documents: readonly CanonicalErpDocumentSummary[];
    }>
  | ReadFailure;

export type ErpDocumentDetailResult =
  | Readonly<{ status: "success"; document: CanonicalErpDocumentDetail }>
  | ReadFailure;

function buildDocumentsSnapshot(
  documents: readonly CanonicalErpDocumentSummary[],
): ErpDocumentsWorkspaceSnapshot {
  const freshRecordCount = documents.filter((row) => row.freshness === "fresh").length;
  const staleRecordCount = documents.filter((row) => row.freshness === "stale").length;
  const contentAvailableCount = documents.filter((row) => row.contentAvailable).length;

  return {
    documentCount: documents.length,
    contentAvailableCount,
    freshRecordCount,
    staleRecordCount,
  };
}

export async function readErpDocuments(): Promise<ErpDocumentsResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const documentsResult = await listErpDocuments(contextResult.context);
  if (documentsResult.status === "failure") {
    return { status: "failure", message: documentsResult.message };
  }

  return {
    status: "success",
    snapshot: buildDocumentsSnapshot(documentsResult.value),
    documents: documentsResult.value,
  };
}

export async function readErpDocumentDetail(documentId: string): Promise<ErpDocumentDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const documentResult = await getErpDocument(contextResult.context, documentId);
  if (documentResult.status === "failure") {
    return { status: "failure", message: documentResult.message };
  }

  return { status: "success", document: documentResult.value };
}
