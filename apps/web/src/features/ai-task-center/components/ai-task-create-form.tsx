"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  AI_TASK_CATEGORIES,
  AI_TASK_PRIORITIES,
  AI_TASK_RISK_LEVELS,
  type AiTaskCategory,
  type AiTaskPriority,
  type AiTaskRiskLevel,
  type CreateAiTaskInput,
} from "../domain/ai-task";
import {
  createAiTaskDraftAction,
  createSourceLinkedAiTaskAction,
} from "../server/ai-task-actions";
import {
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskRiskLevelLabel,
} from "../presentation/ai-task-labels";
import { customerMessage } from "@/lib/presentation/customer-message";

import "./ai-task-create-form-polish.css";
function mapCreatePayload(input: CreateAiTaskInput) {
  return {
    title: input.title,
    summary: input.summary,
    category: input.category,
    priority: input.priority,
    riskLevel: input.riskLevel,
    detectedIssue: input.detectedIssue,
    reasoning: input.reasoning,
    recommendation: {
      title: input.recommendation.title,
      description: input.recommendation.description,
      actionType: input.recommendation.actionType,
      requiresApproval: input.recommendation.requiresApproval,
      reversible: input.recommendation.reversible,
      targetSystem: input.recommendation.targetSystem ?? null,
    },
    confidence: {
      score: input.confidence.score,
      explanation: input.confidence.explanation,
      modelVersion: input.confidence.modelVersion ?? null,
    },
    impact: {
      financialAmount: input.impact.financialAmount ?? null,
      currency: input.impact.currency ?? null,
      productionQuantity: input.impact.productionQuantity ?? null,
      productionUnit: input.impact.productionUnit ?? null,
      estimatedDelayMinutes: input.impact.estimatedDelayMinutes ?? null,
      estimatedDowntimeMinutes: input.impact.estimatedDowntimeMinutes ?? null,
      estimatedSavingsAmount: input.impact.estimatedSavingsAmount ?? null,
      summary: input.impact.summary,
    },
    sources: input.sources.map((source) => ({
      sourceType: source.sourceType,
      sourceId: source.sourceId ?? null,
      sourceLabel: source.sourceLabel,
      observedAt: source.observedAt ?? null,
      valueSummary: source.valueSummary ?? null,
    })),
    siteId: input.siteId ?? null,
    productionLineId: input.productionLineId ?? null,
    machineId: input.machineId ?? null,
    moldId: input.moldId ?? null,
    productionOrderId: input.productionOrderId ?? null,
    salesOrderId: input.salesOrderId ?? null,
    materialId: input.materialId ?? null,
    supplierId: input.supplierId ?? null,
    customerId: input.customerId ?? null,
    dueAtUtc: input.dueAt ?? null,
  };
}

function buildInputFromFormData(formData: FormData): CreateAiTaskInput | null {
  const title = String(formData.get("title") ?? "").trim();
  const summary = String(formData.get("summary") ?? "").trim();
  const detectedIssue = String(formData.get("detectedIssue") ?? "").trim();
  const reasoning = String(formData.get("reasoning") ?? "").trim();
  const recommendationTitle = String(formData.get("recommendationTitle") ?? "").trim();
  const recommendationDescription = String(formData.get("recommendationDescription") ?? "").trim();
  const category = String(formData.get("category") ?? "") as AiTaskCategory;
  const priority = String(formData.get("priority") ?? "") as AiTaskPriority;
  const riskLevel = String(formData.get("riskLevel") ?? "") as AiTaskRiskLevel;

  if (!title || !summary || !detectedIssue || !reasoning || !recommendationTitle || !recommendationDescription) {
    return null;
  }

  return {
    title,
    summary,
    category,
    priority,
    riskLevel,
    detectedIssue,
    reasoning,
    recommendation: {
      title: recommendationTitle,
      description: recommendationDescription,
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.75,
      explanation: "Manuel taslak görev oluşturma.",
    },
    impact: {
      summary,
    },
    sources: [
      {
        sourceType: "manual",
        sourceLabel: "Manuel görev oluşturma",
        valueSummary: "Kullanıcı tarafından oluşturuldu",
      },
    ],
  };
}

