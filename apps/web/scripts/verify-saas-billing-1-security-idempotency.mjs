#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-BILLING-1-CLOSURE";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
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
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SAAS-BILLING-1 security-idempotency verification failed: ${message}`);
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

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
  console.log(`Preview ${CLOSURE_PREFIX} security fixtures: invoices=${invoices}, payments=${payments}, idempotency=${idempotency}`);
  return { invoices, payments, idempotency };
}

function restoreDevPilotBilling() {
  db(`
    UPDATE saas_billing_invoices
    SET invoice_status = 'draft', issued_at_utc = NULL, due_at_utc = NULL, paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_DRAFT_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'open', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00', paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_OPEN_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'paid', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00', paid_at_utc = TIMESTAMPTZ '2026-08-10 00:00:00+00', voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_PAID_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'overdue', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-01 00:00:00+00', paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_OVERDUE_INVOICE_ID}';
    UPDATE saas_billing_payment_attempts
    SET payment_status = 'awaiting-manual-verification', failure_code = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_PAYMENT_AWAITING_ID}';
  `);
}

function seedSalesBillingReadOverride() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference = '${SALES_MEMBER}')
      AND permission_definition_id = '10000000-0000-4000-8000-000000000074';

    INSERT INTO tenant_membership_permission_overrides (
      id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    SELECT
      'dd000003-00b3-4000-8000-0000000000b3',
      '${TENANT_ID}',
      tm.id,
      '10000000-0000-4000-8000-000000000074',
      'allow',
      'tenant',
      'Active',
      NOW(),
      NOW(),
      1
    FROM tenant_memberships tm
    WHERE tm.membership_reference = '${SALES_MEMBER}';
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
    DELETE FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM admin_access_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_payment_attempts
    WHERE invoice_id IN (${closureInvoices}) OR idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_invoice_lines WHERE invoice_id IN (${closureInvoices});
    DELETE FROM saas_billing_invoices WHERE id IN (${closureInvoices});
    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference = '${SALES_MEMBER}')
      AND permission_definition_id IN (
        'bb000003-0001-4000-8000-000000000001',
        '10000000-0000-4000-8000-000000000074',
        '10000000-0000-4000-8000-000000000075');
  `);
  restoreDevPilotBilling();
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
  if (jar?.get("crevafactos.session") && jar.get("crevafactos.csrf") && method !== "GET") {
    headers["X-CrevaFactOS-Csrf"] = jar.get("crevafactos.csrf");
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

async function signIn(email, password, mfaSecret, deviceLabel) {
  const jar = new CookieJar();
  const signInResult = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: deviceLabel ?? `${CLOSURE_PREFIX}-security`,
  }, jar);
  if (mfaSecret) {
    const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSecret) }, jar);
    if (verify.status !== 200) fail(`MFA failed for ${email}: status=${verify.status}`);
  }
  return { jar, signInResult };
}

console.log("Running SAAS-BILLING-1 security-idempotency verifier.");
previewClosureFixtures();
removeClosureFixtures();

const missing = await api("GET", "/api/saas-billing/workspace");
assert("missing auth → 401", missing.status === 401, `status=${missing.status}`);

const salesPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const restricted = await signIn("materials-sales@dev.local", salesPassword);
const restrictedDenied = await api("GET", "/api/saas-billing/workspace", undefined, restricted.jar);
assert("unauthorized billing read → 403", restrictedDenied.status === 403, `status=${restrictedDenied.status}`);

const owner = await signIn(
  "tenant-owner@dev.local",
  requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
);

seedSalesBillingReadOverride();

const salesRead = await signIn("materials-sales@dev.local", salesPassword, undefined, `${CLOSURE_PREFIX}-sales-read`);
const salesWorkspace = await api("GET", "/api/saas-billing/workspace", undefined, salesRead.jar);
assert("read-only user can read workspace", salesWorkspace.status === 200, `status=${salesWorkspace.status}`);

