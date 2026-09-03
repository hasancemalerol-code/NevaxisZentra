"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  AlarmClock,
  ArrowRightLeft,
  CirclePlay,
  Hand,
  RefreshCw,
  UserPlus,
  Users,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

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
  getAiTaskPriorityLabel,
} from "../presentation/ai-task-labels";
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

  function runMutation(action: () => Promise<{ status: string; message?: string }>) {
    setErrorMessage(null);

    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setErrorMessage(result.message ?? "İşlem başarısız oldu.");
        return;
      }

      router.refresh();
    });
  }

  function requireAssignReason(): string | null {
    const reason = assignReason.trim();
    if (!reason) {
      setErrorMessage("Atama gerekçesi zorunludur.");
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

  return (
    <Card
      className="ai-task-operations-panel"
      data-testid="ai-task-operations-panel"
    >
      <div className="ai-task-operations-panel__heading">
        <div>
          <p>Operasyon</p>
          <h2>Sahiplik ve SLA</h2>
        </div>

        <div className="ai-task-operations-panel__states">
          {task.assignee ? (
            <Badge tone="information">{task.assignee.displayName}</Badge>
          ) : (
            <Badge tone="neutral">Atanmadı</Badge>
          )}

          {teamLabel ? (
            <Badge tone="neutral">
              <Users aria-hidden="true" />
              {teamLabel}
            </Badge>
          ) : null}

          {task.acknowledgedAtUtc ? (
            <Badge tone="success">
              <Hand aria-hidden="true" />
              Teslim alındı
            </Badge>
          ) : null}
        </div>
      </div>

      {task.assignmentReason ? (
        <p className="ai-task-operations-panel__description">
          Atama gerekçesi: {task.assignmentReason}
        </p>
      ) : null}

      {errorMessage ? (
        <div className="ai-task-operations-panel__warning" role="alert">
          <AlarmClock aria-hidden="true" />
          <span>{errorMessage}</span>
        </div>
      ) : null}

      {(canAssignToSelf || canAssignToUser || canAssignToTeam || canReassign) ? (
        <label className="ai-task-operations-panel__field">
          Atama gerekçesi
          <input
            disabled={isPending}
            onChange={(event) => setAssignReason(event.target.value)}
            placeholder="Atama veya yeniden atama gerekçesini yazın"
            type="text"
            value={assignReason}
          />
        </label>
      ) : null}

      <div
        aria-label="Görev operasyon aksiyonları"
        className="ai-task-operations-panel__actions"
      >
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
            variant="secondary"
          >
            Bana Ata
          </Button>
        ) : null}

        {canAssignToUser ? (
          <div className="ai-task-operations-panel__inline-form">
            <select
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
            </select>

            <Button
              disabled={isPending || !selectedMembershipId}
              leadingIcon={<UserPlus />}
              onClick={() => assignToMembership(selectedMembershipId)}
              variant="secondary"
            >
              Kullanıcıya Ata
            </Button>
          </div>
        ) : null}

        {canAssignToTeam ? (
          <div className="ai-task-operations-panel__inline-form">
            <select
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
            </select>

            <Button
              disabled={isPending || !selectedTeamChannelId}
              leadingIcon={<Users />}
              onClick={() => assignToTeam(selectedTeamChannelId)}
              variant="secondary"
            >
              Ekibe Ata
            </Button>
          </div>
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
            variant="secondary"
          >
            Yeniden Ata
          </Button>
        ) : null}

        {canAcknowledge ? (
          <Button
            disabled={isPending}
            leadingIcon={<Hand />}
            onClick={() => runMutation(() => acknowledgeAiTaskAction({
              taskId: task.id,
              expectedVersion: version,
            }))}
          >
            Teslim Aldım
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
          >
            Başlat
          </Button>
        ) : null}

        {canChangePriority ? (
          <div className="ai-task-operations-panel__inline-form">
            <select
              disabled={isPending}
              onChange={(event) => setSelectedPriority(event.target.value as AiTaskPriority)}
              value={selectedPriority}
            >
              {AI_TASK_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {getAiTaskPriorityLabel(priority)}
                </option>
              ))}
            </select>

            <Button
              disabled={isPending || selectedPriority === task.priority}
              leadingIcon={<RefreshCw />}
              onClick={() => runMutation(() => changeAiTaskPriorityAction({
                taskId: task.id,
                expectedVersion: version,
                priority: selectedPriority,
              }))}
              variant="secondary"
            >
              Öncelik Değiştir
            </Button>
          </div>
        ) : null}

        {canChangeDueDate ? (
          <div className="ai-task-operations-panel__inline-form">
            <input
              disabled={isPending}
              onChange={(event) => setDueAtLocal(event.target.value)}
              type="datetime-local"
              value={dueAtLocal}
            />

            <Button
              disabled={isPending}
              leadingIcon={<AlarmClock />}
              onClick={() => runMutation(() => changeAiTaskDueDateAction({
                taskId: task.id,
                expectedVersion: version,
                dueAtUtc: dueAtLocal ? new Date(dueAtLocal).toISOString() : null,
              }))}
              variant="secondary"
            >
              Son Tarih Değiştir
            </Button>
          </div>
        ) : null}

        {canEscalate ? (
          <div className="ai-task-operations-panel__inline-form">
            <input
              disabled={isPending}
              onChange={(event) => setEscalationNote(event.target.value)}
              placeholder="Eskalasyon notu (isteğe bağlı)"
              type="text"
              value={escalationNote}
            />

            <Button
              disabled={isPending}
              leadingIcon={<AlarmClock />}
              onClick={() => runMutation(() => escalateAiTaskAction({
                taskId: task.id,
                expectedVersion: version,
                breachType: "manual" satisfies AiTaskEscalationBreachType,
                note: escalationNote.trim() || undefined,
              }))}
              variant="secondary"
            >
              Eskale Et
            </Button>
          </div>
        ) : null}
      </div>

      {!operations.canAssign
        && !operations.canAcknowledge
        && !operations.canManage
        && !operations.canEscalate
        && !operations.canReview ? (
        <p className="ai-task-operations-panel__footnote">
          Bu görev için operasyon yetkiniz bulunmuyor.
        </p>
      ) : null}
    </Card>
  );
}
