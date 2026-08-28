import "./commercial-intelligence-server-boundary";

import type { CommercialMaterialCostProfitabilityApiFilters } from "../application/commercial-material-cost-profitability-filter";
import {
  buildCostDriverProfitabilityTableRows,
  buildMaterialCostProfitabilityKpiCards,
  buildMaterialProfitabilityTableRows,
  filterProfitabilityCostMixComparisons,
  filterProfitabilityMaterialComparisons,
  PROFITABILITY_COST_COMPONENT_MIX_TYPE,
  PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE,
  selectLatestProfitabilityComparison,
} from "../components/commercial-profitability-presentation";
import type { CommercialMaterialCostProfitabilityWorkspacePayload } from "../types/commercial-material-cost-profitability-types";
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

export type { CommercialMaterialCostProfitabilityApiFilters };
export type { CommercialProfitabilityComparisonDetailResult as CommercialMaterialCostProfitabilityComparisonDetailResult };

export type CommercialMaterialCostProfitabilityWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialMaterialCostProfitabilityWorkspacePayload; canManage: boolean }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

function buildFilterScopeLabel(filters: CommercialMaterialCostProfitabilityApiFilters): string {
  const parts: string[] = [];
  if (filters.materialId) parts.push("Malzeme filtresi");
  if (filters.profileId) parts.push("Profil filtresi");
  if (filters.outcome) parts.push("Sonuç filtresi");
  if (filters.evidenceStatus) parts.push("Kanıt filtresi");
  if (filters.windowFromUtc || filters.windowToUtc) parts.push("Zaman penceresi");
  return parts.length > 0 ? parts.join(" · ") : "Tüm filtreler açık";
}

export async function readCommercialMaterialCostProfitabilityWorkspace(
  requestedComparisonId?: string | null,
  filters: CommercialMaterialCostProfitabilityApiFilters = { page: 1, pageSize: 25 },
  activeLens: "material" | "cost-driver" = "material",
): Promise<CommercialMaterialCostProfitabilityWorkspaceResult> {
  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchCurrentEffectiveAccess();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const sharedFilters = {
    materialId: filters.materialId,
    profileId: filters.profileId,
    outcome: filters.outcome,
    evidenceStatus: filters.evidenceStatus,
    windowFromUtc: filters.windowFromUtc,
    windowToUtc: filters.windowToUtc,
    page: filters.page,
    pageSize: filters.pageSize,
  };

  const [workspaceResult, profilesResult, materialComparisonsResult, costMixComparisonsResult] = await Promise.all([
    getCommercialProfitabilityWorkspace(contextResult.context),
    listCommercialProfitabilityProfiles(contextResult.context),
    listCommercialProfitabilityComparisons(contextResult.context, {
      ...sharedFilters,
      comparisonType: PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE,
    }),
    listCommercialProfitabilityComparisons(contextResult.context, {
      ...sharedFilters,
      comparisonType: PROFITABILITY_COST_COMPONENT_MIX_TYPE,
    }),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }
  if (materialComparisonsResult.status === "failure") {
    return { status: "failure", message: materialComparisonsResult.message, code: materialComparisonsResult.code };
  }
  if (costMixComparisonsResult.status === "failure") {
    return { status: "failure", message: costMixComparisonsResult.message, code: costMixComparisonsResult.code };
  }

  const materialComparisons = materialComparisonsResult.value.items
    .map(mapComparisonSummary)
    .filter((item) => item.comparisonType === PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE);
  const costMixComparisons = costMixComparisonsResult.value.items
    .map(mapComparisonSummary)
    .filter((item) => item.comparisonType === PROFITABILITY_COST_COMPONENT_MIX_TYPE);
  const profiles = profilesResult.value.items.map(mapProfile);
  const hasPilot = materialComparisons.some((item) => item.isPilotData)
    || costMixComparisons.some((item) => item.isPilotData)
    || profiles.some((item) => item.isPilotData);

  const scopedMaterial = filterProfitabilityMaterialComparisons(materialComparisons);
  const scopedCostMix = filterProfitabilityCostMixComparisons(costMixComparisons);
  const allComparisons = [...scopedMaterial, ...scopedCostMix];

  const selectedMaterial = requestedComparisonId && activeLens === "material"
    ? scopedMaterial.find((item) => item.id === requestedComparisonId)
      ?? selectLatestProfitabilityComparison(scopedMaterial)
    : selectLatestProfitabilityComparison(scopedMaterial);
  const selectedCostMix = requestedComparisonId && activeLens === "cost-driver"
    ? scopedCostMix.find((item) => item.id === requestedComparisonId)
      ?? selectLatestProfitabilityComparison(scopedCostMix)
    : selectLatestProfitabilityComparison(scopedCostMix);

  return {
    status: "success",
    canManage,
    workspace: {
      facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
      evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
      hasDevelopmentPilotData: hasPilot,
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      comparisonRuleSetVersion: materialComparisonsResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(workspaceResult.value.provenanceDisclaimer),
      realProfitabilityMappingComplete: workspaceResult.value.realProfitabilityMappingComplete,
      reportingPeriodLabel: reportingPeriodLabel(allComparisons),
      freshnessLabel: freshnessLabel(allComparisons),
      filterScopeLabel: buildFilterScopeLabel(filters),
      profiles,
      materialComparisons: scopedMaterial,
      costMixComparisons: scopedCostMix,
      selectedMaterialComparisonId: selectedMaterial?.id ?? null,
      selectedCostMixComparisonId: selectedCostMix?.id ?? null,
      kpiCards: buildMaterialCostProfitabilityKpiCards(scopedMaterial, scopedCostMix),
      materialTableRows: buildMaterialProfitabilityTableRows(scopedMaterial),
      costMixTableRows: buildCostDriverProfitabilityTableRows(scopedCostMix),
      materialPage: materialComparisonsResult.value.page,
      materialPageSize: materialComparisonsResult.value.pageSize,
      materialReturnedCount: scopedMaterial.length,
      costMixPage: costMixComparisonsResult.value.page,
      costMixPageSize: costMixComparisonsResult.value.pageSize,
      costMixReturnedCount: scopedCostMix.length,
    },
  };
}

export async function readCommercialMaterialCostProfitabilityComparisonDetail(
  comparisonId: string,
): Promise<CommercialProfitabilityComparisonDetailResult> {
  return readCommercialProfitabilityComparisonDetail(comparisonId);
}
