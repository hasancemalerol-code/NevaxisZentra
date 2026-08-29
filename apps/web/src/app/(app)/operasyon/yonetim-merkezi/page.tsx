import { ShieldAlert } from "lucide-react";

import { denyUnlessAnyGranted } from "@/features/admin-access/server/require-current-permission";
import { OperationsCommandCenterView } from "@/features/operations-command-center/components/operations-command-center-view";
import {
  readOperationsCommandCenterFilters,
} from "@/features/operations-command-center/application/operations-command-center-filter";
import { OPERATIONS_COMMAND_CENTER_SUBTITLE } from "@/features/operations-command-center/domain/operations-command-center-labels";
import { readOperationsCommandCenterWorkspace } from "@/features/operations-command-center/server/operations-command-center-queries";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

const MODULE_READ_PERMISSIONS = [
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
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>Operasyon Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">{OPERATIONS_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <div
          role="alert"
          className="commint-error-banner"
          data-testid="operations-command-center-forbidden"
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Bu merkeze erişim yetkiniz yok</h3>
            <p>{authorization.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const resolvedSearchParams = await searchParams;
  const filterParams = toFilterSearchParams(resolvedSearchParams);
  const uiFilters = readOperationsCommandCenterFilters(filterParams);

  const result = await readOperationsCommandCenterWorkspace(uiFilters);

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>Operasyon Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">{OPERATIONS_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <div
          role="alert"
          className="commint-partial-error"
          data-testid="operations-command-center-server-error"
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Operasyon merkezi verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <OperationsCommandCenterView workspace={result.workspace} />;
}
