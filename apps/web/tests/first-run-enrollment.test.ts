import assert from "node:assert/strict";
import test from "node:test";

import {
  FIRST_RUN_ENROLL_PATH,
  FIRST_RUN_STATUS_PATH,
  fetchFirstRunAvailability,
  mapEnrollmentFailure,
  submitFirstRunEnrollment,
} from "../src/features/saas-identity/lib/first-run-enrollment";
import { SAAS_IDENTITY_AUTH_ROUTES } from "../src/features/saas-identity/lib/saas-identity-constants";

const payload = {
  token: "TOKEN-VALUE-FOR-TEST",
  organizationCode: "acme-fab",
  organizationName: "Acme Fabrika",
  ownerDisplayName: "Ada Owner",
  ownerEmail: "ada@acme.example",
  password: "Correct-Horse-9-Battery",
} as const;

function fakeFetch(status: number, body: unknown, capture?: { url?: string; init?: RequestInit }) {
  return (async (input: RequestInfo | URL, init?: RequestInit) => {
    if (capture) {
      capture.url = String(input);
      capture.init = init;
    }
    return new Response(JSON.stringify(body), { status });
  }) as typeof fetch;
}

test("enrollment page is a public auth route", () => {
  assert.ok((SAAS_IDENTITY_AUTH_ROUTES as readonly string[]).includes("/kurulum"));
});

test("availability is true only for an explicit available:true answer", async () => {
  assert.equal(await fetchFirstRunAvailability(fakeFetch(200, { available: true })), true);
  assert.equal(await fetchFirstRunAvailability(fakeFetch(200, { available: false })), false);
  assert.equal(await fetchFirstRunAvailability(fakeFetch(404, {})), false);
  assert.equal(
    await fetchFirstRunAvailability((async () => {
      throw new Error("network");
    }) as typeof fetch),
    false,
  );
});

test("successful enrollment posts JSON to the localhost endpoint and never puts the token in the URL", async () => {
  const capture: { url?: string; init?: RequestInit } = {};
  const outcome = await submitFirstRunEnrollment(
    payload,
    fakeFetch(201, { status: "enrolled", tenantKey: "acme-fab" }, capture),
  );

  assert.deepEqual(outcome, { status: "enrolled", tenantKey: "acme-fab" });
  assert.equal(capture.url, FIRST_RUN_ENROLL_PATH);
  assert.ok(!String(capture.url).includes(payload.token));
  assert.equal(capture.init?.method, "POST");
  assert.equal(capture.init?.credentials, "same-origin");
  assert.equal(FIRST_RUN_STATUS_PATH.includes("token"), false);
});

test("validation errors are shown as readable Turkish messages", async () => {
  const outcome = await submitFirstRunEnrollment(
    payload,
    fakeFetch(400, { errors: ["password-too-short", "owner-email-invalid"] }),
  );

  assert.equal(outcome.status, "failure");
  if (outcome.status === "failure") {
    assert.equal(outcome.unavailable, false);
    assert.equal(outcome.messages.length, 2);
    assert.match(outcome.messages[0], /12 karakter/);
    assert.ok(outcome.messages.every((message) => !message.includes(payload.password)));
  }
});

test("invalid or expired token, throttling and closed enrollment are distinguished", () => {
  const forbidden = mapEnrollmentFailure(403, []);
  const closed = mapEnrollmentFailure(409, []);
  const gone = mapEnrollmentFailure(404, []);
  const throttled = mapEnrollmentFailure(429, []);
  const generic = mapEnrollmentFailure(503, []);

  assert.ok(forbidden.status === "failure" && !forbidden.unavailable && /anahtar/i.test(forbidden.messages[0]));
  assert.ok(closed.status === "failure" && closed.unavailable);
  assert.ok(gone.status === "failure" && gone.unavailable);
  assert.ok(throttled.status === "failure" && !throttled.unavailable && /deneme/i.test(throttled.messages[0]));
  assert.ok(generic.status === "failure" && !generic.unavailable);
});

test("a network failure never throws and never reveals details", async () => {
  const outcome = await submitFirstRunEnrollment(payload, (async () => {
    throw new Error("connect ECONNREFUSED secret-detail");
  }) as typeof fetch);

  assert.equal(outcome.status, "failure");
  if (outcome.status === "failure") {
    assert.ok(outcome.messages.every((message) => !message.includes("secret-detail")));
  }
});
