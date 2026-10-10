
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted, notFoundUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { readAiTaskDetail } from "@/features/ai-task-center/server/ai-task-queries";
import { TeamsHandoverForm } from "@/features/teams/components/teams-handover-form";
import { isHandoverChannelType } from "@/features/teams/domain/teams-constants";
import { readTeamsWorkspace } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

type SearchParams = Promise<Readonly<{
  channel?: string;
  message?: string;
  task?: string;
}>>;

export default async function TeamsHandoverCreatePage({
  searchParams,
}: Readonly<{ searchParams: SearchParams }>) {
  await forbidUnlessGranted("teams.handover.create");
  await forbidUnlessGranted("teams.handover.read");

  await notFoundUnlessGranted("teams.handover.create");

  const params = await searchParams;
  const workspace = await readTeamsWorkspace(params.channel ?? null);
  if (workspace.status === "failure") {
    return (
      <PageContent>
        <PageHeader eyebrow="Teams" title="Vardiya devri oluştur" description="Kanal yüklenemedi." />
        <EmptyState description={workspace.message} title="Kanal erişimi yok" />
      </PageContent>
    );
  }

  const eligibleChannels = workspace.channels.items.filter((channel) =>
    isHandoverChannelType(channel.channelType));
  if (eligibleChannels.length === 0) {
    return (
      <PageContent>
        <PageHeader
          description="Vardiya devri yalnızca operasyon veya departman kanalında oluşturulur."
          eyebrow="Teams"
          title="Vardiya devri oluştur"
        />
        <EmptyState
          description="Erişilebilir operasyon veya departman kanalı yok."
          title="Uygun kanal yok"
        />
      </PageContent>
    );
  }

  if (params.channel) {
    const selected = eligibleChannels.find((channel) => channel.id === params.channel);
    if (!selected) {
      return (
        <PageContent>
          <PageHeader eyebrow="Teams" title="Vardiya devri oluştur" description="Kanal uygun değil." />
          <EmptyState
            description="Vardiya devri yalnızca erişilebilir bir operasyon veya departman kanalında oluşturulabilir."
            title="Kanal uygun değil"
          />
        </PageContent>
      );
    }
  }

  const channel = workspace.selectedChannel;
  const message = params.message && channel
    ? channel.messages.find((item) => item.id === params.message)
    : null;
  if (params.message && !message) {
    return (
      <PageContent>
        <PageHeader eyebrow="Teams" title="Vardiya devri oluştur" description="Mesaj bulunamadı." />
        <EmptyState description="Seçilen mesaja erişiminiz yok." title="Mesaj bulunamadı" />
      </PageContent>
    );
  }

  if (params.task) {
    const taskResult = await readAiTaskDetail(params.task);
    if (taskResult.status === "failure") {
      return (
        <PageContent>
          <PageHeader eyebrow="Teams" title="Vardiya devri oluştur" description="Görev bulunamadı." />
          <EmptyState
            description="Seçilen AI görevine erişiminiz yok."
            title="Görev bulunamadı"
          />
        </PageContent>
      );
    }
  }

  const initialItems = params.message || params.task
    ? [{
        itemKind: "attention",
        title: message?.body.slice(0, 80) ?? "Bağlam maddesi",
        body: message?.body ?? "Mevcut kayıttan aktarılan operasyon notu.",
        linkKind: params.message ? "message" as const : "task" as const,
        messageId: params.message ?? "",
        aiTaskId: params.task ?? "",
      }]
    : undefined;

  return (
    <PageContent data-testid="teams-handover-create-page">
      <PageHeader
        description="Sonraki vardiya için yapılandırılmış operasyon bağlamı kaydedin."
        eyebrow="Teams"
        title="Vardiya devri oluştur"
      />
      <Section title="Devir">
        <TeamsHandoverForm
          channels={eligibleChannels.map((item) => ({ id: item.id, name: item.name }))}
          initialItems={initialItems}
          initialSummary={message?.body.slice(0, 200) ?? ""}
          lockedChannelId={params.channel ?? null}
          mode="create"
        />
      </Section>
    </PageContent>
  );
}
