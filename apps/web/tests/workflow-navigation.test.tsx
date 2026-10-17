import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { DecisionCard } from "../src/components/workspace/decision-card";
import { FactoryContextBreadcrumb } from "../src/components/workspace/factory-context-breadcrumb";
import { Sidebar } from "../src/components/app-shell/sidebar";
import { FactoryWorkspacePrimaryNav } from "../src/features/factory-workspace/components/factory-workspace-primary-nav";
import { appsCatalogItems } from "../src/lib/navigation/apps-catalog-navigation";
import { buildCanonicalTaskCenterRedirect } from "../src/lib/navigation/legacy-task-routes";
import { navigationItems, primaryNavigation } from "../src/lib/navigation/primary-navigation";
import {
  resolveRoleLandingPath,
  resolveRoleLandingProfile,
} from "../src/lib/navigation/role-landing";
import {
  HIDDEN_PLACEHOLDER_NAV_HREFS,
  WORKFLOW_PRIMARY_HOMES,
  resolveActiveWorkflowHome,
  resolveContextualNavigationSection,
} from "../src/lib/navigation/workflow-primary-navigation";
import { workflowNavigationFixture } from "./fixtures/workflow-navigation-fixture";

test("workflow primary navigation exposes five homes", () => {
  assert.equal(WORKFLOW_PRIMARY_HOMES.length, 5);
  assert.deepEqual(
    WORKFLOW_PRIMARY_HOMES.map((item) => item.label),
    ["Bugün", "Fabrika", "Plan", "İşler", "Uygulamalar"],
  );
});

test("legacy route catalog remains intact with 53 destinations", () => {
  assert.equal(navigationItems.length, 53);
  assert.ok(primaryNavigation.some((group) => group.items.some((item) => item.href === "/planlama")));
  assert.ok(primaryNavigation.some((group) => group.items.some((item) => item.href === "/dijital-is-gucu")));
});

test("placeholder nav routes are hidden from apps catalog", () => {
  for (const href of HIDDEN_PLACEHOLDER_NAV_HREFS) {
    assert.ok(!appsCatalogItems.some((item) => item.href === href));
  }
});

