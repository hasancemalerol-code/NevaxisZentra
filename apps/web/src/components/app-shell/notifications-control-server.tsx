import { unstable_noStore as noStore } from "next/cache";

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { readTeamsNotifications } from "@/features/teams/server/teams-queries";

import { NotificationsControl } from "./notifications-control";

export async function NotificationsControlServer() {
  noStore();

  const effectiveAccess = await fetchCurrentEffectiveAccess();
  let unreadCount: number | null = null;
  if (hasGrantedPermission(effectiveAccess, "teams.notifications.read")) {
    const notificationsResult = await readTeamsNotifications();
    if (notificationsResult.status === "success") {
      unreadCount = notificationsResult.value.unreadCount;
    }
  }

  return <NotificationsControl unreadCount={unreadCount} />;
}
