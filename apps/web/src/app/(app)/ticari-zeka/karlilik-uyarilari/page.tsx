import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialProfitabilityAlertWorkspaceView } from "@/features/commercial-intelligence/components/commercial-profitability-alert-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import {
  readCommercialProfitabilityAlertFilters,
  toCommercialProfitabilityAlertApiFilters,
} from "@/features/commercial-intelligence/application/commercial-profitability-alert-filter";
import { COMMERCIAL_PROFITABILITY_ALERT_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-profitability-alert-labels";
import {
  readCommercialProfitabilityAlertDetail,
  readCommercialProfitabilityAlertWorkspace,
} from "@/features/commercial-intelligence/server/commercial-profitability-alert-queries";
import type { CommercialProfitabilityAlertDetail } from "@/features/commercial-intelligence/types/commercial-profitability-alert-types";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function CommercialProfitabilityAlertPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialProfitabilityAlertFilters(filterParams);
  const apiFilters = toCommercialProfitabilityAlertApiFilters(uiFilters);
  const alertId = typeof resolvedSearchParams.alert === "string"
    ? resolvedSearchParams.alert
    : null;

  const result = await readCommercialProfitabilityAlertWorkspace(alertId, apiFilters);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Kârlılık Uyarıları</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PROFITABILITY_ALERT_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-profitability-alert-forbidden" : "commercial-profitability-alert-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Kârlılık uyarı verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  let initialAlertDetail: CommercialProfitabilityAlertDetail | null = null;
  let initialDetailStatus: "idle" | "ready" | "error" = "idle";

  if (alertId) {
    const alertAccessible = result.workspace.alerts.some((item) => item.id === alertId);
    if (alertAccessible) {
      const detailResult = await readCommercialProfitabilityAlertDetail(alertId);
      if (detailResult.status === "success") {
        initialAlertDetail = detailResult.detail;
        initialDetailStatus = "ready";
      } else {
        initialDetailStatus = "error";
      }
    }
  }

  return (
    <CommercialProfitabilityAlertWorkspaceView
      workspace={result.workspace}
      initialAlertDetail={initialAlertDetail}
      initialDetailStatus={initialDetailStatus}
    />
  );
}
