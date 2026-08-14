"use client";

import { Bell } from "lucide-react";

import { IconButton } from "@/components/ui/button";

const NOTIFICATIONS_UNAVAILABLE_REASON =
  "Bildirim merkezi henüz kullanıma hazır değil.";

export function NotificationsControl() {
  return (
    <IconButton
      aria-describedby="shell-notifications-status"
      className="shell-notifications"
      disabled
      label={NOTIFICATIONS_UNAVAILABLE_REASON}
      title={NOTIFICATIONS_UNAVAILABLE_REASON}
      type="button"
    >
      <Bell aria-hidden="true" />
      <span className="visually-hidden" id="shell-notifications-status">
        {NOTIFICATIONS_UNAVAILABLE_REASON}
      </span>
    </IconButton>
  );
}
