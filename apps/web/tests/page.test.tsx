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

test("renders the AI task center home without implying live operational data", async () => {
  const { AiTaskListPageContent } = await import(
    "../src/features/ai-task-center/components/ai-task-list-page-content"
  );
  const element = await AiTaskListPageContent({
    listPath: "/",
    showCreateAction: true,
  });
  const markup = renderToStaticMarkup(element as ReactElement);

  assert.match(markup, /AI Görev Merkezi/);
  assert.match(markup, /Gerçek görev verisi bağlı değil/);
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

test("sidebar renders the exact locked navigation and marks AI Task Center active", () => {
  const markup = renderToStaticMarkup(
    <Sidebar pathname="/" collapsed={false} onToggle={() => undefined} navigation={primaryNavigation} />,
  );

  for (const item of navigationItems) {
    const escapedLabel = item.label
      .replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
      .replaceAll("&", "&amp;");
    assert.match(markup, new RegExp(escapedLabel));
  }
  assert.equal(navigationItems.length, 45);
  assert.ok(
    navigationItems.some(
      (item) => item.label === "İhracat" && item.href === "/ihracat",
    ),
  );
  const productionNavIndex = navigationItems.findIndex(
    (item) => item.label === "Üretim",
  );
  const exportNavIndex = navigationItems.findIndex(
    (item) => item.label === "İhracat",
  );
  assert.equal(exportNavIndex, productionNavIndex + 1);
  assert.match(markup, /navigation-label">İhracat</);
  assert.match(markup, /href="\/ihracat"/);
  assert.match(markup, /aria-current="page"[^>]*href="\/"/);
  assert.doesNotMatch(markup, />Finance</i);
  assert.doesNotMatch(markup, />Accounting</i);
  assert.doesNotMatch(markup, />Payroll</i);
  assert.doesNotMatch(markup, />Official Stock/i);
});

test("collapsed sidebar preserves accessible labels and expandable control", () => {
  const markup = renderToStaticMarkup(
    <Sidebar pathname="/dashboard" collapsed onToggle={() => undefined} navigation={primaryNavigation} />,
  );

  assert.match(markup, /data-collapsed="true"/);
  assert.match(markup, /aria-label="Kenar çubuğunu genişlet"/);
  assert.match(markup, /aria-expanded="false"/);
  assert.match(markup, /title="AI Görev Merkezi"/);
  assert.match(
    markup,
    /aria-current="page"[^>]*title="Dashboard \/ Operasyon Merkezi"[^>]*href="\/dashboard"|aria-current="page"[^>]*href="\/dashboard"[^>]*title="Dashboard \/ Operasyon Merkezi"/,
  );
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
