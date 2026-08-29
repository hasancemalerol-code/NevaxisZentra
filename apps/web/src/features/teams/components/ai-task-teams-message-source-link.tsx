import Link from "next/link";
import { ExternalLink } from "lucide-react";

import { readTeamsMessageSourceContext } from "@/features/teams/server/teams-queries";

type AiTaskTeamsMessageSourceLinkProps = Readonly<{
  messageId: string;
}>;

export async function AiTaskTeamsMessageSourceLink({ messageId }: AiTaskTeamsMessageSourceLinkProps) {
  const result = await readTeamsMessageSourceContext(messageId);
  if (result.status === "failure") {
    return (
      <p className="ai-task-source-reference__empty" data-testid="ai-task-teams-message-source-unavailable">
        Teams mesaj bağlamı şu anda kullanılamıyor.
      </p>
    );
  }

  const context = result.value;
  if (!context.canOpen) {
    return (
      <p className="ai-task-source-reference__empty" data-testid="ai-task-teams-message-source-unavailable">
        {context.unavailableReason ?? "Teams mesaj bağlamına erişiminiz yok."}
      </p>
    );
  }

  return (
    <Link
      href={context.teamsDeepLinkPath}
      data-testid="ai-task-teams-message-source-link"
    >
      <ExternalLink aria-hidden="true" />
      Teams bağlamını aç
    </Link>
  );
}
