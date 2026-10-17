"use client";

import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { RefreshCw } from "lucide-react";

import { refreshQualityNcrFollowUpWorkspaceAction } from "../server/quality-ncr-follow-up-actions";

/** Page-header action that reloads the NCR follow-up workspace. */
export function QualityNcrFollowUpRefreshButton() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  return (
    <button
      type="button"
      className="quality-dashboard-action quality-dashboard-action--secondary"
      disabled={isPending}
      data-testid="quality-ncr-follow-up-refresh-button"
      onClick={() =>
        startTransition(async () => {
          await refreshQualityNcrFollowUpWorkspaceAction();
          router.refresh();
        })
      }
    >
      <RefreshCw aria-hidden="true" />
      Yenile
    </button>
  );
}
