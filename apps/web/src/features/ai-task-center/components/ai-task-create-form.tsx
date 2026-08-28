"use client";

import { useState, useTransition } from "react";
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

export function AiTaskCreateForm() {
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  function handleSubmit(formData: FormData) {
    setErrorMessage(null);

    const title = String(formData.get("title") ?? "").trim();
    const summary = String(formData.get("summary") ?? "").trim();
    const detectedIssue = String(formData.get("detectedIssue") ?? "").trim();
    const reasoning = String(formData.get("reasoning") ?? "").trim();
    const recommendationTitle = String(formData.get("recommendationTitle") ?? "").trim();
    const recommendationDescription = String(formData.get("recommendationDescription") ?? "").trim();
    const sourceModule = String(formData.get("sourceModule") ?? "").trim();
    const sourceEntityId = String(formData.get("sourceEntityId") ?? "").trim();
    const idempotencyKey = String(formData.get("idempotencyKey") ?? "").trim();
    const category = String(formData.get("category") ?? "") as AiTaskCategory;
    const priority = String(formData.get("priority") ?? "") as AiTaskPriority;
    const riskLevel = String(formData.get("riskLevel") ?? "") as AiTaskRiskLevel;

    if (!title || !summary || !detectedIssue || !reasoning || !recommendationTitle || !recommendationDescription) {
      setErrorMessage("Zorunlu alanların tamamını doldurun.");
      return;
    }

    const input: CreateAiTaskInput = {
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
        summary: summary,
      },
      sources: [
        {
          sourceType: "manual",
          sourceLabel: "Manuel görev oluşturma",
          valueSummary: "Kullanıcı tarafından oluşturuldu",
        },
      ],
    };

    startTransition(async () => {
      if (idempotencyKey) {
        if (!sourceModule || !sourceEntityId) {
          setErrorMessage("Kaynak bağlantısı için modül ve varlık kimliği gereklidir.");
          return;
        }

        const result = await createSourceLinkedAiTaskAction({
          ...mapCreatePayload(input),
          sourceModule,
          sourceEntityId,
          idempotencyKey,
        });
        if (result && "status" in result && result.status === "failure") {
          setErrorMessage(result.message);
        }
        return;
      }

      const result = await createAiTaskDraftAction(input);
      if (result && "status" in result && result.status === "failure") {
        setErrorMessage(result.message);
      }
    });
  }

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

  return (
    <Card className="ai-task-create-form" data-testid="ai-task-create-form">
      <form action={handleSubmit}>
        <div className="ai-task-create-form__grid">
          <label>
            Başlık
            <input name="title" required type="text" />
          </label>

          <label>
            Özet
            <textarea name="summary" required rows={2} />
          </label>

          <label>
            Kategori
            <select defaultValue="commercial-intelligence" name="category" required>
              {AI_TASK_CATEGORIES.map((category) => (
                <option key={category} value={category}>
                  {getAiTaskCategoryLabel(category)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Öncelik
            <select defaultValue="medium" name="priority" required>
              {AI_TASK_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {getAiTaskPriorityLabel(priority)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Risk seviyesi
            <select defaultValue="medium" name="riskLevel" required>
              {AI_TASK_RISK_LEVELS.map((riskLevel) => (
                <option key={riskLevel} value={riskLevel}>
                  {getAiTaskRiskLevelLabel(riskLevel)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Tespit edilen sorun
            <textarea name="detectedIssue" required rows={3} />
          </label>

          <label>
            Analiz gerekçesi
            <textarea name="reasoning" required rows={3} />
          </label>

          <label>
            Öneri başlığı
            <input name="recommendationTitle" required type="text" />
          </label>

          <label>
            Öneri açıklaması
            <textarea name="recommendationDescription" required rows={3} />
          </label>

          <label>
            Kaynak modül (isteğe bağlı)
            <input defaultValue="commercial-intelligence" name="sourceModule" type="text" />
          </label>

          <label>
            Kaynak varlık kimliği (isteğe bağlı)
            <input name="sourceEntityId" type="text" />
          </label>

          <label>
            Idempotency anahtarı (isteğe bağlı)
            <input name="idempotencyKey" type="text" />
          </label>
        </div>

        {errorMessage ? (
          <p className="ai-task-create-form__error" role="alert">
            {errorMessage}
          </p>
        ) : null}

        <div className="ai-task-create-form__actions">
          <Button disabled={isPending} type="submit">
            Taslak görev oluştur
          </Button>

          <Link className="button button--secondary" href="/ai-gorevleri">
            Vazgeç
          </Link>
        </div>
      </form>
    </Card>
  );
}
