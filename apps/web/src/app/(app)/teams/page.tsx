import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";

import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";

import { TeamsWorkspaceClient } from "@/features/teams/components/teams-workspace-client";
import "@/features/teams/components/teams-workspace.css";
import { readTeamsWorkspace } from "@/features/teams/server/teams-queries";
import { FailureStatePage } from "@/components/ui/failure-state";

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
          eyebrow="Operasyon iletişimi"
          title="Operasyon İletişimi"
        />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Operasyon iletişimi çalışma alanı yüklenemedi" backHref="/dashboard" backLabel="Ana sayfaya dön" inline />
      </PageContent>
    );
  }

  const effectiveAccess = await fetchEffectiveAccessCapabilities();
  const canWrite =
    result.canWrite && hasGrantedPermission(effectiveAccess, "teams.write");
  const canCreateTask = hasGrantedPermission(effectiveAccess, "ai-task.create");
  const canCreateApproval = hasGrantedPermission(effectiveAccess, "teams.approvals.create");
  const canCreateHandover = hasGrantedPermission(effectiveAccess, "teams.handover.create");
  return (
    <PageContent>
        <PageHeader
          description="Tesis kapsamlı operasyon kanalları, mesajlar ve görev bağlamı."
          eyebrow="Operasyon iletişimi"
          title="Operasyon İletişimi"
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
