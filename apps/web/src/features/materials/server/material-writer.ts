"use server";

import "./material-server-boundary";

import { revalidatePath } from "next/cache";

import {
  createMaterial,
  createMaterialConsumptionObservation,
  createMaterialLot,
  createMaterialRequirementProfile,
  evaluateMaterialRegrindPolicy,
  evaluateMaterialRequirement,
  evaluateMaterialVariance,
  linkMaterialLotCertificate,
  observeMaterialLotReceipt,
  recordMaterialLotQualityStatus,
  transitionMaterialLifecycle,
  transitionMaterialLotStatus,
  transferMaterialLot,
  updateMaterial,
  updateMaterialRequirementProfile,
  updateMaterialTechnicalSpecification,
} from "./material-api-client";
import { resolveMaterialRequestContext } from "./material-request-context";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext() {
  const contextResult = await resolveMaterialRequestContext();
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

export async function createMaterialLotAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const body = {
    materialId: String(formData.get("materialId") ?? ""),
    lotNumber: String(formData.get("lotNumber") ?? ""),
    supplierLotNumber: String(formData.get("supplierLotNumber") ?? "") || null,
    manufacturerLotNumber: String(formData.get("manufacturerLotNumber") ?? "") || null,
    supplierReference: String(formData.get("supplierReference") ?? "") || null,
    purchaseOrderReference: String(formData.get("purchaseOrderReference") ?? "") || null,
    goodsReceiptReference: String(formData.get("goodsReceiptReference") ?? "") || null,
    externalSystem: String(formData.get("externalSystem") ?? "") || null,
    externalLotReference: String(formData.get("externalLotReference") ?? "") || null,
    manufactureDate: String(formData.get("manufactureDate") ?? "") || null,
    expiryDate: String(formData.get("expiryDate") ?? "") || null,
    retestDate: String(formData.get("retestDate") ?? "") || null,
    initialQuantity: readOptionalNumber(formData, "initialQuantity"),
    unitOfMeasure: String(formData.get("unitOfMeasure") ?? "kg"),
    qualityStatus: String(formData.get("qualityStatus") ?? "pending"),
    storageLocationId: String(formData.get("storageLocationId") ?? "") || null,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    notes: String(formData.get("notes") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await createMaterialLot(resolved.context, body));
}

export async function observeMaterialLotReceiptAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotId = String(formData.get("lotId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    receivedAtUtc: String(formData.get("receivedAtUtc") ?? new Date().toISOString()),
    observedQuantity: Number(formData.get("observedQuantity") ?? 0),
    goodsReceiptReference: String(formData.get("goodsReceiptReference") ?? "") || null,
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await observeMaterialLotReceipt(resolved.context, lotId, body));
}

export async function transferMaterialLotAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotId = String(formData.get("lotId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    targetStorageLocationId: String(formData.get("targetStorageLocationId") ?? ""),
    reason: String(formData.get("reason") ?? "") || null,
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await transferMaterialLot(resolved.context, lotId, body));
}

export async function transitionMaterialLotStatusAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotId = String(formData.get("lotId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    targetStatus: String(formData.get("targetStatus") ?? ""),
    reason: String(formData.get("reason") ?? ""),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await transitionMaterialLotStatus(resolved.context, lotId, body));
}

export async function recordMaterialLotQualityStatusAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotId = String(formData.get("lotId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    qualityStatus: String(formData.get("qualityStatus") ?? ""),
    reason: String(formData.get("reason") ?? "") || null,
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await recordMaterialLotQualityStatus(resolved.context, lotId, body));
}

export async function linkMaterialLotCertificateAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotId = String(formData.get("lotId") ?? "");
  const body = {
    certificateType: String(formData.get("certificateType") ?? "other"),
    documentReference: String(formData.get("documentReference") ?? ""),
    documentNumber: String(formData.get("documentNumber") ?? "") || null,
    issuer: String(formData.get("issuer") ?? "") || null,
    issuedDate: String(formData.get("issuedDate") ?? "") || null,
    expiryDate: String(formData.get("expiryDate") ?? "") || null,
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await linkMaterialLotCertificate(resolved.context, lotId, body));
}

export async function createMaterialRequirementProfileAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const body = {
    materialId: String(formData.get("materialId") ?? ""),
    requirementType: String(formData.get("requirementType") ?? "manual-planning"),
    productionOrderReference: String(formData.get("productionOrderReference") ?? "") || null,
    productReference: String(formData.get("productReference") ?? "") || null,
    moldReference: String(formData.get("moldReference") ?? "") || null,
    plannedProductionQuantity: readOptionalNumber(formData, "plannedProductionQuantity"),
    goodPartTargetQuantity: readOptionalNumber(formData, "goodPartTargetQuantity"),
    partWeightGrams: readOptionalNumber(formData, "partWeightGrams"),
    runnerWeightGramsPerShot: readOptionalNumber(formData, "runnerWeightGramsPerShot"),
    effectiveCavityCount:
      formData.get("effectiveCavityCount") === null || formData.get("effectiveCavityCount") === ""
        ? null
        : Number(formData.get("effectiveCavityCount")),
    setupAllowanceGrams: readOptionalNumber(formData, "setupAllowanceGrams"),
    processLossAllowancePercent: readOptionalNumber(formData, "processLossAllowancePercent"),
    plannedVirginPercent: readOptionalNumber(formData, "plannedVirginPercent"),
    plannedRegrindPercent: readOptionalNumber(formData, "plannedRegrindPercent"),
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    dataRecordedAtUtc: String(formData.get("dataRecordedAtUtc") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await createMaterialRequirementProfile(resolved.context, body));
}

export async function updateMaterialRequirementProfileAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const profileId = String(formData.get("profileId") ?? "");
  const body = {
    expectedVersion: Number(formData.get("expectedVersion") ?? 0),
    requirementType: String(formData.get("requirementType") ?? "manual-planning"),
    productionOrderReference: String(formData.get("productionOrderReference") ?? "") || null,
    productReference: String(formData.get("productReference") ?? "") || null,
    moldReference: String(formData.get("moldReference") ?? "") || null,
    plannedProductionQuantity: readOptionalNumber(formData, "plannedProductionQuantity"),
    goodPartTargetQuantity: readOptionalNumber(formData, "goodPartTargetQuantity"),
    partWeightGrams: readOptionalNumber(formData, "partWeightGrams"),
    runnerWeightGramsPerShot: readOptionalNumber(formData, "runnerWeightGramsPerShot"),
    effectiveCavityCount:
      formData.get("effectiveCavityCount") === null || formData.get("effectiveCavityCount") === ""
        ? null
        : Number(formData.get("effectiveCavityCount")),
    setupAllowanceGrams: readOptionalNumber(formData, "setupAllowanceGrams"),
    processLossAllowancePercent: readOptionalNumber(formData, "processLossAllowancePercent"),
    plannedVirginPercent: readOptionalNumber(formData, "plannedVirginPercent"),
    plannedRegrindPercent: readOptionalNumber(formData, "plannedRegrindPercent"),
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    dataRecordedAtUtc: String(formData.get("dataRecordedAtUtc") ?? "") || null,
  };

  return mapResult(await updateMaterialRequirementProfile(resolved.context, profileId, body));
}

export async function createMaterialConsumptionObservationAction(formData: FormData): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const body = {
    materialId: String(formData.get("materialId") ?? ""),
    materialLotId: String(formData.get("materialLotId") ?? "") || null,
    requirementProfileId: String(formData.get("requirementProfileId") ?? "") || null,
    observationType: String(formData.get("observationType") ?? "consumption-observed"),
    quantity: Number(formData.get("quantity") ?? 0),
    unitOfMeasure: String(formData.get("unitOfMeasure") ?? "kg"),
    virginQuantity: readOptionalNumber(formData, "virginQuantity"),
    regrindQuantity: readOptionalNumber(formData, "regrindQuantity"),
    goodPartQuantity: readOptionalNumber(formData, "goodPartQuantity"),
    scrapQuantity: readOptionalNumber(formData, "scrapQuantity"),
    productionOrderReference: String(formData.get("productionOrderReference") ?? "") || null,
    productReference: String(formData.get("productReference") ?? "") || null,
    moldReference: String(formData.get("moldReference") ?? "") || null,
    observedAtUtc: String(formData.get("observedAtUtc") ?? new Date().toISOString()),
    sourceType: String(formData.get("sourceType") ?? "manual"),
    sourceReference: String(formData.get("sourceReference") ?? "") || null,
    dataRecordedAtUtc: String(formData.get("dataRecordedAtUtc") ?? "") || null,
    notes: String(formData.get("notes") ?? "") || null,
    idempotencyKey: String(formData.get("idempotencyKey") ?? crypto.randomUUID()),
  };

  return mapResult(await createMaterialConsumptionObservation(resolved.context, body));
}

export async function evaluateMaterialRequirementAssessmentsAction(
  formData: FormData,
): Promise<ActionResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const materialId = String(formData.get("materialId") ?? "");
  const profileId = String(formData.get("requirementProfileId") ?? "");
  const idempotencyKey = String(formData.get("idempotencyKey") ?? crypto.randomUUID());
  const body = {
    requirementProfileId: profileId,
    idempotencyKey,
  };

  const requirementResult = await evaluateMaterialRequirement(resolved.context, materialId, body);
  if (requirementResult.status === "failure") {
    return { status: "failure", message: requirementResult.message };
  }

  const varianceResult = await evaluateMaterialVariance(resolved.context, materialId, {
    requirementProfileId: profileId,
    idempotencyKey: crypto.randomUUID(),
  });
  if (varianceResult.status === "failure") {
    return { status: "failure", message: varianceResult.message };
  }

  const regrindResult = await evaluateMaterialRegrindPolicy(resolved.context, materialId, {
    requirementProfileId: profileId,
    idempotencyKey: crypto.randomUUID(),
  });
  if (regrindResult.status === "failure") {
    return { status: "failure", message: regrindResult.message };
  }

  revalidatePath("/malzemeler");
  return {
    status: "success",
    id: requirementResult.value.id,
    version: requirementResult.value.version,
  };
}
