#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-BILLING-1-CLOSURE";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const DEV_BILLING_ACCOUNT_ID = "bb000003-00a0-4000-8000-0000000000a0";
const DEV_DRAFT_INVOICE_ID = "bb000003-00b0-4000-8000-0000000000b0";
const DEV_OPEN_INVOICE_ID = "bb000003-00b1-4000-8000-0000000000b1";
const DEV_PAID_INVOICE_ID = "bb000003-00b2-4000-8000-0000000000b2";
const DEV_OVERDUE_INVOICE_ID = "bb000003-00b3-4000-8000-0000000000b3";
const DEV_PAYMENT_AWAITING_ID = "bb000003-00c0-4000-8000-0000000000c0";
const DEV_INVOICE_IDS = [
  DEV_DRAFT_INVOICE_ID,
  DEV_OPEN_INVOICE_ID,
  DEV_PAID_INVOICE_ID,
  DEV_OVERDUE_INVOICE_ID,
];
const PERIOD_START = "2026-08-01T00:00:00.000Z";
const PERIOD_END = "2026-09-01T00:00:00.000Z";
const INVOICE_AMOUNT = 100;
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SAAS-BILLING-1 domain verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function closureInvoiceIdsSubquery() {
  const devIds = DEV_INVOICE_IDS.map((id) => `'${id}'`).join(", ");
  return `SELECT id FROM saas_billing_invoices WHERE tenant_id = '${TENANT_ID}' AND id NOT IN (${devIds})`;
}

function previewClosureFixtures() {
  const invoices = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE id IN (${closureInvoiceIdsSubquery()})`));
  const payments = Number(db(`
    SELECT COUNT(*) FROM saas_billing_payment_attempts
    WHERE invoice_id IN (${closureInvoiceIdsSubquery()})
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `));
  const idempotency = Number(db(`SELECT COUNT(*) FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  console.log(`Preview ${CLOSURE_PREFIX} domain fixtures: invoices=${invoices}, payments=${payments}, idempotency=${idempotency}`);
  return { invoices, payments, idempotency };
}

function restoreDevPilotBilling() {
  db(`
    UPDATE saas_billing_invoices
    SET invoice_status = 'draft',
        issued_at_utc = NULL,
        due_at_utc = NULL,
        paid_at_utc = NULL,
        voided_at_utc = NULL,
        version = 1,
        updated_at_utc = NOW()
    WHERE id = '${DEV_DRAFT_INVOICE_ID}';

    UPDATE saas_billing_invoices
    SET invoice_status = 'open',
        issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00',
        due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00',
        paid_at_utc = NULL,
        voided_at_utc = NULL,
        version = 1,
        updated_at_utc = NOW()
    WHERE id = '${DEV_OPEN_INVOICE_ID}';

    UPDATE saas_billing_invoices
    SET invoice_status = 'paid',
        issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00',
        due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00',
        paid_at_utc = TIMESTAMPTZ '2026-08-10 00:00:00+00',
        voided_at_utc = NULL,
        version = 1,
        updated_at_utc = NOW()
    WHERE id = '${DEV_PAID_INVOICE_ID}';

    UPDATE saas_billing_invoices
    SET invoice_status = 'overdue',
        issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00',
        due_at_utc = TIMESTAMPTZ '2026-08-01 00:00:00+00',
        paid_at_utc = NULL,
        voided_at_utc = NULL,
        version = 1,
        updated_at_utc = NOW()
    WHERE id = '${DEV_OVERDUE_INVOICE_ID}';

    UPDATE saas_billing_payment_attempts
    SET payment_status = 'awaiting-manual-verification',
        failure_code = NULL,
        version = 1,
        updated_at_utc = NOW()
    WHERE id = '${DEV_PAYMENT_AWAITING_ID}';
  `);
}

function removeClosureFixtures() {
  const closureInvoices = closureInvoiceIdsSubquery();
  db(`
    DELETE FROM saas_billing_audit_events
    WHERE tenant_id = '${TENANT_ID}'
      AND (
        entity_reference IN (SELECT id::text FROM saas_billing_invoices WHERE id IN (${closureInvoices}))
        OR entity_reference IN (SELECT id::text FROM saas_billing_payment_attempts WHERE invoice_id IN (${closureInvoices}))
      );

    DELETE FROM saas_billing_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_billing_payment_attempts
    WHERE invoice_id IN (${closureInvoices})
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_billing_invoice_lines
    WHERE invoice_id IN (${closureInvoices});

    DELETE FROM saas_billing_invoices
    WHERE id IN (${closureInvoices});
  `);
  restoreDevPilotBilling();
}

