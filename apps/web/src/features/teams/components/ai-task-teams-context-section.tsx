import Link from "next/link";
import { MessagesSquare } from "lucide-react";

import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { readTeamsTaskContextLink } from "@/features/teams/server/teams-queries";

type AiTaskTeamsContextSectionProps = Readonly<{
  taskId: string;
}>;

export async function AiTaskTeamsContextSection({ taskId }: AiTaskTeamsContextSectionProps) {
  const result = await readTeamsTaskContextLink(taskId);
  if (result.status === "failure") {
    return null;
  }

  const access = await fetchEffectiveAccessCapabilities();
  const canCreateApproval = hasGrantedPermission(access, "teams.approvals.create");

  return (
    <section className="zx-panel zx-inline-panel" aria-labelledby="ai-task-teams-context-heading">
      <div className="zx-inline-panel__copy">
        <h2 id="ai-task-teams-context-heading" className="zx-panel__title">Teams operasyon bağlamı</h2>
        <p className="zx-panel__subtitle">{result.value.channelName}</p>
      </div>
      <div className="zx-action-bar">
        <Link
          href={result.value.teamsDeepLinkPath}
          className="zx-button"
          data-testid="ai-task-teams-context-link"
        >
          <MessagesSquare aria-hidden="true" />
          Teams bağlamını aç
        </Link>
        {canCreateApproval ? (
          <Link
            href={`/teams/onaylar/yeni?channel=${encodeURIComponent(result.value.channelId)}&task=${encodeURIComponent(taskId)}`}
            className="zx-button"
            data-testid="ai-task-request-approval"
          >
            Onay iste
          </Link>
        ) : null}
      </div>
    </section>
  );
}