export function AiTaskCreateForm() {
  const router = useRouter();
  const [isPending, setIsPending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  return (
    <Card className="ai-task-create-form" data-testid="ai-task-create-form">
      <form
        data-pending={isPending ? "true" : "false"}
        method="post"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (isPending) {
            return;
          }

          const formData = new FormData(event.currentTarget);
          const input = buildInputFromFormData(formData);
          if (!input) {
            setErrorMessage("Zorunlu alanların tamamını doldurun.");
            return;
          }

          const sourceModule = String(formData.get("sourceModule") ?? "").trim();
          const sourceEntityId = String(formData.get("sourceEntityId") ?? "").trim();
          const idempotencyKey = String(formData.get("idempotencyKey") ?? "").trim();

          setErrorMessage(null);
          setIsPending(true);

          const createPromise = idempotencyKey
            ? (() => {
                if (!sourceModule || !sourceEntityId) {
                  return Promise.resolve({
                    status: "failure" as const,
                    message: "Kaynak bağlantısı için modül ve varlık kimliği gereklidir.",
                  });
                }

                return createSourceLinkedAiTaskAction({
                  ...mapCreatePayload(input),
                  sourceModule,
                  sourceEntityId,
                  idempotencyKey,
                });
              })()
            : createAiTaskDraftAction(input);

          void createPromise.then((result) => {
            if (!result || result.status === "failure") {
              setIsPending(false);
              setErrorMessage(result?.message ?? "Görev oluşturulamadı.");
              return;
            }

            router.push(`/ai-gorevleri/${result.taskId}`);
            router.refresh();
          });
        }}
      >
        <div className="ai-task-create-form__grid">
          <h2 className="ai-task-create-form__group">Genel</h2>

          <label>
            Başlık <span className="zx-required" aria-hidden="true">*</span>
            <input disabled={isPending} name="title" required type="text" />
          </label>

          <label>
            Özet <span className="zx-required" aria-hidden="true">*</span>
            <textarea disabled={isPending} name="summary" required rows={2} />
          </label>

          <div className="ai-task-create-form__row">
          <label>
            Kategori <span className="zx-required" aria-hidden="true">*</span>
            <select defaultValue="" disabled={isPending} name="category" required>
              <option disabled value="">Kategori seçin</option>
              {AI_TASK_CATEGORIES.map((category) => (
                <option key={category} value={category}>
                  {getAiTaskCategoryLabel(category)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Öncelik <span className="zx-required" aria-hidden="true">*</span>
            <select defaultValue="medium" disabled={isPending} name="priority" required>
              {AI_TASK_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {getAiTaskPriorityLabel(priority)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Risk seviyesi <span className="zx-required" aria-hidden="true">*</span>
            <select defaultValue="medium" disabled={isPending} name="riskLevel" required>
              {AI_TASK_RISK_LEVELS.map((riskLevel) => (
                <option key={riskLevel} value={riskLevel}>
                  {getAiTaskRiskLevelLabel(riskLevel)}
                </option>
              ))}
            </select>
          </label>

          </div>

          <h2 className="ai-task-create-form__group">Analiz</h2>

          <label>
            Tespit edilen sorun <span className="zx-required" aria-hidden="true">*</span>
            <textarea disabled={isPending} name="detectedIssue" required rows={3} />
          </label>

          <label>
            Analiz gerekçesi <span className="zx-required" aria-hidden="true">*</span>
            <textarea disabled={isPending} name="reasoning" required rows={3} />
          </label>

          <h2 className="ai-task-create-form__group">Öneri</h2>

          <label>
            Öneri başlığı <span className="zx-required" aria-hidden="true">*</span>
            <input disabled={isPending} name="recommendationTitle" required type="text" />
          </label>

          <label>
            Öneri açıklaması <span className="zx-required" aria-hidden="true">*</span>
            <textarea disabled={isPending} name="recommendationDescription" required rows={3} />
          </label>

          <details className="ai-task-create-form__advanced">
            <summary>Gelişmiş: kaynak bağlantısı (isteğe bağlı)</summary>
            <div className="ai-task-create-form__advanced-grid">
            <label>
              Kaynak modül (isteğe bağlı)
              <input disabled={isPending} name="sourceModule" type="text" />
            </label>

            <label>
              Kaynak varlık kimliği (isteğe bağlı)
              <input disabled={isPending} name="sourceEntityId" type="text" />
            </label>

            <label>
              Tekrar önleme anahtarı (isteğe bağlı)
              <input disabled={isPending} name="idempotencyKey" type="text" />
            </label>
            </div>
          </details>
        </div>

        {errorMessage ? (
          <p
            className="ai-task-create-form__error"
            data-testid="ai-task-create-form-error"
            role="alert"
          >
            {customerMessage(errorMessage)}
          </p>
        ) : null}

        <div className="ai-task-create-form__actions">
          <Button disabled={isPending} type="submit">
            {isPending ? "Taslak oluşturuluyor…" : "Taslak görev oluştur"}
          </Button>

          <Link className="button button--secondary" href="/ai-gorevleri">
            Vazgeç
          </Link>
        </div>
      </form>
    </Card>
  );
}
