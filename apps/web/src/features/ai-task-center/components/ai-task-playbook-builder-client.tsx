"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ArrowDown, ArrowUp, CheckCircle2, Plus, Trash2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import { AI_TASK_PRIORITIES } from "../domain/ai-task";
import type {
  AiTaskPlaybookDependencyDefinition,
  AiTaskPlaybookDetail,
  AiTaskPlaybookStepDefinition,
  CreateAiTaskPlaybookInput,
  UpdateAiTaskPlaybookInput,
} from "../domain/ai-task-playbook";
import {
  AI_TASK_PLAYBOOK_CATEGORIES,
  AI_TASK_PLAYBOOK_CONDITION_FIELDS,
  AI_TASK_PLAYBOOK_CONDITION_OPERATORS,
  AI_TASK_PLAYBOOK_DEPENDENCY_TYPES,
  AI_TASK_PLAYBOOK_STEP_TYPES,
} from "../domain/ai-task-playbook";
import { getAiTaskPriorityLabel } from "../presentation/ai-task-labels";
import {
  getAiTaskPlaybookCategoryLabel,
  getAiTaskPlaybookConditionFieldLabel,
  getAiTaskPlaybookConditionOperatorLabel,
  getAiTaskPlaybookDependencyTypeLabel,
  getAiTaskPlaybookStepTypeLabel,
} from "../presentation/ai-task-playbook-labels";
import {
  createAiTaskPlaybookAction,
  updateAiTaskPlaybookAction,
  validateAiTaskPlaybookGraphAction,
} from "../server/ai-task-playbook-actions";
import type { AiTaskPlaybookCapabilities } from "../server/ai-task-playbook-queries";
import {
  AiTaskDetailField,
  AiTaskDetailInput,
  AiTaskDetailSectionCard,
  AiTaskDetailSelect,
  AiTaskDetailTextarea,
} from "./ai-task-detail-form-controls";

type AiTaskPlaybookBuilderClientProps = Readonly<{
  mode: "create" | "edit";
  initialPlaybook?: AiTaskPlaybookDetail | null;
  capabilities: AiTaskPlaybookCapabilities;
}>;

function slugifyKey(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
}

function emptyStep(sortOrder: number): AiTaskPlaybookStepDefinition {
  const key = `step-${sortOrder}`;
  return {
    stepKey: key,
    name: `Adım ${sortOrder}`,
    description: null,
    sortOrder,
    stepType: "task",
    required: true,
    defaultPriority: "medium",
    assignmentPolicyJson: null,
    slaPolicyJson: null,
    evidencePolicyJson: null,
    closurePolicyJson: null,
    condition: null,
    targetDurationMinutes: null,
  };
}

function mapDetailSteps(playbook: AiTaskPlaybookDetail): AiTaskPlaybookStepDefinition[] {
  return playbook.steps.map((step) => ({
    stepKey: step.stepKey,
    name: step.name,
    description: step.description,
    sortOrder: step.sortOrder,
    stepType: step.stepType,
    required: step.required,
    defaultPriority: step.defaultPriority,
    assignmentPolicyJson: step.assignmentPolicyJson,
    slaPolicyJson: step.slaPolicyJson,
    evidencePolicyJson: step.evidencePolicyJson,
    closurePolicyJson: step.closurePolicyJson,
    condition: step.condition,
    targetDurationMinutes: step.targetDurationMinutes,
  }));
}

function mapDetailDependencies(playbook: AiTaskPlaybookDetail): AiTaskPlaybookDependencyDefinition[] {
  return playbook.dependencies.map((dep) => ({
    stepKey: dep.stepKey,
    dependsOnStepKey: dep.dependsOnStepKey,
    dependencyType: dep.dependencyType,
  }));
}

