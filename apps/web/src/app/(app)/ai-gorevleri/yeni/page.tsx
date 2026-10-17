import { notFound } from "next/navigation";

import {
  PageContent,
  PageHeader,
} from "@/components/ui/page";

import { AiTaskCreateForm } from "@/features/ai-task-center/components/ai-task-create-form";
import {
  TASK_CENTER_CREATE_PAGE_EYEBROW,
  TASK_CENTER_CREATE_PAGE_TITLE,
  TASK_CENTER_CREATE_RETURN_LABEL,
} from "@/features/ai-task-center/domain/ai-task-product-labels";
import { readAiTaskWorkspace } from "@/features/ai-task-center/server/ai-task-queries";
import { FailureStatePage } from "@/components/ui/failure-state";

export default async function AiTaskCreatePage() {
  const result = await readAiTaskWorkspace({ page: 1, pageSize: 1 });

  if (result.status === "failure") {
    return (
      <PageContent>
        <PageHeader
          description="Yeni bir görev oluşturmak için gerekli yetki veya bağlantı doğrulanamadı."
          eyebrow={TASK_CENTER_CREATE_PAGE_EYEBROW}
          title={TASK_CENTER_CREATE_PAGE_TITLE}
        />

        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Görev oluşturma kullanılamıyor" backHref="/ai-gorevleri" backLabel={TASK_CENTER_CREATE_RETURN_LABEL} inline />
      </PageContent>
    );
  }

  if (!result.workspace.canCreate) {
    notFound();
  }

  return (
    <PageContent data-testid="ai-task-create-page">
      <PageHeader
        description="Yeni bir taslak görev oluşturun. Görevler otomatik onaylanmaz; inceleme sürecine sunulmalıdır."
        eyebrow={TASK_CENTER_CREATE_PAGE_EYEBROW}
        title={TASK_CENTER_CREATE_PAGE_TITLE}
      />

      <AiTaskCreateForm />
    </PageContent>
  );
}
