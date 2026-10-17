import assert from "node:assert/strict";
import { test } from "node:test";

import {
  formatApplicationVersion,
  formatBuildDetail,
  resolveSystemStatusHeadline,
} from "../src/features/enterprise-reliability/domain/enterprise-reliability-labels";

const base = { showsFalseHealth: false, operatingMode: "fully-operational", alerts: [], components: [] } as const;

test("overall status is Doğrulanamadı, never Her şey normal, while core components are unknown", () => {
  const headline = resolveSystemStatusHeadline({ ...base, coreStatuses: ["unknown", "unknown", "unknown", "unknown", "unknown"] });
  assert.equal(headline.label, "Doğrulanamadı");
  assert.equal(headline.tone, "neutral");
});

test("no observation at all is not a normal system", () => {
  assert.equal(resolveSystemStatusHeadline(base).label, "Doğrulanamadı");
});

test("one unknown component among healthy ones keeps the overall state unverified", () => {
  const headline = resolveSystemStatusHeadline({ ...base, coreStatuses: ["healthy", "healthy", "unknown"] });
  assert.equal(headline.label, "Doğrulanamadı");
});

test("Her şey normal is claimed only when every observed component is healthy", () => {
  const headline = resolveSystemStatusHeadline({
    ...base,
    components: [{ healthStatus: "healthy" }],
    coreStatuses: ["healthy", "healthy"],
  });
  assert.equal(headline.label, "Her şey normal");
  assert.equal(headline.tone, "success");
});

test("a degraded component is reported as a problem even when others are unknown", () => {
  const headline = resolveSystemStatusHeadline({ ...base, coreStatuses: ["unavailable", "unknown"] });
  assert.equal(headline.label, "Bazı bileşenlerde sorun var");
});

test("placeholder version and build strings become plain Turkish statements", () => {
  assert.equal(formatApplicationVersion("1.0.0.0"), "Sürüm bilgisi yok");
  assert.equal(formatApplicationVersion("2.4.1"), "2.4.1");
  assert.equal(formatBuildDetail("unknown"), "Derleme bilgisi yok");
  assert.equal(formatBuildDetail("abcdef1234567890"), "Derleme abcdef12");
});
