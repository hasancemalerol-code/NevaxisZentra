import type { ReactNode } from "react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TeamsSubNav } from "@/features/teams/components/teams-sub-nav";

export default async function TeamsLayout({ children }: Readonly<{ children: ReactNode }>) {
  const handover = await denyUnlessGranted("teams.handover.read");
  return (
    <>
      <div className="zx-subnav-bar">
        <TeamsSubNav canReadHandover={handover.status !== "failure"} />
      </div>
      {children}
    </>
  );
}
