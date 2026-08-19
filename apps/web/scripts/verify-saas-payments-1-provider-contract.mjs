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
  startMockIyzicoServer,
  api,
} from "./saas-payments-1-verifier-lib.mjs";

console.log("Running SAAS-PAYMENTS-1 provider-contract verifier.");
previewClosureFixtures();
removeClosureFixtures();
const mockServer = await startMockIyzicoServer();

const owner = await signInOwner();
const provider = await api("GET", "/api/saas-billing/provider-status", undefined, owner);
assert("provider status readable", provider.status === 200, `status=${provider.status}`);
assertNoSecretLeakage(provider.raw);

const mode = await readProviderMode(owner);
if (mode === "not-configured") {
  const denied = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: 1,
    idempotencyKey: `${CLOSURE_PREFIX}-not-configured`,
  }, owner);
  assert("not-configured checkout blocked", denied.status === 422, `status=${denied.status}`);
} else {
  const invoice = await api("GET", `/api/saas-billing/invoices/${DEV_OPEN_INVOICE_ID}`, undefined, owner);
  const init = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: invoice.json?.version ?? 1,
    idempotencyKey: `${CLOSURE_PREFIX}-contract-init`,
  }, owner);
  assert("sandbox checkout initialize", init.status === 201 || init.status === 200, `status=${init.status}`);
  assert("hosted checkout url present", Boolean(init.json?.hostedCheckoutUrl));
  assertNoSecretLeakage(init.raw);
}

mockServer.close();
removeClosureFixtures();
console.log("SAAS-PAYMENTS-1 provider-contract verifier passed.");
