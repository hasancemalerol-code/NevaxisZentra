"use client";

import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  AI_TASK_AUTOMATION_ACTION_TYPES,
  AI_TASK_AUTOMATION_CONDITION_FIELDS,
  AI_TASK_AUTOMATION_CONDITION_OPERATORS,
  AI_TASK_AUTOMATION_TRIGGER_TYPES,
  type AiTaskAutomationAction,
  type AiTaskAutomationCondition,
  type AiTaskAutomationRuleDetail,
  type CreateAiTaskAutomationRuleInput,
  type UpdateAiTaskAutomationRuleInput,
} from "../domain/ai-task-automation";
import {
  AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES,
  AI_TASK_POLICY_LIBRARY_SLA_POLICIES,
} from "../domain/ai-task-policy-library";
import { AI_TASK_PRIORITIES } from "../domain/ai-task";
import { AI_TASK_AUTOMATION_TEAM_CHANNELS } from "../presentation/ai-task-automation-entity-lookup";
import {
  getClosurePolicyDisplayName,
  getSlaPolicyDisplayName,
} from "../presentation/ai-task-automation-presentation";
import {
  getAiTaskAutomationActionTypeLabel,
  getAiTaskAutomationConditionFieldLabel,
  getAiTaskAutomationConditionOperatorLabel,
  getAiTaskAutomationTriggerLabel,
} from "../presentation/ai-task-automation-labels";
import { getAiTaskPriorityLabel } from "../presentation/ai-task-labels";
import {
  AiTaskDetailField,
  AiTaskDetailInput,
  AiTaskDetailSectionCard,
  AiTaskDetailSelect,
  AiTaskDetailTextarea,
} from "./ai-task-detail-form-controls";

type AiTaskAutomationRuleBuilderProps = Readonly<{
  mode: "create" | "edit";
  initialRule?: AiTaskAutomationRuleDetail | null;
  onSaveCreate?: (input: CreateAiTaskAutomationRuleInput) => Promise<void>;
  onSaveUpdate?: (input: UpdateAiTaskAutomationRuleInput) => Promise<void>;
  onCancel: () => void;
}>;

function emptyCondition(): AiTaskAutomationCondition {
  return { field: "taskCategory", operator: "equals", value: "", values: null };
}

function emptyAction(): AiTaskAutomationAction {
  return {
    type: "setPriority",
    value: "medium",
    guidValue: AI_TASK_AUTOMATION_TEAM_CHANNELS[0]?.id ?? null,
    intValue: null,
    boolValue: null,
    values: null,
  };
}

function slugifyKey(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
}

function needsListOperator(operator: string): boolean {
  return operator === "in" || operator === "not-in";
}

function needsBoolAction(type: string): boolean {
  return type === "requireEvidence" || type === "requireClosureApproval" || type === "enableFourEyes";
}

function needsPolicyKeyAction(type: string): boolean {
  return type === "applySlaPolicy" || type === "applyClosurePolicy";
}

function needsPriorityAction(type: string): boolean {
  return type === "setPriority";
}

function needsTeamChannelAction(type: string): boolean {
  return type === "assignToTeam" || type === "routeToQueue" || type === "notifyTeamsChannel";
}

function needsTagAction(type: string): boolean {
  return type === "addTaskTag";
}

