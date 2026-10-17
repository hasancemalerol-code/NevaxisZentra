"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  acknowledgeTeamsHandoverAction,
  publishTeamsHandoverAction,
} from "@/features/teams/server/teams-actions";
import { customerMessage } from "@/lib/presentation/customer-message";
import { Notice } from "@/components/ui/notice";

type TeamsHandoverActionsProps = Readonly<{
  handoverId: string;
  canPublish: boolean;
  canAcknowledge: boolean;
}>;

export function TeamsHandoverActions({
  handoverId,
  canPublish,
  canAcknowledge,
}: TeamsHandoverActionsProps) {
  const router = useRouter();
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

  if (!canPublish && !canAcknowledge) {
    return null;
  }

  return (
    <div className="teams-task-draft-form" data-testid="teams-handover-actions">
      {canPublish ? (
        <Button
          data-testid="teams-handover-publish"
          disabled={pending}
          onClick={() => run(() => publishTeamsHandoverAction({ handoverId }))}
          type="button"
        >
          Vardiya devrini yayımla
        </Button>
      ) : null}

      {canAcknowledge ? (
        <Button
          data-testid="teams-handover-acknowledge"
          disabled={pending}
          onClick={() => run(() => acknowledgeTeamsHandoverAction({ handoverId }))}
          type="button"
        >
          Teslim aldım
        </Button>
      ) : null}

      {error ? (
        <Notice tone="danger" alert data-testid="teams-handover-actions-error">{customerMessage(error)}</Notice>
      ) : null}
    </div>
  );
}
