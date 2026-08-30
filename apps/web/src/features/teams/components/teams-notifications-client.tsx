"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTransition } from "react";

import { Badge } from "@/components/ui/badge";
import type { RawTeamsNotificationItem } from "@/features/teams/server/teams-api-client";
import { markTeamsNotificationReadAction } from "@/features/teams/server/teams-actions";
import { TEAMS_NOTIFICATION_TYPES } from "@/features/teams/domain/teams-constants";

type TeamsNotificationsClientProps = Readonly<{
  items: readonly RawTeamsNotificationItem[];
  disclaimer: string;
}>;

const DATE_TIME = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "short",
  timeStyle: "short",
});

function typeLabel(notificationType: string): string {
  return TEAMS_NOTIFICATION_TYPES[notificationType as keyof typeof TEAMS_NOTIFICATION_TYPES] ?? notificationType;
}

export function TeamsNotificationsClient({ items, disclaimer }: TeamsNotificationsClientProps) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();

  function openNotification(item: RawTeamsNotificationItem) {
    startTransition(async () => {
      if (!item.readAt) {
        await markTeamsNotificationReadAction({ notificationId: item.id });
      }
      if (item.canOpen) {
        router.push(item.deepLinkPath);
      }
    });
  }

  return (
    <>
      <ul className="teams-approval-list" data-testid="teams-notifications-list">
        {items.map((item) => (
          <li key={item.id}>
            {item.canOpen ? (
              <button
                type="button"
                className="teams-notification-row"
                data-testid={`teams-notification-${item.id}`}
                disabled={pending}
                onClick={() => openNotification(item)}
              >
                <strong>{item.title}</strong>
                <span>{item.reason}</span>
                {item.sourcePreview ? <span>{item.sourcePreview}</span> : null}
                <span>{DATE_TIME.format(new Date(item.createdAt))}</span>
                <Badge tone={item.readAt ? "neutral" : "information"}>
                  {item.readAt ? "Okundu" : "Okunmadı"}
                </Badge>
                <Badge tone="neutral">{typeLabel(item.notificationType)}</Badge>
              </button>
            ) : (
              <div className="teams-notification-row is-unavailable" data-testid={`teams-notification-${item.id}`}>
                <strong>{item.title}</strong>
                <span>{item.unavailableReason ?? "Kaynak artık erişilebilir değil."}</span>
                <Badge tone="neutral">{typeLabel(item.notificationType)}</Badge>
              </div>
            )}
          </li>
        ))}
      </ul>
      <p className="teams-disclaimer" data-testid="teams-notifications-disclaimer">{disclaimer}</p>
      <p className="teams-empty-hint">
        <Link href="/teams">Teams çalışma alanına dön</Link>
      </p>
    </>
  );
}
