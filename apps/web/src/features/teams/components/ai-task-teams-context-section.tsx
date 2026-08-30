import Link from "next/link";
import { MessagesSquare } from "lucide-react";

import { Card } from "@/components/ui/card";
import {
  fetchCurrentEffectiveAccess,
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

  const access = await fetchCurrentEffectiveAccess();
  const canCreateApproval = hasGrantedPermission(access, "teams.approvals.create");

  return (
    <Card className="p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium">Teams operasyon bağlamı</p>
          <p className="mt-1 text-sm text-muted-foreground">
            {result.value.channelName}
          </p>
        </div>
        <div className="flex flex-col items-end gap-2">
          <Link
            href={result.value.teamsDeepLinkPath}
            className="inline-flex items-center gap-2 text-sm font-medium text-primary"
            data-testid="ai-task-teams-context-link"
          >
            <MessagesSquare aria-hidden="true" size={16} />
            Teams bağlamını aç
          </Link>
          {canCreateApproval ? (
            <Link
              href={`/teams/onaylar/yeni?channel=${encodeURIComponent(result.value.channelId)}&task=${encodeURIComponent(taskId)}`}
              className="inline-flex items-center gap-2 text-sm font-medium text-primary"
              data-testid="ai-task-request-approval"
            >
              Onay iste
            </Link>
          ) : null}
        </div>
      </div>
    </Card>
  );
}
