import "./works-server-boundary";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { resolveWorksQueueId } from "../domain/works-queues";
import type { WorksWorkspacePagePayload } from "../domain/works-types";
import { fetchWorksCaseDetail, fetchWorksWorkspace } from "./works-api-client";

export type WorksWorkspacePageResult =
  | Readonly<{ status: "success"; page: WorksWorkspacePagePayload }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

async function resolveProductionContext() {
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

export async function readWorksWorkspacePage(input?: Readonly<{
  initialQueue?: string | null;
  selectedCaseId?: string | null;
  search?: string | null;
}>): Promise<WorksWorkspacePageResult> {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, requiredRoutePermission("isler"))) {
    return {
      status: "failure",
      message: "Bu modüle erişim yetkiniz yok.",
      code: "permission-denied",
    };
  }

  const contextResult = await resolveProductionContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const initialQueue = resolveWorksQueueId(input?.initialQueue ?? null);
  const search = input?.search?.trim() ?? "";
  const selectedCaseId = input?.selectedCaseId?.trim() || null;

  try {
    const workspace = await fetchWorksWorkspace(contextResult.context, {
      queue: initialQueue,
      search: search || undefined,
      limit: 50,
    });

    let caseDetail = null;
    if (selectedCaseId) {
      try {
        caseDetail = await fetchWorksCaseDetail(contextResult.context, selectedCaseId);
      } catch (error) {
        const message = error instanceof Error ? error.message : "İş detayı yüklenemedi.";
        if (message.toLowerCase().includes("not found") || message.includes("404")) {
          return {
            status: "failure",
            message: "Seçilen iş bulunamadı.",
            code: "not-found",
          };
        }
        return { status: "failure", message };
      }
    }

    const canDecideApproval = hasGrantedPermission(access, "digital-workforce.approval.decide");

    return {
      status: "success",
      page: {
        workspace,
        caseDetail,
        canDecideApproval,
        initialQueue,
        selectedCaseId,
        search,
      },
    };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "İş çalışma alanı yüklenemedi.",
    };
  }
}

export async function readWorksCaseDetail(caseId: string) {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, requiredRoutePermission("isler"))) {
    return {
      status: "failure" as const,
      message: "Bu modüle erişim yetkiniz yok.",
      code: "permission-denied" as const,
    };
  }

  const contextResult = await resolveProductionContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  try {
    const detail = await fetchWorksCaseDetail(contextResult.context, caseId);
    return { status: "success" as const, detail };
  } catch (error) {
    const message = error instanceof Error ? error.message : "İş detayı yüklenemedi.";
    if (message.toLowerCase().includes("not found") || message.includes("404")) {
      return { status: "failure" as const, message, code: "not-found" as const };
    }
    return { status: "failure" as const, message };
  }
}
