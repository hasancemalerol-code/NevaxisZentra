import Link from "next/link";


import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted, notFoundUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { readAiTaskDetail } from "@/features/ai-task-center/server/ai-task-queries";
import { TeamsApprovalRequestForm } from "@/features/teams/components/teams-approval-request-form";
import { readTeamsWorkspace } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

type SearchParams = Promise<Readonly<{
  channel?: string;
  message?: string;
  task?: string;
}>>;

export default async function TeamsApprovalCreatePage({
  searchParams,
}: Readonly<{ searchParams: SearchParams }>) {
  await forbidUnlessGranted("teams.approvals.create");
  await forbidUnlessGranted("teams.read");

  await notFoundUnlessGranted("teams.approvals.create");

  const params = await searchParams;
  if (!params.channel || ((!params.message) === (!params.task))) {
    return (
      <PageContent>
        <PageHeader
          description="Mevcut bir kanal mesajı veya görev bağlamından onay isteyin."
          eyebrow="Operasyon iletişimi"
          title="Onay isteği oluştur"
        />
        <EmptyState
          action={<div className="teams-empty-actions"><Link className="button button-primary button-medium" href="/teams">Kanallara git</Link><Link className="button button-secondary button-medium" href="/teams/onaylar">Listeye dön</Link></div>}
          description="Onay isteği için bir kanal mesajı veya bir görev seçin."
          title="Kaynak bağlam gerekli"
        />
      </PageContent>
    );
  }

  const workspace = await readTeamsWorkspace(params.channel);
  if (workspace.status === "failure" || !workspace.selectedChannel) {
    return (
      <PageContent>
        <PageHeader eyebrow="Operasyon iletişimi" title="Onay isteği oluştur" description="Kanal yüklenemedi." />
        <EmptyState
          action={<div className="teams-empty-actions"><Link className="button button-primary button-medium" href="/teams">Kanallara git</Link><Link className="button button-secondary button-medium" href="/teams/onaylar">Listeye dön</Link></div>}
          description={workspace.status === "failure" ? workspace.message : "Kanal bulunamadı."}
          title="Kanal erişimi yok"
        />
      </PageContent>
    );
  }

  const channel = workspace.selectedChannel;
  const message = params.message
    ? channel.messages.find((item) => item.id === params.message)
    : null;
  if (params.message && !message) {
    return (
      <PageContent>
        <PageHeader eyebrow="Operasyon iletişimi" title="Onay isteği oluştur" description="Mesaj bulunamadı." />
        <EmptyState action={<div className="teams-empty-actions"><Link className="button button-primary button-medium" href="/teams">Kanallara git</Link><Link className="button button-secondary button-medium" href="/teams/onaylar">Listeye dön</Link></div>} description="Seçilen mesaja erişiminiz yok." title="Mesaj bulunamadı" />
      </PageContent>
    );
  }

  let sourcePreview = message?.body ?? channel.name;
  if (params.task) {
    const taskResult = await readAiTaskDetail(params.task);
    if (taskResult.status === "failure") {
      return (
        <PageContent>
          <PageHeader eyebrow="Operasyon iletişimi" title="Onay isteği oluştur" description="Görev bulunamadı." />
          <EmptyState
          action={<div className="teams-empty-actions"><Link className="button button-primary button-medium" href="/teams">Kanallara git</Link><Link className="button button-secondary button-medium" href="/teams/onaylar">Listeye dön</Link></div>}
            description="Seçilen göreve erişiminiz yok."
            title="Görev bulunamadı"
          />
        </PageContent>
      );
    }

    sourcePreview = taskResult.detail.task.title;
  }

  const reviewerCandidates = channel.members.map((member) => ({
    tenantMembershipId: member.tenantMembershipId,
    displayLabel: member.displayLabel,
  }));

  return (
    <PageContent data-testid="teams-approval-create-page">
      <PageHeader
        description={`${channel.name} kanalından insan kararı isteyin.`}
        eyebrow="Operasyon iletişimi"
        title="Onay isteği oluştur"
      />
      <Section title="İstek">
        <TeamsApprovalRequestForm
          aiTaskId={params.task ?? null}
          channelId={channel.id}
          channelName={channel.name}
          messageId={params.message ?? null}
          reviewerCandidates={reviewerCandidates}
          sourcePreview={sourcePreview}
        />
      </Section>
    </PageContent>
  );
}
