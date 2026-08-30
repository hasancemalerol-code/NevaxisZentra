import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TeamsNotificationsClient } from "@/features/teams/components/teams-notifications-client";
import "@/features/teams/components/teams-workspace.css";
import { readTeamsNotifications } from "@/features/teams/server/teams-queries";

export const dynamic = "force-dynamic";

export default async function TeamsNotificationsPage() {
  await forbidUnlessGranted("teams.notifications.read");

  const result = await readTeamsNotifications();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Teams operasyon bildirimlerinizin kalıcı in-app görünümü."
          eyebrow="Teams"
          title="Bildirim merkezi"
        />
        <EmptyState description={result.message} title="Bildirimler yüklenemedi" />
      </PageContent>
    );
  }

  return (
    <PageContent data-testid="teams-notifications-page">
      <PageHeader
        description={`${result.value.unreadCount} okunmamış bildirim.`}
        eyebrow="Teams"
        title="Bildirim merkezi"
      />
      <Section title="Bildirimler">
        {result.value.items.length === 0 ? (
          <EmptyState
            description="Yeni Teams olayları oluştuğunda burada görünür."
            title="Bildirim yok"
          />
        ) : (
          <TeamsNotificationsClient
            disclaimer={result.value.disclaimer}
            items={result.value.items}
          />
        )}
      </Section>
    </PageContent>
  );
}
