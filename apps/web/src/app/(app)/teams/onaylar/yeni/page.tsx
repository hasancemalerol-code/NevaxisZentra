import { notFound } from "next/navigation";

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

  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, "teams.approvals.create")) {
    notFound();
  }

  const params = await searchParams;
  if (!params.channel || ((!params.message) === (!params.task))) {
    return (
      <PageContent>
        <PageHeader
          description="Mevcut bir kanal mesajı veya AI görev bağlamından onay isteyin."
          eyebrow="Teams"
          title="Onay isteği oluştur"
        />
        <EmptyState
          description="Onay isteği için bir Teams mesajı veya bir AI görevi seçin."
          title="Kaynak bağlam gerekli"
        />
      </PageContent>
    );
  }

  const workspace = await readTeamsWorkspace(params.channel);
  if (workspace.status === "failure" || !workspace.selectedChannel) {
    return (
      <PageContent>
        <PageHeader eyebrow="Teams" title="Onay isteği oluştur" description="Kanal yüklenemedi." />
        <EmptyState
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
        <PageHeader eyebrow="Teams" title="Onay isteği oluştur" description="Mesaj bulunamadı." />
        <EmptyState description="Seçilen mesaja erişiminiz yok." title="Mesaj bulunamadı" />
      </PageContent>
    );
  }

  let sourcePreview = message?.body ?? channel.name;
  if (params.task) {
    const taskResult = await readAiTaskDetail(params.task);
    if (taskResult.status === "failure") {
      return (
        <PageContent>
          <PageHeader eyebrow="Teams" title="Onay isteği oluştur" description="Görev bulunamadı." />
          <EmptyState
            description="Seçilen AI görevine erişiminiz yok."
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
        eyebrow="Teams"
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
