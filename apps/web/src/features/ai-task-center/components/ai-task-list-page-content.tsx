import Link from "next/link";
import { Suspense, type ReactNode } from "react";
import {
  BookOpen,
  Plus,
  RefreshCw,
  Workflow,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";

import { AiTaskCenterUnifiedWorkspace } from "@/features/ai-task-center/components/ai-task-center-unified-workspace";
import { AiTaskKpiLinks, type AiTaskExtraKpi } from "@/features/ai-task-center/components/ai-task-kpi-links";
import { deriveAiTaskCenterKpis } from "@/features/ai-task-center/application/ai-task-center-kpis";
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
import { FailureStatePage } from "@/components/ui/failure-state";

import "./ai-task-list-page-content.css";
import "@/features/ai-task-center/components/ai-task-center-round3.css";

type AiTaskListPageContentProps = Readonly<{
  listPath?: string;
  showCreateAction?: boolean;
  /** True when rendered inside another screen (the Today page) that already owns the page h1. */
  embedded?: boolean;
  initialView?: string;
  initialOriginFilter?: string;
  /** Hosts that merge this screen with their own summary (the Today page) add actions, KPIs and sections here. */
  extraActions?: ReactNode;
  extraKpis?: readonly AiTaskExtraKpi[];
  beforeWorkspace?: ReactNode;
  /** Optional header overrides for hosts (for example the Teams work inbox). */
  eyebrow?: string;
  title?: string;
  description?: string;
}>;

async function RefreshButton() {
  async function refresh() {
    "use server";
    await refreshAiTaskWorkspaceAction();
  }

  return (
    <form action={refresh} className="zx-inline-form">
      <Button leadingIcon={<RefreshCw />} type="submit" variant="secondary">
        Veriyi yenile
      </Button>
    </form>
  );
}

export async function AiTaskListPageContent({
  listPath = "/ai-gorevleri",
  showCreateAction = true,
  embedded = false,
  initialView,
  initialOriginFilter,
  extraActions,
  extraKpis,
  beforeWorkspace,
  eyebrow = TASK_CENTER_PRODUCT_NAME,
  title = TASK_CENTER_PRODUCT_NAME,
  description = TASK_CENTER_PRODUCT_DESCRIPTION,
}: AiTaskListPageContentProps) {
  const result = await readAiTaskCenterUnified();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          headingLevel={embedded ? 2 : 1}
          description={description}
          eyebrow={eyebrow}
          title={title}
        />

        {beforeWorkspace}

        <div data-testid="ai-task-center-list-error">
          <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle={`${TASK_CENTER_PRODUCT_NAME} şu anda alınamıyor`} inline />
        </div>
      </PageContent>
    );
  }

  const { center } = result;

  return (
    <PageContent data-list-path={listPath} data-testid="ai-task-center-list">
      <PageHeader
        headingLevel={embedded ? 2 : 1}
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
              <span>Operasyon rehberleri</span>
            </Link>

            {extraActions}

            <RefreshButton />
          </>
        }
        description={description}
        eyebrow={eyebrow}
        title={title}
      />

      {center.canReadTasks ? (
        <Suspense fallback={<AiTaskKpiLoading />}>
          <AiTaskKpiLinks extraKpis={extraKpis} kpis={deriveAiTaskCenterKpis(center.items, center.kpis)} listPath={listPath} />
        </Suspense>
      ) : null}

      {beforeWorkspace}

      <Section
        description="Görevleri, onayları, vardiya teslimlerini ve etiketlemeleri tek merkezden inceleyin."
        title="Birleşik çalışma alanı"
      >
        <Suspense>
          <AiTaskCenterUnifiedWorkspace
            assignedTaskIds={center.assignedTaskIds}
            initialOriginFilter={parseAiTaskCenterTaskOriginFilter(initialOriginFilter)}
            initialView={initialView ? parseAiTaskCenterView(initialView) : undefined}
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
