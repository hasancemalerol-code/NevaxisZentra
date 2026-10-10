import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import { getErpDocument } from "../src/features/erp/server/erp-api-client";
import { getQualityInspection } from "../src/features/quality-control/server/quality-control-api-client";
import { isNotFoundFailureCode } from "../src/lib/api/api-failure-classifier";

// A record that does not exist is "not found", not "failed to load": the page must say so truthfully, and a 404 must
// not be disguised as an unavailable source. Fresh-tenant empty states must be explicit and side-effect-free.
const context = {
  runtimeMode: "api" as const,
  apiBaseUrl: "http://127.0.0.1:5000",
  facilityId: "bb000001-0005-4000-8000-000000000005",
  requestHeaders: { Cookie: "NevaxisZentra.session=test" },
  correlationId: "correlation-test",
};
const realFetch = globalThis.fetch;

test("not-found failure codes are recognised, other codes are not", () => {
  for (const code of ["not-found", "mixed-production.not-found", "outcome-not-found", "http-404", "factory-case.not_found"]) {
    assert.equal(isNotFoundFailureCode(code), true, code);
  }
  for (const code of ["source-unavailable", "permission-denied", "http-500", "", undefined, null, 404]) {
    assert.equal(isNotFoundFailureCode(code), false, String(code));
  }
});

test("ERP and quality clients report an HTTP 404 as not-found, not as an unavailable source", async () => {
  globalThis.fetch = (async () => new Response("{}", { status: 404 })) as typeof fetch;
  try {
    const erp = await getErpDocument(context, "00000000-0000-4000-8000-000000000001");
    assert.equal(erp.status, "failure");
    assert.equal(erp.status === "failure" ? erp.code : "", "not-found");
    assert.equal(erp.status === "failure" ? erp.retryable : true, false);

    const quality = await getQualityInspection(context, "00000000-0000-4000-8000-000000000001");
    assert.equal(quality.status === "failure" ? quality.code : "", "not-found");

    globalThis.fetch = (async () => new Response("{}", { status: 503 })) as typeof fetch;
    const unavailable = await getErpDocument(context, "00000000-0000-4000-8000-000000000001");
    assert.equal(unavailable.status === "failure" ? unavailable.code : "", "source-unavailable");
  } finally {
    globalThis.fetch = realFetch;
  }
});

const DETAIL_PAGES = [
  "erp-entegrasyonlari/belgeler/[documentId]/page.tsx",
  "erp-entegrasyonlari/onayli-islemler/[writebackId]/page.tsx",
  "erp-entegrasyonlari/satin-alma/[purchaseOrderId]/page.tsx",
  "erp-entegrasyonlari/siparisler/[orderId]/page.tsx",
  "erp-entegrasyonlari/uretim/[productionOrderId]/page.tsx",
  "kalite/kontroller/[inspectionId]/page.tsx",
  "kalite/kontroller/[inspectionId]/ozet/page.tsx",
  "kalite/ncr/[ncrId]/page.tsx",
  "operasyon/yonetim-merkezi/vakalar/[caseId]/page.tsx",
  "operasyon/yonetim-merkezi/kararlar/[decisionId]/page.tsx",
  "uretim/hatlar/[lineId]/page.tsx",
  "bakim/varliklar/[assetReference]/page.tsx",
  "ai-gorevleri/playbooklar/[playbookId]/page.tsx",
  "golden-demo/gumruk-dosyasi/page.tsx",
  "golden-demo/uretim-kurtarma/page.tsx",
] as const;

test("shipped detail pages render a truthful not-found state instead of a generic load failure", () => {
  const appRoot = path.resolve(__dirname, "..", "src", "app", "(app)");
  for (const relative of DETAIL_PAGES) {
    const source = readFileSync(path.join(appRoot, relative), "utf8");
    assert.match(
      source,
      /isNotFoundFailureCode|code === "not-found"/,
      `${relative} must distinguish not-found from a load failure`,
    );
  }
});

test("a gate failure page says 'no permission' only for a real denial", () => {
  const appRoot = path.resolve(__dirname, "..", "src", "app", "(app)");
  for (const relative of [
    "ayarlar/page.tsx",
    "ayarlar/sistem-durumu/page.tsx",
    "fabrika-agi/page.tsx",
    "operasyon/yonetim-merkezi/page.tsx",
    "operasyon/yonetim-merkezi/surekli-iyilestirme/page.tsx",
    "operasyon/yonetim-merkezi/vakalar/[caseId]/page.tsx",
    "operasyon/yonetim-merkezi/kararlar/[decisionId]/page.tsx",
  ]) {
    const source = readFileSync(path.join(appRoot, relative), "utf8");
    assert.match(source, /code === "authorization-denied"/, `${relative} hard-codes a denial for every gate failure`);
  }
});
