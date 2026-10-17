"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  AlarmClock,
  ArrowRightLeft,
  CirclePlay,
  Hand,
  Pencil,
  RefreshCw,
  ShieldAlert,
  UserPlus,
  Users,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

import {
  AI_TASK_PRIORITIES,
  type AiTask,
  type AiTaskEscalationBreachType,
  type AiTaskPriority,
} from "../domain/ai-task";
import {
  canExecuteAiTask,
  getAllowedAiTaskTransitions,
} from "../domain/ai-task-policy";
import {
  AI_TASK_PRIORITY_TONES,
  getAiTaskPriorityLabel,
} from "../presentation/ai-task-labels";
import {
  AI_TASK_ESCALATION_STATE_TONES,
  AI_TASK_SLA_STATE_TONES,
  getAiTaskEscalationBadgeLabel,
  getAiTaskEscalationStateLabel,
  getAiTaskSlaDisplayText,
  getAiTaskResolutionDeadlineSummary,
  getAiTaskSlaBreachLabel,
} from "../presentation/ai-task-sla-labels";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import type {
  AiTaskAssignmentCandidate,
  AiTaskOperationCapabilities,
  AiTaskTeamChannelOption,
} from "../server/ai-task-queries";
import {
  acknowledgeAiTaskAction,
  assignAiTaskAction,
  changeAiTaskDueDateAction,
  changeAiTaskPriorityAction,
  escalateAiTaskAction,
  transitionAiTaskAction,
} from "../server/ai-task-actions";
import {
  AiTaskDetailField,
  AiTaskDetailInput,
  AiTaskDetailSectionCard,
  AiTaskDetailSelect,
  AiTaskDetailTextarea,
  AiTaskDetailValueRow,
} from "./ai-task-detail-form-controls";
import { customerMessage } from "@/lib/presentation/customer-message";

type AiTaskOperationsPanelClientProps = Readonly<{
  task: AiTask;
  version: number;
  operations: AiTaskOperationCapabilities;
  teamChannelLabels: Readonly<Record<string, string>>;
}>;

function toDateTimeLocalValue(value: string | undefined): string {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}

function resolveAssigneeInitials(label: string | null | undefined): string {
  if (!label) {
    return "?";
  }

  const parts = label.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase();
  }

  return `${parts[0]![0] ?? ""}${parts[parts.length - 1]![0] ?? ""}`.toUpperCase();
}

