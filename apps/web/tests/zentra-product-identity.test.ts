import assert from "node:assert/strict";
import { describe, it } from "node:test";

import {
  ZENTRA_BRAND,
  buildZentraDocumentTitle,
} from "../src/lib/brand/zentra-brand";
import { readZentraEnv } from "../src/lib/config/zentra-env";
import { getWebApplicationConfig } from "../src/config";

describe("Zentra product identity", () => {
  it("uses Zentra branding constants", () => {
    assert.equal(ZENTRA_BRAND.productName, "Zentra");
    assert.equal(ZENTRA_BRAND.fullProductName, "Nevaxis Zentra");
    assert.match(buildZentraDocumentTitle(), /Zentra/);
    assert.doesNotMatch(buildZentraDocumentTitle(), /FactOS/i);
  });

  it("prefers NEVAXIS_ZENTRA env over legacy prefixes", () => {
    const suffix = "WEB_APPLICATION_NAME";
    const env = {
      ...process.env,
      [`NEVAXIS_ZENTRA_${suffix}`]: "Zentra",
      [`NEVAXIS_FACTOS_${suffix}`]: "FactOS",
      [`CREVAFACTOS_${suffix}`]: "Creva",
    };

    assert.equal(readZentraEnv(suffix, env), "Zentra");
    assert.equal(getWebApplicationConfig(env).applicationName, "Zentra");
  });
});
