"use server";

import "./material-server-boundary";

import { revalidatePath } from "next/cache";

import {
  createMaterial,
  transitionMaterialLifecycle,
  updateMaterial,
  updateMaterialTechnicalSpecification,
} from "./material-api-client";
import { resolveMaterialRequestContext } from "./material-request-context";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext() {
  const contextResult = resolveMaterialRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

function mapResult(result: Awaited<ReturnType<typeof createMaterial>>): ActionResult {
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/malzemeler");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

function readOptionalNumber(formData: FormData, key: string): number | null {
  const raw = formData.get(key);
  if (raw === null || raw === "") {
    return null;
  }
  return Number(raw);
}

function readOptionalBoolean(formData: FormData, key: string): boolean | null {
  const raw = formData.get(key);
  if (raw === null || raw === "") {
    return null;
  }
  return raw === "true" || raw === "on";
}

export async function createMaterialAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const body = {
    code: String(formData.get("code") ?? ""),
    name: String(formData.get("name") ?? ""),
    description: String(formData.get("description") ?? "") || null,
    materialCategory: String(formData.get("materialCategory") ?? "virgin-polymer"),
    polymerFamily: String(formData.get("polymerFamily") ?? "unknown"),
    manufacturer: String(formData.get("manufacturer") ?? "") || null,
    tradeNameOrGrade: String(formData.get("tradeNameOrGrade") ?? "") || null,
    colorName: String(formData.get("colorName") ?? "") || null,
    colorCode: String(formData.get("colorCode") ?? "") || null,
    physicalForm: String(formData.get("physicalForm") ?? "granule"),
    unitOfMeasure: String(formData.get("unitOfMeasure") ?? "kg"),
    isActive: formData.get("isActive") === "true" || formData.get("isActive") === "on",
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceSystem: String(formData.get("sourceSystem") ?? "") || null,
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    externalMaterialCode: String(formData.get("externalMaterialCode") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await createMaterial(resolved.context, body));
}

export async function updateMaterialAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const materialId = String(formData.get("materialId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    name: String(formData.get("name") ?? ""),
    description: String(formData.get("description") ?? "") || null,
    materialCategory: String(formData.get("materialCategory") ?? "virgin-polymer"),
    polymerFamily: String(formData.get("polymerFamily") ?? "unknown"),
    manufacturer: String(formData.get("manufacturer") ?? "") || null,
    tradeNameOrGrade: String(formData.get("tradeNameOrGrade") ?? "") || null,
    colorName: String(formData.get("colorName") ?? "") || null,
    colorCode: String(formData.get("colorCode") ?? "") || null,
    physicalForm: String(formData.get("physicalForm") ?? "granule"),
    unitOfMeasure: String(formData.get("unitOfMeasure") ?? "kg"),
    isActive: formData.get("isActive") === "true" || formData.get("isActive") === "on",
    sourceSystem: String(formData.get("sourceSystem") ?? "") || null,
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    externalMaterialCode: String(formData.get("externalMaterialCode") ?? "") || null,
    dataRecordedAtUtc: String(formData.get("dataRecordedAtUtc") ?? "") || null,
  };

  return mapResult(await updateMaterial(resolved.context, materialId, body));
}

export async function updateMaterialTechnicalAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const materialId = String(formData.get("materialId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    densityGPerCm3: readOptionalNumber(formData, "densityGPerCm3"),
    meltFlowRate: readOptionalNumber(formData, "meltFlowRate"),
    meltFlowUnit: String(formData.get("meltFlowUnit") ?? "") || null,
    meltFlowTestTemperatureC: readOptionalNumber(formData, "meltFlowTestTemperatureC"),
    meltFlowTestLoadKg: readOptionalNumber(formData, "meltFlowTestLoadKg"),
    moistureLimitPercent: readOptionalNumber(formData, "moistureLimitPercent"),
    dryingRequired: readOptionalBoolean(formData, "dryingRequired"),
    dryingTemperatureMinC: readOptionalNumber(formData, "dryingTemperatureMinC"),
    dryingTemperatureMaxC: readOptionalNumber(formData, "dryingTemperatureMaxC"),
    dryingTimeMinHours: readOptionalNumber(formData, "dryingTimeMinHours"),
    dryingTimeMaxHours: readOptionalNumber(formData, "dryingTimeMaxHours"),
    meltTemperatureMinC: readOptionalNumber(formData, "meltTemperatureMinC"),
    meltTemperatureMaxC: readOptionalNumber(formData, "meltTemperatureMaxC"),
    moldTemperatureMinC: readOptionalNumber(formData, "moldTemperatureMinC"),
    moldTemperatureMaxC: readOptionalNumber(formData, "moldTemperatureMaxC"),
    storageTemperatureMinC: readOptionalNumber(formData, "storageTemperatureMinC"),
    storageTemperatureMaxC: readOptionalNumber(formData, "storageTemperatureMaxC"),
    maximumRegrindPercent: readOptionalNumber(formData, "maximumRegrindPercent"),
    technicalNotes: String(formData.get("technicalNotes") ?? "") || null,
    safetyDataSheetReference: String(formData.get("safetyDataSheetReference") ?? "") || null,
    technicalDataSheetReference: String(formData.get("technicalDataSheetReference") ?? "") || null,
    specificationSource: String(formData.get("specificationSource") ?? "manual"),
    specificationSourceReference: String(formData.get("specificationSourceReference") ?? "") || null,
    specificationRecordedAtUtc: String(formData.get("specificationRecordedAtUtc") ?? "") || null,
  };

  return mapResult(await updateMaterialTechnicalSpecification(resolved.context, materialId, body));
}

export async function transitionMaterialLifecycleAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const materialId = String(formData.get("materialId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    targetStatus: String(formData.get("targetStatus") ?? ""),
    reason: String(formData.get("reason") ?? ""),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await transitionMaterialLifecycle(resolved.context, materialId, body));
}
