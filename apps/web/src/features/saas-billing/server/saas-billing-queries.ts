import "./saas-billing-server-boundary";

import {
  fetchBillingInvoiceDetail,
  fetchBillingInvoices,
  fetchBillingPaymentAttempts,
  fetchBillingProviderStatus,
  fetchBillingWorkspace,
} from "./saas-billing-api-client";

export async function readBillingWorkspaceForAdmin() {
  const [workspaceResult, providerResult] = await Promise.all([
    fetchBillingWorkspace(),
    fetchBillingProviderStatus(),
  ]);

  if (workspaceResult.status === "failure") {
    return {
      status: "failure" as const,
      code: workspaceResult.code ?? "load-failed",
      message: workspaceResult.message,
      httpStatus: workspaceResult.httpStatus,
    };
  }

  return {
    status: "success" as const,
    workspace: workspaceResult.value,
    provider: providerResult.status === "success" ? providerResult.value : null,
    partialErrors: providerResult.status === "failure" ? [providerResult.message] : [],
  };
}

export {
  fetchBillingInvoiceDetail,
  fetchBillingInvoices,
  fetchBillingPaymentAttempts,
  fetchBillingProviderStatus,
  fetchBillingWorkspace,
};
