import Link from "next/link";
import { notFound } from "next/navigation";

import {
  PageContent,
  PageHeader,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskCreateForm } from "@/features/ai-task-center/components/ai-task-create-form";
import { readAiTaskWorkspace } from "@/features/ai-task-center/server/ai-task-queries";

export default async function AiTaskCreatePage() {
  const result = await readAiTaskWorkspace({ page: 1, pageSize: 1 });

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Yeni bir AI görevi oluşturmak için gerekli yetki veya bağlantı doğrulanamadı."
          eyebrow="AI Görev Merkezi"
          title="AI görevi oluşturma"
        />

        <EmptyState
          action={
            <Link href="/ai-gorevleri">
              AI Görev Merkezi&apos;ne dön
            </Link>
          }
          description={result.message}
          title="Görev oluşturma kullanılamıyor"
        />
      </PageContent>
    );
  }

  if (!result.workspace.canCreate) {
    notFound();
  }

  return (
    <PageContent data-testid="ai-task-create-page">
      <PageHeader
        description="Yeni bir taslak AI görevi oluşturun. Görevler otomatik onaylanmaz; inceleme sürecine sunulmalıdır."
        eyebrow="AI Görev Merkezi"
        title="AI görevi oluşturma"
      />

      <AiTaskCreateForm />
    </PageContent>
  );
}