function assertDevPilotPreserved() {
  const accountCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_billing_accounts WHERE id = '${DEV_BILLING_ACCOUNT_ID}'`));
  assert("dev billing account preserved", accountCount === 1, `count=${accountCount}`);
  for (const invoiceId of DEV_INVOICE_IDS) {
    const count = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE id = '${invoiceId}'`));
    assert(`dev invoice preserved ${invoiceId.slice(-4)}`, count === 1, `count=${count}`);
  }
  const awaiting = db(`SELECT payment_status FROM saas_billing_payment_attempts WHERE id = '${DEV_PAYMENT_AWAITING_ID}'`);
  assert("dev awaiting payment preserved", awaiting === "awaiting-manual-verification", awaiting);
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA shared secret.");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const secretBytes = Buffer.from(output);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

class CookieJar {
  constructor() {
    this.cookies = new Map();
  }

  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function"
      ? response.headers.getSetCookie()
      : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";");
      const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar, extraHeaders = {}) {
  const headers = { "Content-Type": "application/json", Accept: "application/json", ...extraHeaders };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisZentra.session") && jar.get("NevaxisZentra.csrf") && method !== "GET") {
    headers["X-NevaxisZentra-Csrf"] = jar.get("NevaxisZentra.csrf");
  }
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

async function signInOwner() {
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxiszentra.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: `${CLOSURE_PREFIX}-domain`,
  }, jar);
  if (signIn.status !== 200) fail(`owner sign-in failed: status=${signIn.status}`);
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail(`owner MFA failed: status=${verify.status}`);
  return jar;
}

console.log("Running SAAS-BILLING-1 domain verifier.");

const endpoints = readRequired("apps/api/SaasBilling/SaasBillingEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/SaasBilling/SaasBillingEndpointHelpers.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/SaasBilling/SaasBillingConstants.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("manage access helper", endpoints.includes("ResolveManageAccessAsync"));
assert("helpers return 401 without identity", helpers.includes("TypedResults.Unauthorized()"));
assert("immutable issued invoice guard", constants.includes("IsInvoiceImmutable"));
assert("mutable lines only in draft", constants.includes("MutableLineStatuses") && constants.includes("Draft"));

previewClosureFixtures();
removeClosureFixtures();
assertDevPilotPreserved();

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const owner = await signInOwner();

const workspace = await api("GET", "/api/saas-billing/workspace", undefined, owner);
assert("workspace summary", workspace.status === 200 && workspace.json?.billingAccount, workspace.raw.slice(0, 180));
assert("workspace lists dev invoices", (workspace.json?.invoices?.length ?? 0) >= 4, String(workspace.json?.invoices?.length));

const provider = await api("GET", "/api/saas-billing/provider-status", undefined, owner);
assert("provider status", provider.status === 200, provider.raw.slice(0, 180));
assert("provider-not-configured", provider.json?.provider?.cardCheckoutStatus === "provider-not-configured", JSON.stringify(provider.json?.provider));

const createDraft = await api("POST", "/api/saas-billing/invoices", {
  subscriptionId: SUBSCRIPTION_ID,
  periodStartUtc: PERIOD_START,
  periodEndUtc: PERIOD_END,
  idempotencyKey: `${CLOSURE_PREFIX}-create-draft`,
}, owner);
assert("closure draft invoice created", createDraft.status === 201, createDraft.raw.slice(0, 200));
const closureInvoiceId = createDraft.json?.entityId;
assert("closure draft invoice id returned", Boolean(closureInvoiceId), createDraft.raw.slice(0, 200));

const draftDetail = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner);
const draftVersion = Number(draftDetail.json?.version ?? 1);
const lineCountBeforeOpen = draftDetail.json?.lines?.length ?? 0;
const totalBeforeOpen = Number(draftDetail.json?.totalAmount ?? 0);
assert("draft invoice has lines", lineCountBeforeOpen >= 1, String(lineCountBeforeOpen));

