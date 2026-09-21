import assert from "node:assert/strict";
import { test } from "node:test";

import {
  BULK_DATA_ONBOARDING_ROUTE,
  BULK_DATA_ONBOARDING_TITLE,
  BULK_ONBOARDING_WORKFLOW_STEPS,
} from "../src/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";
import { primaryNavigation } from "../src/lib/navigation/primary-navigation";

test("bulk data onboarding route is registered in navigation", () => {
  const sistem = primaryNavigation.find((group) => group.label === "Sistem");
  assert.ok(sistem);
  const item = sistem!.items.find((entry) => entry.href === BULK_DATA_ONBOARDING_ROUTE);
  assert.ok(item);
  assert.equal(item!.label, "Veri Aktarım Merkezi");
  assert.equal(item!.permissionKey, "bulk-onboarding.read");
});

test("bulk data onboarding workflow includes required steps", () => {
  assert.equal(BULK_DATA_ONBOARDING_TITLE, "Veri Aktarım Merkezi");
  assert.ok(BULK_ONBOARDING_WORKFLOW_STEPS.includes("Dry Run"));
  assert.ok(BULK_ONBOARDING_WORKFLOW_STEPS.includes("Mutabakat"));
  assert.ok(BULK_ONBOARDING_WORKFLOW_STEPS.includes("Karantina"));
});
