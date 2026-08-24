"use server";

import "./commercial-intelligence-server-boundary";

import { revalidatePath } from "next/cache";

import {
  evaluateCommercialIntelligenceAssessment,
  evaluateCommercialIntelligenceComparison,
  updateCommercialIntelligenceProfile,
} from "./commercial-intelligence-api-client";
import {
  readCommercialIntelligenceAssessmentDetail,
  readCommercialIntelligenceComparisonDetail,
  readCommercialIntelligenceWorkspace,
} from "./commercial-intelligence-queries";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";

const PURCHASE_PRICE_ROUTE_PATH = "/ticari-zeka/satin-alma-fiyat-analizi";
const SUPPLIER_COMPARISON_ROUTE_PATH = "/ticari-zeka/tedarikci-karsilastirmasi";

function revalidateCommercialIntelligenceRoutes() {
  revalidatePath(PURCHASE_PRICE_ROUTE_PATH);
  revalidatePath(SUPPLIER_COMPARISON_ROUTE_PATH);
}

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveManageContext(): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted("commercial-intelligence.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function refreshCommercialIntelligenceWorkspaceAction() {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  revalidateCommercialIntelligenceRoutes();
  const result = await readCommercialIntelligenceWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadCommercialIntelligenceAssessmentDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const result = await readCommercialIntelligenceAssessmentDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadCommercialIntelligenceComparisonDetailAction(comparisonId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const result = await readCommercialIntelligenceComparisonDetail(comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function updateCommercialIntelligenceProfileAction(input: Readonly<{
  profileId: string;
  expectedVersion: number;
  displayName: string;
  status: string;
  comparisonDimension: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumPositiveVarianceRatio: number;
  supplierSpreadThreshold: number | null;
  allowedEvidenceStatuses: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  sourceProvenance: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await updateCommercialIntelligenceProfile(resolved.context, input.profileId, input);
  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "concurrency-conflict"
        ? "Profil başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin."
        : result.message,
      code: result.code,
    };
  }

  revalidateCommercialIntelligenceRoutes();
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function refreshCommercialSupplierComparisonWorkspaceAction(comparisonId?: string | null) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  revalidateCommercialIntelligenceRoutes();
  const { readCommercialSupplierComparisonWorkspace } = await import("./commercial-supplier-comparison-queries");
  const result = await readCommercialSupplierComparisonWorkspace(comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function evaluateCommercialIntelligenceAssessmentAction(input: Readonly<{
  profileId: string;
  materialId: string;
  supplierReference?: string | null;
  comparisonDimension: string;
  currentUnitPrice: number;
  currencyCode: string;
  normalizedUnit: string;
  windowStartUtc: string;
  windowEndUtc: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
  sourceProvenance: string;
  idempotencyKey: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateCommercialIntelligenceAssessment(resolved.context, input);
  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "idempotency-conflict" || result.code === "concurrency-conflict"
        ? "Fiyat analizi çakıştı. Aynı anahtar farklı yük ile kullanılamaz veya kayıt güncellendi."
        : result.message,
      code: result.code,
    };
  }

  revalidateCommercialIntelligenceRoutes();
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function evaluateCommercialIntelligenceComparisonAction(input: Readonly<{
  profileId: string;
  expectedProfileVersion: number;
  comparisonType: string;
  materialId: string;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  referenceWindowStartUtc?: string | null;
  referenceWindowEndUtc?: string | null;
  currencyCode: string;
  normalizedUnit: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
  sourceProvenance: string;
  idempotencyKey: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateCommercialIntelligenceComparison(resolved.context, input);
  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: result.code === "idempotency-conflict" || result.code === "concurrency-conflict"
        ? "Karşılaştırma çakıştı. Aynı anahtar farklı yük ile kullanılamaz veya kayıt güncellendi."
        : result.message,
      code: result.code,
    };
  }

  revalidateCommercialIntelligenceRoutes();
  return {
    status: "success" as const,
    entityId: result.value.entityId,
    idempotentReplay: result.value.idempotentReplay,
  };
}
