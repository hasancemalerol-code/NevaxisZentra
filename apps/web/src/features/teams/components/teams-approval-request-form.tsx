"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { TEAMS_APPROVAL_DISCLAIMER } from "@/features/teams/domain/teams-constants";
import { createTeamsApprovalAction } from "@/features/teams/server/teams-actions";

type ReviewerCandidate = Readonly<{
  tenantMembershipId: string;
  displayLabel: string;
}>;

type TeamsApprovalRequestFormProps = Readonly<{
  channelId: string;
  channelName: string;
  messageId?: string | null;
  aiTaskId?: string | null;
  sourcePreview: string;
  reviewerCandidates: readonly ReviewerCandidate[];
}>;

export function TeamsApprovalRequestForm({
  channelId,
  channelName,
  messageId = null,
  aiTaskId = null,
  sourcePreview,
  reviewerCandidates,
}: TeamsApprovalRequestFormProps) {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [rationale, setRationale] = useState("");
  const [selectedReviewers, setSelectedReviewers] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function toggleReviewer(membershipId: string) {
    setSelectedReviewers((current) =>
      current.includes(membershipId)
        ? current.filter((id) => id !== membershipId)
        : [...current, membershipId],
    );
  }

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setError(null);
    setPending(true);
    void (async () => {
      try {
        const result = await createTeamsApprovalAction({
          channelId,
          messageId,
          aiTaskId,
          title: title.trim(),
          rationale: rationale.trim(),
          reviewerMembershipIds: selectedReviewers,
        });
        if (result.status === "failure") {
          setError(result.message);
          return;
        }
        router.push(`/teams/onaylar/${result.requestId}`);
        router.refresh();
      } finally {
        setPending(false);
      }
    })();
  }

  return (
    <form
      className="teams-task-draft-form"
      data-testid="teams-approval-request-form"
      data-pending={pending ? "true" : "false"}
      onSubmit={submit}
    >
      <div className="teams-task-draft-form__source" data-testid="teams-approval-source-preview">
        <p style={{ margin: "0 0 0.35rem", fontSize: "0.8125rem", color: "#6b7280" }}>
          Kaynak bağlam — {channelName}
        </p>
        <p style={{ margin: 0, whiteSpace: "pre-wrap" }}>{sourcePreview}</p>
      </div>

      <label>
        Onay başlığı
        <input
          data-testid="teams-approval-title-input"
          disabled={pending}
          onChange={(event) => setTitle(event.target.value)}
          required
          type="text"
          value={title}
        />
      </label>

      <label>
        Gerekçe
        <textarea
          data-testid="teams-approval-rationale-input"
          disabled={pending}
          onChange={(event) => setRationale(event.target.value)}
          required
          rows={4}
          value={rationale}
        />
      </label>

      <fieldset>
        <legend>İnceleyenler</legend>
        {reviewerCandidates.length === 0 ? (
          <p style={{ color: "#6b7280" }}>Bu kanalda atanabilir inceleyen yok.</p>
        ) : (
          reviewerCandidates.map((member) => (
            <label key={member.tenantMembershipId} style={{ display: "flex", gap: "0.5rem", alignItems: "center" }}>
              <input
                checked={selectedReviewers.includes(member.tenantMembershipId)}
                data-testid={`teams-approval-reviewer-${member.tenantMembershipId}`}
                disabled={pending}
                onChange={() => toggleReviewer(member.tenantMembershipId)}
                type="checkbox"
              />
              {member.displayLabel}
            </label>
          ))
        )}
      </fieldset>

      <p className="teams-task-draft-form__disclaimer">{TEAMS_APPROVAL_DISCLAIMER}</p>

      {error ? (
        <p role="alert" style={{ color: "#b91c1c", margin: 0 }} data-testid="teams-approval-request-error">
          {error}
        </p>
      ) : null}

      <div>
        <Button
          data-testid="teams-approval-request-submit"
          disabled={pending || !title.trim() || !rationale.trim() || selectedReviewers.length === 0}
          type="submit"
        >
          Onay isteği oluştur
        </Button>
      </div>
    </form>
  );
}
