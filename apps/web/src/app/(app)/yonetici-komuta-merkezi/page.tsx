import { denyUnlessAnyGranted } from "@/features/admin-access/server/require-current-permission";
import { readExecutiveCommandCenterFilters } from "@/features/executive-command-center/application/executive-command-center-filter";
import { ExecutiveCommandCenterView } from "@/features/executive-command-center/components/executive-command-center-view";
import {
  EXECUTIVE_COMMAND_CENTER_SUBTITLE,
  EXECUTIVE_COMMAND_CENTER_TITLE,
} from "@/features/executive-command-center/domain/executive-command-center-labels";
import { readExecutiveCommandCenterWorkspace } from "@/features/executive-command-center/server/executive-command-center-queries";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { FailureStatePage } from "@/components/ui/failure-state";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

const EXECUTIVE_READ_PERMISSIONS = [
  "factory-case.read",
  "ai-task.read",
  "factory-intelligence.read",
  "throughput-optimizer.read",
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
      <StatePage
        kind={authorization.code === "authorization-denied" ? "forbidden" : "unavailable"}
        title={authorization.code === "authorization-denied" ? "Yönetici Komuta Merkezi’ne erişim yetkiniz yok" : "Yetki doğrulanamadı"}
        description={
          authorization.code === "authorization-denied"
            ? "Hesabınızın rolü bu merkezi görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
            : "Yetki doğrulaması şu anda yapılamıyor. Lütfen biraz sonra yeniden deneyin."
        }
        actions={<StatePageLink href="/dashboard" primary>Ana sayfaya dön</StatePageLink>}
      />
    );
  }

  const resolvedSearchParams = await searchParams;
  const filters = readExecutiveCommandCenterFilters(toFilterSearchParams(resolvedSearchParams));
  const result = await readExecutiveCommandCenterWorkspace(filters);

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">YÖNETİCİ KOMUTA KATMANI</p>
            <h1>{EXECUTIVE_COMMAND_CENTER_TITLE}</h1>
            <p className="commint-executive__subtitle">{EXECUTIVE_COMMAND_CENTER_SUBTITLE}</p>
          </div>
        </header>
        <FailureStatePage
          code={"code" in result ? result.code : undefined}
          message={result.message}
          failedTitle="Yönetici özeti şu anda yüklenemedi"
          backHref="/dashboard"
          backLabel="Ana sayfaya dön"
          inline
        />
      </div>
    );
  }

  return <ExecutiveCommandCenterView workspace={result.workspace} />;
}
