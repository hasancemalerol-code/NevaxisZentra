"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { TEAMS_APPROVAL_DECISIONS } from "@/features/teams/domain/teams-constants";
import {
  assignTeamsApprovalReviewerAction,
  cancelTeamsApprovalAction,
  decideTeamsApprovalAction,
} from "@/features/teams/server/teams-actions";

type TeamsApprovalDecisionFormProps = Readonly<{
  requestId: string;
  canDecide: boolean;
  canCancel: boolean;
  canManageReviewers: boolean;
  reviewerCandidates: ReadonlyArray<Readonly<{ tenantMembershipId: string; displayLabel: string }>>;
}>;

export function TeamsApprovalDecisionForm({
  requestId,
  canDecide,
  canCancel,
  canManageReviewers,
  reviewerCandidates,
}: TeamsApprovalDecisionFormProps) {
  const router = useRouter();
  const [note, setNote] = useState("");
  const [reviewerId, setReviewerId] = useState(reviewerCandidates[0]?.tenantMembershipId ?? "");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function run(action: () => Promise<{ status: "success" | "failure"; message?: string }>) {
    if (pending) return;
    setError(null);
    setPending(true);
    void (async () => {
      try {
        const result = await action();
        if (result.status === "failure") {
          setError(result.message ?? "İşlem başarısız.");
          return;
        }
        router.refresh();
      } finally {
        setPending(false);
      }
    })();
  }

  return (
    <div className="teams-task-draft-form" data-testid="teams-approval-decision-panel">
      {canDecide ? (
        <>
          <label>
            Karar notu (isteğe bağlı)
            <textarea
              data-testid="teams-approval-decision-note"
              disabled={pending}
              onChange={(event) => setNote(event.target.value)}
              rows={2}
              value={note}
            />
          </label>
          <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
            <Button
              data-testid="teams-approval-decide-approved"
              disabled={pending}
              onClick={() => run(() => decideTeamsApprovalAction({ requestId, decision: "approved", note }))}
              type="button"
            >
              {TEAMS_APPROVAL_DECISIONS.approved}
            </Button>
            <Button
              data-testid="teams-approval-decide-rejected"
              disabled={pending}
              onClick={() => run(() => decideTeamsApprovalAction({ requestId, decision: "rejected", note }))}
              type="button"
            >
              {TEAMS_APPROVAL_DECISIONS.rejected}
            </Button>
            <Button
              data-testid="teams-approval-decide-changes"
              disabled={pending}
              onClick={() => run(() => decideTeamsApprovalAction({ requestId, decision: "changes-requested", note }))}
              type="button"
            >
              {TEAMS_APPROVAL_DECISIONS["changes-requested"]}
            </Button>
          </div>
        </>
      ) : null}

      {canCancel ? (
        <Button
          data-testid="teams-approval-cancel"
          disabled={pending}
          onClick={() => run(() => cancelTeamsApprovalAction({ requestId }))}
          type="button"
        >
          İsteği iptal et
        </Button>
      ) : null}

      {canManageReviewers && reviewerCandidates.length > 0 ? (
        <div>
          <label>
            İnceleyen ekle
            <select
              data-testid="teams-approval-manage-reviewer"
              disabled={pending}
              onChange={(event) => setReviewerId(event.target.value)}
              value={reviewerId}
            >
              {reviewerCandidates.map((member) => (
                <option key={member.tenantMembershipId} value={member.tenantMembershipId}>
                  {member.displayLabel}
                </option>
              ))}
            </select>
          </label>
          <Button
            data-testid="teams-approval-assign-reviewer"
            disabled={pending || !reviewerId}
            onClick={() => run(() => assignTeamsApprovalReviewerAction({
              requestId,
              tenantMembershipId: reviewerId,
            }))}
            type="button"
          >
            İnceleyen ata
          </Button>
        </div>
      ) : null}

      {error ? (
        <p role="alert" style={{ color: "#b91c1c", margin: 0 }} data-testid="teams-approval-decision-error">
          {error}
        </p>
      ) : null}
    </div>
  );
}
