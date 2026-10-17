import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { FactoryDecisionCenterView } from "../src/features/factory-decision-center/components/factory-decision-center-view";
import type {
  FactoryDecisionCaseQueueItemApi,
  FactoryDecisionCenterWorkspaceApi,
} from "../src/features/factory-decision-center/server/factory-decision-center-api-client";
import { appsCatalogItems } from "../src/lib/navigation/apps-catalog-navigation";
import { primaryNavigation } from "../src/lib/navigation/primary-navigation";
import { CONTEXTUAL_NAVIGATION_SECTIONS, WORKFLOW_PRIMARY_HOMES } from "../src/lib/navigation/workflow-primary-navigation";
import {
  deferredSurfacesEnabled,
  isDeferredSurfaceHref,
  isSurfaceHrefVisible,
} from "../src/lib/runtime/deferred-surfaces";

// Regression guards for the Production audit: the deferred Digital Workforce wording (P1) and the internal Intelligence
// Runtime tooling (P2-D) must never reach a normal Production installation.
const srcRoot = path.resolve(__dirname, "..", "src");
const PRODUCTION = { NODE_ENV: "production" } as const;
const DEMO = { NODE_ENV: "production", NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL: "DEMO" } as const;

function walk(directory: string, found: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full, found);
    else found.push(full);
  }
  return found;
}

const rel = (file: string) => path.relative(srcRoot, file).split(path.sep).join("/");
const sources = walk(srcRoot).filter((file) => /\.(ts|tsx)$/.test(file));

