import { Suspense } from "react";
import {
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
import { calculateAiTaskKpis } from "@/features/ai-task-center/application/ai-task-query";
import { AiTaskKpiLinks } from "@/features/ai-task-center/components/ai-task-kpi-links";
import { AiTaskKpiLoading } from "@/features/ai-task-center/components/ai-task-kpi-loading";
import { AiTaskWorkspace } from "@/features/ai-task-center/components/ai-task-workspace";
import { AiTaskWorkspaceLoading } from "@/features/ai-task-center/components/ai-task-workspace-loading";
import { AI_TASK_PREVIEW_DATA } from "@/features/ai-task-center/data/ai-task-preview-data";

const PREVIEW_NOW_ISO =
  "2026-08-05T01:08:00+03:00";

const PREVIEW_NOW = new Date(
  PREVIEW_NOW_ISO,
);

export default function HomePage() {
  const kpis = calculateAiTaskKpis(
    AI_TASK_PREVIEW_DATA,
    PREVIEW_NOW,
  );

  return (
    <PageContent>
      <PageHeader
        actions={
          <Button
            disabled
            leadingIcon={<RefreshCw />}
            title="Gerçek veri bağlantısı sonraki entegrasyon paketlerinde uygulanacaktır."
            variant="secondary"
          >
            Veriyi yenile
          </Button>
        }
        description="Üretim, kalite, bakım, malzeme, ticari zekâ ve ERP entegrasyonlarından oluşan AI önerilerini tek merkezden inceleyin."
        eyebrow="AI Görev Merkezi"
        preview
        title="Operasyon karar merkezi"
      />

      <Suspense fallback={<AiTaskKpiLoading />}>
        <AiTaskKpiLinks kpis={kpis} />
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
        <Suspense fallback={<AiTaskWorkspaceLoading />}>
          <AiTaskWorkspace
            nowIso={PREVIEW_NOW_ISO}
            tasks={AI_TASK_PREVIEW_DATA}
          />
        </Suspense>
      </Section>
    </PageContent>
  );
}