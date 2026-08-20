"use server";

import "./energy-server-boundary";

import { revalidatePath } from "next/cache";

import {
  createEnergyTarget,
  evaluateEnergyAssessment,
  updateEnergyTarget,
} from "./energy-api-client";
import {
  readEnergyAssessmentDetail,
  readEnergyMeasurementPointDetail,
  readEnergyWorkspace,
} from "./energy-queries";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { resolveEnergyRequestContext } from "./energy-request-context";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;
type ActionSuccess = Readonly<{ status: "success"; id: string; version: number }>;

export async function refreshEnergyWorkspaceAction() {
  const authorization = await denyUnlessGranted("energy.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  revalidatePath("/enerji");
  const result = await readEnergyWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadEnergyMeasurementPointDetailAction(reference: string) {
  const authorization = await denyUnlessGranted("energy.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readEnergyMeasurementPointDetail(reference);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    detail: result.detail,
  };
}

export async function loadEnergyAssessmentDetailAction(reference: string) {
  const authorization = await denyUnlessGranted("energy.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readEnergyAssessmentDetail(reference);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    detail: result.detail,
  };
}

export async function createEnergyTargetAction(input: {
  targetReference: string;
  targetScope: string;
  scopeReference: string;
  targetMetric: string;
  targetValue: number;
  unit: string;
  tolerance: number;
  effectiveFromUtc: string;
  effectiveToUtc: string;
  sourceProvenance: string;
  idempotencyKey: string;
}): Promise<ActionSuccess | ActionFailure> {
  const authorization = await denyUnlessGranted("energy.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Hedef oluşturmak için energy.manage yetkisi gerekir." };
  }

  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await createEnergyTarget(resolved.context, input);
  if (result.status === "failure") {
    return {
      status: "failure",
      message:
        result.code === "concurrency-conflict"
          ? "Hedef oluşturma çakışması: kayıt başka bir işlem tarafından güncellenmiş olabilir."
          : result.message,
    };
  }

  revalidatePath("/enerji");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function updateEnergyTargetAction(input: {
  targetReference: string;
  targetValue: number;
  tolerance: number;
  effectiveFromUtc: string;
  effectiveToUtc: string;
  sourceProvenance: string;
  status: string;
  expectedVersion: number;
}): Promise<ActionSuccess | ActionFailure> {
  const authorization = await denyUnlessGranted("energy.manage");
  if (authorization.status === "failure") {
    return { status: "failure", message: "Hedef güncellemek için energy.manage yetkisi gerekir." };
  }

  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await updateEnergyTarget(resolved.context, input.targetReference, {
    targetValue: input.targetValue,
    tolerance: input.tolerance,
    effectiveFromUtc: input.effectiveFromUtc,
    effectiveToUtc: input.effectiveToUtc,
    sourceProvenance: input.sourceProvenance,
    status: input.status,
    expectedVersion: input.expectedVersion,
  });

  if (result.status === "failure") {
    return {
      status: "failure",
      message:
        result.code === "concurrency-conflict"
          ? "Eşzamanlılık çakışması: hedef başka bir kullanıcı tarafından güncellenmiş olabilir."
          : result.message,
    };
  }

  revalidatePath("/enerji");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function evaluateEnergyAssessmentAction(input: {
  assessmentReference: string;
  comparisonDimension: string;
  measurementPointReference: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  idempotencyKey: string;
}): Promise<ActionSuccess | ActionFailure> {
  const authorization = await denyUnlessGranted("energy.manage");
  if (authorization.status === "failure") {
    return {
      status: "failure",
      message: "Değerlendirme çalıştırmak için energy.manage yetkisi gerekir.",
    };
  }

  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return { status: "failure", message: resolved.message };
  }

  const result = await evaluateEnergyAssessment(resolved.context, input);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/enerji");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}
