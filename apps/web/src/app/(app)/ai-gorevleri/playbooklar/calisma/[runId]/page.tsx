import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";

import { AiTaskPlaybookRunDetailClient } from "@/features/ai-task-center/components/ai-task-playbook-run-detail-client";
import { buildAiTaskPlaybookLibraryHref } from "@/features/ai-task-center/presentation/ai-task-playbook-library-navigation";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readAiTaskPlaybookRunDetail } from "@/features/ai-task-center/server/ai-task-playbook-queries";
import { FailureStatePage } from "@/components/ui/failure-state";

type AiTaskPlaybookRunPageProps = Readonly<{
  params: Promise<Readonly<{ runId: string }>>;
}>;

export default async function AiTaskPlaybookRunPage({
  params,
}: AiTaskPlaybookRunPageProps) {
  const { runId } = await params;
  const result = await readAiTaskPlaybookRunDetail(runId);

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          eyebrow="Görev Merkezi"
          title="Çalışma Detayı"
        />
        <div data-testid="ai-task-playbook-run-detail-error">
          <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Playbook çalışması yüklenemedi" entityLabel="playbook çalışması" backHref="/ai-gorevleri/playbooklar" backLabel="Playbook listesine dön" inline />
        </div>
      </PageContent>
    );
  }

  const access = await fetchEffectiveAccessCapabilities();
  const capabilities = {
    canRead: access ? hasGrantedPermission(access, "ai-task.playbook.read") : false,
    canManage: access ? hasGrantedPermission(access, "ai-task.playbook.manage") : false,
    canRun: access ? hasGrantedPermission(access, "ai-task.playbook.run") : false,
    canOverride: access ? hasGrantedPermission(access, "ai-task.playbook.override") : false,
  };

  return (
    <PageContent data-testid="ai-task-playbook-run-page">
      <PageHeader
        actions={
          <Link className="ai-task-playbook-back-link" href={buildAiTaskPlaybookLibraryHref("active")}>
            <ArrowLeft aria-hidden="true" />
            <span>Çalışan akışlara dön</span>
          </Link>
        }
        description={`${result.run.playbookName} çalışmasının adım durumları ve ilerlemesi.`}
        eyebrow="Görev Merkezi"
        title="Çalışma Detayı"
      />

      <Section
        description="Adım akışını izleyin ve gerekirse müdahale edin."
        title="Çalışma özeti"
      >
        <AiTaskPlaybookRunDetailClient
          capabilities={capabilities}
          run={result.run}
        />
      </Section>
    </PageContent>
  );
}
