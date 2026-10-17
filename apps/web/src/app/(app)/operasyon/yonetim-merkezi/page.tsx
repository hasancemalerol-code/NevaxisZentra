import { AuthorizationGateState, FailureStatePage } from "@/components/ui/failure-state";


import { denyUnlessAnyGranted } from "@/features/admin-access/server/require-current-permission";
import { OperationsCommandCenterView } from "@/features/operations-command-center/components/operations-command-center-view";
import {
  readOperationsCommandCenterFilters,
} from "@/features/operations-command-center/application/operations-command-center-filter";
import { OPERATIONS_COMMAND_CENTER_SUBTITLE } from "@/features/operations-command-center/domain/operations-command-center-labels";
import { readOperationsCommandCenterWorkspace } from "@/features/operations-command-center/server/operations-command-center-queries";
import Link from "next/link";

import { CONTINUOUS_IMPROVEMENT_SUBTITLE } from "@/features/factory-learning/domain/factory-learning-labels";
import { GOLDEN_DEMO_P1_ROUTE, GOLDEN_DEMO_P1_SUBTITLE, GOLDEN_DEMO_P1_TITLE } from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";
import { readGoldenDemoP2PresentationStatus } from "@/features/golden-demo-p2/server/golden-demo-p2-reader";
import { FactoryNetworkCrossLinkSection } from "@/features/factory-network/components/factory-network-workspace-panel";
import { ProductionManagerWorkspacePanel } from "@/features/production-manager/components/production-manager-workspace-panel";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

import "@/features/shift-operations/components/shift-operations-workspace.css";
import "@/features/operations-command-center/components/operations-command-center.css";

import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

const MODULE_READ_PERMISSIONS = [
  "factory-case.read",
  "production.execution.read",
  "material.read",
  "erp.read",
  "quality.read",
  "maintenance.read",
  "mold.read",
  "commercial-intelligence.read",
  "ai-task.read",
] as const;

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function OperationsCommandCenterPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  const authorization = await denyUnlessAnyGranted(MODULE_READ_PERMISSIONS);
  if (authorization.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>Operasyon Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">{OPERATIONS_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <AuthorizationGateState code={authorization.code} />
      </div>
    );
  }

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readOperationsCommandCenterFilters(filterParams);

  const result = await readOperationsCommandCenterWorkspace(uiFilters);
  const presentationResult = deferredSurfacesEnabled()
    ? await readGoldenDemoP2PresentationStatus()
    : ({ status: "failure" } as const);

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>Operasyon Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">{OPERATIONS_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <div data-testid="operations-command-center-server-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Operasyon merkezi verisi yüklenemedi" inline /></div>
      </div>
    );
  }

  return (
    <>
      <OperationsCommandCenterView workspace={result.workspace} />
      <div className="commint-page commint-page--related" aria-label="İlgili çalışma alanları" role="region">
        <ProductionManagerWorkspacePanel />
        <FactoryNetworkCrossLinkSection />
        {deferredSurfacesEnabled() ? (
          <section className="commint-panel" data-testid="golden-demo-p1-link-section">
            <header className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">ALTIN DEMO</p>
                <h2 className="commint-panel__title">{GOLDEN_DEMO_P1_TITLE}</h2>
                <p className="commint-panel__subtitle">{GOLDEN_DEMO_P1_SUBTITLE}</p>
              </div>
            </header>
            <Link className="shift-ops-cta" href={GOLDEN_DEMO_P1_ROUTE}>
              INJ-001 Üretim Kurtarma Akışını Aç
            </Link>
            {presentationResult.status === "success" ? (
              <p data-testid="golden-demo-p2-occ-status">
                Sunum adımı {presentationResult.presentation.stepNumber}/
                {presentationResult.presentation.totalSteps}: {presentationResult.presentation.presentationStepLabel}
              </p>
            ) : null}
          </section>
        ) : null}
        <section className="commint-panel occ-related-link-card" data-testid="continuous-improvement-link-section">
          <div>
            <h2 className="occ-related-link-card__title">Sürekli iyileştirme</h2>
            <p className="occ-related-link-card__text">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
          </div>
          <Link className="commint-button" href="/operasyon/yonetim-merkezi/surekli-iyilestirme">
            Sürekli iyileştirmeyi aç
          </Link>
        </section>
      </div>
    </>
  );
}
