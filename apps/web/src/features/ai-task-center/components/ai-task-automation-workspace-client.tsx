"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  CheckCircle2,
  Clock3,
  History,
  Layers,
  Pencil,
  Play,
  Power,
  PowerOff,
  RefreshCw,
  ShieldCheck,
  Zap,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/states";

import type { AiTask, AiTaskEvidenceType } from "../domain/ai-task";
import type {
  AiTaskAutomationAction,
  AiTaskAutomationEvaluation,
  AiTaskAutomationExecution,
  AiTaskAutomationRuleDetail,
  AiTaskAutomationRuleRevision,
  AiTaskAutomationRuleSummary,
  CreateAiTaskAutomationRuleInput,
  UpdateAiTaskAutomationRuleInput,
} from "../domain/ai-task-automation";
import type {
  AiTaskPolicyLibraryClosureEntry,
  AiTaskPolicyLibrarySlaEntry,
} from "../domain/ai-task-policy-library";
import { AI_TASK_AUTOMATION_TRIGGER_TYPES } from "../domain/ai-task-automation";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import {
  formatAutomationActionChip,
  formatAutomationActionSummaryChip,
  formatAutomationConflictResolution,
  formatAutomationDurationMinutes,
  formatAutomationExecutionActionChip,
  formatAutomationOutcomeSummary,
  formatAutomationProposedFieldLabel,
  formatAutomationProposedValue,
  formatAutomationScopeLabel,
  getClosurePolicyDisplayName,
  getSlaPolicyDisplayName,
  type AiTaskAutomationActionChip,
} from "../presentation/ai-task-automation-presentation";
import { getAiTaskAutomationTriggerLabel } from "../presentation/ai-task-automation-labels";
import { getAiTaskEvidenceTypeLabel } from "../presentation/ai-task-closure-labels";
import { getAiTaskPriorityLabel } from "../presentation/ai-task-labels";
import {
  createAiTaskAutomationRuleAction,
  disableAiTaskAutomationRuleAction,
  enableAiTaskAutomationRuleAction,
  fetchAiTaskAutomationRuleDetailAction,
  fetchAiTaskAutomationRuleHistoryAction,
  simulateAiTaskAutomationAction,
  updateAiTaskAutomationRuleAction,
} from "../server/ai-task-automation-actions";
import type { AiTaskAutomationCapabilities } from "../server/ai-task-automation-queries";
import { AiTaskDetailField, AiTaskDetailSelect } from "./ai-task-detail-form-controls";
import { AiTaskAutomationRuleBuilder } from "./ai-task-automation-rule-builder";

type AiTaskAutomationWorkspaceClientProps = Readonly<{
  rules: readonly AiTaskAutomationRuleSummary[];
  executions: readonly AiTaskAutomationExecution[];
  taskOptions: readonly AiTask[];
  slaPolicies: readonly AiTaskPolicyLibrarySlaEntry[];
  closurePolicies: readonly AiTaskPolicyLibraryClosureEntry[];
  capabilities: AiTaskAutomationCapabilities;
}>;

type BuilderMode = "hidden" | "create" | "edit";

function ActionChip({ chip }: Readonly<{ chip: AiTaskAutomationActionChip }>) {
  return (
    <Badge className="ai-task-automation-chip" tone={chip.tone}>
      {chip.label}
    </Badge>
  );
}

function ActionChipList({
  chips,
}: Readonly<{ chips: readonly AiTaskAutomationActionChip[] }>) {
  if (chips.length === 0) {
    return <span className="ai-task-automation-muted">—</span>;
  }

  return (
    <div className="ai-task-automation-chip-list">
      {chips.map((chip, index) => (
        <ActionChip key={`${chip.label}-${index}`} chip={chip} />
      ))}
    </div>
  );
}

