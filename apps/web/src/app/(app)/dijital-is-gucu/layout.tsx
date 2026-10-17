import type { ReactNode } from "react";

import { DigitalWorkforceShell } from "@/features/digital-workforce/components/digital-workforce-shell";
import {
  readDigitalWorkforceActivity,
  readDigitalWorkforceApprovals,
  readDigitalWorkforceControlCenter,
} from "@/features/digital-workforce/server/digital-workforce-reader";
import { notFound } from "next/navigation";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

export default async function DigitalWorkforceLayout({ children }: Readonly<{ children: ReactNode }>) {
  // Deferred module: Production answers 404 for every Digital Workforce route (fail closed, nothing is fetched).
  if (!deferredSurfacesEnabled()) {
    notFound();
  }

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
