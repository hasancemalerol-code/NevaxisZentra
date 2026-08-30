import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import {
  TEAMS_HANDOVER_DISCLAIMER,
  TEAMS_HANDOVER_SHIFT_TYPES,
  TEAMS_HANDOVER_STATUSES,
} from "@/features/teams/domain/teams-constants";
import { readTeamsHandovers } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

function statusLabel(status: string): string {
  return TEAMS_HANDOVER_STATUSES[status as keyof typeof TEAMS_HANDOVER_STATUSES] ?? status;
}

function shiftLabel(shiftType: string): string {
  return TEAMS_HANDOVER_SHIFT_TYPES[shiftType as keyof typeof TEAMS_HANDOVER_SHIFT_TYPES] ?? shiftType;
}

export default async function TeamsHandoversPage() {
  await forbidUnlessGranted("teams.handover.read");
  const result = await readTeamsHandovers();
  const access = await fetchCurrentEffectiveAccess();
  const canCreate = hasGrantedPermission(access, "teams.handover.create");

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Sonraki vardiyaya insan kontrollü operasyon bağlamı."
          eyebrow="Teams"
          title="Vardiya devri"
        />
        <EmptyState description={result.message} title="Vardiya devirleri yüklenemedi" />
      </PageContent>
    );
  }

  return (
    <PageContent data-testid="teams-handovers-page">
      <PageHeader
        description="Kanal kapsamlı, yayımlanınca salt okunur vardiya devir kayıtları."
        eyebrow="Teams"
        title="Vardiya devri"
        actions={canCreate ? (
          <Link href="/teams/devretme/yeni" className="teams-context-chip" data-testid="teams-handover-create-link">
            Yeni vardiya devri
          </Link>
        ) : undefined}
      />

      <Section title="Devirler">
        {result.value.items.length === 0 ? (
          <EmptyState
            description="Görüntüleyebileceğiniz vardiya devri yok."
            title="Vardiya devri yok"
          />
        ) : (
          <ul className="teams-approval-list" data-testid="teams-handovers-list">
            {result.value.items.map((item) => (
              <li key={item.id}>
                <Link href={`/teams/devretme/${item.id}`} data-testid={`teams-handover-link-${item.id}`}>
                  <strong>{item.shiftDate} · {shiftLabel(item.shiftType)}</strong>
                  <span>{item.channelName}</span>
                  <span>{item.authorLabel}</span>
                  <Badge tone="neutral">{statusLabel(item.status)}</Badge>
                  <span>{item.itemCount} madde</span>
                  <span>{item.acknowledgementCount} teslim</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
        <p className="teams-disclaimer">{TEAMS_HANDOVER_DISCLAIMER}</p>
      </Section>
    </PageContent>
  );
}
