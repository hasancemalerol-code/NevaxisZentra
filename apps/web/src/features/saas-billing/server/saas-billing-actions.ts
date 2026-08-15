"use server";

import { fetchBillingWorkspace, fetchBillingProviderStatus } from "./saas-billing-api-client";

export async function loadBillingWorkspaceAction() {
  const [workspaceResult, providerResult] = await Promise.all([
    fetchBillingWorkspace(),
    fetchBillingProviderStatus(),
  ]);

  if (workspaceResult.status === "failure") {
    return workspaceResult;
  }

  return {
    status: "success" as const,
    workspace: workspaceResult.value,
    provider: providerResult.status === "success" ? providerResult.value : null,
  };
}
