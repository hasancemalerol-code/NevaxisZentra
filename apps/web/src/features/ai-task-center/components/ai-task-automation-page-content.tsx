import Link from "next/link";
import { ArrowLeft, RefreshCw, Sparkles } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskAutomationWorkspaceClient } from "@/features/ai-task-center/components/ai-task-automation-workspace-client";
import { refreshAiTaskAutomationWorkspaceAction } from "@/features/ai-task-center/server/ai-task-automation-actions";
import { readAiTaskAutomationWorkspace } from "@/features/ai-task-center/server/ai-task-automation-queries";

async function RefreshButton() {
  async function refresh() {
    "use server";
    await refreshAiTaskAutomationWorkspaceAction();
  }

  return (
    <form action={refresh}>
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
          description="AI görev otomasyon kuralları ve simülasyon yönetimi"
          eyebrow="AI Görev Merkezi"
          title="Otomasyon Yönetimi"
        />

        <EmptyState
          data-testid="ai-task-automation-error"
          description={result.message}
          title="Otomasyon yönetimi yüklenemedi"
        />
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
        description="Kuralları, politikaları, simülasyonları ve çalışma geçmişini yönetin."
        eyebrow="AI Görev Merkezi"
        title="Otomasyon Yönetimi"
      />

      <Section
        action={
          <Badge tone="ai">
            <Sparkles aria-hidden="true" />
            Human-in-the-Loop
          </Badge>
        }
        description="Otomasyon kuralları görevleri yönlendirir; kritik aksiyonlar insan onayı gerektirir."
        title="Otomasyon çalışma alanı"
      >
        <AiTaskAutomationWorkspaceClient
          capabilities={workspace.capabilities}
          closurePolicies={workspace.closurePolicies}
          executions={workspace.executions}
          rules={workspace.rules}
          slaPolicies={workspace.slaPolicies}
          taskOptions={workspace.taskOptions}
        />
      </Section>
    </PageContent>
  );
}