export function AiTaskAutomationRuleBuilder({
  mode,
  initialRule,
  onSaveCreate,
  onSaveUpdate,
  onCancel,
}: AiTaskAutomationRuleBuilderProps) {
  const [name, setName] = useState(initialRule?.name ?? "");
  const [description, setDescription] = useState(initialRule?.description ?? "");
  const [ruleKey, setRuleKey] = useState(initialRule?.ruleKey ?? "");
  const [triggerType, setTriggerType] = useState(initialRule?.triggerType ?? "task-created");
  const [sortOrder, setSortOrder] = useState(initialRule?.sortOrder ?? 100);
  const [enabled, setEnabled] = useState(initialRule?.enabled ?? true);
  const [changeReason, setChangeReason] = useState("");
  const [conditions, setConditions] = useState<readonly AiTaskAutomationCondition[]>(
    initialRule?.conditions.length ? initialRule.conditions : [emptyCondition()],
  );
  const [actions, setActions] = useState<readonly AiTaskAutomationAction[]>(
    initialRule?.actions.length ? initialRule.actions : [emptyAction()],
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function updateCondition(index: number, patch: Partial<AiTaskAutomationCondition>) {
    setConditions((current) =>
      current.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    );
  }

  function updateAction(index: number, patch: Partial<AiTaskAutomationAction>) {
    setActions((current) =>
      current.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    );
  }

  async function handleSubmit() {
    setError(null);

    if (!name.trim()) {
      setError("Kural adı zorunludur.");
      return;
    }

    if (mode === "create" && !ruleKey.trim()) {
      setError("Kural anahtarı zorunludur.");
      return;
    }

    if (mode === "edit" && !changeReason.trim()) {
      setError("Değişiklik nedeni zorunludur.");
      return;
    }

    setSubmitting(true);
    try {
      if (mode === "create" && onSaveCreate) {
        await onSaveCreate({
          ruleKey: ruleKey.trim() || slugifyKey(name),
          name: name.trim(),
          description: description.trim() || null,
          triggerType,
          sortOrder,
          enabled,
          conditions,
          actions,
        });
      } else if (mode === "edit" && onSaveUpdate && initialRule) {
        await onSaveUpdate({
          expectedVersion: initialRule.version,
          name: name.trim(),
          description: description.trim() || null,
          triggerType,
          sortOrder,
          enabled,
          conditions,
          actions,
          changeReason: changeReason.trim(),
        });
      }
    } catch {
      setError("Kural kaydedilemedi.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="ai-task-automation-builder" data-testid="ai-task-automation-rule-builder">
      <AiTaskDetailSectionCard
        eyebrow="Kural tanımı"
        title={mode === "create" ? "Yeni otomasyon kuralı" : "Kuralı düzenle"}
        actions={
          <div className="ai-task-automation-builder__actions">
            <Button type="button" variant="secondary" onClick={onCancel}>
              İptal
            </Button>
            <Button disabled={submitting} type="button" onClick={handleSubmit}>
              {submitting ? "Kaydediliyor…" : "Kaydet"}
            </Button>
          </div>
        }
      >
        <div className="ai-task-automation-builder__meta">
          <AiTaskDetailField label="Kural adı">
            <AiTaskDetailInput
              value={name}
              onChange={(event) => {
                setName(event.target.value);
                if (mode === "create" && !ruleKey) {
                  setRuleKey(slugifyKey(event.target.value));
                }
              }}
            />
          </AiTaskDetailField>

          {mode === "create" ? (
            <AiTaskDetailField
              helper="Teknik anahtar — kullanıcı arayüzünde ikincil metadata"
              label="Kural anahtarı"
            >
              <AiTaskDetailInput value={ruleKey} onChange={(event) => setRuleKey(event.target.value)} />
            </AiTaskDetailField>
          ) : null}

          <AiTaskDetailField label="Açıklama">
            <AiTaskDetailTextarea value={description} onChange={(event) => setDescription(event.target.value)} rows={2} />
          </AiTaskDetailField>

          <AiTaskDetailField label="Sıra / öncelik">
            <AiTaskDetailInput
              type="number"
              value={sortOrder}
              onChange={(event) => setSortOrder(Number(event.target.value))}
            />
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

          {mode === "edit" ? (
            <AiTaskDetailField label="Değişiklik nedeni">
              <AiTaskDetailTextarea
                value={changeReason}
                onChange={(event) => setChangeReason(event.target.value)}
                rows={2}
              />
            </AiTaskDetailField>
          ) : null}
        </div>

        <div className="ai-task-automation-builder__pipeline">
          <section className="ai-task-automation-builder__stage ai-task-automation-builder__stage--when">
            <div className="ai-task-automation-builder__stage-heading">
              <Badge className="ai-task-automation-builder__stage-badge" tone="ai">WHEN</Badge>
              <h3>Tetikleyici</h3>
            </div>
            <div className="ai-task-automation-builder__workflow-row ai-task-automation-builder__workflow-row--single">
              <span className="ai-task-automation-builder__workflow-label">Tetikleyici</span>
              <AiTaskDetailSelect
                value={triggerType}
                onChange={(event) => setTriggerType(event.target.value)}
              >
                {AI_TASK_AUTOMATION_TRIGGER_TYPES.map((trigger) => (
                  <option key={trigger} value={trigger}>
                    {getAiTaskAutomationTriggerLabel(trigger)}
                  </option>
                ))}
              </AiTaskDetailSelect>
            </div>
          </section>

          <section className="ai-task-automation-builder__stage ai-task-automation-builder__stage--if">
            <div className="ai-task-automation-builder__stage-header">
              <div className="ai-task-automation-builder__stage-heading">
                <Badge className="ai-task-automation-builder__stage-badge" tone="information">IF</Badge>
                <h3>Koşullar</h3>
              </div>
              <Button
                leadingIcon={<Plus />}
                type="button"
                variant="secondary"
                onClick={() => setConditions((current) => [...current, emptyCondition()])}
              >
                Koşul ekle
              </Button>
            </div>

            <div className="ai-task-automation-builder__workflow-rows">
              {conditions.map((condition, index) => (
                <div key={index} className="ai-task-automation-builder__workflow-row">
                  <span className="ai-task-automation-builder__workflow-label">Alan</span>
                  <AiTaskDetailSelect
                    value={condition.field}
                    onChange={(event) => updateCondition(index, { field: event.target.value })}
                  >
                    {AI_TASK_AUTOMATION_CONDITION_FIELDS.map((field) => (
                      <option key={field} value={field}>
                        {getAiTaskAutomationConditionFieldLabel(field)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>

                  <span className="ai-task-automation-builder__workflow-label">Operatör</span>
                  <AiTaskDetailSelect
                    value={condition.operator}
                    onChange={(event) => updateCondition(index, { operator: event.target.value })}
                  >
                    {AI_TASK_AUTOMATION_CONDITION_OPERATORS.map((operator) => (
                      <option key={operator} value={operator}>
                        {getAiTaskAutomationConditionOperatorLabel(operator)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>

                  <span className="ai-task-automation-builder__workflow-label">Değer</span>
                  {needsListOperator(condition.operator) ? (
                    <AiTaskDetailInput
                      value={(condition.values ?? []).join(", ")}
                      onChange={(event) =>
                        updateCondition(index, {
                          values: event.target.value
                            .split(",")
                            .map((part) => part.trim())
                            .filter(Boolean),
                          value: null,
                        })
                      }
                    />
                  ) : (
                    <AiTaskDetailInput
                      value={condition.value ?? ""}
                      onChange={(event) =>
                        updateCondition(index, { value: event.target.value, values: null })
                      }
                    />
                  )}

                  <Button
                    aria-label="Koşulu kaldır"
                    className="ai-task-automation-builder__row-remove"
                    leadingIcon={<Trash2 />}
                    type="button"
                    variant="quiet"
                    onClick={() =>
                      setConditions((current) => current.filter((_, i) => i !== index))
                    }
                  />
                </div>
              ))}
            </div>
          </section>

          <section className="ai-task-automation-builder__stage ai-task-automation-builder__stage--then">
            <div className="ai-task-automation-builder__stage-header">
              <div className="ai-task-automation-builder__stage-heading">
                <Badge className="ai-task-automation-builder__stage-badge" tone="success">THEN</Badge>
                <h3>Aksiyonlar</h3>
              </div>
              <Button
                leadingIcon={<Plus />}
                type="button"
                variant="secondary"
                onClick={() => setActions((current) => [...current, emptyAction()])}
              >
                Aksiyon ekle
              </Button>
            </div>

            <div className="ai-task-automation-builder__workflow-rows">
              {actions.map((action, index) => (
                <div key={index} className="ai-task-automation-builder__workflow-row">
                  <span className="ai-task-automation-builder__workflow-label">Aksiyon</span>
                  <AiTaskDetailSelect
                    value={action.type}
                    onChange={(event) => {
                      const nextType = event.target.value;
                      updateAction(index, {
                        type: nextType,
                        guidValue: needsTeamChannelAction(nextType)
                          ? (action.guidValue ?? AI_TASK_AUTOMATION_TEAM_CHANNELS[0]?.id ?? null)
                          : action.guidValue,
                      });
                    }}
                  >
                    {AI_TASK_AUTOMATION_ACTION_TYPES.map((type) => (
                      <option key={type} value={type}>
                        {getAiTaskAutomationActionTypeLabel(type)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>

                  <span className="ai-task-automation-builder__workflow-label">Değer</span>
                  {needsPriorityAction(action.type) ? (
                    <AiTaskDetailSelect
                      value={action.value ?? "medium"}
                      onChange={(event) => updateAction(index, { value: event.target.value })}
                    >
                      {AI_TASK_PRIORITIES.map((priority) => (
                        <option key={priority} value={priority}>
                          {getAiTaskPriorityLabel(priority)}
                        </option>
                      ))}
                    </AiTaskDetailSelect>
                  ) : null}

                  {needsPolicyKeyAction(action.type) ? (
                    <AiTaskDetailSelect
                      value={action.value ?? ""}
                      onChange={(event) => updateAction(index, { value: event.target.value })}
                    >
                      {action.type === "applySlaPolicy"
                        ? AI_TASK_POLICY_LIBRARY_SLA_POLICIES.map((policy) => (
                            <option key={policy.policyKey} value={policy.policyKey}>
                              {getSlaPolicyDisplayName(policy.policyKey)}
                            </option>
                          ))
                        : AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES.map((policy) => (
                            <option key={policy.policyKey} value={policy.policyKey}>
                              {getClosurePolicyDisplayName(policy.policyKey)}
                            </option>
                          ))}
                    </AiTaskDetailSelect>
                  ) : null}

                  {needsTeamChannelAction(action.type) ? (
                    <AiTaskDetailSelect
                      value={action.guidValue ?? ""}
                      onChange={(event) => updateAction(index, { guidValue: event.target.value })}
                    >
                      {AI_TASK_AUTOMATION_TEAM_CHANNELS.map((team) => (
                        <option key={team.id} value={team.id}>
                          {team.label}
                        </option>
                      ))}
                    </AiTaskDetailSelect>
                  ) : null}

                  {needsBoolAction(action.type) ? (
                    <AiTaskDetailSelect
                      value={action.boolValue ? "true" : "false"}
                      onChange={(event) =>
                        updateAction(index, { boolValue: event.target.value === "true" })
                      }
                    >
                      <option value="true">Evet</option>
                      <option value="false">Hayır</option>
                    </AiTaskDetailSelect>
                  ) : null}

                  {needsTagAction(action.type) ? (
                    <AiTaskDetailInput
                      placeholder="Etiket adı"
                      value={action.value ?? ""}
                      onChange={(event) => updateAction(index, { value: event.target.value })}
                    />
                  ) : null}

                  {!needsPriorityAction(action.type)
                    && !needsPolicyKeyAction(action.type)
                    && !needsBoolAction(action.type)
                    && !needsTeamChannelAction(action.type)
                    && !needsTagAction(action.type) ? (
                      <AiTaskDetailInput
                        placeholder="Değer"
                        value={action.value ?? ""}
                        onChange={(event) => updateAction(index, { value: event.target.value })}
                      />
                    ) : null}

                  <Button
                    aria-label="Aksiyonu kaldır"
                    className="ai-task-automation-builder__row-remove"
                    leadingIcon={<Trash2 />}
                    type="button"
                    variant="quiet"
                    onClick={() => setActions((current) => current.filter((_, i) => i !== index))}
                  />
                </div>
              ))}
            </div>
          </section>
        </div>

        {error ? (
          <p className="ai-task-automation-builder__error" role="alert">{error}</p>
        ) : null}
      </AiTaskDetailSectionCard>
    </div>
  );
}