const openInvoice = await api("POST", "/api/saas-billing/invoices/transitions", {
  invoiceId: closureInvoiceId,
  targetStatus: "open",
  expectedVersion: draftVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-open-draft`,
}, owner);
assert("draft → open transition", openInvoice.status === 201, openInvoice.raw.slice(0, 200));

const opened = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner);
assert("issued invoice status open", opened.json?.invoiceStatus === "open", opened.json?.invoiceStatus);
assert("issued invoice has issuedAtUtc", Boolean(opened.json?.issuedAtUtc));
assert("issued invoice lines unchanged", (opened.json?.lines?.length ?? 0) === lineCountBeforeOpen);
assert("issued invoice amount unchanged", Number(opened.json?.totalAmount ?? 0) === totalBeforeOpen);

const reopenDraft = await api("POST", "/api/saas-billing/invoices/transitions", {
  invoiceId: closureInvoiceId,
  targetStatus: "draft",
  expectedVersion: Number(opened.json?.version ?? 2),
  idempotencyKey: `${CLOSURE_PREFIX}-illegal-draft`,
}, owner);
assert("issued invoice cannot return to draft", reopenDraft.status === 409, `status=${reopenDraft.status}`);

const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260815061939_AddSaasBillingAccountInvoicePaymentFoundation.cs");
assert("issued invoice lines append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_billing_invoice_lines"));
assert("issued invoice lines no runtime UPDATE grant", !migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_invoice_lines"));

const partialPayment = await api("POST", "/api/saas-billing/payment-attempts", {
  invoiceId: closureInvoiceId,
  paymentMethod: "bank-transfer",
  amount: 40,
  currencyCode: "TRY",
  externalReferenceMasked: "CLOSURE-REF-***40",
  idempotencyKey: `${CLOSURE_PREFIX}-partial-payment`,
}, owner);
assert("partial bank-transfer payment created", partialPayment.status === 201, partialPayment.raw.slice(0, 200));
const partialPaymentId = partialPayment.json?.entityId;
const partialPaymentRow = db(`SELECT payment_status FROM saas_billing_payment_attempts WHERE id = '${partialPaymentId}'`);
assert("partial payment awaiting verification", partialPaymentRow === "awaiting-manual-verification", partialPaymentRow);

const afterPartial = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner);
assert("partial payment leaves invoice open", afterPartial.json?.invoiceStatus === "open", afterPartial.json?.invoiceStatus);

const verifyPartial = await api("POST", "/api/saas-billing/payment-attempts/verify-bank-transfer", {
  paymentAttemptId: partialPaymentId,
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-verify-partial`,
}, owner);
assert("partial bank-transfer verified", verifyPartial.status === 201, verifyPartial.raw.slice(0, 200));

const afterPartialVerify = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner);
assert("partial reconciliation keeps invoice open", afterPartialVerify.json?.invoiceStatus === "open", afterPartialVerify.json?.invoiceStatus);

const finalPayment = await api("POST", "/api/saas-billing/payment-attempts", {
  invoiceId: closureInvoiceId,
  paymentMethod: "bank-transfer",
  amount: totalBeforeOpen - 40,
  currencyCode: "TRY",
  externalReferenceMasked: "CLOSURE-REF-***60",
  idempotencyKey: `${CLOSURE_PREFIX}-final-payment`,
}, owner);
assert("final bank-transfer payment created", finalPayment.status === 201, finalPayment.raw.slice(0, 200));

const verifyFinal = await api("POST", "/api/saas-billing/payment-attempts/verify-bank-transfer", {
  paymentAttemptId: finalPayment.json?.entityId,
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-verify-final`,
}, owner);
assert("final bank-transfer verified", verifyFinal.status === 201, verifyFinal.raw.slice(0, 200));

const paidInvoice = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner);
assert("open → paid via bank verify", paidInvoice.json?.invoiceStatus === "paid", paidInvoice.json?.invoiceStatus);
assert("paid invoice has paidAtUtc", Boolean(paidInvoice.json?.paidAtUtc));

const overpay = await api("POST", "/api/saas-billing/payment-attempts", {
  invoiceId: DEV_OPEN_INVOICE_ID,
  paymentMethod: "bank-transfer",
  amount: INVOICE_AMOUNT + 1,
  currencyCode: "TRY",
  externalReferenceMasked: "CLOSURE-REF-***99",
  idempotencyKey: `${CLOSURE_PREFIX}-overpay`,
}, owner);
assert("amount reconciliation rejects overpay", overpay.status === 422, `status=${overpay.status} ${overpay.raw.slice(0, 160)}`);

const cardAttempt = await api("POST", "/api/saas-billing/payment-attempts", {
  invoiceId: DEV_OPEN_INVOICE_ID,
  paymentMethod: "card",
  amount: INVOICE_AMOUNT,
  currencyCode: "TRY",
  externalReferenceMasked: "CARD-REF-***01",
  idempotencyKey: `${CLOSURE_PREFIX}-card-fail`,
}, owner);
assert("card payment attempt accepted as record", cardAttempt.status === 201, cardAttempt.raw.slice(0, 200));

const cardPayments = await api("GET", `/api/saas-billing/payment-attempts?invoiceId=${DEV_OPEN_INVOICE_ID}`, undefined, owner);
const cardRow = (cardPayments.json ?? []).find((item) => item.paymentAttemptId === cardAttempt.json?.entityId);
assert("card payment not succeeded", cardRow?.paymentStatus === "failed", JSON.stringify(cardRow));
assert("card provider-not-configured", cardRow?.failureCode === "saas.billing.provider-not-configured", cardRow?.failureCode);
assert("no fake card success", cardRow?.paymentStatus !== "succeeded");

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.invoices === 0 && remaining.payments === 0 && remaining.idempotency === 0);
assertDevPilotPreserved();

void ROOT;
console.log("SAAS-BILLING-1 domain verification passed.");