export function AiTaskPlaybookBuilderClient({
  mode,
  initialPlaybook,
  capabilities,
}: AiTaskPlaybookBuilderClientProps) {
  const router = useRouter();
  const [name, setName] = useState(initialPlaybook?.name ?? "");
  const [description, setDescription] = useState(initialPlaybook?.description ?? "");
  const [playbookKey, setPlaybookKey] = useState(initialPlaybook?.playbookKey ?? "");
  const [category, setCategory] = useState(initialPlaybook?.category ?? AI_TASK_PLAYBOOK_CATEGORIES[0]);
  const [triggerContext, setTriggerContext] = useState(initialPlaybook?.triggerContext ?? "");
  const [enabled, setEnabled] = useState(initialPlaybook?.enabled ?? true);
  const [changeReason, setChangeReason] = useState("");
  const [steps, setSteps] = useState<readonly AiTaskPlaybookStepDefinition[]>(
    initialPlaybook?.steps.length ? mapDetailSteps(initialPlaybook) : [emptyStep(1)],
  );
  const [dependencies, setDependencies] = useState<readonly AiTaskPlaybookDependencyDefinition[]>(
    initialPlaybook ? mapDetailDependencies(initialPlaybook) : [],
  );
  const [validationErrors, setValidationErrors] = useState<readonly string[]>([]);
  const [validationSuccess, setValidationSuccess] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const canSave = capabilities.canManage;

  function reorderSteps(nextSteps: AiTaskPlaybookStepDefinition[]) {
    return nextSteps.map((step, index) => ({
      ...step,
      sortOrder: index + 1,
    }));
  }

  function updateStep(index: number, patch: Partial<AiTaskPlaybookStepDefinition>) {
    setSteps((current) =>
      reorderSteps(
        current.map((step, stepIndex) =>
          stepIndex === index ? { ...step, ...patch } : step,
        ),
      ),
    );
    setValidationSuccess(false);
  }

  function addStep() {
    setSteps((current) => reorderSteps([...current, emptyStep(current.length + 1)]));
    setValidationSuccess(false);
  }

  function removeStep(index: number) {
    const removedKey = steps[index]?.stepKey;
    setSteps((current) => reorderSteps(current.filter((_, stepIndex) => stepIndex !== index)));
    if (removedKey) {
      setDependencies((current) =>
        current.filter(
          (dep) => dep.stepKey !== removedKey && dep.dependsOnStepKey !== removedKey,
        ),
      );
    }
    setValidationSuccess(false);
  }

  function moveStep(index: number, direction: -1 | 1) {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= steps.length) return;
    const next = [...steps];
    const [item] = next.splice(index, 1);
    next.splice(targetIndex, 0, item);
    setSteps(reorderSteps(next));
    setValidationSuccess(false);
  }

  function addDependency() {
    const first = steps[0]?.stepKey ?? "";
    const second = steps[1]?.stepKey ?? first;
    setDependencies((current) => [
      ...current,
      {
        stepKey: second,
        dependsOnStepKey: first,
        dependencyType: "required-predecessor",
      },
    ]);
    setValidationSuccess(false);
  }

  function updateDependency(
    index: number,
    patch: Partial<AiTaskPlaybookDependencyDefinition>,
  ) {
    setDependencies((current) =>
      current.map((dep, depIndex) =>
        depIndex === index ? { ...dep, ...patch } : dep,
      ),
    );
    setValidationSuccess(false);
  }

  function removeDependency(index: number) {
    setDependencies((current) => current.filter((_, depIndex) => depIndex !== index));
    setValidationSuccess(false);
  }

  async function handleValidate() {
    setActionError(null);
    setValidationSuccess(false);
    const result = await validateAiTaskPlaybookGraphAction({ steps, dependencies });
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    setValidationErrors(result.errors);
    setValidationSuccess(result.isValid);
  }

  async function handleSave() {
    if (!canSave) return;
    setActionError(null);
    setSubmitting(true);

    const resolvedKey = playbookKey.trim() || slugifyKey(name);
    const payloadSteps = steps.map((step) => ({
      ...step,
      stepKey: step.stepKey.trim() || slugifyKey(step.name),
    }));

    try {
      if (mode === "create") {
        const input: CreateAiTaskPlaybookInput = {
          playbookKey: resolvedKey,
          name: name.trim(),
          description: description.trim() || null,
          category,
          triggerContext: triggerContext.trim() || null,
          enabled,
          steps: payloadSteps,
          dependencies,
        };
        const result = await createAiTaskPlaybookAction(input);
        if (result.status === "failure") {
          setActionError(result.message);
          return;
        }
        router.push(`/ai-gorevleri/playbooklar/${result.playbookId}`);
        router.refresh();
        return;
      }

      if (!initialPlaybook) {
        setActionError("Playbook bulunamadı.");
        return;
      }

      const input: UpdateAiTaskPlaybookInput = {
        expectedVersion: initialPlaybook.version,
        name: name.trim(),
        description: description.trim() || null,
        category,
        triggerContext: triggerContext.trim() || null,
        enabled,
        steps: payloadSteps,
        dependencies,
        changeReason: changeReason.trim() || "Playbook güncellendi",
      };
      const result = await updateAiTaskPlaybookAction(initialPlaybook.id, input);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/playbooklar/${result.playbookId}`);
      router.refresh();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="ai-task-playbook-builder" data-testid="ai-task-playbook-builder">
      {actionError ? (
        <p className="ai-task-playbook-builder__error" role="alert">{actionError}</p>
      ) : null}

      <AiTaskDetailSectionCard title="Playbook Bilgileri">
        <div className="ai-task-playbook-builder__grid">
          <AiTaskDetailField label="Ad">
            <AiTaskDetailInput
              value={name}
              onChange={(event) => {
                setName(event.target.value);
                if (mode === "create" && !playbookKey) {
                  setPlaybookKey(slugifyKey(event.target.value));
                }
              }}
            />
          </AiTaskDetailField>

          <AiTaskDetailField label="Anahtar">
            <AiTaskDetailInput
              disabled={mode === "edit"}
              value={playbookKey}
              onChange={(event) => setPlaybookKey(event.target.value)}
            />
          </AiTaskDetailField>

          <AiTaskDetailField label="Kategori">
            <AiTaskDetailSelect
              value={category}
              onChange={(event) => setCategory(event.target.value)}
            >
              {AI_TASK_PLAYBOOK_CATEGORIES.map((item) => (
                <option key={item} value={item}>
                  {getAiTaskPlaybookCategoryLabel(item)}
                </option>
              ))}
            </AiTaskDetailSelect>
          </AiTaskDetailField>

          <AiTaskDetailField label="Durum">
            <AiTaskDetailSelect
              value={enabled ? "enabled" : "disabled"}
              onChange={(event) => setEnabled(event.target.value === "enabled")}
            >
              <option value="enabled">Etkin</option>
              <option value="disabled">Devre Dışı</option>
            </AiTaskDetailSelect>
          </AiTaskDetailField>

          <AiTaskDetailField className="ai-task-playbook-builder__full" label="Açıklama">
            <AiTaskDetailTextarea
              rows={2}
              value={description}
              onChange={(event) => setDescription(event.target.value)}
            />
          </AiTaskDetailField>

          <AiTaskDetailField className="ai-task-playbook-builder__full" label="Tetikleme bağlamı">
            <AiTaskDetailInput
              placeholder="Örn. makine arızası bildirimi"
              value={triggerContext}
              onChange={(event) => setTriggerContext(event.target.value)}
            />
          </AiTaskDetailField>

          {mode === "edit" ? (
            <AiTaskDetailField className="ai-task-playbook-builder__full" label="Değişiklik nedeni">
              <AiTaskDetailInput
                value={changeReason}
                onChange={(event) => setChangeReason(event.target.value)}
              />
            </AiTaskDetailField>
          ) : null}
        </div>
      </AiTaskDetailSectionCard>

      <AiTaskDetailSectionCard title="Adımlar">
        <div className="ai-task-playbook-builder__section-actions">
          <Button leadingIcon={<Plus />} type="button" variant="secondary" onClick={addStep}>
            Adım Ekle
          </Button>
        </div>

        <div className="ai-task-playbook-builder__steps">
          {steps.map((step, index) => (
            <Card
              key={`${step.stepKey}-${index}`}
              className="ai-task-playbook-builder__step-card"
              data-testid={`ai-task-playbook-step-${step.stepKey}`}
            >
              <div className="ai-task-playbook-builder__step-header">
                <div className="ai-task-playbook-builder__step-title">
                  <Badge tone="information">#{step.sortOrder}</Badge>
                  <strong>{step.name || `Adım ${step.sortOrder}`}</strong>
                  <Badge tone={step.required ? "warning" : "neutral"}>
                    {step.required ? "Zorunlu" : "İsteğe bağlı"}
                  </Badge>
                </div>
                <div className="ai-task-playbook-builder__step-toolbar">
                  <button
                    aria-label="Yukarı taşı"
                    className="ai-task-playbook-icon-button"
                    disabled={index === 0}
                    type="button"
                    onClick={() => moveStep(index, -1)}
                  >
                    <ArrowUp aria-hidden="true" />
                  </button>
                  <button
                    aria-label="Aşağı taşı"
                    className="ai-task-playbook-icon-button"
                    disabled={index === steps.length - 1}
                    type="button"
                    onClick={() => moveStep(index, 1)}
                  >
                    <ArrowDown aria-hidden="true" />
                  </button>
                  <button
                    aria-label="Adımı sil"
                    className="ai-task-playbook-icon-button ai-task-playbook-icon-button--danger"
                    disabled={steps.length <= 1}
                    type="button"
                    onClick={() => removeStep(index)}
                  >
                    <Trash2 aria-hidden="true" />
                  </button>
                </div>
              </div>

              <div className="ai-task-playbook-builder__grid">
                <AiTaskDetailField label="Adım anahtarı">
                  <AiTaskDetailInput
                    value={step.stepKey}
                    onChange={(event) => updateStep(index, { stepKey: event.target.value })}
                  />
                </AiTaskDetailField>

                <AiTaskDetailField label="Ad">
                  <AiTaskDetailInput
                    value={step.name}
                    onChange={(event) => updateStep(index, { name: event.target.value })}
                  />
                </AiTaskDetailField>

                <AiTaskDetailField label="Tip">
                  <AiTaskDetailSelect
                    value={step.stepType}
                    onChange={(event) => updateStep(index, { stepType: event.target.value })}
                  >
                    {AI_TASK_PLAYBOOK_STEP_TYPES.map((item) => (
                      <option key={item} value={item}>
                        {getAiTaskPlaybookStepTypeLabel(item)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <AiTaskDetailField label="Öncelik">
                  <AiTaskDetailSelect
                    value={step.defaultPriority}
                    onChange={(event) => updateStep(index, { defaultPriority: event.target.value })}
                  >
                    {AI_TASK_PRIORITIES.map((item) => (
                      <option key={item} value={item}>
                        {getAiTaskPriorityLabel(item)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <AiTaskDetailField label="Zorunluluk">
                  <AiTaskDetailSelect
                    value={step.required ? "required" : "optional"}
                    onChange={(event) =>
                      updateStep(index, { required: event.target.value === "required" })
                    }
                  >
                    <option value="required">Zorunlu</option>
                    <option value="optional">İsteğe bağlı</option>
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <AiTaskDetailField label="Hedef süre (dk)">
                  <AiTaskDetailInput
                    inputMode="numeric"
                    type="number"
                    value={step.targetDurationMinutes ?? ""}
                    onChange={(event) =>
                      updateStep(index, {
                        targetDurationMinutes: event.target.value
                          ? Number(event.target.value)
                          : null,
                      })
                    }
                  />
                </AiTaskDetailField>

                <AiTaskDetailField className="ai-task-playbook-builder__full" label="Açıklama">
                  <AiTaskDetailTextarea
                    rows={2}
                    value={step.description ?? ""}
                    onChange={(event) =>
                      updateStep(index, { description: event.target.value || null })
                    }
                  />
                </AiTaskDetailField>
              </div>

              <details className="ai-task-playbook-builder__condition">
                <summary>Koşul (isteğe bağlı)</summary>
                <div className="ai-task-playbook-builder__grid">
                  <AiTaskDetailField label="Alan">
                    <AiTaskDetailSelect
                      value={step.condition?.field ?? ""}
                      onChange={(event) => {
                        const field = event.target.value;
                        if (!field) {
                          updateStep(index, { condition: null });
                          return;
                        }
                        updateStep(index, {
                          condition: {
                            field,
                            operator: step.condition?.operator ?? "equals",
                            value: step.condition?.value ?? "",
                          },
                        });
                      }}
                    >
                      <option value="">Koşul yok</option>
                      {AI_TASK_PLAYBOOK_CONDITION_FIELDS.map((item) => (
                        <option key={item} value={item}>
                          {getAiTaskPlaybookConditionFieldLabel(item)}
                        </option>
                      ))}
                    </AiTaskDetailSelect>
                  </AiTaskDetailField>

                  {step.condition ? (
                    <>
                      <AiTaskDetailField label="Operatör">
                        <AiTaskDetailSelect
                          value={step.condition.operator}
                          onChange={(event) =>
                            updateStep(index, {
                              condition: {
                                ...step.condition!,
                                operator: event.target.value,
                              },
                            })
                          }
                        >
                          {AI_TASK_PLAYBOOK_CONDITION_OPERATORS.map((item) => (
                            <option key={item} value={item}>
                              {getAiTaskPlaybookConditionOperatorLabel(item)}
                            </option>
                          ))}
                        </AiTaskDetailSelect>
                      </AiTaskDetailField>

                      <AiTaskDetailField label="Değer">
                        <AiTaskDetailInput
                          value={step.condition.value}
                          onChange={(event) =>
                            updateStep(index, {
                              condition: {
                                ...step.condition!,
                                value: event.target.value,
                              },
                            })
                          }
                        />
                      </AiTaskDetailField>
                    </>
                  ) : null}
                </div>
              </details>
            </Card>
          ))}
        </div>
      </AiTaskDetailSectionCard>

      <AiTaskDetailSectionCard title="Bağımlılıklar">
        <div className="ai-task-playbook-builder__section-actions">
          <Button
            disabled={steps.length < 2}
            leadingIcon={<Plus />}
            type="button"
            variant="secondary"
            onClick={addDependency}
          >
            Bağımlılık Ekle
          </Button>
        </div>

        {dependencies.length === 0 ? (
          <p className="ai-task-playbook-muted">Adımlar arasında bağımlılık tanımlanmadı.</p>
        ) : (
          <div className="ai-task-playbook-builder__dependencies">
            {dependencies.map((dep, index) => (
              <div key={`${dep.stepKey}-${dep.dependsOnStepKey}-${index}`} className="ai-task-playbook-builder__dependency-row">
                <AiTaskDetailSelect
                  value={dep.dependsOnStepKey}
                  onChange={(event) =>
                    updateDependency(index, { dependsOnStepKey: event.target.value })
                  }
                >
                  {steps.map((step) => (
                    <option key={step.stepKey} value={step.stepKey}>
                      {step.name} ({step.stepKey})
                    </option>
                  ))}
                </AiTaskDetailSelect>
                <span className="ai-task-playbook-builder__dependency-arrow">→</span>
                <AiTaskDetailSelect
                  value={dep.stepKey}
                  onChange={(event) => updateDependency(index, { stepKey: event.target.value })}
                >
                  {steps.map((step) => (
                    <option key={step.stepKey} value={step.stepKey}>
                      {step.name} ({step.stepKey})
                    </option>
                  ))}
                </AiTaskDetailSelect>
                <AiTaskDetailSelect
                  value={dep.dependencyType}
                  onChange={(event) =>
                    updateDependency(index, { dependencyType: event.target.value })
                  }
                >
                  {AI_TASK_PLAYBOOK_DEPENDENCY_TYPES.map((item) => (
                    <option key={item} value={item}>
                      {getAiTaskPlaybookDependencyTypeLabel(item)}
                    </option>
                  ))}
                </AiTaskDetailSelect>
                <button
                  aria-label="Bağımlılığı sil"
                  className="ai-task-playbook-icon-button ai-task-playbook-icon-button--danger"
                  type="button"
                  onClick={() => removeDependency(index)}
                >
                  <Trash2 aria-hidden="true" />
                </button>
              </div>
            ))}
          </div>
        )}
      </AiTaskDetailSectionCard>

      {validationErrors.length > 0 ? (
        <Card className="ai-task-playbook-builder__validation">
          <h3>Doğrulama hataları</h3>
          <ul>
            {validationErrors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </Card>
      ) : null}

      {validationSuccess ? (
        <p className="ai-task-playbook-builder__success">
          <CheckCircle2 aria-hidden="true" />
          Graf doğrulaması başarılı.
        </p>
      ) : null}

      <div className="ai-task-playbook-builder__footer">
        <Link className="ai-task-playbook-back-link" href="/ai-gorevleri/playbooklar">
          İptal
        </Link>
        <div className="ai-task-playbook-builder__footer-actions">
          <Button type="button" variant="secondary" onClick={handleValidate}>
            Grafiği Doğrula
          </Button>
          {canSave ? (
            <Button disabled={submitting} type="button" onClick={handleSave}>
              {mode === "create" ? "Oluştur" : "Kaydet"}
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}
