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
import { TeamsDirectMessagesClient } from "@/features/teams/components/teams-direct-messages-client";
import "@/features/teams/components/teams-workspace.css";
import {
  readTeamsDirectConversation,
  readTeamsDirectConversations,
  readTeamsDirectRecipients,
} from "@/features/teams/server/teams-queries";
import { FailureStatePage } from "@/components/ui/failure-state";

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
          eyebrow="Operasyon iletişimi"
          title="Özel mesajlar"
        />
        <FailureStatePage code={"code" in conversationsResult ? conversationsResult.code : undefined} message={conversationsResult.message} failedTitle="Konuşmalar yüklenemedi" inline />
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
          eyebrow="Operasyon iletişimi"
          title="Özel mesajlar"
        />
        <FailureStatePage code={"code" in selectedConversationResult ? selectedConversationResult.code : undefined} message={selectedConversationResult.message} failedTitle="Konuşma yüklenemedi" inline />
      </PageContent>
    );
  }

  return (
    <PageContent>
      <PageHeader
        description="Kalıcı, salt metin bire bir operasyon mesajlaşması."
        eyebrow="Operasyon iletişimi"
        title="Özel mesajlar"
      />
      <Section title="Bire bir mesajlaşma">
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
