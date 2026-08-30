import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import {
  TEAMS_WORK_INBOX_DISCLAIMER,
  TEAMS_WORK_INBOX_SOURCE_KINDS,
} from "@/features/teams/domain/teams-constants";
import { readTeamsWorkInbox } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

function sourceLabel(sourceKind: string): string {
  return TEAMS_WORK_INBOX_SOURCE_KINDS[sourceKind as keyof typeof TEAMS_WORK_INBOX_SOURCE_KINDS] ?? sourceKind;
}

export default async function TeamsWorkInboxPage() {
  await forbidUnlessGranted("teams.read");
  const result = await readTeamsWorkInbox();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Mevcut kaynaklardaki size ait işlerin salt okunur görünümü."
          eyebrow="Teams"
          title="İş kutum"
        />
        <EmptyState description={result.message} title="İş kutusu yüklenemedi" />
      </PageContent>
    );
  }

  return (
    <PageContent data-testid="teams-work-inbox-page">
      <PageHeader
        description="AI görevleri, onay istekleri, vardiya devirleri ve etiketler — kaynak sistemler yetkilidir."
        eyebrow="Teams"
        title="İş kutum"
      />

      <Section title="Bekleyen işler">
        {result.value.items.length === 0 ? (
          <EmptyState
            description="Bu oturumda görüntüleyebileceğiniz bekleyen iş yok."
            title="İş öğesi yok"
          />
        ) : (
          <ul className="teams-approval-list" data-testid="teams-work-inbox-list">
            {result.value.items.map((item) => (
              <li key={`${item.sourceKind}:${item.sourceId}`}>
                <Link
                  href={item.deepLinkPath}
                  data-testid={`teams-work-inbox-item-${item.sourceKind}-${item.sourceId}`}
                >
                  <strong>{item.title}</strong>
                  <span>{item.reason}</span>
                  <Badge tone="neutral">{sourceLabel(item.sourceKind)}</Badge>
                </Link>
              </li>
            ))}
          </ul>
        )}
        <p className="teams-disclaimer" data-testid="teams-work-inbox-disclaimer">
          {TEAMS_WORK_INBOX_DISCLAIMER}
        </p>
      </Section>
    </PageContent>
  );
}
