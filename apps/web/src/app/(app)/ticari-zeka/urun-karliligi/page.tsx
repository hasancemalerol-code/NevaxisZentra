

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialProfitabilityWorkspaceView } from "@/features/commercial-intelligence/components/commercial-profitability-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { COMMERCIAL_PROFITABILITY_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-profitability-labels";
import {
  readCommercialProfitabilityComparisonDetail,
  readCommercialProfitabilityWorkspace,
} from "@/features/commercial-intelligence/server/commercial-profitability-queries";
import type { CommercialProfitabilityComparisonDetail } from "@/features/commercial-intelligence/types/commercial-profitability-types";
import {
  readCommercialProfitabilityComparisonFilters,
  toCommercialProfitabilityApiFilters,
  type CommercialProfitabilityComparisonApiFilters,
} from "@/features/commercial-intelligence/application/commercial-profitability-filter";

import { FailureStatePage } from "@/components/ui/failure-state";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function CommercialProfitabilityPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialProfitabilityComparisonFilters(filterParams);
  const apiFilters: CommercialProfitabilityComparisonApiFilters = toCommercialProfitabilityApiFilters(uiFilters);
  const comparisonId = typeof resolvedSearchParams.comparison === "string"
    ? resolvedSearchParams.comparison
    : null;

  const result = await readCommercialProfitabilityWorkspace(comparisonId, apiFilters);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Ürün Kârlılığı</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PROFITABILITY_SUBTITLE}
            </p>
          </div>
        </header>
        <div data-testid={isUnauthorized ? "commercial-profitability-forbidden" : "commercial-profitability-server-error"}><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Kârlılık karşılaştırma verisi yüklenemedi" inline /></div>
      </div>
    );
  }

  let initialComparisonDetail: CommercialProfitabilityComparisonDetail | null = null;
  let initialDetailStatus: "idle" | "ready" | "error" = "idle";

  if (comparisonId) {
    const comparisonAccessible = result.workspace.comparisons.some((item) => item.id === comparisonId);
    if (comparisonAccessible) {
      const detailResult = await readCommercialProfitabilityComparisonDetail(comparisonId);
      if (detailResult.status === "success") {
        initialComparisonDetail = detailResult.detail;
        initialDetailStatus = "ready";
      } else {
        initialDetailStatus = "error";
      }
    }
  }

  return (
    <CommercialProfitabilityWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
      initialComparisonDetail={initialComparisonDetail}
      initialDetailStatus={initialDetailStatus}
    />
  );
}
