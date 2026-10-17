import Link from "next/link";
import { ArrowLeft, RefreshCw } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  PageContent,
  PageHeader,
} from "@/components/ui/page";

import { AiTaskAutomationWorkspaceClient } from "@/features/ai-task-center/components/ai-task-automation-workspace-client";
import { refreshAiTaskAutomationWorkspaceAction } from "@/features/ai-task-center/server/ai-task-automation-actions";
import { readAiTaskAutomationWorkspace } from "@/features/ai-task-center/server/ai-task-automation-queries";
import { FailureStatePage } from "@/components/ui/failure-state";
import "@/features/ai-task-center/components/ai-task-center-round3.css";

async function RefreshButton() {
  async function refresh() {
    "use server";
    await refreshAiTaskAutomationWorkspaceAction();
  }

  return (
    <form action={refresh} className="zx-inline-form">
      <Button leadingIcon={<RefreshCw />} type="submit" variant="secondary">
        Veriyi yenile
      </Button>
    </form>
  );
}

export async function AiTaskAutomationPageContent() {
  const result = await readAiTaskAutomationWorkspace();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Yapay zekâ görev otomasyon kuralları ve simülasyon yönetimi"
          eyebrow="Görev Merkezi"
          title="Otomasyon Yönetimi"
        />

        <div data-testid="ai-task-automation-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Otomasyon yönetimi yüklenemedi" inline /></div>
      </PageContent>
    );
  }

  const { workspace } = result;

  return (
    <PageContent data-testid="ai-task-automation-page">
      <PageHeader
        actions={
          <>
            <Link className="ai-task-automation-back-link" href="/ai-gorevleri">
              <ArrowLeft aria-hidden="true" />
              <span>Görev listesine dön</span>
            </Link>
            <RefreshButton />
          </>
        }
        description="Kuralları, politikaları, simülasyonları ve çalışma geçmişini yönetin. Kritik aksiyonlar insan onayı gerektirir."
        eyebrow="Görev Merkezi"
        title="Otomasyon Yönetimi"
      />

      <AiTaskAutomationWorkspaceClient
          capabilities={workspace.capabilities}
          closurePolicies={workspace.closurePolicies}
          executions={workspace.executions}
          rules={workspace.rules}
          slaPolicies={workspace.slaPolicies}
          taskOptions={workspace.taskOptions}
        />
    </PageContent>
  );
}
