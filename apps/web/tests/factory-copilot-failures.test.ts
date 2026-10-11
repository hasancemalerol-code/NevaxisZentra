import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import { classifyApiFailure } from "../src/lib/api/api-failure-classifier";
import {
  describeFactoryCopilotAiState,
  describeFactoryCopilotRequestFailure,
} from "../src/features/factory-copilot/domain/factory-copilot-failures";

// Factory Copilot failures are different things for the customer to do; they must never collapse into "Copilot failed".
test("request failures are told apart: session, permission, licence, input, context, outage, unexpected", () => {
  const cases: Array<[number, unknown, string]> = [
    [401, null, "authentication"],
    [403, { code: "facility-access-denied" }, "authorization"],
    [403, { failureCode: "saas-subscription.module-not-entitled" }, "subscription"],
    [400, { failureCode: "factory-copilot.validation-failed", message: "Query text exceeds 1000 characters." }, "invalid-input"],
    [422, { failureCode: "factory-copilot.insufficient-context" }, "insufficient-context"],
    [503, null, "service-unavailable"],
    [502, { failureCode: "factory-copilot.provider-unavailable" }, "service-unavailable"],
    [409, { failureCode: "factory-copilot.schema-rejected" }, "unexpected"],
  ];
  const seen = new Set<string>();
  for (const [status, body, expected] of cases) {
    const message = typeof body === "object" && body !== null && "message" in body ? String((body as { message: string }).message) : null;
    const description = describeFactoryCopilotRequestFailure(classifyApiFailure(status, body), message);
    assert.equal(description.category, expected, `${status} ${JSON.stringify(body)}`);
    assert.ok(description.message.length > 0);
    seen.add(description.message);
  }
  assert.ok(seen.size >= 7, "each failure category has its own customer message");
});

test("an oversized-input refusal shows the server's reason, not a generic failure", () => {
  const description = describeFactoryCopilotRequestFailure(
    classifyApiFailure(400, { failureCode: "factory-copilot.validation-failed" }),
    "Query text exceeds 1000 characters.",
  );
  assert.equal(description.message, "Query text exceeds 1000 characters.");
});

test("every AI state the API can report has its own customer wording", () => {
  const states = [
    "AI_NOT_CONFIGURED",
    "AI_AUTHENTICATION_FAILED",
    "AI_RATE_LIMITED",
    "AI_TIMEOUT",
    "AI_PROVIDER_UNAVAILABLE",
    "AI_INVALID_OUTPUT",
    "AI_BUDGET_LIMITED",
    "INSUFFICIENT_DATA",
    "prompt-injection-detected",
  ];
  const labels = states.map((state) => describeFactoryCopilotAiState(state));
  assert.ok(labels.every((label) => label !== null && label.length > 0));
  assert.equal(new Set(labels).size, states.length, "no two distinct AI states share the same wording");
  assert.equal(describeFactoryCopilotAiState("AI_UNGROUNDED_RESPONSE"), describeFactoryCopilotAiState("AI_INVALID_OUTPUT"));
  assert.equal(describeFactoryCopilotAiState(null), null);
  assert.equal(describeFactoryCopilotAiState("SOMETHING_ELSE"), null);
});

test("the Copilot actions classify failures instead of surfacing a raw error message", () => {
  const dir = path.resolve(__dirname, "..", "src", "features", "factory-copilot", "server");
  const actions = readFileSync(path.join(dir, "factory-copilot-actions.ts"), "utf8");
  const client = readFileSync(path.join(dir, "factory-copilot-api-client.ts"), "utf8");
  assert.match(actions, /describeFactoryCopilotRequestFailure/);
  assert.doesNotMatch(actions, /error instanceof Error \? error\.message/);
  assert.match(client, /classifyFailedResponse/);
  assert.match(client, /FactoryCopilotRequestError/);
});

test("an action call that throws is classified: a login redirect is a lost session, anything else an unreachable service", async () => {
  const { describeFactoryCopilotTransportFailure } = await import("../src/features/factory-copilot/domain/factory-copilot-failures");

  const redirect = await describeFactoryCopilotTransportFailure("/fabrika", async () => ({ type: "opaqueredirect", status: 0 }) as Response);
  assert.equal(redirect.category, "authentication");
  const found = await describeFactoryCopilotTransportFailure("/fabrika", async () => ({ type: "basic", status: 307 }) as Response);
  assert.equal(found.category, "authentication");
  const outage = await describeFactoryCopilotTransportFailure("/fabrika", async () => ({ type: "basic", status: 200 }) as Response);
  assert.equal(outage.category, "service-unavailable");
  const unreachable = await describeFactoryCopilotTransportFailure("/fabrika", async () => {
    throw new TypeError("network");
  });
  assert.equal(unreachable.category, "service-unavailable");
  assert.notEqual(redirect.message, outage.message);
});

test("the drawer never lets a failed action call crash the page", () => {
  const drawer = readFileSync(
    path.resolve(__dirname, "..", "src", "features", "factory-copilot", "components", "factory-copilot-drawer.tsx"),
    "utf8",
  );
  assert.match(drawer, /describeFactoryCopilotTransportFailure/);
  assert.match(drawer, /catch \{/);
});
