import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TeamsDirectMessagesClient } from "@/features/teams/components/teams-direct-messages-client";
import "@/features/teams/components/teams-workspace.css";
import {
  readTeamsDirectConversation,
  readTeamsDirectConversations,
  readTeamsDirectRecipients,
} from "@/features/teams/server/teams-queries";

export const dynamic = "force-dynamic";

type TeamsDirectMessagesPageProps = Readonly<{
  searchParams: Promise<Readonly<{ conversation?: string }>>;
}>;

export default async function TeamsDirectMessagesPage({ searchParams }: TeamsDirectMessagesPageProps) {
  await forbidUnlessGranted("teams.direct.read");

  const params = await searchParams;
  const conversationsResult = await readTeamsDirectConversations();
  if (conversationsResult.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Aynı tesis kapsamındaki üyelerle kalıcı bire bir mesajlaşma."
          eyebrow="Teams"
          title="Özel mesajlar"
        />
        <EmptyState description={conversationsResult.message} title="Konuşmalar yüklenemedi" />
      </PageContent>
    );
  }

  const effectiveAccess = await fetchEffectiveAccessCapabilities();
  const canWrite = hasGrantedPermission(effectiveAccess, "teams.direct.write");
  const recipientsResult = canWrite
    ? await readTeamsDirectRecipients()
    : { status: "success" as const, value: { items: [] } };

  const selectedConversationId = params.conversation ?? conversationsResult.value.items[0]?.id ?? null;
  const selectedConversationResult = selectedConversationId
    ? await readTeamsDirectConversation(selectedConversationId)
    : null;

  if (selectedConversationResult?.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Aynı tesis kapsamındaki üyelerle kalıcı bire bir mesajlaşma."
          eyebrow="Teams"
          title="Özel mesajlar"
        />
        <EmptyState description={selectedConversationResult.message} title="Konuşma yüklenemedi" />
      </PageContent>
    );
  }

  return (
    <PageContent>
      <PageHeader
        description="Kalıcı, salt metin bire bir operasyon mesajlaşması."
        eyebrow="Teams"
        title="Özel mesajlar"
      />
      <Section title="Konuşmalar">
        <TeamsDirectMessagesClient
          canWrite={canWrite && conversationsResult.value.capabilities.canWrite}
          conversations={conversationsResult.value}
          recipients={recipientsResult.status === "success" ? recipientsResult.value : { items: [] }}
          selectedConversation={selectedConversationResult?.status === "success" ? selectedConversationResult.value : null}
          selectedConversationId={selectedConversationId}
        />
      </Section>
    </PageContent>
  );
}
