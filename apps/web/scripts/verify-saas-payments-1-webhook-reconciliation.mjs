#!/usr/bin/env node
import { randomUUID } from "node:crypto";
import {
  assert,
  CLOSURE_PREFIX,
  DEV_OPEN_INVOICE_ID,
  TENANT_ID,
  computeWebhookSignature,
  previewClosureFixtures,
  readProviderMode,
  removeClosureFixtures,
  signInOwner,
  api,
  db,
} from "./saas-payments-1-verifier-lib.mjs";

console.log("Running SAAS-PAYMENTS-1 webhook-reconciliation verifier.");
previewClosureFixtures();
removeClosureFixtures();

const owner = await signInOwner();
const mode = await readProviderMode(owner);

if (mode === "not-configured") {
  console.log("Provider not configured — webhook reconciliation scenarios skipped.");
} else {
  const invoice = await api("GET", `/api/saas-billing/invoices/${DEV_OPEN_INVOICE_ID}`, undefined, owner);
  const init = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: invoice.json?.version ?? 1,
    idempotencyKey: `${CLOSURE_PREFIX}-reconcile-init`,
  }, owner);
  assert("checkout init for reconciliation", [200, 201].includes(init.status), `status=${init.status}`);
  const paymentAttemptId = init.json?.paymentAttemptId;
  const conversationId = db(`SELECT conversation_id FROM saas_billing_checkout_sessions WHERE payment_attempt_id = '${paymentAttemptId}'`).trim();
  assert("checkout conversation id present", Boolean(conversationId), `paymentAttemptId=${paymentAttemptId}`);
  const attemptAmount = db(`SELECT amount FROM saas_billing_payment_attempts WHERE id = '${paymentAttemptId}'`).trim();
  const attemptCurrency = db(`SELECT currency_code FROM saas_billing_payment_attempts WHERE id = '${paymentAttemptId}'`).trim();
  const successReference = `${CLOSURE_PREFIX}-event-${randomUUID()}`;

  const successBody = JSON.stringify({
    iyziEventType: "CHECKOUT_FORM_AUTH",
    status: "SUCCESS",
    paymentConversationId: conversationId,
    iyziPaymentId: "mock-payment-1",
    token: "closure-token-success",
    price: String(attemptAmount),
    currency: attemptCurrency || "TRY",
    iyziReferenceCode: successReference,
  });
  const signature = computeWebhookSignature(successBody);
  const webhook = await api("POST", "/api/saas-payments/iyzico/webhook", JSON.parse(successBody), undefined, {
    "X-IYZ-SIGNATURE-V3": signature,
  });
  assert("valid signed webhook accepted", webhook.status === 200, `status=${webhook.status}`);

  const replay = await api("POST", "/api/saas-payments/iyzico/webhook", JSON.parse(successBody), undefined, {
    "X-IYZ-SIGNATURE-V3": signature,
  });
  assert("duplicate webhook replay acknowledged", replay.status === 200, `status=${replay.status}`);

  const mismatchBody = JSON.stringify({
    iyziEventType: "CHECKOUT_FORM_AUTH",
    status: "SUCCESS",
    paymentConversationId: conversationId,
    iyziPaymentId: "mock-payment-2",
    token: "closure-token-mismatch",
    price: "0.01",
    currency: "TRY",
    iyziReferenceCode: `${CLOSURE_PREFIX}-event-mismatch-${randomUUID()}`,
  });
  const mismatchSig = computeWebhookSignature(mismatchBody);
  const mismatch = await api("POST", "/api/saas-payments/iyzico/webhook", JSON.parse(mismatchBody), undefined, {
    "X-IYZ-SIGNATURE-V3": mismatchSig,
  });
  assert("amount mismatch handled", [200, 422].includes(mismatch.status), `status=${mismatch.status}`);
}

removeClosureFixtures();
const orphanSessions = Number(db(`SELECT COUNT(*) FROM saas_billing_checkout_sessions WHERE conversation_id LIKE '${CLOSURE_PREFIX}%'`));
const orphanEvents = Number(db(`SELECT COUNT(*) FROM saas_billing_provider_events WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
const orphanPayments = Number(db(`SELECT COUNT(*) FROM saas_billing_payment_attempts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("fixture cleanup sessions", orphanSessions === 0, `count=${orphanSessions}`);
assert("fixture cleanup events", orphanEvents === 0, `count=${orphanEvents}`);
assert("fixture cleanup payments", orphanPayments === 0, `count=${orphanPayments}`);
console.log("SAAS-PAYMENTS-1 webhook-reconciliation verifier passed.");
