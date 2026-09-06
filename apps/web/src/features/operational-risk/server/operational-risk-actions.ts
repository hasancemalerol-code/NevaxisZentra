"use server";

import "./operational-risk-server-boundary";

import { revalidatePath } from "next/cache";

import {
  executeOperationalRiskDetectionRun,
  transitionOperationalRiskCase,
  updateOperationalRiskDetectionProfile,
} from "./operational-risk-api-client";
import {
  readOperationalRiskCaseDetail,
  readOperationalRiskRunDetail,
  readOperationalRiskWorkspace,
} from "./operational-risk-queries";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { resolveOperationalRiskRequestContext } from "./operational-risk-request-context";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveManageContext(): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted("operational-risk.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveOperationalRiskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function refreshOperationalRiskWorkspaceAction() {
  const authorization = await denyUnlessGranted("operational-risk.read");
  if (authorization.status === "failure") return authorization;

  revalidatePath("/operasyonel-riskler");
  const result = await readOperationalRiskWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadOperationalRiskCaseDetailAction(caseId: string) {
  const authorization = await denyUnlessGranted("operational-risk.read");
  if (authorization.status === "failure") return authorization;

  const result = await readOperationalRiskCaseDetail(caseId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadOperationalRiskRunDetailAction(runId: string) {
  const authorization = await denyUnlessGranted("operational-risk.read");
  if (authorization.status === "failure") return authorization;

  const result = await readOperationalRiskRunDetail(runId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function transitionOperationalRiskCaseAction(input: Readonly<{
  caseId: string;
  expectedVersion: number;
  targetStatus: string;
  note?: string;
  sourceProvenance: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await transitionOperationalRiskCase(resolved.context, input.caseId, {
    expectedVersion: input.expectedVersion,
    targetStatus: input.targetStatus,
    note: input.note,
    sourceProvenance: input.sourceProvenance,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "concurrency-conflict"
        ? "Vaka başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin."
        : result.message,
      code: result.code,
    };
  }

  revalidatePath("/operasyonel-riskler");
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function updateOperationalRiskDetectionProfileAction(input: Readonly<{
  profileId: string;
  expectedVersion: number;
  displayName: string;
  status: string;
  sourceProvenance: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await updateOperationalRiskDetectionProfile(
    resolved.context,
    input.profileId,
    input,
  );

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "concurrency-conflict"
        ? "Profil başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin."
        : result.message,
      code: result.code,
    };
  }

  revalidatePath("/operasyonel-riskler");
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function executeOperationalRiskDetectionRunAction(input: Readonly<{
  profileId: string;
  windowStartUtc: string;
  windowEndUtc: string;
  sourceProvenance: string;
  idempotencyKey: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await executeOperationalRiskDetectionRun(resolved.context, input);
  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "idempotency-conflict" || result.code === "concurrency-conflict"
        ? "Tespit çalıştırması çakıştı. Aynı anahtar farklı yük ile kullanılamaz veya kayıt güncellendi."
        : result.message,
      code: result.code,
    };
  }

  revalidatePath("/operasyonel-riskler");
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}