export function AiTaskOperationsPanelClient({
  task,
  version,
  operations,
  teamChannelLabels,
}: AiTaskOperationsPanelClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [assignReason, setAssignReason] = useState("");
  const [selectedMembershipId, setSelectedMembershipId] = useState("");
  const [selectedTeamChannelId, setSelectedTeamChannelId] = useState("");
  const [selectedPriority, setSelectedPriority] = useState<AiTaskPriority>(task.priority);
  const [dueAtLocal, setDueAtLocal] = useState(toDateTimeLocalValue(task.dueAt));
  const [escalationNote, setEscalationNote] = useState("");

  const [showUserAssign, setShowUserAssign] = useState(false);
  const [showTeamAssign, setShowTeamAssign] = useState(false);
  const [showPriorityEdit, setShowPriorityEdit] = useState(false);
  const [showDueDateEdit, setShowDueDateEdit] = useState(false);
  const [showEscalationForm, setShowEscalationForm] = useState(false);

  const allowedTransitions = getAllowedAiTaskTransitions(task.status);
  const executionDecision = canExecuteAiTask(task);

  const isFinal =
    task.status === "completed"
    || task.status === "rejected"
    || task.status === "cancelled";

  const hasAssignment = Boolean(
    task.assignedMembershipId || task.assignedTeamChannelId || task.assignee,
  );

  const teamLabel = task.assignedTeamChannelId
    ? teamChannelLabels[task.assignedTeamChannelId] ?? "Ekip kanalı"
    : null;

  const membershipCandidates = useMemo(
    () => operations.membershipCandidates.filter(
      (candidate) => candidate.tenantMembershipId !== operations.currentMembershipId,
    ),
    [operations.currentMembershipId, operations.membershipCandidates],
  );

  const canAssignToSelf = operations.canAssign
    && Boolean(operations.currentMembershipId)
    && task.assignedMembershipId !== operations.currentMembershipId;

  const canAssignToUser = operations.canAssign && membershipCandidates.length > 0;
  const canAssignToTeam = operations.canAssign && operations.teamChannels.length > 0;
  const canReassign = operations.canAssign && hasAssignment;
  const canAcknowledge = operations.canAcknowledge && !task.acknowledgedAtUtc && hasAssignment;
  const canStart = operations.canReview
    && executionDecision.allowed
    && allowedTransitions.includes("in-progress");
  const canChangePriority = operations.canManage;
  const canChangeDueDate = operations.canManage;
  const canEscalate = operations.canEscalate && !isFinal;

  const slaDueAt = task.sla?.resolutionDueAt
    ?? task.sla?.responseDueAt
    ?? task.sla?.acknowledgmentDueAt
    ?? task.dueAt;
  const slaDisplay = getAiTaskSlaDisplayText(
    task.slaState,
    slaDueAt,
    undefined,
    task.sla?.breachType,
  )
  const resolutionDeadline = getAiTaskResolutionDeadlineSummary(
    task.slaState,
    task.sla?.resolutionDueAt ?? task.dueAt,
  );
  const escalationBadge = task.escalationLevel > 0
    ? getAiTaskEscalationBadgeLabel(task.escalationLevel)
    : null;

  const latestEscalationNote = task.auditTrail
    .find((event) => event.eventType === "escalated")?.note ?? null;

  function runMutation(action: () => Promise<{ status: string; message?: string }>) {
    setErrorMessage(null);

    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setErrorMessage(result.message ?? "İşlem başarısız oldu.");
        return;
      }

      setShowUserAssign(false);
      setShowTeamAssign(false);
      setShowPriorityEdit(false);
      setShowDueDateEdit(false);
      setShowEscalationForm(false);
      router.refresh();
    });
  }

  function requireAssignReason(): string | null {
    const reason = assignReason.trim();
    if (!reason) {
      setErrorMessage("Atama notu zorunludur.");
      return null;
    }

    return reason;
  }

  function assignToMembership(
    targetMembershipId: string,
    source?: string,
  ) {
    const reason = requireAssignReason();
    if (!reason) {
      return;
    }

    runMutation(() => assignAiTaskAction({
      taskId: task.id,
      expectedVersion: version,
      targetMembershipId,
      targetTeamChannelId: null,
      reason,
      source: source ?? null,
    }));
  }

  function assignToTeam(targetTeamChannelId: string) {
    const reason = requireAssignReason();
    if (!reason) {
      return;
    }

    runMutation(() => assignAiTaskAction({
      taskId: task.id,
      expectedVersion: version,
      targetMembershipId: null,
      targetTeamChannelId,
      reason,
    }));
  }

  const hasAnyOperations = operations.canAssign
    || operations.canAcknowledge
    || operations.canManage
    || operations.canEscalate
    || operations.canReview;

  return (
    <aside
      className="ai-task-detail-sidebar"
      data-testid="ai-task-operations-panel"
    >
      {errorMessage ? (
        <div className="ai-task-detail-sidebar__alert" role="alert">
          <AlarmClock aria-hidden="true" />
          <span>{customerMessage(errorMessage)}</span>
        </div>
      ) : null}

      <div className="ai-task-detail-anchor" id="ai-task-assignment">
      <AiTaskDetailSectionCard title="Sorumlu">
        <div className="ai-task-detail-sidebar__owner">
          <span aria-hidden="true" className="ai-task-detail-sidebar__avatar">
            {resolveAssigneeInitials(task.assignee?.displayName ?? teamLabel)}
          </span>
          <div>
            <strong>
              {task.assignee?.displayName ?? teamLabel ?? "Atanmamış"}
            </strong>
            {teamLabel && task.assignee ? (
              <p className="ai-task-detail-sidebar__meta">
                <Users aria-hidden="true" />
                {teamLabel}
              </p>
            ) : null}
          </div>
        </div>

        {task.assignmentReason ? (
          <p className="ai-task-detail-sidebar__note">
            Son atama notu: {task.assignmentReason}
          </p>
        ) : null}

        {(canAssignToSelf || canAssignToUser || canAssignToTeam || canReassign) ? (
          <AiTaskDetailField
            helper="Atama veya yeniden atama nedeni"
            label="Atama notu"
          >
            <AiTaskDetailInput
              disabled={isPending}
              onChange={(event) => setAssignReason(event.target.value)}
              placeholder="Atama veya yeniden atama nedeni"
              type="text"
              value={assignReason}
            />
          </AiTaskDetailField>
        ) : null}

        <div className="ai-task-detail-sidebar__controls">
          {canAssignToSelf ? (
            <Button
              disabled={isPending}
              leadingIcon={<UserPlus />}
              onClick={() => {
                if (!operations.currentMembershipId) {
                  return;
                }

                assignToMembership(operations.currentMembershipId, "manual");
              }}
              size="small"
              variant="secondary"
            >
              Bana ata
            </Button>
          ) : null}

          {canAssignToUser ? (
            <Button
              disabled={isPending}
              leadingIcon={<UserPlus />}
              onClick={() => setShowUserAssign((current) => !current)}
              size="small"
              variant="secondary"
            >
              Kullanıcıya Ata
            </Button>
          ) : null}

          {canAssignToTeam ? (
            <Button
              disabled={isPending}
              leadingIcon={<Users />}
              onClick={() => setShowTeamAssign((current) => !current)}
              size="small"
              variant="secondary"
            >
              Ekibe Ata
            </Button>
          ) : null}

          {canReassign ? (
            <Button
              disabled={isPending}
              leadingIcon={<ArrowRightLeft />}
              onClick={() => {
                if (operations.currentMembershipId) {
                  assignToMembership(operations.currentMembershipId, "reassignment");
                  return;
                }

                if (selectedMembershipId) {
                  assignToMembership(selectedMembershipId, "reassignment");
                  return;
                }

                if (selectedTeamChannelId) {
                  assignToTeam(selectedTeamChannelId);
                }
              }}
              size="small"
              variant="secondary"
            >
              Yeniden ata
            </Button>
          ) : null}
        </div>

        {showUserAssign && canAssignToUser ? (
          <div className="ai-task-detail-sidebar__expand">
            <AiTaskDetailField label="Kullanıcı">
              <AiTaskDetailSelect
                disabled={isPending}
                onChange={(event) => setSelectedMembershipId(event.target.value)}
                value={selectedMembershipId}
              >
                <option value="">Kullanıcı seçin</option>
                {membershipCandidates.map((candidate: AiTaskAssignmentCandidate) => (
                  <option
                    key={candidate.tenantMembershipId}
                    value={candidate.tenantMembershipId}
                  >
                    {candidate.displayLabel}
                  </option>
                ))}
              </AiTaskDetailSelect>
            </AiTaskDetailField>
            <Button
              disabled={isPending || !selectedMembershipId}
              leadingIcon={<UserPlus />}
              onClick={() => assignToMembership(selectedMembershipId)}
              size="small"
            >
              Ata
            </Button>
          </div>
        ) : null}

        {showTeamAssign && canAssignToTeam ? (
          <div className="ai-task-detail-sidebar__expand">
            <AiTaskDetailField label="Ekip kanalı">
              <AiTaskDetailSelect
                disabled={isPending}
                onChange={(event) => setSelectedTeamChannelId(event.target.value)}
                value={selectedTeamChannelId}
              >
                <option value="">Ekip kanalı seçin</option>
                {operations.teamChannels.map((channel: AiTaskTeamChannelOption) => (
                  <option key={channel.id} value={channel.id}>
                    {channel.name}
                  </option>
                ))}
              </AiTaskDetailSelect>
            </AiTaskDetailField>
            <Button
              disabled={isPending || !selectedTeamChannelId}
              leadingIcon={<Users />}
              onClick={() => assignToTeam(selectedTeamChannelId)}
              size="small"
            >
              Ekibe Ata
            </Button>
          </div>
        ) : null}
      </AiTaskDetailSectionCard>
      </div>

      <AiTaskDetailSectionCard
        actions={
          slaDisplay ? (
            <Badge tone={AI_TASK_SLA_STATE_TONES[task.slaState]}>
              {slaDisplay}
            </Badge>
          ) : null
        }
        title="SLA ve son tarih"
      >
        <AiTaskDetailValueRow
          label="SLA durumu"
          value={
            <Badge tone={AI_TASK_SLA_STATE_TONES[task.slaState]}>
              {getAiTaskSlaBreachLabel(task.slaState, task.sla?.breachType)}
            </Badge>
          }
        />
        {task.slaState !== "not-applicable" ? (
          <AiTaskDetailValueRow
            label="Onay hedefi"
            value={
              task.acknowledgedAtUtc ? (
                <Badge tone="success">Karşılandı</Badge>
              ) : task.sla?.acknowledgmentDueAt
                && Date.parse(task.sla.acknowledgmentDueAt) < Date.now() ? (
                  <Badge tone="danger">Aşıldı</Badge>
                ) : (
                  <Badge tone="neutral">Bekleniyor</Badge>
                )
            }
          />
        ) : null}
        <AiTaskDetailValueRow
          label="Çözüm hedefi"
          value={
            resolutionDeadline ? (
              <>
                {task.sla?.resolutionDueAt ? `${formatAiTaskShortDateTime(task.sla.resolutionDueAt)} · ` : ""}
                <Badge tone={resolutionDeadline.passed ? "danger" : "neutral"}>
                  {resolutionDeadline.passed
                    ? `${resolutionDeadline.value} gecikti`
                    : `${resolutionDeadline.value} kaldı`}
                </Badge>
              </>
            ) : (
              "SLA hedefi tanımlı değil"
            )
          }
        />
        <AiTaskDetailValueRow
          label="Teslim durumu"
          value={
            task.acknowledgedAtUtc
              ? `Teslim alındı · ${formatAiTaskShortDateTime(task.acknowledgedAtUtc)}`
              : hasAssignment
                ? "Teslim bekleniyor"
                : "Atama gerekli"
          }
        />
        <AiTaskDetailValueRow
          label="Son tarih"
          value={task.dueAt ? formatAiTaskShortDateTime(task.dueAt) : "Tanımlanmadı"}
        />

        {canChangeDueDate ? (
          <>
            {!showDueDateEdit ? (
              <Button
                disabled={isPending}
                leadingIcon={<Pencil />}
                onClick={() => setShowDueDateEdit(true)}
                size="small"
                variant="secondary"
              >
                Değiştir
              </Button>
            ) : (
              <div className="ai-task-detail-sidebar__expand">
                <AiTaskDetailField label="Yeni son tarih">
                  <AiTaskDetailInput
                    disabled={isPending}
                    onChange={(event) => setDueAtLocal(event.target.value)}
                    type="datetime-local"
                    value={dueAtLocal}
                  />
                </AiTaskDetailField>
                <div className="ai-task-detail-sidebar__controls">
                  <Button
                    disabled={isPending}
                    leadingIcon={<AlarmClock />}
                    onClick={() => runMutation(() => changeAiTaskDueDateAction({
                      taskId: task.id,
                      expectedVersion: version,
                      dueAtUtc: dueAtLocal ? new Date(dueAtLocal).toISOString() : null,
                    }))}
                    size="small"
                  >
                    Kaydet
                  </Button>
                  <Button
                    disabled={isPending}
                    onClick={() => {
                      setShowDueDateEdit(false);
                      setDueAtLocal(toDateTimeLocalValue(task.dueAt));
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

      <AiTaskDetailSectionCard
        actions={
          <Badge tone={AI_TASK_PRIORITY_TONES[task.priority]}>
            {getAiTaskPriorityLabel(task.priority)}
          </Badge>
        }
        title="Öncelik"
      >
        {!showPriorityEdit ? (
          <Button
            disabled={isPending || !canChangePriority}
            leadingIcon={<Pencil />}
            onClick={() => setShowPriorityEdit(true)}
            size="small"
            variant="secondary"
          >
            Önceliği değiştir
          </Button>
        ) : (
          <div className="ai-task-detail-sidebar__expand">
            <AiTaskDetailField label="Yeni öncelik">
              <AiTaskDetailSelect
                disabled={isPending}
                onChange={(event) => setSelectedPriority(event.target.value as AiTaskPriority)}
                value={selectedPriority}
              >
                {AI_TASK_PRIORITIES.map((priority) => (
                  <option key={priority} value={priority}>
                    {getAiTaskPriorityLabel(priority)}
                  </option>
                ))}
              </AiTaskDetailSelect>
            </AiTaskDetailField>
            <div className="ai-task-detail-sidebar__controls">
              <Button
                disabled={isPending || selectedPriority === task.priority}
                leadingIcon={<RefreshCw />}
                onClick={() => runMutation(() => changeAiTaskPriorityAction({
                  taskId: task.id,
                  expectedVersion: version,
                  priority: selectedPriority,
                }))}
                size="small"
              >
                Kaydet
              </Button>
              <Button
                disabled={isPending}
                onClick={() => {
                  setShowPriorityEdit(false);
                  setSelectedPriority(task.priority);
                }}
                size="small"
                variant="secondary"
              >
                Vazgeç
              </Button>
            </div>
          </div>
        )}
      </AiTaskDetailSectionCard>

      <AiTaskDetailSectionCard
        actions={
          escalationBadge ? (
            <Badge tone={AI_TASK_ESCALATION_STATE_TONES[task.escalationState]}>
              {escalationBadge}
            </Badge>
          ) : null
        }
        title="Üst seviyeye aktarım"
      >
        <AiTaskDetailValueRow
          label="Durum"
          value={getAiTaskEscalationStateLabel(task.escalationState)}
        />
        {latestEscalationNote ? (
          <AiTaskDetailValueRow label="Son not" value={latestEscalationNote} />
        ) : null}

        {canEscalate ? (
          !showEscalationForm ? (
            <Button
              disabled={isPending}
              leadingIcon={<ShieldAlert />}
              onClick={() => setShowEscalationForm(true)}
              size="small"
              variant="secondary"
            >
              Üst seviyeye aktar
            </Button>
          ) : (
            <div className="ai-task-detail-sidebar__expand">
              <AiTaskDetailField
                helper="Üst seviyeye aktarım gerekçesini kısa olarak belirtin"
                label="Üst seviyeye aktarım notu"
              >
                <AiTaskDetailTextarea
                  disabled={isPending}
                  onChange={(event) => setEscalationNote(event.target.value)}
                  placeholder="Üst seviyeye aktarım nedeni"
                  rows={2}
                  value={escalationNote}
                />
              </AiTaskDetailField>
              <div className="ai-task-detail-sidebar__controls">
                <Button
                  disabled={isPending}
                  leadingIcon={<ShieldAlert />}
                  onClick={() => runMutation(() => escalateAiTaskAction({
                    taskId: task.id,
                    expectedVersion: version,
                    breachType: "manual" satisfies AiTaskEscalationBreachType,
                    note: escalationNote.trim() || undefined,
                  }))}
                  size="small"
                  variant="danger"
                >
                  Üst seviyeye aktar
                </Button>
                <Button
                  disabled={isPending}
                  onClick={() => {
                    setShowEscalationForm(false);
                    setEscalationNote("");
                  }}
                  size="small"
                  variant="secondary"
                >
                  Vazgeç
                </Button>
              </div>
            </div>
          )
        ) : null}
      </AiTaskDetailSectionCard>

      {(canAcknowledge || canStart) ? (
      <AiTaskDetailSectionCard title="Sıradaki işlem">
        <div
          aria-label="Görev operasyon aksiyonları"
          className="ai-task-detail-sidebar__quick-actions"
        >
          {canAcknowledge ? (
            <Button
              disabled={isPending}
              leadingIcon={<Hand />}
              onClick={() => runMutation(() => acknowledgeAiTaskAction({
                taskId: task.id,
                expectedVersion: version,
              }))}
              size="small"
            >
              Üstlendim
            </Button>
          ) : null}

          {canStart ? (
            <Button
              disabled={isPending}
              leadingIcon={<CirclePlay />}
              onClick={() => runMutation(() => transitionAiTaskAction({
                taskId: task.id,
                expectedVersion: version,
                targetStatus: "in-progress",
              }))}
              size="small"
            >
              Başlat
            </Button>
          ) : null}
        </div>
      </AiTaskDetailSectionCard>
      ) : null}

      {!hasAnyOperations ? (
        <p className="ai-task-detail-sidebar__footnote">
          Bu görev için operasyon yetkiniz bulunmuyor.
        </p>
      ) : null}
    </aside>
  );
}
