"use server";

import {
  fetchBillingWorkspace,
  fetchBillingProviderStatus,
  initializeCardCheckout,
  verifyCheckoutReturn,
  fetchPaymentAttemptDetail,
  fetchBillingInvoiceDetail,
} from "./saas-billing-api-client";

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

export async function initializeCardCheckoutAction(
  invoiceId: string,
  invoiceExpectedVersion: number,
  idempotencyKey: string,
) {
  return initializeCardCheckout(invoiceId, invoiceExpectedVersion, idempotencyKey);
}

export async function verifyCheckoutReturnAction(
  paymentAttemptId: string,
  checkoutSessionToken: string,
  idempotencyKey: string,
) {
  return verifyCheckoutReturn(paymentAttemptId, checkoutSessionToken, idempotencyKey);
}

export async function loadPaymentAttemptDetailAction(paymentAttemptId: string) {
  return fetchPaymentAttemptDetail(paymentAttemptId);
}

export async function loadBillingInvoiceDetailAction(invoiceId: string) {
  return fetchBillingInvoiceDetail(invoiceId);
}