function stripComments(text: string): string {
  return text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|[^:"'`])\/\/.*$/gm, "$1");
}

const WORKFORCE_COPY = /dijital\s+(iş\s+gücü|çalışan)|digital\s+(workforce|worker|employee)/;
const mentionsWorkforce = (text: string) => WORKFORCE_COPY.test(stripComments(text).toLocaleLowerCase("tr-TR"));

// Folders and routes that only exist for the deferred / demo deployments (their layouts answer 404 in Production).
const GATED_FOLDERS = [
  "features/digital-workforce/",
  "app/(app)/dijital-is-gucu/",
  "features/golden-demo-",
  "app/(app)/golden-demo/",
  "features/presentation-mode/",
  "app/(app)/sunum-modu/",
];
const ALLOWED_FILES = new Set(["lib/runtime/deferred-surfaces.ts", "components/dashboard/digital-workforce-panel.tsx"]);

// Files that legitimately name the module but only render it behind the server decision. Each must carry a gate token.
const GATED_BY_TOKEN: Readonly<Record<string, RegExp>> = {
  "components/dashboard/active-shift-panel.tsx": /deferredSurfacesEnabled\(\)/,
  "features/executive-command-center/application/executive-command-center-builder.ts": /digitalWorkforce \?|digitalWorkforce &&/,
  "features/executive-command-center/components/executive-command-center-view.tsx": /digitalWorkforce\.available/,
  "features/factory-decision-center/components/factory-decision-center-view.tsx": /showWorkforce \?/,
  "features/factory-decision-center/components/factory-decision-case-detail-view.tsx": /\{showWorkforce \? \(/,
  "features/factory-orchestration/components/factory-case-detail-view.tsx": /showWorkforce/,
  "features/isler-hub/server/isler-hub-queries.ts": /deferredSurfacesEnabled\(\)/,
  "features/production-manager/components/factory-decision-detail-view.tsx": /deferredSurfacesEnabled\(\) \?/,
  "features/shift-operations/components/shift-operations-active-shift-view.tsx": /deferredSurfacesEnabled\(\) \?/,
  "features/shift-operations/components/shift-operations-overview-view.tsx": /deferredSurfacesEnabled\(\) \?/,
  "features/works/components/works-workspace-view.tsx": /showWorkforce &&/,
  // Navigation definitions: every entry is filtered by isSurfaceHrefVisible (proved below).
  "lib/navigation/primary-navigation.ts": /dijital-is-gucu/,
  "lib/navigation/workflow-primary-navigation.ts": /dijital-is-gucu/,
};

test("regression guard: Digital Workforce wording appears in Production-rendered copy only behind the deferred gate", () => {
  const offenders: string[] = [];
  for (const file of sources) {
    const key = rel(file);
    if (ALLOWED_FILES.has(key) || GATED_FOLDERS.some((folder) => key.startsWith(folder))) continue;
    const text = readFileSync(file, "utf8");
    if (!mentionsWorkforce(text)) continue;
    const token = GATED_BY_TOKEN[key];
    if (!token || !token.test(stripComments(text))) offenders.push(key);
  }
  assert.deepEqual(offenders, [], "ungated Digital Workforce wording in a Production-reachable file");
});

test("regression guard: shared label constants never carry Digital Workforce wording", () => {
  for (const key of [
    "features/factory-decision-center/domain/factory-decision-center-labels.ts",
    "features/shift-operations/domain/shift-operations-labels.ts",
  ]) {
    assert.equal(mentionsWorkforce(readFileSync(path.join(srcRoot, key), "utf8")), false, key);
  }
});

test("regression guard: Digital Workforce navigation entries are filtered out in Production and shown when deferred surfaces are on", () => {
  const items = [
    ...primaryNavigation.flatMap((group) => group.items),
    ...WORKFLOW_PRIMARY_HOMES,
    ...CONTEXTUAL_NAVIGATION_SECTIONS.flatMap((section) => section.items),
    ...appsCatalogItems,
  ];
  const workforce = items.filter((item) => item.href.startsWith("/dijital-is-gucu") || /dijital iş gücü/i.test(item.label));
  assert.ok(workforce.length > 0, "the navigation definitions still contain the deferred entries");
  for (const item of workforce) {
    assert.equal(isSurfaceHrefVisible(item.href, PRODUCTION), false, `${item.href} must be hidden in Production`);
    assert.equal(isSurfaceHrefVisible(item.href, DEMO), true, `${item.href} must be visible in Demo`);
  }
  const visibility = readFileSync(path.join(srcRoot, "lib/navigation/navigation-visibility.ts"), "utf8");
  assert.match(visibility, /\.filter\(\(item\) => isSurfaceHrefVisible\(item\.href\)\)/);
});

function queueItem(overrides: Partial<FactoryDecisionCaseQueueItemApi> = {}): FactoryDecisionCaseQueueItemApi {
  return {
    caseId: "case-1",
    title: "Teslimat riski",
    severity: "critical",
    status: "executing",
    orderReference: "ORD-8742",
    customerName: "Müşteri A",
    deliveryCommitmentLabel: "18 Eylül",
    riskSummary: "Üretim açığı",
    requiredQuantity: 1000,
    projectedQuantity: 820,
    gapQuantity: 180,
    recommendedStrategy: "Kurtarma planı",
    digitalWorkerNames: ["Üretim Kurtarma Dijital Çalışanı"],
    verificationState: "pending",
    updatedAtUtc: "2026-09-16T09:12:00.000Z",
    concurrencyToken: 1,
    ...overrides,
  };
}

function workspace(withCase: boolean): FactoryDecisionCenterWorkspaceApi {
  const queue = withCase ? [queueItem()] : [];
  return {
    summary: { criticalDecisions: queue.length, outcomesAtRisk: queue.length, pendingHumanApproval: 0, recoveryOperations: queue.length, pendingVerification: 0 },
    priorityQueue: queue,
    liveQueues: {
      criticalDecisions: queue,
      activeRecoveryOperations: queue,
      pendingHumanApproval: [],
      blockedOrEscalated: [],
      pendingVerification: [],
      verifiedClosedToday: [],
    },
    earlyWarnings: {
      criticalPredictedRisks: [],
      deliveryRisks: [],
      machineDegradationTrends: [],
      materialDepletionRisks: [],
      qualityDeteriorationTrends: [],
    },
    generatedAtUtc: "2026-09-16T09:12:00.000Z",
  };
}

test("regression guard: Fabrika Karar Merkezi renders no Digital Workforce wording with the deferred flag off", () => {
  const html = renderToStaticMarkup(<FactoryDecisionCenterView workspace={workspace(true)} />);
  assert.doesNotMatch(html.toLocaleLowerCase("tr-TR"), WORKFORCE_COPY);
  assert.doesNotMatch(html, /Üretim Kurtarma Dijital/);
  assert.match(html, /kurtarma operasyonlarını tek merkezden yönetin/);
});

test("Fabrika Karar Merkezi shows the workforce block only when the server enables the deferred surfaces", () => {
  const html = renderToStaticMarkup(<FactoryDecisionCenterView workspace={workspace(true)} showWorkforce />);
  assert.match(html, /Dijital İş Gücü/);
});

test("Fabrika Karar Merkezi queues render as equal structured cards with title, count and status", () => {
  const html = renderToStaticMarkup(<FactoryDecisionCenterView workspace={workspace(true)} />);
  assert.equal((html.match(/class="fdc-queue-card"/g) ?? []).length, 6);
  assert.match(html, /fdc-queue-card__count/);
  assert.match(html, /Müdahale gerekli/);
  assert.match(html, /Bekleyen yok/);
  const empty = renderToStaticMarkup(<FactoryDecisionCenterView workspace={workspace(false)} />);
  assert.match(empty, /Bekleyen karar yok/);
  assert.equal((empty.match(/class="fdc-queue-card"/g) ?? []).length, 6);
  assert.doesNotMatch(empty, /fdc-clear__list/);
});

// P2-D: Intelligence Runtime is internal ML tooling.

test("regression guard: /intelligence-runtime is a deferred (internal) surface hidden from Production navigation", () => {
  for (const href of ["/intelligence-runtime", "/intelligence-runtime/anything"]) {
    assert.equal(isDeferredSurfaceHref(href), true, href);
    assert.equal(isSurfaceHrefVisible(href, PRODUCTION), false, href);
    assert.equal(isSurfaceHrefVisible(href, DEMO), true, href);
  }
  assert.equal(isSurfaceHrefVisible("/intelligence-runtime", { NODE_ENV: "production", NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES: "1" }), true);
  assert.equal(deferredSurfacesEnabled(PRODUCTION), false);
  assert.equal(isDeferredSurfaceHref("/intelligence-runtimes"), false);
});

test("regression guard: the intelligence runtime route fails closed in its layout and no customer page links to it", () => {
  const layout = readFileSync(path.join(srcRoot, "app/(app)/intelligence-runtime/layout.tsx"), "utf8");
  assert.match(layout, /deferredSurfacesEnabled\(\)[\s\S]{0,200}notFound\(\)/);
  const linking = sources
    .map((file) => rel(file))
    .filter((key) => !key.startsWith("features/intelligence-runtime/") && !key.startsWith("app/(app)/intelligence-runtime/"))
    .filter((key) => {
      const text = stripComments(readFileSync(path.join(srcRoot, key), "utf8"));
      return /href=[^\n]*intelligence-runtime|router\.(push|replace)\([^)]*intelligence-runtime/.test(text);
    });
  assert.deepEqual(linking, []);
});

test("regression guard: the intelligence runtime route answers a middleware 404 in Production and is reachable with the internal flag", async () => {
  const { middleware } = await import("../src/middleware");
  const { NextRequest } = await import("next/server");
  const saved = { ...process.env };
  try {
    delete process.env.NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL;
    delete process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES;
    (process.env as Record<string, string>).NODE_ENV = "production";
    const blocked = middleware(new NextRequest("http://localhost/intelligence-runtime"));
    assert.equal(blocked.status, 404);
    assert.match(blocked.headers.get("x-middleware-rewrite") ?? "", /__deferred-surface-not-found/);
    process.env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES = "1";
    const allowed = middleware(new NextRequest("http://localhost/intelligence-runtime"));
    assert.doesNotMatch(allowed.headers.get("x-middleware-rewrite") ?? "", /__deferred-surface-not-found/);
  } finally {
    for (const key of Object.keys(process.env)) if (!(key in saved)) delete process.env[key];
    Object.assign(process.env, saved);
  }
});
