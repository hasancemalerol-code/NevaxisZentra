import Link from "next/link";
import { Suspense } from "react";
import {
  Plus,
  RefreshCw,
  Sparkles,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskKpiLinks } from "@/features/ai-task-center/components/ai-task-kpi-links";
import { AiTaskKpiLoading } from "@/features/ai-task-center/components/ai-task-kpi-loading";
import { AiTaskWorkspace } from "@/features/ai-task-center/components/ai-task-workspace";
import { AiTaskWorkspaceLoading } from "@/features/ai-task-center/components/ai-task-workspace-loading";
import { refreshAiTaskWorkspaceAction } from "@/features/ai-task-center/server/ai-task-actions";
import { readAiTaskWorkspace } from "@/features/ai-task-center/server/ai-task-queries";

type AiTaskListPageContentProps = Readonly<{
  listPath?: string;
  showCreateAction?: boolean;
}>;

async function RefreshButton() {
  async function refresh() {
    "use server";
    await refreshAiTaskWorkspaceAction();
  }

  return (
    <form action={refresh}>
      <Button leadingIcon={<RefreshCw />} type="submit" variant="secondary">
        Veriyi yenile
      </Button>
    </form>
  );
}

export async function AiTaskListPageContent({
  listPath = "/ai-gorevleri",
  showCreateAction = true,
}: AiTaskListPageContentProps) {
  const result = await readAiTaskWorkspace({ page: 1, pageSize: 100 });

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Üretim, kalite, bakım, malzeme, ticari zekâ ve ERP entegrasyonlarından oluşan AI önerilerini tek merkezden inceleyin."
          eyebrow="AI Görev Merkezi"
          title="Operasyon karar merkezi"
        />

        <EmptyState
          data-testid="ai-task-center-list-error"
          description={result.message}
          title="AI görevleri yüklenemedi"
        />
      </PageContent>
    );
  }

  const { workspace } = result;

  return (
    <PageContent data-testid="ai-task-center-list">
      <PageHeader
        actions={
          <>
            {showCreateAction && workspace.canCreate ? (
              <Link
                className="ai-task-create-button"
                href="/ai-gorevleri/yeni"
              >
                <Plus aria-hidden="true" />
                <span>
                  <strong>AI&apos;ya Görev Ver</strong>
                  <small>Yeni taslak görev oluştur</small>
                </span>
              </Link>
            ) : null}

            <RefreshButton />
          </>
        }
        description="Üretim, kalite, bakım, malzeme, ticari zekâ ve ERP entegrasyonlarından oluşan AI önerilerini tek merkezden inceleyin."
        eyebrow="AI Görev Merkezi"
        title="Operasyon karar merkezi"
      />

      <Suspense fallback={<AiTaskKpiLoading />}>
        <AiTaskKpiLinks kpis={workspace.kpis} />
      </Suspense>

      <Section
        action={
          <Badge tone="ai">
            <Sparkles aria-hidden="true" />
            Human-in-the-Loop
          </Badge>
        }
        description="Görevleri arayın, filtreleyin ve risk, öncelik ve gecikme durumlarına göre inceleyin."
        title="AI görev çalışma alanı"
      >
        {workspace.tasks.length === 0 ? (
          <EmptyState
            action={
              workspace.canCreate ? (
                <Link href="/ai-gorevleri/yeni">
                  İlk taslak görevi oluştur
                </Link>
              ) : (
                <Link href={listPath}>
                  Listeye dön
                </Link>
              )
            }
            description="Henüz kayıtlı AI görevi bulunmuyor. Yetkili kullanıcılar yeni taslak görev oluşturabilir."
            title="Görev listesi boş"
          />
        ) : (
          <Suspense fallback={<AiTaskWorkspaceLoading />}>
            <AiTaskWorkspace tasks={workspace.tasks} />
          </Suspense>
        )}
      </Section>
    </PageContent>
  );
}
