import assert from "node:assert/strict";
import test from "node:test";

import {
  acceptanceRoleContract,
  isSubscriptionExemptCustomerWorkflowPath,
  requiredRoutePermission,
  requiredRouteQueryPermission,
  routeAccessForPermissions,
} from "../src/lib/authorization/acceptance-role-contract";
import { appsCatalogItems } from "../src/lib/navigation/apps-catalog-navigation";
import { isNavigationItemVisible } from "../src/lib/navigation/navigation-visibility";
import { navigationItems } from "../src/lib/navigation/primary-navigation";
import {
  CONTEXTUAL_NAVIGATION_SECTIONS,
  WORKFLOW_PRIMARY_HOMES,
} from "../src/lib/navigation/workflow-primary-navigation";

const subscriptionUnknown = { status: "failure" as const, message: "unavailable" };
const subscriptionDenied = {
  status: "success" as const,
  value: {
    paidApplicationAccess: { allowed: false },
    entitlements: [],
    moduleAccess: { shiftWorkforce: { allowed: false } },
    limits: {
      users: { allowed: false },
      facilities: { allowed: false },
      connectedMachines: { allowed: false },
    },
    subscription: { status: "suspended" },
    evaluatedAtUtc: "2026-09-22T00:00:00Z",
  },
} as unknown as Parameters<typeof isNavigationItemVisible>[2];

test("acceptance role sees required workflow navigation", () => {
  const granted = acceptanceRoleContract.requiredPermissionKeys;
  for (const home of WORKFLOW_PRIMARY_HOMES) {
    assert.equal(
      isNavigationItemVisible(home, granted, subscriptionUnknown),
      true,
      home.label,
    );
  }

  const fabrika = CONTEXTUAL_NAVIGATION_SECTIONS.find((section) => section.homeId === "fabrika");
  for (const href of ["/fabrika", "/uretim/hatlar", "/uretim/kaynaklar"]) {
    const item = fabrika?.items.find((candidate) => candidate.href === href);
    assert.ok(item, href);
    assert.equal(isNavigationItemVisible(item, granted, subscriptionDenied), true, href);
  }

  const dataTransfer = navigationItems.find((item) => item.href === "/ayarlar/veri-aktarim-merkezi");
  assert.equal(dataTransfer?.permissionKey, requiredRoutePermission("veri-aktarim"));
  assert.equal(isNavigationItemVisible(dataTransfer!, granted, subscriptionDenied), true);
  assert.ok(appsCatalogItems.some((item) => item.href === "/ayarlar/veri-aktarim-merkezi"));
});

test("missing permission hides the item and does not authorize the route", () => {
  const restricted: string[] = [];
  for (const routeId of ["fabrika", "hatlar", "kaynaklar", "plan", "isler", "veri-aktarim", "factory-copilot"]) {
    assert.equal(routeAccessForPermissions(routeId, restricted), "deny", routeId);
  }

  const fabrika = WORKFLOW_PRIMARY_HOMES.find((item) => item.href === "/fabrika");
  const overview = CONTEXTUAL_NAVIGATION_SECTIONS
    .find((section) => section.homeId === "fabrika")
    ?.items.find((item) => item.href === "/fabrika");
  const bugun = WORKFLOW_PRIMARY_HOMES.find((item) => item.href === "/");
  const apps = WORKFLOW_PRIMARY_HOMES.find((item) => item.href === "/uygulamalar");
  assert.equal(isNavigationItemVisible(fabrika!, restricted, subscriptionUnknown), false);
  assert.equal(isNavigationItemVisible(overview!, restricted, subscriptionUnknown), false);
  assert.equal(isNavigationItemVisible(bugun!, restricted, subscriptionUnknown), true);
  assert.equal(isNavigationItemVisible(apps!, restricted, subscriptionUnknown), true);
  assert.equal(routeAccessForPermissions("bugun", restricted), "allow");
});

test("core workflow routes stay visible when an unrelated entitlement is denied", () => {
  const granted = acceptanceRoleContract.requiredPermissionKeys;
  for (const path of ["/", "/fabrika", "/uretim/hatlar", "/uretim/kaynaklar", "/planlama", "/isler", "/uygulamalar", "/ayarlar/veri-aktarim-merkezi"]) {
    assert.equal(isSubscriptionExemptCustomerWorkflowPath(path), true, path);
  }

  const maintenance = navigationItems.find((item) => item.href === "/bakim");
  assert.ok(maintenance);
  assert.equal(isNavigationItemVisible(maintenance, granted, subscriptionDenied), false);
});

test("copilot query permission is distinct from the shell read permission", () => {
  assert.equal(requiredRoutePermission("factory-copilot"), "factory-copilot.read");
  assert.equal(requiredRouteQueryPermission("factory-copilot"), "factory-copilot.query");
  assert.equal(acceptanceRoleContract.catalogVersion, "2026.09.22.1");
  for (const key of acceptanceRoleContract.forbiddenPermissionKeys) {
    assert.equal(acceptanceRoleContract.requiredPermissionKeys.includes(key), false, key);
  }
});
