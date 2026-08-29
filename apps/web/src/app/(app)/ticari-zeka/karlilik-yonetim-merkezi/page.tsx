import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialProfitabilityCommandCenterView } from "@/features/commercial-intelligence/components/commercial-profitability-command-center-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import {
  readCommercialProfitabilityCommandCenterFilters,
} from "@/features/commercial-intelligence/application/commercial-profitability-command-center-filter";
import { COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-profitability-command-center-labels";
import { readCommercialProfitabilityCommandCenterWorkspace } from "@/features/commercial-intelligence/server/commercial-profitability-command-center-queries";

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function CommercialProfitabilityCommandCenterPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readCommercialProfitabilityCommandCenterFilters(filterParams);

  const result = await readCommercialProfitabilityCommandCenterWorkspace(uiFilters);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Kârlılık Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
          data-testid={isUnauthorized ? "commercial-profitability-command-center-forbidden" : "commercial-profitability-command-center-server-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Yönetim merkezi verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <CommercialProfitabilityCommandCenterView workspace={result.workspace} />
  );
}
