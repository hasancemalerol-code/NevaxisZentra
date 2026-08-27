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
const SALES_PRICE_ROUTE_PATH = "/ticari-zeka/satis-fiyat-analizi";
const QUOTATION_RISK_ROUTE_PATH = "/ticari-zeka/teklif-riskleri";
const PROFITABILITY_ROUTE_PATH = "/ticari-zeka/urun-karliligi";

function revalidateCommercialIntelligenceRoutes() {
  revalidatePath(PURCHASE_PRICE_ROUTE_PATH);
  revalidatePath(SUPPLIER_COMPARISON_ROUTE_PATH);
  revalidatePath(SALES_PRICE_ROUTE_PATH);
  revalidatePath(QUOTATION_RISK_ROUTE_PATH);
  revalidatePath(PROFITABILITY_ROUTE_PATH);
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

export async function refreshCommercialSalesPriceWorkspaceAction(
  assessmentId?: string | null,
  comparisonId?: string | null,
) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  revalidateCommercialIntelligenceRoutes();
  const { readCommercialSalesPriceWorkspace } = await import("./commercial-sales-price-queries");
  const result = await readCommercialSalesPriceWorkspace(assessmentId, comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadCommercialSalesPriceAssessmentDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const { readCommercialSalesPriceAssessmentDetail } = await import("./commercial-sales-price-queries");
  const result = await readCommercialSalesPriceAssessmentDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadCommercialSalesPriceComparisonDetailAction(comparisonId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const { readCommercialSalesPriceComparisonDetail } = await import("./commercial-sales-price-queries");
  const result = await readCommercialSalesPriceComparisonDetail(comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function evaluateCommercialSalesPriceComparisonAction(input: Readonly<{
  profileId: string;
  expectedProfileVersion: number;
  comparisonType: string;
  materialId: string;
  customerReference?: string | null;
  sourceEvidenceKind: string;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  referenceWindowStartUtc?: string | null;
  referenceWindowEndUtc?: string | null;
  currencyCode: string;
  normalizedUnit: string;
  priceBasisGrossNet: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
  sourceProvenance: string;
  idempotencyKey: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const { evaluateCommercialSalesPriceComparison } = await import("./commercial-sales-price-api-client");
  const result = await evaluateCommercialSalesPriceComparison(resolved.context, input);
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

export async function refreshCommercialQuotationRiskWorkspaceAction(
  assessmentId?: string | null,
  comparisonId?: string | null,
) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  revalidateCommercialIntelligenceRoutes();
  const { readCommercialQuotationRiskWorkspace } = await import("./commercial-quotation-risk-queries");
  const result = await readCommercialQuotationRiskWorkspace(assessmentId, comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadCommercialQuotationRiskAssessmentDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const { readCommercialQuotationRiskAssessmentDetail } = await import("./commercial-quotation-risk-queries");
  const result = await readCommercialQuotationRiskAssessmentDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadCommercialQuotationRiskComparisonDetailAction(comparisonId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const { readCommercialQuotationRiskComparisonDetail } = await import("./commercial-quotation-risk-queries");
  const result = await readCommercialQuotationRiskComparisonDetail(comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function updateCommercialQuotationRiskProfileAction(input: Readonly<{
  profileId: string;
  expectedVersion: number;
  name: string;
  status: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumNegativePriceVarianceRatio: number;
  maximumPositivePriceVarianceRatio: number;
  maximumPaymentTermDays: number;
  expiryWarningDays: number;
  maximumEvidenceAgeDays: number;
  allowedEvidenceStatuses: string;
  allowedSalesEvidenceKinds: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  requiredGrossNetBasis: string;
  sourceProvenance: string;
}>) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const { updateCommercialQuotationRiskProfile } = await import("./commercial-quotation-risk-api-client");
  const result = await updateCommercialQuotationRiskProfile(resolved.context, input.profileId, input);
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

export async function refreshCommercialProfitabilityWorkspaceAction(
  comparisonId?: string | null,
  filters?: import("./commercial-profitability-queries").CommercialProfitabilityComparisonApiFilters,
) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  revalidateCommercialIntelligenceRoutes();
  const { readCommercialProfitabilityWorkspace } = await import("./commercial-profitability-queries");
  const result = await readCommercialProfitabilityWorkspace(comparisonId, filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadCommercialProfitabilityComparisonDetailAction(comparisonId: string) {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") return authorization;

  const { readCommercialProfitabilityComparisonDetail } = await import("./commercial-profitability-queries");
  const result = await readCommercialProfitabilityComparisonDetail(comparisonId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, detail: result.detail };
}