export function AiTaskAutomationWorkspaceClient({
  rules,
  executions,
  taskOptions,
  slaPolicies,
  closurePolicies,
  capabilities,
}: AiTaskAutomationWorkspaceClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [builderMode, setBuilderMode] = useState<BuilderMode>("hidden");
  const [editingRule, setEditingRule] = useState<AiTaskAutomationRuleDetail | null>(null);
  const [historyRuleId, setHistoryRuleId] = useState<string | null>(null);
  const [historyItems, setHistoryItems] = useState<readonly AiTaskAutomationRuleRevision[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const [simulateTaskId, setSimulateTaskId] = useState(taskOptions[0]?.id ?? "");
  const [simulateRuleId, setSimulateRuleId] = useState(rules[0]?.id ?? "");
  const [simulateTrigger, setSimulateTrigger] = useState<string>(AI_TASK_AUTOMATION_TRIGGER_TYPES[0]);
  const [simulationResult, setSimulationResult] = useState<AiTaskAutomationEvaluation | null>(null);
  const [simulationError, setSimulationError] = useState<string | null>(null);

  const taskTitleById = useMemo(
    () => new Map(taskOptions.map((task) => [task.id, task.title])),
    [taskOptions],
  );

  function refresh() {
    startTransition(() => {
      router.refresh();
    });
  }

  async function openEdit(ruleId: string) {
    setActionError(null);
    const result = await fetchAiTaskAutomationRuleDetailAction(ruleId);
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    setEditingRule(result.rule);
    setBuilderMode("edit");
    setHistoryRuleId(null);
  }

  async function openHistory(ruleId: string) {
    setActionError(null);
    setHistoryLoading(true);
    setHistoryRuleId(ruleId);
    setBuilderMode("hidden");
    const result = await fetchAiTaskAutomationRuleHistoryAction(ruleId);
    setHistoryLoading(false);
    if (result.status === "failure") {
      setActionError(result.message);
      setHistoryItems([]);
      return;
    }
    setHistoryItems(result.revisions);
  }

  async function handleEnable(rule: AiTaskAutomationRuleSummary) {
    setActionError(null);
    const result = await enableAiTaskAutomationRuleAction(rule.id, rule.version);
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    refresh();
  }

  async function handleDisable(rule: AiTaskAutomationRuleSummary) {
    setActionError(null);
    const result = await disableAiTaskAutomationRuleAction(rule.id, rule.version);
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    refresh();
  }

  async function handleSimulate() {
    setSimulationError(null);
    setSimulationResult(null);
    if (!simulateTaskId) {
      setSimulationError("Görev seçin.");
      return;
    }

    const result = await simulateAiTaskAutomationAction({
      taskId: simulateTaskId,
      ruleId: simulateRuleId || null,
      triggerType: simulateTrigger,
    });

    if (result.status === "failure") {
      setSimulationError(result.message);
      return;
    }

    setSimulationResult(result.evaluation);
  }

  async function handleCreate(input: CreateAiTaskAutomationRuleInput) {
    const result = await createAiTaskAutomationRuleAction(input);
    if (result.status === "failure") {
      setActionError(result.message);
      throw new Error(result.message);
    }
    setBuilderMode("hidden");
    refresh();
  }

  async function handleUpdate(input: UpdateAiTaskAutomationRuleInput) {
    if (!editingRule) return;
    const result = await updateAiTaskAutomationRuleAction(editingRule.id, input);
    if (result.status === "failure") {
      setActionError(result.message);
      throw new Error(result.message);
    }
    setBuilderMode("hidden");
    setEditingRule(null);
    refresh();
  }

  return (
    <div className="ai-task-automation-workspace" data-testid="ai-task-automation-workspace">
      {actionError ? (
        <p className="ai-task-automation-workspace__error" role="alert">{actionError}</p>
      ) : null}

      {builderMode === "create" && capabilities.canManage ? (
        <AiTaskAutomationRuleBuilder
          mode="create"
          onCancel={() => setBuilderMode("hidden")}
          onSaveCreate={handleCreate}
        />
      ) : null}

      {builderMode === "edit" && editingRule && capabilities.canManage ? (
        <AiTaskAutomationRuleBuilder
          initialRule={editingRule}
          mode="edit"
          onCancel={() => {
            setBuilderMode("hidden");
            setEditingRule(null);
          }}
          onSaveUpdate={handleUpdate}
        />
      ) : null}

      {historyRuleId ? (
        <Card className="ai-task-automation-history-panel" data-testid="ai-task-automation-history">
          <div className="ai-task-automation-history-panel__header">
            <h3>Kural revizyon geçmişi</h3>
            <Button type="button" variant="quiet" onClick={() => setHistoryRuleId(null)}>
              Kapat
            </Button>
          </div>
          {historyLoading ? (
            <p className="ai-task-automation-muted">Yükleniyor…</p>
          ) : historyItems.length === 0 ? (
            <EmptyState description="Bu kural için revizyon kaydı bulunamadı." title="Geçmiş boş" />
          ) : (
            <div className="ai-task-automation-activity">
              {historyItems.map((item) => (
                <article key={item.id} className="ai-task-automation-activity__item">
                  <div className="ai-task-automation-activity__marker" aria-hidden="true">
                    <History />
                  </div>
                  <div className="ai-task-automation-activity__body">
                    <div className="ai-task-automation-activity__header">
                      <strong>{item.name}</strong>
                      <Badge tone={item.enabled ? "success" : "neutral"}>
                        {item.enabled ? "Etkin" : "Devre Dışı"}
                      </Badge>
                      <span className="ai-task-automation-activity__meta">Sürüm {item.version}</span>
                    </div>
                    <p className="ai-task-automation-activity__detail">
                      {item.changeReason || "Değişiklik kaydı"}
                    </p>
                    <div className="ai-task-automation-activity__footer">
                      <span>{formatAiTaskShortDateTime(item.changedAtUtc)}</span>
                      <span>{item.changedBy}</span>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </Card>
      ) : null}

      <Card className="ai-task-automation-module" data-testid="ai-task-automation-rules-section">
        <section className="ai-task-automation-section">
          <div className="ai-task-automation-section__header">
            <div>
              <p className="ai-task-automation-module__eyebrow">Modül 1</p>
              <h2>Otomasyon Kuralları</h2>
              <p>Tetikleyici, koşul ve aksiyon tabanlı kuralları yönetin.</p>
            </div>
            <div className="ai-task-automation-section__actions">
              {capabilities.canManage ? (
                <Button
                  type="button"
                  onClick={() => {
                    setBuilderMode("create");
                    setEditingRule(null);
                    setHistoryRuleId(null);
                  }}
                >
                  Yeni Kural
                </Button>
              ) : null}
              <Button
                disabled={isPending}
                leadingIcon={<RefreshCw />}
                type="button"
                variant="quiet"
                onClick={refresh}
              >
                Yenile
              </Button>
            </div>
          </div>

          {rules.length === 0 ? (
            <EmptyState
              description="Henüz otomasyon kuralı tanımlanmadı."
              title="Kural bulunamadı"
            />
          ) : (
            <div className="ai-task-automation-rule-list">
              {rules.map((rule) => (
                <article
                  key={rule.id}
                  className={`ai-task-automation-rule-card${rule.enabled ? " ai-task-automation-rule-card--enabled" : ""}`}
                  data-testid={`ai-task-automation-rule-${rule.id}`}
                >
                  <div className="ai-task-automation-rule-card__main">
                    <div className="ai-task-automation-rule-card__title-row">
                      <div>
                        <h3 className="ai-task-automation-rule-card__name">{rule.name}</h3>
                        {rule.description ? (
                          <p className="ai-task-automation-rule-card__description">{rule.description}</p>
                        ) : null}
                      </div>
                      <Badge
                        className="ai-task-automation-rule-card__status"
                        tone={rule.enabled ? "success" : "neutral"}
                      >
                        {rule.enabled ? "Etkin" : "Devre Dışı"}
                      </Badge>
                    </div>

                    <div className="ai-task-automation-rule-card__meta-grid">
                      <div className="ai-task-automation-rule-card__meta-item">
                        <span className="ai-task-automation-rule-card__meta-label">Kapsam</span>
                        <span>{formatAutomationScopeLabel(Boolean(rule.facilityId))}</span>
                      </div>
                      <div className="ai-task-automation-rule-card__meta-item">
                        <span className="ai-task-automation-rule-card__meta-label">Tetikleyici</span>
                        <span>{getAiTaskAutomationTriggerLabel(rule.triggerType)}</span>
                      </div>
                      <div className="ai-task-automation-rule-card__meta-item">
                        <span className="ai-task-automation-rule-card__meta-label">Öncelik / sıra</span>
                        <span>{rule.sortOrder}</span>
                      </div>
                      <div className="ai-task-automation-rule-card__meta-item">
                        <span className="ai-task-automation-rule-card__meta-label">Son eşleşme</span>
                        <span>
                          {rule.lastMatchedAtUtc
                            ? formatAiTaskShortDateTime(rule.lastMatchedAtUtc)
                            : "—"}
                        </span>
                      </div>
                      <div className="ai-task-automation-rule-card__meta-item">
                        <span className="ai-task-automation-rule-card__meta-label">Son güncelleme</span>
                        <span>{formatAiTaskShortDateTime(rule.updatedAtUtc)}</span>
                      </div>
                    </div>

                    <div className="ai-task-automation-rule-card__actions-summary">
                      <span className="ai-task-automation-rule-card__meta-label">Aksiyon özeti</span>
                      <ActionChipList
                        chips={rule.actionSummaries.map((summary) =>
                          formatAutomationActionSummaryChip(summary),
                        )}
                      />
                    </div>
                  </div>

                  <div className="ai-task-automation-rule-card__toolbar">
                    {capabilities.canManage ? (
                      <Button
                        aria-label="Düzenle"
                        leadingIcon={<Pencil />}
                        type="button"
                        variant="secondary"
                        onClick={() => openEdit(rule.id)}
                      >
                        Düzenle
                      </Button>
                    ) : null}
                    {capabilities.canSimulate ? (
                      <Button
                        aria-label="Simüle Et"
                        leadingIcon={<Play />}
                        type="button"
                        variant="secondary"
                        onClick={() => {
                          setSimulateRuleId(rule.id);
                          setSimulateTrigger(rule.triggerType);
                        }}
                      >
                        Simüle Et
                      </Button>
                    ) : null}
                    {capabilities.canManage && !rule.enabled ? (
                      <Button
                        aria-label="Etkinleştir"
                        leadingIcon={<Power />}
                        type="button"
                        variant="secondary"
                        onClick={() => handleEnable(rule)}
                      >
                        Etkinleştir
                      </Button>
                    ) : null}
                    {capabilities.canManage && rule.enabled ? (
                      <Button
                        aria-label="Devre Dışı Bırak"
                        leadingIcon={<PowerOff />}
                        type="button"
                        variant="secondary"
                        onClick={() => handleDisable(rule)}
                      >
                        Devre Dışı Bırak
                      </Button>
                    ) : null}
                    <Button
                      aria-label="Geçmiş"
                      leadingIcon={<History />}
                      type="button"
                      variant="quiet"
                      onClick={() => openHistory(rule.id)}
                    >
                      Geçmiş
                    </Button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </Card>

      <Card className="ai-task-automation-module" data-testid="ai-task-automation-policies-section">
        <section className="ai-task-automation-section">
          <div className="ai-task-automation-section__header">
            <div>
              <p className="ai-task-automation-module__eyebrow">Modül 2</p>
              <h2>Politikalar</h2>
              <p>SLA ve kapanış politika kütüphanesi — otomasyon aksiyonlarında referans.</p>
            </div>
          </div>

          <div className="ai-task-automation-policies">
            <div className="ai-task-automation-policies__group">
              <h3 className="ai-task-automation-policies__group-title">
                <ShieldCheck aria-hidden="true" />
                SLA politikaları
              </h3>
              <div className="ai-task-automation-policy-cards">
                {slaPolicies.map((policy) => (
                  <Card key={policy.policyKey} className="ai-task-automation-policy-card">
                    <header className="ai-task-automation-policy-card__header">
                      <div>
                        <h4>{getSlaPolicyDisplayName(policy.policyKey)}</h4>
                        <code className="ai-task-automation-policy-card__key">{policy.policyKey}</code>
                      </div>
                      <Badge tone="ai">
                        {getAiTaskPriorityLabel(policy.priority as "low" | "medium" | "high" | "critical")}
                      </Badge>
                    </header>
                    <dl className="ai-task-automation-policy-card__kv">
                      <div>
                        <dt>Onay hedefi</dt>
                        <dd>{formatAutomationDurationMinutes(policy.acknowledgmentTargetMinutes)}</dd>
                      </div>
                      <div>
                        <dt>Yanıt hedefi</dt>
                        <dd>{formatAutomationDurationMinutes(policy.responseTargetMinutes)}</dd>
                      </div>
                      <div>
                        <dt>Çözüm hedefi</dt>
                        <dd>{formatAutomationDurationMinutes(policy.resolutionTargetMinutes)}</dd>
                      </div>
                    </dl>
                  </Card>
                ))}
              </div>
            </div>

            <div className="ai-task-automation-policies__group">
              <h3 className="ai-task-automation-policies__group-title">
                <CheckCircle2 aria-hidden="true" />
                Kapanış politikaları
              </h3>
              <div className="ai-task-automation-policy-cards">
                {closurePolicies.map((policy) => (
                  <Card key={policy.policyKey} className="ai-task-automation-policy-card">
                    <header className="ai-task-automation-policy-card__header">
                      <div>
                        <h4>{getClosurePolicyDisplayName(policy.policyKey)}</h4>
                        <code className="ai-task-automation-policy-card__key">{policy.policyKey}</code>
                      </div>
                    </header>
                    <dl className="ai-task-automation-policy-card__kv">
                      <div>
                        <dt>Kanıt gereksinimi</dt>
                        <dd>{policy.minimumEvidenceCount} adet</dd>
                      </div>
                      <div>
                        <dt>Kanıt türleri</dt>
                        <dd>
                          {policy.requiredEvidenceTypes.length > 0
                            ? policy.requiredEvidenceTypes
                              .map((type) => getAiTaskEvidenceTypeLabel(type as AiTaskEvidenceType))
                              .join(", ")
                            : "—"}
                        </dd>
                      </div>
                      <div>
                        <dt>Onay gereksinimi</dt>
                        <dd>
                          <Badge tone={policy.approvalRequired ? "warning" : "neutral"}>
                            {policy.approvalRequired ? "Zorunlu" : "Yok"}
                          </Badge>
                        </dd>
                      </div>
                      <div>
                        <dt>Çift göz</dt>
                        <dd>
                          <Badge tone={policy.fourEyesRequired ? "warning" : "neutral"}>
                            {policy.fourEyesRequired ? "Zorunlu" : "Yok"}
                          </Badge>
                        </dd>
                      </div>
                    </dl>
                  </Card>
                ))}
              </div>
            </div>
          </div>
        </section>
      </Card>

      {capabilities.canSimulate ? (
        <Card className="ai-task-automation-module" data-testid="ai-task-automation-simulation-section">
          <section className="ai-task-automation-section">
            <div className="ai-task-automation-section__header">
              <div>
                <p className="ai-task-automation-module__eyebrow">Modül 3</p>
                <h2>Simülasyon</h2>
                <p>Görev ve kural seçerek eşleşmeleri ve önerilen değişiklikleri önizleyin.</p>
              </div>
            </div>

            <div className="ai-task-automation-simulation__banner">
              <Badge tone="ai">SİMÜLASYON</Badge>
              <span>Veri değiştirilmez</span>
            </div>

            <Card className="ai-task-automation-simulation__form">
              <div className="ai-task-automation-simulation__controls">
                <AiTaskDetailField label="Görev">
                  <AiTaskDetailSelect
                    value={simulateTaskId}
                    onChange={(event) => setSimulateTaskId(event.target.value)}
                  >
                    {taskOptions.map((task) => (
                      <option key={task.id} value={task.id}>
                        {task.title}
                      </option>
                    ))}
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <AiTaskDetailField label="Kural (isteğe bağlı)">
                  <AiTaskDetailSelect
                    value={simulateRuleId}
                    onChange={(event) => setSimulateRuleId(event.target.value)}
                  >
                    <option value="">Tüm kurallar</option>
                    {rules.map((rule) => (
                      <option key={rule.id} value={rule.id}>
                        {rule.name}
                      </option>
                    ))}
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <AiTaskDetailField label="Tetikleyici">
                  <AiTaskDetailSelect
                    value={simulateTrigger}
                    onChange={(event) => setSimulateTrigger(event.target.value)}
                  >
                    {AI_TASK_AUTOMATION_TRIGGER_TYPES.map((trigger) => (
                      <option key={trigger} value={trigger}>
                        {getAiTaskAutomationTriggerLabel(trigger)}
                      </option>
                    ))}
                  </AiTaskDetailSelect>
                </AiTaskDetailField>

                <Button leadingIcon={<Play />} type="button" variant="secondary" onClick={handleSimulate}>
                  Simülasyonu çalıştır
                </Button>
              </div>

              {simulationError ? (
                <p className="ai-task-automation-workspace__error" role="alert">{simulationError}</p>
              ) : null}

              {simulationResult ? (
                <div className="ai-task-automation-simulation__results">
                  <Card className="ai-task-automation-simulation__outcome">
                    <div className="ai-task-automation-simulation__outcome-label">
                      <Zap aria-hidden="true" />
                      <span>Nihai Simülasyon Sonucu</span>
                    </div>
                    <p>{formatAutomationOutcomeSummary(simulationResult.outcomeSummary)}</p>
                  </Card>

                  <div className="ai-task-automation-simulation__result-grid">
                    <Card className="ai-task-automation-simulation__panel">
                      <h4>
                        <Layers aria-hidden="true" />
                        Eşleşen Kurallar
                      </h4>
                      {simulationResult.matchedRules.length === 0 ? (
                        <p className="ai-task-automation-muted">Eşleşen kural yok.</p>
                      ) : (
                        <ul className="ai-task-automation-simulation__list">
                          {simulationResult.matchedRules.map((rule) => (
                            <li key={rule.ruleId}>
                              <strong>{rule.ruleName}</strong>
                              <span className="ai-task-automation-muted">
                                sürüm {rule.ruleVersion} · sıra {rule.sortOrder}
                              </span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </Card>

                    <Card className="ai-task-automation-simulation__panel">
                      <h4>Önerilen Değişiklikler</h4>
                      {simulationResult.proposedChanges.length === 0 ? (
                        <p className="ai-task-automation-muted">Önerilen değişiklik yok.</p>
                      ) : (
                        <ul className="ai-task-automation-simulation__change-list">
                          {simulationResult.proposedChanges.map((change, index) => (
                            <li key={index}>
                              <span className="ai-task-automation-simulation__change-field">
                                {formatAutomationProposedFieldLabel(change.field)}
                              </span>
                              <span className="ai-task-automation-simulation__change-values">
                                {formatAutomationProposedValue(change.field, change.currentValue)}
                                {" → "}
                                {formatAutomationProposedValue(change.field, change.proposedValue)}
                              </span>
                              {change.sourceRuleName ? (
                                <span className="ai-task-automation-muted">({change.sourceRuleName})</span>
                              ) : null}
                            </li>
                          ))}
                        </ul>
                      )}
                    </Card>

                    <Card className="ai-task-automation-simulation__panel">
                      <h4>Çakışmalar</h4>
                      {simulationResult.conflicts.length === 0 ? (
                        <p className="ai-task-automation-muted">Çakışma yok.</p>
                      ) : (
                        <ul className="ai-task-automation-simulation__change-list">
                          {simulationResult.conflicts.map((conflict, index) => (
                            <li key={index}>
                              <span className="ai-task-automation-simulation__change-field">
                                {formatAutomationProposedFieldLabel(conflict.field)}
                              </span>
                              <span className="ai-task-automation-simulation__change-values">
                                {conflict.competingValues
                                  .map((value) => formatAutomationProposedValue(conflict.field, value))
                                  .join(" · ")}
                              </span>
                              <span className="ai-task-automation-muted">
                                {formatAutomationConflictResolution(conflict.resolution)}
                              </span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </Card>

                    <Card className="ai-task-automation-simulation__panel">
                      <h4>Uygulanacak Aksiyonlar</h4>
                      {simulationResult.actionsApplied.length === 0 ? (
                        <p className="ai-task-automation-muted">Aksiyon yok.</p>
                      ) : (
                        <ActionChipList
                          chips={simulationResult.actionsApplied.map((action: AiTaskAutomationAction) =>
                            formatAutomationActionChip(action),
                          )}
                        />
                      )}
                    </Card>
                  </div>
                </div>
              ) : null}
            </Card>
          </section>
        </Card>
      ) : null}

      <Card className="ai-task-automation-module" data-testid="ai-task-automation-executions-section">
        <section className="ai-task-automation-section">
          <div className="ai-task-automation-section__header">
            <div>
              <p className="ai-task-automation-module__eyebrow">Modül 4</p>
              <h2>Çalışma Geçmişi</h2>
              <p>Otomasyon kuralı çalıştırma kayıtları.</p>
            </div>
          </div>

          {executions.length === 0 ? (
            <EmptyState description="Henüz çalıştırma kaydı yok." title="Geçmiş boş" />
          ) : (
            <div className="ai-task-automation-activity">
              {executions.map((execution) => (
                <article key={execution.id} className="ai-task-automation-activity__item">
                  <div className="ai-task-automation-activity__marker" aria-hidden="true">
                    <Clock3 />
                  </div>
                  <div className="ai-task-automation-activity__body">
                    <div className="ai-task-automation-activity__header">
                      <Link href={`/ai-gorevleri/${execution.taskId}`}>
                        {taskTitleById.get(execution.taskId) ?? "Görev"}
                      </Link>
                      <Badge tone={execution.simulated ? "information" : "success"}>
                        {execution.simulated ? "Simülasyon" : "Canlı"}
                      </Badge>
                    </div>
                    <p className="ai-task-automation-activity__detail">
                      <strong>{execution.ruleName}</strong>
                      {" · "}
                      {getAiTaskAutomationTriggerLabel(execution.triggerType)}
                    </p>
                    <div className="ai-task-automation-activity__chips">
                      <ActionChipList
                        chips={execution.actionsApplied.map((action) =>
                          formatAutomationExecutionActionChip(action),
                        )}
                      />
                    </div>
                    <div className="ai-task-automation-activity__footer">
                      <span>{formatAiTaskShortDateTime(execution.executedAtUtc)}</span>
                      <span className="ai-task-automation-muted">Sürüm {execution.ruleVersion}</span>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </Card>
    </div>
  );
}
