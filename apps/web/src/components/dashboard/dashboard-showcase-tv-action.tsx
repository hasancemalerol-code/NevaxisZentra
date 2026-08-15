import Link from "next/link";
import { Monitor } from "lucide-react";

import { evaluateSubscriptionAccess } from "@/features/saas-subscription/server/require-subscription-access";

import { DASHBOARD_SHOWCASE_TV_TARGET } from "./dashboard-navigation";

export async function DashboardShowcaseTvAction() {
  const access = await evaluateSubscriptionAccess(DASHBOARD_SHOWCASE_TV_TARGET.href);

  if (!access.allowed) {
    return null;
  }

  return (
    <Link
      aria-label="Fabrika vitrini TV görünümünü aç"
      className="command-center__showcase-tv-button"
      data-dashboard-showcase-tv-link="true"
      href={DASHBOARD_SHOWCASE_TV_TARGET.href}
      rel="noopener noreferrer"
      target="_blank"
    >
      <Monitor aria-hidden="true" size={16} strokeWidth={1.85} />
      <span>TV Görünümünü Aç</span>
    </Link>
  );
}
