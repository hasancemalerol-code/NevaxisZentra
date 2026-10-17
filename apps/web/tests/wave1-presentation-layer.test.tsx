import assert from "node:assert/strict";
import test from "node:test";

import { classifyFailureCode } from "../src/lib/api/api-failure-classifier";
import { displayActor, isOpaqueActorId } from "../src/lib/presentation/display-label";
import { enumLabel, humanizeEnum, localizeStatusWords, resolveEnumLabel } from "../src/lib/presentation/enum-labels";
import {
  formatEvidenceSentence,
  formatEvidenceValue,
  formatDateRange,
  formatDateTime,
  formatMinutes,
  formatPercent,
  formatRatioAsPercent,
  formatWeekKey,
} from "../src/lib/presentation/format";
import { deferredSurfacesEnabled, isDeferredSurfaceHref, isSurfaceHrefVisible } from "../src/lib/runtime/deferred-surfaces";
import { failureStateContent } from "../src/components/ui/failure-state";

test("percent formatting is Turkish and never double-multiplies", () => {
  assert.equal(formatPercent(35), "%35,0");
  assert.equal(formatRatioAsPercent(0.35), "%35,0");
  assert.equal(formatRatioAsPercent(35), "%35,0");
  assert.equal(formatRatioAsPercent(null), "—");
});

test("dates are local facility time, never ISO/UTC", () => {
  const text = formatDateTime("2026-10-13T21:30:00Z");
  assert.ok(!text.includes("T") && !text.includes("Z"), text);
  assert.ok(text.includes("00:30"), `Istanbul is UTC+3: ${text}`);
  assert.equal(formatDateTime("not a date"), "—");
  assert.ok(formatDateRange("2026-10-01T09:00:00Z", "2026-10-07T09:00:00Z").includes("–"));
});

test("week keys and minutes read as words", () => {
  assert.equal(formatWeekKey("2026-W42"), "42. hafta 2026");
  assert.equal(formatMinutes(125), "2 sa 5 dk");
  assert.equal(formatMinutes(45), "45 dk");
});

test("enum values map to Turkish labels, unknown ones are humanized", () => {
  for (const raw of ["critical", "open", "scheduled", "draft", "awaiting-approval", "requires-review", "fresh", "good", "measurement", "data-quality"]) {
    const label = enumLabel(raw);
    assert.notEqual(label, raw, raw);
    assert.ok(!/[a-z]-[a-z]/.test(label), label);
  }
  assert.equal(resolveEnumLabel("critical").level, "critical");
  assert.equal(humanizeEnum("some_new_state"), "Some new state");
});

test("opaque actor ids are never shown as a person", () => {
  assert.ok(isOpaqueActorId("3f2b8c1e-aaaa-bbbb-cccc-0123456789ab"));
  assert.ok(isOpaqueActorId("mem_9f8e7d6c5b"));
  assert.equal(displayActor("mem_9f8e7d6c5b"), "Yetkili kullanıcı");
  assert.equal(displayActor("Ayşe Yılmaz"), "Ayşe Yılmaz");
});

test("deferred surfaces are fail-closed in production and cover demo routes", () => {
  for (const href of ["/dijital-is-gucu", "/dijital-is-gucu/onaylar", "/sunum-modu", "/golden-demo/uretim-kurtarma"]) {
    assert.ok(isDeferredSurfaceHref(href), href);
    assert.equal(isSurfaceHrefVisible(href, { NODE_ENV: "production" }), false, href);
    assert.equal(isSurfaceHrefVisible(href, { NODE_ENV: "production", NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL: "DEMO" }), true, href);
  }
  assert.equal(isDeferredSurfaceHref("/sunum-modulari"), false);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production" }), false);
});

test("failure codes map to distinct customer states", () => {
  const options = { failedTitle: "Yüklenemedi", entityLabel: "hat" } as const;
  assert.equal(failureStateContent({ code: "permission-denied" }, options).kind, "forbidden");
  assert.equal(failureStateContent({ code: "subscription-required" }, options).kind, "entitlement");
  assert.equal(failureStateContent({ code: "http-404" }, options).kind, "not-found");
  assert.equal(failureStateContent({ code: "http-403" }, options).kind, "forbidden");
  assert.equal(failureStateContent({ code: "provider-not-configured" }, options).kind, "unavailable");
  assert.equal(failureStateContent({ code: "provider-unavailable" }, options).kind, "unavailable");
  assert.equal(classifyFailureCode(undefined), "source-unavailable");

  const denied = failureStateContent({ code: "permission-denied" }, options);
  assert.ok(!/sonra|yeniden dene/i.test(denied.description), "denied must not say retry later");
  const unavailable = failureStateContent({ code: "http-503", message: "İstek başarısız." }, options);
  assert.ok(!/yetki/i.test(unavailable.description), "unavailable must not claim missing permission");
  assert.ok(!unavailable.description.includes("İstek başarısız"), "raw backend text never shown");
  const missing = failureStateContent({ code: "not-found" }, options);
  assert.ok(!/yetki/i.test(missing.title), "missing record is not a denial");
});

test("generated evidence sentences lose machine words and ISO timestamps", () => {
  assert.equal(localizeStatusWords("Endpoint: degraded"), "Endpoint: bozulmuş");
  const sentence = formatEvidenceSentence("Kaynak tazeliği: fresh | ERP güncelleme: 2026-10-13T16:56:57.8881930+00:00");
  assert.ok(!/fresh|T\d{2}:\d{2}/.test(sentence), sentence);
  assert.equal(formatEvidenceValue("open"), "Açık");
  assert.equal(formatEvidenceValue("750.0000"), "750");
});