const idempotencyBefore = Number(db(`SELECT COUNT(*) FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
const auditBefore = Number(db(`SELECT COUNT(*) FROM saas_billing_audit_events WHERE tenant_id = '${TENANT_ID}'`));
const deniedMutation = await api("POST", "/api/saas-billing/invoices/transitions", {
  invoiceId: DEV_DRAFT_INVOICE_ID,
  targetStatus: "open",
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-read-only-mutation`,
}, salesRead.jar);
assert("read-only mutation → 403", deniedMutation.status === 403, `status=${deniedMutation.status}`);
const idempotencyAfterDenied = Number(db(`SELECT COUNT(*) FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("denied mutation created no idempotency record", idempotencyAfterDenied === idempotencyBefore, `${idempotencyBefore}→${idempotencyAfterDenied}`);
const auditAfterDenied = Number(db(`SELECT COUNT(*) FROM saas_billing_audit_events WHERE tenant_id = '${TENANT_ID}'`));
assert("denied mutation created no audit growth", auditAfterDenied === auditBefore, `${auditBefore}→${auditAfterDenied}`);

const untrustedTenant = await api(
  "GET",
  "/api/saas-billing/workspace",
  undefined,
  owner.jar,
  { "X-CrevaFactOS-Tenant": "other-tenant-not-pilot" },
);
assert("untrusted tenant header rejected", untrustedTenant.status === 403, `status=${untrustedTenant.status}`);

const createDraft = await api("POST", "/api/saas-billing/invoices", {
  subscriptionId: SUBSCRIPTION_ID,
  periodStartUtc: PERIOD_START,
  periodEndUtc: PERIOD_END,
  idempotencyKey: `${CLOSURE_PREFIX}-create-draft`,
}, owner.jar);
assert("closure draft invoice created", createDraft.status === 201, createDraft.raw.slice(0, 180));
const closureInvoiceId = createDraft.json?.entityId;

const draftDetail = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner.jar);
const openBody = {
  invoiceId: closureInvoiceId,
  targetStatus: "open",
  expectedVersion: Number(draftDetail.json?.version ?? 1),
  idempotencyKey: `${CLOSURE_PREFIX}-concurrent-open`,
};
const invoicesBeforeConcurrent = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE id = '${closureInvoiceId}' AND invoice_status = 'open'`));
const [concurrentA, concurrentB] = await Promise.all([
  api("POST", "/api/saas-billing/invoices/transitions", openBody, owner.jar),
  api("POST", "/api/saas-billing/invoices/transitions", openBody, owner.jar),
]);
const concurrentStatuses = [concurrentA.status, concurrentB.status].sort((a, b) => a - b);
assert("concurrent identical transition no HTTP 500", concurrentStatuses.every((status) => status !== 500));
assert("concurrent identical transition statuses 200/201", concurrentStatuses[0] === 200 && concurrentStatuses[1] === 201, concurrentStatuses.join(","));
assert("concurrent identical transition replay flag", concurrentA.json?.idempotentReplay === true || concurrentB.json?.idempotentReplay === true);
const openCount = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE id = '${closureInvoiceId}' AND invoice_status = 'open'`));
assert("concurrent identical transition one open invoice", openCount === 1 && invoicesBeforeConcurrent === 0, `openCount=${openCount}`);

const opened = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner.jar);
const conflict = await api("POST", "/api/saas-billing/invoices/transitions", {
  invoiceId: closureInvoiceId,
  targetStatus: "void",
  expectedVersion: Number(opened.json?.version ?? 2),
  idempotencyKey: `${CLOSURE_PREFIX}-concurrent-open`,
}, owner.jar);
assert("different payload idempotency conflict → 409", conflict.status === 409, `status=${conflict.status} ${conflict.raw.slice(0, 160)}`);

const legacyKey = `${CLOSURE_PREFIX}-legacy-null-hash`;
db(`
  INSERT INTO saas_billing_idempotency_records (id, tenant_id, idempotency_key, request_hash, operation, created_at_utc)
  VALUES ('dd000003-00b1-4000-8000-0000000000b1', '${TENANT_ID}', '${legacyKey}', '', 'billing.invoice.transition', NOW())
  ON CONFLICT (tenant_id, idempotency_key) DO UPDATE SET request_hash = '';
`);
const legacyRetry = await api("POST", "/api/saas-billing/invoices/transitions", {
  invoiceId: closureInvoiceId,
  targetStatus: "void",
  expectedVersion: Number((await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner.jar)).json?.version ?? 2),
  idempotencyKey: legacyKey,
}, owner.jar);
assert("legacy NULL request_hash → 409", legacyRetry.status === 409, `status=${legacyRetry.status} ${legacyRetry.raw.slice(0, 160)}`);

const paymentBody = {
  invoiceId: closureInvoiceId,
  paymentMethod: "bank-transfer",
  amount: Number(opened.json?.totalAmount ?? 100),
  currencyCode: opened.json?.currencyCode ?? "TRY",
  externalReferenceMasked: "CLOSURE-REF-***SEC",
  idempotencyKey: `${CLOSURE_PREFIX}-bank-payment`,
};
const payment = await api("POST", "/api/saas-billing/payment-attempts", paymentBody, owner.jar);
assert("manual bank-transfer payment created", payment.status === 201, payment.raw.slice(0, 180));

const verifyBody = {
  paymentAttemptId: payment.json?.entityId,
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-verify-bank`,
};
const [verifyA, verifyB] = await Promise.all([
  api("POST", "/api/saas-billing/payment-attempts/verify-bank-transfer", verifyBody, owner.jar),
  api("POST", "/api/saas-billing/payment-attempts/verify-bank-transfer", verifyBody, owner.jar),
]);
const verifyStatuses = [verifyA.status, verifyB.status].sort((a, b) => a - b);
assert("concurrent bank-transfer verify statuses 200/201", verifyStatuses[0] === 200 && verifyStatuses[1] === 201, verifyStatuses.join(","));
assert("bank-transfer verify replay flag", verifyA.json?.idempotentReplay === true || verifyB.json?.idempotentReplay === true);

const paid = await api("GET", `/api/saas-billing/invoices/${closureInvoiceId}`, undefined, owner.jar);
assert("manual bank-transfer verification marks invoice paid", paid.json?.invoiceStatus === "paid", paid.json?.invoiceStatus);

const auditPayloads = db(`
  SELECT payload_json FROM saas_billing_audit_events
  WHERE tenant_id = '${TENANT_ID}'
  ORDER BY occurred_at_utc DESC
  LIMIT 20
`);
const auditText = auditPayloads.toLowerCase();
assert("audit secrecy no password", !auditText.includes("password"));
assert("audit secrecy no totp", !auditText.includes("totp"));
assert("audit secrecy no sessiontoken", !auditText.includes("sessiontoken"));

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.invoices === 0 && remaining.payments === 0 && remaining.idempotency === 0);
const accountCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_billing_accounts WHERE id = '${DEV_BILLING_ACCOUNT_ID}'`));
assert("dev billing account preserved", accountCount === 1, `count=${accountCount}`);

void ROOT;
console.log("SAAS-BILLING-1 security-idempotency verification passed.");
