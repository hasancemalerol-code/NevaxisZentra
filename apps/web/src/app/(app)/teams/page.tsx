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

  return (
    <PageContent>
      <PageHeader
        description="Tesis kapsamlı operasyon kanalları, mesajlar ve görev bağlamı."
        eyebrow="Teams"
        title="Operasyon İletişimi"
      />

      <Section title="Çalışma alanı">
        <TeamsWorkspaceClient
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
