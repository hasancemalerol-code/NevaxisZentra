"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  AlertTriangle,
  Check,
  ClipboardCheck,
  FileCheck2,
  FolderOpen,
  RefreshCw,
  RotateCcw,
  ShieldCheck,
  X,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import {
  AI_TASK_EVIDENCE_TYPES,
  AI_TASK_RESOLUTION_CODES,
  type AiTask,
  type AiTaskEvidenceType,
  type AiTaskResolutionCode,
} from "../domain/ai-task";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import {
  AI_TASK_CLOSURE_STATE_TONES,
  AI_TASK_CLOSURE_BADGE_LABELS,
  AI_TASK_CLOSURE_BADGE_TONES,
  getAiTaskClosureStateLabel,
  getAiTaskEvidenceTypeLabel,
  getAiTaskResolutionCodeLabel,
  resolveAiTaskClosureBadge,
} from "../presentation/ai-task-closure-labels";
import type { AiTaskOperationCapabilities } from "../server/ai-task-queries";
import {
  approveAiTaskClosureAction,
  rejectAiTaskClosureAction,
  reopenAiTaskAction,
  requestAiTaskClosureAction,
  submitAiTaskEvidenceAction,
  updateAiTaskResolutionAction,
} from "../server/ai-task-actions";
import {
  AiTaskDetailField,
  AiTaskDetailInput,
  AiTaskDetailSectionCard,
  AiTaskDetailSelect,
  AiTaskDetailTextarea,
  AiTaskDetailValueRow,
} from "./ai-task-detail-form-controls";

type AiTaskClosurePanelClientProps = Readonly<{
  task: AiTask;
  version: number;
  operations: AiTaskOperationCapabilities;
}>;

