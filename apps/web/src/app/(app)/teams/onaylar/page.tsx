import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TEAMS_APPROVAL_DISCLAIMER, TEAMS_APPROVAL_STATUSES } from "@/features/teams/domain/teams-constants";
import { readTeamsApprovals } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

function statusLabel(status: string): string {
  return TEAMS_APPROVAL_STATUSES[status as keyof typeof TEAMS_APPROVAL_STATUSES] ?? status;
}

export default async function TeamsApprovalsPage() {
  await forbidUnlessGranted("teams.read");
  const result = await readTeamsApprovals();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="İnsan kontrollü operasyon onay istekleri."
          eyebrow="Teams"
          title="Onay istekleri"
        />
        <EmptyState description={result.message} title="Onay istekleri yüklenemedi" />
      </PageContent>
    );
  }

  return (
    <PageContent data-testid="teams-approvals-page">
      <PageHeader
        description="Kanal veya görev bağlamından oluşturulan insan karar kayıtları."
        eyebrow="Teams"
        title="Onay istekleri"
      />

      <Section title="İstekler">
        {result.value.items.length === 0 ? (
          <EmptyState
            description="Görüntüleyebileceğiniz onay isteği yok."
            title="Onay isteği yok"
          />
        ) : (
          <ul className="teams-approval-list" data-testid="teams-approvals-list">
            {result.value.items.map((item) => (
              <li key={item.id}>
                <Link href={`/teams/onaylar/${item.id}`} data-testid={`teams-approval-link-${item.id}`}>
                  <strong>{item.title}</strong>
                  <span>{item.channelName}</span>
                  <Badge tone="neutral">{statusLabel(item.status)}</Badge>
                </Link>
              </li>
            ))}
          </ul>
        )}
        <p className="teams-disclaimer">{TEAMS_APPROVAL_DISCLAIMER}</p>
      </Section>
    </PageContent>
  );
}
