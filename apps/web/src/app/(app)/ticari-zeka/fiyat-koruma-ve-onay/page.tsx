import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialPricingScenarioWorkspaceView } from "@/features/commercial-intelligence/components/commercial-pricing-scenario-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import {
  readCommercialPricingScenarioFilters,
  toCommercialPricingScenarioApiFilters,
} from "@/features/commercial-intelligence/application/commercial-pricing-scenario-filter";
import { COMMERCIAL_PRICING_SCENARIO_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-pricing-scenario-labels";
import {
  readCommercialPricingScenarioDetail,
  readCommercialPricingScenarioWorkspace,
} from "@/features/commercial-intelligence/server/commercial-pricing-scenario-queries";
import type { CommercialPricingScenarioDetail } from "@/features/commercial-intelligence/types/commercial-pricing-scenario-types";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function CommercialPricingScenarioPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialPricingScenarioFilters(filterParams);
  const apiFilters = toCommercialPricingScenarioApiFilters(uiFilters);
  const scenarioId = typeof resolvedSearchParams.scenario === "string"
    ? resolvedSearchParams.scenario
    : null;

  const result = await readCommercialPricingScenarioWorkspace(scenarioId, apiFilters);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Fiyat Koruma ve Onay</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PRICING_SCENARIO_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-pricing-scenario-forbidden" : "commercial-pricing-scenario-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Fiyat senaryosu verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  let initialScenarioDetail: CommercialPricingScenarioDetail | null = null;
  let initialDetailStatus: "idle" | "ready" | "error" = "idle";

  if (scenarioId) {
    const scenarioAccessible = result.workspace.scenarios.some((item) => item.id === scenarioId);
    if (scenarioAccessible) {
      const detailResult = await readCommercialPricingScenarioDetail(scenarioId);
      if (detailResult.status === "success") {
        initialScenarioDetail = detailResult.detail;
        initialDetailStatus = "ready";
      } else {
        initialDetailStatus = "error";
      }
    }
  }

  return (
    <CommercialPricingScenarioWorkspaceView
      workspace={result.workspace}
      initialScenarioDetail={initialScenarioDetail}
      initialDetailStatus={initialDetailStatus}
    />
  );
}
