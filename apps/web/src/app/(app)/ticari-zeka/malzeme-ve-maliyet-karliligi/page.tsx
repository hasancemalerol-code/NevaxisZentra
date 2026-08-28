import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialMaterialCostProfitabilityWorkspaceView } from "@/features/commercial-intelligence/components/commercial-material-cost-profitability-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import {
  readCommercialMaterialCostProfitabilityFilters,
  toCommercialMaterialCostProfitabilityApiFilters,
} from "@/features/commercial-intelligence/application/commercial-material-cost-profitability-filter";
import { COMMERCIAL_MATERIAL_COST_PROFITABILITY_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-material-cost-profitability-labels";
import {
  readCommercialMaterialCostProfitabilityComparisonDetail,
  readCommercialMaterialCostProfitabilityWorkspace,
} from "@/features/commercial-intelligence/server/commercial-material-cost-profitability-queries";
import type { CommercialMaterialCostProfitabilityLensId } from "@/features/commercial-intelligence/types/commercial-material-cost-profitability-types";
import type { CommercialProfitabilityComparisonDetail } from "@/features/commercial-intelligence/types/commercial-profitability-types";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

function resolveActiveLens(searchParams: PageSearchParams): CommercialMaterialCostProfitabilityLensId {
  const lens = typeof searchParams.lens === "string" ? searchParams.lens : "material";
  return lens === "cost-driver" ? "cost-driver" : "material";
}

export default async function CommercialMaterialCostProfitabilityPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialMaterialCostProfitabilityFilters(filterParams);
  const apiFilters = toCommercialMaterialCostProfitabilityApiFilters(uiFilters);
  const activeLens = resolveActiveLens(resolvedSearchParams);
  const comparisonId = typeof resolvedSearchParams.comparison === "string"
    ? resolvedSearchParams.comparison
    : null;

  const result = await readCommercialMaterialCostProfitabilityWorkspace(comparisonId, apiFilters, activeLens);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Malzeme ve Maliyet Kârlılığı</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_MATERIAL_COST_PROFITABILITY_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-material-cost-profitability-forbidden" : "commercial-material-cost-profitability-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Malzeme ve maliyet kârlılık verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  let initialComparisonDetail: CommercialProfitabilityComparisonDetail | null = null;
  let initialDetailStatus: "idle" | "ready" | "error" = "idle";

  if (comparisonId) {
    const accessibleComparisons = activeLens === "material"
      ? result.workspace.materialComparisons
      : result.workspace.costMixComparisons;
    const comparisonAccessible = accessibleComparisons.some((item) => item.id === comparisonId);
    if (comparisonAccessible) {
      const detailResult = await readCommercialMaterialCostProfitabilityComparisonDetail(comparisonId);
      if (detailResult.status === "success") {
        initialComparisonDetail = detailResult.detail;
        initialDetailStatus = "ready";
      } else {
        initialDetailStatus = "error";
      }
    }
  }

  return (
    <CommercialMaterialCostProfitabilityWorkspaceView
      workspace={result.workspace}
      initialComparisonDetail={initialComparisonDetail}
      initialDetailStatus={initialDetailStatus}
    />
  );
}
