import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { notFound } from "next/navigation";

import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskPlaybookBuilderClient } from "@/features/ai-task-center/components/ai-task-playbook-builder-client";
import { readAiTaskPlaybookBuilderContext } from "@/features/ai-task-center/server/ai-task-playbook-queries";

type AiTaskPlaybookBuilderPageProps = Readonly<{
  params: Promise<Readonly<{ playbookId: string }>>;
}>;

export default async function AiTaskPlaybookBuilderPage({
  params,
}: AiTaskPlaybookBuilderPageProps) {
  const { playbookId } = await params;
  if (playbookId === "calisma") {
    notFound();
  }
  const isCreate = playbookId === "yeni";
  const result = await readAiTaskPlaybookBuilderContext(playbookId);

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Playbook tanımını düzenleyin"
          eyebrow="AI Görev Merkezi"
          title={isCreate ? "Yeni Playbook" : "Playbook Düzenle"}
        />
        <EmptyState
          data-testid="ai-task-playbook-builder-error"
          description={isNotFoundFailureCode(result.code) ? "İstenen playbook kaydı bulunamadı." : result.message}
          title={isNotFoundFailureCode(result.code) ? "Playbook bulunamadı" : "Playbook yüklenemedi"}
        />
      </PageContent>
    );
  }

  if (!isCreate && !result.playbook) {
    return (
      <PageContent>
        <PageHeader
          description="Playbook tanımını düzenleyin"
          eyebrow="AI Görev Merkezi"
          title="Playbook Düzenle"
        />
        <EmptyState
          data-testid="ai-task-playbook-builder-error"
          description="İstenen playbook kaydı bulunamadı."
          title="Playbook bulunamadı"
        />
      </PageContent>
    );
  }

  return (
    <PageContent data-testid="ai-task-playbook-builder-page">
      <PageHeader
        actions={
          <Link className="ai-task-playbook-back-link" href="/ai-gorevleri/playbooklar">
            <ArrowLeft aria-hidden="true" />
            <span>Playbook kütüphanesine dön</span>
          </Link>
        }
        description={
          isCreate
            ? "Yeni operasyonel playbook tanımı oluşturun."
            : `${result.playbook?.name ?? "Playbook"} tanımını düzenleyin.`
        }
        eyebrow="AI Görev Merkezi"
        title={isCreate ? "Yeni Playbook" : "Playbook Düzenle"}
      />

      <Section
        description="Adımları, bağımlılıkları ve koşulları yapılandırın."
        title="Playbook oluşturucu"
      >
        <AiTaskPlaybookBuilderClient
          capabilities={result.capabilities}
          initialPlaybook={result.playbook}
          mode={isCreate ? "create" : "edit"}
        />
      </Section>
    </PageContent>
  );
}
