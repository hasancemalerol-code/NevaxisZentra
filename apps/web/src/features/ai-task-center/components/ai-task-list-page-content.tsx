import Link from "next/link";
import { Suspense } from "react";
import {
  BookOpen,
  Plus,
  RefreshCw,
  Workflow,
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
import {
  TASK_CENTER_CREATE_ACTION,
  TASK_CENTER_PRODUCT_DESCRIPTION,
  TASK_CENTER_PRODUCT_NAME,
} from "@/features/ai-task-center/domain/ai-task-product-labels";
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
          description={TASK_CENTER_PRODUCT_DESCRIPTION}
          eyebrow={TASK_CENTER_PRODUCT_NAME}
          title={TASK_CENTER_PRODUCT_NAME}
        />

        <EmptyState
          data-testid="ai-task-center-list-error"
          description={result.message}
          title={`${TASK_CENTER_PRODUCT_NAME} yüklenemedi`}
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
                  <strong>{TASK_CENTER_CREATE_ACTION}</strong>
                  <small>Yeni taslak görev oluştur</small>
                </span>
              </Link>
            ) : null}

            <Link
              className="ai-task-automation-nav-link"
              href="/ai-gorevleri/otomasyon"
            >
              <Workflow aria-hidden="true" />
              <span>Otomasyon</span>
            </Link>

            <Link
              className="ai-task-automation-nav-link"
              href="/ai-gorevleri/playbooklar"
            >
              <BookOpen aria-hidden="true" />
              <span>Playbooklar</span>
            </Link>

            <RefreshButton />
          </>
        }
        description={TASK_CENTER_PRODUCT_DESCRIPTION}
        eyebrow={TASK_CENTER_PRODUCT_NAME}
        title={TASK_CENTER_PRODUCT_NAME}
      />

      {center.canReadTasks ? (
        <Suspense fallback={<AiTaskKpiLoading />}>
          <AiTaskKpiLinks kpis={center.kpis} listPath={listPath} />
        </Suspense>
      ) : null}

      <Section
        action={
          <Badge tone="neutral">
            İnsan onayı
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
            closureQueueCounts={center.closureQueueCounts}
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
