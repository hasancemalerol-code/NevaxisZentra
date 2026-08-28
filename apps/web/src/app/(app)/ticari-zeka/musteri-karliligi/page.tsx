import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialCustomerProfitabilityWorkspaceView } from "@/features/commercial-intelligence/components/commercial-customer-profitability-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import {
  readCommercialCustomerProfitabilityFilters,
  toCommercialCustomerProfitabilityApiFilters,
} from "@/features/commercial-intelligence/application/commercial-customer-profitability-filter";
import { COMMERCIAL_CUSTOMER_PROFITABILITY_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-customer-profitability-labels";
import {
  readCommercialCustomerProfitabilityComparisonDetail,
  readCommercialCustomerProfitabilityWorkspace,
} from "@/features/commercial-intelligence/server/commercial-customer-profitability-queries";
import type { CommercialProfitabilityComparisonDetail } from "@/features/commercial-intelligence/types/commercial-profitability-types";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function CommercialCustomerProfitabilityPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialCustomerProfitabilityFilters(filterParams);
  const apiFilters = toCommercialCustomerProfitabilityApiFilters(uiFilters);
  const comparisonId = typeof resolvedSearchParams.comparison === "string"
    ? resolvedSearchParams.comparison
    : null;

  const result = await readCommercialCustomerProfitabilityWorkspace(comparisonId, apiFilters);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Müşteri Kârlılığı</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_CUSTOMER_PROFITABILITY_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-customer-profitability-forbidden" : "commercial-customer-profitability-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Müşteri kârlılık verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  let initialComparisonDetail: CommercialProfitabilityComparisonDetail | null = null;
  let initialDetailStatus: "idle" | "ready" | "error" = "idle";

  if (comparisonId) {
    const comparisonAccessible = result.workspace.comparisons.some((item) => item.id === comparisonId);
    if (comparisonAccessible) {
      const detailResult = await readCommercialCustomerProfitabilityComparisonDetail(comparisonId);
      if (detailResult.status === "success") {
        initialComparisonDetail = detailResult.detail;
        initialDetailStatus = "ready";
      } else {
        initialDetailStatus = "error";
      }
    }
  }

  return (
    <CommercialCustomerProfitabilityWorkspaceView
      workspace={result.workspace}
      initialComparisonDetail={initialComparisonDetail}
      initialDetailStatus={initialDetailStatus}
    />
  );
}
