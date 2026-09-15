import assert from "node:assert/strict";
import test from "node:test";

import { hasGrantedPermission } from "../src/features/admin-access/server/admin-access-effective-access";
import { navigationItems, primaryNavigation } from "../src/lib/navigation/primary-navigation";
import {
  mapCriticalOrderPresentationStatus,
  mapCustomsPresentationStatus,
  mapProductionRecoveryPresentationStatus,
} from "../src/features/presentation-mode/domain/presentation-mode-status";
import { PRESENTATION_MODE_ACCESS_PERMISSION } from "../src/features/presentation-mode/server/require-presentation-mode-access";

test("presentation mode navigation entry is permission gated", () => {
  const item = navigationItems.find((candidate) => candidate.href === "/sunum-modu");
  assert.ok(item);
  assert.equal(item?.label, "Sunum Modu");
  assert.equal(item?.permissionKey, PRESENTATION_MODE_ACCESS_PERMISSION);
});

test("golden demo routes are not exposed in primary navigation", () => {
  assert.ok(!navigationItems.some((item) => item.href.startsWith("/golden-demo/")));
});

test("sunum modu navigation is visible only with presentation permission", () => {
  const item = primaryNavigation
    .flatMap((group) => group.items)
    .find((candidate) => candidate.href === "/sunum-modu");

  assert.ok(item?.permissionKey);
  assert.equal(
    hasGrantedPermission(
      { memberReference: null, grantedPermissionKeys: [PRESENTATION_MODE_ACCESS_PERMISSION], navigation: { machines: false, materials: false, maintenance: false } },
      item.permissionKey,
    ),
    true,
  );
  assert.equal(
    hasGrantedPermission(
      { memberReference: null, grantedPermissionKeys: ["production.execution.read"], navigation: { machines: false, materials: false, maintenance: false } },
      item.permissionKey,
    ),
    false,
  );
});

test("production recovery presentation status maps from live scenario state", () => {
  const ready = mapProductionRecoveryPresentationStatus(
    {
      event: { id: "evt", status: "active", machineCode: "INJ-001" },
      plan: { requiresApproval: true, approvedAtUtc: null },
    } as never,
    { stepNumber: 4, isComplete: false } as never,
  );
  assert.equal(ready, "İnsan Onayı Bekliyor");

  const verified = mapProductionRecoveryPresentationStatus(
    {
      event: { id: "evt", status: "verified", machineCode: "INJ-001" },
      plan: null,
    } as never,
    { stepNumber: 12, isComplete: true } as never,
  );
  assert.equal(verified, "VERIFIED");
});

test("customs presentation status maps from outcome status", () => {
  assert.equal(
    mapCustomsPresentationStatus({ status: "approval-pending" } as never),
    "İnsan Onayı Bekliyor",
  );
  assert.equal(
    mapCustomsPresentationStatus({ status: "verified-ready" } as never),
    "VERIFIED",
  );
});

test("critical order presentation status maps from factory decision case", () => {
  assert.equal(
    mapCriticalOrderPresentationStatus({
      status: "awaiting-approval",
      verificationState: "pending",
    } as never),
    "İnsan Onayı Bekliyor",
  );
  assert.equal(
    mapCriticalOrderPresentationStatus({
      status: "closed",
      verificationState: "passed",
    } as never),
    "VERIFIED",
  );
});
