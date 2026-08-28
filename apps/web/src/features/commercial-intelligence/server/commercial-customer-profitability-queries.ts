import "./commercial-intelligence-server-boundary";

import {
  buildCustomerProfitabilityKpiCards,
  buildCustomerProfitabilityTableRows,
  filterProfitabilityCustomerComparisons,
  PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE,
  selectLatestProfitabilityComparison,
} from "../components/commercial-profitability-presentation";
import type { CommercialCustomerProfitabilityApiFilters } from "../application/commercial-customer-profitability-filter";
import type { CommercialCustomerProfitabilityWorkspacePayload } from "../types/commercial-customer-profitability-types";
import { getCommercialIntelligenceEvidenceStatusLabel } from "../domain/commercial-intelligence-labels";
import { localizeCommercialProfitabilityProvenance } from "../domain/commercial-profitability-labels";
import {
  getCommercialProfitabilityWorkspace,
  listCommercialProfitabilityComparisons,
  listCommercialProfitabilityProfiles,
} from "./commercial-profitability-api-client";
import {
  freshnessLabel,
  mapComparisonSummary,
  mapProfile,
  readCommercialProfitabilityComparisonDetail,
  reportingPeriodLabel,
  type CommercialProfitabilityComparisonDetailResult,
} from "./commercial-profitability-queries";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

export type { CommercialCustomerProfitabilityApiFilters };
export type { CommercialProfitabilityComparisonDetailResult as CommercialCustomerProfitabilityComparisonDetailResult };

export type CommercialCustomerProfitabilityWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialCustomerProfitabilityWorkspacePayload; canManage: boolean }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

export async function readCommercialCustomerProfitabilityWorkspace(
  requestedComparisonId?: string | null,
  filters: CommercialCustomerProfitabilityApiFilters = { page: 1, pageSize: 25 },
): Promise<CommercialCustomerProfitabilityWorkspaceResult> {
  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchCurrentEffectiveAccess();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, comparisonsResult] = await Promise.all([
    getCommercialProfitabilityWorkspace(contextResult.context),
    listCommercialProfitabilityProfiles(contextResult.context),
    listCommercialProfitabilityComparisons(contextResult.context, {
      comparisonType: PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE,
      customerReference: filters.customerReference,
      profileId: filters.profileId,
      outcome: filters.outcome,
      evidenceStatus: filters.evidenceStatus,
      windowFromUtc: filters.windowFromUtc,
      windowToUtc: filters.windowToUtc,
      page: filters.page,
      pageSize: filters.pageSize,
    }),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }
  if (comparisonsResult.status === "failure") {
    return { status: "failure", message: comparisonsResult.message, code: comparisonsResult.code };
  }

  const comparisons = comparisonsResult.value.items
    .map(mapComparisonSummary)
    .filter((item) => item.comparisonType === PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE);
  const profiles = profilesResult.value.items.map(mapProfile);
  const hasPilot = comparisons.some((item) => item.isPilotData) || profiles.some((item) => item.isPilotData);
  const customerComparisons = filterProfitabilityCustomerComparisons(comparisons);
  const selected = requestedComparisonId
    ? customerComparisons.find((item) => item.id === requestedComparisonId)
      ?? selectLatestProfitabilityComparison(customerComparisons)
    : null;

  return {
    status: "success",
    canManage,
    workspace: {
      facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
      evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
      hasDevelopmentPilotData: hasPilot,
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      comparisonRuleSetVersion: comparisonsResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(workspaceResult.value.provenanceDisclaimer),
      realProfitabilityMappingComplete: workspaceResult.value.realProfitabilityMappingComplete,
      reportingPeriodLabel: reportingPeriodLabel(customerComparisons),
      freshnessLabel: freshnessLabel(customerComparisons),
      profiles,
      comparisons: customerComparisons,
      selectedComparisonId: selected?.id ?? null,
      kpiCards: buildCustomerProfitabilityKpiCards(customerComparisons),
      tableRows: buildCustomerProfitabilityTableRows(customerComparisons),
      page: comparisonsResult.value.page,
      pageSize: comparisonsResult.value.pageSize,
      returnedCount: customerComparisons.length,
    },
  };
}

export async function readCommercialCustomerProfitabilityComparisonDetail(
  comparisonId: string,
): Promise<CommercialProfitabilityComparisonDetailResult> {
  return readCommercialProfitabilityComparisonDetail(comparisonId);
}