export function AiTaskClosurePanelClient({
  task,
  version,
  operations,
}: AiTaskClosurePanelClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const closure = task.closure;
  const closurePolicy = task.closurePolicy;
  const closureValidation = task.closureValidation;
  const evidenceItems = task.evidence ?? [];

  const [resolutionSummary, setResolutionSummary] = useState(
    closure?.resolutionSummary ?? "",
  );
  const [resolutionCode, setResolutionCode] = useState<AiTaskResolutionCode>(
    closure?.resolutionCode ?? "resolved",
  );
  const [closureReason, setClosureReason] = useState(
    closure?.closureReason ?? "",
  );
  const [closureNote, setClosureNote] = useState("");
  const [approvalNote, setApprovalNote] = useState("");
  const [rejectionReason, setRejectionReason] = useState("");
  const [reopenReason, setReopenReason] = useState("");

  const [evidenceType, setEvidenceType] = useState<AiTaskEvidenceType>("note");
  const [evidenceTitle, setEvidenceTitle] = useState("");
  const [evidenceDescription, setEvidenceDescription] = useState("");

  const [showRejectForm, setShowRejectForm] = useState(false);
  const [showReopenForm, setShowReopenForm] = useState(false);

  const closureState = closure?.state ?? "none";
  const closureBadgeKey = resolveAiTaskClosureBadge({
    closureState,
    evidenceCount: task.evidenceCount ?? evidenceItems.length,
    status: task.status,
    category: task.category,
    priority: task.priority,
    closureValidationIsValid: closureValidation?.isValid,
  });

  const canEditResolution = operations.canRequestClosure
    && closureState !== "closed"
    && closureState !== "awaiting-closure-approval";
  const canSubmitEvidence = operations.canManageEvidence
    && closureState !== "closed";
  const canRequestClosure = operations.canRequestClosure
    && closureValidation?.isValid
    && closureState !== "closed"
    && closureState !== "awaiting-closure-approval";
  const canApproveClosure = operations.canApproveClosure
    && closureState === "awaiting-closure-approval";
  const canRejectClosure = operations.canApproveClosure
    && closureState === "awaiting-closure-approval";
  const canReopen = operations.canReopen
    && (closureState === "closed" || closureState === "closure-rejected");

  const policySummary = useMemo(() => {
    if (!closurePolicy) {
      return null;
    }

    const parts = [
      closurePolicy.name,
      `Min. kanıt: ${closurePolicy.minimumEvidenceCount}`,
    ];

    if (closurePolicy.approvalRequired) {
      parts.push("Onay gerekli");
    }

    if (closurePolicy.resolutionSummaryRequired) {
      parts.push("Çözüm özeti zorunlu");
    }

    return parts.join(" · ");
  }, [closurePolicy]);

  function runMutation(action: () => Promise<{ status: string; message?: string }>) {
    setErrorMessage(null);

    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setErrorMessage(result.message ?? "İşlem başarısız oldu.");
        return;
      }

      setShowRejectForm(false);
      setShowReopenForm(false);
      setClosureNote("");
      setApprovalNote("");
      setRejectionReason("");
      setEvidenceTitle("");
      setEvidenceDescription("");
      router.refresh();
    });
  }

  return (
    <Card
      className="ai-task-closure-panel"
      data-closure-state={closureState}
      data-testid="ai-task-closure-panel"
    >
      <div className="ai-task-closure-panel__heading">
        <div>
          <p>Kapanış yönetişimi</p>
          <h2>Görev kapanışı</h2>
        </div>

        <div className="ai-task-closure-panel__badges">
          <Badge tone={AI_TASK_CLOSURE_STATE_TONES[closureState]}>
            {getAiTaskClosureStateLabel(closureState)}
          </Badge>

          {closureBadgeKey ? (
            <Badge tone={AI_TASK_CLOSURE_BADGE_TONES[closureBadgeKey]}>
              {AI_TASK_CLOSURE_BADGE_LABELS[closureBadgeKey]}
            </Badge>
          ) : null}
        </div>
      </div>

      {policySummary ? (
        <p className="ai-task-closure-panel__policy">{policySummary}</p>
      ) : null}

      {errorMessage ? (
        <div className="ai-task-closure-panel__alert" role="alert">
          <AlertTriangle aria-hidden="true" />
          <span>{errorMessage}</span>
        </div>
      ) : null}

      <div className="ai-task-closure-panel__sections">
        <AiTaskDetailSectionCard eyebrow="Resolution" title="Çözüm">
          <AiTaskDetailValueRow
            label="Çözüm kodu"
            value={
              closure?.resolutionCode
                ? getAiTaskResolutionCodeLabel(closure.resolutionCode)
                : "Tanımlanmadı"
            }
          />
          <AiTaskDetailValueRow
            label="Çözüm özeti"
            value={closure?.resolutionSummary ?? "Henüz kaydedilmedi"}
          />
          {closure?.closureReason ? (
            <AiTaskDetailValueRow
              label="Kapanış gerekçesi"
              value={closure.closureReason}
            />
          ) : null}

          {canEditResolution ? (
            <div className="ai-task-closure-panel__form">
              <AiTaskDetailField label="Çözüm kodu">
                <AiTaskDetailSelect
                  disabled={isPending}
                  onChange={(event) => setResolutionCode(event.target.value as AiTaskResolutionCode)}
                  value={resolutionCode}
                >
                  {AI_TASK_RESOLUTION_CODES.map((code) => (
                    <option key={code} value={code}>
                      {getAiTaskResolutionCodeLabel(code)}
                    </option>
                  ))}
                </AiTaskDetailSelect>
              </AiTaskDetailField>

              <AiTaskDetailField label="Çözüm özeti">
                <AiTaskDetailTextarea
                  disabled={isPending}
                  onChange={(event) => setResolutionSummary(event.target.value)}
                  placeholder="Uygulanan çözümü ve sonucu açıklayın"
                  rows={3}
                  value={resolutionSummary}
                />
              </AiTaskDetailField>

              <AiTaskDetailField label="Kapanış gerekçesi" helper="İsteğe bağlı">
                <AiTaskDetailTextarea
                  disabled={isPending}
                  onChange={(event) => setClosureReason(event.target.value)}
                  placeholder="Kapanış gerekçesi"
                  rows={2}
                  value={closureReason}
                />
              </AiTaskDetailField>

              <Button
                disabled={isPending || !resolutionSummary.trim()}
                leadingIcon={<ClipboardCheck />}
                onClick={() => runMutation(() => updateAiTaskResolutionAction({
                  taskId: task.id,
                  expectedVersion: version,
                  resolutionSummary: resolutionSummary.trim(),
                  resolutionCode,
                  closureReason: closureReason.trim() || undefined,
                }))}
                size="small"
              >
                Çözümü Kaydet
              </Button>
            </div>
          ) : null}
        </AiTaskDetailSectionCard>

        <AiTaskDetailSectionCard
          actions={
            <Badge tone="neutral">
              <FolderOpen aria-hidden="true" />
              {evidenceItems.length} kayıt
            </Badge>
          }
          eyebrow="Evidence"
          title="Kanıtlar"
        >
          {evidenceItems.length > 0 ? (
            <ul className="ai-task-closure-panel__evidence-list">
              {evidenceItems.map((item) => (
                <li className="ai-task-closure-panel__evidence-item" key={item.id}>
                  <div className="ai-task-closure-panel__evidence-top">
                    <strong>{item.title}</strong>
                    <Badge tone="neutral">
                      {getAiTaskEvidenceTypeLabel(item.evidenceType)}
                    </Badge>
                  </div>
                  {item.description ? <p>{item.description}</p> : null}
                  <small>
                    {item.submittedByLabel} · {formatAiTaskShortDateTime(item.submittedAt)}
                  </small>
                </li>
              ))}
            </ul>
          ) : (
            <p className="ai-task-closure-panel__empty">Henüz kanıt eklenmedi.</p>
          )}

          {canSubmitEvidence ? (
            <div className="ai-task-closure-panel__form">
              <AiTaskDetailField label="Kanıt türü">
                <AiTaskDetailSelect
                  disabled={isPending}
                  onChange={(event) => setEvidenceType(event.target.value as AiTaskEvidenceType)}
                  value={evidenceType}
                >
                  {AI_TASK_EVIDENCE_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {getAiTaskEvidenceTypeLabel(type)}
                    </option>
                  ))}
                </AiTaskDetailSelect>
              </AiTaskDetailField>

              <AiTaskDetailField label="Başlık">
                <AiTaskDetailInput
                  disabled={isPending}
                  onChange={(event) => setEvidenceTitle(event.target.value)}
                  placeholder="Kanıt başlığı"
                  value={evidenceTitle}
                />
              </AiTaskDetailField>

              <AiTaskDetailField label="Açıklama" helper="İsteğe bağlı">
                <AiTaskDetailTextarea
                  disabled={isPending}
                  onChange={(event) => setEvidenceDescription(event.target.value)}
                  placeholder="Kanıt açıklaması"
                  rows={2}
                  value={evidenceDescription}
                />
              </AiTaskDetailField>

              <Button
                disabled={isPending || !evidenceTitle.trim()}
                leadingIcon={<FileCheck2 />}
                onClick={() => runMutation(() => submitAiTaskEvidenceAction({
                  taskId: task.id,
                  expectedVersion: version,
                  evidenceType,
                  title: evidenceTitle.trim(),
                  description: evidenceDescription.trim() || undefined,
                }))}
                size="small"
              >
                Kanıt Ekle
              </Button>
            </div>
          ) : null}
        </AiTaskDetailSectionCard>

        <AiTaskDetailSectionCard eyebrow="Closure" title="Kapanış">
          {closureValidation && !closureValidation.isValid ? (
            <ul className="ai-task-closure-panel__issues">
              {closureValidation.issues.map((issue) => (
                <li key={`${issue.code}-${issue.message}`}>
                  <AlertTriangle aria-hidden="true" />
                  <span>{issue.message}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="ai-task-closure-panel__ready">
              <ShieldCheck aria-hidden="true" />
              Kapanış doğrulaması geçerli.
            </p>
          )}

          {closure?.requestedAt ? (
            <AiTaskDetailValueRow
              label="Talep zamanı"
              value={formatAiTaskShortDateTime(closure.requestedAt)}
            />
          ) : null}

          {canRequestClosure ? (
            <div className="ai-task-closure-panel__form">
              <AiTaskDetailField label="Kapanış notu" helper="İsteğe bağlı">
                <AiTaskDetailTextarea
                  disabled={isPending}
                  onChange={(event) => setClosureNote(event.target.value)}
                  placeholder="Kapanış talebi notu"
                  rows={2}
                  value={closureNote}
                />
              </AiTaskDetailField>

              <Button
                disabled={isPending}
                leadingIcon={<Check />}
                onClick={() => runMutation(() => requestAiTaskClosureAction({
                  taskId: task.id,
                  expectedVersion: version,
                  note: closureNote.trim() || undefined,
                }))}
                size="small"
              >
                Kapanış Talep Et
              </Button>
            </div>
          ) : null}
        </AiTaskDetailSectionCard>

        {(canApproveClosure || canRejectClosure || closure?.approvedAt || closure?.rejectedAt) ? (
          <AiTaskDetailSectionCard eyebrow="Approval" title="Onay">
            {closure?.approvedAt ? (
              <AiTaskDetailValueRow
                label="Onay zamanı"
                value={formatAiTaskShortDateTime(closure.approvedAt)}
              />
            ) : null}

            {closure?.rejectionReason ? (
              <div className="ai-task-closure-panel__decision-note">
                <strong>Red gerekçesi</strong>
                <p>{closure.rejectionReason}</p>
              </div>
            ) : null}

            {canApproveClosure || canRejectClosure ? (
              <div className="ai-task-closure-panel__actions">
                {canApproveClosure ? (
                  <>
                    <AiTaskDetailField label="Onay notu" helper="İsteğe bağlı">
                      <AiTaskDetailTextarea
                        disabled={isPending}
                        onChange={(event) => setApprovalNote(event.target.value)}
                        placeholder="Onay notu"
                        rows={2}
                        value={approvalNote}
                      />
                    </AiTaskDetailField>
                    <Button
                      disabled={isPending}
                      leadingIcon={<Check />}
                      onClick={() => runMutation(() => approveAiTaskClosureAction({
                        taskId: task.id,
                        expectedVersion: version,
                        note: approvalNote.trim() || undefined,
                      }))}
                      size="small"
                    >
                      Kapanışı Onayla
                    </Button>
                  </>
                ) : null}

                {canRejectClosure ? (
                  <>
                    {!showRejectForm ? (
                      <Button
                        disabled={isPending}
                        leadingIcon={<X />}
                        onClick={() => setShowRejectForm(true)}
                        size="small"
                        variant="secondary"
                      >
                        Revizyon İste
                      </Button>
                    ) : (
                      <div className="ai-task-closure-panel__form">
                        <AiTaskDetailField label="Red gerekçesi">
                          <AiTaskDetailTextarea
                            disabled={isPending}
                            onChange={(event) => setRejectionReason(event.target.value)}
                            placeholder="Revizyon gerekçesi"
                            rows={2}
                            value={rejectionReason}
                          />
                        </AiTaskDetailField>
                        <div className="ai-task-closure-panel__actions">
                          <Button
                            disabled={isPending || !rejectionReason.trim()}
                            leadingIcon={<X />}
                            onClick={() => runMutation(() => rejectAiTaskClosureAction({
                              taskId: task.id,
                              expectedVersion: version,
                              rejectionReason: rejectionReason.trim(),
                            }))}
                            size="small"
                            variant="secondary"
                          >
                            Reddet
                          </Button>
                          <Button
                            disabled={isPending}
                            onClick={() => {
                              setShowRejectForm(false);
                              setRejectionReason("");
                            }}
                            size="small"
                            variant="secondary"
                          >
                            Vazgeç
                          </Button>
                        </div>
                      </div>
                    )}
                  </>
                ) : null}
              </div>
            ) : null}
          </AiTaskDetailSectionCard>
        ) : null}

        {(canReopen || closure?.reopenedAt || closureState === "closed") ? (
          <AiTaskDetailSectionCard eyebrow="Reopen" title="Reopen">
            {closure?.reopenedAt ? (
              <AiTaskDetailValueRow
                label="Yeniden açılma"
                value={formatAiTaskShortDateTime(closure.reopenedAt)}
              />
            ) : null}

            {closure?.reopenReason ? (
              <div className="ai-task-closure-panel__decision-note">
                <strong>Yeniden açma gerekçesi</strong>
                <p>{closure.reopenReason}</p>
              </div>
            ) : null}

            {closure?.finalClosedAt ? (
              <AiTaskDetailValueRow
                label="Nihai kapanış"
                value={formatAiTaskShortDateTime(closure.finalClosedAt)}
              />
            ) : null}

            {canReopen ? (
              <>
                {!showReopenForm ? (
                  <Button
                    disabled={isPending}
                    leadingIcon={<RotateCcw />}
                    onClick={() => setShowReopenForm(true)}
                    size="small"
                    variant="secondary"
                  >
                    Görevi Yeniden Aç
                  </Button>
                ) : (
                  <div className="ai-task-closure-panel__form">
                    <AiTaskDetailField label="Yeniden açma gerekçesi">
                      <AiTaskDetailTextarea
                        disabled={isPending}
                        onChange={(event) => setReopenReason(event.target.value)}
                        placeholder="Yeniden açma gerekçesi"
                        rows={2}
                        value={reopenReason}
                      />
                    </AiTaskDetailField>
                    <div className="ai-task-closure-panel__actions">
                      <Button
                        disabled={isPending || !reopenReason.trim()}
                        leadingIcon={<RefreshCw />}
                        onClick={() => runMutation(() => reopenAiTaskAction({
                          taskId: task.id,
                          expectedVersion: version,
                          reopenReason: reopenReason.trim(),
                        }))}
                        size="small"
                      >
                        Yeniden Aç
                      </Button>
                      <Button
                        disabled={isPending}
                        onClick={() => {
                          setShowReopenForm(false);
                          setReopenReason("");
                        }}
                        size="small"
                        variant="secondary"
                      >
                        Vazgeç
                      </Button>
                    </div>
                  </div>
                )}
              </>
            ) : null}
          </AiTaskDetailSectionCard>
        ) : null}
      </div>
    </Card>
  );
}
