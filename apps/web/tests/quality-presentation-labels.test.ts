import assert from "node:assert/strict";
import test from "node:test";

import { getQualityStatusLabel } from "../src/features/quality-control/domain/quality-control";
import {
  formatQualityTrendLabel,
  hasEnoughQualityTrendData,
} from "../src/features/quality-control/presentation/quality-reporting-labels";
import { getQualityCategoryLabel } from "../src/features/quality-control/presentation/quality-ui-labels";

test("quality categories are shown in Turkish, never as raw English values", () => {
  assert.equal(getQualityCategoryLabel("surface"), "Yüzey");
  assert.equal(getQualityCategoryLabel("Surface"), "Yüzey");
  assert.equal(getQualityCategoryLabel("dimensional"), "Ölçü");
  assert.equal(getQualityCategoryLabel("inspection-failure"), "Kontrol uygunsuzluğu");
  assert.equal(getQualityCategoryLabel(""), "—");
  assert.equal(getQualityCategoryLabel(undefined), "—");
  assert.equal(getQualityCategoryLabel("major"), "Majör");
});

test("the same NCR status reads identically on every quality surface", () => {
  assert.equal(getQualityStatusLabel("action-required"), "Aksiyon gerekli");
  assert.notEqual(getQualityStatusLabel("action-required"), "Action required");
});

test("trend labels are compact and well formed", () => {
  assert.equal(formatQualityTrendLabel("week", "2026-W42", "2026-10-12T00:00:00Z"), "H42");
  assert.equal(formatQualityTrendLabel("week", "2026-W07", "2026-02-09T00:00:00Z"), "H7");
  assert.doesNotMatch(formatQualityTrendLabel("day", "2026-10-12", "2026-10-12T00:00:00Z"), /\d{4}$/);
});

test("a trend chart needs two periods and distinct integer ticks", () => {
  assert.equal(hasEnoughQualityTrendData([]), false);
  assert.equal(hasEnoughQualityTrendData([{ passed: 3, failed: 1, marker: 1 }]), false);
  assert.equal(
    hasEnoughQualityTrendData([
      { passed: 1, failed: 0, marker: 0 },
      { passed: 0, failed: 0, marker: 1 },
    ]),
    false,
  );
  assert.equal(
    hasEnoughQualityTrendData([
      { passed: 2, failed: 0, marker: 0 },
      { passed: 1, failed: 1, marker: 1 },
    ]),
    true,
  );
});
