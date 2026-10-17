import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { Activity } from "lucide-react";

import { GET as getHealth } from "../src/app/health/route";
import { AppShellFrame } from "../src/components/app-shell/app-shell-frame";
import { buildShellSearchItems } from "../src/components/app-shell/shell-navigation";
import { Sidebar } from "../src/components/app-shell/sidebar";
import { KpiCard } from "../src/components/ui/card";
import { PageHeader } from "../src/components/ui/page";
import { navigationItems, primaryNavigation } from "../src/lib/navigation/primary-navigation";
import { WORKFLOW_PRIMARY_HOMES } from "../src/lib/navigation/workflow-primary-navigation";
import { workflowNavigationFixture } from "./fixtures/workflow-navigation-fixture";

test("renders the AI task center home without implying live operational data", async () => {
  const { AiTaskListPageContent } = await import(
    "../src/features/ai-task-center/components/ai-task-list-page-content"
  );
  const element = await AiTaskListPageContent({
    listPath: "/",
    showCreateAction: true,
  });
  const markup = renderToStaticMarkup(element as ReactElement);

  assert.match(markup, /Görev Merkezi/);
  assert.match(markup, /Görev Merkezi şu anda alınamıyor|şu anda alınamıyor/);
  assert.doesNotMatch(markup, /Active Production Orders|Live Alarms/);
});

test("app shell renders landmarks, skip link, top bar, and content viewport", () => {
  const markup = renderToStaticMarkup(
    <AppShellFrame navigation={primaryNavigation} searchItems={buildShellSearchItems(primaryNavigation)}>
      <p>Shell content</p>
    </AppShellFrame>,
  );

  assert.match(markup, /Ana içeriğe geç/);
  assert.match(markup, /aria-label="Ana uygulama navigasyonu"/);
  assert.match(markup, /<header class="top-bar">/);
  assert.match(markup, /<main class="content-viewport" id="main-content"/);
});

test("sidebar renders workflow-first primary navigation and marks Bugün active", () => {
  const markup = renderToStaticMarkup(
    <Sidebar pathname="/" collapsed={false} onToggle={() => undefined} navigation={workflowNavigationFixture} />,
  );

  for (const item of WORKFLOW_PRIMARY_HOMES) {
    const escapedLabel = item.label
      .replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
      .replaceAll("&", "&amp;");
    assert.match(markup, new RegExp(escapedLabel));
  }

  assert.equal(WORKFLOW_PRIMARY_HOMES.length, 5);
  assert.equal(navigationItems.length, 53);
  assert.ok(
    navigationItems.some(
      (item) => item.label === "Fabrika Karar Merkezi" && item.href === "/fabrika-karar-merkezi",
    ),
  );
  assert.match(markup, /aria-current="page"[^>]*href="\/"/);
  assert.doesNotMatch(markup, /navigation-label">Operasyon Merkezi</);
});

test("collapsed sidebar preserves accessible labels and expandable control", () => {
  const markup = renderToStaticMarkup(
    <Sidebar pathname="/fabrika" collapsed onToggle={() => undefined} navigation={workflowNavigationFixture} />,
  );

  assert.match(markup, /data-collapsed="true"/);
  assert.match(markup, /aria-label="Kenar çubuğunu genişlet"/);
  assert.match(markup, /aria-expanded="false"/);
  assert.match(markup, /title="Fabrika"/);
  assert.match(markup, /aria-current="page"[^>]*href="\/fabrika"/);
});

test("page header and KPI variants render typed foundational treatments", () => {
  const markup = renderToStaticMarkup(
    <>
      <PageHeader title="Test başlığı" description="Açıklama" preview />
      <KpiCard label="Durum" value="—" detail="Önizleme" icon={<Activity />} tone="success" preview />
    </>,
  );

  assert.match(markup, /<h1>Test başlığı<\/h1>/);
  assert.match(markup, /kpi-card kpi-success/);
  assert.match(markup, /class="kpi-value">—<\/div>/);
});

test("all interactive shell controls use keyboard-focusable native elements", () => {
  const markup = renderToStaticMarkup(
    <AppShellFrame navigation={primaryNavigation} searchItems={buildShellSearchItems(primaryNavigation)}>Content</AppShellFrame>,
  );

  assert.match(markup, /<a class="navigation-link"/);
  assert.match(markup, /<button class="icon-button/);
  assert.doesNotMatch(markup, /<div[^>]+onClick=/);
});

test("health route remains unaffected", async () => {
  const response = await getHealth();
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { status: "Healthy" });
});
