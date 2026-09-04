import { Suspense } from "react";

import {
  PageContent,
  PageHeader,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskPlaybookHeaderActions } from "@/features/ai-task-center/components/ai-task-playbook-header-actions";
import { AiTaskPlaybookLibraryClient } from "@/features/ai-task-center/components/ai-task-playbook-library-client";
import { readAiTaskPlaybookLibrary } from "@/features/ai-task-center/server/ai-task-playbook-queries";

function HeaderActionsFallback() {
  return <div className="ai-task-playbook-page-actions" aria-hidden="true" />;
}

export async function AiTaskPlaybookPageContent() {
  const result = await readAiTaskPlaybookLibrary();

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Operasyonel playbook kütüphanesi ve çalışma takibi"
          eyebrow="AI Görev Merkezi"
          title="Operasyonel Playbooklar"
        />

        <EmptyState
          data-testid="ai-task-playbook-error"
          description={result.message}
          title="Playbook kütüphanesi yüklenemedi"
        />
      </PageContent>
    );
  }

  const { library } = result;

  return (
    <PageContent data-testid="ai-task-playbook-page">
      <PageHeader
        actions={
          <Suspense fallback={<HeaderActionsFallback />}>
            <AiTaskPlaybookHeaderActions
              canManage={library.capabilities.canManage}
              counts={{
                playbooks: library.playbooks.length,
                active: library.activeRuns.length,
                history: library.historyRuns.length,
              }}
            />
          </Suspense>
        }
        description="Standart operasyon akışlarını tanımlayın, çalıştırın ve izleyin."
        eyebrow="AI Görev Merkezi"
        title="Operasyonel Playbooklar"
      />

      <Suspense fallback={null}>
        <AiTaskPlaybookLibraryClient
          activeRuns={library.activeRuns}
          capabilities={library.capabilities}
          historyRuns={library.historyRuns}
          playbooks={library.playbooks}
        />
      </Suspense>
    </PageContent>
  );
}
