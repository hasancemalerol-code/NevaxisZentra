import Link from "next/link";

import { denyUnlessAnyGranted } from "@/features/admin-access/server/require-current-permission";
import { readExecutiveCommandCenterFilters } from "@/features/executive-command-center/application/executive-command-center-filter";
import { ExecutiveCommandCenterView } from "@/features/executive-command-center/components/executive-command-center-view";
import {
  EXECUTIVE_COMMAND_CENTER_SUBTITLE,
  EXECUTIVE_COMMAND_CENTER_TITLE,
} from "@/features/executive-command-center/domain/executive-command-center-labels";
import { readExecutiveCommandCenterWorkspace } from "@/features/executive-command-center/server/executive-command-center-queries";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

const EXECUTIVE_READ_PERMISSIONS = [
  "factory-case.read",
  "digital-workforce.read",
  "ai-task.read",
  "factory-intelligence.read",
] as const;

type PageSearchParams = Readonly<Record<string, string | string[] | undefined>>;

function toFilterSearchParams(searchParams: PageSearchParams): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (typeof value === "string") params.set(key, value);
  }
  return params;
}

export default async function ExecutiveCommandCenterPage({
  searchParams,
}: Readonly<{
  searchParams: Promise<PageSearchParams>;
}>) {
  const authorization = await denyUnlessAnyGranted(EXECUTIVE_READ_PERMISSIONS);
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">YÖNETİCİ KOMUTA KATMANI</p>
            <h1>{EXECUTIVE_COMMAND_CENTER_TITLE}</h1>
            <p className="commint-executive__subtitle">{EXECUTIVE_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <div role="alert" className="commint-error-banner" data-testid="executive-command-center-forbidden">
          Yönetici Komuta Merkezi görüntüleme yetkiniz yok.
        </div>
        <p>
          <Link href="/dashboard">Dashboard</Link>
        </p>
      </main>
    );
  }

  const resolvedSearchParams = await searchParams;
  const filters = readExecutiveCommandCenterFilters(toFilterSearchParams(resolvedSearchParams));
  const result = await readExecutiveCommandCenterWorkspace(filters);

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">YÖNETİCİ KOMUTA KATMANI</p>
            <h1>{EXECUTIVE_COMMAND_CENTER_TITLE}</h1>
            <p className="commint-executive__subtitle">{EXECUTIVE_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <div role="alert" className="commint-error-banner" data-testid="executive-command-center-error">
          {result.message}
        </div>
      </main>
    );
  }

  return <ExecutiveCommandCenterView workspace={result.workspace} />;
}
