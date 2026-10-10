import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskPlaybookRunDetailClient } from "@/features/ai-task-center/components/ai-task-playbook-run-detail-client";
import { buildAiTaskPlaybookLibraryHref } from "@/features/ai-task-center/presentation/ai-task-playbook-library-navigation";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readAiTaskPlaybookRunDetail } from "@/features/ai-task-center/server/ai-task-playbook-queries";

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
          description="Playbook çalışması adım akışı"
          eyebrow="AI Görev Merkezi"
          title="Çalışma Detayı"
        />
        <EmptyState
          data-testid="ai-task-playbook-run-detail-error"
          description={result.message}
          title="Playbook çalışması bulunamadı"
        />
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
        eyebrow="AI Görev Merkezi"
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
