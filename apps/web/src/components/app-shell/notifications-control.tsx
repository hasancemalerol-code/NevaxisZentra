"use client";

import Link from "next/link";
import { Bell } from "lucide-react";

type NotificationsControlProps = Readonly<{
  unreadCount?: number | null;
}>;

export function NotificationsControl({ unreadCount = null }: NotificationsControlProps) {
  const visibleCount = typeof unreadCount === "number" && unreadCount > 0 ? unreadCount : 0;
  const badgeLabel = visibleCount > 0
    ? `${visibleCount} okunmamış bildirim`
    : "Bildirim merkezi";

  return (
    <Link
      aria-describedby="shell-notifications-status"
      className="icon-button shell-notifications"
      data-shell-notifications-count={visibleCount}
      href="/teams/bildirimler"
      id="shell-notifications-link"
      title={badgeLabel}
    >
      <Bell aria-hidden="true" />
      {visibleCount > 0 ? (
        <span
          aria-hidden="true"
          className="shell-notifications-badge"
          data-testid="shell-notifications-unread-badge"
        >
          {visibleCount > 99 ? "99+" : visibleCount}
        </span>
      ) : null}
      <span className="visually-hidden" id="shell-notifications-status">
        {badgeLabel}
      </span>
    </Link>
  );
}
