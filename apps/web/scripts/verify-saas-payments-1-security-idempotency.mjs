#!/usr/bin/env node
import {
  assert,
  assertNoSecretLeakage,
  CLOSURE_PREFIX,
  DEV_OPEN_INVOICE_ID,
  previewClosureFixtures,
  readProviderMode,
  removeClosureFixtures,
  signInOwner,
  api,
  requireEnv,
  CookieJar,
  ORGANIZATION,
} from "./saas-payments-1-verifier-lib.mjs";

console.log("Running SAAS-PAYMENTS-1 security-idempotency verifier.");
previewClosureFixtures();
removeClosureFixtures();

const missing = await api("GET", "/api/saas-billing/payments/checkout/initialize");
assert("unauthenticated checkout → 401", missing.status === 401, `status=${missing.status}`);

const salesPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const salesJar = new CookieJar();
await api("POST", "/api/identity/sign-in", {
  organizationContext: ORGANIZATION,
  email: "materials-sales@dev.local",
  password: salesPassword,
  deviceLabel: `${CLOSURE_PREFIX}-sales`,
}, salesJar);

const denied = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
  invoiceId: DEV_OPEN_INVOICE_ID,
  invoiceExpectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-denied`,
}, salesJar);
assert("missing manage permission → 403", denied.status === 403, `status=${denied.status}`);

const owner = await signInOwner();
const mode = await readProviderMode(owner);
if (mode !== "not-configured") {
  const invoice = await api("GET", `/api/saas-billing/invoices/${DEV_OPEN_INVOICE_ID}`, undefined, owner);
  const body = {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: invoice.json?.version ?? 1,
    idempotencyKey: `${CLOSURE_PREFIX}-idem`,
  };
  const [first, second] = await Promise.all([
    api("POST", "/api/saas-billing/payments/checkout/initialize", body, owner),
    api("POST", "/api/saas-billing/payments/checkout/initialize", body, owner),
  ]);
  assert("concurrent identical init succeeds", [200, 201].includes(first.status) && [200, 201].includes(second.status));
  assert("concurrent replay stable", first.json?.paymentAttemptId === second.json?.paymentAttemptId);

  const conflict = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    ...body,
    invoiceExpectedVersion: (invoice.json?.version ?? 1) + 99,
  }, owner);
  assert("different payload conflict → 409", conflict.status === 409, `status=${conflict.status}`);

  const legacy = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: invoice.json?.version ?? 1,
  }, owner);
  assert("legacy NULL fingerprint → 409", legacy.status === 409, `status=${legacy.status}`);
}

const invalidWebhook = await api("POST", "/api/saas-payments/iyzico/webhook", {
  iyziEventType: "CHECKOUT_FORM_AUTH",
  status: "SUCCESS",
  paymentConversationId: `${CLOSURE_PREFIX}-unknown`,
}, undefined, { "X-IYZ-SIGNATURE-V3": "invalid" });
assert("invalid webhook signature rejected", invalidWebhook.status === 401, `status=${invalidWebhook.status}`);
assertNoSecretLeakage(invalidWebhook.raw);

removeClosureFixtures();
console.log("SAAS-PAYMENTS-1 security-idempotency verifier passed.");