test("sidebar renders workflow-first primary navigation", () => {
  const markup = renderToStaticMarkup(
    <Sidebar
      pathname="/fabrika"
      collapsed={false}
      onToggle={() => undefined}
      navigation={workflowNavigationFixture}
    />,
  );

  assert.match(markup, /navigation-label">Bugün</);
  assert.match(markup, /navigation-label">Fabrika</);
  assert.match(markup, /navigation-label">Plan</);
  assert.match(markup, /navigation-label">İşler</);
  assert.match(markup, /navigation-label">Uygulamalar</);
  assert.match(markup, /aria-current="page"[^>]*href="\/fabrika"/);
  assert.match(markup, /navigation-label">Makineler ve Kaynaklar</);
  assert.match(markup, /navigation-label">Genel Bakış</);
  assert.match(markup, /navigation-label">Hatlar</);
  assert.doesNotMatch(markup, /navigation-label">Operasyon Merkezi</);
});

test("contextual navigation resolves by active workflow home", () => {
  assert.equal(resolveActiveWorkflowHome("/"), "bugun");
  assert.equal(resolveActiveWorkflowHome("/uretim/hatlar/line-1"), "fabrika");
  assert.equal(resolveActiveWorkflowHome("/planlama"), "plan");
  assert.equal(resolveActiveWorkflowHome("/dijital-is-gucu/onaylar"), "isler");
  assert.equal(resolveActiveWorkflowHome("/bakim"), "apps");

  const fabrikaSection = resolveContextualNavigationSection("/uretim/kaynaklar");
  assert.equal(fabrikaSection?.homeId, "fabrika");
  assert.ok(
    fabrikaSection?.items.some(
      (item) => item.href === "/uretim/kaynaklar" && item.label === "Makineler ve Kaynaklar",
    ),
  );
  assert.ok(fabrikaSection?.items.some((item) => item.label === "Genel Bakış" && item.href === "/fabrika"));
  assert.ok(fabrikaSection?.items.some((item) => item.label === "Hatlar"));
});

test("factory workspace primary nav exposes machines and resources entry", () => {
  const markup = renderToStaticMarkup(
    <FactoryWorkspacePrimaryNav pathname="/fabrika" /> as ReactElement,
  );

  assert.match(markup, /data-testid="factory-workspace-primary-nav"/);
  assert.match(markup, /Makineler ve Kaynaklar/);
  assert.match(markup, /href="\/uretim\/kaynaklar"/);
});

test("role landing prioritizes planner and operator contexts", () => {
  const plannerAccess = {
    memberReference: "m1",
    grantedPermissionKeys: ["production.planning.read"],
    navigation: { machines: false, materials: false, maintenance: false },
  };
  assert.equal(resolveRoleLandingProfile(plannerAccess), "planner");
  assert.equal(resolveRoleLandingPath(plannerAccess), "/planlama");

  const operatorAccess = {
    memberReference: "m2",
    grantedPermissionKeys: ["ai-task.read"],
    navigation: { machines: false, materials: false, maintenance: false },
  };
  assert.equal(resolveRoleLandingProfile(operatorAccess), "operator");
  assert.equal(resolveRoleLandingPath(operatorAccess), "/");
});

test("decision card primitive follows signal-to-action order", () => {
  const markup = renderToStaticMarkup(
    <DecisionCard
      title="PO-24001"
      signal={{ label: "Sinyal", value: "Termin riski" }}
      cause={{ label: "Neden", value: "Makine duruşu" }}
      impact={{ label: "Etki", value: "120 parça gecikme" }}
      recommendation={{ label: "Öneri", value: "Vardiya uzat" }}
      expectedResult={{ label: "Beklenen sonuç", value: "Termin korunur" }}
      actions={[{ label: "Kararı aç", href: "/fabrika-karar-merkezi/vakalar/c1" }]}
      testId="decision-card-test"
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="decision-card-test"/);
  assert.match(markup, /Sinyal/);
  assert.match(markup, /Neden/);
  assert.match(markup, /Etki/);
  assert.match(markup, /Öneri/);
  assert.match(markup, /Beklenen sonuç/);
  assert.match(markup, /Kararı aç/);
});

test("factory contextual breadcrumb preserves fabrika hierarchy", () => {
  const markup = renderToStaticMarkup(
    <FactoryContextBreadcrumb
      segments={[
        { label: "Hatlar", href: "/uretim/hatlar" },
        { label: "Mixed Line 01" },
      ]}
    /> as ReactElement,
  );

  assert.match(markup, /href="\/fabrika">Fabrika</);
  assert.match(markup, /href="\/uretim\/hatlar">Hatlar</);
  assert.match(markup, /aria-current="page">Mixed Line 01</);
});

test("dashboard legacy route (mock data) is available only in isolated environments", async () => {
  const { default: DashboardPage } = await import("../src/app/(app)/dashboard/page");
  const previous = process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES;
  try {
    delete process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES;
    await assert.rejects(() => DashboardPage({ searchParams: Promise.resolve({ legacy: "1" }) }), /redirect/i);
    process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES = "1";
    const legacyMarkup = renderToStaticMarkup(
      await DashboardPage({ searchParams: Promise.resolve({ legacy: "1" }) }),
    );
    assert.match(legacyMarkup, /data-testid="dashboard-legacy-banner"/);
    assert.match(legacyMarkup, /Bugün/);
  } finally {
    if (previous === undefined) delete process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES;
    else process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES = previous;
  }
});

test("legacy ai-gorevleri list route preserves query compatibility", () => {
  assert.equal(
    buildCanonicalTaskCenterRedirect({ gorunum: "onay", kaynak: "teams" }),
    "/?gorunum=onay&kaynak=teams",
  );
  assert.equal(buildCanonicalTaskCenterRedirect(undefined), "/");
});
