import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import {
  classifyApiFailure,
  failureForRefusal,
  refusalCode,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
} from "../src/lib/api/api-failure-classifier";

// An HTTP 403 does not always mean "you lack a permission": the API also answers 403 for a lapsed/missing
// subscription (failureCode saas-subscription.*). The backend failure code is authoritative; the status is the
// fallback only when the body carries no structured code.
test("a structured backend code outranks the HTTP status", () => {
  assert.equal(classifyApiFailure(403, { failureCode: "saas-subscription.entitlement.subscription-not-found" }).kind, "subscription-required");
  assert.equal(classifyApiFailure(403, { failureCode: "saas-subscription.entitlement.module-not-entitled" }).kind, "subscription-required");
  assert.equal(classifyApiFailure(403, { failureCode: "facility-access-denied" }).kind, "permission-denied");
  assert.equal(classifyApiFailure(403, { failureCode: "production.permission-denied" }).kind, "permission-denied");
  assert.equal(classifyApiFailure(403, { code: "session-expired" }).kind, "unauthenticated");
  assert.equal(classifyApiFailure(500, { failureCode: "production.order.not-found" }).kind, "not-found");
  assert.equal(classifyApiFailure(400, { failureCode: "throughput-optimizer.validation-failed" }).kind, "validation");
  assert.equal(classifyApiFailure(409, { failureCode: "optimistic-concurrency-conflict" }).kind, "conflict");
  assert.equal(classifyApiFailure(503, { failureCode: "source-unavailable" }).kind, "source-unavailable");
});

test("without a structured code the HTTP status decides", () => {
  assert.deepEqual(classifyApiFailure(401, null), { kind: "unauthenticated", failureCode: null, structured: false });
  assert.equal(classifyApiFailure(403, null).kind, "permission-denied");
  assert.equal(classifyApiFailure(403, "not json").kind, "permission-denied");
  assert.equal(classifyApiFailure(404, {}).kind, "not-found");
  assert.equal(classifyApiFailure(422, {}).kind, "validation");
  assert.equal(classifyApiFailure(409, {}).kind, "conflict");
  assert.equal(classifyApiFailure(500, {}).kind, "source-unavailable");
  assert.equal(classifyApiFailure(502, undefined).kind, "source-unavailable");
});

test("a refusal reports a licence or session cause as such and only a real denial as permission denied", () => {
  const denied = failureForRefusal(classifyApiFailure(403, { failureCode: "facility-access-denied" }), "custom denial");
  assert.deepEqual(denied, { status: "failure", code: "permission-denied", message: "custom denial", retryable: false });

  const licence = failureForRefusal(classifyApiFailure(403, { failureCode: "saas-subscription.entitlement.module-not-entitled" }), "custom denial");
  assert.equal(licence.code, "subscription-required");
  assert.equal(licence.message, SUBSCRIPTION_REQUIRED_MESSAGE);

  const session = failureForRefusal(classifyApiFailure(403, { failureCode: "session-expired" }), "custom denial");
  assert.equal(session.code, "unauthenticated");
  assert.equal(session.message, SESSION_REQUIRED_MESSAGE);

  assert.equal(refusalCode(403, { failureCode: "saas-subscription.x" }), "subscription-required");
  assert.equal(refusalCode(403, null), "permission-denied");
  assert.equal(refusalCode(403, { failureCode: "something-new-and-unknown" }), "permission-denied");
});

function featureSources(directory: string, files: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) featureSources(full, files);
    else if (/\.ts$/.test(entry.name)) files.push(full);
  }
  return files;
}

// Auth/identity endpoints answer 403 for genuine authorization/token refusals (not licence causes).
const CLASSIFIER_NOT_APPLICABLE = new Set([
  "saas-identity/client/saas-identity-browser-client.ts",
  "saas-identity/lib/first-run-enrollment.ts",
  "saas-identity/server/saas-identity-api-client.ts",
]);

test("every feature API client that handles an HTTP 403 classifies it through the shared classifier", () => {
  const featuresRoot = path.resolve(__dirname, "..", "src", "features");
  const offenders: string[] = [];
  for (const file of featureSources(featuresRoot)) {
    const relative = path.relative(featuresRoot, file).split(path.sep).join("/");
    const source = readFileSync(file, "utf8");
    if (!/status === 403|case 403/.test(source)) continue;
    if (CLASSIFIER_NOT_APPLICABLE.has(relative)) continue;
    const classified = /refusalCode|failureForRefusal|classifyApiFailure|classifyFailedResponse|isSubscriptionEntitlementFailure|failure\?\.kind/.test(source);
    if (!classified) offenders.push(relative);
  }
  assert.deepEqual(offenders, [], "a blanket 403 -> permission-denied mapping would mislabel licence/session failures");
});
