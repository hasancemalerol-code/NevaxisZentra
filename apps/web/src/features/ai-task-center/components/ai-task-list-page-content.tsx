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
import { AiTaskCenterUnifiedWorkspace } from "@/features/ai-task-center/components/ai-task-center-unified-workspace";
import { AiTaskKpiLinks } from "@/features/ai-task-center/components/ai-task-kpi-links";
import { AiTaskKpiLoading } from "@/features/ai-task-center/components/ai-task-kpi-loading";
import {
  parseAiTaskCenterTaskOriginFilter,
  parseAiTaskCenterView,
} from "@/features/ai-task-center/domain/ai-task-center-item";
import { refreshAiTaskWorkspaceAction } from "@/features/ai-task-center/server/ai-task-actions";
import { readAiTaskCenterUnified } from "@/features/ai-task-center/server/ai-task-center-queries";

type AiTaskListPageContentProps = Readonly<{
  listPath?: string;
  showCreateAction?: boolean;
  initialView?: string;
  initialOriginFilter?: string;
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
  initialView,
  initialOriginFilter,
}: AiTaskListPageContentProps) {
  const result = await readAiTaskCenterUnified();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="AI destekli birleşik görev ve aksiyon yönetimi"
          eyebrow="AI Görev Merkezi"
          title="AI Görev Merkezi"
        />

        <EmptyState
          data-testid="ai-task-center-list-error"
          description={result.message}
          title="AI Görev Merkezi yüklenemedi"
        />
      </PageContent>
    );
  }

  const { center } = result;

  return (
    <PageContent data-testid="ai-task-center-list">
      <PageHeader
        actions={
          <>
            {showCreateAction && center.canCreate ? (
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
        description="AI destekli birleşik görev ve aksiyon yönetimi"
        eyebrow="AI Görev Merkezi"
        title="AI Görev Merkezi"
      />

      {center.canReadTasks ? (
        <Suspense fallback={<AiTaskKpiLoading />}>
          <AiTaskKpiLinks kpis={center.kpis} />
        </Suspense>
      ) : null}

      <Section
        action={
          <Badge tone="ai">
            <Sparkles aria-hidden="true" />
            Human-in-the-Loop
          </Badge>
        }
        description="Görevleri, onayları, vardiya teslimlerini ve etiketlemeleri tek merkezden inceleyin."
        title="Birleşik çalışma alanı"
      >
        <Suspense>
          <AiTaskCenterUnifiedWorkspace
            assignedTaskIds={center.assignedTaskIds}
            initialOriginFilter={parseAiTaskCenterTaskOriginFilter(initialOriginFilter)}
            initialView={parseAiTaskCenterView(initialView)}
            items={center.items}
            listPath={listPath}
          />
        </Suspense>

        {center.workInboxDisclaimer ? (
          <p className="ai-task-center-unified__disclaimer" data-testid="ai-task-center-disclaimer">
            {center.workInboxDisclaimer}
          </p>
        ) : null}
      </Section>
    </PageContent>
  );
}
