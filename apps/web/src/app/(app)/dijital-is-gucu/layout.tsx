import type { ReactNode } from "react";

import { DigitalWorkforceShell } from "@/features/digital-workforce/components/digital-workforce-shell";
import {
  readDigitalWorkforceActivity,
  readDigitalWorkforceApprovals,
  readDigitalWorkforceControlCenter,
} from "@/features/digital-workforce/server/digital-workforce-reader";

export default async function DigitalWorkforceLayout({ children }: Readonly<{ children: ReactNode }>) {
  const [hub, approvals, activity] = await Promise.all([
    readDigitalWorkforceControlCenter(),
    readDigitalWorkforceApprovals(),
    readDigitalWorkforceActivity(),
  ]);

  const apiError =
    hub.status === "failure"
      ? hub.message
      : approvals.status === "failure"
        ? approvals.message
        : activity.status === "failure"
          ? activity.message
          : null;

  return (
    <DigitalWorkforceShell
      apiError={apiError}
      initialActivity={activity.status === "success" ? activity.activity : []}
      initialApprovals={approvals.status === "success" ? approvals.approvals : []}
      initialSnapshot={hub.status === "success" ? hub.snapshot : null}
    >
      {children}
    </DigitalWorkforceShell>
  );
}
