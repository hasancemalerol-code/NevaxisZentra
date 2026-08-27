import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialProfitabilityWorkspaceView } from "@/features/commercial-intelligence/components/commercial-profitability-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { COMMERCIAL_PROFITABILITY_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-profitability-labels";
import {
  readCommercialProfitabilityWorkspace,
} from "@/features/commercial-intelligence/server/commercial-profitability-queries";
import {
  readCommercialProfitabilityComparisonFilters,
  toCommercialProfitabilityApiFilters,
  type CommercialProfitabilityComparisonApiFilters,
} from "@/features/commercial-intelligence/application/commercial-profitability-filter";

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
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Ürün Kârlılığı</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PROFITABILITY_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-profitability-forbidden" : "commercial-profitability-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Kârlılık karşılaştırma verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <CommercialProfitabilityWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
