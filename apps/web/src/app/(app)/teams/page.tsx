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
import Link from "next/link";

import { TeamsWorkspaceClient } from "@/features/teams/components/teams-workspace-client";
import "@/features/teams/components/teams-workspace.css";
import { readTeamsWorkspace } from "@/features/teams/server/teams-queries";

export const dynamic = "force-dynamic";

type TeamsPageProps = Readonly<{
  searchParams: Promise<Readonly<{ channel?: string; message?: string }>>;
}>;

export default async function TeamsPage({ searchParams }: TeamsPageProps) {
  await forbidUnlessGranted("teams.read");

  const params = await searchParams;
  const result = await readTeamsWorkspace(params.channel ?? null);

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Operasyon ekipleri arasında kalıcı kanal iletişimi."
          eyebrow="Teams"
          title="Operasyon İletişimi"
        />
        <Section title="Yükleme hatası">
          <EmptyState
            description={result.message}
            title="Teams çalışma alanı yüklenemedi"
          />
        </Section>
      </PageContent>
    );
  }

  const effectiveAccess = await fetchCurrentEffectiveAccess();
  const canWrite =
    result.canWrite && hasGrantedPermission(effectiveAccess, "teams.write");
  const canCreateTask = hasGrantedPermission(effectiveAccess, "ai-task.create");
  const canCreateApproval = hasGrantedPermission(effectiveAccess, "teams.approvals.create");
  const canCreateHandover = hasGrantedPermission(effectiveAccess, "teams.handover.create");
  const canReadHandover = hasGrantedPermission(effectiveAccess, "teams.handover.read");

  return (
    <PageContent>
        <PageHeader
          description="Tesis kapsamlı operasyon kanalları, mesajlar ve görev bağlamı."
          eyebrow="Teams"
          title="Operasyon İletişimi"
          actions={
            <>
              <Link href="/teams/is-kutum" className="teams-context-chip" data-testid="teams-work-inbox-index-link">
                İş kutum
              </Link>
              <Link href="/teams/ozel-mesajlar" className="teams-context-chip" data-testid="teams-direct-messages-index-link">
                Özel mesajlar
              </Link>
              <Link href="/teams/onaylar" className="teams-context-chip" data-testid="teams-approvals-index-link">
                Onay istekleri
              </Link>
              {canReadHandover ? (
                <Link href="/teams/devretme" className="teams-context-chip" data-testid="teams-handovers-index-link">
                  Vardiya devri
                </Link>
              ) : null}
            </>
          }
        />

      <Section title="Çalışma alanı">
        <TeamsWorkspaceClient
          canCreateApproval={canCreateApproval}
          canCreateHandover={canCreateHandover}
          canCreateTask={canCreateTask}
          canWrite={canWrite}
          channels={result.channels}
          selectedChannel={result.selectedChannel}
          selectedChannelId={params.channel ?? result.selectedChannel?.id ?? null}
          selectedMessageId={params.message ?? null}
        />
      </Section>
    </PageContent>
  );
}
